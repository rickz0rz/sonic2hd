using System.Runtime.CompilerServices;
using SonicOrca.Core;

namespace SONICORCA.OBJECTS.TORNADO;

/// <summary>
/// The state of the end of Wing Fortress Zone that the Tornado (ObjB2, subtype $54) shares with the area of the
/// zone. The Tornado sets the flags, and the area reads them: it does not end the act without the Tornado while the
/// Tornado controls the player, and it ends the act when the original goes to Death Egg Zone.
/// </summary>
public sealed class WFZEnding
{
	private static readonly ConditionalWeakTable<Level, WFZEnding> All = new ConditionalWeakTable<Level, WFZEnding>();

	/// <summary>The Tornado controls the player (ObjB2_Wait_Leader_position is complete).</summary>
	public bool Started { get; set; }

	/// <summary>The player holds on to Eggman's ship, and the ship flies away (LevEvents_WFZ_Routine4).</summary>
	public bool Leaving { get; set; }

	/// <summary>The act is complete (ObjB2_Start_DEZ).</summary>
	public bool Finished { get; set; }

	public static WFZEnding For(Level level) => All.GetValue(level, l => new WFZEnding());

	/// <summary>Clears all the flags, for a new start of the level.</summary>
	public void Reset()
	{
		Started = false;
		Leaving = false;
		Finished = false;
	}
}
