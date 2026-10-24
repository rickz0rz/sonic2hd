using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OCTUS;

/// <summary>
/// Octus, as in Sonic 2 (Obj4A). It falls to the floor with gravity $38 and turns once if the main player is to its
/// right. When the main player is within $80 pixels on the x axis, it waits 33 frames, then jumps at -$200 with
/// gravity $10. At the top of the jump it fires a bullet at $200 to the front, hovers for 61 frames, and then falls
/// back to its start height.
/// </summary>
public class OctusInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int FallGravity = 0x38;

	private const int JumpGravity = 0x10;

	private const int JumpSpeed = -0x200;

	private const int BulletSpeed = 0x200;

	private const int TriggerDistance = 0x80;

	private const int DelayTicks = 0x20;

	private const int HoverTicks = 60;

	private const int YRadius = 0xB;

	/// <summary>Collision size $A: 16 by 8 pixels to each side.</summary>
	private const int TouchWidth = 0x10 * Scale;

	private const int TouchHeight = 8 * Scale;

	/// <summary>The scripts of Ani_obj4A: 0 waits, 1 swims, 2 is the bullet, 3 crouches, 4 starts the jump.</summary>
	private static readonly AnimationScript.Script[] Scripts =
	{
		new AnimationScript.Script(0xF, new[] { 1, 0 }),
		new AnimationScript.Script(3, new[] { 1, 2, 3 }),
		new AnimationScript.Script(2, new[] { 5, 6 }),
		new AnimationScript.Script(0xF, new[] { 4 }),
		new AnimationScript.Script(7, new[] { 0, 1 }, 1)
	};

	private readonly AnimationScript _script = new AnimationScript(Scripts);

	private AnimationInstance _animation;

	private bool _flipX;

	private bool _facingRight;

	private State _state;

	private int _timer;

	private int _velocityY;

	private double _startY;

	private enum State
	{
		Fall,
		Wait,
		Delay,
		Jump,
		Hover,
		Drop
	}

	/// <summary>Octus faces right at the start (the flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public OctusInstance() => DesignBounds = new Rectanglei(-96, -96, 192, 192);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OctusType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchWidth, -TouchHeight, TouchWidth * 2, TouchHeight * 2)
		};
		_facingRight = _flipX;
		_state = State.Fall;
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Fall:
				Fall();
				return;
			case State.Wait:
				ICharacter player = Level.Player.Protagonist;
				if (player != null && Math.Abs(Position.X - player.Position.X) <= TriggerDistance * Scale)
				{
					_state = State.Delay;
					_script.Animation = 3;
					_timer = DelayTicks;
				}
				break;
			case State.Delay:
				if (--_timer < 0)
				{
					_state = State.Jump;
					_script.Animation = 4;
					_velocityY = JumpSpeed;
					Move();
				}
				break;
			case State.Jump:
				_velocityY += JumpGravity;
				if (_velocityY >= 0)
				{
					_state = State.Hover;
					_timer = HoverTicks;
					FireBullet();
				}
				else
				{
					Move();
				}
				break;
			case State.Hover:
				if (--_timer < 0)
				{
					_state = State.Drop;
				}
				break;
			case State.Drop:
				_velocityY += JumpGravity;
				if (PositionPrecise.Y >= _startY)
				{
					_state = State.Wait;
					_script.Animation = 0;
					_script.Frame = 1;
					_velocityY = 0;
				}
				else
				{
					Move();
				}
				break;
		}
		_script.Step();
	}

	/// <summary>Falls until the bottom is in the floor, then stands on it and turns toward the main player.</summary>
	private void Fall()
	{
		PositionPrecise += new Vector2(0.0, _velocityY * Unit);
		_velocityY += FallGravity;
		int? floor = LandscapeFloor.GetDistance(Level, Position + new Vector2i(0, YRadius * Scale));
		if (floor < 0)
		{
			PositionPrecise += new Vector2(0.0, floor.Value * Scale);
			_velocityY = 0;
			_state = State.Wait;
			ICharacter player = Level.Player.Protagonist;
			if (player != null && Position.X < player.Position.X)
			{
				_facingRight = !_facingRight;
			}
		}
		_startY = PositionPrecise.Y;
	}

	private void Move() => PositionPrecise += new Vector2(0.0, _velocityY * Unit);

	private void FireBullet()
	{
		Bullet bullet = Level.ObjectManager.AddSubObject<Bullet>(this);
		bullet.PositionPrecise = PositionPrecise;
		bullet.VelocityX = (_facingRight ? BulletSpeed : -BulletSpeed) * Unit;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The art faces left
		_animation.Index = _script.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
	}

	/// <summary>
	/// The bullet of Octus. It waits 16 frames at the place where Octus fired it, then flies straight on. It hurts a
	/// character that it touches (collision size $18: 4 pixels to each side).
	/// </summary>
	private class Bullet : Enemy
	{
		private const int WaitTicks = 0xF;

		private const int TouchRadius = 4 * Scale;

		private readonly AnimationScript _script = new AnimationScript(Scripts) { Animation = 2 };

		private AnimationInstance _animation;

		private int _timer = WaitTicks;

		public double VelocityX { get; set; }

		public Bullet() => DesignBounds = new Rectanglei(-80, -64, 160, 128);

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OctusType.AnimationGroupResourceKey));
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
			};
		}

		protected override void OnUpdate()
		{
			if (--_timer >= 0)
			{
				return;
			}
			PositionPrecise += new Vector2(VelocityX, 0.0);
			_script.Step();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			// The original does not draw the bullet while it waits
			if (_timer >= 0)
			{
				return;
			}
			_animation.Index = _script.Frame;
			renderer.GetObjectRenderer().RenderOriginal(_animation, VelocityX > 0.0);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
