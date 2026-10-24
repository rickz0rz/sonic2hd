using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZLAUNCHER;

/// <summary>
/// A speed launcher (the catapult), as in Sonic 2 (ObjC0). It is a platform $10 pixels to each side. When a character
/// stands on it, it shoots to the left (to the right when flipped) at speed $C00, and the speed increases by $80 each
/// frame. The characters on it stay at its centre and cannot walk. When it has gone subtype * $10 pixels, it stops and
/// throws the characters that are still on it with its speed to the side and $400 up. Then it goes back to its start
/// at 4 pixels each frame.
/// </summary>
public class WFZLauncherInstance : Platform
{
	private const int Scale = 4;

	private const int HalfWidth = 0x10;

	private const int TopHeight = 0x11;

	private const int LaunchSpeed = 0xC00;

	private const int Acceleration = 0x80;

	private const int ThrowSpeedY = -0x400;

	private const int ReturnSpeed = 4;

	private AnimationInstance _animation;

	private int _distance;

	private bool _flipX;

	private State _state;

	/// <summary>The start (objoff_34) and the end (objoff_32) of the travel, in original pixels.</summary>
	private int _homeX;

	private int _targetX;

	/// <summary>The position in original pixels.</summary>
	private double _x;

	/// <summary>The speed and the acceleration, in 1/256 pixels per frame.</summary>
	private int _velocityX;

	private int _acceleration;

	private enum State
	{
		Waiting,
		Shooting,
		Returning
	}

	/// <summary>The travel of the launcher, in units of $10 pixels (the subtype of the original).</summary>
	[StateVariable]
	private int Distance
	{
		get => _distance;
		set => _distance = value;
	}

	/// <summary>The launcher shoots to the right.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public WFZLauncherInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -0x20 * Scale, HalfWidth * 2 * Scale, 0x20 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZLauncherType.AnimationGroupResourceKey));
		_x = PositionPrecise.X / Scale;
		_homeX = (int)_x;
		_targetX = _homeX + (_flipX ? _distance << 4 : -(_distance << 4));
		_state = State.Waiting;
		// The launcher carries the characters on it (Conveyor), as PlatformObject in the original
		CollisionVectors = new[]
		{
			new CollisionVector(this, new Vector2i(-HalfWidth * Scale, -TopHeight * Scale), new Vector2i(HalfWidth * Scale, -TopHeight * Scale), flags: CollisionFlags.Conveyor)
		};
	}

	protected override void UpdateMovement()
	{
		switch (_state)
		{
			case State.Waiting:
				if (!IsCharacterInteractingWithPlatform())
				{
					break;
				}
				_state = State.Shooting;
				_velocityX = _flipX ? LaunchSpeed : -LaunchSpeed;
				_acceleration = _flipX ? Acceleration : -Acceleration;
				HoldCharacters();
				Move();
				break;
			case State.Shooting:
				_velocityX += _acceleration;
				Move();
				int remaining = _flipX ? (int)_x - _targetX : _targetX - (int)_x;
				// The launcher has not reached the end while it is before the end on its way
				if (remaining < 0)
				{
					HoldCharacters();
					break;
				}
				_state = State.Returning;
				_x = _targetX;
				ThrowCharacters();
				Return();
				break;
			case State.Returning:
				Return();
				break;
		}
		PositionPrecise = new Vector2(_x * Scale, PositionPrecise.Y);
	}

	private void Move() => _x += _velocityX / 256.0;

	/// <summary>The launcher goes back to its start at 4 pixels each frame.</summary>
	private void Return()
	{
		double x = _x + (_flipX ? -ReturnSpeed : ReturnSpeed);
		bool reached = _flipX ? x < _homeX : x >= _homeX;
		if (reached)
		{
			_state = State.Waiting;
			x = _homeX;
		}
		_x = x;
	}

	/// <summary>The characters on the launcher stop at its centre and face the way that it shoots.</summary>
	private void HoldCharacters()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters.Where(c => c.ObjectLink == this))
		{
			character.GroundVelocity = 0.0;
			character.Velocity = new Vector2(0.0, character.Velocity.Y);
			character.PositionPrecise = new Vector2(_x * Scale, character.PositionPrecise.Y);
			character.Facing = _flipX ? 1 : -1;
		}
	}

	private void ThrowCharacters()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters.Where(c => c.ObjectLink == this).ToArray())
		{
			character.LeaveGround();
			character.IsAirborne = true;
			character.Velocity = new Vector2(_velocityX * Scale / 256.0, ThrowSpeedY * Scale / 256.0);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = 0;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
