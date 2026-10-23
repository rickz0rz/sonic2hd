using System;

namespace SonicOrca.Core.Collision;

/// <summary>
/// Flags that set how a collision vector or object interacts with a character.
/// </summary>
[Flags]
public enum CollisionFlags
{
	Mobile = 1,
	Conveyor = 2,
	Reacts = 4,
	Rotate = 8,
	ForceRoll = 0x10,
	PreventJump = 0x20,
	DontFall = 0x40,
	NoAngle = 0x80,
	NoLanding = 0x100,
	Snap = 0x200,
	NoBalance = 0x400,
	Solid = 0x800,
	NoPathFollowing = 0x1000,
	Ignore = 0x2000,
	NoAutoAlignment = 0x4000
}
