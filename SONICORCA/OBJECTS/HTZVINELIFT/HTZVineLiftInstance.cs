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

/// <summary>
/// The vine lift from Hill Top Zone. When a character stands on its platform, the lift moves diagonally down for a
/// fixed number of ticks. Then the platform detaches and falls.
/// </summary>
public class HTZVineLiftInstance : ActiveObject
{
	private const int VerticalVinesOutdoorsAnimationIndex = 0;
	private const int VerticalVinesDetachOutdoorsAnimationIndex = 1;
	private const int VerticalVinesInsideAnimationIndex = 5;
	private const int VerticalVinesDetachInsideAnimationIndex = 6;

	/// <summary>The horizontal offset of the platform when the lift goes to the left.</summary>
	private const int LeftPlatformOffsetX = 16;

	private AnimationInstance _animation;
	private Vector2 _velocity;
	private int _duration = 60;
	private LiftDirection _direction;
	private bool _outdoors = true;
	private State _state;
	private LiftPlatform _platform;

	/// <summary>The number of ticks that the lift moves before the platform detaches.</summary>
	[StateVariable]
	private int Duration
	{
		get => _duration;
		set => _duration = value;
	}

	[StateVariable]
	private LiftDirection Direction
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
		if (_direction == LiftDirection.Left)
			_velocity.X *= -1.0;
		Priority = 1256;
		int animationIndex = _outdoors ? VerticalVinesOutdoorsAnimationIndex : VerticalVinesInsideAnimationIndex;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), animationIndex);
		_platform = Level.ObjectManager.AddSubObject<LiftPlatform>(this);
		_platform.Position = Position;
		_platform.LockLifetime = true;
		if (Direction != LiftDirection.Left)
			return;
		_platform.Position = new Vector2i(Position.X - LeftPlatformOffsetX, Position.Y);
	}

	protected override void OnUpdateEditor() => _platform.Position = Position;

	protected override void OnUpdatePrepare()
	{
		switch (_state)
		{
			case State.Waiting:
				if (!_platform.Activated)
					break;
				_state = State.Moving;
				break;
			case State.Moving:
				--_duration;
				if (_duration == 0)
				{
					_state = State.Falling;
					_velocity = new Vector2();
					_platform.Velocity = new Vector2();
					if (Direction == LiftDirection.Left)
						_platform.Position = new Vector2i(_platform.Position.X - LeftPlatformOffsetX, _platform.Position.Y);
					_platform.BeginDetach();
					break;
				}
				_platform.Velocity = _velocity;
				break;
			case State.Falling:
				_animation.Index = _outdoors ? VerticalVinesDetachOutdoorsAnimationIndex : VerticalVinesDetachInsideAnimationIndex;
				break;
		}
	}

	protected override void OnUpdate()
	{
		if (_state != State.Moving)
			return;
		MovePrecise(_velocity);
	}

	protected override void OnStop()
	{
		if (_state == State.Falling)
			return;
		_platform.FinishForever();
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		bool flipX = _direction == LiftDirection.Left;
		objectRenderer.Render(_animation, (Vector2)new Vector2i(-16, -8), flipX);
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

	/// <summary>
	/// The platform that hangs from the vine lift. Characters stand on it. After the lift stops, it bounces a little and
	/// then falls with gravity.
	/// </summary>
	private class LiftPlatform : ActiveObject, IPlatform, IActiveObject
	{
		private const int PlatformAnimationIndex = 4;
		private const double Gravity = 0.875;

		/// <summary>The number of ticks between two vine lift sounds.</summary>
		private const int SoundInterval = 16;

		/// <summary>The Y offsets from the detach position for the first ticks after the platform detaches.</summary>
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
				new CollisionVector(this, new Vector2i(-132, 17), new Vector2i(132, 17), flags: CollisionFlags.Conveyor)
			};
			Priority = 1256;
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), PlatformAnimationIndex);
		}

		protected override void OnUpdatePrepare()
		{
			if (!Falling)
				return;
			if (_fallingTicks < FallElasticYOffsets.Length)
			{
				Vector2i targetPosition = _fallingOriginPosition;
				targetPosition.Y += FallElasticYOffsets[_fallingTicks];
				Velocity = (Vector2)targetPosition - PositionPrecise;
			}
			else
				Velocity += new Vector2(0.0, Gravity);
		}

		protected override void OnUpdate()
		{
			if (!Activated && Level.ObjectManager.IsCharacterStandingOn(CollisionVectors[0]))
				Activated = true;
			if (Falling)
				++_fallingTicks;
			else if (Activated && Level.Ticks % SoundInterval == 0)
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
			// This object draws the platform only from the seventh tick after it detaches.
			if (!Falling || _fallingTicks < 7)
				return;
			renderer.GetObjectRenderer().Render(_animation, (Vector2)new Vector2i(8, 13));
		}
	}
}
