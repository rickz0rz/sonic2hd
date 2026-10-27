using System;
using System.IO;

namespace SonicOrca.Original.Music;

/// <summary>
/// The music part of the Sonic 2 sound driver (s2.sounddriver.asm of the disassembly, without the fixes of
/// FixDriverBugs). It plays a <see cref="SmpsSong"/> one frame at a time and writes the registers of the
/// <see cref="Ym2612"/> and the <see cref="Sn76489"/>. The memory of each track has the layout of zTrack, so the code
/// follows the driver closely. The DAC drums go to <see cref="DacRequest"/>, because the Z80 plays them between the
/// frames.
/// </summary>
public sealed class SmpsDriver
{
	/// <summary>The clock of the Mega Drive (NTSC).</summary>
	public const double MasterClock = 53693175.0;

	/// <summary>The frames each second of the Mega Drive (NTSC): the master clock over 3420 x 262.</summary>
	public const double FrameRate = MasterClock / (3420.0 * 262.0);

	public const double Z80Clock = MasterClock / 15.0;

	private const double FmSampleRate = MasterClock / 7.0 / 144.0;

	private const double PsgSampleRate = Z80Clock / 16.0;

	private const int TrackCount = 10;

	private const int TrackLength = 0x2A;

	private const int DacTrack = 0;

	private const int FirstFmTrack = 1;

	private const int FirstPsgTrack = 7;

	// The offsets of zTrack
	private const int PlaybackControl = 0x00;
	private const int VoiceControl = 0x01;
	private const int TempoDivider = 0x02;
	private const int DataPointerLow = 0x03;
	private const int Transpose = 0x05;
	private const int Volume = 0x06;
	private const int AmsFmsPan = 0x07;
	private const int VoiceIndex = 0x08;
	private const int VolFlutter = 0x09;
	private const int StackPointer = 0x0A;
	private const int DurationTimeout = 0x0B;
	private const int SavedDuration = 0x0C;
	private const int FreqLow = 0x0D;
	private const int SavedDac = 0x0D;
	private const int FreqHigh = 0x0E;
	private const int NoteFillTimeout = 0x0F;
	private const int NoteFillMaster = 0x10;
	private const int ModulationPtrLow = 0x11;
	private const int ModulationWait = 0x13;
	private const int ModulationSpeed = 0x14;
	private const int ModulationDelta = 0x15;
	private const int ModulationSteps = 0x16;
	private const int ModulationValLow = 0x17;
	private const int Detune = 0x19;
	private const int VolTlMask = 0x1A;
	private const int PsgNoise = 0x1B;
	private const int TlPtrLow = 0x1E;
	private const int LoopCounters = 0x20;
	private const int GoSubStack = 0x2A;

	// The bits of PlaybackControl
	private const int AtRest = 0x02;
	private const int SfxOverride = 0x04;
	private const int ModulationOn = 0x08;
	private const int NoAttack = 0x10;
	private const int Playing = 0x80;

	private static readonly byte[] FmDacInitBytes = { 6, 0, 1, 2, 4, 5, 6 };

	private static readonly byte[] PsgInitBytes = { 0x80, 0xA0, 0xC0 };

	private static readonly byte[] VolTlMaskTable = { 8, 8, 8, 8, 0x0C, 0x0E, 0x0E, 0x0F };

	private static readonly ushort[] FmFrequencies = MakeFmFrequencies();

	private static readonly ushort[] PsgFrequencies = MakePsgFrequencies();

