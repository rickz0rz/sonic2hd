using System.Collections.Generic;

namespace SonicOrca.Core;

/// <summary>
/// The levels of the game, for the level editor. The game gives the catalog, because the list of the levels and the
/// order of the story are part of the game, not of the engine.
/// </summary>
public interface ILevelCatalog
{
	/// <summary>The levels that can be played: first the levels of the story in their order, then the other levels.</summary>
	IReadOnlyList<LevelCatalogEntry> GetLevels();

	/// <summary>Saves a new order of the story, and uses it at once. Returns the path of the file.</summary>
	/// <param name="mnemonics">The short names of the levels of the story, in their order.</param>
	string SaveStoryOrder(IReadOnlyList<string> mnemonics);
}

/// <summary>A level in the <see cref="ILevelCatalog"/>.</summary>
/// <param name="Mnemonic">The short name of the level, for example "ehz".</param>
/// <param name="Name">The name of the level, for example "Emerald Hill Zone".</param>
/// <param name="Acts">The number of acts.</param>
/// <param name="InStory">True when the level is in the story.</param>
/// <param name="IsMod">True when a mod adds the level.</param>
public sealed record LevelCatalogEntry(string Mnemonic, string Name, int Acts, bool InStory, bool IsMod);
