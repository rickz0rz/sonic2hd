using System;

namespace SonicOrca.Original.Music;

/// <summary>
/// An emulator of the SN76489 PSG of the Mega Drive (the Sega variant in the VDP). It has three square wave tone
/// channels and one noise channel. The chip runs at the clock divided by 16. Each output sample is the average of the
/// chip output over the time of that sample, and a high-pass filter removes the DC offset as the output capacitor of
/// the console does.
/// </summary>
public sealed class Sn76489
{
	/// <summary>The PSG clock of an NTSC Mega Drive in Hz.</summary>
	public const double NtscClock = 3579545.0;

	/// <summary>
	/// The output of one channel at full volume before the high-pass filter. The output of a channel goes from 0 to
	/// this value, so a square wave has a peak of about half this value after the filter.
	/// </summary>
	private const float FullVolume = 0.18f;

	/// <summary>The cutoff frequency of the high-pass filter in Hz.</summary>
	private const double HighPassCutoff = 15.0;

	/// <summary>The output level for each 4-bit volume value. Each step is 2 dB and 15 is off.</summary>
	private static readonly float[] Volumes = BuildVolumes();

	/// <summary>The 10-bit periods of the three tone channels.</summary>
	private readonly int[] _periods = new int[3];

	/// <summary>The counters of the three tone channels.</summary>
	private readonly int[] _counters = new int[3];

	/// <summary>The output bits (0 or 1) of the three tone channels.</summary>
	private readonly int[] _outputs = { 1, 1, 1 };

	/// <summary>The 4-bit volumes of the three tone channels and the noise channel.</summary>
	private readonly int[] _volumes = { 15, 15, 15, 15 };

	/// <summary>The number of chip ticks in one output sample.</summary>
	private readonly double _ticksPerSample;

	private readonly float _highPassFactor;

	private int _noiseRegister;

	private int _noiseCounter;

	private int _noiseToggle;

	private int _shiftRegister = 0x8000;

	private int _latchedChannel;

	private bool _latchedVolume;

	/// <summary>The part of the current tick that the previous output samples used.</summary>
	private double _tickPosition;

	/// <summary>The output level of the chip after the most recent tick.</summary>
	private float _level;

	private float _previousInput;

	private float _previousOutput;

	/// <summary>
	/// Creates a chip that renders at the given sample rate.
	/// </summary>
	/// <param name="sampleRate">The sample rate of the output in Hz.</param>
	/// <param name="clock">The clock of the chip in Hz.</param>
	public Sn76489(int sampleRate, double clock = NtscClock)
	{
		if (sampleRate <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(sampleRate));
		}

		_ticksPerSample = clock / 16.0 / sampleRate;
		_highPassFactor = (float)Math.Exp(-2.0 * Math.PI * HighPassCutoff / sampleRate);
		_level = CalculateLevel();
	}

	/// <summary>
	/// Writes a byte to the PSG port. A byte with bit 7 set is a latch byte: bits 5-6 give the channel, bit 4 selects
	/// the volume (1) or the tone (0), and bits 0-3 give the data. A byte with bit 7 clear sets the high 6 bits of the
	/// tone period of the latched channel, or the volume or the noise register when one of those is latched.
	/// </summary>
	/// <param name="value">The 8-bit value.</param>
	public void Write(int value)
	{
		value &= 0xFF;
		if ((value & 0x80) != 0)
		{
			_latchedChannel = (value >> 5) & 3;
			_latchedVolume = (value & 0x10) != 0;
			int data = value & 0x0F;
			if (_latchedVolume)
			{
				_volumes[_latchedChannel] = data;
			}
			else if (_latchedChannel == 3)
			{
				WriteNoise(data);
			}
			else
			{
				_periods[_latchedChannel] = (_periods[_latchedChannel] & 0x3F0) | data;
			}
		}
		else if (_latchedVolume)
		{
			_volumes[_latchedChannel] = value & 0x0F;
		}
		else if (_latchedChannel == 3)
		{
			WriteNoise(value & 0x0F);
		}
		else
		{
			_periods[_latchedChannel] = (_periods[_latchedChannel] & 0x00F) | ((value & 0x3F) << 4);
		}

		_level = CalculateLevel();
	}

	/// <summary>
	/// Renders the output of the chip and adds it (mono) to both buffers.
	/// </summary>
	/// <param name="left">The buffer of the left channel.</param>
	/// <param name="right">The buffer of the right channel. It must have the same length as the left buffer.</param>
	public void Render(Span<float> left, Span<float> right)
	{
		if (right.Length < left.Length)
		{
			throw new ArgumentException("The right buffer is shorter than the left buffer.", nameof(right));
		}

		double scale = 1.0 / _ticksPerSample;
		for (int i = 0; i < left.Length; i++)
		{
			// Integrate the output over the time of this sample. The parts of the ticks at the two ends count only
			// for the part that is in this sample.
			double needed = _ticksPerSample;
			double sum = 0.0;
			while (needed > 0.0)
			{
				double available = 1.0 - _tickPosition;
				if (available <= needed)
				{
					sum += available * _level;
					needed -= available;
					_tickPosition = 0.0;
					Tick();
				}
				else
				{
					sum += needed * _level;
					_tickPosition += needed;
					needed = 0.0;
				}
			}

			float input = (float)(sum * scale);

			// A one-pole high-pass filter removes the DC offset.
			float output = input - _previousInput + _highPassFactor * _previousOutput;
			_previousInput = input;
			_previousOutput = output;

			left[i] += output;
			right[i] += output;
		}
	}

	private void WriteNoise(int data)
	{
		_noiseRegister = data & 7;
		_shiftRegister = 0x8000;
	}

	/// <summary>Moves the chip one tick (the clock divided by 16) and calculates the new output level.</summary>
	private void Tick()
	{
		for (int i = 0; i < 3; i++)
		{
			if (--_counters[i] <= 0)
			{
				_counters[i] = _periods[i];
				_outputs[i] ^= 1;
			}
		}

		if (--_noiseCounter <= 0)
		{
			int rate = _noiseRegister & 3;
			_noiseCounter = rate == 3 ? _periods[2] : 0x10 << rate;
			_noiseToggle ^= 1;

			// The shift register moves when the toggle goes from 0 to 1.
			if (_noiseToggle == 1)
			{
				int feedback = (_noiseRegister & 4) != 0
					? (_shiftRegister ^ (_shiftRegister >> 3)) & 1
					: _shiftRegister & 1;
				_shiftRegister = (_shiftRegister >> 1) | (feedback << 15);
			}
		}

		_level = CalculateLevel();
	}

	private float CalculateLevel()
	{
		float level = 0.0f;
		for (int i = 0; i < 3; i++)
		{
			// A period of 0 or 1 gives a constant high output on the Sega PSG.
			if (_periods[i] <= 1 || _outputs[i] != 0)
			{
				level += Volumes[_volumes[i]];
			}
		}

		if ((_shiftRegister & 1) != 0)
		{
			level += Volumes[_volumes[3]];
		}

		return level;
	}

	private static float[] BuildVolumes()
	{
		float[] volumes = new float[16];
		for (int i = 0; i < 15; i++)
		{
			volumes[i] = (float)(FullVolume * Math.Pow(10.0, -2.0 * i / 20.0));
		}

		volumes[15] = 0.0f;
		return volumes;
	}
}
