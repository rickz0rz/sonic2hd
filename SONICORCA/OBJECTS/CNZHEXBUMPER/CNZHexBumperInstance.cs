using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZHEXBUMPER;

/// <summary>
/// A hexagonal bumper, as in Sonic 2 (ObjD7). It bounces a character straight away from the side that the character
/// touches, at speed $800. A bounce up or down also moves the character $200 to the side, away from the centre. A
/// moving bumper goes $60 pixels to the left and to the right of its start, one pixel each frame.
/// </summary>
public class CNZHexBumperInstance : ActiveObject
{
	/// <summary>$800 at four times the resolution.</summary>
	private const double BounceSpeed = 32.0;

	/// <summary>$200 at four times the resolution.</summary>
	private const double SideSpeed = 8.0;

	/// <summary>The original collision size $A: 16 by 8 pixels from the centre.</summary>
	private const int TouchWidth = 64;

	private const int TouchHeight = 32;

	/// <summary>$60 pixels at four times the resolution, and one pixel each frame.</summary>
	private const int MoveDistance = 384;

	private const int MoveSpeed = 4;

	private const int BounceFrameTicks = 4;

	private AnimationInstance _animation;

	/// <summary>The squashed frame of the current bounce: 1 after a bounce up or down, 2 after a bounce to the side.</summary>
	private int _bounceFrame;

	private int _bounceTicks = -1;

	private int _startX;

	private bool _movingLeft;

	private bool _moving;

	private bool _startLeft;

	[StateVariable]
	private bool Moving
	{
		get => _moving;
		set => _moving = value;
	}

	/// <summary>A moving bumper starts to the left instead of to the right (the flip of the original).</summary>
	[StateVariable]
	private bool StartLeft
	{
		get => _startLeft;
		set => _startLeft = value;
	}

	public CNZHexBumperInstance() => DesignBounds = new Rectanglei(-96, -64, 192, 128);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZHexBumperType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchWidth, -TouchHeight, TouchWidth * 2, TouchHeight * 2)
		};
		_startX = Position.X;
		_movingLeft = _startLeft;
	}

	protected override void OnUpdate()
	{
		if (_bounceTicks >= 0 && ++_bounceTicks >= 3 * BounceFrameTicks)
		{
			_bounceTicks = -1;
		}
		if (!_moving)
		{
			return;
		}
		int x = Position.X + (_movingLeft ? -MoveSpeed : MoveSpeed);
		if (x == _startX - MoveDistance)
		{
			_movingLeft = false;
		}
		else if (x == _startX + MoveDistance)
		{
			_movingLeft = true;
		}
		Position = new Vector2i(x, Position.Y);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (Character)e.ActiveObject;
		// The direction from the character to the bumper, in the 256 angle units of the original, in four sides
		int dx = Position.X - character.Position.X;
		int dy = Position.Y - character.Position.Y;
		int angle = (int)Math.Round(Math.Atan2(dy, dx) * 128.0 / Math.PI) & 0xFF;
		Vector2 velocity = character.Velocity;
		switch ((angle + 0x20) & 0xC0)
		{
			case 0x00:
				velocity.X = -BounceSpeed;
				_bounceFrame = 2;
				break;
			case 0x80:
				velocity.X = BounceSpeed;
				_bounceFrame = 2;
				break;
			case 0x40:
				// The bumper is below the character: bounce up
				velocity.X += dx < 0 ? SideSpeed : -SideSpeed;
				velocity.Y = -BounceSpeed;
				_bounceFrame = 1;
				break;
			default:
				// The bumper is above the character: bounce down
				velocity.X += dx < 0 ? SideSpeed : -SideSpeed;
				velocity.Y = BounceSpeed;
				_bounceFrame = 1;
				break;
		}
		character.LeaveGround();
		character.IsAirborne = true;
		character.IsJumping = false;
		character.IsRollJumping = false;
		character.IsPushing = false;
		character.Velocity = velocity;
		_bounceTicks = 0;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZHexBumperType.SoundResourceKey));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The bounce animation of the original: the squashed frame, the normal frame, the squashed frame
		_animation.Index = _bounceTicks < 0 || _bounceTicks / BounceFrameTicks == 1 ? 0 : _bounceFrame;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
