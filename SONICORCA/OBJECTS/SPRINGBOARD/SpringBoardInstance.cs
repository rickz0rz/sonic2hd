// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPRINGBOARD.SpringBoardInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
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

	private const double LaunchVelocity = -32.0;

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
					// This velocity is never 0, so every standing character is launched. The launch velocity is
					// always LaunchVelocity: the value of this lerp is not used.
					int boardPosition = GetPositionOnBoard(standingCharacter);
					double velocityY = !_flipX
						? MathX.Lerp(-16.0, -32.0, (boardPosition - 32) / 210.0)
						: MathX.Lerp(-16.0, -32.0, (210 - boardPosition) / 210.0);
					if (velocityY != 0.0)
					{
						standingCharacter.LeaveGround();
						standingCharacter.Velocity = new Vector2(standingCharacter.Velocity.X, LaunchVelocity);
						standingCharacter.TumbleAngle = 0.0;
						standingCharacter.TumbleTurns = 1;
					}
				}
				_state = StateIdle;
				break;
		}
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
