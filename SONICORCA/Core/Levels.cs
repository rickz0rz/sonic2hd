using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Core;

/// <summary>
/// The list of the levels of the game, and functions to find a level by its short name (mnemonic).
/// </summary>
internal static class Levels
{
	// The values of LevelInfo.GameIndex.
	public const int Sonic1 = 1;
	public const int Sonic2 = 2;
	public const int Sonic3 = 3;
	public const int SonicSK = 4;
	public static readonly IReadOnlyList<LevelInfo> LevelList = new LevelInfo[11]
	{
		new LevelInfo(Sonic2, "ehz", "Emerald Hill Zone", 2),
		new LevelInfo(Sonic2, "cpz", "Chemical Plant Zone", 2),
		new LevelInfo(Sonic2, "arz", "Aquatic Ruin Zone", 2, true),
		new LevelInfo(Sonic2, "cnz", "Casino Night Zone", 2, true),
		new LevelInfo(Sonic2, "htz", "Hill Top Zone", 2),
		new LevelInfo(Sonic2, "mcz", "Mystic Cave Zone", 2, true),
		new LevelInfo(Sonic2, "ooz", "Oil Ocean Zone", 2, true),
		new LevelInfo(Sonic2, "mtz", "Metropolis Zone", 3, true),
		new LevelInfo(Sonic2, "scz", "Sky Chase Zone", 1, true),
		new LevelInfo(Sonic2, "wfz", "Wing Fortress Zone", 1, true),
		new LevelInfo(Sonic2, "dez", "Death Egg Zone", 1, true)
	};

	/// <summary>Returns the resource key of the area of a Sonic 2 level, or null when the level does not exist.</summary>
	public static string GetAreaResourceKey(string mnemonic)
	{
		LevelInfo level = LevelList.Where(x => x.GameIndex == Sonic2).FirstOrDefault(x => x.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase));
		return level != null ? $"SONICORCA/LEVELS/{mnemonic.ToUpper()}/AREA" : null;
	}

	/// <summary>
	/// Returns the number of a Sonic 2 level, counted from 1, or -1 when the level does not exist.
	/// The count includes the unreleased levels. The comparison is case-sensitive.
	/// </summary>
	public static int GetLevelIndex(string mnemonic)
	{
		int levelIndex = 1;
		foreach (LevelInfo level in LevelList)
		{
			if (level.GameIndex == Sonic2)
			{
				if (level.Mnemonic == mnemonic)
					return levelIndex;
				++levelIndex;
			}
		}
		return -1;
	}
}
