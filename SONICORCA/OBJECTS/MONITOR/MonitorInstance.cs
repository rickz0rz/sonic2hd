using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SONICORCA.OBJECTS.MONITOR;

/// <summary>
/// An item monitor. A spinning character breaks it and gets the item that shows on its screen.
/// A hit from below knocks the monitor up, and it falls back to the ground.
/// </summary>
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
	private const int AnimationContentsLifeTails = 16;
	private const int AnimationContentsRobotnik = 17;
	private const int AnimationContentsRing = 18;
	private const int AnimationContentsSpeedShoesSonic = 19;
	private const int AnimationContentsSpeedShoesTails = 20;
	private const int AnimationContentsBarrier = 21;
	private const int AnimationContentsInvincibility = 22;
	private const int AnimationContentsSwapPlaces = 23;
	private const int AnimationContentsRandom = 24;

	/// <summary>The collision vector ID of the bottom side. A character that hits it knocks the monitor up.</summary>
	private const int BottomCollisionId = 1;

	private const int RingsInMonitor = 10;
	private const double Gravity = 0.875;
	private const double KnockUpVelocity = -6.0;
	private const double ContentsGravity = 0.375;

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

	// The character is the protagonist character type as an integer: 1 is Sonic and 2 is Tails.
	private static int GetScreenContentsAnimationIndex(MonitorContents contents, int character)
	{
		switch (contents)
		{
			case MonitorContents.Life:
				return AnimationScreenContentsLifeSonic + character - 1;
			case MonitorContents.Ring:
				return AnimationScreenContentsRing;
			case MonitorContents.SpeedShoes:
				return AnimationScreenContentsSpeedShoesSonic + character - 1;
			case MonitorContents.Barrier:
				return AnimationScreenContentsBarrier;
			case MonitorContents.Invincibility:
				return AnimationScreenContentsInvincibility;
			case MonitorContents.Robotnik:
				return AnimationScreenContentsRobotnik;
			case MonitorContents.SwapPlaces:
				return AnimationScreenContentsSwapPlaces;
			default:
				return AnimationScreenContentsRandom;
		}
	}

	/// <summary>
	/// The animation of the item that rises from a broken monitor. In Original physics it is the clean icon
	/// (AnimationContents*), as in the original game. The HD release shows the screen icon with its scan lines, and
	/// HD physics keeps that. The two lists of animations have the same order.
	/// </summary>
	private int GetContentsAnimationIndex(MonitorContents contents, int character)
	{
		int screenIndex = GetScreenContentsAnimationIndex(contents, character);
		return Level.OriginalPhysics ? screenIndex - AnimationScreenContentsLifeSonic + AnimationContentsLifeSonic : screenIndex;
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
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, MonitorType.AnimationGroupResourceKey);
		_shellAnimation = new AnimationInstance(animationGroup);
		_shellOverlayAnimation = new AnimationInstance(animationGroup);
		_screenAnimation = new AnimationInstance(animationGroup);
		_screenOverlayAnimation = new AnimationInstance(animationGroup);
		_shellAnimation.Index = _broken ? AnimationBrokenShell : AnimationShell;
		_shellOverlayAnimation.Index = _broken ? AnimationBrokenShellOverlay : AnimationShellOverlay;
		_screenAnimation.Index = GetScreenContentsAnimationIndex(_contents, (int)Level.Player.ProtagonistCharacterType);
		_screenOverlayAnimation.Index = AnimationScreenOverlay;
		if (_broken)
			return;
		_contentsAnimation = new AnimationInstance(animationGroup);
		_contentsAnimation.Index = GetContentsAnimationIndex(_contents, (int)Level.Player.ProtagonistCharacterType);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-60, -60, 120, 120));
		CollisionVectors[3].Id = BottomCollisionId;
	}

	protected override void OnUpdate()
	{
		if (!_stationary)
		{
			PositionPrecise = PositionPrecise + _velocity;
			bool landed = false;
			foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 60))
			{
				// Land only on the terrain floor, not on walls or other objects.
				if (collision.Vector.Owner == null && !collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					landed = true;
				}
			}
			if (landed)
			{
				Entry.Position = Position;
				_stationary = true;
			}
			else
				_velocity += new Vector2(0.0, Gravity);
		}
		if (!_raisingContents)
			return;
		_contentsPosition += _contentsVelocity;
		_contentsVelocity.Y += ContentsGravity;
		if (_contentsVelocity.Y < 0.0)
			return;

		// The contents reached the top of their path: give the item to the character.
		_raisingContents = false;
		InvokeContents();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		if (e.Id == BottomCollisionId)
		{
			_stationary = false;
			_velocity.Y = KnockUpVelocity;
			character.Velocity = new Vector2(character.Velocity.X, character.Velocity.Y * -1.0);
			e.MaintainVelocity = true;
		}
		else
		{
			if (!CanCharacterBreak(character))
				return;
			Break(character);
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
		character.Velocity = new Vector2(character.Velocity.X, character.Velocity.Y * -1.0);
		if (character.Jumped)
			character.IsJumping = true;
		_broken = true;
		(Entry.State as MonitorInstance).Broken = true;
		_shellAnimation.Index = AnimationBrokenShell;
		_shellOverlayAnimation.Index = AnimationBrokenShellOverlay;
		CollisionVectors = new CollisionVector[0];
		CollisionRectangles = new CollisionRectangle[0];
		RegisterCollisionUpdate();
		CreateExplosionObject();
		CreateRaisingContents();
		LockLifetime = true;
	}

	private void CreateExplosionObject()
	{
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(MonitorType.ExplosionResourceKey), Level.Map.Layers.IndexOf(Layer), Position));
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
				_breaker.Player.GainRings(RingsInMonitor);
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
			case MonitorContents.Robotnik:
				// The Robotnik monitor hurts the character (robotnik_monitor and Touch_ChkHurt2 in the original).
				if (_breaker.CanBeHurt)
					_breaker.Hurt(Math.Sign(_breaker.Position.X - Position.X));
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
