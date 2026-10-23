// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.MONITOR.MonitorInstance
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
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.OBJECTS.MONITOR;

public class MonitorInstance : ActiveObject
{
	private const int AnimationShell = 0;
	private const int AnimationShellOverlay = 1;
	private const int AnimationBrokenShell = 2;
	private const int AnimationBrokenShellOverlay = 3;
	private const int AnimationScreenOverlay = 4;
	private const int AnimationScreenContentsLifeSonic = 5;
	private const int AnimationScreenContentsLifeTails = 6;
	private const int AnimationScreenContentsRobotnik = 7;
	private const int AnimationScreenContentsRing = 8;
	private const int AnimationScreenContentsSpeedShoesSonic = 9;
	private const int AnimationScreenContentsSpeedShoesTails = 10;
	private const int AnimationScreenContentsBarrier = 11;
	private const int AnimationScreenContentsInvincibility = 12;
	private const int AnimationScreenContentsSwapPlaces = 13;
	private const int AnimationScreenContentsRandom = 14;
	private const int AnimationContentsLifeSonic = 15;
	private const int AnimationContentsLifeTails = 16 /*0x10*/;
	private const int AnimationContentsRobotnik = 17;
	private const int AnimationContentsRing = 18;
	private const int AnimationContentsSpeedShoesSonic = 19;
	private const int AnimationContentsSpeedShoesTails = 20;
	private const int AnimationContentsBarrier = 21;
	private const int AnimationContentsInvincibility = 22;
	private const int AnimationContentsSwapPlaces = 23;
	private const int AnimationContentsRandom = 24;
	private MonitorContents _contents;
	private bool _broken;
	private AnimationInstance _shellAnimation;
	private AnimationInstance _shellOverlayAnimation;
	private AnimationInstance _screenAnimation;
	private AnimationInstance _screenOverlayAnimation;
	private bool _stationary;
	private Vector2 _velocity;
	private AnimationInstance _contentsAnimation;
	private Vector2 _contentsVelocity;
	private Vector2 _contentsPosition;
	private bool _raisingContents;
	private ICharacter _breaker;

	private static int GetScreenContentsAnimationIndex(MonitorContents contents, int character)
	{
		switch (contents)
		{
			case MonitorContents.Life:
				return 5 + character - 1;
			case MonitorContents.Ring:
				return 8;
			case MonitorContents.SpeedShoes:
				return 9 + character - 1;
			case MonitorContents.Barrier:
				return 11;
			case MonitorContents.Invincibility:
				return 12;
			case MonitorContents.Robotnik:
				return 7;
			case MonitorContents.SwapPlaces:
				return 13;
			default:
				return 14;
		}
	}

	private static int GetContentsAnimationIndex(MonitorContents contents, int character)
	{
		return MonitorInstance.GetScreenContentsAnimationIndex(contents, character);
	}

	[StateVariable]
	private MonitorContents Contents
	{
		get => _contents;
		set => _contents = value;
	}

	[StateVariable]
	private bool Broken
	{
		get => _broken;
		set => _broken = value;
	}

	public MonitorInstance() => DesignBounds = new Rectanglei(-60, -60, 120, 120);

