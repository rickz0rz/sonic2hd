using System;
using SonicOrca.Audio;
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

namespace SONICORCA.OBJECTS.DRILLEGGMAN;

/// <summary>
/// The Emerald Hill Zone boss. Eggman flies in with the egg mobile and lands on a car with a drill at the front.
/// The car drives from side to side between the left and right edges. At 1 health the car releases the drill.
/// When the boss is defeated, the car breaks apart and Eggman flies away to the right.
/// </summary>
public class DrillEggmanInstance : BossObject
{
	private const int AnimationCar = 0;
	private const int AnimationWheelFrontMoving = 1;
	private const int AnimationWheelFrontStill = 2;
	private const int AnimationWheelBackMoving = 3;
	private const int AnimationWheelBackStill = 4;
	private const int AnimationDrill = 5;
	private const int AnimationPropellerStatic = 6;
	private const int AnimationPropellerOut = 7;
	private const int AnimationPropellerSpinning = 8;
	private const int AnimationPropellerIn = 9;
	private const int AnimationCarLight = 10;
	private const int AnimationBrokenCar = 11;
	private const int AnimationBrokenVent = 12;
	private const int AnimationGlassFragment = 13;
	private const int AnimationMetalFragment = 14;
	private const int AnimationRedFragment = 15;
	private const int AnimationPipeFragment = 16;
	private const int AnimationSpark = 17;
	private const int AnimationEggMobileChasis = 18;
	private const int AnimationEggMobileChasisFlame = 19;
	private const int AnimationEggMobileScreen = 20;
	private const int AnimationEggMobileCap = 21;
	private const int AnimationEggMobileChasisTurn1 = 22;
	private const int AnimationEggMobileChasisTurn2 = 23;
	private const int AnimationEggMobileScreenTurn1 = 24;
	private const int AnimationEggMobileScreenTurn2 = 25;
	private const int AnimationBrokenEggMobileChasis = 26;
	private const int AnimationBrokenEggMobileChasisFlame = 27;
	private const int AnimationBrokenEggMobileScreen = 28;
	private const int AnimationBrokenEggMobileCap = 29;
	private const int AnimationBrokenEggMobileChasisTurn1 = 30;
	private const int AnimationBrokenEggMobileChasisTurn2 = 31;
	private const int AnimationBrokenEggMobileScreenTurn1 = 32;
	private const int AnimationBrokenEggMobileScreenTurn2 = 33;
	private const int AnimationEggMobileCapTurn1 = 34;
	private const int AnimationEggMobileCapTurn2 = 35;
	private const int AnimationRobotnikNormal = 0;
	private const int AnimationRobotnikSmiling = 1;
	private const int AnimationRobotnikFrown = 2;
	private const int AnimationRobotnikDefeated = 3;
	private const int AnimationRobotnikTurn1 = 4;
	private const int AnimationRobotnikTurn2 = 5;
	private const int AnimationDustSmoke = 1;
	private const double Gravity = 0.875;

	private const int InitialHealth = 8;
	private const int DefeatScore = 1000;

	/// <summary>
	/// The car starts this distance below the start position. The egg mobile starts at the start position.
	/// </summary>
	private const int CarOffsetY = 1176;

	/// <summary>
	/// The distance that the egg mobile flies to the left before it starts to move down.
	/// </summary>
	private const int ApproachDistance = 1152;
	private const int ApproachSpeed = 4;
	private const int CarSpeed = 8;

	/// <summary>
	/// The car turns around when the egg mobile is this distance past the left or right edge.
	/// </summary>
	private const int TurnMargin = 512;

	/// <summary>
	/// In the original physics, the car turns around at fixed positions, relative to the left edge.
	/// The left edge is at the left limit of the camera in the original ($28F0). The original turn points
	/// are $28A0 and $2B08, a lap of 616 px.
	/// </summary>
	private const int OriginalTurnLeftOffset = -80 * 4;
	private const int OriginalTurnRightOffset = 536 * 4;

