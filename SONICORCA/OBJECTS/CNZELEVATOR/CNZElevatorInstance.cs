using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZELEVATOR;

/// <summary>
/// An elevator, as in Sonic 2 (ObjD5). It waits at one end. When a character stands on it, it travels to the other
/// end: its speed changes by 8/256 of a pixel each frame toward the middle, so it stops at the other end. When no
/// character stands on it, it goes back, and characters cannot stand on it on the way back.
/// </summary>
public class CNZElevatorInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The change of speed each frame, in 1/256 of an original pixel.</summary>
	private const int Acceleration = 8;

	/// <summary>The platform is 16 pixels to each side, and its top is 8 pixels above the centre.</summary>
	private const int HalfWidth = 16 * Scale;

	private const int Top = -8 * Scale;

	private const int FeetTolerance = 8;

	private AnimationInstance _animation;

	private int _distance;

	private bool _startAtBottom;

	private Vector2 _middle;

	private double _offset;

	private int _speed;

	private State _state;

	private CollisionVector[] _surface;

	private enum State
	{
		WaitForCharacter,
		Travel,
		WaitForNoCharacter,
		Return
	}

	/// <summary>The distance from the middle to each end, in original pixels (subtype * 4).</summary>
	[StateVariable]
	private int Distance
	{
		get => _distance;
		set => _distance = value;
	}

	/// <summary>The elevator waits at the bottom instead of the top (the flip of the original).</summary>
	[StateVariable]
	private bool StartAtBottom
	{
		get => _startAtBottom;
		set => _startAtBottom = value;
	}

	public CNZElevatorInstance() => DesignBounds = new Rectanglei(-64, -64, 128, 128);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZElevatorType.AnimationGroupResourceKey));
		DesignBounds = new Rectanglei(-64, -32 - _distance * Scale, 128, 64 + _distance * Scale * 2);
		_middle = (Vector2)Position;
		_offset = (_startAtBottom ? _distance : -_distance) + 0.5;
		PositionPrecise = GetPosition();
		// A platform: only the top is solid
		_surface = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, Top), new Vector2i(HalfWidth, Top)) };
		CollisionVectors = _surface;
	}

	protected override void UpdateMovement()
	{
		switch (_state)
		{
			case State.WaitForCharacter:
				if (IsCharacterOnTop())
				{
					_state = State.Travel;
				}
				break;
			case State.WaitForNoCharacter:
				if (!IsCharacterOnTop())
				{
					_state = State.Return;
					CollisionVectors = new CollisionVector[0];
				}
				break;
			default:
				_speed += _offset < 0.0 ? Acceleration : -Acceleration;
				_offset += _speed / 256.0;
				if (_speed == 0)
				{
					// Stop exactly at the end
					bool atBottom = _offset > 0.0;
					_offset = (atBottom ? _distance : -_distance) + 0.5;
					if (_state == State.Travel)
					{
						_state = State.WaitForNoCharacter;
					}
					else
					{
						_state = State.WaitForCharacter;
						CollisionVectors = _surface;
						RegisterCollisionUpdate();
					}
				}
				break;
		}
		PositionPrecise = GetPosition();
	}

	/// <summary>
	/// True when a character stands on the elevator. At its end the elevator top can be level with the landscape,
	/// and the character then stands on the landscape line. So also check the feet of the characters on the ground.
	/// </summary>
	private bool IsCharacterOnTop()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				return true;
			}
			int feet = character.Position.Y + character.CollisionRadius.Y;
			if (!character.IsAirborne && System.Math.Abs(character.Position.X - Position.X) <= HalfWidth &&
				System.Math.Abs(feet - (Position.Y + Top)) <= FeetTolerance)
			{
				return true;
			}
		}
		return false;
	}

	private Vector2 GetPosition() => _middle + new Vector2(0.0, _offset * Scale);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
