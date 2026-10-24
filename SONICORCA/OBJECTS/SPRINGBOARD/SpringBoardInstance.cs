using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SPRINGBOARD;

/// <summary>
/// A spring board: a sloped board with a raised end. When a character steps on the board, the board goes down, then
/// springs back up and launches the characters on it into the air.
/// </summary>
public class SpringBoardInstance : ActiveObject
{
	private const int AnimationNormal = 0;
	private const int AnimationStep = 1;
	private const int AnimationDown = 2;
	private const int AnimationBounce = 3;

	/// <summary>The state in which the board waits for a character.</summary>
	private const int StateIdle = 0;

	/// <summary>The state in which the board launches the characters on it.</summary>
	private const int StateLaunch = 7;

	/// <summary>The launch y velocity without the boost of the launch table (-$400 in the original).</summary>
	private const double LaunchVelocity = -16.0;

	/// <summary>The minimum x speed for the x boost of the launch table ($400 in the original).</summary>
	private const double BoostMinSpeedX = 16.0;

	/// <summary>The distance from the board position to the left end of the original board ($1C px).</summary>
	private const int OriginalHalfWidth = 0x1C * 4;

	/// <summary>
	/// The launch boost for each original pixel from the lowered end of the board (byte_26550 in the original). One
	/// step is $100 (1 original pixel per frame) of y speed, and of x speed toward the raised end.
	/// </summary>
	private static readonly byte[] LaunchBoosts =
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1,
		1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2,
		3, 3, 3, 3, 3, 3, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0,
	};

	private AnimationInstance _animation;
	private bool _flipX;

	/// <summary>The step of the bounce sequence, from StateIdle to StateLaunch. Each update goes one step forward.</summary>
	private int _state;

	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public SpringBoardInstance() => DesignBounds = new Rectanglei(-116, -32, 232, 64);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath(SpringBoardType.AnimationGroupResourceKey)));

		// Vectors 0 and 1 are the two parts of the top of the board. SetCollision sets the positions.
		CollisionVectors = new CollisionVector[]
		{
			new CollisionVector(this, new Vector2i(), new Vector2i(), flags: CollisionFlags.Conveyor | CollisionFlags.NoAngle | CollisionFlags.Solid),
			new CollisionVector(this, new Vector2i(), new Vector2i(), flags: CollisionFlags.Conveyor | CollisionFlags.NoAngle | CollisionFlags.Solid),
			new CollisionVector(this, new Vector2i(), new Vector2i()),
			new CollisionVector(this, new Vector2i(), new Vector2i())
		};
		SetCollision();
	}

	protected override void OnUpdate()
	{
	}

	protected override void OnUpdateCollision() => UpdateBumping();

	private void UpdateBumping()
	{
		switch (_state)
		{
			case StateIdle:
				bool stepped = false;
				foreach (ICharacter standingCharacter in GetStandingCharacters())
				{
					if (IsOnTriggerPart(GetPositionOnBoard(standingCharacter)))
						stepped = true;
				}
				if (!stepped)
					break;
				SetCollision(32);
				++_state;
				break;
			case 1:
			case 2:
				++_state;
				break;
			case 3:
			case 4:
				SetCollision(16);
				++_state;
				break;
			case 5:
				SetCollision(32);
				++_state;
				break;
			case 6:
				SetCollision();
				++_state;
				Level.SoundManager.PlaySound(this, SpringBoardType.SoundResourceKey);
				break;
			case StateLaunch:
				foreach (ICharacter standingCharacter in GetStandingCharacters())
				{
					// The boost grows toward the raised end. When the character runs at $400 or faster, the boost
					// also pushes it toward the raised end.
					int boost = GetLaunchBoost(standingCharacter);
					double velocityX = standingCharacter.Velocity.X;
					if (System.Math.Abs(velocityX) >= BoostMinSpeedX)
						velocityX += _flipX ? -4 * boost : 4 * boost;
					standingCharacter.LeaveGround();
					standingCharacter.Velocity = new Vector2(velocityX, LaunchVelocity - 4 * boost);
					standingCharacter.TumbleAngle = 0.0;
					standingCharacter.TumbleTurns = 1;
				}
				_state = StateIdle;
				break;
		}
	}

	/// <summary>
	/// Gets the launch boost of a character from its distance to the lowered end of the board, in original pixels
	/// (loc_2645E in the original). The original mirrors the distance on a flipped board with an offset of $27
	/// instead of 2 * $1C, which makes the launch of a flipped board lower. This uses the fixed offset, so both
	/// boards launch the same.
	/// </summary>
	private int GetLaunchBoost(ICharacter character)
	{
		int distance = (character.Position.X - (Position.X - OriginalHalfWidth)) / 4;
		if (_flipX)
			distance = 2 * 0x1C - 1 - distance;
		return LaunchBoosts[MathX.Clamp(0, distance, LaunchBoosts.Length - 1)];
	}

	/// <summary>
	/// Gets the position of a character along the board, from the left end of the board (0) to its right end (242).
	/// </summary>
	private int GetPositionOnBoard(ICharacter character)
	{
		if (!_flipX)
			return 132 + MathX.Clamp(-132, character.Position.X - Position.X, 110);
		return 110 + MathX.Clamp(-110, character.Position.X - Position.X, 132);
	}

	/// <summary>Gets whether a position along the board is on the part that makes the board go down.</summary>
	private bool IsOnTriggerPart(int boardPosition)
	{
		return !_flipX ? boardPosition >= 32 : boardPosition < 210;
	}

	private List<ICharacter> GetStandingCharacters()
	{
		List<ICharacter> standingCharacters = new List<ICharacter>();
		foreach (Character character in Level.ObjectManager.Characters)
		{
			if (character.GroundVector == CollisionVectors[0] || character.GroundVector == CollisionVectors[1])
				standingCharacters.Add(character);
		}
		return standingCharacters;
	}

	/// <summary>
	/// Sets the collision shape of the board. The height is the height of the raised end above the base.
	/// </summary>
	private void SetCollision(int height = 64)
	{
		if (!_flipX)
		{
			Rectanglei bounds = Rectanglei.FromLTRB(-132, -32 - height, 110, -32);
			Vector2i bend = new Vector2i(42, bounds.Top);
			CollisionVectors[0].RelativeA = bounds.BottomLeft;
			CollisionVectors[0].RelativeB = bend;
			CollisionVectors[1].RelativeA = bend;
			CollisionVectors[1].RelativeB = bounds.TopRight;
			CollisionVectors[2].RelativeA = bounds.TopRight;
			CollisionVectors[2].RelativeB = bounds.BottomRight;
			CollisionVectors[3].RelativeA = bounds.BottomRight;
			CollisionVectors[3].RelativeB = bounds.BottomLeft;
		}
		else
		{
			Rectanglei bounds = Rectanglei.FromLTRB(-110, -32 - height, 132, -32);
			Vector2i bend = new Vector2i(42, bounds.Top);
			CollisionVectors[0].RelativeA = bend;
			CollisionVectors[0].RelativeB = bounds.BottomRight;
			CollisionVectors[1].RelativeA = bounds.TopLeft;
			CollisionVectors[1].RelativeB = bend;
			CollisionVectors[2].RelativeA = bounds.BottomLeft;
			CollisionVectors[2].RelativeB = bounds.TopLeft;
			CollisionVectors[3].RelativeA = bounds.BottomRight;
			CollisionVectors[3].RelativeB = bounds.BottomLeft;
		}
		RegisterCollisionUpdate();
	}

	protected override void OnAnimate()
	{
		_animation.Index = _state != StateIdle ? (_state >= 4 ? (_state >= 6 ? AnimationBounce : AnimationDown) : AnimationStep) : AnimationNormal;
		_animation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, (Vector2)new Vector2i(0, -78), _flipX);
	}
}
