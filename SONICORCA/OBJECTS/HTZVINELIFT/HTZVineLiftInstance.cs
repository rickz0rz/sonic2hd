// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZVINELIFT.HTZVineLiftInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.HTZVINELIFT;

public class HTZVineLiftInstance : ActiveObject
{
	private const int VerticalVinesOutdoorsAnimationIndex = 0;
	private const int VerticalVinesDetachOutdoorsAnimationIndex = 1;
	private const int VerticalVinesInsideAnimationIndex = 5;
	private const int VerticalVinesDetachInsideAnimationIndex = 6;
	private AnimationInstance _animation;
	private Vector2 _velocity;
	private int _duration = 60;
	private HTZVineLiftInstance.LiftDirection _direction;
	private bool _outdoors = true;
	private HTZVineLiftInstance.State _state;
	private HTZVineLiftInstance.LiftPlatform _platform;

	[StateVariable]
	private int Duration
	{
		get => _duration;
		set => _duration = value;
	}

	[StateVariable]
	private HTZVineLiftInstance.LiftDirection Direction
	{
		get => _direction;
		set => _direction = value;
	}

	[StateVariable]
	private bool Outdoors
	{
		get => _outdoors;
		set => _outdoors = value;
	}

	public HTZVineLiftInstance() => DesignBounds = new Rectanglei(-132, -41, 264, 82);

	protected override void OnStart()
	{
		base.OnStart();
		_velocity = new Vector2(8.0, 4.0);
		if (_direction == HTZVineLiftInstance.LiftDirection.Left)
			_velocity.X *= -1.0;
		Priority = 1256;
		int index = 5;
		if (_outdoors)
			index = 0;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), index);
		_platform = Level.ObjectManager.AddSubObject<HTZVineLiftInstance.LiftPlatform>(this);
		_platform.Position = Position;
		_platform.LockLifetime = true;
		if (Direction != HTZVineLiftInstance.LiftDirection.Left)
			return;
		_platform.Position = new Vector2i(Position.X - 16 /*0x10*/, Position.Y);
	}

	protected override void OnUpdateEditor() => _platform.Position = Position;

	protected override void OnUpdatePrepare()
	{
		switch (_state)
		{
			case HTZVineLiftInstance.State.Waiting:
				if (!_platform.Activated)
					break;
				_state = HTZVineLiftInstance.State.Moving;
				break;
			case HTZVineLiftInstance.State.Moving:
				--_duration;
				if (_duration == 0)
				{
					_state = HTZVineLiftInstance.State.Falling;
					_velocity = new Vector2();
					_platform.Velocity = new Vector2();
					if (Direction == HTZVineLiftInstance.LiftDirection.Left)
						_platform.Position = new Vector2i(_platform.Position.X - 16 /*0x10*/, _platform.Position.Y);
					_platform.BeginDetach();
					break;
				}
				_platform.Velocity = _velocity;
				break;
			case HTZVineLiftInstance.State.Falling:
				int num = 6;
				if (_outdoors)
					num = 1;
				_animation.Index = num;
				break;
		}
	}

	protected override void OnUpdate()
	{
		if (_state != HTZVineLiftInstance.State.Moving)
			return;
		MovePrecise(_velocity);
	}

	protected override void OnStop()
	{
		if (_state == HTZVineLiftInstance.State.Falling)
			return;
		_platform.FinishForever();
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		bool flag = _direction == HTZVineLiftInstance.LiftDirection.Left;
		AnimationInstance animation = _animation;
		Vector2 destination = (Vector2)new Vector2i(-16, -8);
		int num = flag ? 1 : 0;
		objectRenderer.Render(animation, destination, num != 0);
	}

	private enum State
	{
		Waiting,
		Moving,
		Falling,
	}

	private enum LiftDirection
	{
		Left,
		Right,
	}

	private class LiftPlatform : ActiveObject, IPlatform, IActiveObject
	{
		private const int PlatformAnimationIndex = 4;
		private const double Gravity = 0.875;
		private static readonly int[] FallElasticYOffsets = new int[6]
		{
	  0,
	  20,
	  40,
	  40,
	  60,
	  60
		};
		private AnimationInstance _animation;
		private int _fallingTicks;
		private Vector2i _fallingOriginPosition;

		public bool Activated { get; set; }

		public bool Falling { get; set; }

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			CollisionVectors = new CollisionVector[1]
			{
		new CollisionVector( this, new Vector2i(-132, 17), new Vector2i(132, 17), flags: CollisionFlags.Conveyor)
			};
			Priority = 1256;
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 4);
		}

		protected override void OnUpdatePrepare()
		{
			if (!Falling)
				return;
			if (_fallingTicks < 6)
			{
				int fallElasticYoffset = HTZVineLiftInstance.LiftPlatform.FallElasticYOffsets[_fallingTicks];
				Vector2i fallingOriginPosition = _fallingOriginPosition;
				fallingOriginPosition.Y += fallElasticYoffset;
				Velocity = (Vector2)fallingOriginPosition - PositionPrecise;
			}
			else
				Velocity += new Vector2(0.0, 0.875);
		}

		protected override void OnUpdate()
		{
			if (!Activated && Level.ObjectManager.IsCharacterStandingOn(CollisionVectors[0]))
				Activated = true;
			if (Falling)
				++_fallingTicks;
			else if (Activated && Level.Ticks % 16 /*0x10*/ == 0)
				Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/VINELIFT"));
			MovePrecise(Velocity);
		}

		protected override void OnStop()
		{
			if (!Falling)
				return;
			FinishForever();
		}

		protected override void OnAnimate() => _animation.Animate();

		public void BeginDetach()
		{
			Falling = true;
			LockLifetime = false;
			_fallingOriginPosition = Position;
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (!Falling || _fallingTicks < 7)
				return;
			renderer.GetObjectRenderer().Render(_animation, (Vector2)new Vector2i(8, 13));
		}
	}
}
