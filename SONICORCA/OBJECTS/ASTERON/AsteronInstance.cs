using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ASTERON;

/// <summary>
/// Asteron, as in Sonic 2 (ObjA4). The starfish waits until the nearer player is less than $60 pixels away on the x
/// axis and less than $40 pixels on the y axis. Then, when the player is $10 to $60 pixels away on an axis, it moves
/// toward the player at $40 on that axis (on both axes if both are in range) and flashes. After 65 frames it
/// explodes and throws 5 spikes in a star shape (Obj98). The explosion gives no points and no animal. The spikes
/// hurt.
/// </summary>
public class AsteronInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int Speed = 0x40;

	private const int WakeDistanceX = 0x60;

	private const int WakeDistanceY = 0x40;

	private const int MinDistance = 0x10;

	private const int MaxDistance = 0x60;

	private const int FuseTicks = 0x40;

	/// <summary>Collision size $B: 8 by 8 pixels to each side.</summary>
	private const int TouchRadius = 8 * Scale;

	/// <summary>The spikes: the offset in pixels, the speed in pixels per frame, the mapping frame and the flip.</summary>
	private static readonly (int X, int Y, int VelocityX, int VelocityY, int Frame, bool FlipX)[] Spikes =
	{
		(0, -8, 0, -4, 2, false),
		(8, -4, 3, -1, 3, true),
		(8, 8, 3, 3, 4, true),
		(-8, 8, -3, 3, 4, false),
		(-8, -4, -3, -1, 3, false)
	};

	private static readonly AnimationScript.Script[] Scripts =
	{
		new AnimationScript.Script(1, new[] { 0, 1 })
	};

	private readonly AnimationScript _script = new AnimationScript(Scripts);

	private AnimationInstance _animation;

	private State _state;

	private int _timer;

	private int _velocityX;

	private int _velocityY;

	private enum State
	{
		Wait,
		Aim,
		Fuse
	}

	public AsteronInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, -0x10 * Scale, 0x20 * Scale, 0x20 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(AsteronType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
		_state = State.Wait;
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Wait:
			{
				PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
				if (orientation.Player != null &&
					orientation.DeltaX >= -WakeDistanceX && orientation.DeltaX < WakeDistanceX &&
					orientation.DeltaY >= -WakeDistanceY && orientation.DeltaY < WakeDistanceY)
				{
					_state = State.Aim;
				}
				break;
			}
			case State.Aim:
				Aim();
				break;
			case State.Fuse:
				if (--_timer < 0)
				{
					Explode();
					return;
				}
				PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);
				_script.Step();
				break;
		}
	}

	private void Aim()
	{
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		if (orientation.Player == null)
		{
			return;
		}
		int distanceX = System.Math.Abs(orientation.DeltaX);
		if (distanceX >= MinDistance && distanceX < MaxDistance)
		{
			_velocityX = orientation.PlayerRight ? Speed : -Speed;
			StartFuse();
		}
		int distanceY = System.Math.Abs(orientation.DeltaY);
		if (distanceY >= MinDistance && distanceY < MaxDistance)
		{
			_velocityY = orientation.PlayerBelow ? Speed : -Speed;
			StartFuse();
		}
	}

	private void StartFuse()
	{
		_state = State.Fuse;
		_timer = FuseTicks;
	}

	private void Explode()
	{
		CreateExplosionObject();
		foreach (var data in Spikes)
		{
			Spike spike = Level.ObjectManager.AddSubObject<Spike>(this);
			spike.PositionPrecise = PositionPrecise + new Vector2(data.X * Scale, data.Y * Scale);
			spike.Velocity = new Vector2(data.VelocityX * Scale, data.VelocityY * Scale);
			spike.Frame = data.Frame;
			spike.FlipX = data.FlipX;
		}
		FinishForever();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _script.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}

	/// <summary>
	/// A spike of an Asteron (Obj98 with ObjA4_SubObjData2). It flies straight on until it leaves the screen.
	/// Collision $98: it hurts, 4 by 4 pixels to each side.
	/// </summary>
	private class Spike : Enemy
	{
		private const int TouchRadius = 4 * Scale;

		private AnimationInstance _animation;

		public Vector2 Velocity { get; set; }

		public int Frame { get; set; }

		public bool FlipX { get; set; }

		public Spike() => DesignBounds = new Rectanglei(-32, -32, 64, 64);

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(AsteronType.AnimationGroupResourceKey));
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
			};
		}

		protected override void OnUpdate() => PositionPrecise += Velocity;

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			_animation.Index = Frame;
			renderer.GetObjectRenderer().RenderOriginal(_animation, FlipX);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
