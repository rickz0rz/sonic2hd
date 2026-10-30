using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Editor;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Gives the levels of the game to the level editor, and saves the order of the story into mods/editor/LEVELS.dat.
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
		IEnumerable<LevelCatalogEntry> story = Levels.GetStoryLevels(tree).Select(level => ToEntry(level, inStory: true));
		IEnumerable<LevelCatalogEntry> others = Levels.GetOtherLevels(tree).Select(level => ToEntry(level, inStory: false));
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

	private static LevelCatalogEntry ToEntry(LevelInfo level, bool inStory)
	{
		return new LevelCatalogEntry(level.Mnemonic, level.Name, level.Acts, inStory, level.GameIndex == Levels.Mod);
	}
}
