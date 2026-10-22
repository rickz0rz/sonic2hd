using System;

namespace SonicOrca.Core.Objects.Base;

[Flags]
public enum CharacterState
{
	Balancing = 2,
	Pushing = 4,
	Braking = 8,
	Spinball = 0x10,
	Rolling = 0x20,
	Airborne = 0x40,
	Jumping = 0x80,
	Left = 0x100,
	Underwater = 0x200,
	Hurt = 0x400,
	Dying = 0x800,
	Drowning = 0x1000,
	Dead = 0x2000,
	Debug = 0x4000,
	Flying = 0x8000,
	VirtualPlatform = 0x10000,
	SpeedShoes = 0x20000,
	Invincible = 0x40000,
	ForceSpinball = 0x80000,
	Winning = 0x100000,
	Super = 0x200000,
	ChargingSpindash = 0x400000,
	ShouldReactToLevel = 0x800000,
	ObjectControlled = 0x1000000
}
