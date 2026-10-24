using System;
using System.Runtime.CompilerServices;
using SonicOrca.Core;

namespace SONICORCA.OBJECTS.CNZSLOTCAGE;

/// <summary>
/// The slot machine of Casino Night Zone, as in Sonic 2 (SlotMachine in s2disasm). All machines of a level share one
/// state, as in the original. Each frame one reel moves and redraws, in turn. A slot cage starts a spin. When the
/// spin ends, <see cref="Reward"/> gives the rings (or, when negative, the bombs) for the cage to hand out.
/// </summary>
public sealed class CNZSlotMachine
{
	/// <summary>The routines of the original (slot_rout).</summary>
	private const int Init = 0x00;

	private const int FirstDraw = 0x04;

	private const int Start = 0x08;

	private const int SpinUp = 0x0C;

	private const int Stop = 0x10;

	private const int Finish = 0x14;

	private const int Idle = 0x18;

	/// <summary>The face of each of the 8 positions of each reel (SlotSequence1 to 3): 0 Sonic, 1 Tails, 2 Robotnik, 3 jackpot, 4 ring, 5 bar.</summary>
	public static readonly int[][] Sequences =
	{
		new[] { 3, 0, 1, 4, 2, 5, 4, 1 },
		new[] { 3, 0, 1, 4, 2, 5, 0, 2 },
		new[] { 3, 0, 1, 4, 2, 5, 4, 1 },
	};

	/// <summary>The rings for three of each face (SlotRingRewards). Robotnik gives bombs.</summary>
	private static readonly int[] Rewards = { 30, 25, -1, 150, 10, 20 };

	/// <summary>SlotTargetValues: the chance (of 256), the target of the right reel, and the targets of the middle and the left reel.</summary>
	private static readonly int[,] Targets =
	{
		{ 0x08, 3, 0x33 }, { 0x12, 0, 0x00 }, { 0x12, 1, 0x11 }, { 0x24, 2, 0x22 }, { 0x1E, 4, 0x44 }, { 0x1E, 5, 0x55 },
	};

	/// <summary>
	/// The right-reel target of the other 116 cases. The original reads it with the wrong register, past the end of
	/// SlotSequence1, so these are the faces of the bytes that follow in the ROM (REV01).
	/// </summary>
	private static readonly byte[] FallbackRight =
	{
		0,
		0, 1, 4, 2, 5, 4, 1, 3, 0, 1, 4, 2, 5, 0, 2, 3, 0, 1, 4, 2, 5, 4, 1, 0,
		4, 1, 4, 1, 0, 1, 5, 2, 4, 1, 0, 1, 3, 2, 4, 1, 0, 1, 5, 4, 4, 1, 0, 1,
		5, 2, 4, 1, 0, 1, 5, 4, 4, 1, 0, 0, 3, 4, 4, 1, 0, 1, 5, 4, 0, 0, 0, 0,
		0, 0, 0, 4, 2, 3, 0, 4, 4, 3, 0, 2, 0, 4, 0, 4, 4, 0, 0, 4, 1, 4, 0, 2,
		4, 4, 0, 4, 1, 4, 3, 4, 0, 2, 1, 0, 2, 0, 1, 4, 0, 4, 0, 1,
	};

	private static readonly ConditionalWeakTable<Level, CNZSlotMachine> Machines = new ConditionalWeakTable<Level, CNZSlotMachine>();

	private readonly Random _random;

	private readonly ushort[] _position = new ushort[3];

	private readonly sbyte[] _speed = new sbyte[3];

	private readonly int[] _reelRoutine = new int[3];

	private readonly int[] _shownRow = new int[3];

	private int _routine;

	private int _timer;

	private int _reel;

	/// <summary>The targets while a reel spins (slots_targ), then the reward (SlotMachine_Reward).</summary>
	private int _targets;

	/// <summary>Makes a machine. A level gets its machine from <see cref="For"/>.</summary>
	public CNZSlotMachine(Random random)
	{
		_random = random;
	}

