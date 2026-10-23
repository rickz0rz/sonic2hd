// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPRING.SpringInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.SPRING;

/// <summary>
/// A spring. When a character touches the spring side, the spring bounces the character in its direction with its
/// strength. A spring with a strength of 52 or more uses the strong spring sprite.
/// </summary>
public class SpringInstance : ActiveObject
{
	private const int AnimationStrong = 0;
	private const int AnimationWeak = 2;

	/// <summary>The diagonal sprites come 4 animations after the straight sprites.</summary>
	private const int DiagonalAnimationOffset = 4;

	private const int StrongSpringMinStrength = 52;

	/// <summary>The collision vector ID of the spring side.</summary>
	private const int SpringCollisionId = 1;

	/// <summary>The Character animation that a diagonal spring sets. It stays until the character lands.</summary>
	private const int CharacterAnimationSpringLaunch = 12;

	private const int MinSlopeLockTicks = 16;

	private SpringDirection _direction = SpringDirection.Right;
	private int _strength = 40;
	private AnimationInstance _animation;
	private int _bounceAnimationIndex;
	private Vector2 _bounceVelocity;

	[StateVariable]
	private SpringDirection Direction
	{
		get => _direction;
		set => _direction = value;
	}

	[StateVariable]
	private int Strength
	{
		get => _strength;
		set => _strength = value;
	}

	public SpringInstance() => DesignBounds = new Rectanglei(-101, -104, 202, 208);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpringType.AnimationGroupResourceKey));
		_animation.Index = _strength >= StrongSpringMinStrength ? AnimationStrong : AnimationWeak;
		if ((int)_direction % 2 == 1)
			_animation.Index += DiagonalAnimationOffset;
		_bounceAnimationIndex = _animation.Index + 1;

		// Set the collision shape. The vectors with the spring collision ID are the spring side.
		// DownRight and DownLeft springs have no collision.
		switch (_direction)
		{
			case SpringDirection.Up:
				CollisionVectors = new CollisionVector[]
				{
					new CollisionVector(this, new Vector2i(-64, 64), new Vector2i(-64, 8)),
					new CollisionVector(this, new Vector2i(-64, 8), new Vector2i(-56, 0)),
					new CollisionVector(this, new Vector2i(-56, 0), new Vector2i(56, 0)),
					new CollisionVector(this, new Vector2i(56, 0), new Vector2i(64, 8)),
					new CollisionVector(this, new Vector2i(64, 8), new Vector2i(64, 64)),
					new CollisionVector(this, new Vector2i(-64, 64), new Vector2i(64, 64))
				};
				CollisionVectors[1].Id = SpringCollisionId;
				CollisionVectors[2].Id = SpringCollisionId;
				CollisionVectors[3].Id = SpringCollisionId;
				_bounceVelocity = new Vector2(0.0, -_strength);
				break;
			case SpringDirection.UpRight:
				CollisionVectors = new CollisionVector[]
				{
					new CollisionVector(this, new Vector2i(-48, -48), new Vector2i(48, 48)),
					new CollisionVector(this, new Vector2i(-48, -84), new Vector2i(-48, -48)),
					new CollisionVector(this, new Vector2i(-84, -84), new Vector2i(-48, -84)),
					new CollisionVector(this, new Vector2i(-84, 80), new Vector2i(-84, -48)),
					new CollisionVector(this, new Vector2i(48, 80), new Vector2i(-84, 80)),
					new CollisionVector(this, new Vector2i(48, 48), new Vector2i(48, 80))
				};
				CollisionVectors[0].Id = SpringCollisionId;
				CollisionVectors[1].Id = SpringCollisionId;
				CollisionVectors[2].Id = SpringCollisionId;
				CollisionVectors[5].Id = SpringCollisionId;
				_bounceVelocity = new Vector2(_strength, -_strength);
				break;
			case SpringDirection.Right:
				CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -60, 64, 128));
				CollisionVectors[2].Id = SpringCollisionId;
				_bounceVelocity = new Vector2(_strength, 0.0);
				break;
			case SpringDirection.Down:
				CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -69, 128, 64));
				CollisionVectors[3].Id = SpringCollisionId;
				_bounceVelocity = new Vector2(0.0, _strength);
				break;
			case SpringDirection.Left:
				CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(5, -64, 64, 128));
				CollisionVectors[0].Id = SpringCollisionId;
				_bounceVelocity = new Vector2(-_strength, 0.0);
				break;
			case SpringDirection.UpLeft:
				CollisionVectors = new CollisionVector[]
				{
					new CollisionVector(this, new Vector2i(-48, 48), new Vector2i(48, -48)),
					new CollisionVector(this, new Vector2i(48, -48), new Vector2i(84, -48)),
					new CollisionVector(this, new Vector2i(84, -48), new Vector2i(84, 80)),
					new CollisionVector(this, new Vector2i(84, 80), new Vector2i(-48, 80)),
					new CollisionVector(this, new Vector2i(-48, 80), new Vector2i(-48, 48))
				};
				CollisionVectors[0].Id = SpringCollisionId;
				CollisionVectors[1].Id = SpringCollisionId;
				_bounceVelocity = new Vector2(-_strength, -_strength);
				break;
		}
		Priority = -256;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != SpringCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		Character character = (Character)e.ActiveObject;
		if (_direction == SpringDirection.Up || _direction == SpringDirection.UpRight || _direction == SpringDirection.UpLeft)
		{
			character.LeaveGround();
			character.IsAirborne = true;
			character.IsSpinball = false;
			if (_direction == SpringDirection.Up)
			{
				// An up spring makes the character do one somersault.
				character.TumbleAngle = 0.0;
				character.TumbleTurns = 1;
				character.GroundVelocity = 1.0;
			}
			else
				character.Animation.Index = CharacterAnimationSpringLaunch;
		}
		if (character.IsAirborne)
		{
			// Keep the velocity of the character on an axis where the spring has no push.
			double velocityX = _bounceVelocity.X == 0.0 ? character.Velocity.X : _bounceVelocity.X;
			double velocityY = _bounceVelocity.Y == 0.0 ? character.Velocity.Y : _bounceVelocity.Y;
			character.Velocity = new Vector2(velocityX, velocityY);
			character.IsHurt = false;
		}
		else
		{
			character.GroundVelocity = _bounceVelocity.X;
			character.Facing = Math.Sign(_bounceVelocity.X);
			character.SlopeLockTicks = Math.Max(MinSlopeLockTicks, character.SlopeLockTicks);
			character.IsPushing = false;
		}
		_animation.Index = _bounceAnimationIndex;
		e.MaintainVelocity = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(SpringType.SoundResourceKey));
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			switch (_direction)
			{
				case SpringDirection.Up:
				case SpringDirection.UpRight:
				case SpringDirection.DownRight:
					objectRenderer.Render(_animation);
					break;
				case SpringDirection.Right:
					objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Math.PI / 2.0);
					objectRenderer.Render(_animation, true);
					break;
				case SpringDirection.Down:
				case SpringDirection.DownLeft:
					objectRenderer.Render(_animation, flipY: true);
					break;
				case SpringDirection.Left:
					objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Math.PI / 2.0);
					objectRenderer.Render(_animation, true, true);
					break;
				case SpringDirection.UpLeft:
					objectRenderer.Render(_animation, true);
					break;
			}
		}
	}
}
