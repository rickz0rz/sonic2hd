using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests of the list of the bosses that can end a mod level.</summary>
public sealed class BossesTests
{
	/// <summary>
	/// The arena of the EHZ boss in a mod level must be the arena of the HD map of EHZ: from boss_left to boss_right,
	/// with its bottom at boss_bottom, as distances from boss_position. The arena values are in original pixels, so
	/// each edge can be up to 4 units off.
	/// </summary>
	[Fact]
	public void EmeraldHill_ArenaIsTheArenaOfTheHdMap()
	{
		Rectanglei arena = BossArena.GetArenaBounds(Bosses.EmeraldHill.Arena, new Vector2i(0, 0), 1920, 1080);

		Assert.InRange(arena.Left, 86656 - 88901 - 4, 86656 - 88901 + 4);
		Assert.InRange(arena.Right, 88576 - 88901 - 4, 88576 - 88901 + 4);
		Assert.InRange(arena.Bottom, 4736 - 3232 - 4, 4736 - 3232 + 4);
		Assert.Equal(1080, arena.Height);
	}

	/// <summary>Each boss has its own object, and each arena is at least as large as the HD screen.</summary>
	[Fact]
	public void EachBoss_HasItsOwnObject_AndAnArenaOfAtLeastOneScreen()
	{
		Assert.Equal(Bosses.All.Count, Bosses.All.Select(boss => boss.ObjectKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
		foreach (BossInfo boss in Bosses.All)
		{
			Vector2i origin = boss.Arena.BossPosition * -4;
			Rectanglei arena = BossArena.GetArenaBounds(boss.Arena, origin, 1920, 1080);
			Assert.True(arena.Width >= 1920 && arena.Height >= 1080, $"{boss.Name}: {arena}");
			Assert.Same(boss, Bosses.Find(boss.ObjectKey.ToLowerInvariant()));
		}
	}
}
