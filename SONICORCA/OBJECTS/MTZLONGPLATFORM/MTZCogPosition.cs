using System.Runtime.CompilerServices;
using SonicOrca.Core;

namespace SONICORCA.OBJECTS.MTZLONGPLATFORM;

/// <summary>
/// The x position that the cogs of Metropolis Zone turn with, as in Sonic 2 (MTZ_Platform_Cog_X). The long platform
/// that rides along when a character stands on it (Obj65 subtype 4) writes its x position each frame. Each free cog
/// (Obj65 with art 2) reads it. The value belongs to the level. It goes back to zero when the level starts again.
/// </summary>
public sealed class MTZCogPosition
{
	private static readonly ConditionalWeakTable<Level, MTZCogPosition> All = new ConditionalWeakTable<Level, MTZCogPosition>();

	private int _x;

	private int _ticks;

	public static MTZCogPosition For(Level level)
	{
		MTZCogPosition position = All.GetValue(level, l => new MTZCogPosition());
		if (level.Ticks < position._ticks)
		{
			position._x = 0;
		}
		position._ticks = level.Ticks;
		return position;
	}

	/// <summary>The x position in original pixels.</summary>
	public int X
	{
		get => _x;
		set => _x = value;
	}

	/// <summary>The cog frame for a position (byte_26EBA): the cog turns one frame each 2 or 3 pixels.</summary>
	public static int GetFrame(int x) => (x & 7) switch
	{
		0 or 1 => 0,
		2 or 3 or 4 => 2,
		_ => 1,
	};
}
