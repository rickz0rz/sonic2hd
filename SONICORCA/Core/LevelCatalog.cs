using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core.Editor;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Gives the levels of the game to the level editor, saves the order of the story into mods/editor/LEVELS.dat, and
/// makes new levels as mod folders, mods/NAME.
/// </summary>
internal sealed class LevelCatalog : ILevelCatalog
{
	private readonly SonicOrcaGameContext _gameContext;

	public LevelCatalog(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
	}

	public IReadOnlyList<LevelCatalogEntry> GetLevels()
	{
		ResourceTree tree = _gameContext.ResourceTree;
		IEnumerable<LevelCatalogEntry> story = Levels.GetStoryLevels(tree).Select((level, index) => ToEntry(level, index + 1));
		IEnumerable<LevelCatalogEntry> others = Levels.GetOtherLevels(tree).Select(level => ToEntry(level, 0));
		return story.Concat(others).ToArray();
	}

	public string SaveStoryOrder(IReadOnlyList<string> mnemonics)
	{
		ResourceTree tree = _gameContext.ResourceTree;
		Resource resource = tree[Levels.OrderResourceKey]?.Resource;
		if (resource == null)
		{
			// The first save: the game data has no order resource. The new resource replaces the default order at once.
			resource = new Resource(Levels.OrderResourceKey, ResourceTypeIdentifier.Xml, new MemoryResourceSource(Levels.WriteOrder(mnemonics)));
			tree.SetOrAdd(Levels.OrderResourceKey, resource);
		}
		EditorModFile.Change change = new EditorModFile.Change(resource, Levels.WriteOrder(mnemonics));
		return EditorModFile.Save(new[] { change }, EditorModFile.GetPath(_gameContext.UserDataDirectory, Levels.OrderResourceKey));
	}

	public LevelCatalogEntry CreateLevel(string mnemonic, string name, string tilesFrom)
	{
		ResourceTree tree = _gameContext.ResourceTree;
		mnemonic = mnemonic?.ToLowerInvariant();
		if (NewLevel.CheckMnemonic(mnemonic) is string problem)
			throw new InvalidOperationException(problem);
		if (string.IsNullOrWhiteSpace(name))
			throw new InvalidOperationException("The name is empty.");
		if (Levels.GetAreaResourceKey(mnemonic, tree) != null)
			throw new InvalidOperationException($"A level with the short name {mnemonic} exists.");
		string folder = Path.Combine(_gameContext.UserDataDirectory, "mods", mnemonic);
		if (Directory.Exists(folder) || File.Exists(folder + ".dat"))
			throw new InvalidOperationException($"The mod {mnemonic} exists in the mods folder.");
		string tileSetKey = Levels.GetTileSetResourceKey(tilesFrom, tree);
		if (tileSetKey == null)
			throw new InvalidOperationException($"The level {tilesFrom} has no tile set.");

		string levelKey = $"SONICORCA/LEVELS/{mnemonic.ToUpperInvariant()}";
		ResourceTree files = new ResourceTree();
		void Add(string key, ResourceTypeIdentifier identifier, byte[] data) =>
			files.SetOrAdd($"{levelKey}/{key}", new Resource($"{levelKey}/{key}", identifier, new MemoryResourceSource(data)));
		Add("AREA", ResourceTypeIdentifier.Area, NewLevel.MakeArea());
		Add("INFO", ResourceTypeIdentifier.Xml, NewLevel.MakeInfo(name.Trim(), tileSetKey));
		Add("MAP", ResourceTypeIdentifier.Map, NewLevel.MakeMap());
		Add("BINDING", ResourceTypeIdentifier.Binding, NewLevel.MakeBinding());
		ResourceDirectory.Export(files, folder);

		// The game loads the mod folders when it starts. The new folder loads now, so that the level can open.
		foreach (KeyValuePair<string, Resource> item in ResourceDirectory.Import(folder).GetResourceListing())
			tree.SetOrAdd(item.Key, item.Value);
		return GetLevels().First(level => level.Mnemonic == mnemonic);
	}

	private LevelCatalogEntry ToEntry(LevelInfo level, int storyNumber)
	{
		return new LevelCatalogEntry(level.Mnemonic, level.Name, level.Acts, level.GameIndex == Levels.Mod,
			Levels.GetAreaResourceKey(level.Mnemonic, _gameContext.ResourceTree), storyNumber);
	}
}