	protected override void OnStart()
	{
		_stationary = true;
		AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_shellAnimation = new AnimationInstance(loadedResource);
		_shellOverlayAnimation = new AnimationInstance(loadedResource);
		_screenAnimation = new AnimationInstance(loadedResource);
		_screenOverlayAnimation = new AnimationInstance(loadedResource);
		_shellAnimation.Index = _broken ? 2 : 0;
		_shellOverlayAnimation.Index = _broken ? 3 : 1;
		_screenAnimation.Index = MonitorInstance.GetScreenContentsAnimationIndex(_contents, (int)Level.Player.ProtagonistCharacterType);
		_screenOverlayAnimation.Index = 4;
		if (_broken)
			return;
		_contentsAnimation = new AnimationInstance(loadedResource);
		_contentsAnimation.Index = MonitorInstance.GetContentsAnimationIndex(_contents, (int)Level.Player.ProtagonistCharacterType);
		CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(-60.0, -60.0, 120.0, 120.0));
		CollisionVectors[3].Id = 1;
	}

	protected override void OnUpdate()
	{
		if (!_stationary)
		{
			PositionPrecise = PositionPrecise + _velocity;
			bool flag = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 60))
			{
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					Vector2 positionPrecise = PositionPrecise;
					double x = positionPrecise.X;
					positionPrecise = PositionPrecise;
					double y = positionPrecise.Y + collision.Shift;
					PositionPrecise = new Vector2(x, y);
					flag = true;
				}
			}
			if (flag)
			{
				Entry.Position = Position;
				_stationary = true;
			}
			else
				_velocity += new Vector2(0.0, 0.875);
		}
		if (!_raisingContents)
			return;
		_contentsPosition += _contentsVelocity;
		_contentsVelocity.Y += 0.375;
		if (_contentsVelocity.Y < 0.0)
			return;
		_raisingContents = false;
		InvokeContents();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (e.Id == 1)
		{
			_stationary = false;
			_velocity.Y = -6.0;
			activeObject.Velocity = new Vector2(activeObject.Velocity.X, activeObject.Velocity.Y * -1.0);
			e.MaintainVelocity = true;
		}
		else
		{
			if (!CanCharacterBreak(activeObject))
				return;
			Break(activeObject);
			e.IgnoreCollision = true;
		}
	}

	private bool CanCharacterBreak(ICharacter character)
	{
		return character.Velocity.Y >= 0.0 && !character.IsSidekick && character.IsSpinball;
	}

	private void Break(ICharacter character)
	{
		_breaker = character;
		ICharacter character1 = character;
		Vector2 velocity = character.Velocity;
		double x = velocity.X;
		velocity = character.Velocity;
		double y = velocity.Y * -1.0;
		Vector2 vector2 = new Vector2(x, y);
		character1.Velocity = vector2;
		if (character.Jumped)
			character.IsJumping = true;
		_broken = true;
		(Entry.State as MonitorInstance).Broken = true;
		_shellAnimation.Index = 2;
		_shellOverlayAnimation.Index = 3;
		CollisionVectors = new CollisionVector[0];
		CollisionRectangles = new CollisionRectangle[0];
		RegisterCollisionUpdate();
		CreateExplosionObject();
		CreateRaisingContents();
		LockLifetime = true;
	}

	private void CreateExplosionObject()
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath("SONICORCA/OBJECTS/EXPLOSION/BADNIK"), Level.Map.Layers.IndexOf(Layer), Position));
	}

	private void CreateRaisingContents()
	{
		_contentsVelocity = new Vector2(0.0, -12.0);
		_contentsPosition = new Vector2(0.0, -13.0);
		_raisingContents = true;
	}

	private void InvokeContents()
	{
		LockLifetime = false;
		switch (_contents)
		{
			case MonitorContents.Life:
				_breaker.Player.GainLives();
				break;
			case MonitorContents.Ring:
				_breaker.Player.GainRings(10);
				Level.SoundManager.PlaySound(_breaker, "SONICORCA/SOUND/RING");
				break;
			case MonitorContents.SpeedShoes:
				_breaker.Player.GiveSpeedShoes();
				break;
			case MonitorContents.Barrier:
				_breaker.Player.GiveBarrier(BarrierType.Classic);
				Level.SoundManager.PlaySound(_breaker, "SONICORCA/SOUND/BARRIER");
				break;
			case MonitorContents.Invincibility:
				_breaker.Player.GiveInvincibility();
				break;
		}
	}

	protected override void OnAnimate()
	{
		_shellAnimation.Animate();
		_shellOverlayAnimation.Animate();
		if (!_broken)
		{
			_screenAnimation.Animate();
			_screenOverlayAnimation.Animate();
		}
		if (!_raisingContents)
			return;
		_contentsAnimation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_shellAnimation);
		if (viewOptions.Shadows)
			return;
		objectRenderer.FilterAmount *= 0.25;
		if (!_broken)
		{
			objectRenderer.Render(_screenAnimation, new Vector2(2.0, -10.0));
			objectRenderer.Render(_screenOverlayAnimation, new Vector2(0.0, -13.0));
		}
		objectRenderer.Render(_shellOverlayAnimation);
		if (!_raisingContents)
			return;
		objectRenderer.Render(_contentsAnimation, _contentsPosition);
	}
}
