using System;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// The player that an object of Sonic 2 looks at, and where that player is (Obj_GetOrientationToPlayer): the main
/// player or the sidekick, whichever is nearer on the x axis (the main player on a tie).
/// </summary>
public readonly struct PlayerOrientation
{
	public ICharacter Player { get; }

	/// <summary>The object x minus the player x, in original pixels.</summary>
	public int DeltaX { get; }

	/// <summary>The object y minus the player y, in original pixels.</summary>
	public int DeltaY { get; }

	/// <summary>The player is to the right of the object.</summary>
	public bool PlayerRight => DeltaX < 0;

	/// <summary>The player is below the object.</summary>
	public bool PlayerBelow => DeltaY < 0;

	private PlayerOrientation(ICharacter player, int deltaX, int deltaY)
	{
		Player = player;
		DeltaX = deltaX;
		DeltaY = deltaY;
	}

	public static PlayerOrientation Get(Level level, Vector2i objectPosition)
	{
		const int scale = 4;
		ICharacter main = level.Player.Protagonist;
		ICharacter target = main;
		ICharacter sidekick = level.Player.Sidekick;
		if (sidekick != null && main != null && Math.Abs(objectPosition.X - sidekick.Position.X) < Math.Abs(objectPosition.X - main.Position.X))
		{
			target = sidekick;
		}
		if (target == null)
		{
			return new PlayerOrientation(null, 0, 0);
		}
		return new PlayerOrientation(target, (objectPosition.X - target.Position.X) / scale, (objectPosition.Y - target.Position.Y) / scale);
	}
}
