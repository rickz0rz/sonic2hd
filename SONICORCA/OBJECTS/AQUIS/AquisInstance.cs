using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.AQUIS;

/// <summary>
/// Aquis, as in Sonic 2 (Obj50). It waits until it is on the screen. Then it fires a bullet and stops for 33 frames.
/// After the stop it fires again, and chases the nearer player for 128 frames: it accelerates $10 toward the player
/// on each axis, up to $100. It fires only when the player is below it, and only at the start of a chase. After its
/// fourth stop it flies away to the left at $200. Its wing flaps beside its body.
/// </summary>
public class AquisInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int Acceleration = 0x10;

	private const int MaxSpeed = 0x100;

	private const int ChaseTicks = 0x80;

	private const int StopTicks = 0x20;

	private const int Shots = 3;

	private const int FlyAwaySpeed = -0x200;

	/// <summary>Collision size $A: 16 by 8 pixels to each side.</summary>
	private const int TouchWidth = 0x10 * Scale;

	private const int TouchHeight = 8 * Scale;

	/// <summary>The wing is $A pixels behind the body and 6 pixels up.</summary>
	private const int WingX = 0xA;

	private const int WingY = -6;

	/// <summary>The bullet leaves $10 pixels in front of the body and $A pixels up, at $300 forward and $200 down.</summary>
	private const int BulletX = 0x10;

	private const int BulletY = -0xA;

	private const int BulletSpeedX = 0x300;

	private const int BulletSpeedY = 0x200;

	/// <summary>The scripts of Ani_obj50 that the object uses: 0 is the body, 2 the bullet, 3 the wing.</summary>
	private static readonly AnimationScript.Script[] Scripts =
	{
		new AnimationScript.Script(0xE, new[] { 0 }),
		new AnimationScript.Script(5, new[] { 3, 4, 3, 4, 3, 4 }),
		new AnimationScript.Script(3, new[] { 5, 6, 7, 6 }),
		new AnimationScript.Script(3, new[] { 1, 2 })
	};

	private readonly AnimationScript _wingScript = new AnimationScript(Scripts) { Animation = 3, Frame = 1 };

	private AnimationInstance _animation;

	private State _state;

	/// <summary>The timer is a byte, as in the original: $80 counts down through $7F.</summary>
	private sbyte _timer;

	private sbyte _shotsLeft = Shots;

	private bool _shotDone;

	private int _velocityX;

	private int _velocityY;

	private bool _facingRight;

	private enum State
	{
		WaitForScreen,
		Chase,
		Stop,
		FlyAway
	}

	public AquisInstance() => DesignBounds = new Rectanglei(-96, -128, 192, 224);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(AquisType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchWidth, -TouchHeight, TouchWidth * 2, TouchHeight * 2)
		};
	}

	protected override void OnUpdate()
	{
		_wingScript.Step();
		switch (_state)
		{
			case State.WaitForScreen:
				if (Level.Camera.Bounds.Contains((Vector2)Position))
				{
					_state = State.Chase;
				}
				break;
			case State.Chase:
				Chase();
				break;
			case State.Stop:
				WaitForNextShot();
				CheckShoot();
				break;
			case State.FlyAway:
				Move();
				break;
		}
	}

	private void Chase()
	{
		_timer--;
		if (_timer < 0)
		{
			_state = State.Stop;
			_timer = StopTicks;
			_velocityX = 0;
			_velocityY = 0;
			return;
		}
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		_facingRight = orientation.PlayerRight;
		_velocityX = Math.Clamp(_velocityX + (orientation.PlayerRight ? Acceleration : -Acceleration), -MaxSpeed, MaxSpeed);
		_velocityY = Math.Clamp(_velocityY + (orientation.PlayerBelow ? Acceleration : -Acceleration), -MaxSpeed, MaxSpeed);
		Move();
	}

	/// <summary>At the end of a stop, the next chase starts and can fire again, or Aquis flies away after its last shot.</summary>
	private void WaitForNextShot()
	{
		_timer--;
		if (_timer >= 0)
		{
			return;
		}
		_shotsLeft--;
		if (_shotsLeft < 0)
		{
			_state = State.FlyAway;
			_velocityX = FlyAwaySpeed;
			_velocityY = 0;
			return;
		}
		_state = State.Chase;
		_velocityY = -MaxSpeed;
		unchecked
		{
			_timer = (sbyte)ChaseTicks;
		}
		_shotDone = false;
	}

	/// <summary>Fires once after each change from a stop to a chase (and on the first stop), when the player is below.</summary>
	private void CheckShoot()
	{
		if (_shotDone)
		{
			return;
		}
		_shotDone = true;
		if (!PlayerOrientation.Get(Level, Position).PlayerBelow)
		{
			return;
		}
		Bullet bullet = Level.ObjectManager.AddSubObject<Bullet>(this);
		int direction = _facingRight ? 1 : -1;
		bullet.PositionPrecise = PositionPrecise + new Vector2(BulletX * direction * Scale, BulletY * Scale);
		bullet.Velocity = new Vector2(BulletSpeedX * direction * Unit, BulletSpeedY * Unit);
	}

	private void Move() => PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The art faces left. The wing is behind the body and draws in front of it.
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = 0;
		objectRenderer.RenderOriginal(_animation, _facingRight);
		_animation.Index = _wingScript.Frame;
		objectRenderer.RenderOriginal(_animation, new Vector2((_facingRight ? -WingX : WingX) * Scale, WingY * Scale), _facingRight);
	}

	/// <summary>The bullet of Aquis. It flies down at an angle and hurts a character that it touches (collision size $18).</summary>
	private class Bullet : Enemy
	{
		private const int TouchRadius = 4 * Scale;

		private readonly AnimationScript _script = new AnimationScript(Scripts) { Animation = 2, Frame = 5 };

		private AnimationInstance _animation;

		public Vector2 Velocity { get; set; }

		public Bullet() => DesignBounds = new Rectanglei(-64, -64, 128, 128);

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(AquisType.AnimationGroupResourceKey));
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
			};
		}

		protected override void OnUpdate()
		{
			PositionPrecise += Velocity;
			_script.Step();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			_animation.Index = _script.Frame;
			renderer.GetObjectRenderer().Render(_animation);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
