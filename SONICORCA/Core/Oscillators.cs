using System.Runtime.CompilerServices;

namespace SonicOrca.Core;

/// <summary>
/// The 16 global oscillators of Sonic 2 (OscillateNumInit and OscillateNumDo). Many objects move with them, so all
/// objects of a level read the same values. The values catch up with the level ticks, so the update order of the
/// objects does not matter. As in the original, an object reads the values of the end of the previous frame.
/// </summary>
public sealed class Oscillators
{
	private static readonly ConditionalWeakTable<Level, Oscillators> All = new ConditionalWeakTable<Level, Oscillators>();

	private static readonly int[] InitialValues = { 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x3848, 0x2080, 0x3080, 0x5080, 0x7080, 0x80, 0x4000 };

	private static readonly int[] InitialDeltas = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xEE, 0xB4, 0x10E, 0x1C2, 0x276, 0, 0xFE };

	/// <summary>The speed and the limit of each oscillator (Osc_Data2).</summary>
	private static readonly int[,] SpeedsAndLimits =
	{
		{ 2, 0x10 }, { 2, 0x18 }, { 2, 0x20 }, { 2, 0x30 }, { 4, 0x20 }, { 8, 8 }, { 8, 0x40 }, { 4, 0x40 },
		{ 2, 0x38 }, { 2, 0x38 }, { 2, 0x20 }, { 3, 0x30 }, { 5, 0x50 }, { 7, 0x70 }, { 2, 0x40 }, { 2, 0x40 },
	};

	private readonly int[] _values = new int[16];

	private readonly int[] _deltas = new int[16];

	private int _control;

	private int _ticks = -1;

	public static Oscillators For(Level level) => All.GetValue(level, l => new Oscillators());

	/// <summary>The high byte of oscillator i, as objects read it.</summary>
	public int GetHighByte(Level level, int i)
	{
		CatchUp(level.Ticks);
		return (_values[i] >> 8) & 0xFF;
	}

	private void CatchUp(int ticks)
	{
		if (ticks < _ticks || _ticks < 0)
		{
			Reset();
			_ticks = 0;
		}
		// Values are those of the end of the previous frame
		while (_ticks < ticks)
		{
			Step();
			_ticks++;
		}
	}

	private void Reset()
	{
		_control = 0b0000000001111101;
		for (int i = 0; i < 16; i++)
		{
			_values[i] = InitialValues[i];
			_deltas[i] = InitialDeltas[i];
		}
	}

	private void Step()
	{
		for (int i = 0; i < 16; i++)
		{
			int bit = 1 << (15 - i);
			int speed = SpeedsAndLimits[i, 0];
			int limit = SpeedsAndLimits[i, 1];
			if ((_control & bit) == 0)
			{
				_deltas[i] = (_deltas[i] + speed) & 0xFFFF;
				_values[i] = (_values[i] + _deltas[i]) & 0xFFFF;
				if (!(limit > ((_values[i] >> 8) & 0xFF)))
				{
					_control |= bit;
				}
			}
			else
			{
				_deltas[i] = (_deltas[i] - speed) & 0xFFFF;
				_values[i] = (_values[i] + _deltas[i]) & 0xFFFF;
				if (!(limit <= ((_values[i] >> 8) & 0xFF)))
				{
					_control &= ~bit;
				}
			}
		}
	}
}
