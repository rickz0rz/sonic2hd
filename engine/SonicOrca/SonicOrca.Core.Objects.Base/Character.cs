using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core.Objects.Base;

public abstract class Character : ActiveObject, ICharacter, IActiveObject
{
	private enum CollisionBox
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	public class Intersector
	{
		private static double MyEpsilon = 1E-05;

		private static double[] OverlapIntervals(double ub1, double ub2)
		{
			double val = Math.Min(ub1, ub2);
			double val2 = Math.Max(ub1, ub2);
			double num = Math.Max(0.0, val);
			double num2 = Math.Min(1.0, val2);
			if (num > num2)
			{
				return new double[0];
			}
			if (num != num2)
			{
				return new double[2] { num, num2 };
			}
			return new double[1] { num };
		}

		private static Vector2[] OneD_Intersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			double num = a2.X - a1.X;
			double num2 = a2.Y - a1.Y;
			double ub;
			double ub2;
			if (Math.Abs(num) > Math.Abs(num2))
			{
				ub = (b1.X - a1.X) / num;
				ub2 = (b2.X - a1.X) / num;
			}
			else
			{
				ub = (b1.Y - a1.Y) / num2;
				ub2 = (b2.Y - a1.Y) / num2;
			}
			List<Vector2> list = new List<Vector2>();
			double[] array = OverlapIntervals(ub, ub2);
			for (int i = 0; i < array.Length; i++)
			{
				float num3 = (float)array[i];
				double x = a2.X * (double)num3 + a1.X * (double)(1f - num3);
				double y = a2.Y * (double)num3 + a1.Y * (double)(1f - num3);
				Vector2 item = new Vector2(x, y);
				list.Add(item);
			}
			return list.ToArray();
		}

		private static bool PointOnLine(Vector2 p, Vector2 a1, Vector2 a2)
		{
			double u = 0.0;
			return DistFromSeg(p, a1, a2, MyEpsilon, ref u) < MyEpsilon;
		}

		private static double DistFromSeg(Vector2 p, Vector2 q0, Vector2 q1, double radius, ref double u)
		{
			double num = q1.X - q0.X;
			double num2 = q1.Y - q0.Y;
			double num3 = q0.X - p.X;
			double num4 = q0.Y - p.Y;
			double num5 = Math.Sqrt(num * num + num2 * num2);
			if (num5 < MyEpsilon)
			{
				throw new Exception("Expected line segment, not point.");
			}
			return Math.Abs(num * num4 - num3 * num2) / num5;
		}

