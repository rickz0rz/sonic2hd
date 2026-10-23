// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.WATEREGGMAN.WaterEggmanInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace SONICORCA.OBJECTS.WATEREGGMAN;

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
	public const int AnimationJarFillC = 16 /*0x10*/;
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
	private const int AnimationArmTurn2 = 31 /*0x1F*/;
	private const int AnimationSplash = 32 /*0x20*/;
	private const int AnimationSplashDrop = 33;
	private const int AnimationSplashSmallDrop = 34;
	private const int AnimationSmoke = 35;
	private const int AnimationBrokenJar = 36;
	private const int AnimationBrokenBoiler = 37;
	private const int AnimationBrokenTube = 38;
	private const int AnimationBrokenBar = 39;
	private const int AnimationBrokenPieces = 40;
	private const int AnimationBrokenEggMobileChasis = 47;
	private const int AnimationBrokenEggMobileChasisFlame = 48 /*0x30*/;
	private const int AnimationBrokenEggMobileScreen = 49;
	private const int AnimationBrokenEggMobileCap = 50;
	private const int AnimationBrokenEggMobileChasisTurn1 = 51;
	private const int AnimationBrokenEggMobileChasisTurn2 = 52;
	private const int AnimationBrokenEggMobileScreenTurn1 = 53;
	private const int AnimationBrokenEggMobileScreenTurn2 = 54;
	private const int AnimationRobotnikFaceSplash = 55;
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
	private WaterEggmanInstance.State _state;
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
	private int _fillSide;
	private bool _dripVisible;
	private Vector2i _dripPosition;
	private bool _jarEmpty;
	private bool _jarFull;
	private bool _willJarSplashRobotnik;
	private int _jarFillY;
	private bool _jarLidOpen;
	private double _velocityY;
	private WaterEggmanInstance.Boiler _boiler;
	private WaterEggmanInstance.JarBack _jarBack;
	private WaterEggmanInstance.ArmFront _armFront;
	private WaterEggmanInstance.SuctionTube _suctionTube;
	private bool _mobileExploding;
	private bool _isFaceSplash;

	public WaterEggmanInstance.State ActionState
	{
		get => _state;
		set => _state = value;
	}

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
		AnimationGroup animationGroup = _animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		AnimationGroup loadedResource = resourceTree.GetLoadedResource<AnimationGroup>(Type, "SONICORCA/OBJECTS/ROBOTNIK/ANIGROUP");
		_animationEggMobileChasis = new AnimationInstance(animationGroup, 23);
		_animationEggMobileScreen = new AnimationInstance(animationGroup, 24);
		_animationEggMobileCap = new AnimationInstance(animationGroup, 25);
		_animationRobotnik = new AnimationInstance(loadedResource);
		_animationArmTurn = new AnimationInstance(animationGroup, 30);
		_animationRobotnikFaceSplash = new AnimationInstance(animationGroup, 55);
		_animationBase = new AnimationInstance(animationGroup);
		_animationBaseShadow = new AnimationInstance(animationGroup, 8);
		_animationBaseLightOn = new AnimationInstance(animationGroup, 1);
		_animationBaseLightOff = new AnimationInstance(animationGroup, 2);
		_animationArm = new AnimationInstance(animationGroup, 5);
		_animationArmTap = new AnimationInstance(animationGroup, 9);
		_animationArmTapCog = new AnimationInstance(animationGroup, 3);
		_animationArmJar = new AnimationInstance(animationGroup, 12);
		_animationArmJarReflection = new AnimationInstance(animationGroup, 13);
		_animationArmJarLidClosed = new AnimationInstance(animationGroup, 19);
		_animationArmJarLidBack = new AnimationInstance(animationGroup, 20);
		_animationArmJarLidFront = new AnimationInstance(animationGroup, 21);
		_animationDrip = new AnimationInstance(animationGroup, 11);
		_animationArmJarFill = new AnimationInstance(animationGroup, 14);
		_animationArmJarFillPlaceholderPrimary = new AnimationInstance(animationGroup, 58);
		_animationArmJarFillPlaceholderSecondary = new AnimationInstance(animationGroup, 59);
		_animationArmJarFilled = new AnimationInstance(animationGroup, 17);
		ExplosionResourceKey = Type.GetAbsolutePath("SONICORCA/OBJECTS/EXPLOSION/BOSS");
		HitSoundResourceKey = Type.GetAbsolutePath("SONICORCA/SOUND/BOSSHIT");
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -100, -100, 200, 200)
		};
		LockLifetime = true;
		_state = WaterEggmanInstance.State.MoveToSide;
		Health = 8;
		_nonHoverPosition = (Vector2)Position;
		_fillSide = 1;
		_boiler = Level.ObjectManager.AddSubObject<WaterEggmanInstance.Boiler>(this);
		_jarBack = Level.ObjectManager.AddSubObject<WaterEggmanInstance.JarBack>(this);
		_armFront = Level.ObjectManager.AddSubObject<WaterEggmanInstance.ArmFront>(this);
		_suctionTube = Level.ObjectManager.AddSubObject<WaterEggmanInstance.SuctionTube>(this);
		_jarEmpty = true;
		_isFaceSplash = false;
		_updater = new Updater(GetUpdates());
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		_updater.Update();
		if (_state != WaterEggmanInstance.State.Defeated && _state != WaterEggmanInstance.State.Recover)
			Hover();
		if (!_mobileExploding)
			return;
		UpdateExplosions(64 /*0x40*/);
	}

	private IEnumerable<UpdateResult> GetUpdates()
	{
		while (_state < WaterEggmanInstance.State.Defeated)
		{
			switch (_state)
			{
				case WaterEggmanInstance.State.MoveToSide:
					int dest1 = _fillSide == -1 ? LeftFillX : RightFillX;
					_nonHoverPosition.X = MathX.GoTowards(_nonHoverPosition.X, dest1, 12.0);
					_armExtension = Math.Max(0, _armExtension - 8);
					bool flag = _nonHoverPosition.X < (LeftFillX + RightFillX) / 2;
					if (_armExtension == 0)
					{
						if (_flipX != flag)
						{
							if (_animationEggMobileChasis.Index != 26)
							{
								_animationEggMobileChasis.Index = 26;
								_animationEggMobileChasis.Cycles = 0;
								_animationEggMobileScreen.Index = 28;
								_animationRobotnik.Index = 4;
								_animationArmTurn.Index = 30;
								_animationArmTurn.ResetFrame();
								_animationEggMobileCap.Index = 56;
								_turning = true;
							}
							else if (_animationEggMobileChasis.Cycles >= 1)
							{
								_animationEggMobileChasis.Index = 27;
								_animationEggMobileScreen.Index = 29;
								_animationRobotnik.Index = 5;
								_animationArmTurn.Index = 31 /*0x1F*/;
								_animationArmTurn.ResetFrame();
								_animationEggMobileCap.Index = 57;
								_flipX = flag;
							}
						}
						else if (_animationEggMobileChasis.Cycles >= 2)
							_turning = false;
					}
					if (_nonHoverPosition.X == dest1 && _armExtension == 0 && _flipX == flag && !_turning)
					{
						_state = WaterEggmanInstance.State.LoweringTube;
						break;
					}
					break;
				case WaterEggmanInstance.State.LoweringTube:
					if (_tubeY < 396)
					{
						_tubeY += 33;
						_armExtension = Math.Max(0, _armExtension - 8);
						break;
					}
					_tubeY = 396;
					_armExtension = 0;
					_stateTicks = 0;
					_state = WaterEggmanInstance.State.FillingJar;
					break;
				case WaterEggmanInstance.State.FillingJar:
					if (_stateTicks < 236)
					{
						++_stateTicks;
						if (_stateTicks > 100)
							_willJarSplashRobotnik = true;
						if (_stateTicks > 176 /*0xB0*/)
						{
							_jarFull = true;
							break;
						}
						break;
					}
					_state = WaterEggmanInstance.State.RaisingTube;
					break;
				case WaterEggmanInstance.State.RaisingTube:
					if (_tubeY > 0)
					{
						_tubeY -= 33;
						break;
					}
					_tubeY = 0;
					_state = WaterEggmanInstance.State.Aiming;
					break;
				case WaterEggmanInstance.State.Aiming:
					_armExtension = Math.Min(256 /*0x0100*/, _armExtension + 8);
					if (_armExtension >= 24)
						_willJarSplashRobotnik = false;
					ICharacter protagonist = Level.Player.Protagonist;
					int dest2 = _fillSide != -1 ? protagonist.Position.X + 256 /*0x0100*/ + 48 /*0x30*/ : protagonist.Position.X - 256 /*0x0100*/ - 48 /*0x30*/;
					_nonHoverPosition.X = MathX.GoTowards(_nonHoverPosition.X, dest2, 12.0);
					if (_armExtension == 256 /*0x0100*/ && _nonHoverPosition.X == dest2)
					{
						_stateTicks = 0;
						_state = WaterEggmanInstance.State.DroppingChemical;
						break;
					}
					break;
				case WaterEggmanInstance.State.DroppingChemical:
					if (_stateTicks == 2)
					{
						Vector2i vector2i = new Vector2i(_flipX ? 33 : -33, -186);
						vector2i.X += _flipX ? _armExtension : -_armExtension;
						vector2i.X += _flipX ? 8 : -8;
						vector2i.Y -= 16 /*0x10*/;
						WaterEggmanInstance.ChemicalDroplet chemicalDroplet = Level.ObjectManager.AddSubObject<WaterEggmanInstance.ChemicalDroplet>(this);
						chemicalDroplet.FlipX = _flipX;
						chemicalDroplet.Position = Position + vector2i;
						_jarEmpty = true;
						_jarFull = false;
					}
					if (_stateTicks < 30)
					{
						++_stateTicks;
						break;
					}
					_fillSide = _fillSide == -1 ? 1 : -1;
					_state = WaterEggmanInstance.State.MoveToSide;
					break;
			}
			yield return UpdateResult.Next();
		}
		_animationEggMobileChasis.Index = 47;
		_animationEggMobileScreen.Index = 49;
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
		do
		{
			_velocityY += 0.375;
			_nonHoverPosition += new Vector2(0.0, _velocityY);
			PositionPrecise = _nonHoverPosition;
			yield return UpdateResult.Next();
		}
		while (_velocityY < 15.0);
		_isFaceSplash = false;
		DoRobotnikNormal();
		_state = WaterEggmanInstance.State.Recover;
		for (int i = 0; i < 60; ++i)
		{
			_nonHoverPosition += new Vector2(0.0, -6.0);
			PositionPrecise = _nonHoverPosition;
			yield return UpdateResult.Next();
		}
		Defeated = true;
		_state = WaterEggmanInstance.State.Fleeing;
		Fleeing = true;
		PositionPrecise = _nonHoverPosition;
		int hover_failure_ticks = 0;
		while (true)
		{
			_nonHoverPosition += new Vector2(12.0, 0.0);
			if (!_turning)
			{
				switch (hover_failure_ticks)
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
				if (_animationEggMobileChasis.Index != 51)
				{
					_animationEggMobileChasis.Index = 51;
					_animationEggMobileChasis.Cycles = 0;
					_animationEggMobileScreen.Index = 53;
					_animationEggMobileCap.Index = 56;
					_turning = false;
				}
				else if (_animationEggMobileChasis.Cycles >= 1)
				{
					_animationEggMobileChasis.Index = 52;
					_animationEggMobileScreen.Index = 54;
					_animationEggMobileCap.Index = 57;
					_flipX = true;
				}
			}
			else if (_animationEggMobileChasis.Cycles >= 2 && _turning)
			{
				_turning = false;
				_animationEggMobileScreen.Index = 49;
			}
			if (!_turning && hover_failure_ticks < 15 && _flipX)
			{
				_animationEggMobileChasis.Index = hover_failure_ticks % 5 != 0 ? 47 : 48 /*0x30*/;
				++hover_failure_ticks;
				_nonHoverPosition += new Vector2(0.0, 2.0);
			}
			else if (!_turning && hover_failure_ticks >= 15 && hover_failure_ticks < 45 && _flipX)
			{
				if (_animationEggMobileChasis.Index != 48 /*0x30*/)
					_animationEggMobileChasis.Index = 48 /*0x30*/;
				if (hover_failure_ticks < 30)
					_nonHoverPosition += new Vector2(0.0, -2.0);
				++hover_failure_ticks;
			}
			else
				hover_failure_ticks = 0;
			if (_stateTicks > 600)
				LockLifetime = false;
			yield return UpdateResult.Next();
		}
	}

	private void EmitSmoke(Vector2 offset, Vector2 velocity)
	{
		Matrix4 matrix4 = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
		if (_flipX)
			matrix4 *= Matrix4.CreateScale(-1.0, 1.0);
		Vector2i position = (Vector2i)((matrix4 * Matrix4.CreateTranslation(offset)).RotateZ(0.0) * new Vector2(0.0, 100.0));
		DustInstance dustInstance = Level.ObjectManager.AddObject(new ObjectPlacement("SONICORCA/OBJECTS/DUST", Level.Map.Layers.IndexOf(Layer), position)) as DustInstance;
		dustInstance.SetDustAnimationIndex(1);
		dustInstance.Velocity = velocity;
	}

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
		_state = WaterEggmanInstance.State.Defeated;
		_stateTicks = 0;
		_nonHoverPosition = PositionPrecise;
		DoRobotnikDefeated();
		CollisionRectangles = new CollisionRectangle[0];
		Level.Player.GainScore(1000);
		Level.CreateScoreObject(1000, Position + new Vector2i(0, sbyte.MinValue));
	}

	protected override void OnHurtCharacter(ICharacter character) => DoRobotnikSmile();

	public void DoRobotnikSmile()
	{
		if (_animationRobotnik.Index != 0)
			return;
		_animationRobotnik.Index = 1;
	}

	public void DoRobotnikFrown() => _animationRobotnik.Index = 2;

	public void DoRobotnikDefeated() => _animationRobotnik.Index = 3;

	public void DoRobotnikNormal() => _animationRobotnik.Index = 0;

	protected override void OnAnimate()
	{
		switch (_state)
		{
			case WaterEggmanInstance.State.MoveToSide:
				_animationArmTapCog.Index = 3;
				_animationArmTap.Index = 9;
				break;
			case WaterEggmanInstance.State.LoweringTube:
				_animationArmTapCog.Index = 3;
				_animationArmTap.Index = 9;
				_jarFillY = 0;
				_jarEmpty = true;
				_dripVisible = false;
				_suctionTube.TubeSuctionIndex = 22f;
				break;
			case WaterEggmanInstance.State.FillingJar:
				if (_stateTicks < 140)
				{
					_animationArmTapCog.Index = 4;
					_animationArmTap.Index = 10;
					_suctionTube.TubeSuctionIndex -= 0.5f;
					if ((double)_suctionTube.TubeSuctionIndex <= 0.0)
						_suctionTube.TubeSuctionIndex = 22f;
				}
				else if ((double)_suctionTube.TubeSuctionIndex > 0.0)
					_suctionTube.TubeSuctionIndex = Math.Max(0.0f, _suctionTube.TubeSuctionIndex - 0.5f);
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
					_animationArmJarFill.Index = 14;
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
					_animationArmJarFill.Index = 15;
				}
				else if (_stateTicks == 80 /*0x50*/)
				{
					_dripVisible = true;
					_dripPosition = new Vector2i();
				}
				else if (_stateTicks == 84)
				{
					_dripVisible = false;
					_animationArmJarFill.Index = 16 /*0x10*/;
					_jarFillY -= 20;
				}
				else if (_stateTicks == 110)
				{
					_dripVisible = true;
					_dripPosition = new Vector2i();
				}
				else if (_stateTicks == 114)
				{
					_dripVisible = false;
					_animationArmJarFill.Index = 16 /*0x10*/;
					_animationArmJarFill.ResetFrame();
					_jarFillY -= 20;
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
					_animationArmJarFill.Index = 16 /*0x10*/;
					_animationArmJarFill.ResetFrame();
					_jarFillY -= 20;
				}
				_animationArmJarFill.Animate();
				_animationArmJarFilled.Animate();
				break;
			case WaterEggmanInstance.State.DroppingChemical:
				if (_stateTicks == 0)
				{
					_jarLidOpen = true;
					_animationArmJarLidBack.ResetFrame();
					_animationArmJarLidFront.ResetFrame();
					_animationArmJarLidBack.Cycles = 0;
					_animationArmJarLidFront.Cycles = 0;
					break;
				}
				break;
			case WaterEggmanInstance.State.Defeated:
				if (_isFaceSplash)
				{
					_animationRobotnikFaceSplash.Animate();
					break;
				}
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
			Vector2i destination1 = new Vector2i(-26);
			Vector2i destination2 = destination1 + new Vector2i(0, 0);
			Vector2i destination3 = destination1 + new Vector2i(0, -76);
			Vector2i destination4 = destination1 + new Vector2i(25, -223);
			Vector2i destination5 = destination1 + new Vector2i(0, 0);
			or.FilterAmount *= 0.5;
			if (IsInvincibleFlashing)
				or.AdditiveColour = BossObject.FlashAdditiveColour;
			or.Render(_animationEggMobileCap, (Vector2)destination2);
			or.Render(_animationEggMobileChasis, (Vector2)destination1);
			or.AdditiveColour = Colours.Black;
			if (_isFaceSplash)
				or.Render(_animationRobotnikFaceSplash, (Vector2)destination4);
			else
				or.Render(_animationRobotnik, (Vector2)destination3);
			if (IsInvincibleFlashing)
				or.AdditiveColour = BossObject.FlashAdditiveColour;
			or.Render(_animationEggMobileScreen, (Vector2)destination5);
		}
		using (or.BeginMatixState())
		{
			if (_flipX)
				or.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			Vector2i vector2i1 = new Vector2i(0);
			Vector2i vector2i2 = new Vector2i(60, -171);
			if (_animationArmTurn.Index == 30)
			{
				if (_animationArmTurn.CurrentFrameIndex == 0)
					vector2i2.Y = -172;
				else if (_animationArmTurn.CurrentFrameIndex > 0)
					vector2i2.Y = -195;
			}
			if (_animationArmTurn.Index == 31 /*0x1F*/)
			{
				if (_animationArmTurn.CurrentFrameIndex == 3)
					vector2i2.Y = -172;
				else if (_animationArmTurn.CurrentFrameIndex == 2)
					vector2i2.Y = -172;
				else if (_animationArmTurn.CurrentFrameIndex < 3)
					vector2i2.Y = -195;
			}
			or.ModelMatrix *= Matrix4.CreateTranslation(vector2i2.X, vector2i2.Y);
			Vector2i vector2i3 = new Vector2i(-64, 0);
			Vector2i destination = vector2i1 + vector2i3;
			if (!_turning || _state >= WaterEggmanInstance.State.Defeated)
				return;
			or.Render(_animationArmTurn, (Vector2)destination);
		}
	}

	private void ReleaseFragmentMetal(Vector2i position, double speed, int count = 1, bool explode = true)
	{
		for (int index = 0; index < count; ++index)
		{
			double num = Level.Random.NextRadians();
			Vector2 velocity = new Vector2(speed * Math.Cos(num), speed * Math.Sin(num));
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
		fragment.AnimationIndex = Level.Random.Next(40, 47);
		fragment.Position = position;
		fragment.Velocity = velocity;
		fragment.AngularVelocity = Math.PI / 30.0;
		fragment.FlipX = Level.Random.NextBoolean();
		fragment.Initialise();
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

	private class Boiler : ActiveObject
	{
		private static Vector2i OriginOffset = new Vector2i(60, -176);
		private Updater _updater;
		private bool _flipX;
		private WaterEggmanInstance.ExplodeState _explodeState;
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
				if (_explodeState == WaterEggmanInstance.ExplodeState.Falling)
					objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(_angle);
				objectRenderer.Render(Parent._animationBaseShadow, new Vector2(3.0, 100.0));
				if (Parent.IsInvincibleFlashing)
					objectRenderer.AdditiveColour = BossObject.FlashAdditiveColour;
				if (_explodeState < WaterEggmanInstance.ExplodeState.Exploded)
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
			Vector2i vector2i = WaterEggmanInstance.Boiler.OriginOffset + offset;
			if (_flipX)
				vector2i.X *= -1;
			return Position + vector2i;
		}

		private IEnumerable<UpdateResult> GetUpdates()
		{
			while (_explodeState == WaterEggmanInstance.ExplodeState.NotExploding)
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
			_explodeState = WaterEggmanInstance.ExplodeState.Exploded;
			_explodedAnimationInstance = new AnimationInstance(Parent._animationGroup, 37);
			yield return UpdateResult.Wait(60);
			int flipMultiplier = _flipX ? -1 : 1;
			while (_explodeState != WaterEggmanInstance.ExplodeState.Falling)
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
			Matrix4 matrix4 = Matrix4.Identity * Matrix4.CreateTranslation((Vector2)Position);
			if (_flipX)
				matrix4 *= Matrix4.CreateScale(-1.0, 1.0);
			Vector2i position = (Vector2i)((matrix4 * Matrix4.CreateTranslation((Vector2)WaterEggmanInstance.Boiler.OriginOffset)).RotateZ(_angle) * new Vector2(0.0, 100.0));
			(Level.ObjectManager.AddObject(new ObjectPlacement("SONICORCA/OBJECTS/DUST", Level.Map.Layers.IndexOf(Layer), position)) as DustInstance).SetDustAnimationIndex(1);
		}

		public bool Explode()
		{
			if (_explodeState == WaterEggmanInstance.ExplodeState.NotExploding)
			{
				_explodeState = WaterEggmanInstance.ExplodeState.Exploding;
				return true;
			}
			return _explodeState == WaterEggmanInstance.ExplodeState.Exploding;
		}

		public void Fall() => _explodeState = WaterEggmanInstance.ExplodeState.Falling;
	}

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
			if (parent._state == WaterEggmanInstance.State.Fleeing)
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

	private class ArmFront : ActiveObject
	{
		private static readonly Vector2i ArmBeginPoint = new Vector2i(10, -186);
		private Updater _updater;
		private bool _flipX;
		private WaterEggmanInstance.ExplodeState _jarExplodeState;
		private WaterEggmanInstance.ExplodeState _barExplodeState;
		private int _barExplodedLength;
		private int _armJarPlaceholderY;

		public Vector2 Velocity { get; set; }

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		protected override void OnStart()
		{
			_updater = new Updater(GetUpdates());
			Priority = 330;
			LockLifetime = true;
			_armJarPlaceholderY = 0;
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
			if (_jarExplodeState != WaterEggmanInstance.ExplodeState.NotExploding)
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
				int num1 = 47;
				bool flag;
				if (Parent._jarFillY < 0)
				{
					flag = true;
					_armJarPlaceholderY = num1 + Parent._jarFillY;
				}
				else
					flag = false;
				for (int index = num1; index > num1 + Parent._jarFillY; index -= 11)
				{
					Rectanglei source = Parent._animationArmJarFilled.CurrentFrame.Source;
					Rectanglei destination = new Rectanglei((int)offset.X + x - source.Width / 2, (int)offset.Y + index - 16 /*0x10*/ - source.Height / 2, source.Width, source.Height);
					or.Texture = Parent._animationArmJarFilled.CurrentTexture;
					or.Render((Rectangle)source, (Rectangle)destination, _flipX);
				}
				int y = num1 + Parent._jarFillY - Parent._animationArmJarFill.CurrentFrame.Source.Height / 2;
				if (Parent._animationArmJarFill.Index == 16 /*0x10*/)
					y += 17;
				or.Render(Parent._animationArmJarFill, offset + new Vector2(x, y), _flipX);
				if (Parent._animationArmJarFill.Index >= 15 && flag)
				{
					int num2 = Parent._jarFillY / -20;
					for (int index = 0; index < num2; ++index)
					{
						switch (num2)
						{
							case 1:
								or.Render(Parent._animationArmJarFillPlaceholderPrimary, offset + new Vector2(x, num1 + index * -20 - 16 /*0x10*/ + Math.Sign(index) * 8 * index), _flipX);
								break;
							case 2:
								if (index == 0)
								{
									or.Render(Parent._animationArmJarFillPlaceholderPrimary, offset + new Vector2(x, num1 + index * -20 - 16 /*0x10*/ + Math.Sign(index) * 8 * index), _flipX);
									break;
								}
								or.Render(Parent._animationArmJarFillPlaceholderPrimary, offset + new Vector2(x, num1 + index * -20 - 16 /*0x10*/ + Math.Sign(index) * 8 * index), _flipX, true);
								break;
							default:
								if (index == 0)
								{
									or.Render(Parent._animationArmJarFillPlaceholderPrimary, offset + new Vector2(x, num1 + index * -20 - 16 /*0x10*/ + Math.Sign(index) * 8 * index), _flipX);
									break;
								}
								if (index == num2 - 1)
								{
									or.Render(Parent._animationArmJarFillPlaceholderPrimary, offset + new Vector2(x, num1 + index * -20 - 16 /*0x10*/ + Math.Sign(index) * 8 * index), _flipX, true);
									break;
								}
								or.Render(Parent._animationArmJarFillPlaceholderSecondary, offset + new Vector2(x, num1 + index * -20 - 16 /*0x10*/ + Math.Sign(index) * 8 * index), _flipX);
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

		private IEnumerable<UpdateResult> GetUpdates()
		{
			while (_jarExplodeState == WaterEggmanInstance.ExplodeState.NotExploding)
			{
				_flipX = Parent._flipX;
				Position = Parent.Position;
				if (Parent.Finished)
					Finish();
				yield return UpdateResult.Next();
			}
			WaterEggmanInstance.BrokenJar brokenJar = CreateBrokenJar();
			_jarExplodeState = WaterEggmanInstance.ExplodeState.Exploded;
			while (_barExplodeState != WaterEggmanInstance.ExplodeState.Exploding)
				yield return UpdateResult.Next();
			brokenJar.Fall();
			_jarExplodeState = WaterEggmanInstance.ExplodeState.Falling;
			yield return UpdateResult.Wait(2);
			Vector2i[] vector2iArray = GetArmBarPositions().Reverse<Vector2i>().ToArray<Vector2i>();
			for (int index = 0; index < vector2iArray.Length; ++index)
			{
				Vector2i vector2i = vector2iArray[index];
				_barExplodedLength += 16 /*0x10*/;
				EmitBar(Position + vector2i);
				yield return UpdateResult.Wait(4);
			}
			vector2iArray = null;
			_barExplodeState = WaterEggmanInstance.ExplodeState.Exploded;
		}

		private IEnumerable<Vector2i> GetArmBarPositions()
		{
			for (Vector2i position = WaterEggmanInstance.ArmFront.ArmBeginPoint; position.X >= -Parent._armExtension + 4 + _barExplodedLength; position.X -= 16 /*0x10*/)
			{
				if (_flipX)
					yield return position;
				else
					yield return position;
			}
		}

		private WaterEggmanInstance.BrokenJar CreateBrokenJar()
		{
			Parent._jarBack.Finish();
			Vector2i position = Position + Parent.JarOffset;
			Parent.ReleaseFragmentMetal(position, 12.0, 12);
			WaterEggmanInstance.BrokenJar brokenJar = Level.ObjectManager.AddSubObject<WaterEggmanInstance.BrokenJar>(Parent);
			brokenJar.Position = position;
			return brokenJar;
		}

		private void EmitBar(Vector2i position)
		{
			Vector2 vector2 = new Vector2(Level.Random.NextSign() * Level.Random.NextDouble(2.0, 8.0), -12.0);
			Fragment fragment = Level.ObjectManager.AddSubObject<Fragment>(this);
			fragment.AnimationGroup = Parent._animationGroup;
			fragment.AnimationIndex = 39;
			fragment.Position = position;
			fragment.Velocity = vector2;
			fragment.AngularVelocity = Math.PI / 20.0;
			fragment.Initialise();
			Parent.ExplodeAt(position);
		}

		public bool ExplodeJar()
		{
			if (_jarExplodeState == WaterEggmanInstance.ExplodeState.NotExploding)
			{
				_jarExplodeState = WaterEggmanInstance.ExplodeState.Exploding;
				return true;
			}
			return _jarExplodeState == WaterEggmanInstance.ExplodeState.Exploding;
		}

		public bool ExplodeBar()
		{
			if (_barExplodeState == WaterEggmanInstance.ExplodeState.NotExploding)
			{
				_barExplodeState = WaterEggmanInstance.ExplodeState.Exploding;
				return true;
			}
			return _barExplodeState == WaterEggmanInstance.ExplodeState.Exploding;
		}
	}

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
			_animation = new AnimationInstance(Parent._animationGroup, 36);
		}

		protected override void OnUpdate()
		{
			if (!_falling)
				return;
			int num = Parent._flipX ? -1 : 1;
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
					_angle -= Math.PI / 30.0 * num;
					_velocity += new Vector2(0.0, 0.875);
				}
			}
			else
			{
				_angle -= Math.PI / 75.0 * num;
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

	private class SuctionTube : ActiveObject
	{
		public const int TubeMaxIndex = 22;
		private Updater _updater;
		private bool _flipX;
		private int _numSegments;
		private int[] _frameMap = new int[26];
		private WaterEggmanInstance.ExplodeState _explodeState;
		private AnimationInstance _explodedAnimationInstance;
		private Vector2 _velocity;
		private double[] _angle = new double[2];

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
			int y1 = 80 /*0x50*/;
			if (_explodeState <= WaterEggmanInstance.ExplodeState.Exploded)
			{
				Animation animation = Parent._animationGroup[7];
				int[] frameMap = _frameMap;
				Array.Clear(frameMap, 0, frameMap.Length);
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
					Animation.Frame frame = animation.Frames[index + 4 >= 22 ? 0 : frameMap[index + 4]];
					objectRenderer.Texture = Parent._animationGroup.Textures[frame.TextureIndex];
					objectRenderer.Render((Rectangle)frame.Source, new Vector2(x, y1));
					y1 += 16 /*0x10*/;
				}
			}
			else
			{
				int num = 80 /*0x50*/ + _numSegments * 16 /*0x10*/;
				int index = 0;
				int y2 = y1 - 8;
				while (y2 < num)
				{
					Rectanglei source = _explodedAnimationInstance.CurrentFrame.Source;
					Rectanglei destination = (Rectanglei)new Rectangle(x - source.Width / 2, y2, source.Width, source.Height);
					if (destination.Bottom > num)
					{
						destination.Bottom = num;
						source.Height = destination.Height;
					}
					using (objectRenderer.BeginMatixState())
					{
						objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.Translate(destination.Centre.X, destination.Centre.Y).RotateZ(_angle[index]);
						destination.X = -destination.Width / 2;
						destination.Y = -destination.Height / 2;
						objectRenderer.Texture = _explodedAnimationInstance.CurrentTexture;
						objectRenderer.Render((Rectangle)source, (Rectangle)destination);
						y2 += source.Height;
					}
					index = (index + 1) % 2;
				}
			}
		}

		private IEnumerable<UpdateResult> GetUpdates()
		{
			while (_explodeState == WaterEggmanInstance.ExplodeState.NotExploding)
			{
				_flipX = Parent._flipX;
				_numSegments = (Parent._tubeY - 80 /*0x50*/) / 16 /*0x10*/;
				Position = Parent.Position;
				if (Parent.Finished)
					Finish();
				yield return UpdateResult.Next();
			}
			int maxY = 80 /*0x50*/ + _numSegments * 16 /*0x10*/;
			int x = _flipX ? 32 /*0x20*/ : -24;
			int y = 80 /*0x50*/;
			while (y < maxY)
			{
				Parent.ReleaseFragmentMetal(Position + new Vector2i(x, y), 4.0);
				y += 136;
				yield return UpdateResult.Wait(4);
			}
			_explodeState = WaterEggmanInstance.ExplodeState.Exploded;
			_explodedAnimationInstance = new AnimationInstance(Parent._animationGroup, 38);
			while (_explodeState != WaterEggmanInstance.ExplodeState.Falling)
				yield return UpdateResult.Next();
			Parent.ReleaseFragmentMetal(Position + new Vector2i(0, 80 /*0x50*/), 4.0);
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

		public bool Explode()
		{
			if (_explodeState == WaterEggmanInstance.ExplodeState.NotExploding)
			{
				_explodeState = WaterEggmanInstance.ExplodeState.Exploding;
				return true;
			}
			return _explodeState == WaterEggmanInstance.ExplodeState.Exploding;
		}

		public void Fall() => _explodeState = WaterEggmanInstance.ExplodeState.Falling;
	}

	private class ChemicalDroplet : Enemy
	{
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
			_animationInstance = new AnimationInstance(Parent._animationGroup, 18);
			Priority = 320;
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -64, -64, 128 /*0x80*/, 128 /*0x80*/)
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
					Finish();
					yield break;
				}
			}
			while (!HasHitFloor());
			EmitProjectiles();
			EmitSmallProjectile(new Vector2i(-64, -12));
			EmitSmallProjectile(new Vector2i(-38, -22));
			EmitSmallProjectile(new Vector2i(32 /*0x20*/, -22));
			EmitSmallProjectile(new Vector2i(60, -14));
			_splashed = true;
			_animationInstance = new AnimationInstance(Parent._animationGroup, 32 /*0x20*/);
			while (_animationInstance.Cycles == 0)
				yield return UpdateResult.Next();
			Finish();
		}

		private bool HasHitFloor()
		{
			double num = double.MaxValue;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 64 /*0x40*/))
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
					num = Math.Min(num, collision.Shift);
				}
			}
			if (num == double.MaxValue)
				return false;
			MovePrecise(0.0, num);
			return true;
		}

		private void EmitProjectiles()
		{
			for (int index = 0; index < 5; ++index)
			{
				WaterEggmanInstance.ChemicalDropletProjectile dropletProjectile = Level.ObjectManager.AddSubObject<WaterEggmanInstance.ChemicalDropletProjectile>(Parent);
				dropletProjectile.Position = Position;
				dropletProjectile.Velocity = new Vector2(Level.Random.NextSign() * Level.Random.Next(2, 5), -_velocityY - Level.Random.Next(0, 4));
			}
		}

		private void EmitSmallProjectile(Vector2i position)
		{
			WaterEggmanInstance.SmallChemicalDropletProjectile dropletProjectile = Level.ObjectManager.AddSubObject<WaterEggmanInstance.SmallChemicalDropletProjectile>(ParentObject);
			dropletProjectile.Position = Position + position;
			double num = Math.Atan2(position.Y, position.X);
			dropletProjectile.Velocity = new Vector2(8.0 * Math.Cos(num), 32.0 * Math.Sin(num));
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
					Vector2 vector2 = new Vector2(32.0, 0.0);
					Rectanglei source = _animationInstance.CurrentFrame.Source;
					Rectanglei destination = (Rectanglei)new Rectangle(-source.Width / 2 + vector2.X, 64 /*0x40*/ - source.Height, source.Width, source.Height);
					objectRenderer.Texture = _animationInstance.CurrentTexture;
					objectRenderer.Render((Rectangle)source, (Rectangle)destination);
				}
			}
		}
	}

	private class ChemicalDropletProjectile : Enemy
	{
		private AnimationInstance _animationInstance;

		public WaterEggmanInstance Parent => ParentObject as WaterEggmanInstance;

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			_animationInstance = new AnimationInstance(Parent._animationGroup, 34);
			Priority = 320;
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -8, -8, 16 /*0x10*/, 16 /*0x10*/)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Velocity += new Vector2(0.0, 0.875);
			PositionPrecise = PositionPrecise + Velocity;
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

	private class SmallChemicalDropletProjectile : ParticleObject
	{
		public Vector2 Velocity { get; set; }

		public SmallChemicalDropletProjectile()
		  : base("/ANIGROUP", 33, 0)
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
