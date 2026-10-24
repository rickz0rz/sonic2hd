using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;
using SONICORCA.OBJECTS.DUST;

namespace SONICORCA.OBJECTS.WATEREGGMAN;

/// <summary>
/// The Chemical Plant Zone boss. Eggman flies to one side of the arena, lowers a tube into the chemical water
/// and fills a jar on the arm of the egg mobile. Then he flies over the protagonist and drops the chemical.
/// After the defeat, the parts of the machine break off one by one and Eggman flies away to the right.
/// </summary>
public class WaterEggmanInstance : BossObject
{
	public const int AnimationBase = 0;
	public const int AnimationBaseLightOn = 1;
	public const int AnimationBaseLightOff = 2;
	public const int AnimationArmCog = 3;
	public const int AnimationArmCogTurn = 4;
	public const int AnimationArm = 5;
	public const int AnimationTube = 6;
	public const int AnimationTubeSucking = 7;
	public const int AnimationBaseShadow = 8;
	public const int AnimationArmTap = 9;
	public const int AnimationArmTapDrip = 10;
	public const int AnimationDrip = 11;
	public const int AnimationJar = 12;
	public const int AnimationJarReflection = 13;
	public const int AnimationJarFillA = 14;
	public const int AnimationJarFillB = 15;
	public const int AnimationJarFillC = 16;
	public const int AnimationJarFillD = 17;
	public const int AnimationJarDroplet = 18;
	public const int AnimationJarLidClosed = 19;
	public const int AnimationJarLidBack = 20;
	public const int AnimationJarLidFront = 21;
	private const int AnimationEggMobileChasis = 22;
	private const int AnimationEggMobileChasisFlame = 23;
	private const int AnimationEggMobileScreen = 24;
	private const int AnimationEggMobileCap = 25;
	private const int AnimationEggMobileChasisTurn1 = 26;
	private const int AnimationEggMobileChasisTurn2 = 27;
	private const int AnimationEggMobileScreenTurn1 = 28;
	private const int AnimationEggMobileScreenTurn2 = 29;
	private const int AnimationArmTurn1 = 30;
	private const int AnimationArmTurn2 = 31;
	private const int AnimationSplash = 32;
	private const int AnimationSplashDrop = 33;
	private const int AnimationSplashSmallDrop = 34;
	private const int AnimationSmoke = 35;
	private const int AnimationBrokenJar = 36;
	private const int AnimationBrokenBoiler = 37;
	private const int AnimationBrokenTube = 38;
	private const int AnimationBrokenBar = 39;

	/// <summary>
	/// The first of the seven broken piece animations (40 to 46).
	/// </summary>
	private const int AnimationBrokenPieces = 40;
	private const int BrokenPiecesCount = 7;
	private const int AnimationBrokenEggMobileChasis = 47;
	private const int AnimationBrokenEggMobileChasisFlame = 48;
	private const int AnimationBrokenEggMobileScreen = 49;
	private const int AnimationBrokenEggMobileCap = 50;
	private const int AnimationBrokenEggMobileChasisTurn1 = 51;
	private const int AnimationBrokenEggMobileChasisTurn2 = 52;
	private const int AnimationBrokenEggMobileScreenTurn1 = 53;
	private const int AnimationBrokenEggMobileScreenTurn2 = 54;
	private const int AnimationRobotnikFaceSplash = 55;

	// The game also uses these two cap animations when the egg mobile turns before the defeat.
	private const int AnimationBrokenEggMobileCapTurn1 = 56;
	private const int AnimationBrokenEggMobileCapTurn2 = 57;
	private const int AnimationJarFluidBottomPrimary = 58;
	private const int AnimationJarFluidBottomSecondary = 59;
	private const int AnimationRobotnikNormal = 0;
	private const int AnimationRobotnikSmiling = 1;
	private const int AnimationRobotnikFrown = 2;
	private const int AnimationRobotnikDefeated = 3;
	private const int AnimationRobotnikTurn1 = 4;
	private const int AnimationRobotnikTurn2 = 5;
	private const int AnimationDustSmoke = 1;

	private const int InitialHealth = 8;
	private const int DefeatScore = 1000;

	/// <summary>
	/// The size of the area around the egg mobile in which explosions appear after the defeat.
	/// </summary>
	private const int ExplosionAreaSize = 64;

	/// <summary>
	/// The speed at which the egg mobile flies to a position.
	/// </summary>
	private const double FlySpeed = 12.0;

	/// <summary>
	/// The speed at which the arm extends and retracts.
	/// </summary>
	private const int ArmSpeed = 8;
	private const int MaxArmExtension = 256;

	/// <summary>
	/// In the original physics, the container moves 1 px per frame, from $10 px to $58 px in front of the egg mobile.
	/// The jar is 33 units in front of the egg mobile when the arm is in, so this extension puts it at $58 px.
	/// </summary>
	private const int OriginalArmSpeed = 4;
	private const int OriginalMaxArmExtension = 0x58 * 4 - 33;

	/// <summary>
	/// In the original physics, the container can only drop the chemical when it is more than $40 px in front
	/// of the egg mobile.
	/// </summary>
	private const int OriginalMinDropArmExtension = 0x40 * 4 - 33;

	/// <summary>
	/// In the original physics, the egg mobile follows the protagonist at 1 px per frame.
	/// </summary>
	private const double OriginalFollowSpeed = 4.0;

	/// <summary>
	/// In the original physics, the egg mobile stays between $2A28 and $2B70 when it faces left. The fill
	/// positions are $2A50 and $2B30, so the limits are 40 px and 64 px outside them. The limits are mirrored
	/// when the egg mobile faces right.
	/// </summary>
	private const int OriginalFollowNearLimit = 40 * 4;
	private const int OriginalFollowFarLimit = 64 * 4;

	/// <summary>
	/// When the arm extends past this length, the jar can no longer splash Eggman if the boss is defeated.
	/// </summary>
	private const int ArmSplashLimit = 24;

	/// <summary>
	/// The horizontal distance between the protagonist and the target position of the egg mobile when it aims.
	/// </summary>
	private const int AimDistance = 256 + 48;

	/// <summary>
	/// The lowest position of the suction tube, and the speed at which the tube moves.
	/// </summary>
	private const int MaxTubeY = 396;
	private const int TubeSpeed = 33;

	/// <summary>
	/// The number of ticks in the fill state, and the ticks after which the jar can splash Eggman and the jar is full.
	/// </summary>
	private const int FillTicks = 236;
	private const int FillSplashTicks = 100;
	private const int FillFullTicks = 176;

	/// <summary>
	/// The tick of the drop state at which the chemical droplet falls, and the number of ticks in the drop state.
	/// </summary>
	private const int DropTick = 2;
	private const int DropTicks = 30;

	/// <summary>
	/// In the original physics, the chemical falls 18 frames after the container opens.
	/// </summary>
	private const int OriginalDropTick = 18;

	/// <summary>
	/// The number of ticks in which the suction tube sucks, and the step of the suction animation per tick.
	/// </summary>
	private const int SuctionTicks = 140;
	private const float SuctionStep = 0.5f;

	/// <summary>
	/// The distance that the jar fluid rises for each fill step.
	/// </summary>
	private const int JarFillStep = 20;

	private const double DefeatedFallGravity = 0.375;
	private const double DefeatedMaxFallVelocity = 15.0;
	private const int RecoverTicks = 60;
	private const double RecoverRiseSpeed = 6.0;
	private const double FleeSpeed = 12.0;

