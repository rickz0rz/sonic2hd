using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Resources;

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
	/// <summary>The game index of a level that a mod adds.</summary>
	public const int Mod = 0;
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

	/// <summary>
	/// True when the level can be played: a released level, or an unreleased level with the data of
	/// "s2hd-data import-level" from a mod. Every import writes the texture of the priority tiles.
	/// </summary>
	public static bool IsAvailable(LevelInfo level, ResourceTree resourceTree)
	{
		return !level.Unreleased || resourceTree[$"SONICORCA/LEVELS/{level.Mnemonic.ToUpperInvariant()}/TILESET/PRIORITY"]?.Resource != null;
	}

	/// <summary>Returns the Sonic 2 levels that can be played, in the order of the game.</summary>
	public static IReadOnlyList<LevelInfo> GetAvailableLevels(ResourceTree resourceTree)
	{
		return LevelList.Where(x => x.GameIndex == Sonic2 && IsAvailable(x, resourceTree)).ToArray();
	}

	/// <summary>Returns the resource key of the area of a Sonic 2 level, or null when the level does not exist.</summary>
	public static string GetAreaResourceKey(string mnemonic)
	{
		LevelInfo level = LevelList.Where(x => x.GameIndex == Sonic2).FirstOrDefault(x => x.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase));
		return level != null ? $"SONICORCA/LEVELS/{mnemonic.ToUpper()}/AREA" : null;
	}

	/// <summary>
	/// Returns the levels that mods add: each SONICORCA/LEVELS/NAME/AREA that is not a Sonic 2 level, by name. The
	/// name of a level comes from its INFO file. A mod level has one act.
	/// </summary>
	public static IReadOnlyList<LevelInfo> GetModLevels(ResourceTree resourceTree)
	{
		List<LevelInfo> levels = new List<LevelInfo>();
		foreach (string key in resourceTree.GetResourceListing().Keys)
		{
			string[] parts = key.Split('/');
			if (parts.Length != 4 || parts[0] != "SONICORCA" || parts[1] != "LEVELS" || parts[3] != "AREA")
				continue;
			string mnemonic = parts[2].ToLowerInvariant();
			if (GetAreaResourceKey(mnemonic) != null || !mnemonic.All(char.IsAsciiLetterOrDigit))
				continue;
			levels.Add(new LevelInfo(Mod, mnemonic, ReadModLevelName(resourceTree, parts[2]) ?? parts[2], 1));
		}
		return levels.OrderBy(level => level.Name, StringComparer.OrdinalIgnoreCase).ToArray();
	}

	/// <summary>The name in the INFO file of a mod level, or null.</summary>
	private static string ReadModLevelName(ResourceTree resourceTree, string folder)
	{
		Resource info = resourceTree[$"SONICORCA/LEVELS/{folder}/INFO"]?.Resource;
		if (info == null)
			return null;
		try
		{
			System.Xml.XmlDocument document = new System.Xml.XmlDocument();
			using (System.IO.Stream stream = info.Source.ReadUncompressed())
				document.Load(stream);
			string name = document.DocumentElement?.GetAttribute("name");
			return string.IsNullOrEmpty(name) ? null : name;
		}
		catch (System.Xml.XmlException)
		{
			return null;
		}
	}

	/// <summary>
	/// Returns the resource key of the area of a Sonic 2 level, or of a level that a mod adds in
	/// SONICORCA/LEVELS/NAME/AREA. Returns null when the level does not exist. A name has only letters and digits.
	/// </summary>
	public static string GetAreaResourceKey(string mnemonic, ResourceTree resourceTree)
	{
		string key = GetAreaResourceKey(mnemonic);
		if (key != null || string.IsNullOrEmpty(mnemonic) || !mnemonic.All(char.IsAsciiLetterOrDigit))
			return key;
		key = $"SONICORCA/LEVELS/{mnemonic.ToUpperInvariant()}/AREA";
		return resourceTree[key]?.Resource != null ? key : null;
	}

	/// <summary>
	/// Returns the number of a Sonic 2 level, counted from 1 over the levels that can be played, or -1 when the level
	/// cannot be played. Story mode uses the same numbers (see <see cref="GetAvailableLevels"/>), so the next level
	/// after a level from --level is the next level of the story. The comparison ignores case.
	/// </summary>
	public static int GetLevelIndex(string mnemonic, ResourceTree resourceTree)
	{
		IReadOnlyList<LevelInfo> levels = GetAvailableLevels(resourceTree);
		for (int i = 0; i < levels.Count; i++)
		{
			if (levels[i].Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
				return i + 1;
		}
		return -1;
	}
}
