using System;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// Actions and events of a character during one tick. The character clears each flag when it handles it.
/// </summary>
[Flags]
public enum CharacterEvent
{
	Fall = 1,
	Jump = 2,
	DoubleJump = 4,
	SpindashLaunch = 8,
	Fly = 0x10,
	BarrierAttack = 0x20,
	Hurt = 0x40
}