	private State _state;
	private AnimationGroup _animationGroup;
	private AnimationInstance _animationEggMobileChasis;
	private AnimationInstance _animationEggMobileScreen;
	private AnimationInstance _animationEggMobileCap;
	private AnimationInstance _animationRobotnik;
	private AnimationInstance _animationArmTurn;
	private AnimationInstance _animationRobotnikFaceSplash;
	private AnimationInstance _animationBase;
	private AnimationInstance _animationBaseShadow;
	private AnimationInstance _animationBaseLightOn;
	private AnimationInstance _animationBaseLightOff;
	private AnimationInstance _animationArm;
	private AnimationInstance _animationArmTap;
	private AnimationInstance _animationArmTapCog;
	private AnimationInstance _animationArmJar;
	private AnimationInstance _animationArmJarReflection;
	private AnimationInstance _animationArmJarLidClosed;
	private AnimationInstance _animationArmJarLidBack;
	private AnimationInstance _animationArmJarLidFront;
	private AnimationInstance _animationDrip;
	private AnimationInstance _animationArmJarFill;
	private AnimationInstance _animationArmJarFillPlaceholderPrimary;
	private AnimationInstance _animationArmJarFillPlaceholderSecondary;
	private AnimationInstance _animationArmJarFilled;
	private Vector2 _nonHoverPosition;
	private double _floatOffsetAngle;
	private bool _turning;
	private bool _flipX;
	private Updater _updater;
	private int _stateTicks;
	private int _armExtension;
	private int _tubeY;

	/// <summary>
	/// The side of the arena where the boss fills the jar: -1 is the left side and 1 is the right side.
	/// </summary>
	private int _fillSide;
	private bool _dripVisible;
	private Vector2i _dripPosition;
	private bool _jarEmpty;
	private bool _jarFull;

	/// <summary>
	/// True when a defeat at this time makes the jar splash Eggman in the face.
	/// </summary>
	private bool _willJarSplashRobotnik;

	/// <summary>
	/// The height of the jar fluid. The value is 0 or negative, and it goes down as the jar fills.
	/// </summary>
	private int _jarFillY;
	private bool _jarLidOpen;
	private double _velocityY;
	private Boiler _boiler;
	private JarBack _jarBack;
	private ArmFront _armFront;
	private SuctionTube _suctionTube;
	private bool _mobileExploding;
	private bool _isFaceSplash;

	/// <summary>
	/// True when the chemical droplet has hit the floor or gone into the water.
	/// </summary>
	private bool _chemicalLanded;

	public State ActionState
	{
		get => _state;
		set => _state = value;
	}

	/// <summary>
	/// The position of the jar relative to the egg mobile. It moves out with the arm.
	/// </summary>
	[HideInEditor]
	public Vector2i JarOffset
	{
		get
		{
			Vector2i jarOffset = new Vector2i(_flipX ? 33 : -33, -186);
			jarOffset.X += _flipX ? _armExtension : -_armExtension;
			return jarOffset;
		}
	}

	[StateVariable]
	public int LeftFillX { get; set; }

	[StateVariable]
	public int RightFillX { get; set; }

