using System;

namespace SonicOrca.Core;

/// <summary>
/// Flags that show what a level does in the current frame.
/// </summary>
[Flags]
public enum LevelStateFlags
{
	Animating = 2,
	Updating = 4,
	AllowCharacterControl = 8,
	FadingIn = 0x10,
	FadingOut = 0x20,
	CompletingStage = 0x40,
	Restarting = 0x80,
	UpdateTime = 0x100,
	TitleCardActive = 0x200,
	Editing = 0x400,
	Dead = 0x800,
	StageCompleted = 0x1000,
	TimeOver = 0x2000,
	GameOver = 0x4000,
	TimeUp = 0x8000,
	Paused = 0x10000,
	WaitingForCharacterToWin = 0x20000
}