	private static readonly byte[][] PsgEnvelopes =
	{
		new byte[] { 0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 7, 0x80 },
		new byte[] { 0, 2, 4, 6, 8, 0x10, 0x80 },
		new byte[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 0x80 },
		new byte[] { 0, 0, 2, 3, 4, 4, 5, 5, 5, 6, 0x80 },
		new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 0x80 },
		new byte[] { 3, 3, 3, 2, 2, 2, 2, 1, 1, 1, 0, 0, 0, 0, 0x80 },
		new byte[] { 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 7, 0x80 },
		new byte[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 7, 7, 7, 0x80 },
		new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x80 },
		new byte[]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 0x80
		},
		new byte[] { 4, 4, 4, 3, 3, 3, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 0x80 },
		new byte[]
		{
			4, 4, 3, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2,
			2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
			3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
			5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 0x80
		},
		new byte[] { 0x0E, 0x0D, 0x0C, 0x0B, 0x0A, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0x80 }
	};

	/// <summary>The drums of the notes $81 to $91 of the DAC track (zDACMasterPlaylist): the sample and its speed.</summary>
	private static readonly (DacSample Sample, double Scale)[] DacPlaylist =
	{
		(DacSample.Kick, 1.0), (DacSample.Snare, 1.0), (DacSample.Clap, 1.0), (DacSample.Scratch, 1.0),
		(DacSample.Timpani, 1.0), (DacSample.Tom, 1.0), (DacSample.Bongo, 1.0), (DacSample.Timpani, 1.30),
		(DacSample.Timpani, 1.20), (DacSample.Timpani, 0.97), (DacSample.Timpani, 0.95), (DacSample.Tom, 1.70),
		(DacSample.Tom, 1.30), (DacSample.Tom, 1.10), (DacSample.Bongo, 2.00), (DacSample.Bongo, 1.75),
		(DacSample.Bongo, 1.30)
	};

	private readonly byte[] _data;
	private readonly Ym2612 _fm;
	private readonly Sn76489 _psg;
	private readonly byte[] _ram = new byte[TrackCount * TrackLength];
	private int _ix;
	private int _tempoTimeout;
	private int _currentTempo;
	private bool _dacUpdating;
	private int _voiceTablePointer;
	private bool _soundEffect;

	/// <summary>A drum that the DAC track started in the last frame, or null.</summary>
	public (DacSample Sample, double Scale)? DacRequest { get; private set; }

	/// <summary>True while at least one track plays.</summary>
	public bool IsPlaying
	{
		get
		{
			for (int i = 0; i < TrackCount; i++)
			{
				if ((_ram[i * TrackLength + PlaybackControl] & Playing) != 0)
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>Starts a song, as zBGMLoad does, or a sound effect, as zPlaySound does.</summary>
	public SmpsDriver(SmpsSong song, Ym2612 fm, Sn76489 psg)
	{
		_data = song.Data;
		_fm = fm;
		_psg = psg;
		ClearTrackPlaybackMem();
		if (song.IsSoundEffect)
		{
			LoadSoundEffect();
		}
		else
		{
			LoadSong();
		}
	}

	/// <summary>
	/// True when the next frame moves the music on: the tempo counter overflows (see TempoWait). A sound effect
	/// moves on each frame.
	/// </summary>
	public bool NextFrameTicks => _soundEffect || _currentTempo + _tempoTimeout > 0xFF;

	/// <summary>
	/// Copies the state of the driver: the memory of the tracks that play, and the tempo. The tempo counter is not
	/// part of the state. When the state before a tick is the same as before an earlier tick, the song repeats from
	/// that tick.
	/// </summary>
	public byte[] GetState()
	{
		byte[] state = new byte[_ram.Length + 2];
		for (int track = 0; track < TrackCount; track++)
		{
			// TempoWait changes the duration of a track that does not play, but the track has no effect on the sound.
			int start = track * TrackLength;
			if ((_ram[start + PlaybackControl] & Playing) != 0)
			{
				Array.Copy(_ram, start, state, start, TrackLength);
			}
		}
		state[_ram.Length] = (byte)_currentTempo;
		return state;
	}

	/// <summary>Plays one frame: zUpdateMusic and the start of a new DAC drum.</summary>
	public void UpdateFrame()
	{
		DacRequest = null;
		if (!_soundEffect)
		{
			TempoWait();
		}
		_dacUpdating = true;
		_ix = DacTrack * TrackLength;
		if ((Get(PlaybackControl) & Playing) != 0)
		{
			DacUpdateTrack();
		}
		_dacUpdating = false;
		for (int track = FirstFmTrack; track < FirstPsgTrack; track++)
		{
			_ix = track * TrackLength;
			if ((Get(PlaybackControl) & Playing) != 0)
			{
				FmUpdateTrack();
			}
		}
		for (int track = FirstPsgTrack; track < TrackCount; track++)
		{
			_ix = track * TrackLength;
			if ((Get(PlaybackControl) & Playing) != 0)
			{
				PsgUpdateTrack();
			}
		}
	}

	private static ushort[] MakeFmFrequencies()
	{
		// The base octave starts at B and ends at B-flat.
		double[] baseOctave = { 15.39, 16.35, 17.34, 18.36, 19.45, 20.64, 21.84, 23.13, 24.51, 25.98, 27.53, 29.15 };
		ushort[] table = new ushort[8 * 12];
		for (int octave = 0; octave < 8; octave++)
		{
			for (int note = 0; note < 12; note++)
			{
				int value = (int)Math.Floor(baseOctave[note] * 1024 * 1024 * 2 / FmSampleRate + 0.5);
				table[octave * 12 + note] = (ushort)(value + octave * 0x800);
			}
		}
		return table;
	}

	private static ushort[] MakePsgFrequencies()
	{
		double[] frequencies =
		{
			130.98, 138.78, 146.99, 155.79, 165.22, 174.78, 185.19, 196.24, 207.91, 220.63, 233.52, 247.47,
			261.96, 277.56, 293.59, 311.58, 329.97, 349.56, 370.39, 392.49, 415.83, 440.39, 468.03, 494.95,
			522.71, 556.51, 588.73, 621.44, 661.89, 699.12, 740.79, 782.24, 828.59, 880.79, 932.17, 989.91,
			1045.42, 1107.52, 1177.47, 1242.89, 1316.00, 1398.25, 1491.47, 1575.50, 1669.55, 1747.82, 1864.34, 1962.46,
			2071.49, 2193.34, 2330.42, 2485.78, 2601.40, 2796.51, 2943.69, 3107.23, 3290.01, 3495.64, 3608.40, 3857.25,
			4142.98, 4302.32, 4660.85, 4863.50, 5084.56, 5326.69, 5887.39, 6214.47, 6580.02, 223721.56
		};
		ushort[] table = new ushort[frequencies.Length];
		for (int i = 0; i < frequencies.Length; i++)
		{
			table[i] = (ushort)Math.Min(0x3FF, (int)Math.Floor(PsgSampleRate / (frequencies[i] * 2) + 0.5));
		}
		return table;
	}

	private int Get(int offset) => _ram[_ix + offset];

	private void Set(int offset, int value) => _ram[_ix + offset] = (byte)value;

	private void SetBit(int offset, int bit) => _ram[_ix + offset] |= (byte)bit;

	private void ClearBit(int offset, int bit) => _ram[_ix + offset] &= (byte)~bit;

	private bool HasBit(int offset, int bit) => (_ram[_ix + offset] & bit) != 0;

	private int GetWord(int offset) => _ram[_ix + offset] | (_ram[_ix + offset + 1] << 8);

	private void SetWord(int offset, int value)
	{
		_ram[_ix + offset] = (byte)value;
		_ram[_ix + offset + 1] = (byte)(value >> 8);
	}

	private int Read(int address) => address >= 0 && address < _data.Length ? _data[address] : 0;

	private static int SignExtend(int value) => (sbyte)(byte)value;

	private void WriteFmI(int register, int value) => _fm.Write(0, register, value & 0xFF);

	/// <summary>zWriteFMIorII: writes to part I or part II, from bit 2 of the voice control byte.</summary>
	private void WriteFmIorII(int register, int value)
	{
		_fm.Write(HasBit(VoiceControl, 4) ? 1 : 0, register & 0xFF, value & 0xFF);
	}

	private void WritePsg(int value) => _psg.Write(value & 0xFF);

	private void ClearTrackPlaybackMem()
	{
		WriteFmI(0x2B, 0x80);
		WriteFmI(0x27, 0x00);
		Array.Clear(_ram);
		FmSilenceAll();
		PsgSilenceAll();
	}

	private void FmSilenceAll()
	{
		for (int port = 0; port < 2; port++)
		{
			for (int register = 0x40; register < 0x50; register++)
			{
				if ((register & 3) != 3)
				{
					_fm.Write(port, register, 0x7F);
				}
			}
		}
		foreach (int channel in new[] { 0, 1, 2, 4, 5, 6 })
		{
			WriteFmI(0x28, channel);
		}
	}

	private void PsgSilenceAll()
	{
		WritePsg(0x9F);
		WritePsg(0xBF);
		WritePsg(0xDF);
		WritePsg(0xFF);
	}

	/// <summary>zBGMLoad and zInitBGMPSG for an uncompressed song in memory.</summary>
	private void LoadSong()
	{
		_voiceTablePointer = Read(0) | (Read(1) << 8);
		int fmCount = Read(2);
		int psgCount = Read(3);
		int tempoDivider = Read(4);
		_currentTempo = Read(5);
		_tempoTimeout = _currentTempo;
		int header = 6;
		for (int i = 0; i < fmCount && i < FirstPsgTrack; i++)
		{
			_ix = i * TrackLength;
			Set(PlaybackControl, Playing | AtRest);
			Set(VoiceControl, FmDacInitBytes[i]);
			Set(TempoDivider, tempoDivider);
			Set(StackPointer, GoSubStack);
			Set(AmsFmsPan, 0xC0);
			Set(DurationTimeout, 1);
			Set(DataPointerLow, Read(header));
			Set(DataPointerLow + 1, Read(header + 1));
			Set(Transpose, Read(header + 2));
			Set(Volume, Read(header + 3));
			header += 4;
		}
		if (fmCount == 7)
		{
			WriteFmI(0x2B, 0x00);
		}
		else
		{
			WriteFmI(0x28, 6);
			for (int register = 0x42; register < 0x52; register += 4)
			{
				_fm.Write(1, register, 0xFF);
			}
			_fm.Write(1, 0xB6, 0xC0);
			WriteFmI(0x2B, 0x80);
		}
		for (int i = 0; i < psgCount && i < 3; i++)
		{
			_ix = (FirstPsgTrack + i) * TrackLength;
			Set(PlaybackControl, Playing | AtRest);
			Set(VoiceControl, PsgInitBytes[i]);
			Set(TempoDivider, tempoDivider);
			Set(StackPointer, GoSubStack);
			Set(DurationTimeout, 1);
			Set(DataPointerLow, Read(header));
			Set(DataPointerLow + 1, Read(header + 1));
			Set(Transpose, Read(header + 2));
			Set(Volume, Read(header + 3));
			Set(VoiceIndex, Read(header + 5));
			header += 6;
		}
		for (int track = FirstFmTrack; track < FirstPsgTrack; track++)
		{
			_ix = track * TrackLength;
			FmNoteOff();
		}
		for (int track = FirstPsgTrack; track < TrackCount; track++)
		{
			_ix = track * TrackLength;
			PsgNoteOff();
		}
	}

	/// <summary>
	/// zPlaySound: each track of a sound effect names its channel. The driver puts the track into the memory of the
	/// music track of that channel. The real driver keeps the tracks of the sound effects in separate memory. It also
	/// stops the music on their channels. Here, no music plays. All the tracks use the voices of the sound effect.
	/// </summary>
	private void LoadSoundEffect()
	{
		_soundEffect = true;
		_voiceTablePointer = Read(0) | (Read(1) << 8);
		int tempoDivider = Read(2);
		int count = Read(3);
		int header = 4;
		for (int i = 0; i < count; i++)
		{
			int channel = Read(header + 1);
			bool psg = (channel & 0x80) != 0;
			int track = psg ? FirstPsgTrack + ((channel >> 5) & 3) : Array.IndexOf(FmDacInitBytes, (byte)channel, FirstFmTrack);
			if (track < 0 || track >= TrackCount)
			{
				throw new InvalidDataException($"A sound effect cannot use the channel ${channel:X2}.");
			}
			if (channel == 0xC0)
			{
				// PSG3: silence the tone channel and the noise channel
				WritePsg(0xDF);
				WritePsg(0xFF);
			}
			_ix = track * TrackLength;
			Set(PlaybackControl, Read(header));
			Set(VoiceControl, channel);
			Set(TempoDivider, tempoDivider);
			Set(DurationTimeout, 1);
			Set(StackPointer, GoSubStack);
			Set(DataPointerLow, Read(header + 2));
			Set(DataPointerLow + 1, Read(header + 3));
			Set(Transpose, Read(header + 4));
			Set(Volume, Read(header + 5));
			if (!psg)
			{
				Set(AmsFmsPan, 0xC0);
			}
			header += 6;
		}
	}

	/// <summary>
	/// TempoWait: when the tempo counter does not overflow, all the durations wait one more frame. A tempo of $80
	/// plays the song at half speed.
	/// </summary>
	private void TempoWait()
	{
		int sum = _currentTempo + _tempoTimeout;
		_tempoTimeout = sum & 0xFF;
		if (sum > 0xFF)
		{
			return;
		}
		for (int track = 0; track < TrackCount; track++)
		{
			_ram[track * TrackLength + DurationTimeout]++;
		}
	}

	private void DacUpdateTrack()
	{
		Set(DurationTimeout, Get(DurationTimeout) - 1);
		if (Get(DurationTimeout) != 0)
		{
			return;
		}
		int hl = GetWord(DataPointerLow);
		int a;
		while (true)
		{
			a = Read(hl++);
			if (a < 0xE0)
			{
				break;
			}
			if (!CoordFlag(a, ref hl))
			{
				return;
			}
		}
		if (a < 0x80)
		{
			SetDuration(a);
		}
		else
		{
			Set(SavedDac, a);
			int next = Read(hl);
			if (next < 0x80)
			{
				hl++;
				SetDuration(next);
			}
			else
			{
				Set(DurationTimeout, Get(SavedDuration));
			}
		}
		SetWord(DataPointerLow, hl);
		if (HasBit(PlaybackControl, SfxOverride))
		{
			return;
		}
		int note = Get(SavedDac);
		if (note == 0x80)
		{
			return;
		}
		int index = note - 0x81;
		if (index >= 0 && index < DacPlaylist.Length)
		{
			DacRequest = DacPlaylist[index];
		}
	}

	private void FmUpdateTrack()
	{
		Set(DurationTimeout, Get(DurationTimeout) - 1);
		if (Get(DurationTimeout) == 0)
		{
			ClearBit(PlaybackControl, NoAttack);
			if (!FmDoNext())
			{
				return;
			}
			FmPrepareNote();
			FmNoteOn();
		}
		else if (!NoteFillUpdate())
		{
			return;
		}
		if (DoModulation(out int frequency))
		{
			FmUpdateFreq(frequency);
		}
	}

	/// <summary>zFMDoNext. Returns false when the track stopped.</summary>
	private bool FmDoNext()
	{
		int hl = GetWord(DataPointerLow);
		ClearBit(PlaybackControl, AtRest);
		int a;
		while (true)
		{
			a = Read(hl++);
			if (a < 0xE0)
			{
				break;
			}
			if (!CoordFlag(a, ref hl))
			{
				return false;
			}
		}
		FmNoteOff();
		if (a < 0x80)
		{
			SetDuration(a);
		}
		else
		{
			FmSetFreq(a);
			int next = Read(hl);
			if (next < 0x80)
			{
				hl++;
				SetDuration(next);
			}
		}
		FinishTrackUpdate(hl);
		return true;
	}

	private void FmSetFreq(int note)
	{
		int a = (note - 0x80) & 0xFF;
		if (a == 0)
		{
			SetBit(PlaybackControl, AtRest);
			Set(FreqLow, 0);
			Set(FreqHigh, 0);
			return;
		}
		int index = ((a + Get(Transpose)) & 0xFF) & 0x7F;
		int frequency = FmFrequencies[Math.Min(index, FmFrequencies.Length - 1)];
		SetWord(FreqLow, frequency);
	}

	/// <summary>zSetDuration: the duration times the tempo divider, in 8 bits.</summary>
	private void SetDuration(int duration)
	{
		int divider = Get(TempoDivider);
		int value = duration * (divider == 0 ? 256 : divider);
		Set(SavedDuration, value);
		Set(DurationTimeout, value);
	}

	private void FinishTrackUpdate(int hl)
	{
		SetWord(DataPointerLow, hl);
		Set(DurationTimeout, Get(SavedDuration));
		if (HasBit(PlaybackControl, NoAttack))
		{
			return;
		}
		Set(NoteFillTimeout, Get(NoteFillMaster));
		Set(VolFlutter, 0);
		if (HasBit(PlaybackControl, ModulationOn))
		{
			SetModulation(GetWord(ModulationPtrLow));
		}
	}

	/// <summary>zNoteFillUpdate. Returns false when the note fill ends the note.</summary>
	private bool NoteFillUpdate()
	{
		if (Get(NoteFillTimeout) == 0)
		{
			return true;
		}
		Set(NoteFillTimeout, Get(NoteFillTimeout) - 1);
		if (Get(NoteFillTimeout) != 0)
		{
			return true;
		}
		SetBit(PlaybackControl, AtRest);
		if (HasBit(VoiceControl, 0x80))
		{
			PsgNoteOff();
		}
		else
		{
			FmNoteOff();
		}
		return false;
	}

	/// <summary>zDoModulation. Returns true with the new frequency when the modulation changes it.</summary>
	private bool DoModulation(out int frequency)
	{
		frequency = 0;
		if (HasBit(PlaybackControl, AtRest) || !HasBit(PlaybackControl, ModulationOn))
		{
			return false;
		}
		if (Get(ModulationWait) != 0)
		{
			Set(ModulationWait, Get(ModulationWait) - 1);
			return false;
		}
		Set(ModulationSpeed, Get(ModulationSpeed) - 1);
		if (Get(ModulationSpeed) != 0)
		{
			return false;
		}
		int hl = GetWord(ModulationPtrLow);
		Set(ModulationSpeed, Read(hl + 1));
		if (Get(ModulationSteps) == 0)
		{
			Set(ModulationSteps, Read(hl + 3));
			Set(ModulationDelta, -Get(ModulationDelta));
			return false;
		}
		Set(ModulationSteps, Get(ModulationSteps) - 1);
		int value = (GetWord(ModulationValLow) + SignExtend(Get(ModulationDelta))) & 0xFFFF;
		SetWord(ModulationValLow, value);
		frequency = (value + GetWord(FreqLow)) & 0xFFFF;
		return true;
	}

	private void SetModulation(int hl)
	{
		Set(ModulationWait, Read(hl));
		Set(ModulationSpeed, Read(hl + 1));
		Set(ModulationDelta, Read(hl + 2));
		Set(ModulationSteps, Read(hl + 3) >> 1);
		if (HasBit(PlaybackControl, NoAttack))
		{
			return;
		}
		SetWord(ModulationValLow, 0);
	}

	private void FmPrepareNote()
	{
		if (HasBit(PlaybackControl, AtRest))
		{
			return;
		}
		int frequency = GetWord(FreqLow);
		if (frequency == 0)
		{
			SetBit(PlaybackControl, AtRest);
			return;
		}
		FmUpdateFreq(frequency);
	}

	private void FmUpdateFreq(int frequency)
	{
		if (HasBit(PlaybackControl, SfxOverride))
		{
			return;
		}
		int hl = (SignExtend(Get(Detune)) + frequency) & 0xFFFF;
		int channel = Get(VoiceControl) & 3;
		WriteFmIorII(0xA4 + channel, hl >> 8);
		WriteFmIorII(0xA0 + channel, hl);
	}

	private void FmNoteOn()
	{
		if ((Get(PlaybackControl) & (AtRest | SfxOverride)) != 0)
		{
			return;
		}
		WriteFmI(0x28, Get(VoiceControl) | 0xF0);
	}

	private void FmNoteOff()
	{
		if ((Get(PlaybackControl) & (NoAttack | SfxOverride)) != 0)
		{
			return;
		}
		WriteFmI(0x28, Get(VoiceControl));
	}

	private void PsgUpdateTrack()
	{
		Set(DurationTimeout, Get(DurationTimeout) - 1);
		if (Get(DurationTimeout) == 0)
		{
			ClearBit(PlaybackControl, NoAttack);
			if (!PsgDoNext())
			{
				return;
			}
			PsgDoNoteOn();
			PsgDoVolFx();
		}
		else
		{
			if (!NoteFillUpdate())
			{
				return;
			}
			if (Get(VoiceIndex) != 0)
			{
				PsgDoVolFx();
			}
		}
		if (DoModulation(out int frequency))
		{
			PsgUpdateFreq(frequency);
		}
	}

	private bool PsgDoNext()
	{
		int hl = GetWord(DataPointerLow);
		ClearBit(PlaybackControl, AtRest);
		int a;
		while (true)
		{
			a = Read(hl++);
			if (a < 0xE0)
			{
				break;
			}
			if (!CoordFlag(a, ref hl))
			{
				return false;
			}
		}
		if (a < 0x80)
		{
			SetDuration(a);
		}
		else
		{
			PsgSetFreq(a);
			int next = Read(hl);
			if (next < 0x80)
			{
				hl++;
				SetDuration(next);
			}
		}
		FinishTrackUpdate(hl);
		return true;
	}

	private void PsgSetFreq(int note)
	{
		if (note == 0x80)
		{
			SetBit(PlaybackControl, AtRest);
			Set(FreqLow, 0xFF);
			Set(FreqHigh, 0xFF);
			PsgNoteOff();
			return;
		}
		int index = ((note - 0x81 + Get(Transpose)) & 0xFF) & 0x7F;
		SetWord(FreqLow, PsgFrequencies[Math.Min(index, PsgFrequencies.Length - 1)]);
	}

	private void PsgDoNoteOn()
	{
		if (HasBit(FreqHigh, 0x80))
		{
			SetBit(PlaybackControl, AtRest);
			return;
		}
		PsgUpdateFreq(GetWord(FreqLow));
	}

	private void PsgUpdateFreq(int frequency)
	{
		if ((Get(PlaybackControl) & (AtRest | SfxOverride)) != 0)
		{
			return;
		}
		int hl = (SignExtend(Get(Detune)) + frequency) & 0xFFFF;
		int channel = Get(VoiceControl);
		if (channel == 0xE0)
		{
			channel = 0xC0;
		}
		WritePsg((hl & 0x0F) | channel);
		WritePsg((hl >> 4) & 0x3F);
	}

	private void PsgDoVolFx()
	{
		int volume = Get(Volume);
		int tone = Get(VoiceIndex);
		if (tone != 0)
		{
			byte[] envelope = PsgEnvelopes[Math.Min(tone, PsgEnvelopes.Length) - 1];
			int step = Get(VolFlutter);
			int value = step < envelope.Length ? envelope[step] : 0x80;
			Set(VolFlutter, step + 1);
			if (value == 0x80)
			{
				// zVolEnvHold: the envelope stays at its last value and the volume does not change.
				Set(VolFlutter, Get(VolFlutter) - 1);
				return;
			}
			volume = (volume + value) & 0xFF;
		}
		PsgUpdateVol(volume);
	}

	private void PsgUpdateVol(int volume)
	{
		if ((Get(PlaybackControl) & (AtRest | SfxOverride)) != 0)
		{
			return;
		}
		if (HasBit(PlaybackControl, NoAttack) && Get(NoteFillMaster) != 0 && Get(NoteFillTimeout) == 0)
		{
			return;
		}
		WritePsg(Math.Min(volume, 0x0F) | Get(VoiceControl) | 0x10);
	}

	private void PsgNoteOff()
	{
		if (HasBit(PlaybackControl, SfxOverride))
		{
			return;
		}
		WritePsg(Get(VoiceControl) | 0x1F);
	}

	/// <summary>
	/// zCoordFlag: runs a coordination flag ($E0 to $F9). hl points after the first parameter, as in the driver.
	/// Returns false when the track stopped.
	/// </summary>
	private bool CoordFlag(int flag, ref int hl)
	{
		int a = Read(hl++);
		switch (flag)
		{
			case 0xE0:
				if (HasBit(VoiceControl, 0x80) || HasBit(PlaybackControl, SfxOverride))
				{
					break;
				}
				Set(AmsFmsPan, (Get(AmsFmsPan) & 0x37) | a);
				WriteFmIorII(0xB4 + (Get(VoiceControl) & 3), Get(AmsFmsPan));
				break;
			case 0xE1:
				Set(Detune, a);
				break;
			case 0xE2:
				break;
			case 0xE3:
			{
				int stack = Get(StackPointer);
				hl = _ram[Math.Min(_ix + stack, _ram.Length - 2)] | (_ram[Math.Min(_ix + stack + 1, _ram.Length - 1)] << 8);
				Set(StackPointer, stack + 2);
				break;
			}
			case 0xE4:
				// The fade to the previous song is only for the extra life jingle.
				return StopTrack();
			case 0xE5:
				Set(TempoDivider, a);
				break;
			case 0xE6:
				Set(Volume, Get(Volume) + a);
				SetChanVol();
				break;
			case 0xE7:
				SetBit(PlaybackControl, NoAttack);
				hl--;
				break;
			case 0xE8:
				Set(NoteFillTimeout, a);
				Set(NoteFillMaster, a);
				break;
			case 0xE9:
				Set(Transpose, Get(Transpose) + a);
				break;
			case 0xEA:
				_currentTempo = a;
				break;
			case 0xEB:
				for (int track = 0; track < TrackCount; track++)
				{
					_ram[track * TrackLength + TempoDivider] = (byte)a;
				}
				break;
			case 0xEC:
				Set(Volume, Get(Volume) + a);
				break;
			case 0xED:
				// The parameter is not put back, so the driver skips a byte.
				break;
			case 0xEE:
				hl--;
				break;
			case 0xEF:
				Set(VoiceIndex, a);
				if (!HasBit(PlaybackControl, SfxOverride))
				{
					SetVoice(_voiceTablePointer, a);
				}
				break;
			case 0xF0:
				SetBit(PlaybackControl, ModulationOn);
				hl--;
				SetWord(ModulationPtrLow, hl);
				SetModulation(hl);
				hl += 4;
				break;
			case 0xF1:
				hl--;
				SetBit(PlaybackControl, ModulationOn);
				break;
			case 0xF2:
				hl--;
				return StopTrack();
			case 0xF3:
				Set(VoiceControl, 0xE0);
				Set(PsgNoise, a);
				if (!HasBit(PlaybackControl, SfxOverride))
				{
					WritePsg(a);
				}
				break;
			case 0xF4:
				hl--;
				ClearBit(PlaybackControl, ModulationOn);
				break;
			case 0xF5:
				Set(VoiceIndex, a);
				break;
			case 0xF6:
				hl = a | (Read(hl) << 8);
				break;
			case 0xF7:
			{
				int count = Read(hl++);
				int counter = _ix + LoopCounters + a;
				if (counter < _ram.Length)
				{
					if (_ram[counter] == 0)
					{
						_ram[counter] = (byte)count;
					}
					_ram[counter]--;
					if (_ram[counter] != 0)
					{
						hl = Read(hl) | (Read(hl + 1) << 8);
						break;
					}
				}
				hl += 2;
				break;
			}
			case 0xF8:
			{
				int stack = (Get(StackPointer) - 2) & 0xFF;
				Set(StackPointer, stack);
				int high = Read(hl++);
				int address = _ix + stack;
				if (address + 1 < _ram.Length)
				{
					_ram[address] = (byte)hl;
					_ram[address + 1] = (byte)(hl >> 8);
				}
				hl = a | (high << 8);
				break;
			}
			case 0xF9:
				WriteFmI(0x88, 0x0F);
				WriteFmI(0x8C, 0x0F);
				hl--;
				break;
			default:
				// $FA to $FF are not in the Sonic 2 driver; they jump into other code.
				return StopTrack();
		}
		return true;
	}

	/// <summary>cfStopTrack. Always returns false: the update of the track ends.</summary>
	private bool StopTrack()
	{
		ClearBit(PlaybackControl, Playing);
		ClearBit(PlaybackControl, NoAttack);
		if (HasBit(VoiceControl, 0x80))
		{
			PsgNoteOff();
		}
		else if (!_dacUpdating)
		{
			FmNoteOff();
		}
		return false;
	}

	/// <summary>zSetVoice: writes the 25 bytes of a voice to the channel of the track, and applies the volume.</summary>
	private void SetVoice(int table, int voice)
	{
		int hl = (table + voice * 25) & 0xFFFF;
		int feedbackAlgorithm = Read(hl++);
		int channel = Get(VoiceControl) & 3;
		WriteFmIorII(0xB0 + channel, feedbackAlgorithm);
		for (int i = 0; i < 4; i++)
		{
			WriteFmIorII(0x30 + channel + i * 4, Read(hl++));
		}
		for (int i = 0; i < 16; i++)
		{
			WriteFmIorII(0x50 + channel + i * 4, Read(hl++));
		}
		WriteFmIorII(0xB4 + channel, Get(AmsFmsPan));
		SetWord(TlPtrLow, hl);
		int mask = VolTlMaskTable[feedbackAlgorithm & 7];
		Set(VolTlMask, mask);
		SetFmTls(0x40 + channel, hl, mask, Get(Volume));
	}

	private void SetFmTls(int register, int hl, int mask, int volume)
	{
		for (int i = 0; i < 4; i++)
		{
			int level = Read(hl++);
			if ((mask & 1) != 0)
			{
				level = (volume + level) & 0xFF;
			}
			mask >>= 1;
			WriteFmIorII(register, level);
			register += 4;
		}
	}

	private void SetChanVol()
	{
		if (HasBit(VoiceControl, 0x80) || HasBit(PlaybackControl, SfxOverride))
		{
			return;
		}
		int volume = Get(Volume);
		if ((volume & 0x80) != 0)
		{
			return;
		}
		SetFmTls(0x40 + (Get(VoiceControl) & 3), GetWord(TlPtrLow), Get(VolTlMask), volume);
	}

	/// <summary>The playback rate of a DAC drum, from the loop counter of zWriteToDAC (dpcmLoopCounter).</summary>
	public static double GetDacRate(int sampleRate, double scale)
	{
		int rate = (int)(sampleRate * scale);
		// pcmLoopCounterBase in integer arithmetic: 295/2 cycles for each sample, plus 13 cycles for each loop.
		int counter = 1 + ((int)(Z80Clock / rate) - 147 + 6) / 13;
		return Z80Clock / (147 + 13 * (counter - 1));
	}
}

/// <summary>The DAC drum samples of Sonic 2 (sound/DAC/*.wav of the disassembly).</summary>
public enum DacSample
{
	Kick,
	Snare,
	Clap,
	Scratch,
	Timpani,
	Tom,
	Bongo
}
