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
	Quit
}