	/// <summary>The machine of a level.</summary>
	public static CNZSlotMachine For(Level level) => Machines.GetValue(level, l => new CNZSlotMachine(l.Random));

	/// <summary>True when no spin runs: a cage can start one, and hands out the reward of the last one.</summary>
	public bool IsIdle => _routine == Idle;

	/// <summary>A cage of a slot machine holds a player (SlotMachineInUse).</summary>
	public bool InUse { get; set; }

	/// <summary>The rings that the last spin gave, or a negative value for bombs. A cage counts it down.</summary>
	public int Reward
	{
		get => (short)_targets;
		set => _targets = value & 0xFFFF;
	}

	/// <summary>The first row of the 256-row strip of a reel that shows at the top of its window.</summary>
	public int GetShownRow(int reel) => _shownRow[reel];

	/// <summary>Sets the machine to its state at the start of a level.</summary>
	public void Reset()
	{
		_routine = Init;
		InUse = false;
	}

	/// <summary>Starts a spin. Returns false when the machine is not idle.</summary>
	public bool StartSpin()
	{
		if (_routine != Idle)
		{
			return false;
		}
		_routine = Start;
		return true;
	}

	/// <summary>Runs one frame of the machine.</summary>
	public void Update()
	{
		switch (_routine)
		{
			case Init:
				Initialise();
				break;
			case FirstDraw:
				DrawSlot();
				if (_timer == 0)
				{
					_routine = Idle;
					StopReels();
				}
				break;
			case Start:
				StartReels();
				break;
			case SpinUp:
				DrawSlot();
				if (_timer == 0)
				{
					int r3 = NextByte();
					for (int k = 0; k < 3; k++)
					{
						_speed[k] = (sbyte)(_speed[k] + 0x30);
					}
					_timer = (r3 & 0xF) + 0xC;
					_routine = Stop;
				}
				break;
			case Stop:
				StopStep();
				break;
			case Finish:
				FinishSpin();
				break;
		}
	}

	private int NextByte() => _random.Next(256);

	private void Initialise()
	{
		Array.Clear(_position);
		Array.Clear(_speed);
		Array.Clear(_reelRoutine);
		_targets = 0;
		_reel = 0;
		int r = NextByte();
		_position[0] = (ushort)((r << 8) | 8);
		_position[1] = (ushort)((RotateRight(r, 3) << 8) | 8);
		_position[2] = (ushort)((RotateRight(r, 6) << 8) | 8);
		for (int k = 0; k < 3; k++)
		{
			_speed[k] = 8;
			_shownRow[k] = (_position[k] >> 3) & 0xFF;
		}
		_timer = 1;
		_routine = FirstDraw;
	}

	private void StopReels()
	{
		Array.Clear(_speed);
		Array.Clear(_reelRoutine);
	}

	private void StartReels()
	{
		int r3 = NextByte();
		int r2 = NextByte();
		_speed[0] = (sbyte)((r3 & 7) + 0x2C);
		_speed[1] = (sbyte)((((r3 << 4) | (r3 >> 4)) & 7) + 0x2C);
		_speed[2] = (sbyte)((r2 & 7) + 0x2C);
		_timer = 2;
		_reel = 0;
		Array.Clear(_reelRoutine);
		_routine = SpinUp;
		// Choose the targets: walk the table with a random byte
		int d0 = RotateRight(r3, 3);
		for (int entry = 0; entry < Targets.GetLength(0); entry++)
		{
			bool borrow = d0 < Targets[entry, 0];
			d0 = (d0 - Targets[entry, 0]) & 0xFF;
			if (borrow)
			{
				_targets = (Targets[entry, 1] << 8) | Targets[entry, 2];
				return;
			}
		}
		// The terminator ($FF) always borrows: d0 is now 1 to 116
		d0 = (d0 - 0xFF) & 0xFF;
		int right = FallbackRight[Math.Clamp(d0, 0, FallbackRight.Length - 1)];
		int middle = Sequences[1][RotateRight(d0, 3) & 7];
		int left = Sequences[2][RotateRight(d0, 6) & 7];
		_targets = (right << 8) | (middle << 4) | left;
	}

