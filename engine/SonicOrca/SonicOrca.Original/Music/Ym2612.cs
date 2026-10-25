using System;

namespace SonicOrca.Original.Music;

/// <summary>
/// An emulator of the Yamaha YM2612 (OPN2), the FM sound chip of the Mega Drive. The chip runs at its native sample
/// rate (the clock divided by 144). A windowed-sinc filter then converts the output to the sample rate of the caller.
/// The emulator uses log-sine and exponent tables, the real envelope rate tables and 14-bit operator output, as the
/// hardware does. It does not emulate the timers, SSG-EG, CSM key on or the ladder effect of the DAC.
/// </summary>
public sealed class Ym2612
{
	/// <summary>The master clock of an NTSC Mega Drive in Hz.</summary>
	public const double NtscClock = 7670453.0;

	/// <summary>The scale from the 14-bit output of a channel to a float sample.</summary>
	private const float OutputScale = 1.0f / 45056.0f;

	/// <summary>Half the number of taps of the resampling filter.</summary>
	private const int HalfTaps = 16;

	private const int Taps = HalfTaps * 2;

	/// <summary>The number of fractional positions in the table of filter coefficients.</summary>
	private const int FilterPhases = 512;

	private const int HistorySize = 64;

	private const int HistoryMask = HistorySize - 1;

	private const int MaxAttenuation = 0x3FF;

	private const int StateAttack = 0;

	private const int StateDecay = 1;

	private const int StateSustain = 2;

	private const int StateRelease = 3;

	/// <summary>A quarter of a sine wave as attenuation in units of 1/256 of a factor of 2 (4.8 fixed point).</summary>
	private static readonly int[] LogSine = new int[256];

	/// <summary>The value 2^(-x) for the fraction x of an attenuation, as an 11-bit value with a leading 1.</summary>
	private static readonly int[] Exponent = new int[256];

