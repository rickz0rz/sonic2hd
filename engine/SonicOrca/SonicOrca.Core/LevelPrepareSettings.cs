using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// Settings that the game uses to prepare and start a level.
/// </summary>
public class LevelPrepareSettings
{
	public bool TimeTrial { get; set; }

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

	public bool Debugging { get; set; }

	public double NightMode { get; set; }

	public LevelPrepareSettings()
	{
		Lives = 3;
	}
}
