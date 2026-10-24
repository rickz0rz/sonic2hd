using System.Runtime.CompilerServices;

namespace SonicOrca.Core;

/// <summary>
/// The 16 trigger bytes that switches share with the objects they open, as in Sonic 2 (ButtonVine_Trigger). A switch
/// sets a bit of the byte at its index, and a bridge or a door with the same index reads it. The bytes belong to the
/// level. They go back to zero when the level starts again (the level ticks go back), as the original clears them
/// when it loads a level.
/// </summary>
public sealed class ButtonVineTriggers
{
	public const int Count = 16;

	private static readonly ConditionalWeakTable<Level, ButtonVineTriggers> All = new ConditionalWeakTable<Level, ButtonVineTriggers>();

	private readonly byte[] _values = new byte[Count];

	private int _ticks;

	public static ButtonVineTriggers For(Level level)
	{
		ButtonVineTriggers triggers = All.GetValue(level, l => new ButtonVineTriggers());
		if (level.Ticks < triggers._ticks)
		{
			System.Array.Clear(triggers._values);
		}
		triggers._ticks = level.Ticks;
		return triggers;
	}

	/// <summary>True when the bit of the trigger at the index is set.</summary>
	public bool IsSet(int index, int bit = 0) => (_values[index & (Count - 1)] & (1 << bit)) != 0;

	/// <summary>True when any bit of the trigger at the index is set.</summary>
	public bool IsAnySet(int index) => _values[index & (Count - 1)] != 0;

	public void Set(int index, int bit = 0) => _values[index & (Count - 1)] |= (byte)(1 << bit);

	public void Clear(int index, int bit = 0) => _values[index & (Count - 1)] &= (byte)~(1 << bit);
}
