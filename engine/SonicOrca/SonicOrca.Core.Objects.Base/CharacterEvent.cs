using System;

namespace SonicOrca.Core.Objects.Base;

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
