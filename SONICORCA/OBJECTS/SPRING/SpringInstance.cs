using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SPRING;

/// <summary>
/// A spring. When a character touches the spring side, the spring bounces the character in its direction with its
/// strength. A spring with a strength of 52 or more uses the strong spring sprite. A horizontal spring launches only a
/// character on the ground, and also a character on the ground just in front of it.
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

	/// <summary>
	/// The area in front of a horizontal spring where it launches a character on the ground without a touch
	/// (loc_18BC6 in the original): $28 px in front and $18 px up and down.
	/// </summary>
	private const int ProximityDistance = 0x28 * 4;
	private const int ProximityHalfHeight = 0x18 * 4;

	/// <summary>
	/// In the Original physics, a diagonal up spring launches only a character on its sloped face that is past this
	/// distance behind the spring position (4 px, loc_18DB4 in the original).
	/// </summary>
	private const int DiagonalLaunchDistance = 4 * 4;

	private SpringDirection _direction = SpringDirection.Right;
	private int _strength = 40;
	private AnimationInstance _animation;
	private int _bounceAnimationIndex;
	private Vector2 _bounceVelocity;
	private int? _twirlTurns;
	private int _path = -1;
	private bool _clearOtherVelocity;

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

	/// <summary>
	/// The number of somersaults of a launched character (subtype bit 0 in the original). The default is one
	/// somersault for an up spring and none for the other springs.
	/// </summary>
	[StateVariable]
	private int TwirlTurns
	{
		get => _twirlTurns ?? (_direction == SpringDirection.Up ? 1 : 0);
		set => _twirlTurns = value;
	}

	/// <summary>The collision path that the spring sets on the character (subtype bits 2-3), or -1 to keep it.</summary>
	[StateVariable]
	private int Path
	{
		get => _path;
		set => _path = value;
	}

	/// <summary>
	/// Whether the spring sets the velocity of the other axis to 0 (subtype bit 7). An up or down spring then
	/// clears the x velocity.
	/// </summary>
	[StateVariable]
	private bool ClearOtherVelocity
	{
		get => _clearOtherVelocity;
		set => _clearOtherVelocity = value;
	}

	public SpringInstance() => DesignBounds = new Rectanglei(-101, -104, 202, 208);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpringType.AnimationGroupResourceKey));
		_animation.Index = _strength >= StrongSpringMinStrength ? AnimationStrong : AnimationWeak;
		if ((int)_direction % 2 == 1)
			_animation.Index += DiagonalAnimationOffset;
		_bounceAnimationIndex = _animation.Index + 1;

		SetCollision(_direction);
		Priority = -256;
	}

	/// <summary>Sets the collision shape and the bounce velocity. The vectors with the spring collision ID are the spring side.</summary>
	private void SetCollision(SpringDirection direction)
	{
		switch (direction)
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
			case SpringDirection.DownRight:
			case SpringDirection.DownLeft:
				// The shape of the spring that points up to the same side, upside down
				SetUpsideDownCollision(direction == SpringDirection.DownRight ? SpringDirection.UpRight : SpringDirection.UpLeft);
				break;
		}
	}

	/// <summary>
	/// Makes the collision of a spring that points down and to a side from the spring that points up to that side:
	/// the Y of each point changes sign, and each vector changes direction so that its solid side stays outside.
	/// </summary>
	private void SetUpsideDownCollision(SpringDirection upDirection)
	{
		SetCollision(upDirection);
		CollisionVectors = CollisionVectors.Select(v =>
		{
			CollisionVector flipped = new CollisionVector(this, new Vector2i(v.RelativeB.X, -v.RelativeB.Y), new Vector2i(v.RelativeA.X, -v.RelativeA.Y));
			flipped.Id = v.Id;
			return flipped;
		}).ToArray();
		_bounceVelocity = new Vector2(_bounceVelocity.X, -_bounceVelocity.Y);
	}

	private bool IsHorizontal => _direction == SpringDirection.Left || _direction == SpringDirection.Right;

	protected override void OnUpdate()
	{
		if (!Level.OriginalPhysics || !IsHorizontal || _animation.Index == _bounceAnimationIndex)
			return;
		// A horizontal spring also launches a character on the ground in front of it, when the character does not
		// move toward the spring.
		int direction = Math.Sign(_bounceVelocity.X);
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.IsDebug || !character.ShouldReactToLevel || character.IsObjectControlled || character.IsAirborne)
				continue;
			if (character.GroundVelocity * direction < 0.0)
				continue;
			int distance = (character.Position.X - Position.X) * direction;
			int height = character.Position.Y - Position.Y;
			if (distance < 0 || distance >= ProximityDistance || height < -ProximityHalfHeight || height >= ProximityHalfHeight)
				continue;
			Launch((Character)character);
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != SpringCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		Character character = (Character)e.ActiveObject;
		// In the Original physics, a horizontal spring launches only a character that pushes it on the ground.
		if (Level.OriginalPhysics && IsHorizontal && character.IsAirborne)
			return;
		if (Level.OriginalPhysics && IsDiagonalUp && !IsOnDiagonalLaunchArea(character, e))
			return;
		Launch(character);
		e.MaintainVelocity = true;
	}

	private bool IsDiagonalUp => _direction == SpringDirection.UpRight || _direction == SpringDirection.UpLeft;

	/// <summary>
	/// Returns true when the character touches the sloped face (the first collision vector) of a diagonal up spring,
	/// past <see cref="DiagonalLaunchDistance"/> behind the spring position. The original does not launch from the
	/// flat top or from the sides.
	/// </summary>
	private bool IsOnDiagonalLaunchArea(Character character, CollisionEvent e)
	{
		if (e.CollisionInfo?.Vector != CollisionVectors[0])
			return false;
		return _direction == SpringDirection.UpRight
			? character.Position.X > Position.X - DiagonalLaunchDistance
			: character.Position.X <= Position.X + DiagonalLaunchDistance;
	}

	private void Launch(Character character)
	{
		int twirlTurns = TwirlTurns;
		if (_direction == SpringDirection.Up || _direction == SpringDirection.UpRight || _direction == SpringDirection.UpLeft)
		{
			character.LeaveGround();
			character.IsAirborne = true;
			character.IsSpinball = false;
			if (twirlTurns == 0)
				character.Animation.Index = CharacterAnimationSpringLaunch;
		}
		if (twirlTurns != 0 && character.IsAirborne)
		{
			// The character does somersaults.
			character.TumbleAngle = 0.0;
			character.TumbleTurns = twirlTurns;
			character.GroundVelocity = 1.0;
		}
		if (character.IsAirborne)
		{
			// Keep the velocity of the character on an axis where the spring has no push, unless the spring clears it.
			double otherX = _clearOtherVelocity ? 0.0 : character.Velocity.X;
			double otherY = _clearOtherVelocity ? 0.0 : character.Velocity.Y;
			double velocityX = _bounceVelocity.X == 0.0 ? otherX : _bounceVelocity.X;
			double velocityY = _bounceVelocity.Y == 0.0 ? otherY : _bounceVelocity.Y;
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
		if (_path >= 0)
			character.Path = _path;
		_animation.Index = _bounceAnimationIndex;
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
					objectRenderer.Render(_animation);
					break;
				case SpringDirection.DownRight:
					objectRenderer.Render(_animation, flipY: true);
					break;
				case SpringDirection.Right:
					objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Math.PI / 2.0);
					objectRenderer.Render(_animation, true);
					break;
				case SpringDirection.Down:
					objectRenderer.Render(_animation, flipY: true);
					break;
				case SpringDirection.DownLeft:
					objectRenderer.Render(_animation, true, true);
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
