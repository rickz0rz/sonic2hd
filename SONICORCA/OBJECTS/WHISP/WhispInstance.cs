using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WHISP;

/// <summary>
/// Whisp, as in Sonic 2 (Obj8C). When it comes on screen, it chases the nearer player 4 times: each chase starts
/// with a hop up, accelerates $10 toward the player on each axis up to $200, and lasts 96 frames, then a random pause
/// of 0 to 31 frames. After the fourth chase it flies away up and to the left.
/// </summary>
public class WhispInstance : Badnik
{
	/// <summary>Speeds in 1/256 original pixels for each frame, at four times the resolution.</summary>
	private const double Unit = 4.0 / 256.0;

	private const int Attacks = 4;

	private const int ChaseTicks = 96;

	private const int TouchRadius = 8 * 4;

	private const int FrameTicks = 2;

	private AnimationInstance _animation;

	private State _state;

	private int _timer;

	private int _attacksLeft = Attacks;

	private int _velocityX;

	private int _velocityY;

	private bool _facingRight;

	private int _ticks;

	private enum State
	{
		WaitForScreen,
		Chase,
		Pause,
		FlyAway
	}

	public WhispInstance() => DesignBounds = new Rectanglei(-48, -32, 96, 64);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WhispType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
	}

	protected override void OnUpdate()
	{
		_ticks++;
		switch (_state)
		{
			case State.WaitForScreen:
				if (Level.Camera.Bounds.Contains((Vector2)Position))
				{
					StartAttack();
				}
				break;
			case State.Pause:
				if (--_timer < 0)
				{
					StartAttack();
				}
				break;
			case State.Chase:
				Chase();
				break;
			case State.FlyAway:
				Move();
				break;
		}
	}

	private void StartAttack()
	{
		if (--_attacksLeft < 0)
		{
			_state = State.FlyAway;
			_facingRight = false;
			_velocityX = -0x200;
			_velocityY = -0x200;
			Move();
			return;
		}
		_state = State.Chase;
		_velocityY = -0x100;
		_timer = ChaseTicks;
		Chase();
	}

	private void Chase()
	{
		if (--_timer < 0)
		{
			_state = State.Pause;
			_timer = Level.Random.Next(32);
			_velocityX = 0;
			_velocityY = 0;
			return;
		}
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		_facingRight = orientation.PlayerRight;
		_velocityX = Math.Clamp(_velocityX + (orientation.PlayerRight ? 0x10 : -0x10), -0x200, 0x200);
		_velocityY = Math.Clamp(_velocityY + (orientation.PlayerBelow ? 0x10 : -0x10), -0x200, 0x200);
		Move();
	}

	private void Move()
	{
		PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The wings flap: frames 0 and 1, 2 frames each. The art faces left.
		_animation.Index = (_ticks / FrameTicks) % 2;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
	}
}
