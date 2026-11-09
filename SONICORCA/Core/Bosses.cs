using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// A boss of Sonic 2 that a mod level can end with.
/// </summary>
/// <param name="Name">The name that the level editor shows, for example "Emerald Hill (Drill Eggman)".</param>
/// <param name="ObjectKey">The key of the boss object.</param>
/// <param name="Arena">
/// The arena of the boss. Its positions are original pixels from the act origin. In a mod level, the act origin is
/// the "boss" marker less <see cref="BossArenaSettings.BossPosition"/> times 4, so the boss starts at the marker.
/// </param>
/// <param name="State">
/// Makes the state object of the boss from the arena bounds and the act origin, in level units. The property names
/// must match the state variables of the boss object.
/// </param>
/// <param name="FloorY">
/// The floor of the arena in its zone, in level units below the start of the boss. A negative value is above it, for
/// a boss that comes up from below the floor. The level editor draws this floor, so that a level can match it.
/// </param>
public sealed record BossInfo(string Name, string ObjectKey, BossArenaSettings Arena, Func<Rectanglei, Vector2i, object> State, int FloorY);

/// <summary>
/// The bosses of the zones of Sonic 2 that a mod level can end with. The final bosses (WFZ and DEZ) are not in the
/// list, because their zones run them with events of their own.
/// </summary>
public static class Bosses
{
	private const int Scale = 4;

	/// <summary>The height of the HD camera in original pixels.</summary>
	private const int HdScreenHeight = 1080 / Scale;

	private const int OriginalScreenWidth = 320;

	private const int OriginalScreenHeight = 224;

	/// <summary>The state of the bosses that take the act origin.</summary>
	private static object OriginState(Rectanglei arena, Vector2i origin) => new { OriginX = origin.X, OriginY = origin.Y };

	/// <summary>LevEvents_ARZ2 and Obj89.</summary>
	public static readonly BossInfo AquaticRuin = new BossInfo("Aquatic Ruin", "SONICORCA/OBJECTS/ARZBOSS",
		new BossArenaSettings(0x2810, 0x400, 0x2A40, 0x2A40, 0x2A40, 0x3F8, new Vector2i(0x2AE0, 0x388), 0x2C00, SpawnOnLock: true),
		OriginState, (0x4D0 - 0x388) * Scale);

	/// <summary>LevEvents_CNZ2 and Obj51.</summary>
	public static readonly BossInfo CasinoNight = new BossInfo("Casino Night", "SONICORCA/OBJECTS/CNZBOSS",
		new BossArenaSettings(0x27C0, 0x62E, 0x2890, 0x2860, 0x28E0, 0x4E0, new Vector2i(0x2A46, 0x654), 0x2B20),
		OriginState, (0x700 - 0x654) * Scale);

	/// <summary>LevEvents_MCZ2 and Obj57.</summary>
	public static readonly BossInfo MysticCave = new BossInfo("Mystic Cave", "SONICORCA/OBJECTS/MCZBOSS",
		new BossArenaSettings(0x2080, 0x5D0, 0x20F0, 0x20F0, 0x20F0, 0x5C8, new Vector2i(0x21A0, 0x560), 0x2240),
		OriginState, (0x680 - 0x560) * Scale);

	/// <summary>LevEvents_MTZ3 and Obj54.</summary>
	public static readonly BossInfo Metropolis = new BossInfo("Metropolis", "SONICORCA/OBJECTS/MTZBOSS",
		new BossArenaSettings(0x2980, 0x400, 0x2A80, 0x2AB0, 0x2AB0, 0x400, new Vector2i(0x2B50, 0x380), 0x2BF0),
		OriginState, (0x4C0 - 0x380) * Scale);

	/// <summary>LevEvents_OOZ2 and Obj55.</summary>
	public static readonly BossInfo OilOcean = new BossInfo("Oil Ocean", "SONICORCA/OBJECTS/OOZBOSS",
		new BossArenaSettings(0x2668, 0x1E0, 0x2880, 0x2880, 0x28C0, 0x1D8, new Vector2i(0x2940, 0x2D0), 0x2A20),
		OriginState, (0x260 - 0x2D0) * Scale);

