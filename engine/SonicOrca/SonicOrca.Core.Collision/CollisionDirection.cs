using System;

namespace SonicOrca.Core.Collision;

[Flags]
public enum CollisionDirection
{
	None = 0,
	Top = 1,
	Left = 2,
	Bottom = 4,
	Right = 8
}
