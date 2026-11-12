using System.Collections.Generic;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The levels of the game, for the level editor. The game gives the catalog, because the list of the levels and the
/// order of the story are part of the game, not of the engine.
/// </summary>
public interface ILevelCatalog
{
	/// <summary>The levels that can be played: first the levels of the story in their order, then the other levels.</summary>
	IReadOnlyList<LevelCatalogEntry> GetLevels();

	/// <summary>Saves a new order of the story and new names of the Sonic 2 levels, and uses them at once. Returns the path of the file.</summary>
	/// <param name="mnemonics">The short names of the levels of the story, in their order.</param>
	/// <param name="titles">The new names of Sonic 2 levels, by short name. A level without a new name has its name of the game.</param>
	/// <param name="zooms">The zooms of Sonic 2 levels that are not 1, by short name.</param>
	string SaveStoryOrder(IReadOnlyList<string> mnemonics, IReadOnlyDictionary<string, string> titles, IReadOnlyDictionary<string, double> zooms = null);

	/// <summary>
	/// Saves the name, the music, the boss and the zoom of a mod level into its INFO file, and uses them at once. Returns the
	/// path of the file.
	/// </summary>
	/// <param name="boss">The key of the boss object, or null for no boss.</param>
	/// <param name="zoom">The zoom at the start of the level. A zoom of 1 removes the attribute.</param>
	string SaveModLevelInfo(string mnemonic, string name, string music, string boss, double zoom = 1.0);

	/// <summary>The bosses that can end a mod level. A boss of an imported zone is in the list only when the zone is imported.</summary>
	IReadOnlyList<LevelBoss> GetBosses();

	/// <summary>The keys of the music that a level can play.</summary>
	IReadOnlyList<string> GetMusic();

	/// <summary>
	/// The zoom at the start of a level: the attribute <c>zoom</c> of the INFO file of a mod level, or of the order of
	/// the levels for a Sonic 2 level. It is 1 when the level has no zoom.
	/// </summary>
	double GetDefaultZoom(string areaResourceKey);

	/// <summary>The name for the title card of a level with a new name, or null to keep the name of the area.</summary>
	string GetTitleCardName(string areaResourceKey);

	/// <summary>
	/// Makes a new level that uses the tile set of another level, and loads it at once. The new level is not in the
	/// story. Throws <see cref="System.InvalidOperationException"/> with a message for the user when the level cannot
	/// be made, for example when the short name is in use.
	/// </summary>
	/// <param name="mnemonic">The short name of the new level: letters and digits.</param>
	/// <param name="name">The name of the new level.</param>
	/// <param name="tilesFrom">The short name of the level with the tile set.</param>
	/// <param name="tileImagePath">
	/// A PNG file with the tiles of the level, 64 x 64 pixels each, or null for the tile set of <paramref name="tilesFrom"/>.
	/// </param>
	/// <returns>The new level.</returns>
	LevelCatalogEntry CreateLevel(string mnemonic, string name, string tilesFrom, string tileImagePath = null);
}

/// <summary>A level in the <see cref="ILevelCatalog"/>.</summary>
/// <param name="Mnemonic">The short name of the level, for example "ehz".</param>
/// <param name="Name">The name of the level, for example "Emerald Hill Zone".</param>
/// <param name="Acts">The number of acts.</param>
/// <param name="IsMod">True when a mod adds the level.</param>
/// <param name="AreaResourceKey">The key of the area of the level, for example "SONICORCA/LEVELS/EHZ/AREA".</param>
/// <param name="StoryNumber">The number of the level in the story, from 1, or 0 when the level is not in the story.</param>
/// <param name="OriginalName">The name of a Sonic 2 level in the game, or null for a mod level.</param>
/// <param name="Music">The key of the music of a mod level, or null. The area of a Sonic 2 level sets its music.</param>
/// <param name="Boss">The key of the boss object at the end of a mod level, or null. The area of a Sonic 2 level sets its boss.</param>
/// <param name="Zoom">The zoom at the start of the level.</param>
public sealed record LevelCatalogEntry(string Mnemonic, string Name, int Acts, bool IsMod, string AreaResourceKey, int StoryNumber,
	string OriginalName = null, string Music = null, string Boss = null, double Zoom = 1.0)
{
	public bool InStory => StoryNumber > 0;
}

/// <summary>A boss that can end a mod level, in the <see cref="ILevelCatalog"/>.</summary>
/// <param name="Name">The name of the zone of the boss, for example "Emerald Hill".</param>
/// <param name="ObjectKey">The key of the boss object.</param>
/// <param name="Arena">The bounds of the arena on a 1920 x 1080 screen, in level units from the marker "boss".</param>
/// <param name="FloorY">The floor of the arena in the zone of the boss, in level units below the marker "boss".</param>
public sealed record LevelBoss(string Name, string ObjectKey, Rectanglei Arena, int FloorY);