		public static Vector2[] Intersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			if (a1.Equals(a2) && b1.Equals(b2))
			{
				if (a1.Equals(b1))
				{
					return new Vector2[1] { a1 };
				}
				return new Vector2[0];
			}
			if (b1.Equals(b2))
			{
				if (PointOnLine(b1, a1, a2))
				{
					return new Vector2[1] { b1 };
				}
				return new Vector2[0];
			}
			if (a1.Equals(a2))
			{
				if (PointOnLine(a1, b1, b2))
				{
					return new Vector2[1] { a1 };
				}
				return new Vector2[0];
			}
			double num = (b2.X - b1.X) * (a1.Y - b1.Y) - (b2.Y - b1.Y) * (a1.X - b1.X);
			double num2 = (a2.X - a1.X) * (a1.Y - b1.Y) - (a2.Y - a1.Y) * (a1.X - b1.X);
			double num3 = (b2.Y - b1.Y) * (a2.X - a1.X) - (b2.X - b1.X) * (a2.Y - a1.Y);
			if (!(0.0 - MyEpsilon < num3) || !(num3 < MyEpsilon))
			{
				double num4 = num / num3;
				double num5 = num2 / num3;
				if (0.0 <= num4 && num4 <= 1.0 && 0.0 <= num5 && num5 <= 1.0)
				{
					return new Vector2[1]
					{
						new Vector2(a1.X + num4 * (a2.X - a1.X), a1.Y + num4 * (a2.Y - a1.Y))
					};
				}
				return new Vector2[0];
			}
			if ((0.0 - MyEpsilon < num && num < MyEpsilon) || (0.0 - MyEpsilon < num2 && num2 < MyEpsilon))
			{
				if (a1.Equals(a2))
				{
					return OneD_Intersection(b1, b2, a1, a2);
				}
				return OneD_Intersection(a1, a2, b1, b2);
			}
			return new Vector2[0];
		}
	}

	private class InvincibilityParticle
	{
		private readonly Character _character;

		private readonly AnimationInstance _animation;

		private double _angle;

		private Vector2 _position;

		public Vector2 Origin { get; set; }

		public InvincibilityParticle(Character character, double initialAngle, AnimationGroup animationGroup, int animationIndex)
		{
			_character = character;
			_angle = initialAngle;
			_animation = new AnimationInstance(animationGroup);
			_animation.Index = animationIndex;
			_animation.CurrentFrameIndex = _character.Level.Random.Next(animationGroup[animationIndex].Frames.Count);
		}

		public void Animate()
		{
			_angle = MathX.WrapRadians(_angle + Math.PI / 16.0);
			_position.X = Math.Cos(_angle) * 46.0 + _character.Level.Random.Next(-8, 8);
			_position.Y = Math.Sin(_angle) * 64.0 + _character.Level.Random.Next(-8, 8);
			_animation.Animate();
		}

		public void Draw(Renderer renderer)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.BlendMode = BlendMode.Additive;
			_ = objectRenderer.Filter;
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation, Origin - _character.Position + _position);
			objectRenderer.EmitsLight = false;
		}
	}

	private enum AutoSidekickState
	{
		Spawning,
		Flying,
		Normal,
		Spindash
	}

	private BarrierType _barrierTypeAnimation;

	public const double TumbleSpeed = 3.0 / 128.0;

	private const int KeepSpinballAirborneTolerance = 11;

	private readonly bool[] _collisionDetected = new bool[4];

	private readonly CollisionEvent[] _collisionEvents = new CollisionEvent[4];

	private readonly bool[] _collisionDetectedTiles = new bool[4];

	private readonly CollisionEvent[] _collisionEventsTiles = new CollisionEvent[4];

	private readonly List<KeyValuePair<ActiveObject, CollisionEvent>> _collidedVectors = new List<KeyValuePair<ActiveObject, CollisionEvent>>();

	private CollisionVector _originalGroundVector;

	private CollisionVector _overlappingGroundVector;

	private KeyValuePair<ActiveObject, CollisionEvent> _overlappingCollision;

	private Vector2 _lastPositionAdjusted;

	private CollisionVector _lastLeftVector;

	private CollisionVector _lastRightVector;

	private const string DefaultBarrierClassicResourceKey = "SONICORCA/OBJECTS/BARRIER/ANIGROUP";

	private const string DefaultBarrierBubbleResourceKey = "SONICORCA/OBJECTS/BARRIER/BUBBLE/ANIGROUP";

	private const string DefaultBarrierFireResourceKey = "SONICORCA/OBJECTS/BARRIER/FIRE/ANIGROUP";

	private const string DefaultBarrierLightningResourceKey = "SONICORCA/OBJECTS/BARRIER/LIGHTNING/ANIGROUP";

	private const string DefaultSpindashDustResourceKey = "SONICORCA/OBJECTS/SPINDASH/ANIGROUP";

	private const string DefaultBrakeDustResourceKey = "SONICORCA/OBJECTS/DUST";

	private const string DefaultInvincibilityResourceKey = "SONICORCA/OBJECTS/INVINCIBILITY/ANIGROUP";

	private const int MaximumVelocity = 96;

	private readonly CharacterHistoryItem[] _history = new CharacterHistoryItem[32];

	private CharacterBalanceDirection _balanceDirection;

	private int _brakeDustDelay;

	private int _brakeTicks;

	private int _pushDirection;

	private int _pushTicks;

	private double _spindashExtraSpeed;

	private Vector2 _intersection;

	private int _dyingTicks;

	private PlayerDeathCause _deathCause;

	private static readonly IReadOnlyList<Vector2> ScatterRingOffsets = new Vector2[32]
	{
		new Vector2(-3.12, -15.69),
		new Vector2(3.12, -15.69),
		new Vector2(-8.89, -13.3),
		new Vector2(8.89, -13.3),
		new Vector2(-13.3, -8.89),
		new Vector2(13.3, -8.89),
		new Vector2(-15.69, -3.12),
		new Vector2(15.69, -3.12),
		new Vector2(-15.69, 3.12),
		new Vector2(15.69, 3.12),
		new Vector2(-13.3, 8.89),
		new Vector2(13.3, 8.89),
		new Vector2(-8.89, 13.3),
		new Vector2(8.89, 13.3),
		new Vector2(-3.12, 15.69),
		new Vector2(3.12, 15.69),
		new Vector2(-1.56, -7.85),
		new Vector2(1.56, -7.85),
		new Vector2(-4.44, -6.65),
		new Vector2(4.44, -6.65),
		new Vector2(-6.65, -4.44),
		new Vector2(6.65, -4.44),
		new Vector2(-7.85, -1.56),
		new Vector2(7.85, -1.56),
		new Vector2(-7.85, 1.56),
		new Vector2(7.85, 1.56),
		new Vector2(-6.65, 4.44),
		new Vector2(6.65, 4.44),
		new Vector2(-4.44, 6.65),
		new Vector2(4.44, 6.65),
		new Vector2(-1.56, 7.85),
		new Vector2(1.56, 7.85)
	};

	private readonly List<InvincibilityParticle> _invincibilityParticles = new List<InvincibilityParticle>();

	private readonly List<Vector2> _invincibilityCharacterPositionHistory = new List<Vector2>();

	private const int InvincibilityParticlesPerPositionHistory = 3;

	private const int InvincibilityPositionHistoryChunks = 4;

	private const int InvincibilityPositionHistoryDelay = 2;

	public const int NoHumanInputTime = 600;

	public const int SidekickOffscreenTime = 300;

	private bool _humanControlled;

	private int _humanInputTicksRemaining;

	private AutoSidekickState _autoSidekickState;

	private int _sidekickOffscreenTicks;

	private Vector2i _sidekickTargetPosition;

	private bool _autoSidekickJumping;

	private const string DrowningCountDownResourceKey = "SONICORCA/HUD/DROWNING/ANIGROUP";

	private const string InhaleSoundResourceKey = "SONICORCA/SOUND/INHALE";

	private const string SplashSoundResourceKey = "SONICORCA/SOUND/SPLASH";

	private const string DrownWarningSoundResourceKey = "SONICORCA/SOUND/DROWNWARNING";

	private const string DrownSoundResourceKey = "SONICORCA/SOUND/DROWN";

	private static readonly IReadOnlyCollection<int> BreathLeftWarnings = new int[3] { 1500, 1200, 900 };

	private const int BreathLeftDrowningMusic = 720;

	private const int MaximumBreath = 1800;

	private AnimationInstance _drowningAnimation;

	private int _nextBubbleTime;

	private bool _drowningClimax;

	private int _inhalingBubble;

	private Vector2i _drownCountdownPosition;

	private int _drownCountdownValue;

	protected string AnimationGroupResourceKey { get; set; }

	protected string BrakeDustResourceKey { get; set; }

	protected string SpindashDustGroupResourceKey { get; set; }

	public AnimationInstance Animation { get; set; }

	protected AnimationInstance BarrierAnimation { get; set; }

	protected AnimationInstance SpindashDustAnimation { get; set; }

	public double ShowAngle { get; set; }

	public double TumbleAngle { get; set; }

	public int TumbleTurns { get; set; }

	protected bool DrawBodyRotated { get; set; }

	public int LedgeSensorRadius { get; set; }

	public Vector2i NormalCollisionRadius { get; set; }

	public Vector2i SpinballCollisionRadius { get; set; }

	public Vector2i RectangleCollisionRadius { get; set; }

	public Vector2i CollisionRadius { get; set; }

	public int FloorSensorRadius { get; set; }

	public int CollisionSensorSize { get; set; }

	public bool CheckCollision { get; set; }

	public bool CheckLandscapeCollision { get; set; }

	public bool CheckObjectCollision { get; set; }

	public int Path { get; set; }

	public int LastPath { get; set; }

	public CollisionMode Mode { get; set; }

	public CollisionVector GroundVector { get; set; }

	private double GroundAngle
	{
		get
		{
			if (GroundVector != null)
			{
				return GroundVector.Angle;
			}
			return 0.0;
		}
	}

	public double DistanceFromLedge { get; set; }

	public ActiveObject ObjectLink { get; set; }

	public object ObjectTag { get; set; }

	public IPlatform CurrentPlatform => ObjectLink as IPlatform;

	public int LastTickOnGround { get; set; }

	public bool CanFly { get; set; }

	public Player Player { get; set; }

	public CharacterInputState Input { get; set; }

	public int MovingDirection => Math.Sign(Input.HorizontalDirection);

	public CharacterLookDirection LookDirection { get; set; }

	public int LookDelay { get; set; }

	public double GroundVelocity { get; set; }

	public Vector2 Velocity { get; set; }

	public double Acceleration { get; set; }

	private double Deceleration { get; set; }

	private double Friction { get; set; }

	private double TopSpeed { get; set; }

	private double JumpForce { get; set; }

	private double JumpReleaseForce { get; set; }

	private double Gravity { get; set; }

	public int SlopeLockTicks { get; set; }

	public CharacterEvent CharacterEvents { get; set; }

	public BarrierType Barrier { get; set; }

	public bool HasPerformedBarrierAttack { get; set; }

	public bool HasSuperAbility { get; set; }

	public bool HasHyperAbility { get; set; }

	public bool Jumped { get; set; }

	public CharacterSpecialState SpecialState { get; set; }

	public bool HasBarrier => Barrier != BarrierType.None;

	public IReadOnlyList<CharacterHistoryItem> History => _history;

	public bool IsDeadly
	{
		get
		{
			if (!IsSpinball && !IsCharging)
			{
				return IsInvincible;
			}
			return true;
		}
	}

	protected int InvulnerabilityTicks { get; set; }

	public bool CanBeHurt
	{
		get
		{
			if (IsHurt)
			{
				return false;
			}
			if (InvulnerabilityTicks > 0)
			{
				return false;
			}
			if (IsInvincible)
			{
				return false;
			}
			return true;
		}
	}

	protected string InvincibilityGroupResourceKey { get; set; }

	public bool IsSidekick { get; set; }

	public bool Respawning { get; private set; }

	private bool IsLeftBehind
	{
		get
		{
			if (!IsSidekick)
			{
				return false;
			}
			double num = Math.Abs(Position.X - Player.Protagonist.Position.X);
			double num2 = Math.Abs(Position.Y - Player.Protagonist.Position.Y);
			if (!(num > 2069.0))
			{
				return num2 > 1152.0;
			}
			return true;
		}
	}

	public bool IsHumanInput
	{
		get
		{
			if (Input.HorizontalDirection != 0 || Input.VerticalDirection != 0)
			{
				return true;
			}
			if (Input.ABC.HasFlag(CharacterInputButtonState.Down))
			{
				return true;
			}
			return false;
		}
	}

	public CharacterState StateFlags { get; set; }

	public bool IsPushing
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Pushing);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Pushing) : (StateFlags & ~CharacterState.Pushing));
		}
	}

	public bool IsBraking
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Braking);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Braking) : (StateFlags & ~CharacterState.Braking));
		}
	}

	public bool IsSpinball
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Spinball);
		}
		set
		{
			if (IsSpinball != value)
			{
				if (value)
				{
					StateFlags |= CharacterState.Spinball;
					CollisionRadius = SpinballCollisionRadius;
					PositionPrecise += new Vector2(0.0, NormalCollisionRadius.Y - SpinballCollisionRadius.Y);
				}
				else
				{
					StateFlags &= ~CharacterState.Spinball;
					CollisionRadius = NormalCollisionRadius;
					PositionPrecise += new Vector2(0.0, -(NormalCollisionRadius.Y - SpinballCollisionRadius.Y));
				}
				if (!value)
				{
					IsRollJumping = false;
				}
			}
		}
	}

	public bool IsRollJumping
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Rolling);
		}
		set
		{
			if (value)
			{
				StateFlags |= CharacterState.Rolling;
			}
			else
			{
				StateFlags &= ~CharacterState.Rolling;
			}
		}
	}

	public bool IsAirborne
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Airborne);
		}
		set
		{
			if (value)
			{
				if (StateFlags != CharacterState.Airborne)
				{
					StateFlags |= CharacterState.Airborne;
					LastTickOnGround = Level.Ticks;
				}
				Mode = CollisionMode.Air;
				IsBraking = false;
				IsPushing = false;
			}
			else
			{
				StateFlags &= ~CharacterState.Airborne;
				IsFlying = false;
				IsJumping = false;
			}
		}
	}

	public bool IsJumping
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Jumping);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Jumping) : (StateFlags & ~CharacterState.Jumping));
		}
	}

	public int Facing
	{
		get
		{
			if (!StateFlags.HasFlag(CharacterState.Left))
			{
				return 1;
			}
			return -1;
		}
		set
		{
			if (value < 0)
			{
				StateFlags |= CharacterState.Left;
			}
			else if (value > 0)
			{
				StateFlags &= ~CharacterState.Left;
			}
		}
	}

	public bool IsFacingLeft
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Left);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Left) : (StateFlags &= ~CharacterState.Left));
		}
	}

	public bool IsFacingRight
	{
		get
		{
			return !StateFlags.HasFlag(CharacterState.Left);
		}
		set
		{
			StateFlags = (value ? (StateFlags &= ~CharacterState.Left) : (StateFlags | CharacterState.Left));
		}
	}

	public bool IsUnderwater
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Underwater);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Underwater) : (StateFlags & ~CharacterState.Underwater));
		}
	}

	public bool IsHurt
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Hurt);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Hurt) : (StateFlags &= ~CharacterState.Hurt));
		}
	}

	public bool IsDying
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Dying);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Dying) : (StateFlags & ~CharacterState.Dying));
		}
	}

	public bool IsDead
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Dead);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Dead) : (StateFlags & ~CharacterState.Dead));
		}
	}

	public bool IsDebug
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Debug);
		}
		set
		{
			if (value)
			{
				StateFlags = CharacterState.Debug | (StateFlags & CharacterState.Left);
				Velocity = new Vector2(0.0, 0.0);
			}
			else
			{
				StateFlags = CharacterState.Airborne | (StateFlags & CharacterState.Left);
				if (Player.InvincibillityTicks > 0 && !IsSidekick)
				{
					StateFlags |= CharacterState.Invincible;
				}
			}
			IsSpinball = true;
			Mode = CollisionMode.Air;
		}
	}

	public bool IsFlying
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Flying);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Flying) : (StateFlags & ~CharacterState.Flying));
		}
	}

	public bool ExhibitsVirtualPlatform
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.VirtualPlatform);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.VirtualPlatform) : (StateFlags & ~CharacterState.VirtualPlatform));
		}
	}

	public bool HasSpeedShoes
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.SpeedShoes);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.SpeedShoes) : (StateFlags & ~CharacterState.SpeedShoes));
		}
	}

	public bool IsInvincible
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Invincible);
		}
		set
		{
			if (value)
			{
				if (!IsInvincible)
				{
					StateFlags |= CharacterState.Invincible;
					StartInvincibility();
				}
			}
			else
			{
				StateFlags &= ~CharacterState.Invincible;
			}
		}
	}

	public bool ForceSpinball
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.ForceSpinball);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.ForceSpinball) : (StateFlags & ~CharacterState.ForceSpinball));
		}
	}

	public bool IsWinning
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Winning);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Winning) : (StateFlags &= ~CharacterState.Winning));
			if (value)
			{
				GroundVelocity = 0.0;
				Velocity = default;
			}
		}
	}

	public bool IsSuper
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.Super);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.Super) : (StateFlags & ~CharacterState.Super));
		}
	}

	public bool IsCharging
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.ChargingSpindash);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.ChargingSpindash) : (StateFlags & ~CharacterState.ChargingSpindash));
		}
	}

	public bool ShouldReactToLevel
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.ShouldReactToLevel);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.ShouldReactToLevel) : (StateFlags & ~CharacterState.ShouldReactToLevel));
		}
	}

	public bool IsObjectControlled
	{
		get
		{
			return StateFlags.HasFlag(CharacterState.ObjectControlled);
		}
		set
		{
			StateFlags = (value ? (StateFlags | CharacterState.ObjectControlled) : (StateFlags & ~CharacterState.ObjectControlled));
		}
	}

	public int BreathTicks { get; set; }

	protected override void OnAnimate()
	{
		DrawBodyRotated = false;
		Animation.OverrideDelay = null;
		if (!IsDead)
		{
			CharacterSpecialState specialState = SpecialState;
			if (specialState == CharacterSpecialState.Grabbed)
			{
				Animation.Index = 24;
				Animation.Animate();
			}
			else
			{
				AnimationNormalState();
			}
			if (_drowningClimax)
			{
				_drowningAnimation.Animate();
			}
		}
	}

	private void AnimationNormalState()
	{
		if (IsDying)
		{
			if (_deathCause == PlayerDeathCause.Drown)
			{
				Animation.Index = 20;
			}
			else
			{
				Animation.Index = 15;
			}
		}
		else
		{
			if (IsDebug)
			{
				Animation.Index = 10;
				return;
			}
			if (IsHurt)
			{
				Animation.Index = 14;
			}
			else if (IsWinning)
			{
				Animation.Index = 19;
			}
			else if (IsFlying)
			{
				Animation.Index = 17;
			}
			else if (IsCharging)
			{
				Animation.Index = 11;
			}
			else if (IsSpinball)
			{
				Animation.Index = 10;
				Animation.OverrideDelay = (int)(Math.Max(0.0, 20.0 - Math.Abs(GroundVelocity)) / 5.0);
			}
			else if ((Animation.Index != 12 && Animation.Index != 13) || !IsAirborne)
			{
				if (LookDirection == CharacterLookDirection.Up)
				{
					Animation.Index = 1;
				}
				else if (LookDirection == CharacterLookDirection.Ducking)
				{
					Animation.Index = 2;
				}
				else if (IsPushing)
				{
					Animation.Index = 6;
					Animation.OverrideDelay = 8;
					DrawBodyRotated = true;
				}
				else if (IsBraking)
				{
					Animation.Index = 9;
					DrawBodyRotated = true;
				}
				else if (GroundVelocity == 0.0)
				{
					switch (Math.Abs((int)_balanceDirection))
					{
						case 1:
							if ((IsFacingLeft && _balanceDirection > CharacterBalanceDirection.None) || (IsFacingRight && _balanceDirection < CharacterBalanceDirection.None))
							{
								Animation.Index = 5;
							}
							else
							{
								Animation.Index = 4;
							}
							break;
						case 2:
							Animation.Index = 3;
							break;
						default:
							if (IsAirborne)
							{
								Animation.Index = 13;
							}
							else if (Animation.Index == 19)
							{
								Animation.Index = 26;
							}
							else if (Animation.Index != 26)
							{
								Animation.Index = 0;
							}
							break;
					}
				}
				else if (_inhalingBubble != 0)
				{
					if (_inhalingBubble == 1)
					{
						_inhalingBubble = 2;
						Animation.Index = 18;
					}
					else if (Animation.Index != 18)
					{
						_inhalingBubble = 0;
					}
				}
				else
				{
					bool flag = true;
					bool flag2 = false;
					bool flag3 = false;
					if (Player.ProtagonistCharacterType == CharacterType.Sonic && this == Player.Protagonist)
					{
						flag2 = true;
						flag3 = true;
					}
					if (Math.Abs(GroundVelocity) < 24.0)
					{
						if (flag3)
						{
							if (Animation.Index == 0 && Animation.CurrentFrameIndex >= 248)
							{
								Animation.Index = 27;
							}
							else if (Animation.Index == 27)
							{
								GroundVelocity = 0.0;
							}
							else
							{
								Animation.Index = 7;
							}
						}
						else
						{
							Animation.Index = 7;
						}
					}
					else if (Math.Abs(GroundVelocity) >= 48.0)
					{
						if (flag2)
						{
							Animation.Index = 28;
						}
						else
						{
							Animation.Index = 8;
						}
						flag = false;
					}
					else if (flag2)
					{
						if (Animation.Index == 28)
						{
							Animation.Index = 29;
						}
						else
						{
							Animation.Index = 8;
						}
					}
					else
					{
						Animation.Index = 8;
					}
					if (flag)
					{
						Animation.OverrideDelay = (int)(Math.Max(0.0, 32.0 - Math.Abs(GroundVelocity)) / 8.0);
					}
					DrawBodyRotated = true;
				}
			}
		}
		UpdateRotation();
		if (!IsSpinball && TumbleAngle != 0.0 && !IsHurt && !IsDying)
		{
			Animation.Index = 16;
			double num = MathX.Clamp(TumbleAngle, 1.0);
			num = ((num >= 0.0) ? (num / 2.0) : ((1.0 - Math.Abs(num)) / 2.0 + 0.5));
			if (IsFacingLeft)
			{
				num = 1.0 - num;
				num = MathX.Wrap(num + 0.5, 1.0);
			}
			Animation.CurrentFrameIndex = (int)((Animation.AnimationGroup[Animation.Index].Frames.Count - 1) * num);
			Animation.CurrentFrameIndex = MathX.Clamp(0, Animation.CurrentFrameIndex, Animation.AnimationGroup[Animation.Index].Frames.Count - 1);
		}
		else
		{
			Animation.Animate();
		}
		if (HasBarrier)
		{
			if (_barrierTypeAnimation != Barrier)
			{
				_barrierTypeAnimation = Barrier;
				BarrierAnimation = new AnimationInstance(ResourceTree, GetBarrierResourceKey(_barrierTypeAnimation));
			}
			BarrierAnimation.Animate();
		}
		SpindashDustAnimation.Animate();
		if (IsInvincible)
		{
			AnimateInvincibility();
		}
	}

	private void UpdateRotation()
	{
		if (Mode == CollisionMode.Air || GroundVector == null)
		{
			ShowAngle = MathX.GoTowardsWrap(ShowAngle, 0.0, 0.05, 0.0, Math.PI * 2.0);
			if (TumbleTurns == 0)
			{
				TumbleAngle = MathX.ChangeSpeed(TumbleAngle, -3.0 / 128.0);
			}
			else if (TumbleTurns < 0)
			{
				TumbleAngle -= 3.0 / 128.0;
				if (TumbleAngle < 0.0)
				{
					TumbleAngle++;
					TumbleTurns++;
				}
			}
			else if (TumbleTurns > 0)
			{
				TumbleAngle += 3.0 / 128.0;
				if (TumbleAngle > 1.0)
				{
					TumbleAngle -= 2.0;
					TumbleTurns--;
				}
			}
		}
		else
		{
			double to = (GroundVector.Flags.HasFlag(CollisionFlags.Rotate) ? GroundVector.Angle : ((double)Mode * (-Math.PI / 2.0)));
			ShowAngle = MathX.LerpWrap(ShowAngle, to, MathX.Clamp(0.25, Math.Abs(GroundVelocity) / 64.0, 1.0), 0.0, Math.PI * 2.0);
			TumbleAngle = 0.0;
		}
	}

	private static string GetBarrierResourceKey(BarrierType type)
	{
		return type switch
		{
			BarrierType.Classic => "SONICORCA/OBJECTS/BARRIER/ANIGROUP",
			BarrierType.Bubble => "SONICORCA/OBJECTS/BARRIER/BUBBLE/ANIGROUP",
			BarrierType.Fire => "SONICORCA/OBJECTS/BARRIER/FIRE/ANIGROUP",
			BarrierType.Lightning => "SONICORCA/OBJECTS/BARRIER/LIGHTNING/ANIGROUP",
			_ => null,
		};
	}

	private void ProcessCollision()
	{
		ResetCollisionEvents();
		if (GroundVector != null && GroundVector.Owner != null)
		{
			_ = GroundVector;
			if (!GroundVector.Owner.CollisionVectors.Contains(GroundVector))
			{
				LeaveGround();
			}
		}
		if (CheckCollision)
		{
			_originalGroundVector = ((Mode == CollisionMode.Air) ? null : GroundVector);
			_collidedVectors.Clear();
			if (GroundVector != null)
			{
				CollisionMode mode = Mode;
				if ((uint)mode <= 3u)
				{
					ConstrainToPath();
				}
			}
			Bump(CollisionRadius);
			CheckOverlappingVector();
			if (PlayerShouldLand())
			{
				Land();
			}
			UpdateCollisionsWithObjects();
			if (Mode != CollisionMode.Air)
			{
				_collisionDetected[(int)Mode] = true;
				CollisionInfo collisionInfo = new CollisionInfo(GroundVector, default, 0.0, GroundAngle);
				CollisionEvent collisionEvent;
				if (GroundVector != null)
				{
					collisionEvent = new CollisionEvent(this, collisionInfo);
				}
				else
				{
					collisionEvent = new CollisionEvent(this, 0);
					collisionEvent.CollisionInfo = collisionInfo;
				}
				_collisionEvents[(int)Mode] = collisionEvent;
			}
			CheckCrushed();
		}
		UpdateDistanceFromLedge();
		LastPath = Path;
	}

	private void ResetCollisionEvents()
	{
		for (int i = 0; i < 4; i++)
		{
			_collisionEvents[i] = null;
			_collisionDetected[i] = false;
			_collisionEventsTiles[i] = null;
			_collisionDetectedTiles[i] = false;
			_overlappingGroundVector = null;
			_overlappingCollision = new KeyValuePair<ActiveObject, CollisionEvent>(null, null);
		}
	}

	private bool CheckIgnoreFlag(CollisionVector t)
	{
		if (t.Flags.HasFlag(CollisionFlags.Ignore))
		{
			return true;
		}
		return false;
	}

	private bool CheckSolidAngle(CollisionVector t, Vector2 intersection, Vector2 collisionRadius)
	{
		if (t.Flags.HasFlag(CollisionFlags.Solid))
		{
			return false;
		}
		Vector2 vector = default;
		_ = PositionPrecise.X;
		int num = 1;
		if (t.Mode == CollisionMode.Right)
		{
			vector = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X, intersection.Y - collisionRadius.X), Math.PI / 2.0 + GroundAngle);
		}
		else if (t.Mode == CollisionMode.Left)
		{
			vector = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X, intersection.Y + collisionRadius.X), Math.PI / 2.0 + GroundAngle);
		}
		else if (t.Mode == CollisionMode.Top)
		{
			vector = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X - collisionRadius.Y, intersection.Y), Math.PI / 2.0 + GroundAngle);
		}
		else if (t.Mode == CollisionMode.Bottom)
		{
			vector = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X + collisionRadius.Y, intersection.Y), Math.PI / 2.0 + GroundAngle);
		}
		return MathX.DifferenceRadians((PositionPrecise - vector).Angle, t.Angle) * num > 0.0;
	}

	private Vector2 GetPointRotatedFromOrigin(Vector2 center, Vector2 point, double angleInRadians)
	{
		double x = point.X;
		double y = point.Y;
		double x2 = center.X;
		double y2 = center.Y;
		double x3 = (x - x2) * Math.Cos(angleInRadians) - (y - y2) * Math.Sin(angleInRadians) + x2;
		double y3 = (x - x2) * Math.Sin(angleInRadians) + (y - y2) * Math.Cos(angleInRadians) + y2;
		return new Vector2(x3, y3);
	}

	private Vector2 GetPointRotatedFromRelative(Vector2 relative, Vector2 point, double theta)
	{
		double x = point.X;
		double y = point.Y;
		double x2 = relative.X;
		double y2 = relative.Y;
		double x3 = Math.Cos(theta) * (x - x2) - Math.Sin(theta) * (y - y2) + x2;
		double y3 = Math.Sin(theta) * (x - x2) + Math.Cos(theta) * (y - y2) + y2;
		return new Vector2(x3, y3);
	}

	public Vector2[][] GetCollisionBox(Vector2 collisionRadius, bool rotate)
	{
		double theta = 0.0;
		if (rotate)
		{
			theta = GroundAngle;
		}
		Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - collisionRadius.X, PositionPrecise.Y - collisionRadius.Y), theta);
		Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + collisionRadius.X, PositionPrecise.Y - collisionRadius.Y), theta);
		Vector2 pointRotatedFromRelative3 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + collisionRadius.X, PositionPrecise.Y + collisionRadius.Y), theta);
		Vector2 pointRotatedFromRelative4 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - collisionRadius.X, PositionPrecise.Y + collisionRadius.Y), theta);
		return new Vector2[4][]
		{
			new Vector2[2] { pointRotatedFromRelative, pointRotatedFromRelative2 },
			new Vector2[2] { pointRotatedFromRelative3, pointRotatedFromRelative2 },
			new Vector2[2] { pointRotatedFromRelative4, pointRotatedFromRelative3 },
			new Vector2[2] { pointRotatedFromRelative4, pointRotatedFromRelative }
		};
	}

	private bool IsBeyondPathEnd(bool useLedgeSensor)
	{
		if (LastPositionPrecise == PositionPrecise)
		{
			return false;
		}
		int num = (useLedgeSensor ? (CollisionRadius.X - LedgeSensorRadius) : 0);
		Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2i absoluteA = GroundVector.AbsoluteA;
		Vector2i absoluteB = GroundVector.AbsoluteB;
		if (Vector2.Intersects(pointRotatedFromRelative, pointRotatedFromRelative2, absoluteA, absoluteB))
		{
			return false;
		}
		Vector2 intersection = default;
		Vector2.GetLineIntersection(pointRotatedFromRelative, pointRotatedFromRelative2, absoluteA, absoluteB, out intersection);
		double distance = Vector2.GetDistance(intersection, GroundVector.Bounds.Centre);
		double distance2 = Vector2.GetDistance(intersection, absoluteA);
		double distance3 = Vector2.GetDistance(intersection, absoluteB);
		double distance4 = Vector2.GetDistance(GroundVector.Bounds.Centre, absoluteB);
		_ = GroundAngle;
		_ = GroundVector.Angle;
		if (distance3 < distance2 && distance - num > distance4)
		{
			return true;
		}
		return false;
	}

	private bool IsBeyondPathStart(bool useLedgeSensor)
	{
		int num = (useLedgeSensor ? (CollisionRadius.X - LedgeSensorRadius) : 0);
		Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2i absoluteA = GroundVector.AbsoluteA;
		Vector2i absoluteB = GroundVector.AbsoluteB;
		if (Vector2.Intersects(pointRotatedFromRelative, pointRotatedFromRelative2, absoluteA, absoluteB))
		{
			return false;
		}
		Vector2 intersection = default;
		Vector2.GetLineIntersection(pointRotatedFromRelative, pointRotatedFromRelative2, absoluteA, absoluteB, out intersection);
		double distance = Vector2.GetDistance(intersection, GroundVector.Bounds.Centre);
		double distance2 = Vector2.GetDistance(intersection, GroundVector.AbsoluteA);
		double distance3 = Vector2.GetDistance(intersection, GroundVector.AbsoluteB);
		double distance4 = Vector2.GetDistance(GroundVector.Bounds.Centre, absoluteA);
		if (distance3 > distance2 && distance - num > distance4)
		{
			return true;
		}
		return false;
	}

	private double GenesisHexToRadians(double angle)
	{
		return MathX.ToRadians((angle - 256.0) * 1.40625);
	}

	private double GenesisHexToDegrees(double angle)
	{
		return 360.0 + (0.0 - (256.0 - angle)) * 1.40625;
	}

	private bool IsLandingViable(CollisionVector vector)
	{
		if (vector.Flags.HasFlag(CollisionFlags.NoLanding))
		{
			Velocity = default;
			return false;
		}
		switch (vector.Mode)
		{
			case CollisionMode.Top:
				return Velocity.Y >= 0.0;
			case CollisionMode.Bottom:
				if (vector.FlipY != 0 && Velocity.Y < 0.0)
				{
					return true;
				}
				if (Velocity.Y < 0.0)
				{
					Velocity = new Vector2(Velocity.X, 0.0);
					if (Mode != CollisionMode.Air)
					{
						GroundVelocity = 0.0;
					}
				}
				return false;
			case CollisionMode.Left:
			case CollisionMode.Right:
				if (vector.FlipX != 0 && Math.Abs(Velocity.X) >= 0.0)
				{
					return true;
				}
				if (Velocity.X * vector.FlipY < 0.0)
				{
					if (vector != null)
					{
						Velocity = new Vector2(0.0, Velocity.Y);
					}
					else
					{
						Velocity = new Vector2(0.0, Velocity.Y);
					}
					if (Mode != CollisionMode.Air)
					{
						GroundVelocity = 0.0;
					}
				}
				else if (vector.FlipY == 0)
				{
					Velocity = new Vector2(0.0, Velocity.Y);
				}
				return false;
			default:
				return false;
		}
	}

	private bool PlayerShouldLand()
	{
		bool result = false;
		CollisionMode collisionMode = CollisionMode.Air;
		for (int i = 0; i < 4; i++)
		{
			if (_collisionEvents[i] != null && !_collisionEvents[i].MaintainVelocity && IsLandingViable(_collisionEvents[i].CollisionInfo.Vector))
			{
				collisionMode = (CollisionMode)i;
			}
		}
		if (collisionMode != CollisionMode.Air)
		{
			if (CharacterEvents.HasFlag(CharacterEvent.Hurt))
			{
				return result;
			}
			bool flag = Mode == CollisionMode.Air;
			CollisionVector vector = _collisionEvents[(int)collisionMode].CollisionInfo.Vector;
			if (vector != _originalGroundVector)
			{
				PositionPrecise = GetAlignmentForVector2(vector, PositionPrecise, GroundAngle);
				AcquireVector(vector);
				if (flag)
				{
					result = true;
				}
				Bump(CollisionRadius);
			}
		}
		return result;
	}

	private void Land(bool updateSpeed = true)
	{
		if (Barrier == BarrierType.Bubble && HasPerformedBarrierAttack)
		{
			Mode = CollisionMode.Air;
			GroundVector = null;
			IsJumping = true;
			IsRollJumping = false;
			HasPerformedBarrierAttack = false;
			Velocity = new Vector2(Velocity.X, -30.0);
		}
		else
		{
			IsAirborne = false;
			IsJumping = false;
			if (Level.Ticks - LastTickOnGround > 11)
			{
				IsSpinball = false;
			}
			IsRollJumping = false;
			IsHurt = false;
			IsFlying = false;
			HasPerformedBarrierAttack = false;
			ShowAngle = GroundAngle;
			Jumped = false;
			if (updateSpeed)
			{
				if (GroundVector.Flags.HasFlag(CollisionFlags.NoAngle))
				{
					GroundVelocity = Velocity.X;
				}
				else
				{
					GroundVelocity = ((Math.Abs(Velocity.X) > Math.Abs(Velocity.Y)) ? (Velocity.X * GroundVector.FlipX) : (Velocity.X * Math.Cos(GroundAngle) + Velocity.Y * Math.Sin(GroundAngle)));
					Velocity = new Vector2(GroundVelocity * Math.Cos(GroundAngle), GroundVelocity * Math.Sin(GroundAngle));
				}
			}
		}
		Player.ResetScoreChain();
	}

	public void LeaveGround()
	{
		if (GroundVector != null)
		{
			RemovePlatformVelocity();
			if (GroundVector.Flags.HasFlag(CollisionFlags.Snap))
			{
				Math.Sign(GroundVelocity);
				double num = MathX.Snap(GroundAngle, Math.PI / 2.0);
				Velocity = new Vector2(GroundVelocity * Math.Cos(num), GroundVelocity * Math.Sin(num));
			}
			GroundVector = null;
			ObjectLink = null;
		}
		Mode = CollisionMode.Air;
		IsAirborne = true;
		IsBraking = false;
		IsCharging = false;
		IsHurt = false;
	}

	private void ConstrainToPath()
	{
		CollisionVector collisionVector = null;
		bool flag = true;
		if (IsBeyondPathEnd(useLedgeSensor: false))
		{
			collisionVector = GroundVector;
			bool flag2 = true;
			double num = double.MaxValue;
			CollisionVector collisionVector2 = null;
			while (flag2)
			{
				if ((collisionVector = collisionVector.GetConnectionB(Path)) != null)
				{
					double distanceToVector = GetDistanceToVector(collisionVector);
					if (distanceToVector < num)
					{
						num = distanceToVector;
						if (_lastLeftVector != collisionVector)
						{
							collisionVector2 = (_lastLeftVector = collisionVector);
						}
					}
					else
					{
						flag2 = false;
					}
				}
				else
				{
					flag2 = false;
				}
			}
			collisionVector = collisionVector2;
		}
		else if (IsBeyondPathStart(useLedgeSensor: false))
		{
			collisionVector = GroundVector;
			bool flag3 = true;
			double num2 = double.MaxValue;
			CollisionVector collisionVector3 = null;
			while (flag3)
			{
				if ((collisionVector = collisionVector.GetConnectionA(Path)) != null)
				{
					double distanceToVector2 = GetDistanceToVector(collisionVector);
					if (distanceToVector2 < num2)
					{
						num2 = distanceToVector2;
						if (_lastRightVector != collisionVector)
						{
							collisionVector3 = (_lastRightVector = collisionVector);
						}
					}
					else
					{
						flag3 = false;
					}
				}
				else
				{
					flag3 = false;
				}
			}
			collisionVector = collisionVector3;
		}
		else
		{
			_lastRightVector = null;
			_lastLeftVector = null;
			flag = false;
		}
		if (collisionVector != null)
		{
			if (!collisionVector.Flags.HasFlag(CollisionFlags.NoPathFollowing))
			{
				PositionPrecise = GetAlignmentForVector2(collisionVector, PositionPrecise, GroundAngle);
				AcquireVector(collisionVector);
			}
			else if (flag)
			{
				LeaveGround();
			}
		}
		else if (flag)
		{
			if (IsBeyondPathEnd(useLedgeSensor: true) || IsBeyondPathStart(useLedgeSensor: true))
			{
				LeaveGround();
			}
		}
		else
		{
			PlayerShouldLand();
			if (CheckIgnoreFlag(GroundVector))
			{
				LeaveGround();
			}
		}
	}

	private IEnumerable<CollisionVector> FixPosition(Vector2 collisionRadius, IEnumerable<CollisionVector> vectorsToCheck)
	{
		List<CollisionVector> result = new List<CollisionVector>();
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		double num4 = 0.0;
		bool flag = false;
		foreach (CollisionVector item in vectorsToCheck)
		{
			double num5 = 0.0;
			double num6 = 0.0;
			if (Mode == CollisionMode.Left || Mode == CollisionMode.Right)
			{
				flag = true;
				num5 = CollisionRadius.Y;
				num6 = CollisionRadius.X;
			}
			else
			{
				num5 = CollisionRadius.X;
				num6 = CollisionRadius.Y;
			}
			Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y + num5), Math.PI / 2.0 + GroundAngle);
			Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y - num5), Math.PI / 2.0 + GroundAngle);
			Vector2 pointRotatedFromRelative3 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + num6, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2 pointRotatedFromRelative4 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - num6, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2[][] collisionBox = GetCollisionBox(new Vector2(num5, num6 - FloorSensorRadius + 20.0), rotate: false);
			Vector2[][] collisionBox2 = GetCollisionBox(new Vector2(num5 - LedgeSensorRadius, num6), rotate: false);
			Vector2[][] array = new Vector2[0][];
			Vector2[][] array2 = new Vector2[0][];
			Vector2 vector = default;
			Vector2 vector2 = default;
			Vector2 vector3 = default;
			Vector2 vector4 = default;
			if (flag)
			{
				array = collisionBox2;
				array2 = collisionBox;
				vector = pointRotatedFromRelative3;
				vector2 = pointRotatedFromRelative4;
				vector3 = pointRotatedFromRelative;
				vector4 = pointRotatedFromRelative2;
			}
			else
			{
				array = collisionBox;
				array2 = collisionBox2;
				vector = pointRotatedFromRelative;
				vector2 = pointRotatedFromRelative2;
				vector3 = pointRotatedFromRelative3;
				vector4 = pointRotatedFromRelative4;
			}
			if (!CanCollideWithVector(item))
			{
				continue;
			}
			Vector2[] array3 = new Vector2[2] { item.AbsoluteA, item.AbsoluteB };
			bool flag2 = false;
			if (CheckIgnoreFlag(item))
			{
				continue;
			}
			bool flag3 = false;
			List<Vector2[]> list;
			if (item.IsWall)
			{
				list = new List<Vector2[]>();
				list.Add(new Vector2[2] { vector, vector2 });
				List<Vector2[]> list2 = list;
				list2.AddRange(array);
				List<Vector2> list3 = new List<Vector2>();
				for (int i = 0; i < list2.Count; i++)
				{
					if (Vector2.Intersects(list2[i][0], list2[i][1], array3[0], array3[1]))
					{
						flag2 = true;
						Vector2 intersection = default;
						Vector2.GetLineIntersection(list2[i][0], list2[i][1], array3[0], array3[1], out intersection);
						list3.Add(intersection);
					}
					flag3 = i >= 1;
				}
				if (!flag2)
				{
					Rectangle rectangle = Rectangle.FromLTRB(array[0][0].X, array[0][0].Y, array[1][0].X, array[1][0].Y);
					if (rectangle.ContainsOrOverlaps(array3[0]))
					{
						Vector2 intersection2 = default;
						Vector2 pointRotatedFromRelative5 = GetPointRotatedFromRelative(array3[0], new Vector2(array3[0].X - 5.0, array3[0].Y), Math.PI / 2.0);
						Vector2.GetLineIntersection(array3[0], pointRotatedFromRelative5, list2[0][0], list2[0][1], out intersection2);
						list3.Add(intersection2);
						flag2 = true;
					}
					if (rectangle.ContainsOrOverlaps(array3[1]))
					{
						Vector2 intersection3 = default;
						Vector2 pointRotatedFromRelative6 = GetPointRotatedFromRelative(array3[1], new Vector2(array3[1].X - 5.0, array3[1].Y), Math.PI / 2.0);
						Vector2.GetLineIntersection(array3[1], pointRotatedFromRelative6, list2[0][0], list2[0][1], out intersection3);
						list3.Add(intersection3);
						flag2 = true;
					}
				}
				if (!flag2)
				{
					continue;
				}
				Vector2 vector5 = default;
				double x = PositionPrecise.X;
				double num7 = num2;
				double num8 = num;
				Vector2 vector6 = new Vector2(-1.0, -1.0);
				if (item.Mode == CollisionMode.Right)
				{
					Vector2 vector7 = list3.First();
					foreach (Vector2 item2 in list3.Skip(1))
					{
						if (item2.X < vector7.X)
						{
							vector7 = item2;
						}
					}
					vector6 = vector7;
					vector5 = GetPointRotatedFromRelative(vector6, new Vector2(vector6.X, vector6.Y - num5), Math.PI / 2.0 + GroundAngle);
					if (vector5.X - x >= num2)
					{
						num2 = vector5.X - x;
					}
				}
				else if (item.Mode == CollisionMode.Left)
				{
					Vector2 vector8 = list3.First();
					foreach (Vector2 item3 in list3.Skip(1))
					{
						if (item3.X > vector8.X)
						{
							vector8 = item3;
						}
					}
					vector6 = vector8;
					vector5 = GetPointRotatedFromRelative(vector6, new Vector2(vector6.X, vector6.Y + num5), Math.PI / 2.0 + GroundAngle);
					if (vector5.X - x < num)
					{
						num = vector5.X - x;
					}
				}
				if (!CheckSolidAngle(item, vector6, new Vector2(num5, num6)))
				{
					double distance = Vector2.GetDistance(pointRotatedFromRelative2, vector6);
					CollisionInfo collisionInfo = new CollisionInfo(item, vector6, distance, item.Angle);
					CollisionEvent collisionEvent = new CollisionEvent(this, collisionInfo);
					if (item.Owner != null)
					{
						item.Owner.Collision(collisionEvent);
					}
					if (!collisionEvent.IgnoreCollision)
					{
						_collisionDetected[(int)item.Mode] = true;
						_collisionEvents[(int)item.Mode] = collisionEvent;
					}
					Vector2 positionPrecise = PositionPrecise;
					double num9 = 0.0;
					double num10 = 0.0;
					if (num7 != num2)
					{
						num9 = num2 - num7;
					}
					if (num8 != num)
					{
						num10 = num - num8;
					}
					if (num9 != 0.0 || num10 != 0.0)
					{
						positionPrecise.X += num9;
						positionPrecise.X += num10;
						PositionPrecise = positionPrecise;
					}
				}
				continue;
			}
			list = new List<Vector2[]>();
			list.Add(new Vector2[2] { vector3, vector4 });
			List<Vector2[]> list4 = list;
			list4.AddRange(array2);
			List<Vector2> list5 = new List<Vector2>();
			for (int j = 0; j < list4.Count; j++)
			{
				if (Vector2.Intersects(list4[j][0], list4[j][1], array3[0], array3[1]))
				{
					flag2 = true;
					Vector2 intersection4 = default;
					Vector2.GetLineIntersection(list4[j][0], list4[j][1], array3[0], array3[1], out intersection4);
					list5.Add(intersection4);
				}
				flag3 = j >= 1;
			}
			if (!flag2)
			{
				Rectangle rectangle2 = Rectangle.FromLTRB(array2[0][0].X, array2[0][0].Y, array2[1][0].X, array2[1][0].Y);
				if (rectangle2.ContainsOrOverlaps(array3[0]))
				{
					Vector2 intersection5 = default;
					Vector2.GetLineIntersection(GetPointRotatedFromRelative(array3[0], new Vector2(array3[0].X, array3[0].Y + 5.0), Math.PI / 2.0), array3[0], list4[0][0], list4[0][1], out intersection5);
					list5.Add(intersection5);
					flag2 = true;
				}
				if (rectangle2.ContainsOrOverlaps(array3[1]))
				{
					Vector2 intersection6 = default;
					Vector2.GetLineIntersection(GetPointRotatedFromRelative(array3[1], new Vector2(array3[1].X, array3[1].Y + 5.0), Math.PI / 2.0), array3[1], list4[0][0], list4[0][1], out intersection6);
					list5.Add(intersection6);
					flag2 = true;
				}
			}
			if (!flag2)
			{
				continue;
			}
			Vector2 vector9 = new Vector2(-1.0, -1.0);
			if (ShouldDisregardAsTooHighStep(item))
			{
				continue;
			}
			Vector2 vector10 = default;
			Math.Sign(GroundVelocity);
			Math.Min(Math.Abs(GroundVelocity), 96.0);
			Vector2 vector11 = new Vector2(MathX.Clamp(-64.0, GroundVelocity * Math.Cos(0.0), 64.0), MathX.Clamp(-64.0, GroundVelocity * Math.Sin(0.0), 64.0));
			double num11 = 0.0;
			num11 = ((!IsAirborne) ? vector11.Y : Velocity.Y);
			double y = PositionPrecise.Y;
			double num12 = num3;
			double num13 = num4;
			if (item.Mode == CollisionMode.Top)
			{
				Vector2 vector12 = list5.First();
				foreach (Vector2 item4 in list5.Skip(1))
				{
					if (item4.Y > vector12.Y)
					{
						vector12 = item4;
					}
				}
				vector9 = vector12;
				if (num11 >= 0.0)
				{
					vector10 = GetPointRotatedFromRelative(vector9, new Vector2(vector9.X - num6, vector9.Y), Math.PI / 2.0 + GroundAngle);
					if (vector10.Y - y < num3)
					{
						num3 = vector10.Y - y;
						if (item != GroundVector && GroundVector != null)
						{
							if (item.Owner != null)
							{
								if (item.Owner.PositionPrecise != item.Owner.LastPositionPrecise)
								{
									_overlappingGroundVector = item;
								}
							}
							else if (GroundVector.Owner != null)
							{
								_overlappingGroundVector = item;
							}
							else if (item.Mode == Mode)
							{
								_overlappingGroundVector = item;
							}
						}
					}
				}
			}
			else if (item.Mode == CollisionMode.Bottom)
			{
				Vector2 vector13 = list5.First();
				foreach (Vector2 item5 in list5.Skip(1))
				{
					if (item5.Y > vector13.Y)
					{
						vector13 = item5;
					}
				}
				vector9 = vector13;
				if (num11 < 0.0)
				{
					vector10 = GetPointRotatedFromRelative(vector9, new Vector2(vector9.X + num6, vector9.Y), Math.PI / 2.0 + GroundAngle);
					if (vector10.Y - y > num4)
					{
						num4 = vector10.Y - y;
					}
				}
			}
			if (CheckSolidAngle(item, vector9, new Vector2(num5, num6)))
			{
				continue;
			}
			double distance2 = Vector2.GetDistance(pointRotatedFromRelative4, vector9);
			CollisionInfo collisionInfo2 = new CollisionInfo(item, vector9, distance2, item.Angle);
			CollisionEvent collisionEvent2 = new CollisionEvent(this, collisionInfo2);
			if (flag3 && GroundVector != null && (item.Mode == CollisionMode.Top || item.Mode == CollisionMode.Bottom) && item.Owner == null && !IsFlying)
			{
				_collisionDetectedTiles[(int)item.Mode] = true;
				_collisionEventsTiles[(int)item.Mode] = collisionEvent2;
				continue;
			}
			if (item.Owner != null)
			{
				KeyValuePair<ActiveObject, CollisionEvent> keyValuePair = new KeyValuePair<ActiveObject, CollisionEvent>(item.Owner, collisionEvent2);
				_collidedVectors.Add(keyValuePair);
				if (_overlappingGroundVector != null)
				{
					if (item.Owner == _overlappingGroundVector.Owner)
					{
						_overlappingCollision = keyValuePair;
					}
					else
					{
						keyValuePair.Key.Collision(keyValuePair.Value);
					}
				}
				else
				{
					keyValuePair.Key.Collision(keyValuePair.Value);
				}
			}
			if (!collisionEvent2.IgnoreCollision)
			{
				_collisionDetected[(int)item.Mode] = true;
				_collisionEvents[(int)item.Mode] = collisionEvent2;
			}
			Vector2 positionPrecise2 = PositionPrecise;
			double num14 = 0.0;
			double num15 = 0.0;
			if (num12 != num3)
			{
				num14 = num3 - num12;
			}
			if (num13 != num4)
			{
				num15 = num4 - num13;
			}
			if (num14 != 0.0 || num15 != 0.0)
			{
				positionPrecise2.Y += num14;
				positionPrecise2.Y += num15;
				PositionPrecise = positionPrecise2;
			}
		}
		return result;
	}

	private void CheckOverlappingVector()
	{
		if (_overlappingGroundVector == null || _overlappingGroundVector == GroundVector || GroundVector == null || _overlappingGroundVector.Mode != GroundVector.Mode || _overlappingGroundVector == GroundVector.GetConnectionA(Path) || _overlappingGroundVector == GroundVector.GetConnectionB(Path))
		{
			return;
		}
		bool flag = true;
		if (_overlappingGroundVector.Owner != null)
		{
			flag = _overlappingGroundVector.Owner.CollisionVectors.Contains(_overlappingGroundVector);
		}
		PositionPrecise = GetAlignmentForVector2(_overlappingGroundVector, PositionPrecise, _overlappingGroundVector.Angle);
		if (flag)
		{
			AcquireVector(_overlappingGroundVector);
			if (_overlappingGroundVector.Owner != null)
			{
				Land(updateSpeed: false);
			}
			else
			{
				Land();
			}
			if (_overlappingCollision.Key != null && _overlappingCollision.Value != null)
			{
				_overlappingCollision.Key.Collision(_overlappingCollision.Value);
			}
		}
	}

	private bool IsAttachedToPlatform()
	{
		Vector2 vector = default;
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.Conveyor))
		{
			vector = ((!(GroundVector.Owner is IPlatform)) ? (GroundVector.Owner.PositionPrecise - GroundVector.Owner.LastPositionPrecise) : (GroundVector.Owner as IPlatform).Velocity);
		}
		return vector != default(Vector2);
	}

	private void Bump(Vector2 collisionRadius)
	{
		Rectangle rectangle = new Rectangle(PositionPrecise.X - 128.0, PositionPrecise.Y - 128.0, 256.0, 256.0);
		IEnumerable<CollisionVector> possibleCollisionIntersections = Level.CollisionTable.GetPossibleCollisionIntersections(rectangle, CheckLandscapeCollision, CheckObjectCollision);
		bool flag = IsAttachedToPlatform();
		possibleCollisionIntersections = ((!((Math.Abs(Velocity.X) >= Math.Abs(Velocity.Y)) | flag)) ? possibleCollisionIntersections.OrderBy((CollisionVector t) => t.IsWall ? 1 : ((t.Mode != CollisionMode.Top && t.Owner != null) ? 2 : 0)) : possibleCollisionIntersections.OrderBy((CollisionVector t) => (!t.IsWall) ? ((t.Mode == CollisionMode.Top || t.Owner == null) ? 1 : 2) : 0));
		possibleCollisionIntersections = FixPosition(collisionRadius, possibleCollisionIntersections);
	}

	private bool ShouldDisregardAsTooHighStep(CollisionVector t)
	{
		bool result = false;
		if (Mode == t.Mode && Math.Abs(t.Bottom - (Position.Y + CollisionRadius.Y)) >= FloorSensorRadius)
		{
			result = true;
			if (t.Owner != null && t.Owner.PositionPrecise.Y - t.Owner.LastPositionPrecise.Y < 0.0)
			{
				result = false;
			}
		}
		return result;
	}

	private bool CanCollideWithVector(CollisionVector vector)
	{
		if (!vector.HasPath(Path))
		{
			return false;
		}
		return true;
	}

	private double GetDistanceToVector(CollisionVector t)
	{
		return Vector2.GetDistance(PositionPrecise, t.Bounds.Centre);
	}

	public Vector2 GetProjectedPointOnLine(Vector2 v1, Vector2 v2, Vector2 point)
	{
		Vector2 vector = point;
		Vector2 vector2 = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
		Vector2 v3 = new Vector2(vector.X - v1.X, vector.Y - v1.Y);
		double num = vector2.Dot(v3);
		double num2 = Math.Sqrt(vector2.X * vector2.X + vector2.Y * vector2.Y);
		double num3 = Math.Sqrt(v3.X * v3.X + v3.Y * v3.Y);
		double num4 = num / (num2 * num3) * num3;
		return new Vector2(v1.X + num4 * vector2.X / num2, v1.Y + num4 * vector2.Y / num2);
	}

	public Vector2 GetProjectedPointOnLine2(Vector2 v1, Vector2 v2, Vector2 point)
	{
		Vector2 vector = point;
		Vector2 vector2 = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
		Vector2 v3 = new Vector2(vector.X - v1.X, vector.Y - v1.Y);
		double num = vector2.Dot(v3);
		double num2 = Math.Sqrt(vector2.X * vector2.X + vector2.Y * vector2.Y);
		double num3 = Math.Sqrt(v3.X * v3.X + v3.Y * v3.Y);
		double num4 = num / (num2 * num3) * num3;
		return new Vector2(v1.X + num4 * vector2.X / num2, v1.Y + num4 * vector2.Y / num2);
	}

	private Vector2 GetAlignmentForVector(CollisionVector t, Vector2 alignmentPosition, double alignmentAngle)
	{
		int y = CollisionRadius.Y;
		GetPointRotatedFromRelative(alignmentPosition, new Vector2(alignmentPosition.X - y, alignmentPosition.Y), Math.PI / 2.0 + alignmentAngle);
		Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(alignmentPosition, new Vector2(alignmentPosition.X + y, alignmentPosition.Y), Math.PI / 2.0 + alignmentAngle);
		Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(pointRotatedFromRelative, new Vector2(pointRotatedFromRelative.X - y, pointRotatedFromRelative.Y), Math.PI / 2.0 + t.Angle);
		Vector2 pointRotatedFromRelative3 = GetPointRotatedFromRelative(pointRotatedFromRelative, new Vector2(pointRotatedFromRelative.X + y, pointRotatedFromRelative.Y), Math.PI / 2.0 + t.Angle);
		Vector2 intersection = Intersector.Intersection(t.AbsoluteB, t.AbsoluteA, pointRotatedFromRelative3, pointRotatedFromRelative2).FirstOrDefault();
		Vector2.GetLineIntersection(t.AbsoluteB, t.AbsoluteA, pointRotatedFromRelative3, pointRotatedFromRelative2, out intersection);
		return GetPointRotatedFromRelative(intersection, new Vector2(intersection.X - y, intersection.Y), Math.PI / 2.0 + t.Angle);
	}

	public Vector2 GetAlignmentForVector2(CollisionVector t, Vector2 alignmentPosition, double alignmentAngle)
	{
		double num = 0.0;
		num = CollisionRadius.Y;
		Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(alignmentPosition, new Vector2(alignmentPosition.X - num, alignmentPosition.Y), Math.PI / 2.0 + t.Angle);
		Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(alignmentPosition, new Vector2(alignmentPosition.X + num, alignmentPosition.Y), Math.PI / 2.0 + t.Angle);
		Vector2 intersection = Intersector.Intersection(t.AbsoluteA, t.AbsoluteB, pointRotatedFromRelative, pointRotatedFromRelative2).FirstOrDefault();
		Vector2.GetLineIntersection(t.AbsoluteA, t.AbsoluteB, pointRotatedFromRelative, pointRotatedFromRelative2, out intersection);
		return GetPointRotatedFromRelative(intersection, new Vector2(intersection.X - num, intersection.Y), Math.PI / 2.0 + t.Angle);
	}

	private void AcquireVector(CollisionVector vector)
	{
		GroundVector = vector;
		Mode = vector.Mode;
		ObjectLink = vector.Owner;
	}

	private void UpdateCollisionsWithObjects()
	{
		int num = RectangleCollisionRadius.Y - NormalCollisionRadius.Y;
		Rectangle rectangle = Rectangle.FromLTRB(PositionPrecise.X - RectangleCollisionRadius.X, PositionPrecise.Y - RectangleCollisionRadius.Y - num, PositionPrecise.X + RectangleCollisionRadius.X, PositionPrecise.Y + RectangleCollisionRadius.Y - num);
		foreach (ActiveObject activeObject in Level.ObjectManager.ActiveObjects)
		{
			CollisionRectangle[] collisionRectangles = activeObject.CollisionRectangles;
			foreach (CollisionRectangle collisionRectangle in collisionRectangles)
			{
				if (collisionRectangle.AbsoluteBounds.IntersectsWith(rectangle))
				{
					activeObject.Collision(new CollisionEvent(this, collisionRectangle.Id));
				}
			}
		}
	}

	private void UpdateDistanceFromLedge()
	{
		if (Mode == CollisionMode.Air || GroundVector == null)
		{
			DistanceFromLedge = 0.0;
			return;
		}
		if (Mode == CollisionMode.Left || Mode == CollisionMode.Right)
		{
			if (GroundVector.GetConnectionB(Path) == null)
			{
				double num = (PositionPrecise.Y - GroundVector.AbsoluteB.Y) * GroundVector.FlipY;
				if (num > 0.0)
				{
					DistanceFromLedge = num;
					return;
				}
			}
			if (GroundVector.GetConnectionA(Path) == null)
			{
				double num = (PositionPrecise.Y - GroundVector.AbsoluteA.Y) * GroundVector.FlipY;
				if (num < 0.0)
				{
					DistanceFromLedge = num;
					return;
				}
			}
		}
		else
		{
			if (GroundVector.GetConnectionB(Path) == null)
			{
				double num = (PositionPrecise.X - GroundVector.AbsoluteB.X) * GroundVector.FlipX;
				if (num > 0.0)
				{
					DistanceFromLedge = num;
					return;
				}
			}
			if (GroundVector.GetConnectionA(Path) == null)
			{
				double num = (PositionPrecise.X - GroundVector.AbsoluteA.X) * GroundVector.FlipX;
				if (num < 0.0)
				{
					DistanceFromLedge = num;
					return;
				}
			}
		}
		DistanceFromLedge = 0.0;
	}

	private void AdjustForPlatform(CollisionVector vector)
	{
		_lastPositionAdjusted = PositionPrecise;
		if (vector != null && vector != GroundVector)
		{
			if (vector.Owner is IPlatform { Velocity: var velocity })
			{
				_lastPositionAdjusted += velocity;
			}
			else if (vector.Owner != null)
			{
				Vector2 vector2 = vector.Owner.PositionPrecise - vector.Owner.LastPositionPrecise;
				_lastPositionAdjusted += vector2;
			}
			if (GroundVector != null && !IsAirborne)
			{
				PositionPrecise = _lastPositionAdjusted;
				PositionPrecise = GetAlignmentForVector2(GroundVector, PositionPrecise, vector.Angle);
			}
		}
	}

	private void AddPlatformVelocity()
	{
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.Conveyor))
		{
			Vector2 vector = default;
			bool flag = false;
			if (GroundVector.Owner is IPlatform)
			{
				vector = (GroundVector.Owner as IPlatform).Velocity;
			}
			else
			{
				Vector2.GetDistance(PositionPrecise, GroundVector.Bounds.Centre);
				flag = !GroundVector.Flags.HasFlag(CollisionFlags.NoAutoAlignment);
			}
			Vector2 vector2 = GroundVector.Owner.PositionPrecise - GroundVector.Owner.Position;
			PositionPrecise = Position + vector2;
			Velocity += vector;
			if (flag)
			{
				PositionPrecise = GetAlignmentForVector2(GroundVector, PositionPrecise, GroundAngle);
			}
		}
	}

	private void RemovePlatformVelocity()
	{
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.Conveyor))
		{
			Vector2 vector = default;
			vector = ((!(GroundVector.Owner is IPlatform)) ? (GroundVector.Owner.PositionPrecise - GroundVector.Owner.LastPositionPrecise) : (GroundVector.Owner as IPlatform).Velocity);
			Velocity -= vector;
		}
	}

	private bool IsCrushedBetweenFloors(Vector2[][] collisionBox, CollisionEvent top, CollisionEvent bottom)
	{
		bool result = false;
		Rectangle rectangle = Rectangle.FromLTRB(collisionBox[0][0].X, collisionBox[0][0].Y, collisionBox[1][0].X, collisionBox[1][0].Y);
		if (top != null)
		{
			Vector2 touch = top.CollisionInfo.Touch;
			if (rectangle.Contains(touch))
			{
				result = true;
			}
		}
		if (bottom != null)
		{
			Vector2 touch2 = bottom.CollisionInfo.Touch;
			if (rectangle.Contains(touch2))
			{
				result = true;
			}
		}
		return result;
	}

	private void CheckCrushed()
	{
		bool num = _collisionDetected[0] && _collisionDetected[2];
		bool flag = _collisionDetected[0] && _collisionDetectedTiles[2];
		bool flag2 = _collisionDetectedTiles[0] && _collisionDetected[2];
		bool flag3 = _collisionDetected[1] && _collisionDetected[3];
		bool flag4 = false;
		Vector2[][] collisionBox = GetCollisionBox(new Vector2(CollisionRadius.X - LedgeSensorRadius, CollisionRadius.Y), rotate: false);
		if (num)
		{
			CollisionEvent collisionEvent = _collisionEvents[0];
			CollisionEvent collisionEvent2 = _collisionEvents[2];
			if (collisionEvent.CollisionInfo.Vector != null && collisionEvent2.CollisionInfo.Vector != null && ((collisionEvent.CollisionInfo.Vector.Owner != null && collisionEvent2.CollisionInfo.Vector.Owner != null) || (collisionEvent.CollisionInfo.Vector.Owner != null && collisionEvent2.CollisionInfo.Vector.Owner == null) || (collisionEvent.CollisionInfo.Vector.Owner == null && collisionEvent2.CollisionInfo.Vector.Owner != null)) && collisionEvent.CollisionInfo.Vector.Owner != collisionEvent2.CollisionInfo.Vector.Owner)
			{
				bool flag5 = true;
				if (collisionEvent.CollisionInfo.Vector.Owner != null)
				{
					flag5 = collisionEvent.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent.CollisionInfo.Vector.Owner.LastPositionPrecise;
				}
				if (collisionEvent2.CollisionInfo.Vector.Owner != null)
				{
					flag5 = (collisionEvent2.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent2.CollisionInfo.Vector.Owner.LastPositionPrecise) | flag5;
				}
				if (flag5)
				{
					flag4 = IsCrushedBetweenFloors(collisionBox, collisionEvent, collisionEvent2);
				}
			}
		}
		else if (flag)
		{
			CollisionEvent collisionEvent3 = _collisionEvents[0];
			CollisionEvent bottom = _collisionEventsTiles[2];
			if (collisionEvent3.CollisionInfo.Vector != null && collisionEvent3.CollisionInfo.Vector.Owner != null && collisionEvent3.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent3.CollisionInfo.Vector.Owner.LastPositionPrecise)
			{
				flag4 = IsCrushedBetweenFloors(collisionBox, collisionEvent3, bottom);
			}
		}
		else if (flag2)
		{
			CollisionEvent top = _collisionEventsTiles[0];
			CollisionEvent collisionEvent4 = _collisionEvents[2];
			if (collisionEvent4.CollisionInfo.Vector != null && collisionEvent4.CollisionInfo.Vector.Owner != null && collisionEvent4.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent4.CollisionInfo.Vector.Owner.LastPositionPrecise)
			{
				flag4 = IsCrushedBetweenFloors(collisionBox, top, collisionEvent4);
			}
		}
		if (flag3)
		{
			Rectangle rectangle = Rectangle.FromLTRB(collisionBox[0][0].X, collisionBox[0][0].Y, collisionBox[1][0].X, collisionBox[1][0].Y);
			CollisionEvent collisionEvent5 = _collisionEvents[1];
			CollisionEvent collisionEvent6 = _collisionEvents[3];
			if (collisionEvent5 != null && collisionEvent5.CollisionInfo.Vector != null && collisionEvent5.CollisionInfo.Vector.Owner != null && collisionEvent5.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent5.CollisionInfo.Vector.Owner.LastPositionPrecise)
			{
				Vector2 touch = _collisionEvents[1].CollisionInfo.Touch;
				if (rectangle.Contains(touch))
				{
					flag4 = true;
				}
			}
			if (collisionEvent6 != null && collisionEvent6.CollisionInfo.Vector != null && collisionEvent6.CollisionInfo.Vector.Owner != null && collisionEvent6.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent6.CollisionInfo.Vector.Owner.LastPositionPrecise)
			{
				Vector2 touch2 = _collisionEvents[3].CollisionInfo.Touch;
				if (rectangle.Contains(touch2))
				{
					flag4 = true;
				}
			}
		}
		if (flag4)
		{
			Kill(PlayerDeathCause.Crushed);
		}
	}

	private static bool IsObjectMovingTowardsCollisionMode(IActiveObject obj, CollisionMode mode)
	{
		Vector2 vector = obj.PositionPrecise - obj.LastPositionPrecise;
		return mode switch
		{
			CollisionMode.Top => vector.Y < 0.0,
			CollisionMode.Left => vector.X < 0.0,
			CollisionMode.Bottom => vector.Y > 0.0,
			CollisionMode.Right => vector.X > 0.0,
			_ => false,
		};
	}

	public Character()
	{
		Input = new CharacterInputState();
		SetDefaultConstants();
	}

	protected override void OnStart()
	{
		ResourceTree resourceTree = ResourceTree;
		Animation = new AnimationInstance(resourceTree.GetLoadedResource<AnimationGroup>(AnimationGroupResourceKey));
		SpindashDustGroupResourceKey = "SONICORCA/OBJECTS/SPINDASH/ANIGROUP";
		SpindashDustAnimation = new AnimationInstance(resourceTree.GetLoadedResource<AnimationGroup>(SpindashDustGroupResourceKey));
		BrakeDustResourceKey = "SONICORCA/OBJECTS/DUST";
		InvincibilityGroupResourceKey = "SONICORCA/OBJECTS/INVINCIBILITY/ANIGROUP";
		_drowningAnimation = new AnimationInstance(resourceTree, "SONICORCA/HUD/DROWNING/ANIGROUP");
		Priority = 1024;
		StateFlags = 0;
		ShowAngle = 0.0;
		GroundVelocity = 0.0;
		Velocity = new Vector2(0.0, 0.0);
		SlopeLockTicks = 0;
		LookDirection = CharacterLookDirection.Normal;
		BreathTicks = 1800;
		CollisionRadius = NormalCollisionRadius;
		CheckCollision = true;
		CheckLandscapeCollision = true;
		CheckObjectCollision = true;
		InvulnerabilityTicks = 0;
		InitialiseInvincibility();
		Mode = CollisionMode.Air;
		_autoSidekickState = AutoSidekickState.Normal;
		ShouldReactToLevel = true;
		SpecialState = CharacterSpecialState.Normal;
	}

	protected override void OnStop()
	{
	}

	protected override void OnUpdate()
	{
		RecordHistory();
		HandleInput();
		if (IsDebug)
		{
			ShouldReactToLevel = false;
			UpdateDebugState();
		}
		else if (IsDying)
		{
			ShouldReactToLevel = false;
			UpdateDying();
			UpdateWaterLogic();
		}
		else if (IsSidekick)
		{
			UpdateSidekick();
		}
		else
		{
			ShouldReactToLevel = true;
			UpdateNormal();
		}
	}

	private void SetDefaultConstants()
	{
		LedgeSensorRadius = 9;
		NormalCollisionRadius = new Vector2i(40, 60);
		RectangleCollisionRadius = new Vector2i(44, 76);
		SpinballCollisionRadius = new Vector2i(34, 56);
		FloorSensorRadius = 32;
		CollisionSensorSize = 1;
		Path = 1;
	}

	public void HandleInput()
	{
		if (IsWinning || IsHurt || IsObjectControlled)
		{
			Input.Clear();
			LookDirection = CharacterLookDirection.Normal;
			return;
		}
		LookDirection = CharacterLookDirection.Normal;
		if (!IsAirborne && !IsSpinball && _balanceDirection == CharacterBalanceDirection.None)
		{
			double throttle = Input.Throttle;
			double num = Input.VerticalDirection;
			if (Math.Abs(num) > Math.Abs(throttle))
			{
				if (num <= -0.5)
				{
					LookDirection = CharacterLookDirection.Up;
				}
				else if (num >= 0.5)
				{
					LookDirection = CharacterLookDirection.Ducking;
				}
				Input.Throttle = 0.0;
			}
		}
		if (Level.ClassicDebugMode)
		{
			if (Input.B == CharacterInputButtonState.Pressed)
			{
				GroundVelocity += 64 * Facing;
			}
			if (Input.C == CharacterInputButtonState.Pressed)
			{
				IsDebug = !IsDebug;
				LeaveGround();
			}
			Input.B = CharacterInputButtonState.Up;
			Input.C = CharacterInputButtonState.Up;
		}
	}

	private void RecordHistory()
	{
		Array.Copy(_history, 0, _history, 1, _history.Length - 1);
		_history[0] = new CharacterHistoryItem(PositionPrecise, StateFlags, Input);
	}

	private void UpdateNormal()
	{
		if (IsWinning)
		{
			GroundVelocity = 0.0;
		}
		UpdateVariables();
		UpdateMovement();
		UpdateSpindash();
		UpdateSpinning();
		UpdateJumping();
		UpdateFalling();
		UpdatePosition();
		UpdateBalance();
		UpdateLooking();
		UpdatePushing();
		UpdateWaterLogic();
		if (InvulnerabilityTicks > 0)
		{
			InvulnerabilityTicks--;
		}
	}

	protected override void OnUpdateCollision()
	{
		if (!IsSidekick || (_autoSidekickState != AutoSidekickState.Spawning && _autoSidekickState != AutoSidekickState.Flying))
		{
			if (!IsDebug && !IsDying && !IsDead)
			{
				ProcessCollision();
			}
			if (!IsDying && !IsDead)
			{
				ApplyLevelBounds();
			}
			SetCameraTracking();
			CharacterEvents &= ~CharacterEvent.Hurt;
		}
	}

	private void UpdateVariables()
	{
		Acceleration = 0.1875;
		Deceleration = 2.0;
		Friction = Acceleration;
		TopSpeed = 24.0;
		JumpForce = 26.0;
		JumpReleaseForce = 16.0;
		Gravity = 0.875;
		if (HasSpeedShoes)
		{
			Acceleration = 0.375;
			Friction = 0.375;
			TopSpeed = 48.0;
		}
		if (Mode == CollisionMode.Air)
		{
			if (IsRollJumping)
			{
				Acceleration = 0.0;
			}
			else
			{
				Acceleration *= 2.0;
			}
		}
		else if (IsSpinball)
		{
			Acceleration = 0.0;
			Deceleration = 0.5;
			Friction /= 2.0;
		}
		else if (MovingDirection != 0)
		{
			Friction = 0.0;
		}
		if (IsUnderwater)
		{
			Acceleration *= 0.5;
			Deceleration *= 0.5;
			Friction *= 0.5;
			TopSpeed *= 0.5;
			JumpForce *= 2205.0 / 4095.0;
			JumpReleaseForce *= 0.5;
			Gravity = 0.25;
		}
		if (IsHurt)
		{
			Gravity = 0.75;
			Acceleration = 0.0;
			Deceleration = 0.0;
		}
		else if (IsCharging)
		{
			Acceleration = 0.0;
			Deceleration = 0.0;
		}
		if (SlopeLockTicks != 0)
		{
			if (Mode == CollisionMode.Air)
			{
				SlopeLockTicks = 0;
				return;
			}
			SlopeLockTicks--;
			Deceleration = 0.0;
		}
	}

	private void UpdateMovement()
	{
		if (Mode == CollisionMode.Air)
		{
			Vector2 velocity = Velocity;
			if (MovingDirection != 0)
			{
				velocity.X = ChangeSpeed(velocity.X, MovingDirection, TopSpeed, Acceleration, Acceleration, 0.0);
				Facing = MovingDirection;
			}
			velocity.Y = ChangeSpeed(velocity.Y, 1, 64.0, Gravity, Gravity, 0.0);
			if (velocity.Y < 0.0 && velocity.Y > 0.0 - JumpReleaseForce)
			{
				velocity.X *= 31.0 / 32.0;
			}
			Velocity = velocity;
		}
		else
		{
			if (!IsCharging)
			{
				ApplySlopeFriction();
			}
			GroundVelocity = ChangeSpeed(GroundVelocity, MovingDirection, TopSpeed, Acceleration, Deceleration, Friction);
			if (MovingDirection * GroundVelocity > 0.0)
			{
				Facing = MovingDirection;
			}
			UpdateBraking();
		}
	}

	private void UpdateBraking()
	{
		if (MovingDirection == Math.Sign(GroundVelocity) || GroundVelocity == 0.0 || Acceleration == 0.0 || IsSpinball || SlopeLockTicks > 0)
		{
			IsBraking = false;
			return;
		}
		if (!IsBraking && MovingDirection != 0 && Math.Abs(GroundVelocity) >= 16.0)
		{
			IsBraking = true;
			_brakeDustDelay = 0;
			_brakeTicks = 33;
			Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/BRAKE");
		}
		if (!IsBraking)
		{
			return;
		}
		_brakeTicks--;
		if (_brakeTicks == 0)
		{
			IsBraking = false;
			return;
		}
		_brakeDustDelay--;
		if (_brakeDustDelay <= 0)
		{
			_brakeDustDelay = 6;
			Level.ObjectManager.AddObject(new ObjectPlacement(BrakeDustResourceKey, Level.Map.Layers.IndexOf(Layer), new Vector2i(Position.X, Position.Y + CollisionRadius.Y)));
		}
		if (Math.Abs(GroundVelocity) < 4.0)
		{
			IsBraking = false;
		}
	}

	private void ApplySlopeFriction()
	{
		if (GroundVector == null || GroundVector.Flags.HasFlag(CollisionFlags.NoAngle) || GroundAngle == 0.0)
		{
			return;
		}
		double num = 0.0 - Math.Sin(GroundAngle);
		if (IsSpinball)
		{
			num = ((!(GroundVelocity * num > 0.0)) ? (num * 1.25) : (num * 0.2873));
		}
		else
		{
			num *= 0.5;
			if (GroundVelocity == 0.0 && Math.Abs(num) <= 0.05)
			{
				return;
			}
		}
		_ = GroundVelocity;
		GroundVelocity -= num;
	}

	private void UpdateSpindash()
	{
		if (IsCharging)
		{
			LookDelay = 120;
			_spindashExtraSpeed *= 31.0 / 32.0;
			if (Input.ABC == CharacterInputButtonState.Pressed)
			{
				_spindashExtraSpeed = Math.Min(_spindashExtraSpeed + 4.0, 16.0);
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPINDASH/CHARGE");
			}
			if (LookDirection != CharacterLookDirection.Ducking)
			{
				IsCharging = false;
				GroundVelocity = (32.0 + _spindashExtraSpeed) * Facing;
				CharacterEvents |= CharacterEvent.SpindashLaunch;
				CameraProperties.Delay = new Vector2i(16, CameraProperties.Delay.Y);
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPINDASH/RELEASE");
			}
		}
		else if (!IsSpinball && Mode != CollisionMode.Air && LookDirection == CharacterLookDirection.Ducking && Input.ABC == CharacterInputButtonState.Pressed)
		{
			IsCharging = true;
			_spindashExtraSpeed = 0.0;
			Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPINDASH/CHARGE");
		}
	}

	private void UpdateSpinning()
	{
		if (Mode == CollisionMode.Air)
		{
			return;
		}
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.ForceRoll))
		{
			UpdateSpinningForced();
		}
		else if (IsSpinball)
		{
			if (Math.Abs(GroundVelocity) < 2.0)
			{
				IsSpinball = false;
			}
		}
		else if ((LookDirection == CharacterLookDirection.Ducking || CharacterEvents.HasFlag(CharacterEvent.SpindashLaunch)) && Math.Abs(GroundVelocity) > 4.0)
		{
			IsSpinball = true;
			if (CharacterEvents.HasFlag(CharacterEvent.SpindashLaunch))
			{
				CharacterEvents &= ~CharacterEvent.SpindashLaunch;
			}
			else
			{
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPINBALL");
			}
		}
	}

	private void UpdateSpinningForced()
	{
		if (!IsSpinball)
		{
			IsSpinball = true;
			Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPINBALL");
		}
		if (Math.Abs(GroundVelocity) < 2.0 && !ShouldFallOffGround())
		{
			int num = Math.Sign(GroundVelocity);
			if (num == 0)
			{
				num = Facing;
			}
			GroundVelocity = 16 * num;
		}
	}

	private void UpdateJumping()
	{
		if (IsHurt)
		{
			return;
		}
		if (IsJumping)
		{
			if (!Input.ABC.HasFlag(CharacterInputButtonState.Down))
			{
				if (Velocity.Y < 0.0 - JumpReleaseForce)
				{
					Velocity = new Vector2(Velocity.X, 0.0 - JumpReleaseForce);
				}
				IsJumping = false;
			}
		}
		else
		{
			if (Input.ABC != CharacterInputButtonState.Pressed || IsCharging)
			{
				return;
			}
			if (Mode == CollisionMode.Air)
			{
				if (!Jumped)
				{
					return;
				}
				if (CanFly && _humanControlled)
				{
					CharacterEvents |= CharacterEvent.Fly;
				}
				else if (Barrier != BarrierType.None && Barrier != BarrierType.Classic)
				{
					if (!HasPerformedBarrierAttack)
					{
						HasPerformedBarrierAttack = true;
						CharacterEvents |= CharacterEvent.BarrierAttack;
					}
				}
				else if (Level.ClassicDebugMode && IsSpinball)
				{
					IsJumping = true;
					CharacterEvents |= CharacterEvent.DoubleJump;
					Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/JUMP");
				}
			}
			else if (GroundVector != null && !GroundVector.Flags.HasFlag(CollisionFlags.PreventJump))
			{
				Jumped = true;
				IsJumping = true;
				if (!IsAirborne && IsSpinball)
				{
					IsRollJumping = true;
				}
				IsSpinball = true;
				CharacterEvents |= CharacterEvent.Fall;
				CharacterEvents |= CharacterEvent.Jump;
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/JUMP");
			}
		}
	}

	private void UpdateFalling()
	{
		if (Mode != CollisionMode.Top && Mode != CollisionMode.Air && !(Math.Abs(GroundVelocity) >= 10.0))
		{
			if ((GroundVector == null || !GroundVector.Flags.HasFlag(CollisionFlags.DontFall)) && ShouldFallOffGround())
			{
				CharacterEvents |= CharacterEvent.Fall;
			}
			else if (SlopeLockTicks == 0)
			{
				SlopeLockTicks = 32;
			}
		}
	}

	private bool ShouldFallOffGround()
	{
		return Math.Cos(GroundAngle) <= 0.0001;
	}

	private void UpdatePosition()
	{
		if (Mode != CollisionMode.Air)
		{
			GroundVelocity = Math.Sign(GroundVelocity) * Math.Min(Math.Abs(GroundVelocity), 96.0);
			Velocity = new Vector2(MathX.Clamp(-64.0, GroundVelocity * Math.Cos(GroundAngle), 64.0), MathX.Clamp(-64.0, GroundVelocity * Math.Sin(GroundAngle), 64.0));
			AddPlatformVelocity();
			if (CharacterEvents.HasFlag(CharacterEvent.Jump))
			{
				double num = GroundAngle;
				if (GroundVector.Flags.HasFlag(CollisionFlags.NoAngle))
				{
					num = 0.0;
				}
				Velocity += new Vector2(JumpForce * Math.Sin(num), JumpForce * (0.0 - Math.Cos(num)));
				CharacterEvents &= ~CharacterEvent.Jump;
			}
			if (CharacterEvents.HasFlag(CharacterEvent.Fall))
			{
				LeaveGround();
				CharacterEvents &= ~CharacterEvent.Fall;
			}
		}
		CharacterEvents.HasFlag(CharacterEvent.Fly);
		if (CharacterEvents.HasFlag(CharacterEvent.BarrierAttack))
		{
			switch (Barrier)
			{
				case BarrierType.Bubble:
					Velocity = new Vector2(0.0, 32.0);
					break;
				case BarrierType.Fire:
					Velocity = new Vector2(32 * Facing, 0.0);
					break;
				case BarrierType.Lightning:
					Velocity = new Vector2(Velocity.X, -22.0);
					break;
			}
			CharacterEvents &= ~CharacterEvent.BarrierAttack;
		}
		if (CharacterEvents.HasFlag(CharacterEvent.DoubleJump))
		{
			Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y, 0.0 - JumpForce));
			CharacterEvents &= ~CharacterEvent.DoubleJump;
		}
		Velocity = new Vector2(Math.Sign(Velocity.X) * Math.Min(Math.Abs(Velocity.X), 96.0), Math.Sign(Velocity.Y) * Math.Min(Math.Abs(Velocity.Y), 96.0));
		bool flag = _collisionEvents[3] != null;
		bool flag2 = _collisionEvents[1] != null;
		double y = 0.0;
		bool flag3 = IsAttachedToPlatform();
		if (IsPushing && !flag3)
		{
			if (flag && _collisionEvents[3].CollisionInfo.Vector.Flags.HasFlag(CollisionFlags.Mobile))
			{
				y = Velocity.Y;
			}
			if (flag2 && _collisionEvents[1].CollisionInfo.Vector.Flags.HasFlag(CollisionFlags.Mobile))
			{
				y = Velocity.Y;
			}
		}
		else
		{
			y = Velocity.Y;
		}
		PositionPrecise += new Vector2(Velocity.X, y);
	}

	private void UpdateBalance()
	{
		if (Mode == CollisionMode.Air || GroundVelocity != 0.0)
		{
			_balanceDirection = CharacterBalanceDirection.None;
			return;
		}
		if (GroundVector.Flags.HasFlag(CollisionFlags.NoBalance))
		{
			_balanceDirection = CharacterBalanceDirection.None;
			return;
		}
		int num = Math.Sign(DistanceFromLedge);
		double num2 = Math.Abs(DistanceFromLedge);
		if (num2 > LedgeSensorRadius)
		{
			_balanceDirection = (CharacterBalanceDirection)(2 * num);
			Facing = num;
		}
		else if (num2 > 0.0)
		{
			_balanceDirection = (CharacterBalanceDirection)num;
		}
		else
		{
			_balanceDirection = CharacterBalanceDirection.None;
		}
	}

	private void UpdateLooking()
	{
		if ((_balanceDirection != CharacterBalanceDirection.None || GroundVelocity != 0.0) && LookDirection == CharacterLookDirection.Up)
		{
			LookDirection = CharacterLookDirection.Normal;
		}
		Vector2 offset = CameraProperties.Offset;
		switch (LookDirection)
		{
			case CharacterLookDirection.Up:
				LookDelay = Math.Max(0, LookDelay - 1);
				offset.Y = ((LookDelay == 0) ? Math.Max(-344.0, offset.Y - 8.0) : MathX.ChangeSpeed(offset.Y, -8.0));
				break;
			case CharacterLookDirection.Normal:
				LookDelay = 120;
				offset.Y = MathX.ChangeSpeed(offset.Y, -8.0);
				break;
			case CharacterLookDirection.Ducking:
				LookDelay = Math.Max(0, LookDelay - 1);
				offset.Y = ((LookDelay == 0) ? Math.Min(352.0, offset.Y + 8.0) : MathX.ChangeSpeed(offset.Y, -8.0));
				break;
		}
		CameraProperties.Offset = offset;
	}

	private void UpdatePushing()
	{
		if (Mode == CollisionMode.Air || MovingDirection == 0 || SlopeLockTicks > 0)
		{
			IsPushing = false;
			_pushTicks = 0;
			return;
		}
		if (Mode != CollisionMode.Top)
		{
			IsPushing = false;
			_pushTicks = 0;
			_pushDirection = 0;
			return;
		}
		bool flag = _collisionEvents[3] != null;
		bool flag2 = _collisionEvents[1] != null;
		if (((MovingDirection == -1) & flag) || ((MovingDirection == 1) & flag2))
		{
			IsPushing = true;
			_pushDirection = MovingDirection;
			_pushTicks = 12;
			return;
		}
		if (MovingDirection != _pushDirection)
		{
			IsPushing = false;
			_pushTicks = 0;
			_pushDirection = 0;
			return;
		}
		_pushTicks--;
		if (_pushTicks == 0)
		{
			IsPushing = false;
			_pushTicks = 0;
			_pushDirection = 0;
		}
	}

	private static double ChangeSpeed(double speed, int direction, double limit, double acceleration, double deceleration, double friction)
	{
		if (direction != 0)
		{
			double num = limit - speed * direction;
			if (num > limit)
			{
				speed += deceleration * direction;
			}
			else if (num > acceleration)
			{
				speed += acceleration * direction;
			}
			else if (num > 0.0)
			{
				speed = limit * direction;
			}
		}
		if (friction > 0.0)
		{
			speed = ((speed > friction) ? (speed - friction) : ((!(speed < 0.0 - friction)) ? 0.0 : (speed + friction)));
		}
		return speed;
	}

	private void ApplyLevelBounds()
	{
		if (Position.X - 36 < Level.Bounds.X)
		{
			PositionPrecise = new Vector2(Level.Bounds.X + 36, PositionPrecise.Y);
			Velocity = new Vector2(0.0, Velocity.Y);
			GroundVelocity = MathX.Clamp(-1.0, GroundVelocity, 0.0);
		}
		else if (Position.X + 36 > Level.Bounds.Right - 1)
		{
			PositionPrecise = new Vector2(Level.Bounds.Right - 1 - 36, PositionPrecise.Y);
			Velocity = new Vector2(0.0, Velocity.Y);
			GroundVelocity = MathX.Clamp(0.0, GroundVelocity, 1.0);
		}
		if (IsDebug)
		{
			if (Position.Y >= Level.Bounds.Bottom)
			{
				PositionPrecise = new Vector2(PositionPrecise.X, Level.Bounds.Bottom);
				Velocity = new Vector2(Velocity.X, 0.0);
			}
			if (Position.Y < Level.Bounds.Top)
			{
				PositionPrecise = new Vector2(PositionPrecise.X, Level.Bounds.Top);
				Velocity = new Vector2(Velocity.X, 0.0);
			}
		}
		else if (Position.Y >= Level.Bounds.Bottom)
		{
			Kill(PlayerDeathCause.BottomlessPit);
		}
	}

	private void SetCameraTracking()
	{
		CameraProperties cameraProperties = CameraProperties;
		cameraProperties.MaxVelocity = new Vector2(64.0, cameraProperties.MaxVelocity.Y);
		if (IsAirborne)
		{
			cameraProperties.Box = new Rectangle(-64.0, -192.0, 64.0, 256.0);
			cameraProperties.MaxVelocity = new Vector2(cameraProperties.MaxVelocity.X, 64.0);
		}
		else
		{
			Rectangle box = new Rectangle(-64.0, -16.0, 64.0, 0.0);
			if (IsSpinball)
			{
				box.Y += NormalCollisionRadius.Y - SpinballCollisionRadius.Y;
			}
			cameraProperties.Box = box;
			cameraProperties.MaxVelocity = new Vector2(cameraProperties.MaxVelocity.X, (Velocity.Y <= 6.0) ? 24.0 : 64.0);
		}
		if (IsDebug)
		{
			cameraProperties.MaxVelocity = new Vector2(Math.Max(64.0, Math.Abs(Velocity.X)), Math.Max(64.0, Math.Abs(Velocity.Y)));
		}
	}

	private void UpdateDebugState()
	{
		int protagonistGamepadIndex = Player.ProtagonistGamepadIndex;
		Controller controller = Level.GameContext.Current[protagonistGamepadIndex];
		Vector2 velocity = Velocity;
		if (controller.DirectionLeft.X == 0.0)
		{
			velocity.X = MathX.ChangeSpeed(Velocity.X, -0.1875);
		}
		else
		{
			velocity.X += controller.DirectionLeft.X * 0.1875 * 2.0;
		}
		if (controller.DirectionLeft.Y == 0.0)
		{
			velocity.Y = MathX.ChangeSpeed(Velocity.Y, -0.1875);
		}
		else
		{
			velocity.Y += controller.DirectionLeft.Y * 0.1875 * 2.0;
		}
		Velocity = velocity;
		UpdatePosition();
		ApplyLevelBounds();
		LookDelay = 120;
		CameraProperties.Delay = new Vector2i(0, 0);
		BreathTicks = 1800;
		if (IsUnderwater)
		{
			LeaveWater();
		}
	}

	public void DrawDebugInfo(Renderer renderer)
	{
		double num = 512.0;
		Level.DebugContext.DrawText(renderer, string.Join("  ", IsFacingLeft ? 'L' : 'R', IsBraking ? 'B' : ' ', IsPushing ? 'P' : ' ', IsAirborne ? 'A' : ' ', IsSpinball ? 'S' : ' ', IsRollJumping ? 'R' : ' ', IsDying ? 'D' : ' ', IsFlying ? 'F' : ' ', ExhibitsVirtualPlatform ? 'V' : ' ', HasSpeedShoes ? "SS" : "  ", IsInvincible ? 'I' : ' '), FontAlignment.MiddleX, 960.0, 16.0, 0.75, 0);
		num += Level.DebugContext.DrawText(renderer, $"LAYER: {Level.Map.Layers.IndexOf(Layer)}", FontAlignment.Left, 8.0, num, 0.75, 0);
		num += Level.DebugContext.DrawText(renderer, $"ANGLE: {GroundAngle:0.00} TUMBLE: {TumbleAngle:0.00}", FontAlignment.Left, 8.0, num, 0.75, 0);
		num += Level.DebugContext.DrawText(renderer, $"POSITION: {PositionPrecise.X:0.00}, {PositionPrecise.Y:0.00}", FontAlignment.Left, 8.0, num, 0.75, 0);
		num += Level.DebugContext.DrawText(renderer, $"VELOCITY: {Velocity.X:0.00}, {Velocity.Y:0.00}", FontAlignment.Left, 8.0, num, 0.75, 0);
		if (!IsAirborne)
		{
			num += Level.DebugContext.DrawText(renderer, $"GROUND VELOCITY: {GroundVelocity:0.00}", FontAlignment.Left, 8.0, num, 0.75, 0);
		}
		if (SlopeLockTicks > 0)
		{
			num += Level.DebugContext.DrawText(renderer, $"SLOPE LOCK: {SlopeLockTicks}", FontAlignment.Left, 8.0, num, 0.75, 0);
		}
		if (LookDelay < 120)
		{
			num += Level.DebugContext.DrawText(renderer, $"LOOK DELAY: {LookDelay}", FontAlignment.Left, 8.0, num, 0.75, 0);
		}
		if (IsCharging)
		{
			num += Level.DebugContext.DrawText(renderer, $"SPINDASH: {32.0 + _spindashExtraSpeed}", FontAlignment.Left, 8.0, num, 0.75, 0);
		}
		if (InvulnerabilityTicks > 0)
		{
			num += Level.DebugContext.DrawText(renderer, $"INVULNERABILITY: {InvulnerabilityTicks}", FontAlignment.Left, 8.0, num, 0.75, 0);
		}
		if (BreathTicks < 1800)
		{
			num += Level.DebugContext.DrawText(renderer, $"DROWNING: {BreathTicks}", FontAlignment.Left, 8.0, num, 0.75, 0);
		}
	}

	protected internal void DrawNewCollisionDebug(Renderer renderer)
	{
		if (IsSidekick)
		{
			return;
		}
		string[] array = new string[7]
		{
			"MODE: " + Mode,
			"POSITION: " + Position.X + ", " + Position.Y,
			"PRECISE: " + (PositionPrecise.X - Position.X) + ", " + (PositionPrecise.Y - Position.Y),
			"VELOCITY: " + (int)Velocity.X + ", " + (int)Velocity.Y,
			"GROUND VELOCITY: " + (int)GroundVelocity,
			"GROUND ID: " + Level.Map.CollisionVectors.IndexOf(GroundVector),
			"PATH: " + Path
		};
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ModelMatrix = Matrix4.Identity;
			double num = 16.0;
			string[] array2 = array;
			foreach (string text in array2)
			{
				num += Level.DebugContext.DrawText(renderer, text.ToUpper(), FontAlignment.Right, 1904.0, num, 0.5, 0);
			}
			i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(0.0 - Level.Camera.Bounds.X, 0.0 - Level.Camera.Bounds.Y);
			if ((Mode == CollisionMode.Top || Mode == CollisionMode.Bottom) && GroundVector != null)
			{
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(GroundVector.AbsoluteA.X, 0.0), new Vector2(GroundVector.AbsoluteA.X, 10000.0), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(GroundVector.AbsoluteA.X, 0.0), new Vector2(GroundVector.AbsoluteA.X, 10000.0), 4.0);
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(GroundVector.AbsoluteB.X, 0.0), new Vector2(GroundVector.AbsoluteB.X, 10000.0), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(GroundVector.AbsoluteB.X, 0.0), new Vector2(GroundVector.AbsoluteB.X, 10000.0), 4.0);
				string text2 = "{X: " + GroundVector.AbsoluteA.X + ", Y: " + GroundVector.AbsoluteA.Y + "}";
				Level.DebugContext.DrawText(renderer, text2.ToUpper(), FontAlignment.Right, GroundVector.AbsoluteA.X - 128, GroundVector.AbsoluteA.Y - 128, 0.5, 0);
				text2 = "{X: " + GroundVector.AbsoluteB.X + ", Y: " + GroundVector.AbsoluteB.Y + "}";
				Level.DebugContext.DrawText(renderer, text2.ToUpper(), FontAlignment.Right, GroundVector.AbsoluteB.X + 256, GroundVector.AbsoluteB.Y - 128, 0.5, 0);
			}
			else if ((Mode == CollisionMode.Left || Mode == CollisionMode.Right) && GroundVector != null)
			{
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(0.0, GroundVector.AbsoluteA.Y), new Vector2(100000.0, GroundVector.AbsoluteA.Y), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(0.0, GroundVector.AbsoluteA.Y), new Vector2(100000.0, GroundVector.AbsoluteA.Y), 4.0);
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(0.0, GroundVector.AbsoluteB.Y), new Vector2(100000.0, GroundVector.AbsoluteB.Y), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(0.0, GroundVector.AbsoluteB.Y), new Vector2(100000.0, GroundVector.AbsoluteB.Y), 4.0);
			}
			Vector2[][] collisionBox = GetCollisionBox(CollisionRadius, rotate: true);
			for (int i = 0; i < collisionBox.Length; i++)
			{
				_ = collisionBox[i];
			}
			new Rectangle(PositionPrecise.X - CollisionRadius.X, PositionPrecise.Y - CollisionRadius.Y, CollisionRadius.X * 2, CollisionRadius.Y * 2);
			double num2 = 0.0;
			double num3 = 0.0;
			bool flag = false;
			if (Mode == CollisionMode.Left || Mode == CollisionMode.Right)
			{
				num2 = CollisionRadius.Y;
				num3 = CollisionRadius.X;
				flag = true;
			}
			else
			{
				num2 = CollisionRadius.X;
				num3 = CollisionRadius.Y;
			}
			Vector2 pointRotatedFromRelative = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y + num2), Math.PI / 2.0 + GroundAngle);
			Vector2 pointRotatedFromRelative2 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y - num2), Math.PI / 2.0 + GroundAngle);
			Vector2 pointRotatedFromRelative3 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - num3, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2 pointRotatedFromRelative4 = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + num3, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2[][] collisionBox2 = GetCollisionBox(new Vector2(num2 - LedgeSensorRadius, num3 - FloorSensorRadius + 20.0), rotate: false);
			Vector2[][] collisionBox3 = GetCollisionBox(new Vector2(num2 - LedgeSensorRadius, num3), rotate: false);
			Vector2[][] array3 = new Vector2[0][];
			Vector2[][] array4 = new Vector2[0][];
			Vector2 vector = default;
			Vector2 vector2 = default;
			Vector2 vector3 = default;
			Vector2 vector4 = default;
			if (!flag)
			{
				array3 = collisionBox2;
				array4 = collisionBox3;
				vector = pointRotatedFromRelative;
				vector2 = pointRotatedFromRelative2;
				vector3 = pointRotatedFromRelative3;
				vector4 = pointRotatedFromRelative4;
			}
			else
			{
				array3 = collisionBox3;
				array4 = collisionBox2;
				vector = pointRotatedFromRelative3;
				vector2 = pointRotatedFromRelative4;
				vector3 = pointRotatedFromRelative;
				vector4 = pointRotatedFromRelative2;
			}
			i2dRenderer.RenderLine(Colour.ParseHex("00FF00"), vector, vector2, 5.0);
			i2dRenderer.RenderLine(Colour.ParseHex("00FF00"), vector3, vector4, 5.0);
			foreach (CollisionVector collisionVector2 in Level.Map.CollisionVectors)
			{
				if (Level.Camera.Bounds.IntersectsWith(collisionVector2.Bounds))
				{
					int num4 = 6;
					i2dRenderer.RenderLine(Colour.ParseHex("FF0000"), collisionVector2.AbsoluteA, collisionVector2.AbsoluteB, 5.0);
					i2dRenderer.RenderRectangle(Colour.ParseHex("FA35FF"), Rectangle.FromLTRB(collisionVector2.AbsoluteA.X - num4, collisionVector2.AbsoluteA.Y - num4, collisionVector2.AbsoluteA.X + num4, collisionVector2.AbsoluteA.Y + num4), num4 * 4);
					i2dRenderer.RenderRectangle(Colour.ParseHex("FA35FF"), Rectangle.FromLTRB(collisionVector2.AbsoluteB.X - num4, collisionVector2.AbsoluteB.Y - num4, collisionVector2.AbsoluteB.X + num4, collisionVector2.AbsoluteB.Y + num4), num4 * 4);
				}
			}
			foreach (ActiveObject activeObject in Level.ObjectManager.ActiveObjects)
			{
				CollisionVector[] collisionVectors = activeObject.CollisionVectors;
				foreach (CollisionVector collisionVector in collisionVectors)
				{
					i2dRenderer.RenderLine(Colour.ParseHex("FF00FF"), collisionVector.AbsoluteA, collisionVector.AbsoluteB, 5.0);
				}
				CollisionRectangle[] collisionRectangles = activeObject.CollisionRectangles;
				foreach (CollisionRectangle collisionRectangle in collisionRectangles)
				{
					i2dRenderer.RenderRectangle(Colour.ParseHex("FF00FF"), collisionRectangle.AbsoluteBounds, 5.0);
				}
			}
			if (GroundVector != null)
			{
				i2dRenderer.RenderLine(Colour.ParseHex("FFFFFF"), GroundVector.AbsoluteA, GroundVector.AbsoluteB, 5.0);
				if (GroundVector.GetConnectionA(Path) != null)
				{
					CollisionVector connectionA = GroundVector.GetConnectionA(Path);
					i2dRenderer.RenderLine(Colour.ParseHex("00FFFF"), connectionA.AbsoluteA, connectionA.AbsoluteB, 5.0);
				}
				if (GroundVector.GetConnectionB(Path) != null)
				{
					CollisionVector connectionB = GroundVector.GetConnectionB(Path);
					i2dRenderer.RenderLine(Colour.ParseHex("FFFF00"), connectionB.AbsoluteA, connectionB.AbsoluteB, 5.0);
				}
			}
			List<Vector2[]> list = new List<Vector2[]>();
			list.Add(new Vector2[2] { vector, vector2 });
			List<Vector2[]> list2 = list;
			list2.AddRange(array3);
			for (int j = 0; j < list2.Count; j++)
			{
				Vector2[] array5 = list2[j];
				i2dRenderer.RenderLine(Colour.ParseHex("00FF00"), array5[0], array5[1], 5.0);
			}
			list = new List<Vector2[]>();
			list.Add(new Vector2[2] { vector3, vector4 });
			list2 = list;
			list2.AddRange(array4);
			for (int k = 0; k < list2.Count; k++)
			{
				Vector2[] array6 = list2[k];
				i2dRenderer.RenderLine(Colour.ParseHex("0000FF"), array6[0], array6[1], 5.0);
			}
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!IsSidekick || _autoSidekickState != AutoSidekickState.Spawning)
		{
			if (IsHurt || InvulnerabilityTicks <= 0 || InvulnerabilityTicks % 8 >= 4)
			{
				DrawBody(renderer, viewOptions);
			}
			DrawSpindashDust(renderer);
			if (!viewOptions.Shadows)
			{
				DrawProtection(renderer);
				DrawBreathTimeRemaining(renderer);
			}
			_intersection -= PositionPrecise;
			renderer.Get2dRenderer().RenderRectangle(Colour.ParseHex("FF00FF"), Rectangle.FromLTRB(_intersection.X - 10.0, _intersection.Y - 10.0, _intersection.X + 10.0, _intersection.Y + 10.0), 40.0);
		}
	}

	protected virtual void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
		characterRenderer.ModelMatrix = i2dRenderer.ModelMatrix;
		double propertyDouble = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_hue_shift");
		double propertyDouble2 = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_sat_shift");
		double propertyDouble3 = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_lum_shift");
		if (DrawBodyRotated)
		{
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
		}
		Animation.Frame currentFrame = Animation.CurrentFrame;
		Vector2 vector = currentFrame.Offset;
		Rectangle destination = new Rectangle(vector.X - currentFrame.Source.Width / 2, vector.Y - currentFrame.Source.Height / 2, currentFrame.Source.Width, currentFrame.Source.Height);
		characterRenderer.Filter = viewOptions.Filter;
		characterRenderer.FilterAmount = viewOptions.FilterAmount;
		characterRenderer.Brightness = (viewOptions.Shadows ? 0f : Brightness);
		characterRenderer.RenderTexture(Animation.AnimationGroup.Textures[1], Animation.AnimationGroup.Textures[0], propertyDouble, propertyDouble2, propertyDouble3, currentFrame.Source, destination, IsFacingRight, IsFacingLeft && Animation.Index == 16);
	}

	protected void DrawSpindashDust(Renderer renderer)
	{
		if (IsCharging)
		{
			Vector2 destination = new Vector2(SpindashDustAnimation.CurrentFrame.Source.Width / 2, CollisionRadius.Y + 8.0 - SpindashDustAnimation.CurrentFrame.Source.Height / 2);
			if (IsFacingRight)
			{
				destination.X *= -1.0;
			}
			renderer.GetObjectRenderer().Render(SpindashDustAnimation, destination, IsFacingRight);
		}
	}

	protected void DrawProtection(Renderer renderer)
	{
		if (IsInvincible)
		{
			DrawInvincibility(renderer);
		}
		else
		{
			DrawBarrier(renderer);
		}
	}

	protected void DrawBarrier(Renderer renderer)
	{
		if (HasBarrier && BarrierAnimation != null)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.BlendMode = BlendMode.Additive;
			objectRenderer.FilterAmount *= 0.25;
			objectRenderer.Render(BarrierAnimation);
		}
	}

	protected void DrawBreathTimeRemaining(Renderer renderer)
	{
		if (_drowningClimax && _drowningAnimation.Cycles == 0)
		{
			I2dRenderer renderer2 = renderer.Get2dRenderer();
			Vector2i vector2i = _drownCountdownPosition - Position;
			_drowningAnimation.Draw(renderer2, vector2i);
		}
	}

	private void ScatterRings(int ringCount, int facing)
	{
		Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/RINGSCATTER");
		ringCount = Math.Min(ringCount, 32);
		for (int i = 0; i < ringCount; i++)
		{
			Vector2 velocity = ScatterRingOffsets[i];
			velocity.X *= facing;
			CreateRing(velocity);
		}
	}

	private void CreateRing(Vector2 velocity)
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(Level.CommonResources.GetResourcePath("ringobject"), Level.Map.Layers.IndexOf(Layer), Position, new
		{
			Scatter = true,
			Velocity = new Vector2(velocity.X, velocity.Y)
		}));
	}

	public void Hurt(int direction, PlayerDeathCause cause = PlayerDeathCause.Hurt)
	{
		if (!CanBeHurt)
		{
			return;
		}
		LeaveGround();
		if (IsSidekick)
		{
			Vector2 velocity = new Vector2(-8.0, -16.0);
			if (IsUnderwater)
			{
				velocity = new Vector2(velocity.X / 2.0, velocity.Y / 2.0);
			}
			if (direction >= 0)
			{
				velocity.X *= -1.0;
			}
			Velocity = velocity;
			Facing = -direction;
			IsAirborne = true;
			IsBraking = false;
			IsCharging = false;
			IsSpinball = false;
			IsFlying = false;
			IsHurt = true;
			ShowAngle = 0.0;
			TumbleAngle = 0.0;
			InvulnerabilityTicks = 120;
		}
		else if (Player.CurrentRings > 0 || HasBarrier)
		{
			Vector2 velocity2 = new Vector2(-8.0, -16.0);
			if (IsUnderwater)
			{
				velocity2 = new Vector2(velocity2.X / 2.0, velocity2.Y / 2.0);
			}
			if (direction >= 0)
			{
				velocity2.X *= -1.0;
			}
			Velocity = velocity2;
			Facing = -direction;
			IsAirborne = true;
			IsBraking = false;
			IsCharging = false;
			IsSpinball = false;
			IsFlying = false;
			IsHurt = true;
			ShowAngle = 0.0;
			TumbleAngle = 0.0;
			InvulnerabilityTicks = 120;
			if (HasBarrier)
			{
				Barrier = BarrierType.None;
				Level.SoundManager.PlaySound(this, (cause == PlayerDeathCause.Spikes) ? "SONICORCA/SOUND/HURT/SPIKES" : "SONICORCA/SOUND/HURT");
			}
			else
			{
				ScatterRings(Player.CurrentRings, -direction);
				Player.LoseAllRings();
			}
		}
		else
		{
			Kill(cause);
		}
		CharacterEvents |= CharacterEvent.Hurt;
	}

	public void Kill(PlayerDeathCause cause)
	{
		_deathCause = cause;
		Velocity = new Vector2(0.0, -28.0);
		_dyingTicks = 120;
		CameraProperties.Delay = new Vector2i(3600, 3600);
		StateFlags = 0;
		IsAirborne = true;
		IsBraking = false;
		IsCharging = false;
		IsFlying = false;
		IsHurt = false;
		IsDying = true;
		ShouldReactToLevel = false;
		ShowAngle = 0.0;
		Level.SoundManager.PlaySound(this, (cause == PlayerDeathCause.Spikes) ? "SONICORCA/SOUND/HURT/SPIKES" : "SONICORCA/SOUND/HURT");
		Priority = 4096;
	}

	private void UpdateDying()
	{
		if (_dyingTicks == 0)
		{
			StateFlags = 0;
			IsDead = true;
			return;
		}
		double num = ((_deathCause == PlayerDeathCause.Drown) ? 0.25 : 0.875);
		Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + num, 96.0));
		PositionPrecise += Velocity;
		_dyingTicks--;
	}

	private void InitialiseInvincibility()
	{
		AnimationGroup loadedResource = Level.GameContext.ResourceTree.GetLoadedResource<AnimationGroup>(InvincibilityGroupResourceKey);
		_invincibilityParticles.Clear();
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				double num = (j * 4 + i) / 12.0;
				_invincibilityParticles.Add(new InvincibilityParticle(this, num * (Math.PI * 2.0), loadedResource, i));
			}
		}
	}

	private void StartInvincibility()
	{
		_invincibilityCharacterPositionHistory.Clear();
		for (int i = 0; i < 8; i++)
		{
			_invincibilityCharacterPositionHistory.Add(Position);
		}
		_invincibilityCharacterPositionHistory.TrimExcess();
	}

	private void AnimateInvincibility()
	{
		if (_invincibilityCharacterPositionHistory.Count > 0)
		{
			_invincibilityCharacterPositionHistory.RemoveAt(_invincibilityCharacterPositionHistory.Count - 1);
		}
		_invincibilityCharacterPositionHistory.Insert(0, Position);
		for (int i = 0; i < 4; i++)
		{
			int num = i * 2;
			if (num < _invincibilityCharacterPositionHistory.Count)
			{
				Vector2 origin = _invincibilityCharacterPositionHistory[num];
				for (int j = 0; j < 3; j++)
				{
					int index = i * 3 + j;
					_invincibilityParticles[index].Origin = origin;
				}
			}
		}
		foreach (InvincibilityParticle invincibilityParticle in _invincibilityParticles)
		{
			invincibilityParticle.Animate();
		}
	}

	private void DrawInvincibility(Renderer renderer)
	{
		foreach (InvincibilityParticle invincibilityParticle in _invincibilityParticles)
		{
			invincibilityParticle.Draw(renderer);
		}
	}

	public bool IsOnScreen()
	{
		Rectangle rect = new Rectangle(Position.X - 128, Position.Y - 128, 256.0, 256.0);
		return Level.Camera.Bounds.IntersectsWith(rect);
	}

	private void UpdateSidekick()
	{
		if (!Level.StateFlags.HasFlag(LevelStateFlags.AllowCharacterControl))
		{
			UpdateNormal();
			return;
		}
		if (IsHumanInput)
		{
			_humanControlled = true;
			_humanInputTicksRemaining = 600;
		}
		else
		{
			_humanInputTicksRemaining--;
			if (_humanInputTicksRemaining <= 0)
			{
				_humanControlled = false;
			}
		}
		ICharacter protagonist = Player.Protagonist;
		CharacterHistoryItem characterHistoryItem = protagonist.History[17];
		switch (_autoSidekickState)
		{
			case AutoSidekickState.Spawning:
				if (!protagonist.IsDebug && (_humanControlled || Level.Ticks % 64 == 0))
				{
					_autoSidekickState = AutoSidekickState.Flying;
					StateFlags = 0;
					CollisionRadius = NormalCollisionRadius;
					Position = new Vector2i(protagonist.Position.X, protagonist.Position.Y - 1152);
					Priority = 8192;
					_sidekickOffscreenTicks = 300;
				}
				break;
			case AutoSidekickState.Flying:
				{
					if (IsOnScreen())
					{
						_sidekickOffscreenTicks = 300;
					}
					else
					{
						_sidekickOffscreenTicks--;
						if (_sidekickOffscreenTicks <= 0)
						{
							_autoSidekickState = AutoSidekickState.Spawning;
							Position = default;
							break;
						}
					}
					_sidekickTargetPosition = characterHistoryItem.Position;
					if (CanFly)
					{
						IsFlying = true;
						IsSpinball = false;
					}
					else
					{
						IsSpinball = true;
					}
					IsAirborne = true;
					IsHurt = false;
					IsDying = false;
					IsDead = false;
					ShouldReactToLevel = false;
					Mode = CollisionMode.Air;
					Vector2i offset = default;
					int num = _sidekickTargetPosition.X - Position.X;
					if (num != 0)
					{
						offset.X = Math.Sign(num) * Math.Min(48, Math.Abs(num) / 4);
						offset.X += (int)protagonist.Velocity.X + 4;
						Facing = Math.Sign(offset.X);
					}
					int num2 = protagonist.NormalCollisionRadius.Y - NormalCollisionRadius.Y;
					int num3 = _sidekickTargetPosition.Y - Position.Y + num2;
					if (num3 != 0)
					{
						offset.Y = Math.Sign(num3) * Math.Min(Math.Abs(num3), CanFly ? 4 : 24);
					}
					CharacterState state = characterHistoryItem.State;
					if ((state & (CharacterState.Dying | CharacterState.Dead)) == 0)
					{
						Move(offset);
					}
					if (Math.Abs(offset.X) <= 48 && offset.Y == 0 && (state & (CharacterState.Rolling | CharacterState.Airborne | CharacterState.Dying | CharacterState.Dead | CharacterState.Debug)) == 0)
					{
						Velocity = default;
						GroundVelocity = 0.0;
						Path = protagonist.Path;
						Layer = protagonist.Layer;
						Mode = CollisionMode.Air;
						_autoSidekickState = AutoSidekickState.Normal;
						goto case AutoSidekickState.Normal;
					}
					break;
				}
			case AutoSidekickState.Normal:
				if (protagonist.IsDying || protagonist.IsDead)
				{
					UpdateNormal();
					break;
				}
				ShouldReactToLevel = true;
				Priority = 1020;
				if (!IsUnableToCatchUp())
				{
					if (_humanControlled || IsObjectControlled || IsWinning || characterHistoryItem == null)
					{
						UpdateNormal();
						break;
					}
					if (SlopeLockTicks > 0)
					{
						_autoSidekickState = AutoSidekickState.Spindash;
						goto case AutoSidekickState.Spindash;
					}
					ApplyAutoInput(GetAutoInputNormal(protagonist, characterHistoryItem));
					UpdateNormal();
					break;
				}
				break;
			case AutoSidekickState.Spindash:
				if (!IsUnableToCatchUp())
				{
					if (SlopeLockTicks > 0)
					{
						UpdateNormal();
						break;
					}
					ApplyAutoInput(GetAutoInputSpindash(protagonist));
					UpdateNormal();
				}
				break;
		}
	}

	private CharacterInputState GetAutoInputNormal(ICharacter protagonist, CharacterHistoryItem protagonistHistory)
	{
		CharacterInputState characterInputState = new CharacterInputState(protagonistHistory.Input);
		IsFacingLeft = protagonistHistory.State.HasFlag(CharacterState.Left);
		int num = protagonistHistory.Position.X - Position.X;
		if (num == 0)
		{
			IsFacingLeft = protagonistHistory.State.HasFlag(CharacterState.Left);
		}
		else if (num > 192)
		{
			if (num >= 64)
			{
				characterInputState.HorizontalDirection = 1;
			}
			if (GroundVelocity != 0.0 && IsFacingRight)
			{
				Move(4, 0);
			}
		}
		else if (num < 128)
		{
			if (num <= -64)
			{
				characterInputState.HorizontalDirection = -1;
			}
			if (GroundVelocity != 0.0 && !IsFacingRight)
			{
				Move(-4, 0);
			}
		}
		if (_autoSidekickJumping)
		{
			characterInputState.A = CharacterInputButtonState.Down;
			if (IsAirborne)
			{
				return characterInputState;
			}
			_autoSidekickJumping = false;
		}
		if (Level.Ticks % 256 == 0 && num >= 256)
		{
			return characterInputState;
		}
		int num2 = protagonistHistory.Position.Y - Position.Y;
		if (num2 >= 0)
		{
			if (protagonist.IsPushing)
			{
				characterInputState.HorizontalDirection = Math.Sign(num);
			}
			return characterInputState;
		}
		if (num2 > -128)
		{
			if (protagonist.IsPushing && IsPushing && _pushDirection != protagonist.Facing && LookDirection != CharacterLookDirection.Ducking)
			{
				characterInputState.A = CharacterInputButtonState.Pressed;
				_autoSidekickJumping = true;
				return characterInputState;
			}
			return characterInputState;
		}
		if (Level.Ticks % 64 == 0 && LookDirection != CharacterLookDirection.Ducking)
		{
			characterInputState.A = CharacterInputButtonState.Pressed;
			_autoSidekickJumping = true;
		}
		return characterInputState;
	}

	private CharacterInputState GetAutoInputSpindash(ICharacter protagonist)
	{
		CharacterInputState characterInputState = new CharacterInputState();
		if (!IsCharging)
		{
			if (GroundVelocity != 0.0)
			{
				return characterInputState;
			}
			Facing = Math.Sign(protagonist.Position.X - Position.X);
			characterInputState.VerticalDirection = 1;
			characterInputState.A = CharacterInputButtonState.Pressed;
		}
		else
		{
			characterInputState.VerticalDirection = 1;
			if (Level.Ticks % 128 != 0)
			{
				if (Level.Ticks % 32 == 0)
				{
					characterInputState.A = CharacterInputButtonState.Pressed;
				}
			}
			else
			{
				_autoSidekickState = AutoSidekickState.Normal;
			}
		}
		return characterInputState;
	}

	private void ApplyAutoInput(CharacterInputState input)
	{
		LookDirection = (CharacterLookDirection)input.VerticalDirection;
		Input = input;
	}

	private bool IsUnableToCatchUp()
	{
		if (IsOnScreen())
		{
			_sidekickOffscreenTicks = 300;
		}
		else
		{
			_sidekickOffscreenTicks--;
		}
		if (IsDead || _sidekickOffscreenTicks <= 0)
		{
			_autoSidekickState = AutoSidekickState.Spawning;
			Position = new Vector2i(0, 0);
			return true;
		}
		return false;
	}

	private void DoInstaShield()
	{
	}

	private void DoFireBarrierMove()
	{
		GroundVelocity = 32 * Facing;
		Velocity = new Vector2(GroundVelocity, 0.0);
	}

	private void DoLightningBarrierMove()
	{
		Velocity = new Vector2(Velocity.X, -22.0);
		IsJumping = false;
	}

	private void DoBubbleBarrierMove()
	{
		GroundVelocity = 0.0;
		Velocity = new Vector2(0.0, 32.0);
	}

	private void DoSuperHyperTransform()
	{
	}

	private void DoHyperDash()
	{
	}

	private void UpdateWaterLogic()
	{
		if (IsDying && _deathCause == PlayerDeathCause.Drown)
		{
			IsUnderwater = true;
			if (_nextBubbleTime-- < 0)
			{
				_nextBubbleTime = Level.Random.Next(2, 8);
				CreateBubble(1);
			}
		}
		else
		{
			if (IsDying || IsDead)
			{
				return;
			}
			bool isUnderwater = IsUnderwater;
			bool flag = (IsUnderwater = Level.WaterManager.IsUnderwater(Position));
			bool flag3 = flag;
			if (!isUnderwater & flag3)
			{
				EnterWater();
			}
			else if (isUnderwater && !flag3)
			{
				LeaveWater();
			}
			if (flag3)
			{
				if (_nextBubbleTime-- < 0)
				{
					_nextBubbleTime = Level.Random.Next((Math.Abs(Velocity.X) > 4.0) ? 8 : 32, (Math.Abs(Velocity.X) > 4.0) ? 32 : 128);
					CreateBubble(0);
				}
				BreathTicks--;
				CheckForDrowning();
				if (Barrier == BarrierType.Fire || Barrier == BarrierType.Lightning)
				{
					Barrier = BarrierType.None;
				}
			}
			else
			{
				BreathTicks = 1800;
			}
		}
	}

	private void EnterWater()
	{
		Velocity = new Vector2(Velocity.X / 2.0, Velocity.Y / 4.0);
		if (IsAirborne && !IsObjectControlled)
		{
			Level.WaterManager.CreateSplash(Layer, SplashType.Enter, Position);
			Level.SoundManager.PlaySound("SONICORCA/SOUND/SPLASH");
		}
	}

	private void LeaveWater()
	{
		if (IsAirborne && Velocity.Y < 0.0)
		{
			Velocity = new Vector2(Velocity.X, Math.Max(Velocity.Y * 2.0, -48.0));
		}
		if (IsAirborne && !IsObjectControlled && !IsDying)
		{
			Level.WaterManager.CreateSplash(Layer, SplashType.Exit, Position);
			Level.SoundManager.PlaySound("SONICORCA/SOUND/SPLASH");
		}
		BreathTicks = 1800;
		if (_drowningClimax && !IsSidekick)
		{
			Level.SoundManager.StopJingle(JingleType.Drowning);
			Level.SoundManager.PlayMusic();
		}
	}

	private void CheckForDrowning()
	{
		if (BreathTicks > 720)
		{
			if (BreathLeftWarnings.Contains(BreathTicks) && !IsSidekick)
			{
				Level.SoundManager.PlaySound("SONICORCA/SOUND/DROWNWARNING");
			}
			_drowningClimax = false;
			return;
		}
		if (!_drowningClimax)
		{
			if (!IsSidekick)
			{
				Level.SoundManager.PlayJingle(JingleType.Drowning);
			}
			_drowningClimax = true;
		}
		if (BreathTicks <= 300 && BreathTicks % 60 == 0)
		{
			ShowBreathTimeRemaining(BreathTicks / 60);
		}
		if (BreathTicks <= -60)
		{
			Drown();
		}
	}

	private void Drown()
	{
		Kill(PlayerDeathCause.Drown);
		Velocity = default;
		if (!IsSidekick)
		{
			Level.SoundManager.StopJingle(JingleType.Drowning);
		}
		Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/DROWN");
	}

	private void ShowBreathTimeRemaining(int secondsLeft)
	{
		_drownCountdownValue = secondsLeft;
		_drownCountdownPosition = Position + new Vector2i(96, -96);
		if (secondsLeft >= 0 && secondsLeft <= 5)
		{
			_drowningAnimation.Index = 5 - secondsLeft;
			_drowningAnimation.Cycles = 0;
		}
	}

	private void CreateBubble(int size)
	{
		Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), Position + new Vector2i(8, -8), size);
	}

	public void InhaleOxygen()
	{
		BreathTicks = 1800;
		if (_drowningClimax && !IsSidekick)
		{
			Level.SoundManager.StopJingle(JingleType.Drowning);
			Level.SoundManager.PlayMusic();
		}
		IsSpinball = false;
		_inhalingBubble = 1;
		Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/INHALE");
	}
}
