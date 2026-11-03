using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// Settings that the game uses to prepare and start a level.
/// </summary>
public class LevelPrepareSettings
{
	public bool TimeTrial { get; set; }

	/// <summary>The physics mode of the level, or null for the mode of the game settings.</summary>
	public PhysicsMode? Physics { get; set; }

	public CharacterType ProtagonistCharacter { get; set; }

	public CharacterType SidekickCharacter { get; set; }

	public string RecordedPlayReadPath { get; set; }

	public string RecordedPlayWritePath { get; set; }

	public string RecordedPlayGhostReadPath { get; set; }

	public byte[] RecordedPlayReadData { get; set; }

	public int LevelNumber { get; set; }

	public string AreaResourceKey { get; set; }

	public int Act { get; set; }

	public Vector2i? StartPosition { get; set; }

	public int StartPath { get; set; }

	public bool Seamless { get; set; }

	/// <summary>
	/// Versus mode: player 2 plays Tails as a second protagonist, with their own rings, score and lives, in place of a
	/// sidekick. See docs/two-player-design.md.
	/// </summary>
	public bool Versus { get; set; }

	/// <summary>The lives of player 2 in versus mode, or null for the lives of player 1.</summary>
	public int? Player2Lives { get; set; }

	/// <summary>In versus mode, each monitor gives the teleport (the "teleport only" items of the original).</summary>
	public bool VersusTeleportOnly { get; set; }

	/// <summary>In versus mode, a zone with two-player object layouts (Casino Night) uses them, as the original does.</summary>
	public bool VersusTwoPlayerLayouts { get; set; }

	public int Lives { get; set; }

	public int Score { get; set; }

	/// <summary>The number of continues. Only the Original physics uses them.</summary>
	public int Continues { get; set; }

	/// <summary>The Chaos Emeralds that the player has, from 0 to 7.</summary>
	public int Emeralds { get; set; }

	/// <summary>The special stage that the next star circle opens, from 0 to 6. It goes up only when an emerald is won.</summary>
	public int SpecialStage { get; set; }

	public bool Debugging { get; set; }

	/// <summary>Open the level editor when the level starts. The game must start with the editor turned on.</summary>
	public bool OpenEditor { get; set; }

	/// <summary>The tool that the level editor selects when it opens with <see cref="OpenEditor"/>, or null for the first tool.</summary>
	public int? EditorToolIndex { get; set; }

	/// <summary>The level editor opens with the form of a new level, for "--editor" without "--level".</summary>
	public bool EditorNewLevelForm { get; set; }

	/// <summary>The name that the form of a new level starts with, for "--level" with a level that does not exist, or null.</summary>
	public string EditorNewLevelName { get; set; }

	/// <summary>The tile image that the form of a new level starts with, for "--tiles", or null.</summary>
	public string EditorNewLevelTileImage { get; set; }

	public double NightMode { get; set; }

	public LevelPrepareSettings()
	{
		Lives = 3;
	}
}