	/// <summary>
	/// The Drill Eggman of Emerald Hill. The values are the boss markers of the HD map of EHZ, from the marker
	/// "boss_position": boss_oneway, boss_left, boss_right and boss_bottom.
	/// </summary>
	public static readonly BossInfo EmeraldHill = new BossInfo("Emerald Hill", "SONICORCA/OBJECTS/DRILLEGGMAN",
		FromHdMarkers(oneWayX: 85184 - 88901, left: 86656 - 88901, right: 88576 - 88901, bottom: 4736 - 3232),
		(arena, origin) => new { LeftEdge = arena.Left, RightEdge = arena.Right }, 1328);

	/// <summary>
	/// The Water Eggman of Chemical Plant, from the boss markers of the HD map of CPZ. The boss flies in from the
	/// right, so the marker "boss" is to the right of the arena. It fills the tubes at boss_fill_left and boss_fill_right.
	/// </summary>
	public static readonly BossInfo ChemicalPlant = new BossInfo("Chemical Plant", "SONICORCA/OBJECTS/WATEREGGMAN",
		FromHdMarkers(oneWayX: 81408 - 87296, left: 84800 - 87296, right: 86720 - 87296, bottom: 8384 - 7872),
		(arena, origin) => new { LeftFillX = origin.X + (85300 - 87296), RightFillX = origin.X + (86228 - 87296) }, 320);

	/// <summary>
	/// The Submarine Eggman of Hill Top, from the boss markers of the HD map of HTZ. The arena is 68 units wider than
	/// boss_left and boss_right on each side, and its bottom is boss_oneway. The boss comes up from below the arena.
	/// </summary>
	public static readonly BossInfo HillTop = new BossInfo("Hill Top", "SONICORCA/OBJECTS/SUBMARINEEGGMAN",
		FromHdMarkers(oneWayX: 90432 - 94464, left: 93056 - 68 - 94464, right: 94856 + 68 - 94464, bottom: 5504 - 5632),
		(arena, origin) => null, -496);

	/// <summary>All the bosses, in the order of the story.</summary>
	public static readonly IReadOnlyList<BossInfo> All = new[]
	{
		EmeraldHill, ChemicalPlant, AquaticRuin, CasinoNight, HillTop, MysticCave, OilOcean, Metropolis
	};

	/// <summary>The boss with the object key, or null.</summary>
	public static BossInfo Find(string objectKey) => All.FirstOrDefault(b => string.Equals(b.ObjectKey, objectKey, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Makes the arena of an HD boss from the positions of its markers, in level units from the boss position. The
	/// act origin is then the boss position. The arena is as high as the HD screen, above the bottom.
	/// </summary>
	/// <param name="oneWayX">The camera left at which the camera cannot go back.</param>
	/// <param name="left">The left of the arena: the camera left at which the arena locks.</param>
	/// <param name="right">The right of the arena.</param>
	/// <param name="bottom">The bottom of the arena.</param>
	private static BossArenaSettings FromHdMarkers(int oneWayX, int left, int right, int bottom)
	{
		// The arena takes a camera X of the original: the left of a 320 pixel screen at the centre of the HD camera
		int hdHalfWidth = 1920 / 2;
		int triggerX = (oneWayX + hdHalfWidth) / Scale - OriginalScreenWidth / 2;
		int lockX = (left + hdHalfWidth) / Scale - OriginalScreenWidth / 2;
		int maxY = bottom / Scale - OriginalScreenHeight;
		int minY = maxY + OriginalScreenHeight - HdScreenHeight;
		return new BossArenaSettings(triggerX, maxY, lockX, left / Scale, right / Scale - OriginalScreenWidth, minY, new Vector2i(0, 0),
			right / Scale - OriginalScreenWidth);
	}
}