	private const int LowerPropellerTicks = 60;
	private const int DefeatedTicks = 179;
	private const int PostDefeatIdleTicks = 12;
	private const int PostDefeatPropellerTicks = 50;
	private const int FleeRiseTicks = 146;
	private const int OriginalFleeRiseTicks = 96;

	/// <summary>
	/// The size of the area around the car in which explosions appear after the defeat.
	/// </summary>
	private const int ExplosionAreaSize = 128;

	private AnimationGroup _animationGroup;
	private AnimationInstance _animation;
	private AnimationInstance _carLightAnimation;
	private AnimationInstance _sparkAnimation;
	private AnimationInstance _brokenVentAnimation;
	private StatusType _status;
	private Vector2i _startPosition;
	private int _stateTimer;
	private int _velocityX;
	private double _velocityY;
	private EggMobile _eggMobile;
	private Drill _drill;
	private Wheel[] _wheels = new Wheel[4];

	/// <summary>
	/// The brightness of the flash that the car makes when it sparks at 1 health.
	/// </summary>
	private float _lastHealthFlash;

	[HideInEditor]
	public StatusType Status => _status;

	[StateVariable]
	public int LeftEdge { get; set; }

	[StateVariable]
	public int RightEdge { get; set; }

	protected override void OnStart()
	{
		ResourceTree resourceTree = Level.GameContext.ResourceTree;
		AnimationGroup animationGroup = _animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, DrillEggmanType.AnimationGroupResourceKey);
		// The result is not used. The egg mobile gets the Robotnik animation group again.
		resourceTree.GetLoadedResource<AnimationGroup>(Type, DrillEggmanType.RobotnikAnimationGroupResourceKey);
		_animation = new AnimationInstance(animationGroup);
		_carLightAnimation = new AnimationInstance(animationGroup, AnimationCarLight);
		_sparkAnimation = new AnimationInstance(animationGroup, AnimationSpark);
		_brokenVentAnimation = new AnimationInstance(animationGroup, AnimationBrokenVent);
		ExplosionResourceKey = Type.GetAbsolutePath(DrillEggmanType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(DrillEggmanType.BossHitSoundResourceKey);
		LockLifetime = true;
		_startPosition = Position;
		Position += new Vector2i(0, CarOffsetY);
		_eggMobile = Level.ObjectManager.AddSubObject<EggMobile>(this);
		_eggMobile.Position = _startPosition;
		for (int index = 0; index < 4; ++index)
		{
			_wheels[index] = Level.ObjectManager.AddSubObject<Wheel>(this);
			_wheels[index].Index = index;
			_wheels[index].LockLifetime = true;
			_wheels[index].AutoPositionToCar();
		}
		_drill = Level.ObjectManager.AddSubObject<Drill>(this);
		_drill.Position = Position + new Vector2i(-224, 52);
		_drill.FlipX = false;
		_drill.LockLifetime = true;
		// Draw order, from back to front: back wheels, egg mobile, car, drill, front wheels.
		_wheels[2].Priority = 252;
		_wheels[3].Priority = 252;
		_eggMobile.Priority = 254;
		Priority = 256;
		_drill.Priority = 258;
		_wheels[0].Priority = 260;
		_wheels[1].Priority = 260;
		Health = InitialHealth;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_status)
		{
			case StatusType.InitialApproch:
				// The egg mobile flies in from the right, and the car drives below it.
				if (_eggMobile.Position.X <= _startPosition.X - ApproachDistance)
				{
					_status = StatusType.FinalApproach;
					break;
				}
				Position += new Vector2i(-ApproachSpeed, 0);
				_drill.Position += new Vector2i(-ApproachSpeed, 0);
				break;
			case StatusType.FinalApproach:
				// The egg mobile moves down until it is in the car.
				if (_eggMobile.Position.Y < _startPosition.Y + CarOffsetY)
					break;
				_eggMobile.Position = new Vector2i(_eggMobile.Position.X, _startPosition.Y + CarOffsetY);
				_eggMobile.BeginLoweringPropeller();
				_status = StatusType.LowerPropeller;
				_stateTimer = LowerPropellerTicks;
				break;
			case StatusType.LowerPropeller:
				if (--_stateTimer > 0)
					break;
				_status = StatusType.Normal;
				_velocityX = -CarSpeed;
				CollisionRectangles = new CollisionRectangle[]
				{
					new CollisionRectangle(this, 0, -128, -128, 256, 256)
				};
				break;
			case StatusType.Normal:
				UpdateNormal();
				break;
			case StatusType.Defeated:
				--_stateTimer;
				if (_stateTimer > 0)
				{
					_eggMobile.DoEggMobileBreak();
					UpdateExplosions(ExplosionAreaSize);
					UpdateCarToGroundLevel();
					break;
				}
				_status = StatusType.PostDefeatedIdleA;
				_stateTimer = PostDefeatIdleTicks;
				_eggMobile.DoRobotnikNormal();
				break;
			case StatusType.PostDefeatedIdleA:
				--_stateTimer;
				if (_stateTimer > 0)
					break;
				_status = StatusType.PostDefeatStartPropeller;
				break;
			case StatusType.PostDefeatStartPropeller:
				Defeated = true;
				_stateTimer = PostDefeatPropellerTicks;
				_status = StatusType.PostDefeatB;
				break;
			case StatusType.PostDefeatB:
				if (--_stateTimer > 0)
					break;
				_status = StatusType.Finished;
				_stateTimer = Level.OriginalPhysics ? OriginalFleeRiseTicks : FleeRiseTicks;
				Fleeing = true;
				break;
			case StatusType.Finished:
				--_stateTimer;
				break;
		}
	}

	/// <summary>
	/// Drives the car from side to side. At 1 health, the car also sparks and emits smoke.
	/// </summary>
	private void UpdateNormal()
	{
		int turnLeftX = Level.OriginalPhysics ? LeftEdge + OriginalTurnLeftOffset : LeftEdge - TurnMargin;
		int turnRightX = Level.OriginalPhysics ? LeftEdge + OriginalTurnRightOffset : RightEdge + TurnMargin;
		if (_eggMobile.Position.X <= turnLeftX || _eggMobile.Position.X >= turnRightX)
		{
			_velocityX *= -1;
			_eggMobile.FlipX = _velocityX > 0;
			if (!_drill.Released)
			{
				// Move the drill to the other side of the car.
				_drill.Position = new Vector2i(Position.X + (Position.X - _drill.Position.X), _drill.Position.Y);
				_drill.FlipX = _velocityX > 0;
			}
			if (Health == 1 && !Level.OriginalPhysics)
				ReleaseDrill();
		}
		// In the original physics, the drill comes off at 1 health as soon as the protagonist is in front of it.
		if (Health == 1 && Level.OriginalPhysics && !_drill.Released && IsProtagonistInFrontOfDrill())
			ReleaseDrill();
		_eggMobile.Position += new Vector2i(_velocityX, 0);
		Position += new Vector2i(_velocityX, 0);
		if (!_drill.Released)
			_drill.Position += new Vector2i(_velocityX, 0);
		if (Health != 1)
			return;
		if (_lastHealthFlash > 0.0)
			_lastHealthFlash = Math.Max(0.0f, _lastHealthFlash - 0.1f);
		if (_sparkAnimation.Cycles > 0 && Level.Random.Next(60) == 0)
		{
			_sparkAnimation.Cycles = 0;
			_sparkAnimation.ResetFrame();
			_lastHealthFlash = 1f;
		}
		if (Level.Random.Next(40) != 0)
			return;
		// The smoke moves away from the direction of travel.
		EmitSmoke((Vector2)new Vector2i(Level.Random.Next(-128, 28), -32), new Vector2(_velocityX >= 0 ? -1.0 : 1.0, -4.0));
	}

	/// <summary>
	/// Returns true when the protagonist is on the side that the drill points to.
	/// </summary>
	private bool IsProtagonistInFrontOfDrill()
	{
		int distance = _drill.Position.X - Level.Player.Protagonist.Position.X;
		return distance >= 0 ? !_drill.FlipX : _drill.FlipX;
	}

	/// <summary>
	/// Makes the broken car fall to the ground. The egg mobile keeps its height relative to the car.
	/// </summary>
	private void UpdateCarToGroundLevel()
	{
		double eggMobileOffsetY = _eggMobile.Position.Y - Position.Y;
		PositionPrecise += new Vector2(0.0, _velocityY);
		_velocityY += Gravity;
		if (_velocityY > 0.0)
		{
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 80))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					_velocityY = 0.0;
				}
			}
		}
		_eggMobile.PositionPrecise = new Vector2(_eggMobile.Position.X, Position.Y + eggMobileOffsetY);
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (Health <= 0)
			return;
		_eggMobile.DoRobotnikFrown();
	}

	protected override void Defeat()
	{
		_status = StatusType.Defeated;
		_stateTimer = DefeatedTicks;
		_eggMobile.DoRobotnikDefeated();
		CollisionRectangles = new CollisionRectangle[0];
		_velocityY = -6.0;
		_animation.Index = AnimationBrokenCar;
		ReleaseFragments();
		ReleaseWheels();
		ReleaseDrill();
		Level.Player.GainScore(DefeatScore);
		Level.CreateScoreObject(DefeatScore, Position + new Vector2i(0, -128));
	}

	private void EmitSmoke(Vector2 offset, Vector2 velocity)
	{
		Matrix4 matrix = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
		if (_eggMobile.FlipX)
			matrix *= Matrix4.CreateScale(-1.0, 1.0);
		Vector2i position = (Vector2i)((matrix * Matrix4.CreateTranslation(offset)).RotateZ(0.0) * new Vector2(0.0, 0.0));
		DustInstance dustInstance = Level.ObjectManager.AddObject(new ObjectPlacement(DrillEggmanType.DefaultBrokenSmokeResourceKey, Level.Map.Layers.IndexOf(Layer), position)) as DustInstance;
		dustInstance.Velocity = velocity;
		dustInstance.SetDustAnimationIndex(AnimationDustSmoke);
		if (Level.Random.NextBoolean())
			dustInstance.Priority = Priority - 8;
		else
			dustInstance.Priority = Priority + 8;
	}

	/// <summary>
	/// Releases the fragments of the broken car. The offsets are for a car that faces left.
	/// </summary>
	private void ReleaseFragments()
	{
		ReleaseFragmentPipe(new Vector2i(106, -15), new Vector2(-4.0, -10.0), 2.2);
		ReleaseFragmentPipe(new Vector2i(118, -15), new Vector2(-2.0, -15.0), 1.4);
		ReleaseFragmentPipe(new Vector2i(130, -15), new Vector2(5.0, -12.0), 2.0);
		for (int index = 0; index < 16; ++index)
			ReleaseFragmentGlass(new Vector2i(-120, 0), new Vector2(-Level.Random.Next(1, 8), -Level.Random.Next(-4, -1)));
		ReleaseFragmentMetal(new Vector2i(-14, -69), new Vector2(-10.0, -8.0));
		ReleaseFragmentMetal(new Vector2i(102, 0), new Vector2(-8.0, -6.0));
		ReleaseFragmentMetal(new Vector2i(132, 0), new Vector2(6.0, -8.0));
		ReleaseFragmentMetal(new Vector2i(102, -30), new Vector2(-4.0, -14.0));
		ReleaseFragmentMetal(new Vector2i(132, -28), new Vector2(8.0, -12.0));
		ReleaseFragmentMetal(new Vector2i(176, 18), new Vector2(12.0, -6.0));
		ReleaseFragmentRed(new Vector2i(-48, 10), new Vector2(-3.0, -8.0));
		ReleaseFragmentRed(new Vector2i(-48, 10), new Vector2(5.0, -8.0));
		ReleaseFragmentRed(new Vector2i(36, 46), new Vector2(-4.0, -4.0));
	}

	/// <summary>
	/// Releases a glass fragment with a random size. Smaller fragments turn faster.
	/// </summary>
	private void ReleaseFragmentGlass(Vector2i offset, Vector2 velocity)
	{
		int sizeIndex = Level.Random.Next(0, 3);
		int direction = _velocityX > 0 ? -1 : 1;
		offset.X *= direction;
		velocity.X *= direction;
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = AnimationGlassFragment;
		fragment.Position += offset;
		fragment.Velocity = velocity;
		fragment.Scale = 1.0 - sizeIndex * 0.25;
		fragment.AngularVelocity = 2.0 * Math.PI / ((sizeIndex + 1) * 0.5 * 60.0);
		fragment.Angle = Level.Random.NextRadians();
		fragment.FlipX = _velocityX > 0;
		fragment.FlipY = Level.Random.NextBoolean();
		fragment.Initialise();
	}

	private void ReleaseFragmentMetal(Vector2i offset, Vector2 velocity)
	{
		ReleaseFragment(offset, velocity, AnimationMetalFragment, Math.PI / 30.0);
	}

	private void ReleaseFragmentRed(Vector2i offset, Vector2 velocity)
	{
		ReleaseFragment(offset, velocity, AnimationRedFragment, Math.PI / 15.0);
	}

	/// <summary>
	/// Releases a pipe fragment that turns one time in the given number of seconds.
	/// </summary>
	private void ReleaseFragmentPipe(Vector2i offset, Vector2 velocity, double angularVelocityTimePeriod)
	{
		ReleaseFragment(offset, velocity, AnimationPipeFragment, 2.0 * Math.PI / (angularVelocityTimePeriod * 60.0));
	}

	/// <summary>
	/// Releases a fragment of the car. The offset and the velocity are for a car that faces left,
	/// and the method mirrors them when the car faces right.
	/// </summary>
	private void ReleaseFragment(Vector2i offset, Vector2 velocity, int animationIndex, double angularVelocity)
	{
		int direction = _velocityX > 0 ? -1 : 1;
		offset.X *= direction;
		velocity.X *= direction;
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = animationIndex;
		fragment.Position += offset;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = angularVelocity;
		fragment.FlipX = _velocityX > 0;
		fragment.Initialise();
	}

	/// <summary>
	/// Releases the wheels. Wheels 1 and 3 jump in the other direction to wheels 0 and 2.
	/// </summary>
	private void ReleaseWheels()
	{
		for (int index = 0; index < 4; ++index)
		{
			Vector2 velocity = new Vector2(-8.0, -12.0);
			if (_velocityX > 0)
				velocity.X *= -1.0;
			if (index == 1 || index == 3)
				velocity.X *= -1.0;
			_wheels[index].Velocity = velocity;
			_wheels[index].Released = true;
			_wheels[index].LockLifetime = false;
		}
	}

	/// <summary>
	/// Releases the drill. The drill then moves forward on its own.
	/// </summary>
	private void ReleaseDrill()
	{
		if (_drill.Released)
			return;
		_drill.Released = true;
		_drill.Velocity = new Vector2i(-12 * (_drill.FlipX ? -1 : 1), 0);
		_drill.LockLifetime = false;
	}

	protected override void OnHurtCharacter(ICharacter character)
	{
		_eggMobile.DoRobotnikSmile();
	}

	protected override void OnAnimate()
	{
		_animation.Animate();
		_carLightAnimation.Animate();
		_sparkAnimation.Animate();
		_brokenVentAnimation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		bool flipX = _velocityX > 0;
		Vector2i carOffset = new Vector2i(-32 * (flipX ? -1 : 1), 32);
		if (Health == 1)
		{
			// At 1 health, the car shakes up and down.
			const int shakePeriod = 4;
			double shakeAngle = 2.0 * Math.PI * (Level.Ticks % shakePeriod / (double)shakePeriod);
			carOffset.Y += (int)(Math.Sin(shakeAngle) * 2.0);
		}
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		float flash = Health == 1 ? _lastHealthFlash : 0.0f;
		if (IsInvincibleFlashing)
			flash = 1f;
		float flashColour = flash * 0.25f;
		objectRenderer.AdditiveColour = flashColour > 0.0 ? new Colour((double)flashColour, (double)flashColour, (double)flashColour) : Colours.Black;
		objectRenderer.Render(_animation, (Vector2)carOffset, flipX);
		if (_status == StatusType.Normal)
		{
			objectRenderer.AdditiveColour = Colours.Black;
			objectRenderer.BlendMode = BlendMode.Additive;
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_carLightAnimation, (Vector2)carOffset + new Vector2(flipX ? 93.0 : -93.0, -24.0), flipX);
			if (Health == 1 && _sparkAnimation.Cycles == 0)
			{
				Animation.Frame currentFrame = _sparkAnimation.CurrentFrame;
				Vector2i offset = currentFrame.Offset;
				if (flipX)
					offset.X *= -1;
				objectRenderer.Texture = _sparkAnimation.CurrentTexture;
				objectRenderer.Render((Rectangle)currentFrame.Source, (Vector2)offset, flipX);
			}
			objectRenderer.EmitsLight = false;
			objectRenderer.BlendMode = BlendMode.Alpha;
		}
		if (_status < StatusType.Defeated)
			return;
		objectRenderer.Render(_brokenVentAnimation, (Vector2)carOffset + new Vector2(flipX ? -112.0 : 112.0, -16.0), flipX);
	}

	public enum StatusType
	{
		InitialApproch,
		FinalApproach,
		LowerPropeller,
		Normal,
		Defeated,
		PostDefeatedIdleA,
		PostDefeatStartPropeller,
		PostDefeatB,
		Finished,
	}

	/// <summary>
	/// The egg mobile with Eggman and the propeller. It flies in at the start and flies away after the defeat.
	/// </summary>
	private class EggMobile : ActiveObject
	{
		private const int FleeSpeed = 24;
		private const int FleeLifetimeTicks = 1200;

		private AnimationInstance _animationEggMobileChasis;
		private AnimationInstance _animationEggMobileScreen;
		private AnimationInstance _animationPropeller;
		private AnimationInstance _animationRobotnik;
		private LevelSound _helicopterSound;
		private int _propellerOffset;
		private int _fleeTicks;

		public bool FlipX { get; set; }

		private bool Turning { get; set; }

		public DrillEggmanInstance DrillEggman => (DrillEggmanInstance)ParentObject;

		protected override void OnStart()
		{
			ResourceTree resourceTree = Level.GameContext.ResourceTree;
			AnimationGroup animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, DrillEggmanType.AnimationGroupResourceKey);
			AnimationGroup robotnikAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, DrillEggmanType.RobotnikAnimationGroupResourceKey);
			_animationEggMobileChasis = new AnimationInstance(animationGroup, AnimationEggMobileChasisFlame);
			_animationEggMobileScreen = new AnimationInstance(animationGroup, AnimationEggMobileScreen);
			_animationPropeller = new AnimationInstance(animationGroup, AnimationPropellerSpinning);
			_animationRobotnik = new AnimationInstance(robotnikAnimationGroup);
			_helicopterSound = new LevelSound(Level, resourceTree.GetLoadedResource<SampleInfo>(Type, DrillEggmanType.BossHelicopterSoundResourceKey), autoFinish: false);
			_helicopterSound.DistanceAudible = 3840;
			Level.SoundManager.AddLevelSound(_helicopterSound);
			_propellerOffset = -136;
			LockLifetime = true;
			Turning = false;
		}

		protected override void OnUpdate()
		{
			switch (DrillEggman._status)
			{
				case StatusType.InitialApproch:
					Position += new Vector2i(-ApproachSpeed, ApproachSpeed);
					_helicopterSound.Play();
					break;
				case StatusType.FinalApproach:
					Position += new Vector2i(0, ApproachSpeed);
					break;
				case StatusType.LowerPropeller:
					if (DrillEggman._stateTimer <= 41)
						++_propellerOffset;
					break;
				case StatusType.Normal:
					_animationEggMobileChasis.Index = AnimationEggMobileChasis;
					break;
				case StatusType.PostDefeatB:
					// Raise the propeller, and then open it.
					_animationEggMobileChasis.Index = AnimationEggMobileChasisFlame;
					if (DrillEggman._stateTimer > 8)
						--_propellerOffset;
					else if (DrillEggman._stateTimer == 8)
						_animationPropeller.Index = AnimationPropellerOut;
					break;
				case StatusType.Finished:
					// Rise, turn to face right, and fly away to the right.
					_helicopterSound.Play();
					if (DrillEggman._stateTimer > 0)
						Position += new Vector2i(0, -ApproachSpeed);
					else if (!FlipX)
					{
						if (_animationEggMobileChasis.Index != AnimationBrokenEggMobileChasisTurn1)
						{
							_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasisTurn1;
							_animationEggMobileChasis.Cycles = 0;
							_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreenTurn1;
							Turning = false;
						}
						else if (_animationEggMobileChasis.Cycles >= 1)
						{
							_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasisTurn2;
							_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreenTurn2;
							FlipX = true;
						}
					}
					else if (_animationEggMobileChasis.Cycles >= 2 && Turning)
					{
						Turning = false;
						_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreen;
					}
					else if (FlipX)
						Position += new Vector2i(FleeSpeed, 0);
					++_fleeTicks;
					if (_fleeTicks > FleeLifetimeTicks)
						LockLifetime = false;
					break;
			}
			_helicopterSound.Position = Position;
		}

		public void BeginLoweringPropeller()
		{
			_animationPropeller.Index = AnimationPropellerIn;
			_helicopterSound.Stop();
		}

		public void DoEggMobileBreak()
		{
			_animationEggMobileChasis.Index = AnimationBrokenEggMobileChasis;
			_animationEggMobileScreen.Index = AnimationBrokenEggMobileScreen;
		}

		public void DoRobotnikSmile()
		{
			if (_animationRobotnik.Index != AnimationRobotnikNormal)
				return;
			_animationRobotnik.Index = AnimationRobotnikSmiling;
		}

		public void DoRobotnikFrown() => _animationRobotnik.Index = AnimationRobotnikFrown;

		public void DoRobotnikDefeated() => _animationRobotnik.Index = AnimationRobotnikDefeated;

		public void DoRobotnikNormal() => _animationRobotnik.Index = AnimationRobotnikNormal;

		protected override void OnStop() => _helicopterSound.Dispose();

		protected override void OnAnimate()
		{
			_animationEggMobileChasis.Animate();
			_animationEggMobileScreen.Animate();
			_animationPropeller.Animate();
			_animationRobotnik.Animate();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				if (FlipX)
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				Vector2i propellerDestination = new Vector2i(36, _propellerOffset);
				Vector2i chasisDestination = new Vector2i(-8, -26);
				Vector2i robotnikDestination = chasisDestination + new Vector2i(0, -76);
				Vector2i screenDestination = chasisDestination;
				objectRenderer.FilterAmount *= 0.5;
				if (DrillEggman.IsInvincibleFlashing)
					objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
				objectRenderer.Render(_animationPropeller, (Vector2)propellerDestination);
				objectRenderer.Render(_animationEggMobileChasis, (Vector2)chasisDestination);
				objectRenderer.AdditiveColour = Colours.Black;
				objectRenderer.Render(_animationRobotnik, (Vector2)robotnikDestination);
				if (DrillEggman.IsInvincibleFlashing)
					objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
				objectRenderer.Render(_animationEggMobileScreen, (Vector2)screenDestination);
			}
		}
	}

	/// <summary>
	/// The drill at the front of the car. It hurts characters. When it is released, it moves forward on its own.
	/// </summary>
	private class Drill : Enemy
	{
		private AnimationInstance _animation;

		public Vector2i Velocity { get; set; }

		public bool FlipX { get; set; }

		public bool Released { get; set; }

		public DrillEggmanInstance DrillEggman => (DrillEggmanInstance)ParentObject;

		protected override void OnStart()
		{
			base.OnStart();
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -96, -96, 192, 192)
			};
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DrillEggmanType.AnimationGroupResourceKey), AnimationDrill);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Position += Velocity;
		}

		protected override void OnCollision(CollisionEvent e)
		{
			// The drill cannot hurt characters before the fight starts.
			if (DrillEggman._status < StatusType.Normal)
				return;
			base.OnCollision(e);
		}

		protected override void OnHurtCharacter(ICharacter character)
		{
			(ParentObject as DrillEggmanInstance)._eggMobile.DoRobotnikSmile();
		}

		protected override void OnAnimate()
		{
			base.OnAnimate();
			if (DrillEggman._status < StatusType.LowerPropeller)
				return;
			// The drill turns faster while the propeller goes in.
			_animation.OverrideDelay = DrillEggman._status != StatusType.LowerPropeller ? null : 3;
			_animation.Animate();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(_animation, FlipX);
		}
	}

	/// <summary>
	/// A wheel of the car. Wheels 0 and 1 are at the front and wheels 2 and 3 are at the back.
	/// When the wheel is released, it bounces away.
	/// </summary>
	private class Wheel : ActiveObject
	{
		private const double Gravity = 0.875;
		private const double BounceVelocity = -8.0;

		/// <summary>
		/// The wheel positions relative to the car when the car faces left.
		/// </summary>
		private static readonly Vector2i[] LeftPositions = new Vector2i[]
		{
			new Vector2i(-48, 80),
			new Vector2i(144, 80),
			new Vector2i(-192, 80),
			new Vector2i(0, 80)
		};

		/// <summary>
		/// The wheel positions relative to the car when the car faces right.
		/// </summary>
		private static readonly Vector2i[] RightPositions = new Vector2i[]
		{
			new Vector2i(48, 80),
			new Vector2i(-144, 80),
			new Vector2i(192, 80),
			new Vector2i(0, 80)
		};

		private AnimationInstance _animation;

		public int Index { get; set; }

		public Vector2 Velocity { get; set; }

		public bool FlipX { get; set; }

		public bool Released { get; set; }

		public DrillEggmanInstance DrillEggman => (DrillEggmanInstance)ParentObject;

		protected override void OnStart()
		{
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DrillEggmanType.AnimationGroupResourceKey));
		}

		protected override void OnUpdate()
		{
			if (Released)
			{
				PositionPrecise += Velocity;
				Velocity += new Vector2(0.0, Gravity);
				if (Velocity.Y <= 0.0 || !MoveToGroundLevel())
					return;
				Velocity += new Vector2(0.0, BounceVelocity);
			}
			else
			{
				AutoPositionToCar();
				Position += new Vector2i(DrillEggman._velocityX, 0);
				MoveToGroundLevel();
			}
		}

		public void AutoPositionToCar()
		{
			FlipX = DrillEggman._velocityX > 0;
			Position = DrillEggman.Position + (DrillEggman._velocityX <= 0 ? LeftPositions[Index] : RightPositions[Index]);
		}

		/// <summary>
		/// Moves the wheel up onto the ground. Returns true when the wheel touches the ground.
		/// </summary>
		private bool MoveToGroundLevel()
		{
			bool onGround = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 64))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					onGround = true;
				}
			}
			return onGround;
		}

		protected override void OnAnimate()
		{
			bool moving = PositionPrecise - LastPositionPrecise != new Vector2();
			if (Index == 0 || Index == 1)
				_animation.Index = moving ? AnimationWheelFrontMoving : AnimationWheelFrontStill;
			else
				_animation.Index = moving ? AnimationWheelBackMoving : AnimationWheelBackStill;
			_animation.Animate();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			if (DrillEggman.IsInvincibleFlashing)
				objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
			objectRenderer.Render(_animation, FlipX);
		}
	}
}