	/// <summary>Moves the next reel, as SlotMachine_DrawSlot does, and remembers the row that it shows.</summary>
	private void DrawSlot()
	{
		int k = _reel / 4;
		_reel += 4;
		if (_reel == 12)
		{
			_reel = 0;
			_timer = (_timer - 1) & 0xFF;
		}
		_position[k] = (ushort)(_position[k] - _speed[k]);
		_shownRow[k] = (_position[k] >> 3) & 0xFF;
	}

	private void StopStep()
	{
		DrawSlot();
		if (_reelRoutine[0] == 0xC && _reelRoutine[1] == 0xC && _reelRoutine[2] == 0xC)
		{
			FinishSpin();
			return;
		}
		int k = _reel / 4;
		int[] sequence = Sequences[k];
		switch (_reelRoutine[k])
		{
			case 0:
				if (k == 0 ? (sbyte)_timer >= 0 : _reelRoutine[k - 1] < 8)
				{
					return;
				}
				if (sequence[((_position[k] - 0xA0) >> 8) & 7] == GetTarget(k))
				{
					_reelRoutine[k] = 4;
					_speed[k] = 0x60;
				}
				break;
			case 4:
				if (sequence[((_position[k] + 0xF0) & 0x700) >> 8] == GetTarget(k))
				{
					int d0 = (_position[k] + 0x80) & 0xFFFF;
					_position[k] = (ushort)((d0 & 0x700) - 0x10);
					SetTarget(k, sequence[(d0 >> 8) & 7]);
					_speed[k] = -8;
					_reelRoutine[k] = 8;
					return;
				}
				if ((byte)_speed[k] > 0x20)
				{
					_speed[k] = (sbyte)(_speed[k] - 0xC);
				}
				if (_speed[k] > 0x18 || (_position[k] & 0xFF) > 0x80)
				{
					return;
				}
				_speed[k] = (sbyte)(_speed[k] - 2);
				break;
			case 8:
				if ((_position[k] & 0xFF) == 0)
				{
					_speed[k] = 0;
					_reelRoutine[k] = 0xC;
				}
				break;
		}
	}

	private void FinishSpin()
	{
		StopReels();
		_timer = 0;
		_targets = ChooseReward() & 0xFFFF;
		_routine = Idle;
	}

	private int GetTarget(int k)
	{
		int value = (_targets >> (4 * k)) & 7;
		return value > 5 ? value - 2 : value;
	}

	private void SetTarget(int k, int face)
	{
		_targets = ((_targets & ~(0xF << (4 * k))) | ((face & 0xF) << (4 * k))) & 0x777;
	}

	/// <summary>The reward of the three faces (A right, B middle, C left), as the original computes it.</summary>
	private int ChooseReward()
	{
		int a = (_targets >> 8) & 0xF;
		int b = (_targets >> 4) & 0xF;
		int c = _targets & 0xF;
		if (a == b && a == c)
		{
			return Rewards[a];
		}
		if (a == c)
		{
			return c == 3 ? Rewards[b] * 4 : b == 3 ? Rewards[c] * 2 : CountBars(a, b, c);
		}
		if (a == b)
		{
			return b == 3 ? Rewards[c] * 4 : c == 3 ? Rewards[b] * 2 : CountBars(a, b, c);
		}
		if (b == c)
		{
			return a == 3 ? Rewards[b] * 2 : b == 3 ? Rewards[a] * 4 : CountBars(a, b, c);
		}
		return CountBars(a, b, c);
	}

	/// <summary>Two rings for each bar.</summary>
	private static int CountBars(int a, int b, int c) => 2 * ((a == 5 ? 1 : 0) + (b == 5 ? 1 : 0) + (c == 5 ? 1 : 0));

	private static int RotateRight(int value, int bits) => ((value >> bits) | (value << (8 - bits))) & 0xFF;
}
