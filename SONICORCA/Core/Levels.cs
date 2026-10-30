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

	/// <summary>
	/// Returns the Sonic 2 levels that can be played, in the order of the game. A level has the name from the order
	/// resource when the level editor gave it a new name.
	/// </summary>
	public static IReadOnlyList<LevelInfo> GetAvailableLevels(ResourceTree resourceTree)
	{
		IReadOnlyDictionary<string, string> titles = ReadTitles(resourceTree);
		return LevelList.Where(x => x.GameIndex == Sonic2 && IsAvailable(x, resourceTree))
			.Select(x => titles.TryGetValue(x.Mnemonic, out string title) ? new LevelInfo(x.GameIndex, x.Mnemonic, title, x.Acts, x.Unreleased) : x)
			.ToArray();
	}

	/// <summary>Returns the name of a Sonic 2 level in the game, without a new name from the level editor.</summary>
	public static string GetOriginalName(string mnemonic)
	{
		return LevelList.FirstOrDefault(x => x.GameIndex == Sonic2 && x.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))?.Name;
	}

	/// <summary>
	/// The resource with the order of the levels of the story, for example
	/// <c>&lt;levels&gt;&lt;level name="ehz" /&gt;&lt;level name="meadow" /&gt;&lt;/levels&gt;</c>. The level editor
	/// writes it. Without it, the story has the Sonic 2 levels in the order of the game.
	/// </summary>
	public const string OrderResourceKey = "SONICORCA/LEVELS/ORDER";

	/// <summary>
	/// Returns the levels of the story, in their order. The story skips a level of the order that cannot be played,
	/// for example a mod level that is not installed.
	/// </summary>
	public static IReadOnlyList<LevelInfo> GetStoryLevels(ResourceTree resourceTree)
	{
		IReadOnlyList<string> order = ReadOrder(resourceTree);
		if (order == null)
			return GetAvailableLevels(resourceTree);
		// A Sonic 2 level that cannot be played is not in the available levels, and a mod level has another short name
		IEnumerable<LevelInfo> playable = GetAvailableLevels(resourceTree).Concat(GetModLevels(resourceTree));
		List<LevelInfo> levels = new List<LevelInfo>();
		foreach (string mnemonic in order.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			LevelInfo level = playable.FirstOrDefault(x => x.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase));
			if (level != null)
				levels.Add(level);
		}
		return levels;
	}

	/// <summary>Returns the levels that can be played but are not in the story: Sonic 2 levels first, then mod levels.</summary>
	public static IReadOnlyList<LevelInfo> GetOtherLevels(ResourceTree resourceTree)
	{
		IReadOnlyList<LevelInfo> story = GetStoryLevels(resourceTree);
		return GetAvailableLevels(resourceTree).Concat(GetModLevels(resourceTree))
			.Where(level => !story.Any(x => x.Mnemonic.Equals(level.Mnemonic, StringComparison.OrdinalIgnoreCase)))
			.ToArray();
	}

	/// <summary>
	/// Reads the new names of the Sonic 2 levels from the order resource, for example
	/// <c>&lt;title level="ehz" text="Green Hill Zone" /&gt;</c>, by short name.
	/// </summary>
	public static IReadOnlyDictionary<string, string> ReadTitles(ResourceTree resourceTree)
	{
		Dictionary<string, string> titles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		System.Xml.XmlDocument document = ReadOrderDocument(resourceTree);
		if (document == null)
			return titles;
		foreach (System.Xml.XmlElement title in document.SelectNodes("levels/title").OfType<System.Xml.XmlElement>())
		{
			string level = title.GetAttribute("level");
			string text = title.GetAttribute("text").Trim();
			if (level.Length > 0 && text.Length > 0)
				titles[level.ToLowerInvariant()] = text;
		}
		return titles;
	}

	private static System.Xml.XmlDocument ReadOrderDocument(ResourceTree resourceTree)
	{
		Resource resource = resourceTree[OrderResourceKey]?.Resource;
		if (resource == null)
			return null;
		try
		{
			System.Xml.XmlDocument document = new System.Xml.XmlDocument();
			using (System.IO.Stream stream = resource.Source.ReadUncompressed())
				document.Load(stream);
			return document;
		}
		catch (System.Xml.XmlException)
		{
			return null;
		}
	}

	/// <summary>Reads the names of the levels in the order resource, or null when there is no order resource.</summary>
	public static IReadOnlyList<string> ReadOrder(ResourceTree resourceTree)
	{
		return ReadOrderDocument(resourceTree)?.SelectNodes("levels/level/@name").OfType<System.Xml.XmlNode>().Select(x => x.Value.ToLowerInvariant()).ToArray();
	}

	/// <summary>Gives the order resource for the levels of the story, and the new names of Sonic 2 levels.</summary>
	public static byte[] WriteOrder(IEnumerable<string> mnemonics, IReadOnlyDictionary<string, string> titles = null)
	{
		using System.IO.MemoryStream stream = new System.IO.MemoryStream();
		using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(stream, new System.Xml.XmlWriterSettings { Indent = true }))
		{
			writer.WriteStartElement("levels");
			foreach (string mnemonic in mnemonics)
			{
				writer.WriteStartElement("level");
				writer.WriteAttributeString("name", mnemonic.ToLowerInvariant());
				writer.WriteEndElement();
			}
			foreach (KeyValuePair<string, string> title in titles ?? new Dictionary<string, string>())
			{
				writer.WriteStartElement("title");
				writer.WriteAttributeString("level", title.Key.ToLowerInvariant());
				writer.WriteAttributeString("text", title.Value);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		return stream.ToArray();
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
	private static string ReadModLevelName(ResourceTree resourceTree, string folder) => ReadInfoAttribute(resourceTree, folder, "name");

	/// <summary>An attribute of the INFO file of a mod level, or null when the attribute or the file is missing.</summary>
	private static string ReadInfoAttribute(ResourceTree resourceTree, string folder, string attribute)
	{
		Resource info = resourceTree[$"SONICORCA/LEVELS/{folder.ToUpperInvariant()}/INFO"]?.Resource;
		if (info == null)
			return null;
		try
		{
			System.Xml.XmlDocument document = new System.Xml.XmlDocument();
			using (System.IO.Stream stream = info.Source.ReadUncompressed())
				document.Load(stream);
			string value = document.DocumentElement?.GetAttribute(attribute);
			return string.IsNullOrEmpty(value) ? null : value;
		}
		catch (System.Xml.XmlException)
		{
			return null;
		}
	}

	/// <summary>The music of a mod level from its INFO file, or null.</summary>
	public static string GetModLevelMusic(string mnemonic, ResourceTree resourceTree) => ReadInfoAttribute(resourceTree, mnemonic, "music");

	/// <summary>
	/// Gives the INFO file of a mod level with a new name and new music. The other attributes of the file stay.
	/// </summary>
	public static byte[] WriteModLevelInfo(System.IO.Stream info, string name, string music)
	{
		System.Xml.XmlDocument document = new System.Xml.XmlDocument();
		document.Load(info);
		document.DocumentElement.SetAttribute("name", name);
		if (string.IsNullOrEmpty(music))
			document.DocumentElement.RemoveAttribute("music");
		else
			document.DocumentElement.SetAttribute("music", music);
		using System.IO.MemoryStream stream = new System.IO.MemoryStream();
		using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(stream, new System.Xml.XmlWriterSettings { Indent = true }))
			document.Save(writer);
		return stream.ToArray();
	}

	/// <summary>The music that a level can play: the music resources of the game and of the mods, in the order of their keys.</summary>
	public static IReadOnlyList<string> GetMusicKeys(ResourceTree resourceTree)
	{
		return resourceTree.GetResourceListing()
			.Where(x => x.Key.StartsWith("SONICORCA/MUSIC/", StringComparison.Ordinal) && x.Value.Identifier == ResourceTypeIdentifier.SampleInfo)
			.Select(x => x.Key)
			.OrderBy(x => x, StringComparer.Ordinal)
			.ToArray();
	}

	/// <summary>
	/// Returns the key of the tile set of a level: the tile set in the INFO file of a mod level, or the TILESET of the
	/// level. Returns null when the level has no tile set.
	/// </summary>
	public static string GetTileSetResourceKey(string mnemonic, ResourceTree resourceTree)
	{
		if (string.IsNullOrEmpty(mnemonic) || !mnemonic.All(char.IsAsciiLetterOrDigit))
			return null;
		string key = ReadInfoAttribute(resourceTree, mnemonic, "tileset") ?? $"SONICORCA/LEVELS/{mnemonic.ToUpperInvariant()}/TILESET";
		return resourceTree[key]?.Resource != null ? key : null;
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
	/// Returns the number of a level in the story, counted from 1, or -1 when the level is not in the story. Story mode
	/// uses the same numbers (see <see cref="GetStoryLevels"/>), so the next level after a level from --level is the
	/// next level of the story. The comparison ignores case.
	/// </summary>
	public static int GetLevelIndex(string mnemonic, ResourceTree resourceTree)
	{
		IReadOnlyList<LevelInfo> levels = GetStoryLevels(resourceTree);
		for (int i = 0; i < levels.Count; i++)
		{
			if (levels[i].Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
				return i + 1;
		}
		return -1;
	}
}
