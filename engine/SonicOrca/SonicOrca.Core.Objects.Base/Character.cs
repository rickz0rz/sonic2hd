using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The base class for a playable character. It contains the movement physics, the collision with the landscape and objects,
/// the sidekick AI, drowning, invincibility and the drawing of the character.
/// </summary>
public abstract class Character : ActiveObject, ICharacter, IActiveObject
{
	/// <summary>
	/// Finds the intersection of two line segments.
	/// </summary>
	public class Intersector
	{
		private const double Epsilon = 1E-05;

		/// <summary>
		/// Clips the interval between the two values to the range 0 to 1.
		/// Returns no value, one value or the two ends of the clipped interval.
		/// </summary>
		private static double[] OverlapIntervals(double ub1, double ub2)
		{
			double low = Math.Min(ub1, ub2);
			double high = Math.Max(ub1, ub2);
			double start = Math.Max(0.0, low);
			double end = Math.Min(1.0, high);
			if (start > end)
			{
				return new double[0];
			}
			if (start != end)
			{
				return new double[2] { start, end };
			}
			return new double[1] { start };
		}

		/// <summary>
		/// Finds the overlap of two collinear segments.
		/// </summary>
		private static Vector2[] OneD_Intersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
		{
			double dx = a2.X - a1.X;
			double dy = a2.Y - a1.Y;
			double ub;
			double ub2;
			if (Math.Abs(dx) > Math.Abs(dy))
			{
				ub = (b1.X - a1.X) / dx;
				ub2 = (b2.X - a1.X) / dx;
			}
			else
			{
				ub = (b1.Y - a1.Y) / dy;
				ub2 = (b2.Y - a1.Y) / dy;
			}
			List<Vector2> points = new List<Vector2>();
			double[] intervals = OverlapIntervals(ub, ub2);
			for (int i = 0; i < intervals.Length; i++)
			{
				// The interpolation uses single precision, as the original code does.
				float t = (float)intervals[i];
				double x = a2.X * (double)t + a1.X * (double)(1f - t);
				double y = a2.Y * (double)t + a1.Y * (double)(1f - t);
				points.Add(new Vector2(x, y));
			}
			return points.ToArray();
		}

		private static bool PointOnLine(Vector2 p, Vector2 a1, Vector2 a2)
		{
			double u = 0.0;
			return DistFromSeg(p, a1, a2, Epsilon, ref u) < Epsilon;
		}

		/// <summary>
		/// Gets the distance from a point to the infinite line through q0 and q1.
		/// </summary>
		private static double DistFromSeg(Vector2 p, Vector2 q0, Vector2 q1, double radius, ref double u)
		{
			double dx = q1.X - q0.X;
			double dy = q1.Y - q0.Y;
			double offsetX = q0.X - p.X;
			double offsetY = q0.Y - p.Y;
			double length = Math.Sqrt(dx * dx + dy * dy);
			if (length < Epsilon)
			{
				throw new Exception("Expected line segment, not point.");
			}
			return Math.Abs(dx * offsetY - offsetX * dy) / length;
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
			double numeratorA = (b2.X - b1.X) * (a1.Y - b1.Y) - (b2.Y - b1.Y) * (a1.X - b1.X);
			double numeratorB = (a2.X - a1.X) * (a1.Y - b1.Y) - (a2.Y - a1.Y) * (a1.X - b1.X);
			double denominator = (b2.Y - b1.Y) * (a2.X - a1.X) - (b2.X - b1.X) * (a2.Y - a1.Y);
			if (!(0.0 - Epsilon < denominator) || !(denominator < Epsilon))
			{
				// The segments are not parallel.
				double ua = numeratorA / denominator;
				double ub = numeratorB / denominator;
				if (0.0 <= ua && ua <= 1.0 && 0.0 <= ub && ub <= 1.0)
				{
					return new Vector2[1]
					{
						new Vector2(a1.X + ua * (a2.X - a1.X), a1.Y + ua * (a2.Y - a1.Y))
					};
				}
				return new Vector2[0];
			}
			if ((0.0 - Epsilon < numeratorA && numeratorA < Epsilon) || (0.0 - Epsilon < numeratorB && numeratorB < Epsilon))
			{
				// The segments are collinear.
				if (a1.Equals(a2))
				{
					return OneD_Intersection(b1, b2, a1, a2);
				}
				return OneD_Intersection(a1, a2, b1, b2);
			}
			return new Vector2[0];
		}
	}

