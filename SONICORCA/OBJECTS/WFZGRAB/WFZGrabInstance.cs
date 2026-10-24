using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZGRAB;

/// <summary>
/// An invisible grab bar, as in Sonic 2 (ObjD9 in Wing Fortress Zone). A character that comes less than $18 pixels
/// to the side of the bar and less than $10 pixels below it hangs from it. The character stops, goes to the height
/// of the bar and keeps its horizontal position. A jump lets go with a speed of $300 up. After that, the character
/// cannot hang on the bar again for 18 frames (60 frames when it holds a direction).
/// </summary>
public class WFZGrabInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x18;

	private const int Depth = 0x10;

	private const int ReleaseSpeedY = -0x300;

	private const int ReleaseTicks = 18;

	private const int ReleaseDirectionTicks = 60;

	private readonly Dictionary<ICharacter, int> _waitTicks = new Dictionary<ICharacter, int>();

	private readonly Dictionary<ICharacter, Vector2> _hanging = new Dictionary<ICharacter, Vector2>();

	private readonly HashSet<ICharacter> _jumpPressed = new HashSet<ICharacter>();

	private readonly HashSet<ICharacter> _directionHeld = new HashSet<ICharacter>();

	public WFZGrabInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -4 * Scale, HalfWidth * 2 * Scale, Depth * Scale);

	protected override void OnUpdatePrepare()
	{
		// The character clears its input when an object controls it, so read the buttons before it updates
		_jumpPressed.Clear();
		_directionHeld.Clear();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character is Character c)
			{
				if (c.Input.ABC == CharacterInputButtonState.Pressed)
				{
					_jumpPressed.Add(character);
				}
				if (c.Input.HorizontalDirection != 0 || c.Input.VerticalDirection != 0)
				{
					_directionHeld.Add(character);
				}
			}
		}
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (_hanging.TryGetValue(character, out Vector2 position))
			{
				UpdateHanging(character, position);
			}
			else
			{
				CheckGrab(character);
			}
		}
	}

	private void UpdateHanging(ICharacter character, Vector2 position)
	{
		if (character.IsDebug || character.IsHurt || character.IsDying || character.IsDead)
		{
			Release(character, ReleaseDirectionTicks);
			return;
		}
		// The sidekick of the computer lets go when the main player jumps, as it copies the buttons of the original
		bool jump = _jumpPressed.Contains(character) || (character.IsSidekick && _jumpPressed.Contains(Level.Player.Protagonist));
		if (jump)
		{
			bool direction = _directionHeld.Contains(character) || (character.IsSidekick && _directionHeld.Contains(Level.Player.Protagonist));
			Release(character, direction ? ReleaseDirectionTicks : ReleaseTicks);
			character.Velocity = new Vector2(0.0, ReleaseSpeedY * Scale / 256.0);
			return;
		}
		Hold(character, position);
	}

	private void CheckGrab(ICharacter character)
	{
		if (_waitTicks.TryGetValue(character, out int ticks))
		{
			if (--ticks > 0)
			{
				_waitTicks[character] = ticks;
				return;
			}
			_waitTicks.Remove(character);
		}
		Vector2 offset = (character.PositionPrecise - PositionPrecise) / Scale;
		if (offset.X < -HalfWidth || offset.X >= HalfWidth || offset.Y < 0 || offset.Y >= Depth)
		{
			return;
		}
		if (character.IsObjectControlled || character.IsDebug || character.IsHurt || character.IsDying || character.IsDead)
		{
			return;
		}
		Vector2 position = new Vector2(character.PositionPrecise.X, PositionPrecise.Y);
		_hanging[character] = position;
		character.GroundVelocity = 0.0;
		character.IsSpinball = false;
		character.IsJumping = false;
		character.SpecialState = CharacterSpecialState.Grabbed;
		Hold(character, position);
	}

	private static void Hold(ICharacter character, Vector2 position)
	{
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.PositionPrecise = position;
		character.Velocity = new Vector2();
	}

	private void Release(ICharacter character, int waitTicks)
	{
		_hanging.Remove(character);
		_waitTicks[character] = waitTicks;
		character.IsObjectControlled = false;
		character.SpecialState = CharacterSpecialState.Normal;
		character.LeaveGround();
		character.IsAirborne = true;
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in _hanging.Keys.ToArray())
		{
			Release(character, ReleaseTicks);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
	}
}
