namespace SonicOrca.Core;

/// <summary>
/// The high-level state of a level.
/// </summary>
public enum LevelState
{
	Null,
	Playing,
	Dead,
	StageCompleted,
	Editing,
	Restart,
	RestartCheckpoint,
	Quit,
	/// <summary>The player entered the star circle of a starpost: play the special stage, then restart there.</summary>
	SpecialStage
}
