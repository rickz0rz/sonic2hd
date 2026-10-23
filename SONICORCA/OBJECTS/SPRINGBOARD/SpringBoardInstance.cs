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

public class SpringBoardInstance : ActiveObject
{
	private const int AnimationNormal = 0;
	private const int AnimationStep = 1;
	private const int AnimationDown = 2;
	private const int AnimationBounce = 3;
	private AnimationInstance _animation;
	private bool _flipX;
	private int _state;

	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public SpringBoardInstance() => DesignBounds = new Rectanglei(-116, -32, 232, 64 /*0x40*/);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP")));
		CollisionVectors = new CollisionVector[4]
		{
	  new CollisionVector( this, new Vector2i(), new Vector2i(), flags: CollisionFlags.Conveyor | CollisionFlags.NoAngle | CollisionFlags.Solid),
	  new CollisionVector( this, new Vector2i(), new Vector2i(), flags: CollisionFlags.Conveyor | CollisionFlags.NoAngle | CollisionFlags.Solid),
	  new CollisionVector( this, new Vector2i(), new Vector2i()),
	  new CollisionVector( this, new Vector2i(), new Vector2i())
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
			case 0:
				bool flag1 = false;
				Vector2i position1;
				foreach (ICharacter standingCharacter in GetStandingCharacters())
				{
					if (!_flipX)
					{
						position1 = standingCharacter.Position;
						int x1 = position1.X;
						position1 = Position;
						int x2 = position1.X;
						if (132 + MathX.Clamp(-132, x1 - x2, 110) >= 32 /*0x20*/)
							flag1 = true;
					}
					else
					{
						position1 = standingCharacter.Position;
						int x3 = position1.X;
						position1 = Position;
						int x4 = position1.X;
						if (110 + MathX.Clamp(-110, x3 - x4, 132) < 210)
							flag1 = true;
					}
				}
				if (!flag1)
					break;
				SetCollision(32 /*0x20*/);
				++_state;
				break;
			case 1:
			case 2:
				++_state;
				break;
			case 3:
			case 4:
				SetCollision(16 /*0x10*/);
				++_state;
				break;
			case 5:
				SetCollision(32 /*0x20*/);
				++_state;
				break;
			case 6:
				SetCollision();
				++_state;
				Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPRING");
				break;
			case 7:
				foreach (ICharacter standingCharacter in GetStandingCharacters())
				{
					Vector2i position2;
					bool flag2;
					double num1;
					if (!_flipX)
					{
						position2 = standingCharacter.Position;
						int x5 = position2.X;
						position2 = Position;
						int x6 = position2.X;
						int num2 = 132 + MathX.Clamp(-132, x5 - x6, 110);
						if (num2 >= 32 /*0x20*/)
							flag2 = true;
						num1 = MathX.Lerp(-16.0, -32.0, (num2 - 32 /*0x20*/) / 210.0);
					}
					else
					{
						position2 = standingCharacter.Position;
						int x7 = position2.X;
						position2 = Position;
						int x8 = position2.X;
						int num3 = 110 + MathX.Clamp(-110, x7 - x8, 132);
						if (num3 < 210)
							flag2 = true;
						num1 = MathX.Lerp(-16.0, -32.0, (210 - num3) / 210.0);
					}
					if (num1 != 0.0)
					{
						standingCharacter.LeaveGround();
						standingCharacter.Velocity = new Vector2(standingCharacter.Velocity.X, -32.0);
						standingCharacter.TumbleAngle = 0.0;
						standingCharacter.TumbleTurns = 1;
					}
				}
				_state = 0;
				break;
		}
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

	private void SetCollision(int height = 64 /*0x40*/)
	{
		if (!_flipX)
		{
			Rectanglei rectanglei = Rectanglei.FromLTRB(-132, -32 - height, 110, -32);
			Vector2i vector2i = new Vector2i(42, rectanglei.Top);
			CollisionVectors[0].RelativeA = rectanglei.BottomLeft;
			CollisionVectors[0].RelativeB = vector2i;
			CollisionVectors[1].RelativeA = vector2i;
			CollisionVectors[1].RelativeB = rectanglei.TopRight;
			CollisionVectors[2].RelativeA = rectanglei.TopRight;
			CollisionVectors[2].RelativeB = rectanglei.BottomRight;
			CollisionVectors[3].RelativeA = rectanglei.BottomRight;
			CollisionVectors[3].RelativeB = rectanglei.BottomLeft;
		}
		else
		{
			Rectanglei rectanglei = Rectanglei.FromLTRB(-110, -32 - height, 132, -32);
			Vector2i vector2i = new Vector2i(42, rectanglei.Top);
			CollisionVectors[0].RelativeA = vector2i;
			CollisionVectors[0].RelativeB = rectanglei.BottomRight;
			CollisionVectors[1].RelativeA = rectanglei.TopLeft;
			CollisionVectors[1].RelativeB = vector2i;
			CollisionVectors[2].RelativeA = rectanglei.BottomLeft;
			CollisionVectors[2].RelativeB = rectanglei.TopLeft;
			CollisionVectors[3].RelativeA = rectanglei.BottomRight;
			CollisionVectors[3].RelativeB = rectanglei.BottomLeft;
		}
		RegisterCollisionUpdate();
	}

	protected override void OnAnimate()
	{
		_animation.Index = _state != 0 ? (_state >= 4 ? (_state >= 6 ? 3 : 2) : 1) : 0;
		_animation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, (Vector2)new Vector2i(0, -78), _flipX);
	}
}
