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

	public double NightMode { get; set; }

	public LevelPrepareSettings()
	{
		Lives = 3;
	}
}