	/// <summary>
	/// The detune (DT1) of the phase increment for each detune value 0-3 (32 entries each) and each key code. This is
	/// the table from the data sheet of the chip.
	/// </summary>
	private static readonly int[] DetuneTable =
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2,
		2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7, 8, 8, 8, 8,
		1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5,
		5, 6, 6, 7, 8, 8, 9, 10, 11, 12, 13, 14, 16, 16, 16, 16,
		2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 6, 6, 7,
		8, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 20, 22, 22, 22, 22,
	};

	/// <summary>The envelope increment for each rate (8 entries each) and each step of the 8-step cycle.</summary>
	private static readonly int[] EnvelopeIncrement = BuildEnvelopeIncrements();

	/// <summary>The number of samples between two steps of the LFO for each LFO frequency.</summary>
	private static readonly int[] LfoPeriods = { 108, 77, 71, 67, 62, 44, 8, 5 };

	/// <summary>The right shift of the LFO amplitude value for each AMS value. AMS 0 gives no modulation.</summary>
	private static readonly int[] AmShifts = { 8, 3, 1, 0 };

	/// <summary>The largest phase modulation of each FMS value in cents.</summary>
	private static readonly double[] PmCents = { 0.0, 3.4, 6.7, 10.0, 14.0, 20.0, 40.0, 80.0 };

	/// <summary>
	/// The offset of the frequency number (in units of a half step of the frequency number) for each FMS value, LFO
	/// level 0-7 and the top 7 bits of the frequency number.
	/// </summary>
	private static readonly int[] PmTable = BuildPmTable();

	/// <summary>The operator for each operator slot of the registers. The registers have the order S1, S3, S2, S4.</summary>
	private static readonly int[] SlotToOperator = { 0, 2, 1, 3 };

	private readonly Channel[] _channels = new Channel[6];

	private readonly float[] _filter;

	private readonly float[] _coefficients = new float[Taps];

	private readonly float[] _historyLeft = new float[HistorySize];

	private readonly float[] _historyRight = new float[HistorySize];

	private readonly double _step;

	private int _newest = -1;

	private int _floor;

	private double _fraction;

	private int _frequencyLatch;

	private int _channel3Latch;

	private int _channel3Mode;

	private bool _lfoEnabled;

	private int _lfoFrequency;

	private int _lfoTimer;

	private int _lfoCounter;

	private int _envelopeDivider;

	private int _envelopeCounter;

	private int _dacValue = 0x80;

	private bool _dacEnabled;

	static Ym2612()
	{
		for (int i = 0; i < 256; i++)
		{
			double angle = (i + 0.5) * Math.PI / 512.0;
			LogSine[i] = (int)Math.Round(-Math.Log2(Math.Sin(angle)) * 256.0);
			Exponent[i] = 1024 + (int)Math.Round((Math.Pow(2.0, (255 - i) / 256.0) - 1.0) * 1024.0);
		}
	}

	/// <summary>
	/// Creates a chip that renders at the given sample rate.
	/// </summary>
	/// <param name="sampleRate">The sample rate of the output in Hz.</param>
	/// <param name="clock">The master clock of the chip in Hz.</param>
	public Ym2612(int sampleRate, double clock = NtscClock)
	{
		if (sampleRate <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(sampleRate));
		}

		for (int i = 0; i < _channels.Length; i++)
		{
			_channels[i] = new Channel();
		}

		double nativeRate = clock / 144.0;
		_step = nativeRate / sampleRate;
		_filter = BuildFilter(Math.Min(1.0, sampleRate / nativeRate));
	}

	/// <summary>
	/// Writes a value to a register. Port 0 gives the registers of part I: the global registers and channels 1-3.
	/// Port 1 gives the registers of part II: channels 4-6.
	/// </summary>
	/// <param name="port">The port: 0 for part I or 1 for part II.</param>
	/// <param name="register">The register number ($21-$B6).</param>
	/// <param name="value">The 8-bit value.</param>
	public void Write(int port, int register, int value)
	{
		register &= 0xFF;
		value &= 0xFF;
		port &= 1;

		if (register < 0x30)
		{
			if (port == 0)
			{
				WriteGlobal(register, value);
			}

			return;
		}

		int channelInPart = register & 3;
		if (channelInPart == 3)
		{
			return;
		}

		int channelIndex = channelInPart + port * 3;
		Channel channel = _channels[channelIndex];

		if (register < 0xA0)
		{
			Operator op = channel.Operators[SlotToOperator[(register >> 2) & 3]];
			switch (register & 0xF0)
			{
				case 0x30:
					op.Detune = (value >> 4) & 7;
					op.Multiple = value & 15;
					UpdateIncrements(channelIndex);
					break;
				case 0x40:
					op.TotalLevel = value & 0x7F;
					break;
				case 0x50:
					op.KeyScale = value >> 6;
					op.AttackRate = value & 31;
					break;
				case 0x60:
					op.AmEnabled = (value & 0x80) != 0;
					op.DecayRate = value & 31;
					break;
				case 0x70:
					op.SustainRate = value & 31;
					break;
				case 0x80:
					op.SustainLevel = value >> 4;
					op.ReleaseRate = value & 15;
					break;
			}

			return;
		}

		switch (register & 0xFC)
		{
			case 0xA0:
				channel.FrequencyNumber = ((_frequencyLatch & 7) << 8) | value;
				channel.Block = (_frequencyLatch >> 3) & 7;
				UpdateIncrements(channelIndex);
				break;
			case 0xA4:
				_frequencyLatch = value & 0x3F;
				break;
			case 0xA8:
				if (port == 0)
				{
					// $A8 sets operator 3, $A9 sets operator 1 and $AA sets operator 2 of channel 3.
					Operator op = _channels[2].Operators[channelInPart == 0 ? 2 : channelInPart - 1];
					op.FrequencyNumber = ((_channel3Latch & 7) << 8) | value;
					op.Block = (_channel3Latch >> 3) & 7;
					UpdateIncrements(2);
				}

				break;
			case 0xAC:
				if (port == 0)
				{
					_channel3Latch = value & 0x3F;
				}

				break;
			case 0xB0:
				channel.Algorithm = value & 7;
				channel.Feedback = (value >> 3) & 7;
				break;
			case 0xB4:
				channel.Left = (value & 0x80) != 0;
				channel.Right = (value & 0x40) != 0;
				channel.Ams = (value >> 4) & 3;
				channel.Fms = value & 7;
				UpdateIncrements(channelIndex);
				break;
		}
	}

	/// <summary>
	/// Renders the output of the chip and adds it to the two buffers. The method keeps the state of the resampler
	/// between calls, so the caller can render very short spans between register writes.
	/// </summary>
	/// <param name="left">The buffer of the left channel.</param>
	/// <param name="right">The buffer of the right channel. It must have the same length as the left buffer.</param>
	public void Render(Span<float> left, Span<float> right)
	{
		if (right.Length < left.Length)
		{
			throw new ArgumentException("The right buffer is shorter than the left buffer.", nameof(right));
		}

		float[] filter = _filter;
		float[] coefficients = _coefficients;
		float[] historyLeft = _historyLeft;
		float[] historyRight = _historyRight;

		for (int i = 0; i < left.Length; i++)
		{
			// Make sure that the history has all the native samples that the filter needs.
			while (_newest - _floor < HalfTaps)
			{
				GenerateNativeSample();
			}

			double position = _fraction * FilterPhases;
			int phase = (int)position;
			float blend = (float)(position - phase);
			int row0 = phase * Taps;
			int row1 = row0 + Taps;
			for (int k = 0; k < Taps; k++)
			{
				float c0 = filter[row0 + k];
				coefficients[k] = c0 + blend * (filter[row1 + k] - c0);
			}

			float sumLeft = 0.0f;
			float sumRight = 0.0f;
			int start = _floor + HalfTaps;
			for (int k = 0; k < Taps; k++)
			{
				int index = (start - k) & HistoryMask;
				sumLeft += historyLeft[index] * coefficients[k];
				sumRight += historyRight[index] * coefficients[k];
			}

			left[i] += sumLeft;
			right[i] += sumRight;

			_fraction += _step;
			int advance = (int)_fraction;
			_fraction -= advance;
			_floor += advance;
		}
	}

	private void WriteGlobal(int register, int value)
	{
		switch (register)
		{
			case 0x22:
				_lfoEnabled = (value & 8) != 0;
				_lfoFrequency = value & 7;
				if (!_lfoEnabled)
				{
					_lfoCounter = 0;
					_lfoTimer = 0;
				}

				for (int i = 0; i < _channels.Length; i++)
				{
					UpdateIncrements(i);
				}

				break;
			case 0x27:
				_channel3Mode = (value >> 6) & 3;
				UpdateIncrements(2);
				break;
			case 0x28:
				int channelIndex = value & 3;
				if (channelIndex == 3)
				{
					return;
				}

				if ((value & 4) != 0)
				{
					channelIndex += 3;
				}

				// Bits 4-7 are the operators S1, S2, S3 and S4.
				Channel channel = _channels[channelIndex];
				for (int i = 0; i < 4; i++)
				{
					if ((value & (0x10 << i)) != 0)
					{
						KeyOn(channel.Operators[i]);
					}
					else
					{
						KeyOff(channel.Operators[i]);
					}
				}

				break;
			case 0x2A:
				_dacValue = value;
				break;
			case 0x2B:
				_dacEnabled = (value & 0x80) != 0;
				break;
		}
	}

	private static void KeyOn(Operator op)
	{
		if (op.KeyIsOn)
		{
			return;
		}

		op.KeyIsOn = true;
		op.Phase = 0;
		op.State = StateAttack;
		if (CalculateRate(op.AttackRate, op) >= 62)
		{
			op.Attenuation = 0;
		}

		if (op.Attenuation == 0)
		{
			EnterDecay(op);
		}
	}

	private static void KeyOff(Operator op)
	{
		if (!op.KeyIsOn)
		{
			return;
		}

		op.KeyIsOn = false;
		op.State = StateRelease;
	}

	private static void EnterDecay(Operator op)
	{
		op.State = op.Attenuation >= SustainAttenuation(op) ? StateSustain : StateDecay;
	}

	/// <summary>The sustain level as attenuation. Each step is 3 dB and level 15 is 93 dB.</summary>
	private static int SustainAttenuation(Operator op)
	{
		return op.SustainLevel == 15 ? 31 << 5 : op.SustainLevel << 5;
	}

	/// <summary>Calculates the envelope rate (0-63) from a 5-bit rate register and the key scale.</summary>
	private static int CalculateRate(int rate, Operator op)
	{
		if (rate == 0)
		{
			return 0;
		}

		int result = 2 * rate + (op.KeyCode >> (3 - op.KeyScale));
		return result > 63 ? 63 : result;
	}

	/// <summary>Calculates the key code (block and the top bits of the frequency number) as the hardware does.</summary>
	private static int CalculateKeyCode(int frequencyNumber, int block)
	{
		int f11 = (frequencyNumber >> 10) & 1;
		int f10 = (frequencyNumber >> 9) & 1;
		int f9 = (frequencyNumber >> 8) & 1;
		int f8 = (frequencyNumber >> 7) & 1;
		int n3 = (f11 & (f10 | f9 | f8)) | ((f11 ^ 1) & f10 & f9 & f8);
		return (block << 2) | (f11 << 1) | n3;
	}

	private bool PhaseModulationActive(Channel channel)
	{
		return _lfoEnabled && channel.Fms != 0;
	}

	/// <summary>Calculates the phase increments and key codes of the four operators of a channel.</summary>
	private void UpdateIncrements(int channelIndex)
	{
		Channel channel = _channels[channelIndex];
		bool special = channelIndex == 2 && _channel3Mode != 0;
		bool modulate = PhaseModulationActive(channel);
		int level = 0;
		bool negative = false;
		if (modulate)
		{
			// The LFO phase value has 32 steps: up 0-7, down 7-0, then the same with a negative sign.
			int step = _lfoCounter >> 2;
			level = (step & 8) == 0 ? step & 7 : 7 - (step & 7);
			negative = (step & 16) != 0;
		}

		for (int i = 0; i < 4; i++)
		{
			Operator op = channel.Operators[i];
			int frequencyNumber = channel.FrequencyNumber;
			int block = channel.Block;
			if (special && i < 3)
			{
				frequencyNumber = op.FrequencyNumber;
				block = op.Block;
			}

			int keyCode = CalculateKeyCode(frequencyNumber, block);
			op.KeyCode = keyCode;

			// Use the frequency number with one more bit of precision for the phase modulation.
			int doubled = frequencyNumber << 1;
			if (modulate && level != 0)
			{
				int offset = PmTable[((channel.Fms << 3) + level) * 128 + (frequencyNumber >> 4)];
				doubled += negative ? -offset : offset;
			}

			int increment = (doubled << block) >> 2;
			int detune = DetuneTable[((op.Detune & 3) << 5) + keyCode];
			increment += (op.Detune & 4) != 0 ? -detune : detune;
			increment &= 0x1FFFF;
			increment = op.Multiple == 0 ? increment >> 1 : increment * op.Multiple;
			op.Increment = increment;
		}
	}

	/// <summary>Calculates one sample at the native rate of the chip and puts it in the history of the resampler.</summary>
	private void GenerateNativeSample()
	{
		int amValue = 0;
		if (_lfoEnabled)
		{
			if (++_lfoTimer >= LfoPeriods[_lfoFrequency])
			{
				_lfoTimer = 0;
				_lfoCounter = (_lfoCounter + 1) & 127;
			}

			// The LFO amplitude value is a triangle from 0 to 126.
			amValue = (_lfoCounter < 64 ? _lfoCounter : 127 - _lfoCounter) << 1;
		}

		int mixLeft = 0;
		int mixRight = 0;
		for (int c = 0; c < 6; c++)
		{
			Channel channel = _channels[c];
			int output;
			if (c == 5 && _dacEnabled)
			{
				output = (_dacValue - 128) << 6;
			}
			else if (channel.IsSilent())
			{
				channel.Feedback0 = 0;
				channel.Feedback1 = 0;
				channel.Memory = 0;
				continue;
			}
			else
			{
				if (PhaseModulationActive(channel))
				{
					UpdateIncrements(c);
				}

				output = RenderChannel(channel, amValue >> AmShifts[channel.Ams]);
			}

			if (channel.Left)
			{
				mixLeft += output;
			}

			if (channel.Right)
			{
				mixRight += output;
			}
		}

		_newest++;
		int index = _newest & HistoryMask;
		_historyLeft[index] = mixLeft * OutputScale;
		_historyRight[index] = mixRight * OutputScale;

		// The envelope generator runs once every three samples.
		if (++_envelopeDivider >= 3)
		{
			_envelopeDivider = 0;
			ClockEnvelopes();
		}
	}

	/// <summary>Calculates the output of a channel for one sample and moves the phases of its operators.</summary>
	private static int RenderChannel(Channel channel, int am)
	{
		Operator op1 = channel.Operators[0];
		Operator op2 = channel.Operators[1];
		Operator op3 = channel.Operators[2];
		Operator op4 = channel.Operators[3];

		int feedback = channel.Feedback != 0 ? (channel.Feedback0 + channel.Feedback1) >> (10 - channel.Feedback) : 0;
		int out1 = RenderOperator(op1, feedback, am);
		channel.Feedback1 = channel.Feedback0;
		channel.Feedback0 = out1;

		int out2;
		int out3;
		int out4;
		int sum;

		// Some connections use the output of the previous sample because of the order of the hardware pipeline.
		switch (channel.Algorithm)
		{
			case 0:
				out2 = RenderOperator(op2, out1 >> 1, am);
				out3 = RenderOperator(op3, channel.Memory >> 1, am);
				channel.Memory = out2;
				sum = RenderOperator(op4, out3 >> 1, am);
				break;
			case 1:
				out2 = RenderOperator(op2, 0, am);
				out3 = RenderOperator(op3, channel.Memory >> 1, am);
				channel.Memory = out1 + out2;
				sum = RenderOperator(op4, out3 >> 1, am);
				break;
			case 2:
				out2 = RenderOperator(op2, 0, am);
				out3 = RenderOperator(op3, channel.Memory >> 1, am);
				channel.Memory = out2;
				sum = RenderOperator(op4, (out1 + out3) >> 1, am);
				break;
			case 3:
				out2 = RenderOperator(op2, out1 >> 1, am);
				out3 = RenderOperator(op3, 0, am);
				sum = RenderOperator(op4, (channel.Memory + out3) >> 1, am);
				channel.Memory = out2;
				break;
			case 4:
				out2 = RenderOperator(op2, out1 >> 1, am);
				out3 = RenderOperator(op3, 0, am);
				out4 = RenderOperator(op4, out3 >> 1, am);
				sum = out2 + out4;
				break;
			case 5:
				out2 = RenderOperator(op2, out1 >> 1, am);
				out3 = RenderOperator(op3, channel.Memory >> 1, am);
				channel.Memory = out1;
				out4 = RenderOperator(op4, out1 >> 1, am);
				sum = out2 + out3 + out4;
				break;
			case 6:
				out2 = RenderOperator(op2, out1 >> 1, am);
				out3 = RenderOperator(op3, 0, am);
				out4 = RenderOperator(op4, 0, am);
				sum = out2 + out3 + out4;
				break;
			default:
				out2 = RenderOperator(op2, 0, am);
				out3 = RenderOperator(op3, 0, am);
				out4 = RenderOperator(op4, 0, am);
				sum = out1 + out2 + out3 + out4;
				break;
		}

		if (sum > 8191)
		{
			sum = 8191;
		}
		else if (sum < -8192)
		{
			sum = -8192;
		}

		return sum;
	}

	/// <summary>
	/// Calculates the 14-bit signed output of an operator and moves its phase. The modulation is in units of the
	/// 10-bit phase.
	/// </summary>
	private static int RenderOperator(Operator op, int modulation, int am)
	{
		int phase = ((op.Phase >> 10) + modulation) & 1023;
		op.Phase = (op.Phase + op.Increment) & 0xFFFFF;

		int attenuation = op.Attenuation + (op.TotalLevel << 3);
		if (op.AmEnabled)
		{
			attenuation += am;
		}

		if (attenuation > MaxAttenuation)
		{
			attenuation = MaxAttenuation;
		}

		int index = phase & 255;
		if ((phase & 256) != 0)
		{
			index ^= 255;
		}

		int log = LogSine[index] + (attenuation << 2);
		int shift = log >> 8;
		if (shift >= 13)
		{
			return 0;
		}

		int value = (Exponent[log & 255] << 2) >> shift;
		return (phase & 512) != 0 ? -value : value;
	}

	/// <summary>Moves the envelope generator one step for all operators.</summary>
	private void ClockEnvelopes()
	{
		_envelopeCounter = (_envelopeCounter + 1) & 0xFFF;
		int counter = _envelopeCounter;

		for (int c = 0; c < 6; c++)
		{
			Operator[] operators = _channels[c].Operators;
			for (int i = 0; i < 4; i++)
			{
				Operator op = operators[i];
				int rateRegister;
				switch (op.State)
				{
					case StateAttack:
						rateRegister = op.AttackRate;
						break;
					case StateDecay:
						rateRegister = op.DecayRate;
						break;
					case StateSustain:
						rateRegister = op.SustainRate;
						break;
					default:
						if (op.Attenuation >= MaxAttenuation)
						{
							continue;
						}

						rateRegister = op.ReleaseRate * 2 + 1;
						break;
				}

				int rate = CalculateRate(rateRegister, op);
				if (rate == 0)
				{
					continue;
				}

				int shift = rate < 48 ? 11 - (rate >> 2) : 0;
				if ((counter & ((1 << shift) - 1)) != 0)
				{
					continue;
				}

				int increment = EnvelopeIncrement[(rate << 3) + ((counter >> shift) & 7)];
				if (op.State == StateAttack)
				{
					if (rate >= 62)
					{
						op.Attenuation = 0;
					}
					else
					{
						// The attack curve is exponential: the step is a part of the remaining attenuation.
						op.Attenuation += (~op.Attenuation * increment) >> 4;
					}

					if (op.Attenuation <= 0)
					{
						op.Attenuation = 0;
						EnterDecay(op);
					}
				}
				else
				{
					op.Attenuation += increment;
					if (op.Attenuation > MaxAttenuation)
					{
						op.Attenuation = MaxAttenuation;
					}

					if (op.State == StateDecay && op.Attenuation >= SustainAttenuation(op))
					{
						op.State = StateSustain;
					}
				}
			}
		}
	}

	private static int[] BuildEnvelopeIncrements()
	{
		int[][] patterns =
		{
			new[] { 0, 1, 0, 1, 0, 1, 0, 1 },
			new[] { 0, 1, 0, 1, 1, 1, 0, 1 },
			new[] { 0, 1, 1, 1, 0, 1, 1, 1 },
			new[] { 0, 1, 1, 1, 1, 1, 1, 1 },
		};
		int[][] fastPatterns =
		{
			new[] { 1, 1, 1, 1, 1, 1, 1, 1 },
			new[] { 1, 1, 1, 2, 1, 1, 1, 2 },
			new[] { 1, 2, 1, 2, 1, 2, 1, 2 },
			new[] { 1, 2, 2, 2, 1, 2, 2, 2 },
		};

		int[] table = new int[64 * 8];
		for (int rate = 2; rate < 64; rate++)
		{
			for (int step = 0; step < 8; step++)
			{
				int value;
				if (rate < 48)
				{
					value = patterns[rate & 3][step];
				}
				else if (rate < 60)
				{
					// Rates 48-51 use the fast patterns, and each group of 4 rates after that doubles them.
					value = fastPatterns[rate & 3][step] << ((rate - 48) >> 2);
				}
				else
				{
					value = 8;
				}

				table[(rate << 3) + step] = value;
			}
		}

		return table;
	}

	private static int[] BuildPmTable()
	{
		int[] table = new int[8 * 8 * 128];
		for (int fms = 0; fms < 8; fms++)
		{
			double depth = Math.Pow(2.0, PmCents[fms] / 1200.0) - 1.0;
			for (int level = 0; level < 8; level++)
			{
				for (int top = 0; top < 128; top++)
				{
					// The offset is in half steps of the frequency number, so multiply by 2.
					double offset = (top << 4) * 2.0 * depth * level / 7.0;
					table[((fms << 3) + level) * 128 + top] = (int)offset;
				}
			}
		}

		return table;
	}

	/// <summary>
	/// Builds the table of coefficients of the resampling filter: a windowed sinc (Kaiser window) with one row for
	/// each fractional position, plus one extra row for the interpolation.
	/// </summary>
	/// <param name="ratio">The ratio of the output rate to the native rate, at most 1.</param>
	private static float[] BuildFilter(double ratio)
	{
		const double Beta = 7.0;
		double cutoff = 0.5 * ratio * 0.92;
		double windowScale = 1.0 / BesselI0(Beta);
		float[] table = new float[(FilterPhases + 1) * Taps];
		double[] row = new double[Taps];

		for (int phase = 0; phase <= FilterPhases; phase++)
		{
			double fraction = (double)phase / FilterPhases;
			double sum = 0.0;
			for (int k = 0; k < Taps; k++)
			{
				double distance = fraction - HalfTaps + k;
				double x = distance / HalfTaps;
				double window = Math.Abs(x) >= 1.0 ? 0.0 : BesselI0(Beta * Math.Sqrt(1.0 - x * x)) * windowScale;
				double argument = 2.0 * cutoff * distance;
				double sinc = Math.Abs(argument) < 1e-12 ? 1.0 : Math.Sin(Math.PI * argument) / (Math.PI * argument);
				row[k] = 2.0 * cutoff * sinc * window;
				sum += row[k];
			}

			// Make the gain at 0 Hz exactly 1 for each row.
			for (int k = 0; k < Taps; k++)
			{
				table[phase * Taps + k] = (float)(row[k] / sum);
			}
		}

		return table;
	}

	private static double BesselI0(double x)
	{
		double sum = 1.0;
		double term = 1.0;
		double half = x / 2.0;
		for (int k = 1; k < 50; k++)
		{
			term *= half / k;
			double squared = term * term;
			sum += squared;
			if (squared < sum * 1e-16)
			{
				break;
			}
		}

		return sum;
	}

	/// <summary>The registers and the state of one of the four operators of a channel.</summary>
	private sealed class Operator
	{
		public int Detune;

		public int Multiple;

		public int TotalLevel = 0x7F;

		public int KeyScale;

		public int AttackRate;

		public int DecayRate;

		public int SustainRate;

		public int SustainLevel;

		public int ReleaseRate;

		public bool AmEnabled;

		/// <summary>The frequency number for the special mode of channel 3.</summary>
		public int FrequencyNumber;

		/// <summary>The block for the special mode of channel 3.</summary>
		public int Block;

		public int KeyCode;

		/// <summary>The 20-bit phase counter. The top 10 bits give the position in the sine wave.</summary>
		public int Phase;

		public int Increment;

		/// <summary>The 10-bit attenuation of the envelope. Each unit is 0.09375 dB.</summary>
		public int Attenuation = MaxAttenuation;

		public int State = StateRelease;

		public bool KeyIsOn;
	}

	/// <summary>The registers and the state of one FM channel.</summary>
	private sealed class Channel
	{
		/// <summary>The operators in the order S1, S2, S3, S4.</summary>
		public readonly Operator[] Operators = { new Operator(), new Operator(), new Operator(), new Operator() };

		public int FrequencyNumber;

		public int Block;

		public int Algorithm;

		public int Feedback;

		public int Ams;

		public int Fms;

		public bool Left = true;

		public bool Right = true;

		/// <summary>The output of operator 1 in the previous sample.</summary>
		public int Feedback0;

		/// <summary>The output of operator 1 two samples before.</summary>
		public int Feedback1;

		/// <summary>The modulation that the algorithm keeps for one sample.</summary>
		public int Memory;

		/// <summary>Returns true when all four operators are released and fully attenuated.</summary>
		public bool IsSilent()
		{
			for (int i = 0; i < 4; i++)
			{
				Operator op = Operators[i];
				if (op.State != StateRelease || op.Attenuation < MaxAttenuation)
				{
					return false;
				}
			}

			return true;
		}
	}
}