	/// <summary>
	/// One star of the invincibility effect. It turns around an origin that follows the character with a delay.
	/// </summary>
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
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation, Origin - _character.Position + _position);
			objectRenderer.EmitsLight = false;
		}
	}

	/// <summary>
	/// The state of the computer-controlled sidekick.
	/// </summary>
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

	private CollisionVector _originalGroundVector;

	private CollisionVector _overlappingGroundVector;

	private KeyValuePair<ActiveObject, CollisionEvent> _overlappingCollision;

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

	/// <summary>
	/// The draw priority of a character in normal play.
	/// </summary>
	private const int NormalPriority = 1024;

	private const int SidekickPriority = 1020;

	private const int DyingPriority = 4096;

	private const int SidekickFlyingPriority = 8192;

	/// <summary>
	/// The number of ticks that a character must look up or down before the camera moves.
	/// </summary>
	private const int DefaultLookDelay = 120;

	/// <summary>
	/// The base ground speed of a spindash release, before the extra charge speed.
	/// </summary>
	private const double SpindashBaseSpeed = 32.0;

	/// <summary>
	/// The number of ticks that a character cannot be hurt after it was hurt.
	/// </summary>
	private const int HurtInvulnerabilityTicks = 120;

	/// <summary>
	/// The number of ticks of the death animation before the character is dead.
	/// </summary>
	private const int DyingTicks = 120;

	/// <summary>
	/// The minimum distance from the side of the level to the centre of a character.
	/// </summary>
	private const int LevelBoundsMargin = 36;

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

	private const int MaximumScatteredRings = 32;

	/// <summary>
	/// The start velocities of the rings that a hurt character loses. The first 16 are fast and the last 16 are slow.
	/// </summary>
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

	/// <summary>
	/// The number of ticks that the sidekick stays behind the protagonist. The sidekick follows this history item.
	/// </summary>
	private const int SidekickHistoryDelay = 17;

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
			StateFlags = value ? (StateFlags | CharacterState.Left) : (StateFlags & ~CharacterState.Left);
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
			StateFlags = value ? (StateFlags & ~CharacterState.Left) : (StateFlags | CharacterState.Left);
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
			StateFlags = value ? (StateFlags | CharacterState.Hurt) : (StateFlags & ~CharacterState.Hurt);
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
			StateFlags = value ? (StateFlags | CharacterState.Winning) : (StateFlags & ~CharacterState.Winning);
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
			if (SpecialState == CharacterSpecialState.Grabbed)
			{
				Animation.Index = (int)CharacterAnimation.Grabbed;
				Animation.Animate();
			}
			else if (SpecialState == CharacterSpecialState.Hanging)
			{
				Animation.Index = (int)CharacterAnimation.Hanging;
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
				Animation.Index = (int)CharacterAnimation.Drown;
			}
			else
			{
				Animation.Index = (int)CharacterAnimation.Death;
			}
		}
		else
		{
			if (IsDebug)
			{
				Animation.Index = (int)CharacterAnimation.Roll;
				return;
			}
			if (IsHurt)
			{
				Animation.Index = (int)CharacterAnimation.Hurt;
			}
			else if (IsWinning)
			{
				Animation.Index = (int)CharacterAnimation.Win;
			}
			else if (IsFlying)
			{
				Animation.Index = (int)CharacterAnimation.Flying;
			}
			else if (IsCharging)
			{
				Animation.Index = (int)CharacterAnimation.Spindash;
			}
			else if (IsSpinball)
			{
				Animation.Index = (int)CharacterAnimation.Roll;
				Animation.OverrideDelay = (int)(Math.Max(0.0, 20.0 - Math.Abs(GroundVelocity)) / 5.0);
			}
			else if ((Animation.Index != (int)CharacterAnimation.Spring && Animation.Index != (int)CharacterAnimation.Postspring) || !IsAirborne)
			{
				// A spring animation continues until the character lands.
				if (LookDirection == CharacterLookDirection.Up)
				{
					Animation.Index = (int)CharacterAnimation.Lookup;
				}
				else if (LookDirection == CharacterLookDirection.Ducking)
				{
					Animation.Index = (int)CharacterAnimation.Duck;
				}
				else if (IsPushing)
				{
					Animation.Index = (int)CharacterAnimation.Push;
					Animation.OverrideDelay = 8;
					DrawBodyRotated = true;
				}
				else if (IsBraking)
				{
					Animation.Index = (int)CharacterAnimation.Stop;
					DrawBodyRotated = true;
				}
				else if (GroundVelocity == 0.0)
				{
					switch (Math.Abs((int)_balanceDirection))
					{
						case 1:
							// Near the ledge. Use the reverse animation when the character faces away from the ledge.
							if ((IsFacingLeft && _balanceDirection > CharacterBalanceDirection.None) || (IsFacingRight && _balanceDirection < CharacterBalanceDirection.None))
							{
								Animation.Index = (int)CharacterAnimation.BalanceFarReverse;
							}
							else
							{
								Animation.Index = (int)CharacterAnimation.BalanceFar;
							}
							break;
						case 2:
							Animation.Index = (int)CharacterAnimation.BalanceClose;
							break;
						default:
							if (IsAirborne)
							{
								Animation.Index = (int)CharacterAnimation.Postspring;
							}
							else if (Animation.Index == (int)CharacterAnimation.Win)
							{
								Animation.Index = (int)CharacterAnimation.WinToStand;
							}
							else if (Animation.Index != (int)CharacterAnimation.WinToStand)
							{
								Animation.Index = (int)CharacterAnimation.Wait;
							}
							break;
					}
				}
				else if (_inhalingBubble != 0)
				{
					// 1: the inhale animation must start. 2: the inhale animation plays.
					if (_inhalingBubble == 1)
					{
						_inhalingBubble = 2;
						Animation.Index = (int)CharacterAnimation.Inhale;
					}
					else if (Animation.Index != (int)CharacterAnimation.Inhale)
					{
						_inhalingBubble = 0;
					}
				}
				else
				{
					bool setRunDelay = true;
					// Only Sonic as the protagonist has the wait-to-stand and Mach animations.
					bool isSonicProtagonist = Player.ProtagonistCharacterType == CharacterType.Sonic && this == Player.Protagonist;
					if (Math.Abs(GroundVelocity) < 24.0)
					{
						if (isSonicProtagonist)
						{
							if (Animation.Index == (int)CharacterAnimation.Wait && Animation.CurrentFrameIndex >= 248)
							{
								Animation.Index = (int)CharacterAnimation.WaitToStand;
							}
							else if (Animation.Index == (int)CharacterAnimation.WaitToStand)
							{
								GroundVelocity = 0.0;
							}
							else
							{
								Animation.Index = (int)CharacterAnimation.Walk;
							}
						}
						else
						{
							Animation.Index = (int)CharacterAnimation.Walk;
						}
					}
					else if (Math.Abs(GroundVelocity) >= 48.0)
					{
						if (isSonicProtagonist)
						{
							Animation.Index = (int)CharacterAnimation.Mach1;
						}
						else
						{
							Animation.Index = (int)CharacterAnimation.Run;
						}
						setRunDelay = false;
					}
					else if (isSonicProtagonist)
					{
						if (Animation.Index == (int)CharacterAnimation.Mach1)
						{
							Animation.Index = (int)CharacterAnimation.Mach1ToRun;
						}
						else
						{
							Animation.Index = (int)CharacterAnimation.Run;
						}
					}
					else
					{
						Animation.Index = (int)CharacterAnimation.Run;
					}
					if (setRunDelay)
					{
						// The animation is faster when the character moves faster.
						Animation.OverrideDelay = (int)(Math.Max(0.0, 32.0 - Math.Abs(GroundVelocity)) / 8.0);
					}
					DrawBodyRotated = true;
				}
			}
		}
		UpdateRotation();
		if (!IsSpinball && TumbleAngle != 0.0 && !IsHurt && !IsDying)
		{
			// Select the tumble frame from the tumble angle, instead of animating it.
			Animation.Index = (int)CharacterAnimation.Tumble;
			double tumblePosition = MathX.Clamp(TumbleAngle, 1.0);
			tumblePosition = (tumblePosition >= 0.0) ? (tumblePosition / 2.0) : ((1.0 - Math.Abs(tumblePosition)) / 2.0 + 0.5);
			if (IsFacingLeft)
			{
				tumblePosition = 1.0 - tumblePosition;
				tumblePosition = MathX.Wrap(tumblePosition + 0.5, 1.0);
			}
			Animation.CurrentFrameIndex = (int)((Animation.AnimationGroup[Animation.Index].Frames.Count - 1) * tumblePosition);
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
				TumbleAngle = MathX.ChangeSpeed(TumbleAngle, -TumbleSpeed);
			}
			else if (TumbleTurns < 0)
			{
				TumbleAngle -= TumbleSpeed;
				if (TumbleAngle < 0.0)
				{
					TumbleAngle++;
					TumbleTurns++;
				}
			}
			else if (TumbleTurns > 0)
			{
				TumbleAngle += TumbleSpeed;
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
			BarrierType.Classic => DefaultBarrierClassicResourceKey,
			BarrierType.Bubble => DefaultBarrierBubbleResourceKey,
			BarrierType.Fire => DefaultBarrierFireResourceKey,
			BarrierType.Lightning => DefaultBarrierLightningResourceKey,
			_ => null,
		};
	}

	private void ProcessCollision()
	{
		ResetCollisionEvents();
		// Leave the ground when the object that owns the ground vector removed it.
		if (GroundVector != null && GroundVector.Owner != null)
		{
			if (!GroundVector.Owner.CollisionVectors.Contains(GroundVector))
			{
				LeaveGround();
			}
		}
		if (CheckCollision)
		{
			_originalGroundVector = (Mode == CollisionMode.Air) ? null : GroundVector;
			if (GroundVector != null && Mode >= CollisionMode.Top && Mode <= CollisionMode.Right)
			{
				ConstrainToPath();
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
		}
		_overlappingGroundVector = null;
		_overlappingCollision = new KeyValuePair<ActiveObject, CollisionEvent>(null, null);
	}

	private bool CheckIgnoreFlag(CollisionVector t)
	{
		return t.Flags.HasFlag(CollisionFlags.Ignore);
	}

	private bool CheckSolidAngle(CollisionVector t, Vector2 intersection, Vector2 collisionRadius)
	{
		if (t.Flags.HasFlag(CollisionFlags.Solid))
		{
			return false;
		}
		Vector2 edgePoint = default;
		if (t.Mode == CollisionMode.Right)
		{
			edgePoint = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X, intersection.Y - collisionRadius.X), Math.PI / 2.0 + GroundAngle);
		}
		else if (t.Mode == CollisionMode.Left)
		{
			edgePoint = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X, intersection.Y + collisionRadius.X), Math.PI / 2.0 + GroundAngle);
		}
		else if (t.Mode == CollisionMode.Top)
		{
			edgePoint = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X - collisionRadius.Y, intersection.Y), Math.PI / 2.0 + GroundAngle);
		}
		else if (t.Mode == CollisionMode.Bottom)
		{
			edgePoint = GetPointRotatedFromRelative(intersection, new Vector2(intersection.X + collisionRadius.Y, intersection.Y), Math.PI / 2.0 + GroundAngle);
		}
		// The vector is not solid from the side where the character is.
		return MathX.DifferenceRadians((PositionPrecise - edgePoint).Angle, t.Angle) > 0.0;
	}

	/// <summary>
	/// Rotates a point around a centre by an angle in radians.
	/// </summary>
	private Vector2 GetPointRotatedFromRelative(Vector2 relative, Vector2 point, double theta)
	{
		double px = point.X;
		double py = point.Y;
		double cx = relative.X;
		double cy = relative.Y;
		double rotatedX = Math.Cos(theta) * (px - cx) - Math.Sin(theta) * (py - cy) + cx;
		double rotatedY = Math.Sin(theta) * (px - cx) + Math.Cos(theta) * (py - cy) + cy;
		return new Vector2(rotatedX, rotatedY);
	}

	/// <summary>
	/// Gets the four edges of a box around the character: top, right, bottom and left.
	/// Each edge is an array of two corners.
	/// </summary>
	public Vector2[][] GetCollisionBox(Vector2 collisionRadius, bool rotate)
	{
		double theta = 0.0;
		if (rotate)
		{
			theta = GroundAngle;
		}
		Vector2 topLeft = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - collisionRadius.X, PositionPrecise.Y - collisionRadius.Y), theta);
		Vector2 topRight = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + collisionRadius.X, PositionPrecise.Y - collisionRadius.Y), theta);
		Vector2 bottomRight = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + collisionRadius.X, PositionPrecise.Y + collisionRadius.Y), theta);
		Vector2 bottomLeft = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - collisionRadius.X, PositionPrecise.Y + collisionRadius.Y), theta);
		return new Vector2[4][]
		{
			new Vector2[2] { topLeft, topRight },
			new Vector2[2] { bottomRight, topRight },
			new Vector2[2] { bottomLeft, bottomRight },
			new Vector2[2] { bottomLeft, topLeft }
		};
	}

	/// <summary>
	/// Returns true when the character has moved past the B end of the ground vector.
	/// </summary>
	private bool IsBeyondPathEnd(bool useLedgeSensor)
	{
		if (LastPositionPrecise == PositionPrecise)
		{
			return false;
		}
		int ledgeInset = useLedgeSensor ? (CollisionRadius.X - LedgeSensorRadius) : 0;
		Vector2 sensorStart = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2 sensorEnd = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2i absoluteA = GroundVector.AbsoluteA;
		Vector2i absoluteB = GroundVector.AbsoluteB;
		if (Vector2.Intersects(sensorStart, sensorEnd, absoluteA, absoluteB))
		{
			return false;
		}
		Vector2.GetLineIntersection(sensorStart, sensorEnd, absoluteA, absoluteB, out Vector2 intersection);
		double distanceToCentre = Vector2.GetDistance(intersection, GroundVector.Bounds.Centre);
		double distanceToA = Vector2.GetDistance(intersection, absoluteA);
		double distanceToB = Vector2.GetDistance(intersection, absoluteB);
		double halfLength = Vector2.GetDistance(GroundVector.Bounds.Centre, absoluteB);
		if (distanceToB < distanceToA && distanceToCentre - ledgeInset > halfLength)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// Returns true when the character has moved past the A end of the ground vector.
	/// </summary>
	private bool IsBeyondPathStart(bool useLedgeSensor)
	{
		int ledgeInset = useLedgeSensor ? (CollisionRadius.X - LedgeSensorRadius) : 0;
		Vector2 sensorStart = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2 sensorEnd = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + CollisionRadius.Y, PositionPrecise.Y), GroundAngle + Math.PI / 2.0);
		Vector2i absoluteA = GroundVector.AbsoluteA;
		Vector2i absoluteB = GroundVector.AbsoluteB;
		if (Vector2.Intersects(sensorStart, sensorEnd, absoluteA, absoluteB))
		{
			return false;
		}
		Vector2.GetLineIntersection(sensorStart, sensorEnd, absoluteA, absoluteB, out Vector2 intersection);
		double distanceToCentre = Vector2.GetDistance(intersection, GroundVector.Bounds.Centre);
		double distanceToA = Vector2.GetDistance(intersection, GroundVector.AbsoluteA);
		double distanceToB = Vector2.GetDistance(intersection, GroundVector.AbsoluteB);
		double halfLength = Vector2.GetDistance(GroundVector.Bounds.Centre, absoluteA);
		if (distanceToB > distanceToA && distanceToCentre - ledgeInset > halfLength)
		{
			return true;
		}
		return false;
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
					Velocity = new Vector2(0.0, Velocity.Y);
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
		// The last mode with a viable landing wins.
		CollisionMode landingMode = CollisionMode.Air;
		for (int i = 0; i < 4; i++)
		{
			if (_collisionEvents[i] != null && !_collisionEvents[i].MaintainVelocity && IsLandingViable(_collisionEvents[i].CollisionInfo.Vector))
			{
				landingMode = (CollisionMode)i;
			}
		}
		if (landingMode != CollisionMode.Air)
		{
			if (CharacterEvents.HasFlag(CharacterEvent.Hurt))
			{
				return result;
			}
			bool wasAirborne = Mode == CollisionMode.Air;
			CollisionVector vector = _collisionEvents[(int)landingMode].CollisionInfo.Vector;
			if (vector != _originalGroundVector)
			{
				PositionPrecise = GetAlignmentForVector2(vector, PositionPrecise, GroundAngle);
				AcquireVector(vector);
				if (wasAirborne)
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
			if (Level.Ticks - LastTickOnGround > KeepSpinballAirborneTolerance)
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
				double snappedAngle = MathX.Snap(GroundAngle, Math.PI / 2.0);
				Velocity = new Vector2(GroundVelocity * Math.Cos(snappedAngle), GroundVelocity * Math.Sin(snappedAngle));
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

	/// <summary>
	/// Moves the character to the next connected vector when it goes past an end of the ground vector.
	/// </summary>
	private void ConstrainToPath()
	{
		CollisionVector nextVector = null;
		bool isBeyondPath = true;
		if (IsBeyondPathEnd(useLedgeSensor: false))
		{
			nextVector = FindClosestConnectedVector(towardsB: true, ref _lastLeftVector);
		}
		else if (IsBeyondPathStart(useLedgeSensor: false))
		{
			nextVector = FindClosestConnectedVector(towardsB: false, ref _lastRightVector);
		}
		else
		{
			_lastRightVector = null;
			_lastLeftVector = null;
			isBeyondPath = false;
		}
		if (nextVector != null)
		{
			if (!nextVector.Flags.HasFlag(CollisionFlags.NoPathFollowing))
			{
				PositionPrecise = GetAlignmentForVector2(nextVector, PositionPrecise, GroundAngle);
				AcquireVector(nextVector);
			}
			else if (isBeyondPath)
			{
				LeaveGround();
			}
		}
		else if (isBeyondPath)
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

	/// <summary>
	/// Follows the connections of the ground vector in one direction while each vector is closer to the character than the previous one.
	/// Returns the closest vector that is not the last vector found in this direction, or null.
	/// </summary>
	private CollisionVector FindClosestConnectedVector(bool towardsB, ref CollisionVector lastVector)
	{
		CollisionVector vector = GroundVector;
		double closestDistance = double.MaxValue;
		CollisionVector closestVector = null;
		while (true)
		{
			vector = towardsB ? vector.GetConnectionB(Path) : vector.GetConnectionA(Path);
			if (vector == null)
			{
				break;
			}
			double distance = GetDistanceToVector(vector);
			if (!(distance < closestDistance))
			{
				break;
			}
			closestDistance = distance;
			if (lastVector != vector)
			{
				closestVector = lastVector = vector;
			}
		}
		return closestVector;
	}

	/// <summary>
	/// Checks the character against each collision vector. It pushes the character out of walls, floors and ceilings,
	/// records the collision events and sends the collisions to the owners of the vectors.
	/// </summary>
	/// <remarks>
	/// The sensor names are for a character that stands on flat ground.
	/// The method uses <see cref="CollisionRadius"/> and not the <paramref name="collisionRadius"/> parameter.
	/// The returned list is always empty.
	/// </remarks>
	private IEnumerable<CollisionVector> FixPosition(Vector2 collisionRadius, IEnumerable<CollisionVector> vectorsToCheck)
	{
		List<CollisionVector> result = new List<CollisionVector>();
		// The largest push in each direction so far. Each vector applies only the change to these values.
		double leftPush = 0.0;
		double rightPush = 0.0;
		double upPush = 0.0;
		double downPush = 0.0;
		// This value stays true after the first vector that is checked while the character is on a wall.
		bool isWallMode = false;
		foreach (CollisionVector item in vectorsToCheck)
		{
			double halfWidth;
			double halfHeight;
			if (Mode == CollisionMode.Left || Mode == CollisionMode.Right)
			{
				isWallMode = true;
				halfWidth = CollisionRadius.Y;
				halfHeight = CollisionRadius.X;
			}
			else
			{
				halfWidth = CollisionRadius.X;
				halfHeight = CollisionRadius.Y;
			}
			Vector2 leftSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y + halfWidth), Math.PI / 2.0 + GroundAngle);
			Vector2 rightSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y - halfWidth), Math.PI / 2.0 + GroundAngle);
			Vector2 bottomSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + halfHeight, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2 topSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - halfHeight, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			// The short box does not reach the floor. The narrow box does not reach past the ledge sensors.
			Vector2[][] shortBox = GetCollisionBox(new Vector2(halfWidth, halfHeight - FloorSensorRadius + 20.0), rotate: false);
			Vector2[][] narrowBox = GetCollisionBox(new Vector2(halfWidth - LedgeSensorRadius, halfHeight), rotate: false);
			Vector2[][] wallBoxEdges;
			Vector2[][] floorBoxEdges;
			Vector2 wallLineStart;
			Vector2 wallLineEnd;
			Vector2 floorLineStart;
			Vector2 floorLineEnd;
			if (isWallMode)
			{
				wallBoxEdges = narrowBox;
				floorBoxEdges = shortBox;
				wallLineStart = bottomSensor;
				wallLineEnd = topSensor;
				floorLineStart = leftSensor;
				floorLineEnd = rightSensor;
			}
			else
			{
				wallBoxEdges = shortBox;
				floorBoxEdges = narrowBox;
				wallLineStart = leftSensor;
				wallLineEnd = rightSensor;
				floorLineStart = bottomSensor;
				floorLineEnd = topSensor;
			}
			if (!CanCollideWithVector(item))
			{
				continue;
			}
			Vector2[] vectorEnds = new Vector2[2] { item.AbsoluteA, item.AbsoluteB };
			if (CheckIgnoreFlag(item))
			{
				continue;
			}
			if (item.IsWall)
			{
				List<Vector2[]> wallLines = new List<Vector2[]> { new Vector2[2] { wallLineStart, wallLineEnd } };
				wallLines.AddRange(wallBoxEdges);
				List<Vector2> wallIntersections = GetSensorIntersections(wallLines, vectorEnds, out _);
				bool crossesWallSensor = wallIntersections.Count > 0;
				if (!crossesWallSensor)
				{
					// No sensor line crosses the vector. Check if an end of the vector is in the box.
					// Edge 0 starts at the top-left corner and edge 1 starts at the bottom-right corner.
					Rectangle wallArea = Rectangle.FromLTRB(wallBoxEdges[0][0].X, wallBoxEdges[0][0].Y, wallBoxEdges[1][0].X, wallBoxEdges[1][0].Y);
					if (wallArea.ContainsOrOverlaps(vectorEnds[0]))
					{
						Vector2 perpendicularPoint = GetPointRotatedFromRelative(vectorEnds[0], new Vector2(vectorEnds[0].X - 5.0, vectorEnds[0].Y), Math.PI / 2.0);
						Vector2.GetLineIntersection(vectorEnds[0], perpendicularPoint, wallLines[0][0], wallLines[0][1], out Vector2 intersection);
						wallIntersections.Add(intersection);
						crossesWallSensor = true;
					}
					if (wallArea.ContainsOrOverlaps(vectorEnds[1]))
					{
						Vector2 perpendicularPoint = GetPointRotatedFromRelative(vectorEnds[1], new Vector2(vectorEnds[1].X - 5.0, vectorEnds[1].Y), Math.PI / 2.0);
						Vector2.GetLineIntersection(vectorEnds[1], perpendicularPoint, wallLines[0][0], wallLines[0][1], out Vector2 intersection);
						wallIntersections.Add(intersection);
						crossesWallSensor = true;
					}
				}
				if (!crossesWallSensor)
				{
					continue;
				}
				double positionX = PositionPrecise.X;
				double rightPushBefore = rightPush;
				double leftPushBefore = leftPush;
				Vector2 wallContact = new Vector2(-1.0, -1.0);
				if (item.Mode == CollisionMode.Right)
				{
					wallContact = GetPointWithLowestX(wallIntersections);
					Vector2 alignedPosition = GetPointRotatedFromRelative(wallContact, new Vector2(wallContact.X, wallContact.Y - halfWidth), Math.PI / 2.0 + GroundAngle);
					if (alignedPosition.X - positionX >= rightPush)
					{
						rightPush = alignedPosition.X - positionX;
					}
				}
				else if (item.Mode == CollisionMode.Left)
				{
					wallContact = GetPointWithHighestX(wallIntersections);
					Vector2 alignedPosition = GetPointRotatedFromRelative(wallContact, new Vector2(wallContact.X, wallContact.Y + halfWidth), Math.PI / 2.0 + GroundAngle);
					if (alignedPosition.X - positionX < leftPush)
					{
						leftPush = alignedPosition.X - positionX;
					}
				}
				if (!CheckSolidAngle(item, wallContact, new Vector2(halfWidth, halfHeight)))
				{
					double wallDistance = Vector2.GetDistance(rightSensor, wallContact);
					CollisionInfo wallInfo = new CollisionInfo(item, wallContact, wallDistance, item.Angle);
					CollisionEvent wallEvent = new CollisionEvent(this, wallInfo);
					if (item.Owner != null)
					{
						item.Owner.Collision(wallEvent);
					}
					if (!wallEvent.IgnoreCollision)
					{
						_collisionDetected[(int)item.Mode] = true;
						_collisionEvents[(int)item.Mode] = wallEvent;
					}
					Vector2 wallPosition = PositionPrecise;
					double rightPushChange = 0.0;
					double leftPushChange = 0.0;
					if (rightPushBefore != rightPush)
					{
						rightPushChange = rightPush - rightPushBefore;
					}
					if (leftPushBefore != leftPush)
					{
						leftPushChange = leftPush - leftPushBefore;
					}
					if (rightPushChange != 0.0 || leftPushChange != 0.0)
					{
						wallPosition.X += rightPushChange;
						wallPosition.X += leftPushChange;
						PositionPrecise = wallPosition;
					}
				}
				continue;
			}
			List<Vector2[]> floorLines = new List<Vector2[]> { new Vector2[2] { floorLineStart, floorLineEnd } };
			floorLines.AddRange(floorBoxEdges);
			List<Vector2> floorIntersections = GetSensorIntersections(floorLines, vectorEnds, out bool checkedBoxEdges);
			bool crossesFloorSensor = floorIntersections.Count > 0;
			if (!crossesFloorSensor)
			{
				// No sensor line crosses the vector. Check if an end of the vector is in the box.
				Rectangle floorArea = Rectangle.FromLTRB(floorBoxEdges[0][0].X, floorBoxEdges[0][0].Y, floorBoxEdges[1][0].X, floorBoxEdges[1][0].Y);
				if (floorArea.ContainsOrOverlaps(vectorEnds[0]))
				{
					Vector2.GetLineIntersection(GetPointRotatedFromRelative(vectorEnds[0], new Vector2(vectorEnds[0].X, vectorEnds[0].Y + 5.0), Math.PI / 2.0), vectorEnds[0], floorLines[0][0], floorLines[0][1], out Vector2 intersection);
					floorIntersections.Add(intersection);
					crossesFloorSensor = true;
				}
				if (floorArea.ContainsOrOverlaps(vectorEnds[1]))
				{
					Vector2.GetLineIntersection(GetPointRotatedFromRelative(vectorEnds[1], new Vector2(vectorEnds[1].X, vectorEnds[1].Y + 5.0), Math.PI / 2.0), vectorEnds[1], floorLines[0][0], floorLines[0][1], out Vector2 intersection);
					floorIntersections.Add(intersection);
					crossesFloorSensor = true;
				}
			}
			if (!crossesFloorSensor)
			{
				continue;
			}
			Vector2 floorContact = new Vector2(-1.0, -1.0);
			if (ShouldDisregardAsTooHighStep(item))
			{
				continue;
			}
			// This is the vertical part of the ground velocity on flat ground.
			double groundVelocityY = MathX.Clamp(-64.0, GroundVelocity * Math.Sin(0.0), 64.0);
			double verticalVelocity = (!IsAirborne) ? groundVelocityY : Velocity.Y;
			double positionY = PositionPrecise.Y;
			double upPushBefore = upPush;
			double downPushBefore = downPush;
			if (item.Mode == CollisionMode.Top)
			{
				floorContact = GetPointWithHighestY(floorIntersections);
				if (verticalVelocity >= 0.0)
				{
					Vector2 alignedPosition = GetPointRotatedFromRelative(floorContact, new Vector2(floorContact.X - halfHeight, floorContact.Y), Math.PI / 2.0 + GroundAngle);
					if (alignedPosition.Y - positionY < upPush)
					{
						upPush = alignedPosition.Y - positionY;
						// Remember a floor that overlaps the ground vector. CheckOverlappingVector can move the character onto it.
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
				floorContact = GetPointWithHighestY(floorIntersections);
				if (verticalVelocity < 0.0)
				{
					Vector2 alignedPosition = GetPointRotatedFromRelative(floorContact, new Vector2(floorContact.X + halfHeight, floorContact.Y), Math.PI / 2.0 + GroundAngle);
					if (alignedPosition.Y - positionY > downPush)
					{
						downPush = alignedPosition.Y - positionY;
					}
				}
			}
			if (CheckSolidAngle(item, floorContact, new Vector2(halfWidth, halfHeight)))
			{
				continue;
			}
			double floorDistance = Vector2.GetDistance(topSensor, floorContact);
			CollisionInfo floorInfo = new CollisionInfo(item, floorContact, floorDistance, item.Angle);
			CollisionEvent floorEvent = new CollisionEvent(this, floorInfo);
			if (checkedBoxEdges && GroundVector != null && (item.Mode == CollisionMode.Top || item.Mode == CollisionMode.Bottom) && item.Owner == null && !IsFlying)
			{
				// A landscape floor or ceiling while on the ground. Keep it only for the crush check.
				_collisionDetectedTiles[(int)item.Mode] = true;
				_collisionEventsTiles[(int)item.Mode] = floorEvent;
				continue;
			}
			if (item.Owner != null)
			{
				KeyValuePair<ActiveObject, CollisionEvent> ownerCollision = new KeyValuePair<ActiveObject, CollisionEvent>(item.Owner, floorEvent);
				if (_overlappingGroundVector != null && item.Owner == _overlappingGroundVector.Owner)
				{
					// CheckOverlappingVector sends this collision later.
					_overlappingCollision = ownerCollision;
				}
				else
				{
					ownerCollision.Key.Collision(ownerCollision.Value);
				}
			}
			if (!floorEvent.IgnoreCollision)
			{
				_collisionDetected[(int)item.Mode] = true;
				_collisionEvents[(int)item.Mode] = floorEvent;
			}
			Vector2 floorPosition = PositionPrecise;
			double upPushChange = 0.0;
			double downPushChange = 0.0;
			if (upPushBefore != upPush)
			{
				upPushChange = upPush - upPushBefore;
			}
			if (downPushBefore != downPush)
			{
				downPushChange = downPush - downPushBefore;
			}
			if (upPushChange != 0.0 || downPushChange != 0.0)
			{
				floorPosition.Y += upPushChange;
				floorPosition.Y += downPushChange;
				PositionPrecise = floorPosition;
			}
		}
		return result;
	}

	/// <summary>
	/// Gets the points where the sensor lines cross a collision vector.
	/// </summary>
	/// <param name="checkedBoxEdges">True when the loop got to the box edges after the first sensor line.</param>
	private static List<Vector2> GetSensorIntersections(List<Vector2[]> sensorLines, Vector2[] vectorEnds, out bool checkedBoxEdges)
	{
		List<Vector2> intersections = new List<Vector2>();
		checkedBoxEdges = false;
		for (int i = 0; i < sensorLines.Count; i++)
		{
			if (Vector2.Intersects(sensorLines[i][0], sensorLines[i][1], vectorEnds[0], vectorEnds[1]))
			{
				Vector2.GetLineIntersection(sensorLines[i][0], sensorLines[i][1], vectorEnds[0], vectorEnds[1], out Vector2 intersection);
				intersections.Add(intersection);
			}
			checkedBoxEdges = i >= 1;
		}
		return intersections;
	}

	/// <summary>
	/// Gets the first point with the lowest X. The list must not be empty.
	/// </summary>
	private static Vector2 GetPointWithLowestX(List<Vector2> points)
	{
		Vector2 result = points.First();
		foreach (Vector2 point in points.Skip(1))
		{
			if (point.X < result.X)
			{
				result = point;
			}
		}
		return result;
	}

	/// <summary>
	/// Gets the first point with the highest X. The list must not be empty.
	/// </summary>
	private static Vector2 GetPointWithHighestX(List<Vector2> points)
	{
		Vector2 result = points.First();
		foreach (Vector2 point in points.Skip(1))
		{
			if (point.X > result.X)
			{
				result = point;
			}
		}
		return result;
	}

	/// <summary>
	/// Gets the first point with the highest Y. The list must not be empty.
	/// </summary>
	private static Vector2 GetPointWithHighestY(List<Vector2> points)
	{
		Vector2 result = points.First();
		foreach (Vector2 point in points.Skip(1))
		{
			if (point.Y > result.Y)
			{
				result = point;
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
		// The owner of the vector can remove it during the collision.
		bool vectorStillExists = true;
		if (_overlappingGroundVector.Owner != null)
		{
			vectorStillExists = _overlappingGroundVector.Owner.CollisionVectors.Contains(_overlappingGroundVector);
		}
		PositionPrecise = GetAlignmentForVector2(_overlappingGroundVector, PositionPrecise, _overlappingGroundVector.Angle);
		if (vectorStillExists)
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

	/// <summary>
	/// Returns true when the character stands on a moving conveyor vector.
	/// </summary>
	private bool IsAttachedToPlatform()
	{
		Vector2 platformVelocity = default;
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.Conveyor))
		{
			platformVelocity = GetGroundOwnerVelocity();
		}
		return platformVelocity != default(Vector2);
	}

	/// <summary>
	/// Gets the velocity of the object that owns the ground vector.
	/// </summary>
	private Vector2 GetGroundOwnerVelocity()
	{
		if (GroundVector.Owner is IPlatform platform)
		{
			return platform.Velocity;
		}
		return GroundVector.Owner.PositionPrecise - GroundVector.Owner.LastPositionPrecise;
	}

	private void Bump(Vector2 collisionRadius)
	{
		Rectangle searchArea = new Rectangle(PositionPrecise.X - 128.0, PositionPrecise.Y - 128.0, 256.0, 256.0);
		IEnumerable<CollisionVector> vectors = Level.CollisionTable.GetPossibleCollisionIntersections(searchArea, CheckLandscapeCollision, CheckObjectCollision);
		bool attachedToPlatform = IsAttachedToPlatform();
		IEnumerable<CollisionVector> orderedVectors;
		if ((Math.Abs(Velocity.X) >= Math.Abs(Velocity.Y)) | attachedToPlatform)
		{
			// Walls first, then landscape vectors and top floors, then the other object vectors.
			orderedVectors = vectors.OrderBy(t => (!t.IsWall) ? ((t.Mode == CollisionMode.Top || t.Owner == null) ? 1 : 2) : 0);
		}
		else
		{
			// Landscape vectors and top floors first, then walls, then the other object vectors.
			orderedVectors = vectors.OrderBy(t => t.IsWall ? 1 : ((t.Mode != CollisionMode.Top && t.Owner != null) ? 2 : 0));
		}
		FixPosition(collisionRadius, orderedVectors);
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
		return vector.HasPath(Path);
	}

	private double GetDistanceToVector(CollisionVector t)
	{
		return Vector2.GetDistance(PositionPrecise, t.Bounds.Centre);
	}

	public Vector2 GetProjectedPointOnLine(Vector2 v1, Vector2 v2, Vector2 point)
	{
		Vector2 lineDirection = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
		Vector2 pointOffset = new Vector2(point.X - v1.X, point.Y - v1.Y);
		double dot = lineDirection.Dot(pointOffset);
		double lineLength = Math.Sqrt(lineDirection.X * lineDirection.X + lineDirection.Y * lineDirection.Y);
		double offsetLength = Math.Sqrt(pointOffset.X * pointOffset.X + pointOffset.Y * pointOffset.Y);
		double projectedLength = dot / (lineLength * offsetLength) * offsetLength;
		return new Vector2(v1.X + projectedLength * lineDirection.X / lineLength, v1.Y + projectedLength * lineDirection.Y / lineLength);
	}

	/// <summary>
	/// Does the same as <see cref="GetProjectedPointOnLine"/>.
	/// </summary>
	public Vector2 GetProjectedPointOnLine2(Vector2 v1, Vector2 v2, Vector2 point)
	{
		Vector2 lineDirection = new Vector2(v2.X - v1.X, v2.Y - v1.Y);
		Vector2 pointOffset = new Vector2(point.X - v1.X, point.Y - v1.Y);
		double dot = lineDirection.Dot(pointOffset);
		double lineLength = Math.Sqrt(lineDirection.X * lineDirection.X + lineDirection.Y * lineDirection.Y);
		double offsetLength = Math.Sqrt(pointOffset.X * pointOffset.X + pointOffset.Y * pointOffset.Y);
		double projectedLength = dot / (lineLength * offsetLength) * offsetLength;
		return new Vector2(v1.X + projectedLength * lineDirection.X / lineLength, v1.Y + projectedLength * lineDirection.Y / lineLength);
	}

	/// <summary>
	/// Gets the position where the character stands on the vector, at the given position along it.
	/// </summary>
	public Vector2 GetAlignmentForVector2(CollisionVector t, Vector2 alignmentPosition, double alignmentAngle)
	{
		double radius = CollisionRadius.Y;
		Vector2 sensorStart = GetPointRotatedFromRelative(alignmentPosition, new Vector2(alignmentPosition.X - radius, alignmentPosition.Y), Math.PI / 2.0 + t.Angle);
		Vector2 sensorEnd = GetPointRotatedFromRelative(alignmentPosition, new Vector2(alignmentPosition.X + radius, alignmentPosition.Y), Math.PI / 2.0 + t.Angle);
		// GetLineIntersection replaces this value. The Intersector call stays because it can throw for a zero radius.
		Vector2 intersection = Intersector.Intersection(t.AbsoluteA, t.AbsoluteB, sensorStart, sensorEnd).FirstOrDefault();
		Vector2.GetLineIntersection(t.AbsoluteA, t.AbsoluteB, sensorStart, sensorEnd, out intersection);
		return GetPointRotatedFromRelative(intersection, new Vector2(intersection.X - radius, intersection.Y), Math.PI / 2.0 + t.Angle);
	}

	private void AcquireVector(CollisionVector vector)
	{
		GroundVector = vector;
		Mode = vector.Mode;
		ObjectLink = vector.Owner;
	}

	private void UpdateCollisionsWithObjects()
	{
		int offsetY = RectangleCollisionRadius.Y - NormalCollisionRadius.Y;
		Rectangle collisionArea = Rectangle.FromLTRB(PositionPrecise.X - RectangleCollisionRadius.X, PositionPrecise.Y - RectangleCollisionRadius.Y - offsetY, PositionPrecise.X + RectangleCollisionRadius.X, PositionPrecise.Y + RectangleCollisionRadius.Y - offsetY);
		foreach (ActiveObject activeObject in Level.ObjectManager.ActiveObjects)
		{
			CollisionRectangle[] collisionRectangles = activeObject.CollisionRectangles;
			foreach (CollisionRectangle collisionRectangle in collisionRectangles)
			{
				if (collisionRectangle.AbsoluteBounds.IntersectsWith(collisionArea))
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

	private void AddPlatformVelocity()
	{
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.Conveyor))
		{
			Vector2 platformVelocity = default;
			bool autoAlign = false;
			if (GroundVector.Owner is IPlatform platform)
			{
				platformVelocity = platform.Velocity;
			}
			else
			{
				autoAlign = !GroundVector.Flags.HasFlag(CollisionFlags.NoAutoAlignment);
			}
			// Copy the sub-pixel part of the owner position to the character.
			Vector2 ownerSubPixelOffset = GroundVector.Owner.PositionPrecise - GroundVector.Owner.Position;
			PositionPrecise = Position + ownerSubPixelOffset;
			Velocity += platformVelocity;
			if (autoAlign)
			{
				PositionPrecise = GetAlignmentForVector2(GroundVector, PositionPrecise, GroundAngle);
			}
		}
	}

	private void RemovePlatformVelocity()
	{
		if (GroundVector != null && GroundVector.Flags.HasFlag(CollisionFlags.Conveyor))
		{
			Velocity -= GetGroundOwnerVelocity();
		}
	}

	/// <summary>
	/// Returns true when the touch point of the top or bottom collision is in the box.
	/// </summary>
	private bool IsCrushedBetweenFloors(Vector2[][] collisionBox, CollisionEvent top, CollisionEvent bottom)
	{
		bool result = false;
		Rectangle innerBox = Rectangle.FromLTRB(collisionBox[0][0].X, collisionBox[0][0].Y, collisionBox[1][0].X, collisionBox[1][0].Y);
		if (top != null)
		{
			if (innerBox.Contains(top.CollisionInfo.Touch))
			{
				result = true;
			}
		}
		if (bottom != null)
		{
			if (innerBox.Contains(bottom.CollisionInfo.Touch))
			{
				result = true;
			}
		}
		return result;
	}

	/// <summary>
	/// Returns true when the vector of the collision belongs to an object that moved during this tick.
	/// </summary>
	private static bool IsOwnerMoving(CollisionEvent collisionEvent)
	{
		return collisionEvent.CollisionInfo.Vector != null && collisionEvent.CollisionInfo.Vector.Owner != null && collisionEvent.CollisionInfo.Vector.Owner.PositionPrecise != collisionEvent.CollisionInfo.Vector.Owner.LastPositionPrecise;
	}

	/// <summary>
	/// Kills the character when a moving object pushes it into a floor, a ceiling or a wall.
	/// </summary>
	private void CheckCrushed()
	{
		const int top = (int)CollisionMode.Top;
		const int left = (int)CollisionMode.Left;
		const int bottom = (int)CollisionMode.Bottom;
		const int right = (int)CollisionMode.Right;
		bool floorAndCeiling = _collisionDetected[top] && _collisionDetected[bottom];
		bool floorAndCeilingTile = _collisionDetected[top] && _collisionDetectedTiles[bottom];
		bool floorTileAndCeiling = _collisionDetectedTiles[top] && _collisionDetected[bottom];
		bool bothWalls = _collisionDetected[left] && _collisionDetected[right];
		bool crushed = false;
		Vector2[][] collisionBox = GetCollisionBox(new Vector2(CollisionRadius.X - LedgeSensorRadius, CollisionRadius.Y), rotate: false);
		if (floorAndCeiling)
		{
			CollisionEvent floorEvent = _collisionEvents[top];
			CollisionEvent ceilingEvent = _collisionEvents[bottom];
			if (floorEvent.CollisionInfo.Vector != null && ceilingEvent.CollisionInfo.Vector != null)
			{
				ActiveObject floorOwner = floorEvent.CollisionInfo.Vector.Owner;
				ActiveObject ceilingOwner = ceilingEvent.CollisionInfo.Vector.Owner;
				// At least one vector must belong to an object, and the two vectors must have different owners.
				if ((floorOwner != null || ceilingOwner != null) && floorOwner != ceilingOwner)
				{
					bool isMoving = true;
					if (floorOwner != null)
					{
						isMoving = floorOwner.PositionPrecise != floorOwner.LastPositionPrecise;
					}
					if (ceilingOwner != null)
					{
						isMoving = (ceilingOwner.PositionPrecise != ceilingOwner.LastPositionPrecise) | isMoving;
					}
					if (isMoving)
					{
						crushed = IsCrushedBetweenFloors(collisionBox, floorEvent, ceilingEvent);
					}
				}
			}
		}
		else if (floorAndCeilingTile)
		{
			CollisionEvent floorEvent = _collisionEvents[top];
			CollisionEvent ceilingTileEvent = _collisionEventsTiles[bottom];
			if (IsOwnerMoving(floorEvent))
			{
				crushed = IsCrushedBetweenFloors(collisionBox, floorEvent, ceilingTileEvent);
			}
		}
		else if (floorTileAndCeiling)
		{
			CollisionEvent floorTileEvent = _collisionEventsTiles[top];
			CollisionEvent ceilingEvent = _collisionEvents[bottom];
			if (IsOwnerMoving(ceilingEvent))
			{
				crushed = IsCrushedBetweenFloors(collisionBox, floorTileEvent, ceilingEvent);
			}
		}
		if (bothWalls)
		{
			Rectangle innerBox = Rectangle.FromLTRB(collisionBox[0][0].X, collisionBox[0][0].Y, collisionBox[1][0].X, collisionBox[1][0].Y);
			CollisionEvent leftEvent = _collisionEvents[left];
			CollisionEvent rightEvent = _collisionEvents[right];
			if (leftEvent != null && IsOwnerMoving(leftEvent))
			{
				if (innerBox.Contains(leftEvent.CollisionInfo.Touch))
				{
					crushed = true;
				}
			}
			if (rightEvent != null && IsOwnerMoving(rightEvent))
			{
				if (innerBox.Contains(rightEvent.CollisionInfo.Touch))
				{
					crushed = true;
				}
			}
		}
		if (crushed)
		{
			Kill(PlayerDeathCause.Crushed);
		}
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
		SpindashDustGroupResourceKey = DefaultSpindashDustResourceKey;
		SpindashDustAnimation = new AnimationInstance(resourceTree.GetLoadedResource<AnimationGroup>(SpindashDustGroupResourceKey));
		BrakeDustResourceKey = DefaultBrakeDustResourceKey;
		InvincibilityGroupResourceKey = DefaultInvincibilityResourceKey;
		_drowningAnimation = new AnimationInstance(resourceTree, DrowningCountDownResourceKey);
		Priority = NormalPriority;
		StateFlags = 0;
		ShowAngle = 0.0;
		GroundVelocity = 0.0;
		Velocity = new Vector2(0.0, 0.0);
		SlopeLockTicks = 0;
		LookDirection = CharacterLookDirection.Normal;
		BreathTicks = MaximumBreath;
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
			// Look up or duck when the vertical input is stronger than the horizontal input.
			double throttle = Input.Throttle;
			double vertical = Input.VerticalDirection;
			if (Math.Abs(vertical) > Math.Abs(throttle))
			{
				if (vertical <= -0.5)
				{
					LookDirection = CharacterLookDirection.Up;
				}
				else if (vertical >= 0.5)
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

	/// <summary>
	/// Moves the history one item back and puts the current state at index 0.
	/// </summary>
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
		double slopeFactor = 0.0 - Math.Sin(GroundAngle);
		if (IsSpinball)
		{
			// A rolling character speeds up downhill more than it slows down uphill.
			slopeFactor = (!(GroundVelocity * slopeFactor > 0.0)) ? (slopeFactor * 1.25) : (slopeFactor * 0.2873);
		}
		else
		{
			slopeFactor *= 0.5;
			if (GroundVelocity == 0.0 && Math.Abs(slopeFactor) <= 0.05)
			{
				return;
			}
		}
		GroundVelocity -= slopeFactor;
	}

	private void UpdateSpindash()
	{
		if (IsCharging)
		{
			LookDelay = DefaultLookDelay;
			_spindashExtraSpeed *= 31.0 / 32.0;
			if (Input.ABC == CharacterInputButtonState.Pressed)
			{
				_spindashExtraSpeed = Math.Min(_spindashExtraSpeed + 4.0, 16.0);
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPINDASH/CHARGE");
			}
			if (LookDirection != CharacterLookDirection.Ducking)
			{
				IsCharging = false;
				GroundVelocity = (SpindashBaseSpeed + _spindashExtraSpeed) * Facing;
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
			int direction = Math.Sign(GroundVelocity);
			if (direction == 0)
			{
				direction = Facing;
			}
			GroundVelocity = 16 * direction;
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
			GroundVelocity = Math.Sign(GroundVelocity) * Math.Min(Math.Abs(GroundVelocity), MaximumVelocity);
			Velocity = new Vector2(MathX.Clamp(-64.0, GroundVelocity * Math.Cos(GroundAngle), 64.0), MathX.Clamp(-64.0, GroundVelocity * Math.Sin(GroundAngle), 64.0));
			AddPlatformVelocity();
			if (CharacterEvents.HasFlag(CharacterEvent.Jump))
			{
				double jumpAngle = GroundAngle;
				if (GroundVector.Flags.HasFlag(CollisionFlags.NoAngle))
				{
					jumpAngle = 0.0;
				}
				Velocity += new Vector2(JumpForce * Math.Sin(jumpAngle), JumpForce * (0.0 - Math.Cos(jumpAngle)));
				CharacterEvents &= ~CharacterEvent.Jump;
			}
			if (CharacterEvents.HasFlag(CharacterEvent.Fall))
			{
				LeaveGround();
				CharacterEvents &= ~CharacterEvent.Fall;
			}
		}
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
		Velocity = new Vector2(Math.Sign(Velocity.X) * Math.Min(Math.Abs(Velocity.X), MaximumVelocity), Math.Sign(Velocity.Y) * Math.Min(Math.Abs(Velocity.Y), MaximumVelocity));
		bool hasRightCollision = _collisionEvents[(int)CollisionMode.Right] != null;
		bool hasLeftCollision = _collisionEvents[(int)CollisionMode.Left] != null;
		// A pushing character does not move vertically, unless it pushes a mobile vector.
		double moveY = 0.0;
		bool attachedToPlatform = IsAttachedToPlatform();
		if (IsPushing && !attachedToPlatform)
		{
			if (hasRightCollision && _collisionEvents[(int)CollisionMode.Right].CollisionInfo.Vector.Flags.HasFlag(CollisionFlags.Mobile))
			{
				moveY = Velocity.Y;
			}
			if (hasLeftCollision && _collisionEvents[(int)CollisionMode.Left].CollisionInfo.Vector.Flags.HasFlag(CollisionFlags.Mobile))
			{
				moveY = Velocity.Y;
			}
		}
		else
		{
			moveY = Velocity.Y;
		}
		PositionPrecise += new Vector2(Velocity.X, moveY);
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
		int ledgeSide = Math.Sign(DistanceFromLedge);
		double ledgeDistance = Math.Abs(DistanceFromLedge);
		if (ledgeDistance > LedgeSensorRadius)
		{
			_balanceDirection = (CharacterBalanceDirection)(2 * ledgeSide);
			Facing = ledgeSide;
		}
		else if (ledgeDistance > 0.0)
		{
			_balanceDirection = (CharacterBalanceDirection)ledgeSide;
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
				LookDelay = DefaultLookDelay;
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
		bool hasRightCollision = _collisionEvents[(int)CollisionMode.Right] != null;
		bool hasLeftCollision = _collisionEvents[(int)CollisionMode.Left] != null;
		if ((MovingDirection == -1 && hasRightCollision) || (MovingDirection == 1 && hasLeftCollision))
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

	/// <summary>
	/// Changes a speed towards the limit in the given direction, and then applies friction.
	/// When the speed is against the direction, the speed changes by the deceleration.
	/// </summary>
	private static double ChangeSpeed(double speed, int direction, double limit, double acceleration, double deceleration, double friction)
	{
		if (direction != 0)
		{
			double headroom = limit - speed * direction;
			if (headroom > limit)
			{
				speed += deceleration * direction;
			}
			else if (headroom > acceleration)
			{
				speed += acceleration * direction;
			}
			else if (headroom > 0.0)
			{
				speed = limit * direction;
			}
		}
		if (friction > 0.0)
		{
			if (speed > friction)
			{
				speed -= friction;
			}
			else if (speed < 0.0 - friction)
			{
				speed += friction;
			}
			else
			{
				speed = 0.0;
			}
		}
		return speed;
	}

	private void ApplyLevelBounds()
	{
		if (Position.X - LevelBoundsMargin < Level.Bounds.X)
		{
			PositionPrecise = new Vector2(Level.Bounds.X + LevelBoundsMargin, PositionPrecise.Y);
			Velocity = new Vector2(0.0, Velocity.Y);
			GroundVelocity = MathX.Clamp(-1.0, GroundVelocity, 0.0);
		}
		else if (Position.X + LevelBoundsMargin > Level.Bounds.Right - 1)
		{
			PositionPrecise = new Vector2(Level.Bounds.Right - 1 - LevelBoundsMargin, PositionPrecise.Y);
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
		LookDelay = DefaultLookDelay;
		CameraProperties.Delay = new Vector2i(0, 0);
		BreathTicks = MaximumBreath;
		if (IsUnderwater)
		{
			LeaveWater();
		}
	}

	public void DrawDebugInfo(Renderer renderer)
	{
		double textY = 512.0;
		Level.DebugContext.DrawText(renderer, string.Join("  ", IsFacingLeft ? 'L' : 'R', IsBraking ? 'B' : ' ', IsPushing ? 'P' : ' ', IsAirborne ? 'A' : ' ', IsSpinball ? 'S' : ' ', IsRollJumping ? 'R' : ' ', IsDying ? 'D' : ' ', IsFlying ? 'F' : ' ', ExhibitsVirtualPlatform ? 'V' : ' ', HasSpeedShoes ? "SS" : "  ", IsInvincible ? 'I' : ' '), FontAlignment.MiddleX, 960.0, 16.0, 0.75, 0);
		textY += Level.DebugContext.DrawText(renderer, $"LAYER: {Level.Map.Layers.IndexOf(Layer)}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		textY += Level.DebugContext.DrawText(renderer, $"ANGLE: {GroundAngle:0.00} TUMBLE: {TumbleAngle:0.00}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		textY += Level.DebugContext.DrawText(renderer, $"POSITION: {PositionPrecise.X:0.00}, {PositionPrecise.Y:0.00}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		textY += Level.DebugContext.DrawText(renderer, $"VELOCITY: {Velocity.X:0.00}, {Velocity.Y:0.00}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		if (!IsAirborne)
		{
			textY += Level.DebugContext.DrawText(renderer, $"GROUND VELOCITY: {GroundVelocity:0.00}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		}
		if (SlopeLockTicks > 0)
		{
			textY += Level.DebugContext.DrawText(renderer, $"SLOPE LOCK: {SlopeLockTicks}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		}
		if (LookDelay < DefaultLookDelay)
		{
			textY += Level.DebugContext.DrawText(renderer, $"LOOK DELAY: {LookDelay}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		}
		if (IsCharging)
		{
			textY += Level.DebugContext.DrawText(renderer, $"SPINDASH: {SpindashBaseSpeed + _spindashExtraSpeed}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		}
		if (InvulnerabilityTicks > 0)
		{
			textY += Level.DebugContext.DrawText(renderer, $"INVULNERABILITY: {InvulnerabilityTicks}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		}
		if (BreathTicks < MaximumBreath)
		{
			textY += Level.DebugContext.DrawText(renderer, $"DROWNING: {BreathTicks}", FontAlignment.Left, 8.0, textY, 0.75, 0);
		}
	}

	protected internal void DrawNewCollisionDebug(Renderer renderer)
	{
		if (IsSidekick)
		{
			return;
		}
		string[] lines = new string[7]
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
			double textY = 16.0;
			foreach (string line in lines)
			{
				textY += Level.DebugContext.DrawText(renderer, line.ToUpper(), FontAlignment.Right, 1904.0, textY, 0.5, 0);
			}
			i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(0.0 - Level.Camera.Bounds.X, 0.0 - Level.Camera.Bounds.Y);
			if ((Mode == CollisionMode.Top || Mode == CollisionMode.Bottom) && GroundVector != null)
			{
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(GroundVector.AbsoluteA.X, 0.0), new Vector2(GroundVector.AbsoluteA.X, 10000.0), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(GroundVector.AbsoluteA.X, 0.0), new Vector2(GroundVector.AbsoluteA.X, 10000.0), 4.0);
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(GroundVector.AbsoluteB.X, 0.0), new Vector2(GroundVector.AbsoluteB.X, 10000.0), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(GroundVector.AbsoluteB.X, 0.0), new Vector2(GroundVector.AbsoluteB.X, 10000.0), 4.0);
				string endText = "{X: " + GroundVector.AbsoluteA.X + ", Y: " + GroundVector.AbsoluteA.Y + "}";
				Level.DebugContext.DrawText(renderer, endText.ToUpper(), FontAlignment.Right, GroundVector.AbsoluteA.X - 128, GroundVector.AbsoluteA.Y - 128, 0.5, 0);
				endText = "{X: " + GroundVector.AbsoluteB.X + ", Y: " + GroundVector.AbsoluteB.Y + "}";
				Level.DebugContext.DrawText(renderer, endText.ToUpper(), FontAlignment.Right, GroundVector.AbsoluteB.X + 256, GroundVector.AbsoluteB.Y - 128, 0.5, 0);
			}
			else if ((Mode == CollisionMode.Left || Mode == CollisionMode.Right) && GroundVector != null)
			{
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(0.0, GroundVector.AbsoluteA.Y), new Vector2(100000.0, GroundVector.AbsoluteA.Y), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(0.0, GroundVector.AbsoluteA.Y), new Vector2(100000.0, GroundVector.AbsoluteA.Y), 4.0);
				i2dRenderer.RenderLine(Colours.Blue, new Vector2(0.0, GroundVector.AbsoluteB.Y), new Vector2(100000.0, GroundVector.AbsoluteB.Y), 8.0);
				i2dRenderer.RenderLine(Colours.Yellow, new Vector2(0.0, GroundVector.AbsoluteB.Y), new Vector2(100000.0, GroundVector.AbsoluteB.Y), 4.0);
			}
			double halfWidth;
			double halfHeight;
			bool isWallMode = false;
			if (Mode == CollisionMode.Left || Mode == CollisionMode.Right)
			{
				halfWidth = CollisionRadius.Y;
				halfHeight = CollisionRadius.X;
				isWallMode = true;
			}
			else
			{
				halfWidth = CollisionRadius.X;
				halfHeight = CollisionRadius.Y;
			}
			Vector2 leftSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y + halfWidth), Math.PI / 2.0 + GroundAngle);
			Vector2 rightSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X, PositionPrecise.Y - halfWidth), Math.PI / 2.0 + GroundAngle);
			Vector2 topSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X - halfHeight, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2 bottomSensor = GetPointRotatedFromRelative(PositionPrecise, new Vector2(PositionPrecise.X + halfHeight, PositionPrecise.Y), Math.PI / 2.0 + GroundAngle);
			Vector2[][] shortBox = GetCollisionBox(new Vector2(halfWidth - LedgeSensorRadius, halfHeight - FloorSensorRadius + 20.0), rotate: false);
			Vector2[][] narrowBox = GetCollisionBox(new Vector2(halfWidth - LedgeSensorRadius, halfHeight), rotate: false);
			Vector2[][] wallBoxEdges;
			Vector2[][] floorBoxEdges;
			Vector2 wallLineStart;
			Vector2 wallLineEnd;
			Vector2 floorLineStart;
			Vector2 floorLineEnd;
			if (!isWallMode)
			{
				wallBoxEdges = shortBox;
				floorBoxEdges = narrowBox;
				wallLineStart = leftSensor;
				wallLineEnd = rightSensor;
				floorLineStart = topSensor;
				floorLineEnd = bottomSensor;
			}
			else
			{
				wallBoxEdges = narrowBox;
				floorBoxEdges = shortBox;
				wallLineStart = topSensor;
				wallLineEnd = bottomSensor;
				floorLineStart = leftSensor;
				floorLineEnd = rightSensor;
			}
			i2dRenderer.RenderLine(Colour.ParseHex("00FF00"), wallLineStart, wallLineEnd, 5.0);
			i2dRenderer.RenderLine(Colour.ParseHex("00FF00"), floorLineStart, floorLineEnd, 5.0);
			const int endMarkerRadius = 6;
			foreach (CollisionVector mapVector in Level.Map.CollisionVectors)
			{
				if (Level.Camera.Bounds.IntersectsWith(mapVector.Bounds))
				{
					i2dRenderer.RenderLine(Colour.ParseHex("FF0000"), mapVector.AbsoluteA, mapVector.AbsoluteB, 5.0);
					i2dRenderer.RenderRectangle(Colour.ParseHex("FA35FF"), Rectangle.FromLTRB(mapVector.AbsoluteA.X - endMarkerRadius, mapVector.AbsoluteA.Y - endMarkerRadius, mapVector.AbsoluteA.X + endMarkerRadius, mapVector.AbsoluteA.Y + endMarkerRadius), endMarkerRadius * 4);
					i2dRenderer.RenderRectangle(Colour.ParseHex("FA35FF"), Rectangle.FromLTRB(mapVector.AbsoluteB.X - endMarkerRadius, mapVector.AbsoluteB.Y - endMarkerRadius, mapVector.AbsoluteB.X + endMarkerRadius, mapVector.AbsoluteB.Y + endMarkerRadius), endMarkerRadius * 4);
				}
			}
			foreach (ActiveObject activeObject in Level.ObjectManager.ActiveObjects)
			{
				foreach (CollisionVector objectVector in activeObject.CollisionVectors)
				{
					i2dRenderer.RenderLine(Colour.ParseHex("FF00FF"), objectVector.AbsoluteA, objectVector.AbsoluteB, 5.0);
				}
				foreach (CollisionRectangle collisionRectangle in activeObject.CollisionRectangles)
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
			List<Vector2[]> wallLines = new List<Vector2[]> { new Vector2[2] { wallLineStart, wallLineEnd } };
			wallLines.AddRange(wallBoxEdges);
			foreach (Vector2[] line in wallLines)
			{
				i2dRenderer.RenderLine(Colour.ParseHex("00FF00"), line[0], line[1], 5.0);
			}
			List<Vector2[]> floorLines = new List<Vector2[]> { new Vector2[2] { floorLineStart, floorLineEnd } };
			floorLines.AddRange(floorBoxEdges);
			foreach (Vector2[] line in floorLines)
			{
				i2dRenderer.RenderLine(Colour.ParseHex("0000FF"), line[0], line[1], 5.0);
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
			// Debug marker that the original code left in. _intersection is never set, so the marker moves each frame.
			_intersection -= PositionPrecise;
			renderer.Get2dRenderer().RenderRectangle(Colour.ParseHex("FF00FF"), Rectangle.FromLTRB(_intersection.X - 10.0, _intersection.Y - 10.0, _intersection.X + 10.0, _intersection.Y + 10.0), 40.0);
		}
	}

	protected virtual void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		ICharacterRenderer characterRenderer = renderer.GetCharacterRenderer();
		characterRenderer.ModelMatrix = i2dRenderer.ModelMatrix;
		double hueShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_hue_shift");
		double saturationShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_sat_shift");
		double luminanceShift = Level.GameContext.Configuration.GetPropertyDouble("debug", "sonic_lum_shift");
		if (DrawBodyRotated)
		{
			characterRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
		}
		Animation.Frame currentFrame = Animation.CurrentFrame;
		Vector2 offset = currentFrame.Offset;
		Rectangle destination = new Rectangle(offset.X - currentFrame.Source.Width / 2, offset.Y - currentFrame.Source.Height / 2, currentFrame.Source.Width, currentFrame.Source.Height);
		characterRenderer.Filter = viewOptions.Filter;
		characterRenderer.FilterAmount = viewOptions.FilterAmount;
		characterRenderer.Brightness = (viewOptions.Shadows ? 0f : Brightness);
		characterRenderer.RenderTexture(Animation.AnimationGroup.Textures[1], Animation.AnimationGroup.Textures[0], hueShift, saturationShift, luminanceShift, currentFrame.Source, destination, IsFacingRight, IsFacingLeft && Animation.Index == (int)CharacterAnimation.Tumble);
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
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			Vector2i offset = _drownCountdownPosition - Position;
			_drowningAnimation.Draw(i2dRenderer, offset);
		}
	}

	private void ScatterRings(int ringCount, int facing)
	{
		Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/RINGSCATTER");
		ringCount = Math.Min(ringCount, MaximumScatteredRings);
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
			// A sidekick does not lose rings and cannot die from a hit.
			KnockBack(direction);
		}
		else if (Player.CurrentRings > 0 || HasBarrier)
		{
			KnockBack(direction);
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

	/// <summary>
	/// Throws the character up and in the given horizontal direction, and makes it invulnerable for a short time.
	/// </summary>
	private void KnockBack(int direction)
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
		InvulnerabilityTicks = HurtInvulnerabilityTicks;
	}

	public void Kill(PlayerDeathCause cause)
	{
		_deathCause = cause;
		Velocity = new Vector2(0.0, -28.0);
		_dyingTicks = DyingTicks;
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
		Priority = DyingPriority;
	}

	private void UpdateDying()
	{
		if (_dyingTicks == 0)
		{
			StateFlags = 0;
			IsDead = true;
			return;
		}
		double gravity = (_deathCause == PlayerDeathCause.Drown) ? 0.25 : 0.875;
		Velocity = new Vector2(Velocity.X, Math.Min(Velocity.Y + gravity, MaximumVelocity));
		PositionPrecise += Velocity;
		_dyingTicks--;
	}

	private void InitialiseInvincibility()
	{
		AnimationGroup animationGroup = Level.GameContext.ResourceTree.GetLoadedResource<AnimationGroup>(InvincibilityGroupResourceKey);
		_invincibilityParticles.Clear();
		// Each chunk uses its own animation. The 12 particles start at equal angles around the circle.
		for (int i = 0; i < InvincibilityPositionHistoryChunks; i++)
		{
			for (int j = 0; j < InvincibilityParticlesPerPositionHistory; j++)
			{
				double fractionOfTurn = (j * InvincibilityPositionHistoryChunks + i) / 12.0;
				_invincibilityParticles.Add(new InvincibilityParticle(this, fractionOfTurn * (Math.PI * 2.0), animationGroup, i));
			}
		}
	}

	private void StartInvincibility()
	{
		_invincibilityCharacterPositionHistory.Clear();
		// 8 = InvincibilityPositionHistoryChunks * InvincibilityPositionHistoryDelay.
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
		// Each chunk of particles follows the character with a larger delay.
		for (int i = 0; i < InvincibilityPositionHistoryChunks; i++)
		{
			int historyIndex = i * InvincibilityPositionHistoryDelay;
			if (historyIndex < _invincibilityCharacterPositionHistory.Count)
			{
				Vector2 origin = _invincibilityCharacterPositionHistory[historyIndex];
				for (int j = 0; j < InvincibilityParticlesPerPositionHistory; j++)
				{
					int index = i * InvincibilityParticlesPerPositionHistory + j;
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
			_humanInputTicksRemaining = NoHumanInputTime;
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
		CharacterHistoryItem protagonistHistory = protagonist.History[SidekickHistoryDelay];
		switch (_autoSidekickState)
		{
			case AutoSidekickState.Spawning:
				if (!protagonist.IsDebug && (_humanControlled || Level.Ticks % 64 == 0))
				{
					// Fly in from above the protagonist.
					_autoSidekickState = AutoSidekickState.Flying;
					StateFlags = 0;
					CollisionRadius = NormalCollisionRadius;
					Position = new Vector2i(protagonist.Position.X, protagonist.Position.Y - 1152);
					Priority = SidekickFlyingPriority;
					_sidekickOffscreenTicks = SidekickOffscreenTime;
				}
				break;
			case AutoSidekickState.Flying:
				{
					if (IsOnScreen())
					{
						_sidekickOffscreenTicks = SidekickOffscreenTime;
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
					_sidekickTargetPosition = protagonistHistory.Position;
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
					int deltaX = _sidekickTargetPosition.X - Position.X;
					if (deltaX != 0)
					{
						offset.X = Math.Sign(deltaX) * Math.Min(48, Math.Abs(deltaX) / 4);
						offset.X += (int)protagonist.Velocity.X + 4;
						Facing = Math.Sign(offset.X);
					}
					int heightDifference = protagonist.NormalCollisionRadius.Y - NormalCollisionRadius.Y;
					int deltaY = _sidekickTargetPosition.Y - Position.Y + heightDifference;
					if (deltaY != 0)
					{
						offset.Y = Math.Sign(deltaY) * Math.Min(Math.Abs(deltaY), CanFly ? 4 : 24);
					}
					CharacterState state = protagonistHistory.State;
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
				Priority = SidekickPriority;
				if (!IsUnableToCatchUp())
				{
					if (_humanControlled || IsObjectControlled || IsWinning || protagonistHistory == null)
					{
						UpdateNormal();
						break;
					}
					if (SlopeLockTicks > 0)
					{
						_autoSidekickState = AutoSidekickState.Spindash;
						goto case AutoSidekickState.Spindash;
					}
					ApplyAutoInput(GetAutoInputNormal(protagonist, protagonistHistory));
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
		// Start from the input of the protagonist some ticks ago.
		CharacterInputState input = new CharacterInputState(protagonistHistory.Input);
		IsFacingLeft = protagonistHistory.State.HasFlag(CharacterState.Left);
		int deltaX = protagonistHistory.Position.X - Position.X;
		if (deltaX == 0)
		{
			IsFacingLeft = protagonistHistory.State.HasFlag(CharacterState.Left);
		}
		else if (deltaX > 192)
		{
			if (deltaX >= 64)
			{
				input.HorizontalDirection = 1;
			}
			if (GroundVelocity != 0.0 && IsFacingRight)
			{
				Move(4, 0);
			}
		}
		else if (deltaX < 128)
		{
			if (deltaX <= -64)
			{
				input.HorizontalDirection = -1;
			}
			if (GroundVelocity != 0.0 && !IsFacingRight)
			{
				Move(-4, 0);
			}
		}
		if (_autoSidekickJumping)
		{
			// Hold the jump button until the sidekick lands.
			input.A = CharacterInputButtonState.Down;
			if (IsAirborne)
			{
				return input;
			}
			_autoSidekickJumping = false;
		}
		if (Level.Ticks % 256 == 0 && deltaX >= 256)
		{
			return input;
		}
		int deltaY = protagonistHistory.Position.Y - Position.Y;
		if (deltaY >= 0)
		{
			if (protagonist.IsPushing)
			{
				input.HorizontalDirection = Math.Sign(deltaX);
			}
			return input;
		}
		if (deltaY > -128)
		{
			if (protagonist.IsPushing && IsPushing && _pushDirection != protagonist.Facing && LookDirection != CharacterLookDirection.Ducking)
			{
				input.A = CharacterInputButtonState.Pressed;
				_autoSidekickJumping = true;
				return input;
			}
			return input;
		}
		// The protagonist is much higher. Jump from time to time.
		if (Level.Ticks % 64 == 0 && LookDirection != CharacterLookDirection.Ducking)
		{
			input.A = CharacterInputButtonState.Pressed;
			_autoSidekickJumping = true;
		}
		return input;
	}

	private CharacterInputState GetAutoInputSpindash(ICharacter protagonist)
	{
		CharacterInputState input = new CharacterInputState();
		if (!IsCharging)
		{
			if (GroundVelocity != 0.0)
			{
				return input;
			}
			// Duck and press A to start the spindash.
			Facing = Math.Sign(protagonist.Position.X - Position.X);
			input.VerticalDirection = 1;
			input.A = CharacterInputButtonState.Pressed;
		}
		else
		{
			// Charge the spindash. Release it every 128 ticks.
			input.VerticalDirection = 1;
			if (Level.Ticks % 128 != 0)
			{
				if (Level.Ticks % 32 == 0)
				{
					input.A = CharacterInputButtonState.Pressed;
				}
			}
			else
			{
				_autoSidekickState = AutoSidekickState.Normal;
			}
		}
		return input;
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
			_sidekickOffscreenTicks = SidekickOffscreenTime;
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
			bool wasUnderwater = IsUnderwater;
			bool isUnderwater = Level.WaterManager.IsUnderwater(Position);
			IsUnderwater = isUnderwater;
			if (!wasUnderwater && isUnderwater)
			{
				EnterWater();
			}
			else if (wasUnderwater && !isUnderwater)
			{
				LeaveWater();
			}
			if (isUnderwater)
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
				BreathTicks = MaximumBreath;
			}
		}
	}

	private void EnterWater()
	{
		Velocity = new Vector2(Velocity.X / 2.0, Velocity.Y / 4.0);
		if (IsAirborne && !IsObjectControlled)
		{
			Level.WaterManager.CreateSplash(Layer, SplashType.Enter, Position);
			Level.SoundManager.PlaySound(SplashSoundResourceKey);
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
			Level.SoundManager.PlaySound(SplashSoundResourceKey);
		}
		BreathTicks = MaximumBreath;
		if (_drowningClimax && !IsSidekick)
		{
			Level.SoundManager.StopJingle(JingleType.Drowning);
			Level.SoundManager.PlayMusic();
		}
	}

	private void CheckForDrowning()
	{
		if (BreathTicks > BreathLeftDrowningMusic)
		{
			if (BreathLeftWarnings.Contains(BreathTicks) && !IsSidekick)
			{
				Level.SoundManager.PlaySound(DrownWarningSoundResourceKey);
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
		// Show the countdown once each second (60 ticks) in the last 5 seconds.
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
		Level.SoundManager.PlaySound(this, DrownSoundResourceKey);
	}

	private void ShowBreathTimeRemaining(int secondsLeft)
	{
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
		BreathTicks = MaximumBreath;
		if (_drowningClimax && !IsSidekick)
		{
			Level.SoundManager.StopJingle(JingleType.Drowning);
			Level.SoundManager.PlayMusic();
		}
		IsSpinball = false;
		_inhalingBubble = 1;
		Level.SoundManager.PlaySound(this, InhaleSoundResourceKey);
	}
}
