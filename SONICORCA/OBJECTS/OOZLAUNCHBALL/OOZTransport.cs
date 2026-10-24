using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.OOZLAUNCHBALL;

/// <summary>
/// Holds and moves the characters that the launchers of Oil Ocean Zone carry (Obj3D and Obj48). A carried character
/// is a spinball that does not collide with the landscape or with objects. The launcher that carries a character is
/// its object link. A launcher that takes a character from another launcher becomes its object link, so the other
/// launcher lets the character go, as the interact byte does in the original.
/// </summary>
public static class OOZTransport
{
	/// <summary>True when the character can enter a launcher.</summary>
	public static bool CanTake(ICharacter character) =>
		!character.IsDebug && !character.IsDying && !character.IsDead && !character.Respawning;

	public static void Take(ActiveObject launcher, ICharacter character)
	{
		if (!character.IsAirborne)
		{
			character.LeaveGround();
		}
		character.ObjectLink = launcher;
		character.IsObjectControlled = true;
		character.CheckCollision = false;
		character.IsHurt = false;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.IsPushing = false;
	}

	public static bool IsCarriedBy(ActiveObject launcher, ICharacter character) => character.ObjectLink == launcher;

	/// <summary>
	/// Puts the character at a position, and keeps it a spinball. The velocity of the character stays very small,
	/// because the launcher moves it.
	/// </summary>
	public static void Hold(ICharacter character, Vector2 position, Vector2 direction)
	{
		character.PositionPrecise = position;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.Velocity = direction.Length > 0.0 ? direction.Normalised * 0.001 : new Vector2(0.0, 0.001);
	}

	/// <summary>Lets the character go with a velocity in HD pixels for each frame.</summary>
	public static void Release(ICharacter character, Vector2 velocity)
	{
		character.ObjectLink = null;
		character.IsObjectControlled = false;
		character.CheckCollision = true;
		character.IsAirborne = true;
		character.Velocity = velocity;
	}

	/// <summary>Lets the character go and keeps its velocity, for example when it dies or enters debug mode.</summary>
	public static void Drop(ICharacter character)
	{
		character.ObjectLink = null;
		character.IsObjectControlled = false;
		character.CheckCollision = true;
	}

	/// <summary>True when the character is on the screen, as the on_screen render flag of the original.</summary>
	public static bool IsOnScreen(Level level, ICharacter character) => level.Camera.Bounds.Contains((Vector2)character.Position);
}
