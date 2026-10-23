// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.DRILLEGGMAN.DrillEggmanInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace SONICORCA.OBJECTS.DRILLEGGMAN;

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
	private const int AnimationPipeFragment = 16 /*0x10*/;
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
	private const int AnimationBrokenEggMobileChasisTurn2 = 31 /*0x1F*/;
	private const int AnimationBrokenEggMobileScreenTurn1 = 32 /*0x20*/;
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
	private AnimationGroup _animationGroup;
	private AnimationInstance _animation;
	private AnimationInstance _carLightAnimation;
	private AnimationInstance _sparkAnimation;
	private AnimationInstance _brokenVentAnimation;
	private DrillEggmanInstance.StatusType _status;
	private Vector2i _startPosition;
	private int _stateTimer;
	private int _velocityX;
	private double _velocityY;
	private DrillEggmanInstance.EggMobile _eggMobile;
	private DrillEggmanInstance.Drill _drill;
	private DrillEggmanInstance.Wheel[] _wheels = new DrillEggmanInstance.Wheel[4];
	private float _lastHealthFlash;

	[HideInEditor]
	public DrillEggmanInstance.StatusType Status => _status;

	[StateVariable]
	public int LeftEdge { get; set; }

	[StateVariable]
	public int RightEdge { get; set; }

	protected override void OnStart()
	{
		ResourceTree resourceTree = Level.GameContext.ResourceTree;
		AnimationGroup animationGroup = _animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		resourceTree.GetLoadedResource<AnimationGroup>(Type, "SONICORCA/OBJECTS/ROBOTNIK/ANIGROUP");
		_animation = new AnimationInstance(animationGroup);
		_carLightAnimation = new AnimationInstance(animationGroup, 10);
		_sparkAnimation = new AnimationInstance(animationGroup, 17);
		_brokenVentAnimation = new AnimationInstance(animationGroup, 12);
		ExplosionResourceKey = Type.GetAbsolutePath("SONICORCA/OBJECTS/EXPLOSION/BOSS");
		HitSoundResourceKey = Type.GetAbsolutePath("SONICORCA/SOUND/BOSSHIT");
		LockLifetime = true;
		_startPosition = Position;
		Position = Position + new Vector2i(0, 1176);
		_eggMobile = Level.ObjectManager.AddSubObject<DrillEggmanInstance.EggMobile>(this);
		_eggMobile.Position = _startPosition;
		for (int index = 0; index < 4; ++index)
		{
			_wheels[index] = Level.ObjectManager.AddSubObject<DrillEggmanInstance.Wheel>(this);
			_wheels[index].Index = index;
			_wheels[index].LockLifetime = true;
			_wheels[index].AutoPositionToCar();
		}
		_drill = Level.ObjectManager.AddSubObject<DrillEggmanInstance.Drill>(this);
		_drill.Position = Position + new Vector2i(-224, 52);
		_drill.FlipX = false;
		_drill.LockLifetime = true;
		_wheels[2].Priority = 252;
		_wheels[3].Priority = 252;
		_eggMobile.Priority = 254;
		Priority = 256 /*0x0100*/;
		_drill.Priority = 258;
		_wheels[0].Priority = 260;
		_wheels[1].Priority = 260;
		Health = 8;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_status)
		{
			case DrillEggmanInstance.StatusType.InitialApproch:
				if (_eggMobile.Position.X <= _startPosition.X - 1152)
				{
					_status = DrillEggmanInstance.StatusType.FinalApproach;
					break;
				}
				Position = Position + new Vector2i(-4, 0);
				DrillEggmanInstance.Drill drill1 = _drill;
				drill1.Position = drill1.Position + new Vector2i(-4, 0);
				break;
			case DrillEggmanInstance.StatusType.FinalApproach:
				Vector2i position1 = _eggMobile.Position;
				if (position1.Y < _startPosition.Y + 1176)
					break;
				DrillEggmanInstance.EggMobile eggMobile1 = _eggMobile;
				position1 = _eggMobile.Position;
				Vector2i vector2i1 = new Vector2i(position1.X, _startPosition.Y + 1176);
				eggMobile1.Position = vector2i1;
				_eggMobile.BeginLoweringPropeller();
				_status = DrillEggmanInstance.StatusType.LowerPropeller;
				_stateTimer = 60;
				break;
			case DrillEggmanInstance.StatusType.LowerPropeller:
				if (--_stateTimer > 0)
					break;
				_status = DrillEggmanInstance.StatusType.Normal;
				_velocityX = -8;
				CollisionRectangles = new CollisionRectangle[1]
				{
		  new CollisionRectangle( this, 0,  sbyte.MinValue,  sbyte.MinValue, 256 /*0x0100*/, 256 /*0x0100*/)
				};
				break;
			case DrillEggmanInstance.StatusType.Normal:
				Vector2i position2 = _eggMobile.Position;
				if (position2.X > LeftEdge - 512 /*0x0200*/)
				{
					position2 = _eggMobile.Position;
					if (position2.X < RightEdge + 512 /*0x0200*/)
						goto label_15;
				}
				_velocityX *= -1;
				_eggMobile.FlipX = _velocityX > 0;
				if (!_drill.Released)
				{
					DrillEggmanInstance.Drill drill2 = _drill;
					position2 = Position;
					int x1 = position2.X;
					position2 = Position;
					int x2 = position2.X;
					position2 = _drill.Position;
					int x3 = position2.X;
					int num = x2 - x3;
					int x4 = x1 + num;
					position2 = _drill.Position;
					int y = position2.Y;
					Vector2i vector2i2 = new Vector2i(x4, y);
					drill2.Position = vector2i2;
					_drill.FlipX = _velocityX > 0;
				}
				if (Health == 1)
					ReleaseDrill();
			label_15:
				DrillEggmanInstance.EggMobile eggMobile2 = _eggMobile;
				eggMobile2.Position = eggMobile2.Position + new Vector2i(_velocityX, 0);
				Position = Position + new Vector2i(_velocityX, 0);
				if (!_drill.Released)
				{
					DrillEggmanInstance.Drill drill3 = _drill;
					drill3.Position = drill3.Position + new Vector2i(_velocityX, 0);
				}
				if (Health != 1)
					break;
				if (_lastHealthFlash > 0.0)
					_lastHealthFlash = Math.Max(0.0f, _lastHealthFlash - 0.1f);
				if (_sparkAnimation.Cycles > 0 && Level.Random.Next(60) == 0)
				{
					_sparkAnimation.Cycles = 0;
					_sparkAnimation.ResetFrame();
					_lastHealthFlash = 1f;
				}
				if (Level.Random.Next(40) != 0)
					break;
				if (_velocityX >= 0)
				{
					EmitSmoke((Vector2)new Vector2i(Level.Random.Next(sbyte.MinValue, 28), -32), new Vector2(-1.0, -4.0));
					break;
				}
				EmitSmoke((Vector2)new Vector2i(Level.Random.Next(sbyte.MinValue, 28), -32), new Vector2(1.0, -4.0));
				break;
			case DrillEggmanInstance.StatusType.Defeated:
				--_stateTimer;
				if (_stateTimer > 0)
				{
					_eggMobile.DoEggMobileBreak();
					UpdateExplosions(128 /*0x80*/);
					UpdateCarToGroundLevel();
					break;
				}
				_status = DrillEggmanInstance.StatusType.PostDefeatedIdleA;
				_stateTimer = 12;
				_eggMobile.DoRobotnikNormal();
				break;
			case DrillEggmanInstance.StatusType.PostDefeatedIdleA:
				--_stateTimer;
				if (_stateTimer > 0)
					break;
				_status = DrillEggmanInstance.StatusType.PostDefeatStartPropeller;
				break;
			case DrillEggmanInstance.StatusType.PostDefeatStartPropeller:
				Defeated = true;
				_stateTimer = 50;
				_status = DrillEggmanInstance.StatusType.PostDefeatB;
				break;
			case DrillEggmanInstance.StatusType.PostDefeatB:
				if (--_stateTimer > 0)
					break;
				_status = DrillEggmanInstance.StatusType.Finished;
				_stateTimer = 146;
				Fleeing = true;
				break;
			case DrillEggmanInstance.StatusType.Finished:
				--_stateTimer;
				break;
		}
	}

	private void UpdateCarToGroundLevel()
	{
		Vector2i position = _eggMobile.Position;
		int y1 = position.Y;
		position = Position;
		int y2 = position.Y;
		double num = y1 - y2;
		PositionPrecise = PositionPrecise + new Vector2(0.0, _velocityY);
		_velocityY += 0.875;
		if (_velocityY > 0.0)
		{
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 80 /*0x50*/))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					Vector2 positionPrecise = PositionPrecise;
					double x = positionPrecise.X;
					positionPrecise = PositionPrecise;
					double y3 = positionPrecise.Y + collision.Shift;
					PositionPrecise = new Vector2(x, y3);
					_velocityY = 0.0;
				}
			}
		}
		_eggMobile.PositionPrecise = new Vector2(_eggMobile.Position.X, Position.Y + num);
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
		_status = DrillEggmanInstance.StatusType.Defeated;
		_stateTimer = 179;
		_eggMobile.DoRobotnikDefeated();
		CollisionRectangles = new CollisionRectangle[0];
		_velocityY = -6.0;
		_animation.Index = 11;
		ReleaseFragments();
		ReleaseWheels();
		ReleaseDrill();
		Level.Player.GainScore(1000);
		Level.CreateScoreObject(1000, Position + new Vector2i(0, sbyte.MinValue));
	}

	private void EmitSmoke(Vector2 offset, Vector2 velocity)
	{
		Matrix4 matrix4 = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
		if (_eggMobile.FlipX)
			matrix4 *= Matrix4.CreateScale(-1.0, 1.0);
		Vector2i position = (Vector2i)((matrix4 * Matrix4.CreateTranslation(offset)).RotateZ(0.0) * new Vector2(0.0, 0.0));
		DustInstance dustInstance = Level.ObjectManager.AddObject(new ObjectPlacement("SONICORCA/OBJECTS/DUST", Level.Map.Layers.IndexOf(Layer), position)) as DustInstance;
		dustInstance.Velocity = velocity;
		dustInstance.SetDustAnimationIndex(1);
		if (Level.Random.NextBoolean())
			dustInstance.Priority = Priority - 8;
		else
			dustInstance.Priority = Priority + 8;
	}

	private void ReleaseFragments()
	{
		ReleaseFragmentPipe(new Vector2i(106, -15), new Vector2(-4.0, -10.0), 2.2);
		ReleaseFragmentPipe(new Vector2i(118, -15), new Vector2(-2.0, -15.0), 1.4);
		ReleaseFragmentPipe(new Vector2i(130, -15), new Vector2(5.0, -12.0), 2.0);
		for (int index = 0; index < 16 /*0x10*/; ++index)
			ReleaseFragmentGlass(new Vector2i(-120, 0), new Vector2(-Level.Random.Next(1, 8), -Level.Random.Next(-4, -1)));
		ReleaseFragmentMetal(new Vector2i(-14, -69), new Vector2(-10.0, -8.0));
		ReleaseFragmentMetal(new Vector2i(102, 0), new Vector2(-8.0, -6.0));
		ReleaseFragmentMetal(new Vector2i(132, 0), new Vector2(6.0, -8.0));
		ReleaseFragmentMetal(new Vector2i(102, -30), new Vector2(-4.0, -14.0));
		ReleaseFragmentMetal(new Vector2i(132, -28), new Vector2(8.0, -12.0));
		ReleaseFragmentMetal(new Vector2i(176 /*0xB0*/, 18), new Vector2(12.0, -6.0));
		ReleaseFragmentRed(new Vector2i(-48, 10), new Vector2(-3.0, -8.0));
		ReleaseFragmentRed(new Vector2i(-48, 10), new Vector2(5.0, -8.0));
		ReleaseFragmentRed(new Vector2i(36, 46), new Vector2(-4.0, -4.0));
	}

	private void ReleaseFragmentGlass(Vector2i offset, Vector2 velocity)
	{
		int num1 = Level.Random.Next(0, 3);
		int num2 = _velocityX > 0 ? -1 : 1;
		offset.X *= num2;
		velocity.X *= num2;
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = 13;
		fragment.Position = fragment.Position + offset;
		fragment.Velocity = velocity;
		fragment.Scale = 1.0 - num1 * 0.25;
		fragment.AngularVelocity = 2.0 * Math.PI / ((num1 + 1) * 0.5 * 60.0);
		fragment.Angle = Level.Random.NextRadians();
		fragment.FlipX = _velocityX > 0;
		fragment.FlipY = Level.Random.NextBoolean();
		fragment.Initialise();
	}

	private void ReleaseFragmentMetal(Vector2i offset, Vector2 velocity)
	{
		int num = _velocityX > 0 ? -1 : 1;
		offset.X *= num;
		velocity.X *= num;
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = 14;
		fragment.Position = fragment.Position + offset;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = Math.PI / 30.0;
		fragment.FlipX = _velocityX > 0;
		fragment.Initialise();
	}

	private void ReleaseFragmentRed(Vector2i offset, Vector2 velocity)
	{
		int num = _velocityX > 0 ? -1 : 1;
		offset.X *= num;
		velocity.X *= num;
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = 15;
		fragment.Position = fragment.Position + offset;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = Math.PI / 15.0;
		fragment.FlipX = _velocityX > 0;
		fragment.Initialise();
	}

	private void ReleaseFragmentPipe(
	  Vector2i offset,
	  Vector2 velocity,
	  double angularVelocityTimePeriod)
	{
		int num = _velocityX > 0 ? -1 : 1;
		offset.X *= num;
		velocity.X *= num;
		Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
		fragment.AnimationGroup = _animationGroup;
		fragment.AnimationIndex = 16 /*0x10*/;
		fragment.Position = fragment.Position + offset;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = 2.0 * Math.PI / (angularVelocityTimePeriod * 60.0);
		fragment.FlipX = _velocityX > 0;
		fragment.Initialise();
	}

	private void ReleaseWheels()
	{
		for (int index = 0; index < 4; ++index)
		{
			Vector2 vector2 = new Vector2(-8.0, -12.0);
			if (_velocityX > 0)
				vector2.X *= -1.0;
			if (index == 1 || index == 3)
				vector2.X *= -1.0;
			_wheels[index].Velocity = vector2;
			_wheels[index].Released = true;
			_wheels[index].LockLifetime = false;
		}
	}

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
		Vector2i destination = new Vector2i(-32 * (flipX ? -1 : 1), 32 /*0x20*/);
		if (Health == 1)
		{
			int num = 4;
			double a = 2.0 * Math.PI * (Level.Ticks % num / (double)num);
			destination.Y += (int)(Math.Sin(a) * 2.0);
		}
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		float num1 = Health == 1 ? _lastHealthFlash : 0.0f;
		if (IsInvincibleFlashing)
			num1 = 1f;
		float num2 = num1 * 0.25f;
		objectRenderer.AdditiveColour = (double)num2 > 0.0 ? new Colour((double)num2, (double)num2, (double)num2) : Colours.Black;
		objectRenderer.Render(_animation, (Vector2)destination, flipX);
		if (_status == DrillEggmanInstance.StatusType.Normal)
		{
			objectRenderer.AdditiveColour = Colours.Black;
			objectRenderer.BlendMode = BlendMode.Additive;
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_carLightAnimation, (Vector2)destination + new Vector2(flipX ? 93.0 : -93.0, -24.0), flipX);
			if (Health == 1 && _sparkAnimation.Cycles == 0)
			{
				Animation.Frame currentFrame = _sparkAnimation.CurrentFrame;
				Vector2i offset = currentFrame.Offset;
				if (flipX)
					offset.X *= -1;
				objectRenderer.Texture = _sparkAnimation.CurrentTexture;
				objectRenderer.Render((Rectangle)currentFrame.Source, (Vector2)currentFrame.Offset, flipX);
			}
			objectRenderer.EmitsLight = false;
			objectRenderer.BlendMode = BlendMode.Alpha;
		}
		if (_status < DrillEggmanInstance.StatusType.Defeated)
			return;
		objectRenderer.Render(_brokenVentAnimation, (Vector2)destination + new Vector2(flipX ? -112.0 : 112.0, -16.0), flipX);
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

	private class EggMobile : ActiveObject
	{
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
			AnimationGroup loadedResource1 = resourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
			AnimationGroup loadedResource2 = resourceTree.GetLoadedResource<AnimationGroup>(Type, "SONICORCA/OBJECTS/ROBOTNIK/ANIGROUP");
			_animationEggMobileChasis = new AnimationInstance(loadedResource1, 19);
			_animationEggMobileScreen = new AnimationInstance(loadedResource1, 20);
			_animationPropeller = new AnimationInstance(loadedResource1, 8);
			_animationRobotnik = new AnimationInstance(loadedResource2);
			_helicopterSound = new LevelSound(Level, resourceTree.GetLoadedResource<SampleInfo>(Type, "SONICORCA/SOUND/BOSSHELICOPTER"), autoFinish: false);
			_helicopterSound.DistanceAudible = 3840 /*0x0F00*/;
			Level.SoundManager.AddLevelSound(_helicopterSound);
			_propellerOffset = -136;
			LockLifetime = true;
			Turning = false;
		}

		protected override void OnUpdate()
		{
			switch (DrillEggman._status)
			{
				case DrillEggmanInstance.StatusType.InitialApproch:
					Position = Position + new Vector2i(-4, 4);
					_helicopterSound.Play();
					break;
				case DrillEggmanInstance.StatusType.FinalApproach:
					Position = Position + new Vector2i(0, 4);
					break;
				case DrillEggmanInstance.StatusType.LowerPropeller:
					if (DrillEggman._stateTimer <= 41)
					{
						++_propellerOffset;
						break;
					}
					break;
				case DrillEggmanInstance.StatusType.Normal:
					_animationEggMobileChasis.Index = 18;
					break;
				case DrillEggmanInstance.StatusType.PostDefeatB:
					_animationEggMobileChasis.Index = 19;
					if (DrillEggman._stateTimer > 8)
					{
						--_propellerOffset;
						break;
					}
					if (DrillEggman._stateTimer == 8)
					{
						_animationPropeller.Index = 7;
						break;
					}
					break;
				case DrillEggmanInstance.StatusType.Finished:
					_helicopterSound.Play();
					if (DrillEggman._stateTimer > 0)
						Position = Position + new Vector2i(0, -4);
					else if (!FlipX)
					{
						if (_animationEggMobileChasis.Index != 30)
						{
							_animationEggMobileChasis.Index = 30;
							_animationEggMobileChasis.Cycles = 0;
							_animationEggMobileScreen.Index = 32 /*0x20*/;
							Turning = false;
						}
						else if (_animationEggMobileChasis.Cycles >= 1)
						{
							_animationEggMobileChasis.Index = 31 /*0x1F*/;
							_animationEggMobileScreen.Index = 33;
							FlipX = true;
						}
					}
					else if (_animationEggMobileChasis.Cycles >= 2 && Turning)
					{
						Turning = false;
						_animationEggMobileScreen.Index = 28;
					}
					else if (FlipX)
						Position = Position + new Vector2i(24, 0);
					++_fleeTicks;
					if (_fleeTicks > 1200)
					{
						LockLifetime = false;
						break;
					}
					break;
			}
			_helicopterSound.Position = Position;
		}

		public void BeginLoweringPropeller()
		{
			_animationPropeller.Index = 9;
			_helicopterSound.Stop();
		}

		public void DoEggMobileBreak()
		{
			_animationEggMobileChasis.Index = 26;
			_animationEggMobileScreen.Index = 28;
		}

		public void DoRobotnikSmile()
		{
			if (_animationRobotnik.Index != 0)
				return;
			_animationRobotnik.Index = 1;
		}

		public void DoRobotnikFrown() => _animationRobotnik.Index = 2;

		public void DoRobotnikDefeated() => _animationRobotnik.Index = 3;

		public void DoRobotnikNormal() => _animationRobotnik.Index = 0;

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
				Vector2i destination1 = new Vector2i(36, _propellerOffset);
				Vector2i destination2 = new Vector2i(-8, -26);
				Vector2i vector2i = destination2 + new Vector2i(0, 0);
				Vector2i destination3 = destination2 + new Vector2i(0, -76);
				Vector2i destination4 = destination2 + new Vector2i(0, 0);
				objectRenderer.FilterAmount *= 0.5;
				if (DrillEggman.IsInvincibleFlashing)
					objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
				objectRenderer.Render(_animationPropeller, (Vector2)destination1);
				objectRenderer.Render(_animationEggMobileChasis, (Vector2)destination2);
				objectRenderer.AdditiveColour = Colours.Black;
				objectRenderer.Render(_animationRobotnik, (Vector2)destination3);
				if (DrillEggman.IsInvincibleFlashing)
					objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
				objectRenderer.Render(_animationEggMobileScreen, (Vector2)destination4);
			}
		}
	}

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
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -96, -96, 192 /*0xC0*/, 192 /*0xC0*/)
			};
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 5);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Position = Position + Velocity;
		}

		protected override void OnCollision(CollisionEvent e)
		{
			if (DrillEggman._status < DrillEggmanInstance.StatusType.Normal)
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
			if (DrillEggman._status < DrillEggmanInstance.StatusType.LowerPropeller)
				return;
			_animation.OverrideDelay = DrillEggman._status != DrillEggmanInstance.StatusType.LowerPropeller ? new int?() : new int?(3);
			_animation.Animate();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(_animation, FlipX);
		}
	}

	private class Wheel : ActiveObject
	{
		private const double Gravity = 0.875;
		private static readonly Vector2i[] LeftPositions = new Vector2i[4]
		{
	  new Vector2i(-48, 80 /*0x50*/),
	  new Vector2i(144 /*0x90*/, 80 /*0x50*/),
	  new Vector2i(-192, 80 /*0x50*/),
	  new Vector2i(0, 80 /*0x50*/)
		};
		private static readonly Vector2i[] RightPositions = new Vector2i[4]
		{
	  new Vector2i(48 /*0x30*/, 80 /*0x50*/),
	  new Vector2i(-144, 80 /*0x50*/),
	  new Vector2i(192 /*0xC0*/, 80 /*0x50*/),
	  new Vector2i(0, 80 /*0x50*/)
		};
		private AnimationInstance _animation;

		public int Index { get; set; }

		public Vector2 Velocity { get; set; }

		public bool FlipX { get; set; }

		public bool Released { get; set; }

		public DrillEggmanInstance DrillEggman => (DrillEggmanInstance)ParentObject;

		protected override void OnStart()
		{
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		}

		protected override void OnUpdate()
		{
			if (Released)
			{
				PositionPrecise = PositionPrecise + Velocity;
				Velocity += new Vector2(0.0, 0.875);
				if (Velocity.Y <= 0.0 || !MoveToGroundLevel())
					return;
				Velocity += new Vector2(0.0, -8.0);
			}
			else
			{
				AutoPositionToCar();
				Position = Position + new Vector2i(DrillEggman._velocityX, 0);
				MoveToGroundLevel();
			}
		}

		public void AutoPositionToCar()
		{
			FlipX = DrillEggman._velocityX > 0;
			Position = DrillEggman.Position + (DrillEggman._velocityX <= 0 ? DrillEggmanInstance.Wheel.LeftPositions[Index] : DrillEggmanInstance.Wheel.RightPositions[Index]);
		}

		private bool MoveToGroundLevel()
		{
			bool groundLevel = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 64 /*0x40*/))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					groundLevel = true;
				}
			}
			return groundLevel;
		}

		protected override void OnAnimate()
		{
			bool flag = false;
			if (PositionPrecise - LastPositionPrecise != new Vector2())
				flag = true;
			_animation.Index = Index == 0 || Index == 1 ? (flag ? 1 : 2) : (flag ? 3 : 4);
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