	protected override void OnStart()
	{
		ResourceTree resourceTree = Level.GameContext.ResourceTree;
		AnimationGroup animationGroup = _animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, WaterEggmanType.AnimationGroupResourceKey);
		AnimationGroup robotnikAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, WaterEggmanType.RobotnikAnimationGroupResourceKey);
		_animationEggMobileChasis = new AnimationInstance(animationGroup, AnimationEggMobileChasisFlame);
		_animationEggMobileScreen = new AnimationInstance(animationGroup, AnimationEggMobileScreen);
		_animationEggMobileCap = new AnimationInstance(animationGroup, AnimationEggMobileCap);
		_animationRobotnik = new AnimationInstance(robotnikAnimationGroup);
		_animationArmTurn = new AnimationInstance(animationGroup, AnimationArmTurn1);
		_animationRobotnikFaceSplash = new AnimationInstance(animationGroup, AnimationRobotnikFaceSplash);
		_animationBase = new AnimationInstance(animationGroup);
		_animationBaseShadow = new AnimationInstance(animationGroup, AnimationBaseShadow);
		_animationBaseLightOn = new AnimationInstance(animationGroup, AnimationBaseLightOn);
		_animationBaseLightOff = new AnimationInstance(animationGroup, AnimationBaseLightOff);
		_animationArm = new AnimationInstance(animationGroup, AnimationArm);
		_animationArmTap = new AnimationInstance(animationGroup, AnimationArmTap);
		_animationArmTapCog = new AnimationInstance(animationGroup, AnimationArmCog);
		_animationArmJar = new AnimationInstance(animationGroup, AnimationJar);
		_animationArmJarReflection = new AnimationInstance(animationGroup, AnimationJarReflection);
		_animationArmJarLidClosed = new AnimationInstance(animationGroup, AnimationJarLidClosed);
		_animationArmJarLidBack = new AnimationInstance(animationGroup, AnimationJarLidBack);
		_animationArmJarLidFront = new AnimationInstance(animationGroup, AnimationJarLidFront);
		_animationDrip = new AnimationInstance(animationGroup, AnimationDrip);
		_animationArmJarFill = new AnimationInstance(animationGroup, AnimationJarFillA);
		_animationArmJarFillPlaceholderPrimary = new AnimationInstance(animationGroup, AnimationJarFluidBottomPrimary);
		_animationArmJarFillPlaceholderSecondary = new AnimationInstance(animationGroup, AnimationJarFluidBottomSecondary);
		_animationArmJarFilled = new AnimationInstance(animationGroup, AnimationJarFillD);
		ExplosionResourceKey = Type.GetAbsolutePath(WaterEggmanType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(WaterEggmanType.BossHitSoundResourceKey);
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -100, -100, 200, 200)
		};
		LockLifetime = true;
		_state = State.MoveToSide;
		Health = InitialHealth;
		_nonHoverPosition = (Vector2)Position;
		_fillSide = 1;
		_boiler = Level.ObjectManager.AddSubObject<Boiler>(this);
		_jarBack = Level.ObjectManager.AddSubObject<JarBack>(this);
		_armFront = Level.ObjectManager.AddSubObject<ArmFront>(this);
		_suctionTube = Level.ObjectManager.AddSubObject<SuctionTube>(this);
		_jarEmpty = true;
		_isFaceSplash = false;
		_updater = new Updater(GetUpdates());
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		_updater.Update();
		if (_state != State.Defeated && _state != State.Recover)
			Hover();
		if (!_mobileExploding)
			return;
		UpdateExplosions(ExplosionAreaSize);
	}

	/// <summary>
	/// The script of the boss. It runs as a coroutine: each yield is one step, one frame or more.
	/// It runs the attack cycle until the defeat, then the defeat sequence, and then the escape.
	/// </summary>
	private IEnumerable<UpdateResult> GetUpdates()
	{
		while (_state < State.Defeated)
		{
			switch (_state)
			{
				case State.MoveToSide:
					// Retract the arm, fly to the fill position and turn to face the middle of the arena.
					int fillX = _fillSide == -1 ? LeftFillX : RightFillX;
					_nonHoverPosition.X = MathX.GoTowards(_nonHoverPosition.X, fillX, FlySpeed);
					_armExtension = Math.Max(0, _armExtension - CurrentArmSpeed);
					bool faceRight = _nonHoverPosition.X < (LeftFillX + RightFillX) / 2;
					if (_armExtension == 0)
					{
						if (_flipX != faceRight)
						{
							if (_animationEggMobileChasis.Index != AnimationEggMobileChasisTurn1)
							{
								_animationEggMobileChasis.Index = AnimationEggMobileChasisTurn1;
								_animationEggMobileChasis.Cycles = 0;
								_animationEggMobileScreen.Index = AnimationEggMobileScreenTurn1;
								_animationRobotnik.Index = AnimationRobotnikTurn1;
								_animationArmTurn.Index = AnimationArmTurn1;
								_animationArmTurn.ResetFrame();
								_animationEggMobileCap.Index = AnimationBrokenEggMobileCapTurn1;
								_turning = true;
							}
							else if (_animationEggMobileChasis.Cycles >= 1)
							{
								_animationEggMobileChasis.Index = AnimationEggMobileChasisTurn2;
								_animationEggMobileScreen.Index = AnimationEggMobileScreenTurn2;
								_animationRobotnik.Index = AnimationRobotnikTurn2;
								_animationArmTurn.Index = AnimationArmTurn2;
								_animationArmTurn.ResetFrame();
								_animationEggMobileCap.Index = AnimationBrokenEggMobileCapTurn2;
								_flipX = faceRight;
							}
						}
						else if (_animationEggMobileChasis.Cycles >= 2)
							_turning = false;
					}
					if (_nonHoverPosition.X == fillX && _armExtension == 0 && _flipX == faceRight && !_turning)
						_state = State.LoweringTube;
					break;
				case State.LoweringTube:
					if (_tubeY < MaxTubeY)
					{
						_tubeY += TubeSpeed;
						_armExtension = Math.Max(0, _armExtension - CurrentArmSpeed);
						break;
					}
					_tubeY = MaxTubeY;
					_armExtension = 0;
					_stateTicks = 0;
					_state = State.FillingJar;
					break;
				case State.FillingJar:
					if (_stateTicks < FillTicks)
					{
						++_stateTicks;
						if (_stateTicks > FillSplashTicks)
							_willJarSplashRobotnik = true;
						if (_stateTicks > FillFullTicks)
							_jarFull = true;
						break;
					}
					_state = State.RaisingTube;
					break;
				case State.RaisingTube:
					if (_tubeY > 0)
					{
						_tubeY -= TubeSpeed;
						break;
					}
					_tubeY = 0;
					_state = State.Aiming;
					break;
				case State.Aiming:
					// Extend the arm and fly to a position where the jar is above the protagonist.
					_armExtension = Math.Min(CurrentMaxArmExtension, _armExtension + CurrentArmSpeed);
					if (_armExtension >= ArmSplashLimit)
						_willJarSplashRobotnik = false;
					if (Level.OriginalPhysics)
					{
						// The original follows the protagonist slowly and drops only when the protagonist is below.
						FollowProtagonist();
						if (_armExtension > OriginalMinDropArmExtension && IsProtagonistBelowJar())
						{
							_stateTicks = 0;
							_chemicalLanded = false;
							_state = State.DroppingChemical;
						}
						break;
					}
					int aimX = GetAimX();
					_nonHoverPosition.X = MathX.GoTowards(_nonHoverPosition.X, aimX, FlySpeed);
					if (_armExtension == MaxArmExtension && _nonHoverPosition.X == aimX)
					{
						_stateTicks = 0;
						_state = State.DroppingChemical;
					}
					break;
				case State.DroppingChemical:
					if (Level.OriginalPhysics)
					{
						// The original keeps following the protagonist until the chemical lands.
						FollowProtagonist();
						if (_stateTicks > OriginalDropTick && _chemicalLanded)
						{
							_fillSide = _fillSide == -1 ? 1 : -1;
							_state = State.MoveToSide;
							break;
						}
					}
					if (_stateTicks == (Level.OriginalPhysics ? OriginalDropTick : DropTick))
					{
						Vector2i dropletOffset = JarOffset;
						dropletOffset.X += _flipX ? 8 : -8;
						dropletOffset.Y -= 16;
						ChemicalDroplet chemicalDroplet = Level.ObjectManager.AddSubObject<ChemicalDroplet>(this);
						chemicalDroplet.FlipX = _flipX;
						chemicalDroplet.Position = Position + dropletOffset;
						_jarEmpty = true;
						_jarFull = false;
					}
					if (_stateTicks < DropTicks || Level.OriginalPhysics)
					{
						++_stateTicks;
						break;
					}
					_fillSide = _fillSide == -1 ? 1 : -1;
					_state = State.MoveToSide;
					break;
			}
			yield return UpdateResult.Next();
		}

		// The defeat: the parts of the machine explode one by one.
		_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasis;
		_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreen;
		_isFaceSplash = _willJarSplashRobotnik;
		_velocityY = 0.0;
		_mobileExploding = true;
		yield return UpdateResult.Wait(10);
		while (_suctionTube.Explode())
			yield return UpdateResult.Next();
		yield return UpdateResult.Wait(10);
		while (_boiler.Explode())
			yield return UpdateResult.Next();
		yield return UpdateResult.Wait(10);
		while (_armFront.ExplodeJar())
			yield return UpdateResult.Next();
		yield return UpdateResult.Wait(10);
		_suctionTube.Fall();
		yield return UpdateResult.Wait(10);
		while (_armFront.ExplodeBar())
			yield return UpdateResult.Next();
		yield return UpdateResult.Wait(4);
		_boiler.Fall();
		yield return UpdateResult.Wait(40);
		_mobileExploding = false;

		// The egg mobile falls, and then it rises again.
		do
		{
			_velocityY += DefeatedFallGravity;
			_nonHoverPosition += new Vector2(0.0, _velocityY);
			PositionPrecise = _nonHoverPosition;
			yield return UpdateResult.Next();
		}
		while (_velocityY < DefeatedMaxFallVelocity);
		_isFaceSplash = false;
		DoRobotnikNormal();
		_state = State.Recover;
		for (int i = 0; i < RecoverTicks; ++i)
		{
			_nonHoverPosition += new Vector2(0.0, -RecoverRiseSpeed);
			PositionPrecise = _nonHoverPosition;
			yield return UpdateResult.Next();
		}

		// The escape: turn to face right and fly away. The damaged egg mobile dips and smokes as it flies.
		Defeated = true;
		_state = State.Fleeing;
		Fleeing = true;
		PositionPrecise = _nonHoverPosition;
		int hoverFailureTicks = 0;
		while (true)
		{
			_nonHoverPosition += new Vector2(FleeSpeed, 0.0);
			if (!_turning)
			{
				switch (hoverFailureTicks)
				{
					case 7:
						EmitSmoke(new Vector2(85.0, -160.0), new Vector2(-7.0, -5.0));
						break;
					case 11:
						EmitSmoke(new Vector2(50.0, -70.0), new Vector2(-3.0, -5.0));
						break;
					case 21:
						EmitSmoke(new Vector2(85.0, -160.0), new Vector2(-7.0, -5.0));
						EmitSmoke(new Vector2(-120.0, -180.0), new Vector2(-3.0, -5.0));
						break;
					case 22:
						EmitSmoke(new Vector2(50.0, -70.0), new Vector2(-3.0, -5.0));
						break;
					case 35:
						EmitSmoke(new Vector2(-80.0, -90.0), new Vector2(-3.0, -5.0));
						break;
					case 45:
						EmitSmoke(new Vector2(0.0, -90.0), new Vector2(-3.0, -7.0));
						break;
				}
			}
			if (!_flipX)
			{
				if (_animationEggMobileChasis.Index != AnimationBrokenEggMobileChasisTurn1)
				{
					_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasisTurn1;
					_animationEggMobileChasis.Cycles = 0;
					_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreenTurn1;
					_animationEggMobileCap.Index = AnimationBrokenEggMobileCapTurn1;
					_turning = false;
				}
				else if (_animationEggMobileChasis.Cycles >= 1)
				{
					_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasisTurn2;
					_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreenTurn2;
					_animationEggMobileCap.Index = AnimationBrokenEggMobileCapTurn2;
					_flipX = true;
				}
			}
			else if (_animationEggMobileChasis.Cycles >= 2 && _turning)
			{
				_turning = false;
				_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreen;
			}
			if (!_turning && hoverFailureTicks < 15 && _flipX)
			{
				// The egg mobile dips down, and the flame flickers.
				_animationEggMobileChasis.Index = hoverFailureTicks % 5 != 0 ? AnimationBrokenEggMobileChasis : AnimationBrokenEggMobileChasisFlame;
				++hoverFailureTicks;
				_nonHoverPosition += new Vector2(0.0, 2.0);
			}
			else if (!_turning && hoverFailureTicks >= 15 && hoverFailureTicks < 45 && _flipX)
			{
				// The egg mobile rises again with the flame on.
				if (_animationEggMobileChasis.Index != AnimationBrokenEggMobileChasisFlame)
					_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasisFlame;
				if (hoverFailureTicks < 30)
					_nonHoverPosition += new Vector2(0.0, -2.0);
				++hoverFailureTicks;
			}
			else
				hoverFailureTicks = 0;
			// No code changes _stateTicks after the defeat, so this condition is never true.
			if (_stateTicks > 600)
				LockLifetime = false;
			yield return UpdateResult.Next();
		}
	}

	private int CurrentArmSpeed => Level.OriginalPhysics ? OriginalArmSpeed : ArmSpeed;

	private int CurrentMaxArmExtension => Level.OriginalPhysics ? OriginalMaxArmExtension : MaxArmExtension;

	/// <summary>
	/// Gets the position that puts the jar above the protagonist. The egg mobile stays on the side
	/// where it filled the jar.
	/// </summary>
	private int GetAimX()
	{
		ICharacter protagonist = Level.Player.Protagonist;
		return _fillSide != -1 ? protagonist.Position.X + AimDistance : protagonist.Position.X - AimDistance;
	}

	/// <summary>
	/// Moves the egg mobile 1 px per frame toward the aim position, between the original limits.
	/// </summary>
	private void FollowProtagonist()
	{
		int leftLimit = LeftFillX - (_fillSide != -1 ? OriginalFollowNearLimit : OriginalFollowFarLimit);
		int rightLimit = RightFillX + (_fillSide != -1 ? OriginalFollowFarLimit : OriginalFollowNearLimit);
		int aimX = Math.Clamp(GetAimX(), leftLimit, rightLimit);
		_nonHoverPosition.X = MathX.GoTowards(_nonHoverPosition.X, aimX, OriginalFollowSpeed);
	}

	/// <summary>
	/// Returns true when the protagonist is in the original drop range below the jar. When the egg mobile faces
	/// left, the range is 8 px to 32 px right of the jar. When it faces right, the range is 16 px left of the jar
	/// to 8 px right of the jar.
	/// </summary>
	private bool IsProtagonistBelowJar()
	{
		int distance = Level.Player.Protagonist.Position.X - (Position.X + JarOffset.X);
		return _flipX ? distance >= -16 * 4 && distance <= 8 * 4 : distance >= 8 * 4 && distance <= 32 * 4;
	}

	private void EmitSmoke(Vector2 offset, Vector2 velocity)
	{
		Matrix4 matrix = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
		if (_flipX)
			matrix *= Matrix4.CreateScale(-1.0, 1.0);
		Vector2i position = (Vector2i)((matrix * Matrix4.CreateTranslation(offset)).RotateZ(0.0) * new Vector2(0.0, 100.0));
		DustInstance dustInstance = Level.ObjectManager.AddObject(new ObjectPlacement(WaterEggmanType.DefaultBrokenSmokeResourceKey, Level.Map.Layers.IndexOf(Layer), position)) as DustInstance;
		dustInstance.SetDustAnimationIndex(AnimationDustSmoke);
		dustInstance.Velocity = velocity;
	}

	/// <summary>
	/// Moves the boss up and down around the hover position.
	/// </summary>
	private void Hover()
	{
		PositionPrecise = _nonHoverPosition + new Vector2(0.0, Math.Sin(_floatOffsetAngle) / Math.PI * 16.0);
		_floatOffsetAngle += Math.PI / 64.0;
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (Health <= 0)
			return;
		DoRobotnikFrown();
	}

	protected override void Defeat()
	{
		_state = State.Defeated;
		_stateTicks = 0;
		_nonHoverPosition = PositionPrecise;
		DoRobotnikDefeated();
		CollisionRectangles = new CollisionRectangle[0];
		Level.Player.GainScore(DefeatScore);
		Level.CreateScoreObject(DefeatScore, Position + new Vector2i(0, -128));
	}

	protected override void OnHurtCharacter(ICharacter character) => DoRobotnikSmile();

	public void DoRobotnikSmile()
	{
		if (_animationRobotnik.Index != AnimationRobotnikNormal)
			return;
		_animationRobotnik.Index = AnimationRobotnikSmiling;
	}

	public void DoRobotnikFrown() => _animationRobotnik.Index = AnimationRobotnikFrown;

	public void DoRobotnikDefeated() => _animationRobotnik.Index = AnimationRobotnikDefeated;

	public void DoRobotnikNormal() => _animationRobotnik.Index = AnimationRobotnikNormal;

	protected override void OnAnimate()
	{
		switch (_state)
		{
			case State.MoveToSide:
				_animationArmTapCog.Index = AnimationArmCog;
				_animationArmTap.Index = AnimationArmTap;
				break;
			case State.LoweringTube:
				_animationArmTapCog.Index = AnimationArmCog;
				_animationArmTap.Index = AnimationArmTap;
				_jarFillY = 0;
				_jarEmpty = true;
				_dripVisible = false;
				_suctionTube.TubeSuctionIndex = SuctionTube.TubeMaxIndex;
				break;
			case State.FillingJar:
				AnimateFillingJar();
				break;
			case State.DroppingChemical:
				if (_stateTicks == 0)
				{
					_jarLidOpen = true;
					_animationArmJarLidBack.ResetFrame();
					_animationArmJarLidFront.ResetFrame();
					_animationArmJarLidBack.Cycles = 0;
					_animationArmJarLidFront.Cycles = 0;
				}
				break;
			case State.Defeated:
				if (_isFaceSplash)
					_animationRobotnikFaceSplash.Animate();
				break;
		}
		_animationEggMobileChasis.Animate();
		_animationEggMobileScreen.Animate();
		_animationEggMobileCap.Animate();
		_animationRobotnik.Animate();
		_animationArmTurn.Animate();
		_animationBase.Animate();
		_animationArm.Animate();
		_animationArmTap.Animate();
		_animationArmTapCog.Animate();
		_animationArmJar.Animate();
		_animationArmJarReflection.Animate();
		_animationArmJarLidClosed.Animate();
		_animationArmJarLidBack.Animate();
		_animationArmJarLidFront.Animate();
		if (_animationArmJarLidBack.Cycles <= 0)
			return;
		_jarLidOpen = false;
	}

	/// <summary>
	/// Animates the suction tube, the tap and the drips that fill the jar. Each drip falls from the tap
	/// for a few ticks, and then the jar fluid rises one step.
	/// </summary>
	private void AnimateFillingJar()
	{
		if (_stateTicks < SuctionTicks)
		{
			_animationArmTapCog.Index = AnimationArmCogTurn;
			_animationArmTap.Index = AnimationArmTapDrip;
			_suctionTube.TubeSuctionIndex -= SuctionStep;
			if (_suctionTube.TubeSuctionIndex <= 0.0)
				_suctionTube.TubeSuctionIndex = SuctionTube.TubeMaxIndex;
		}
		else if (_suctionTube.TubeSuctionIndex > 0.0)
			_suctionTube.TubeSuctionIndex = Math.Max(0.0f, _suctionTube.TubeSuctionIndex - SuctionStep);
		if (_dripVisible)
			_dripPosition.Y += 12;
		if (_stateTicks == 20)
		{
			_dripVisible = true;
			_dripPosition = new Vector2i();
		}
		else if (_stateTicks == 24)
		{
			_dripVisible = false;
			_animationArmJarFill.Index = AnimationJarFillA;
			_animationArmJarFill.ResetFrame();
			_jarEmpty = false;
		}
		else if (_stateTicks == 50)
		{
			_dripVisible = true;
			_dripPosition = new Vector2i();
		}
		else if (_stateTicks == 54)
		{
			_dripVisible = false;
			_animationArmJarFill.Index = AnimationJarFillB;
		}
		else if (_stateTicks == 80)
		{
			_dripVisible = true;
			_dripPosition = new Vector2i();
		}
		else if (_stateTicks == 84)
		{
			_dripVisible = false;
			_animationArmJarFill.Index = AnimationJarFillC;
			_jarFillY -= JarFillStep;
		}
		else if (_stateTicks == 110)
		{
			_dripVisible = true;
			_dripPosition = new Vector2i();
		}
		else if (_stateTicks == 114)
		{
			_dripVisible = false;
			_animationArmJarFill.Index = AnimationJarFillC;
			_animationArmJarFill.ResetFrame();
			_jarFillY -= JarFillStep;
		}
		else if (_stateTicks == 138)
		{
			_dripVisible = true;
			_dripPosition = new Vector2i();
		}
		else if (_stateTicks == 139)
			_dripVisible = false;
		else if (_stateTicks == 140)
		{
			_animationArmJarFill.Index = AnimationJarFillC;
			_animationArmJarFill.ResetFrame();
			_jarFillY -= JarFillStep;
		}
		_animationArmJarFill.Animate();
		_animationArmJarFilled.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		DrawEggMobile(renderer.GetObjectRenderer());
	}

	private void DrawEggMobile(IObjectRenderer or)
	{
		using (or.BeginMatixState())
		{
			if (_flipX)
				or.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			Vector2i chasisDestination = new Vector2i(-26);
			Vector2i capDestination = chasisDestination;
			Vector2i robotnikDestination = chasisDestination + new Vector2i(0, -76);
			Vector2i faceSplashDestination = chasisDestination + new Vector2i(25, -223);
			Vector2i screenDestination = chasisDestination;
			or.FilterAmount *= 0.5;
			if (IsInvincibleFlashing)
				or.AdditiveColour = BossObject.FlashAdditiveColour;
			or.Render(_animationEggMobileCap, (Vector2)capDestination);
			or.Render(_animationEggMobileChasis, (Vector2)chasisDestination);
			or.AdditiveColour = Colours.Black;
			if (_isFaceSplash)
				or.Render(_animationRobotnikFaceSplash, (Vector2)faceSplashDestination);
			else
				or.Render(_animationRobotnik, (Vector2)robotnikDestination);
			if (IsInvincibleFlashing)
				or.AdditiveColour = BossObject.FlashAdditiveColour;
			or.Render(_animationEggMobileScreen, (Vector2)screenDestination);
		}
		using (or.BeginMatixState())
		{
			if (_flipX)
				or.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			// The arm turns with the egg mobile. Its height follows the frames of the turn animation.
			Vector2i armOffset = new Vector2i(60, -171);
			if (_animationArmTurn.Index == AnimationArmTurn1)
			{
				if (_animationArmTurn.CurrentFrameIndex == 0)
					armOffset.Y = -172;
				else if (_animationArmTurn.CurrentFrameIndex > 0)
					armOffset.Y = -195;
			}
			if (_animationArmTurn.Index == AnimationArmTurn2)
			{
				if (_animationArmTurn.CurrentFrameIndex == 3)
					armOffset.Y = -172;
				else if (_animationArmTurn.CurrentFrameIndex == 2)
					armOffset.Y = -172;
				else if (_animationArmTurn.CurrentFrameIndex < 3)
					armOffset.Y = -195;
			}
			or.ModelMatrix *= Matrix4.CreateTranslation(armOffset.X, armOffset.Y);
			Vector2i armTurnDestination = new Vector2i(-64, 0);
			if (!_turning || _state >= State.Defeated)
				return;
			or.Render(_animationArmTurn, (Vector2)armTurnDestination);
		}
	}

	/// <summary>
	/// Releases a number of broken pieces in random directions. If explode is true, also makes an explosion.
	/// </summary>
	private void ReleaseFragmentMetal(Vector2i position, double speed, int count = 1, bool explode = true)
	{
		for (int index = 0; index < count; ++index)
		{
			double angle = Level.Random.NextRadians();
			Vector2 velocity = new Vector2(speed * Math.Cos(angle), speed * Math.Sin(angle));
			ReleaseFragmentMetal(position, velocity);
		}
		if (!explode)
			return;
		ExplodeAt(position);
	}

	private void ReleaseFragmentMetal(Vector2i position, Vector2 velocity)
	{
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = Level.Random.Next(AnimationBrokenPieces, AnimationBrokenPieces + BrokenPiecesCount);
		fragment.Position = position;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = Math.PI / 30.0;
		fragment.FlipX = Level.Random.NextBoolean();
		fragment.Initialise();
	}

	/// <summary>
	/// Starts the explosion of a part. Returns true while the part is still exploding.
	/// </summary>
	private static bool Explode(ref ExplodeState explodeState)
	{
		if (explodeState == ExplodeState.NotExploding)
		{
			explodeState = ExplodeState.Exploding;
			return true;
		}
		return explodeState == ExplodeState.Exploding;
	}

	public enum State
	{
		MoveToSide,
		LoweringTube,
		FillingJar,
		RaisingTube,
		Aiming,
		DroppingChemical,
		Defeated,
		Recover,
		Fleeing,
	}

	private enum ExplodeState
	{
		NotExploding,
		Exploding,
		Exploded,
		Falling,
	}

	/// <summary>
	/// The boiler below the egg mobile, with the tap and the light. After the defeat it explodes and falls.
	/// </summary>
	private class Boiler : ActiveObject
	{
		private static Vector2i OriginOffset = new Vector2i(60, -176);
		private Updater _updater;
		private bool _flipX;
		private ExplodeState _explodeState;
		private AnimationInstance _explodedAnimationInstance;
		private Vector2 _velocity;
		private double _angle;

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			_updater = new Updater(GetUpdates());
			Priority = 300;
			LockLifetime = true;
		}

		protected override void OnUpdate() => _updater.Update();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (Parent._turning)
				return;
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				if (_flipX)
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				objectRenderer.ModelMatrix *= Matrix4.CreateTranslation(OriginOffset.X, OriginOffset.Y);
				if (_explodeState == ExplodeState.Falling)
					objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(_angle);
				objectRenderer.Render(Parent._animationBaseShadow, new Vector2(3.0, 100.0));
				if (Parent.IsInvincibleFlashing)
					objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
				if (_explodeState < ExplodeState.Exploded)
				{
					objectRenderer.Render(Parent._animationBase, new Vector2(0.0, 0.0));
					objectRenderer.Render(Parent._animationArmTap, new Vector2(-69.0, -68.0));
					objectRenderer.Render(Parent._animationArmTapCog, new Vector2(8.0, -75.0));
					if (Parent._jarFull)
						objectRenderer.Render(Parent._animationBaseLightOn, new Vector2(-42.0, 12.0));
					else
						objectRenderer.Render(Parent._animationBaseLightOff, new Vector2(-42.0, 12.0));
				}
				else
					objectRenderer.Render(_explodedAnimationInstance, new Vector2(-24.0, 0.0));
			}
		}

		private Vector2i GetAbsolutePosition(Vector2i offset)
		{
			Vector2i position = OriginOffset + offset;
			if (_flipX)
				position.X *= -1;
			return Position + position;
		}

		/// <summary>
		/// The script of the boiler. It follows the egg mobile until it explodes. Then it waits until it falls,
		/// and it turns as it falls, slower in the water.
		/// </summary>
		private IEnumerable<UpdateResult> GetUpdates()
		{
			while (_explodeState == ExplodeState.NotExploding)
			{
				_flipX = Parent._flipX;
				Position = Parent.Position;
				if (Parent.Finished)
					Finish();
				yield return UpdateResult.Next();
			}
			Parent.ReleaseFragmentMetal(GetAbsolutePosition(new Vector2i(4, 100)), 8.0);
			yield return UpdateResult.Wait(2);
			Parent.ReleaseFragmentMetal(GetAbsolutePosition(new Vector2i(0, 0)), 8.0);
			yield return UpdateResult.Wait(2);
			Parent.ReleaseFragmentMetal(GetAbsolutePosition(new Vector2i(-69, -68)), 8.0);
			yield return UpdateResult.Next();
			_explodeState = ExplodeState.Exploded;
			_explodedAnimationInstance = new AnimationInstance(Parent._animationGroup, AnimationBrokenBoiler);
			yield return UpdateResult.Wait(60);
			int flipMultiplier = _flipX ? -1 : 1;
			while (_explodeState != ExplodeState.Falling)
				yield return UpdateResult.Next();
			Parent.ReleaseFragmentMetal(GetAbsolutePosition(new Vector2i(0, 100)), 8.0);
			for (int i = 0; i < 12; ++i)
			{
				Move(flipMultiplier, 1);
				yield return UpdateResult.Next();
			}
			LockLifetime = false;
			bool isUnderwater = false;
			while (true)
			{
				if (!isUnderwater)
				{
					if (Level.WaterManager.IsUnderwater(Position))
					{
						isUnderwater = true;
						_velocity = new Vector2(0.0, 1.0);
						Level.WaterManager.CreateSplash(Layer, SplashType.Enter, Position);
					}
					else
					{
						_angle += Math.PI / 30.0 * flipMultiplier;
						_velocity += new Vector2(0.0, 0.875);
					}
				}
				else
				{
					_angle += Math.PI / 75.0 * flipMultiplier;
					_velocity += new Vector2(0.0, 7.0 / 16.0);
				}
				MovePrecise(_velocity);
				if (Level.Ticks % 4 == 0)
					EmitSmoke();
				yield return UpdateResult.Next();
			}
		}

		private void EmitSmoke()
		{
			Matrix4 matrix = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
			if (_flipX)
				matrix *= Matrix4.CreateScale(-1.0, 1.0);
			Vector2i position = (Vector2i)((matrix * Matrix4.CreateTranslation((Vector2)OriginOffset)).RotateZ(_angle) * new Vector2(0.0, 100.0));
			(Level.ObjectManager.AddObject(new ObjectPlacement(WaterEggmanType.DefaultBrokenSmokeResourceKey, Level.Map.Layers.IndexOf(Layer), position)) as DustInstance).SetDustAnimationIndex(AnimationDustSmoke);
		}

		public bool Explode() => WaterEggmanInstance.Explode(ref _explodeState);

		public void Fall() => _explodeState = ExplodeState.Falling;
	}

	/// <summary>
	/// The back of the jar: the reflection and the back half of the open lid. It draws behind the arm.
	/// </summary>
	private class JarBack : ActiveObject
	{
		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			Priority = 310;
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			if (!Parent.Finished)
				return;
			Finish();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (Parent._turning)
				return;
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			WaterEggmanInstance parent = Parent;
			if (parent._state == State.Fleeing)
				return;
			using (objectRenderer.BeginMatixState())
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateTranslation((Vector2)parent.JarOffset);
				if (parent._flipX)
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				objectRenderer.Render(parent._animationArmJarReflection);
				if (!parent._jarLidOpen)
					return;
				objectRenderer.Render(parent._animationArmJarLidBack, new Vector2(-27.0, 69.0));
			}
		}
	}

	/// <summary>
	/// The arm of the egg mobile and the jar at its end, with the drip and the fluid. After the defeat,
	/// the jar breaks and the arm bars break off one by one.
	/// </summary>
	private class ArmFront : ActiveObject
	{
		private static readonly Vector2i ArmBeginPoint = new Vector2i(10, -186);

		/// <summary>
		/// The length of one arm bar.
		/// </summary>
		private const int ArmBarLength = 16;

		/// <summary>
		/// The position of the bottom of the jar fluid, relative to the jar.
		/// </summary>
		private const int JarFluidBottomY = 47;

		/// <summary>
		/// The distance between two filled fluid sections.
		/// </summary>
		private const int JarFilledSectionHeight = 11;

		private Updater _updater;
		private bool _flipX;
		private ExplodeState _jarExplodeState;
		private ExplodeState _barExplodeState;
		private int _barExplodedLength;

		public Vector2 Velocity { get; set; }

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			_updater = new Updater(GetUpdates());
			Priority = 330;
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			_updater.Update();
			Parent._jarBack.Position = Position;
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (Parent._turning)
				return;
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			DrawArm(objectRenderer);
			if (_jarExplodeState != ExplodeState.NotExploding)
				return;
			DrawJar(objectRenderer, (Vector2)Parent.JarOffset);
		}

		private void DrawArm(IObjectRenderer or)
		{
			if (Parent.IsInvincibleFlashing)
				or.AdditiveColour = BossObject.FlashAdditiveColour;
			using (or.BeginMatixState())
			{
				if (_flipX)
					or.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				foreach (Vector2i armBarPosition in GetArmBarPositions())
					or.Render(Parent._animationArm, (Vector2)armBarPosition);
			}
			or.AdditiveColour = Colours.Black;
		}

		private void DrawJar(IObjectRenderer or, Vector2 offset)
		{
			if (Parent._dripVisible)
				or.Render(Parent._animationDrip, offset + (Vector2)Parent._dripPosition + (Vector2)new Vector2i(_flipX ? -8 : 8, -32), _flipX);
			if (!Parent._jarEmpty)
			{
				int x = _flipX ? 8 : -8;
				bool hasFilledSections = Parent._jarFillY < 0;

				// Draw the filled sections of the fluid, from the bottom up.
				for (int sectionY = JarFluidBottomY; sectionY > JarFluidBottomY + Parent._jarFillY; sectionY -= JarFilledSectionHeight)
				{
					Rectanglei source = Parent._animationArmJarFilled.CurrentFrame.Source;
					Rectanglei destination = new Rectanglei((int)offset.X + x - source.Width / 2, (int)offset.Y + sectionY - 16 - source.Height / 2, source.Width, source.Height);
					or.Texture = Parent._animationArmJarFilled.CurrentTexture;
					or.Render((Rectangle)source, (Rectangle)destination, _flipX);
				}

				// Draw the fluid surface.
				int surfaceY = JarFluidBottomY + Parent._jarFillY - Parent._animationArmJarFill.CurrentFrame.Source.Height / 2;
				if (Parent._animationArmJarFill.Index == AnimationJarFillC)
					surfaceY += 17;
				or.Render(Parent._animationArmJarFill, offset + new Vector2(x, surfaceY), _flipX);

				// Draw the bottom of the fluid. The first and the last parts use the primary animation, and
				// the last part is flipped vertically. The parts between use the secondary animation.
				if (Parent._animationArmJarFill.Index >= AnimationJarFillB && hasFilledSections)
				{
					int bottomPartCount = Parent._jarFillY / -JarFillStep;
					for (int index = 0; index < bottomPartCount; ++index)
					{
						Vector2 destination = offset + new Vector2(x, JarFluidBottomY + index * -JarFillStep - 16 + Math.Sign(index) * 8 * index);
						switch (bottomPartCount)
						{
							case 1:
								or.Render(Parent._animationArmJarFillPlaceholderPrimary, destination, _flipX);
								break;
							case 2:
								if (index == 0)
								{
									or.Render(Parent._animationArmJarFillPlaceholderPrimary, destination, _flipX);
									break;
								}
								or.Render(Parent._animationArmJarFillPlaceholderPrimary, destination, _flipX, true);
								break;
							default:
								if (index == 0)
								{
									or.Render(Parent._animationArmJarFillPlaceholderPrimary, destination, _flipX);
									break;
								}
								if (index == bottomPartCount - 1)
								{
									or.Render(Parent._animationArmJarFillPlaceholderPrimary, destination, _flipX, true);
									break;
								}
								or.Render(Parent._animationArmJarFillPlaceholderSecondary, destination, _flipX);
								break;
						}
					}
				}
			}
			if (Parent.IsInvincibleFlashing)
				or.AdditiveColour = BossObject.FlashAdditiveColour;
			or.Render(Parent._animationArmJar, offset, _flipX);
			or.AdditiveColour = Colours.Black;
			if (Parent._jarLidOpen)
				or.Render(Parent._animationArmJarLidFront, offset + new Vector2(_flipX ? -14.0 : 14.0, 66.0), _flipX);
			else
				or.Render(Parent._animationArmJarLidClosed, offset + new Vector2(_flipX ? 8.0 : -8.0, 53.0), _flipX);
		}

		/// <summary>
		/// The script of the arm. It follows the egg mobile until the jar explodes. When the bar explosion starts,
		/// the broken jar falls and the bars break off one by one, from the end of the arm.
		/// </summary>
		private IEnumerable<UpdateResult> GetUpdates()
		{
			while (_jarExplodeState == ExplodeState.NotExploding)
			{
				_flipX = Parent._flipX;
				Position = Parent.Position;
				if (Parent.Finished)
					Finish();
				yield return UpdateResult.Next();
			}
			BrokenJar brokenJar = CreateBrokenJar();
			_jarExplodeState = ExplodeState.Exploded;
			while (_barExplodeState != ExplodeState.Exploding)
				yield return UpdateResult.Next();
			brokenJar.Fall();
			_jarExplodeState = ExplodeState.Falling;
			yield return UpdateResult.Wait(2);
			// Take the positions before the loop, because the loop makes the arm shorter.
			Vector2i[] armBarPositions = GetArmBarPositions().Reverse().ToArray();
			for (int index = 0; index < armBarPositions.Length; ++index)
			{
				Vector2i armBarPosition = armBarPositions[index];
				_barExplodedLength += ArmBarLength;
				EmitBar(Position + armBarPosition);
				yield return UpdateResult.Wait(4);
			}
			_barExplodeState = ExplodeState.Exploded;
		}

		/// <summary>
		/// Gets the positions of the arm bars, from the egg mobile to the end of the arm.
		/// The bars that broke off are not included.
		/// </summary>
		private IEnumerable<Vector2i> GetArmBarPositions()
		{
			for (Vector2i position = ArmBeginPoint; position.X >= -Parent._armExtension + 4 + _barExplodedLength; position.X -= ArmBarLength)
				yield return position;
		}

		private BrokenJar CreateBrokenJar()
		{
			Parent._jarBack.Finish();
			Vector2i position = Position + Parent.JarOffset;
			Parent.ReleaseFragmentMetal(position, 12.0, 12);
			BrokenJar brokenJar = Level.ObjectManager.AddSubObject<BrokenJar>(Parent);
			brokenJar.Position = position;
			return brokenJar;
		}

		private void EmitBar(Vector2i position)
		{
			Vector2 velocity = new Vector2(Level.Random.NextSign() * Level.Random.NextDouble(2.0, 8.0), -12.0);
			Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
			fragment.AnimationGroup = Parent._animationGroup;
			fragment.AnimationIndex = AnimationBrokenBar;
			fragment.Position = position;
			fragment.Velocity = velocity;
			fragment.AngularVelocity = Math.PI / 20.0;
			fragment.Initialise();
			Parent.ExplodeAt(position);
		}

		public bool ExplodeJar() => WaterEggmanInstance.Explode(ref _jarExplodeState);

		public bool ExplodeBar() => WaterEggmanInstance.Explode(ref _barExplodeState);
	}

	/// <summary>
	/// The broken jar. It falls and turns, slower in the water.
	/// </summary>
	private class BrokenJar : ActiveObject
	{
		private AnimationInstance _animation;
		private bool _falling;
		private Vector2 _velocity;
		private double _angle;
		private bool _isUnderwater;

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			_animation = new AnimationInstance(Parent._animationGroup, AnimationBrokenJar);
		}

		protected override void OnUpdate()
		{
			if (!_falling)
				return;
			int flipMultiplier = Parent._flipX ? -1 : 1;
			if (!_isUnderwater)
			{
				if (Level.WaterManager.IsUnderwater(Position))
				{
					_isUnderwater = true;
					_velocity = new Vector2(0.0, 1.0);
					Level.WaterManager.CreateSplash(Layer, SplashType.Enter, Position);
				}
				else
				{
					_angle -= Math.PI / 30.0 * flipMultiplier;
					_velocity += new Vector2(0.0, 0.875);
				}
			}
			else
			{
				_angle -= Math.PI / 75.0 * flipMultiplier;
				_velocity += new Vector2(0.0, 7.0 / 16.0);
			}
			MovePrecise(_velocity);
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(_angle);
				objectRenderer.Render(_animation, Parent._flipX);
			}
		}

		public void Fall() => _falling = true;
	}

	/// <summary>
	/// The suction tube that goes down into the chemical water. After the defeat it explodes and falls.
	/// </summary>
	private class SuctionTube : ActiveObject
	{
		public const int TubeMaxIndex = 22;

		/// <summary>
		/// The top of the tube relative to the egg mobile, and the height of one tube segment.
		/// </summary>
		private const int TubeTopY = 80;
		private const int SegmentHeight = 16;

		private Updater _updater;
		private bool _flipX;
		private int _numSegments;

		/// <summary>
		/// The suction animation frame of each tube segment. Frame 0 is the tube without suction.
		/// </summary>
		private int[] _frameMap = new int[26];
		private ExplodeState _explodeState;
		private AnimationInstance _explodedAnimationInstance;
		private Vector2 _velocity;

		/// <summary>
		/// The angles of the two kinds of broken tube pieces. The pieces alternate along the tube.
		/// </summary>
		private double[] _angle = new double[2];

		/// <summary>
		/// The position of the suction bulge along the tube, in segments.
		/// </summary>
		public float TubeSuctionIndex { get; set; }

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			_updater = new Updater(GetUpdates());
			Priority = 124;
			LockLifetime = true;
		}

		protected override void OnUpdate() => _updater.Update();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			int x = _flipX ? 14 : -14;
			int y = TubeTopY;
			if (_explodeState <= ExplodeState.Exploded)
			{
				Animation animation = Parent._animationGroup[AnimationTubeSucking];
				int[] frameMap = _frameMap;
				Array.Clear(frameMap, 0, frameMap.Length);
				// Put the bulge frames on the segments at the suction index. The half steps use other frames.
				int tubeSuctionIndex = (int)TubeSuctionIndex;
				if ((double)TubeSuctionIndex - tubeSuctionIndex < 0.5)
				{
					frameMap[tubeSuctionIndex] = 1;
					frameMap[tubeSuctionIndex + 1] = 2;
					frameMap[tubeSuctionIndex + 2] = 4;
					frameMap[tubeSuctionIndex + 3] = 6;
				}
				else
				{
					frameMap[tubeSuctionIndex + 2] = 3;
					frameMap[tubeSuctionIndex + 3] = 5;
				}
				for (int index = 0; index < _numSegments; ++index)
				{
					Animation.Frame frame = animation.Frames[index + 4 >= TubeMaxIndex ? 0 : frameMap[index + 4]];
					objectRenderer.Texture = Parent._animationGroup.Textures[frame.TextureIndex];
					objectRenderer.Render((Rectangle)frame.Source, new Vector2(x, y));
					y += SegmentHeight;
				}
			}
			else
			{
				// Draw the broken tube pieces down to the bottom of the tube. Each piece turns on its own.
				int bottomY = TubeTopY + _numSegments * SegmentHeight;
				int angleIndex = 0;
				int pieceY = y - 8;
				while (pieceY < bottomY)
				{
					Rectanglei source = _explodedAnimationInstance.CurrentFrame.Source;
					Rectanglei destination = (Rectanglei)new Rectangle(x - source.Width / 2, pieceY, source.Width, source.Height);
					if (destination.Bottom > bottomY)
					{
						destination.Bottom = bottomY;
						source.Height = destination.Height;
					}
					using (objectRenderer.BeginMatixState())
					{
						objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.Translate(destination.Centre.X, destination.Centre.Y).RotateZ(_angle[angleIndex]);
						destination.X = -destination.Width / 2;
						destination.Y = -destination.Height / 2;
						objectRenderer.Texture = _explodedAnimationInstance.CurrentTexture;
						objectRenderer.Render((Rectangle)source, (Rectangle)destination);
						pieceY += source.Height;
					}
					angleIndex = (angleIndex + 1) % 2;
				}
			}
		}

		/// <summary>
		/// The script of the tube. It follows the egg mobile until it explodes. The explosions go down the tube.
		/// Then it waits until it falls.
		/// </summary>
		private IEnumerable<UpdateResult> GetUpdates()
		{
			while (_explodeState == ExplodeState.NotExploding)
			{
				_flipX = Parent._flipX;
				_numSegments = (Parent._tubeY - TubeTopY) / SegmentHeight;
				Position = Parent.Position;
				if (Parent.Finished)
					Finish();
				yield return UpdateResult.Next();
			}
			int maxY = TubeTopY + _numSegments * SegmentHeight;
			int x = _flipX ? 32 : -24;
			int y = TubeTopY;
			while (y < maxY)
			{
				Parent.ReleaseFragmentMetal(Position + new Vector2i(x, y), 4.0);
				y += 136;
				yield return UpdateResult.Wait(4);
			}
			_explodeState = ExplodeState.Exploded;
			_explodedAnimationInstance = new AnimationInstance(Parent._animationGroup, AnimationBrokenTube);
			while (_explodeState != ExplodeState.Falling)
				yield return UpdateResult.Next();
			Parent.ReleaseFragmentMetal(Position + new Vector2i(0, TubeTopY), 4.0);
			LockLifetime = false;
			while (true)
			{
				_angle[0] += Math.PI / 30.0;
				_angle[1] -= Math.PI / 20.0;
				_velocity += new Vector2(0.0, 0.875);
				MovePrecise(_velocity);
				yield return UpdateResult.Next();
			}
		}

		public bool Explode() => WaterEggmanInstance.Explode(ref _explodeState);

		public void Fall() => _explodeState = ExplodeState.Falling;
	}

	/// <summary>
	/// The chemical that drops from the jar. It hurts characters. When it hits the floor, it splashes into
	/// small projectiles. It goes away in the water.
	/// In the original physics, the hit box is 12 x 12 px and the floor check uses a radius of 32 px. Thus the
	/// chemical stops above the floor, and a character that ducks is safe. On the floor, the chemical and the
	/// projectiles are harmless.
	/// </summary>
	private class ChemicalDroplet : Enemy
	{
		private const int FloorRadius = 64;
		private const int OriginalFloorRadius = 128;

		private AnimationInstance _animationInstance;
		private double _velocityY;
		private Updater _updater;
		private bool _splashed;
		public bool FlipX;

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			base.OnStart();
			_updater = new Updater(GetUpdates());
			_animationInstance = new AnimationInstance(Parent._animationGroup, AnimationJarDroplet);
			Priority = 320;
			CollisionRectangles = new CollisionRectangle[]
			{
				Level.OriginalPhysics ? new CollisionRectangle(this, 0, -24, -24, 48, 48) : new CollisionRectangle(this, 0, -64, -64, 128, 128)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			_updater.Update();
		}

		private IEnumerable<UpdateResult> GetUpdates()
		{
			do
			{
				yield return UpdateResult.Next();
				_velocityY += 0.875;
				MovePrecise(0.0, _velocityY);
				if (Level.WaterManager.IsUnderwater(Position))
				{
					Parent._chemicalLanded = true;
					Finish();
					yield break;
				}
			}
			while (!HasHitFloor());
			Parent._chemicalLanded = true;
			if (Level.OriginalPhysics)
			{
				// Put the splash on the floor.
				CollisionRectangles = new CollisionRectangle[0];
				MovePrecise(0.0, OriginalFloorRadius - FloorRadius);
			}
			EmitProjectiles();
			EmitSmallProjectile(new Vector2i(-64, -12));
			EmitSmallProjectile(new Vector2i(-38, -22));
			EmitSmallProjectile(new Vector2i(32, -22));
			EmitSmallProjectile(new Vector2i(60, -14));
			_splashed = true;
			_animationInstance = new AnimationInstance(Parent._animationGroup, AnimationSplash);
			while (_animationInstance.Cycles == 0)
				yield return UpdateResult.Next();
			Finish();
		}

		/// <summary>
		/// Checks for a floor below the droplet. If there is one, moves the droplet onto it and returns true.
		/// Objects that own a floor can tell the droplet to ignore it.
		/// </summary>
		private bool HasHitFloor()
		{
			double shift = double.MaxValue;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, Level.OriginalPhysics ? OriginalFloorRadius : FloorRadius, FloorRadius))
			{
				if (collision.Vector.Mode == CollisionMode.Top)
				{
					ActiveObject owner = collision.Vector.Owner;
					CollisionEvent e = new CollisionEvent(this, collision);
					if (owner != null)
					{
						owner.Collision(e);
						if (e.IgnoreCollision)
							continue;
					}
					shift = Math.Min(shift, collision.Shift);
				}
			}
			if (shift == double.MaxValue)
				return false;
			MovePrecise(0.0, shift);
			return true;
		}

		private void EmitProjectiles()
		{
			for (int index = 0; index < 5; ++index)
			{
				ChemicalDropletProjectile dropletProjectile = Level.ObjectManager.AddSubObject<ChemicalDropletProjectile>(Parent);
				dropletProjectile.Position = Position;
				dropletProjectile.Velocity = new Vector2(Level.Random.NextSign() * Level.Random.Next(2, 5), -_velocityY - Level.Random.Next(0, 4));
			}
		}

		/// <summary>
		/// Emits a small splash projectile at the offset. It moves away from the droplet in the direction of the offset.
		/// </summary>
		private void EmitSmallProjectile(Vector2i offset)
		{
			SmallChemicalDropletProjectile dropletProjectile = Level.ObjectManager.AddSubObject<SmallChemicalDropletProjectile>(ParentObject);
			dropletProjectile.Position = Position + offset;
			double angle = Math.Atan2(offset.Y, offset.X);
			dropletProjectile.Velocity = new Vector2(8.0 * Math.Cos(angle), 32.0 * Math.Sin(angle));
		}

		protected override void OnHurtCharacter(ICharacter character) => Parent.DoRobotnikSmile();

		protected override void OnAnimate()
		{
			base.OnAnimate();
			_animationInstance.Animate();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				if (FlipX)
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				if (!_splashed)
				{
					objectRenderer.Render(_animationInstance);
				}
				else
				{
					if (_animationInstance.Cycles != 0)
						return;
					// Draw the splash so that its bottom is on the floor.
					Vector2 splashOffset = new Vector2(32.0, 0.0);
					Rectanglei source = _animationInstance.CurrentFrame.Source;
					Rectanglei destination = (Rectanglei)new Rectangle(-source.Width / 2 + splashOffset.X, 64 - source.Height, source.Width, source.Height);
					objectRenderer.Texture = _animationInstance.CurrentTexture;
					objectRenderer.Render((Rectangle)source, (Rectangle)destination);
				}
			}
		}
	}

	/// <summary>
	/// A drop of chemical from the splash. It hurts characters, falls, and goes away on the floor or in the water.
	/// </summary>
	private class ChemicalDropletProjectile : Enemy
	{
		private AnimationInstance _animationInstance;

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			_animationInstance = new AnimationInstance(Parent._animationGroup, AnimationSplashSmallDrop);
			Priority = 320;
			// In the original physics, the drops of the splash are harmless.
			CollisionRectangles = Level.OriginalPhysics ? new CollisionRectangle[0] : new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -8, -8, 16, 16)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Velocity += new Vector2(0.0, 0.875);
			PositionPrecise += Velocity;
			if (!Level.WaterManager.IsUnderwater(Position) && !HasHitFloor())
				return;
			Finish();
		}

		private bool HasHitFloor()
		{
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 8))
			{
				if (collision.Vector.Mode == CollisionMode.Top)
					return true;
			}
			return false;
		}

		protected override void OnHurtCharacter(ICharacter character) => Parent.DoRobotnikSmile();

		protected override void OnAnimate() => _animationInstance.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(_animationInstance);
		}
	}

	/// <summary>
	/// A small drop of chemical from the splash. It is only an effect and does not hurt characters.
	/// </summary>
	private class SmallChemicalDropletProjectile : ParticleObject
	{
		public Vector2 Velocity { get; set; }

		public SmallChemicalDropletProjectile()
			: base(WaterEggmanType.AnimationGroupResourceKey, AnimationSplashDrop, 0)
		{
		}

		protected override void OnUpdate()
		{
			Velocity += new Vector2(0.0, 0.875);
			MovePrecise(Velocity);
			if (Level.WaterManager.IsUnderwater(Position))
				Finish();
			base.OnUpdate();
		}
	}
}
