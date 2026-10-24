using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZPLATFORM;

/// <summary>
/// A moving platform of Oil Ocean Zone, as in Sonic 2 (Obj19 with the OOZ art). Subtype bits 4-5 give the size and
/// the frame. Bits 0-3 give the movement: 0 and 1 move to the side with oscillators 2 and 3, 2 moves up and down
/// with oscillator 7, 8 to $F move on a circle with oscillators 14 and 15. Movement 3 waits for a character, then
/// becomes movement 4: the platform speeds up by 8 each frame towards the point $60 pixels above its start, slows
/// down after that point, and stops $C0 pixels above its start. The flipped platform of movement 3 (and movement 7)
/// starts $C0 pixels higher and goes down.
/// </summary>
public class OOZPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int TopHeight = 0x11 * Scale;

	/// <summary>The half width and the mapping frame of each size (Obj19_SubtypeProperties).</summary>
	private static readonly (int HalfWidth, int Frame)[] Sizes = { (0x20, 0), (0x18, 1), (0x40, 2), (0x20, 3) };

	private const int Acceleration = 8;

	private const int TravelHalfDistance = 0x60;

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private int _movement;

	private int _frame;

	/// <summary>The start position (objoff_30 and objoff_32), in original pixels.</summary>
	private Vector2 _origin;

	/// <summary>The position, in original pixels.</summary>
	private Vector2 _position;

	/// <summary>The vertical speed of movements 4 to 7, in 1/256 pixels per frame.</summary>
	private int _velocityY;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The placement is flipped: the movement goes the other way.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public OOZPlatformInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0xD0 * Scale, 0x80 * Scale, 0xE0 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZPlatformType.AnimationGroupResourceKey));
		(int halfWidth, int frame) = Sizes[(_subtype >> 4) & 3];
		_frame = frame;
		_animation.Index = _frame;
		_movement = _subtype & 0xF;
		_origin = PositionPrecise / Scale;
		_position = _origin;
		if ((_movement == 3 && _flipX) || _movement == 7)
		{
			_position.Y -= 0xC0;
		}
		PositionPrecise = _position * Scale;
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-halfWidth * Scale, -TopHeight), new Vector2i(halfWidth * Scale, -TopHeight)) };
	}

	protected override void UpdateMovement()
	{
		Oscillators oscillators = Oscillators.For(Level);
		switch (_movement)
		{
			case 0:
				MoveSideways(oscillators.GetHighByte(Level, 2), 0x40);
				break;
			case 1:
				MoveSideways(oscillators.GetHighByte(Level, 3), 0x60);
				break;
			case 2:
			{
				int offset = oscillators.GetHighByte(Level, 7);
				if (_flipX)
				{
					offset = 0x80 - offset;
				}
				_position.Y = _origin.Y - offset;
				break;
			}
			case 3:
				if (IsCharacterInteractingWithPlatform())
				{
					_movement = 4;
				}
				break;
			case 4:
				// Stop when the speed comes back to 0, at the other end of the travel
				if (Travel() == 0)
				{
					_movement = 5;
				}
				break;
			case 6:
			case 7:
				Travel();
				break;
			case >= 8:
				MoveOnCircle(oscillators);
				break;
		}
		PositionPrecise = _position * Scale;
	}

	/// <summary>Movements 0 and 1: the oscillator gives the distance to the left of the start.</summary>
	private void MoveSideways(int offset, int range)
	{
		if (_flipX)
		{
			offset = range - offset;
		}
		_position.X = _origin.X - offset;
	}

	/// <summary>
	/// Movements 4, 6 and 7: move with the vertical speed, then change the speed by 8 towards the point $60 pixels
	/// above the start. Returns the new speed.
	/// </summary>
	private int Travel()
	{
		_position.Y += _velocityY / 256.0;
		int target = (int)_origin.Y - TravelHalfDistance;
		_velocityY += target >= (int)Math.Floor(_position.Y) ? Acceleration : -Acceleration;
		return _velocityY;
	}

	/// <summary>Movements 8 to $F: a circle of radius $40 with oscillators 14 and 15. Bits 0 and 1 turn it.</summary>
	private void MoveOnCircle(Oscillators oscillators)
	{
		int x = (sbyte)(byte)(oscillators.GetHighByte(Level, 14) - 0x40);
		int y = (sbyte)(byte)(oscillators.GetHighByte(Level, 15) - 0x40);
		if ((_movement & 2) != 0)
		{
			x = -x;
			y = -y;
		}
		if ((_movement & 1) != 0)
		{
			(x, y) = (y, -x);
		}
		if (_movement >= 0xC)
		{
			x = -x;
		}
		_position = new Vector2(_origin.X + x, _origin.Y + y);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = _frame;
		objectRenderer.Render(_animation);
	}
}
