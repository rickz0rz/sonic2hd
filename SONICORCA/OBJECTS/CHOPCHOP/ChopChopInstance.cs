using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CHOPCHOP;

/// <summary>
/// Chop Chop, as in Sonic 2 (Obj91). It swims at $40 and turns each 513 frames, and makes a bubble each 256 frames.
/// When the nearer player is in front of it, 32 to 159 pixels away and at about its height, it stops for 16 frames,
/// then charges at $200 for ever. The charge goes down at $80 when the player was not at its height.
/// </summary>
public class ChopChopInstance : Badnik
{
	private const double Unit = 4.0 / 256.0;

	private const int TurnTicks = 0x200;

	private const int WaitTicks = 0x10;

	private const int TouchWidth = 12 * 4;

	private const int TouchHeight = 20 * 4;

	private const int FrameTicks = 5;

	private AnimationInstance _animation;

	private State _state;

	private bool _startRight;

	private int _velocityX;

	private int _velocityY;

	private int _turnTimer = TurnTicks;

	private int _bubbleTimer;

	private int _waitTimer;

	private int _animationTicks = -1;

	/// <summary>The art faces left. The charge does not turn the fish, as in the original.</summary>
	private bool _facingRight;

	private enum State
	{
		Patrol,
		Wait,
		Charge
	}

	/// <summary>Chop Chop starts to swim to the right (the flip of the original).</summary>
	[StateVariable]
	private bool StartRight
	{
		get => _startRight;
		set => _startRight = value;
	}

	public ChopChopInstance() => DesignBounds = new Rectanglei(-64, -48, 128, 96);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ChopChopType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchWidth, -TouchHeight, TouchWidth * 2, TouchHeight * 2)
		};
		_velocityX = _startRight ? 0x40 : -0x40;
		_facingRight = _startRight;
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Patrol:
				Patrol();
				break;
			case State.Wait:
				_animationTicks++;
				if (--_waitTimer < 0)
				{
					StartCharge();
				}
				break;
			case State.Charge:
				_animationTicks++;
				Move();
				break;
		}
	}

	private void Patrol()
	{
		// The original decrements the high byte of a word here, so a bubble comes each 256 frames
		_bubbleTimer = (_bubbleTimer - 1) & 0xFF;
		if (_bubbleTimer == 0)
		{
			MakeBubble();
		}
		if (--_turnTimer < 0)
		{
			_turnTimer = TurnTicks;
			_velocityX = -_velocityX;
			_facingRight = !_facingRight;
		}
		Move();
		if (SeesPlayer())
		{
			_state = State.Wait;
			_waitTimer = WaitTicks;
			_velocityX = 0;
			_animationTicks = -1;
		}
	}

	private bool SeesPlayer()
	{
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		if ((ushort)(orientation.DeltaY + 0x20) >= 0x40)
		{
			return false;
		}
		int distance = orientation.DeltaX;
		if (distance >= 0)
		{
			if (_velocityX >= 0)
			{
				return false;
			}
		}
		else
		{
			if (_velocityX < 0)
			{
				return false;
			}
			distance = -distance;
		}
		return distance >= 0x20 && distance < 0xA0;
	}

	private void StartCharge()
	{
		_state = State.Charge;
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		_velocityX = orientation.PlayerRight ? 0x200 : -0x200;
		// The original always charges down here, even when the player is above
		if ((ushort)(orientation.DeltaY + 0x10) >= 0x20)
		{
			_velocityY = 0x80;
		}
	}

	private void Move()
	{
		PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);
	}

	private void MakeBubble()
	{
		Vector2i position = Position + new Vector2i((_facingRight ? -0x14 : 0x14) * 4, 6 * 4);
		Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), position, 0);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// Not animated while it swims. Frames 0 and 1, 5 frames each, while it waits and charges. The art faces left.
		_animation.Index = _animationTicks < 0 ? 0 : (_animationTicks / FrameTicks) % 2;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
	}
}
