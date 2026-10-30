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

	/// <summary>
	/// Makes a new level that uses the tile set of another level, and loads it at once. The new level is not in the
	/// story. Throws <see cref="System.InvalidOperationException"/> with a message for the user when the level cannot
	/// be made, for example when the short name is in use.
	/// </summary>
	/// <param name="mnemonic">The short name of the new level: letters and digits.</param>
	/// <param name="name">The name of the new level.</param>
	/// <param name="tilesFrom">The short name of the level with the tile set.</param>
	/// <returns>The new level.</returns>
	LevelCatalogEntry CreateLevel(string mnemonic, string name, string tilesFrom);
}

/// <summary>A level in the <see cref="ILevelCatalog"/>.</summary>
/// <param name="Mnemonic">The short name of the level, for example "ehz".</param>
/// <param name="Name">The name of the level, for example "Emerald Hill Zone".</param>
/// <param name="Acts">The number of acts.</param>
/// <param name="IsMod">True when a mod adds the level.</param>
/// <param name="AreaResourceKey">The key of the area of the level, for example "SONICORCA/LEVELS/EHZ/AREA".</param>
/// <param name="StoryNumber">The number of the level in the story, from 1, or 0 when the level is not in the story.</param>
public sealed record LevelCatalogEntry(string Mnemonic, string Name, int Acts, bool IsMod, string AreaResourceKey, int StoryNumber)
{
	public bool InStory => StoryNumber > 0;
}
