using System;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZMAPBUMPER;

/// <summary>
/// A bumper in the landscape, as in Sonic 2 (Check_CNZ_bumpers). It bounces a character that touches it at speed
/// $A00: straight out from its flat sides, or reflected off its slanted side. The rules use original pixels and copy
/// the original, including two branches that set the wrong velocity (kinds 4 and 5).
/// </summary>
public class CNZMapBumperInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>$A00 at four times the resolution.</summary>
	private const double Speed = 40.0;

	/// <summary>The half width and the half height of each kind, in original pixels.</summary>
	private static readonly int[,] HalfSizes = { { 0x20, 0x20 }, { 0x20, 0x20 }, { 0x40, 8 }, { 0x40, 8 }, { 8, 0x40 }, { 8, 0x40 } };

	private int _kind;

	/// <summary>
	/// The kind of bumper (the original id / 2): 0 and 1 are the two triangles, 2 and 3 the horizontal bars that bounce
	/// up and down, 4 and 5 the vertical bars that bounce to the sides.
	/// </summary>
	[StateVariable]
	private int Kind
	{
		get => _kind;
		set => _kind = value;
	}

	protected override void OnStart()
	{
		_kind = Math.Clamp(_kind, 0, HalfSizes.GetLength(0) - 1);
		DesignBounds = new Rectanglei(-HalfSizes[_kind, 0] * Scale, -HalfSizes[_kind, 1] * Scale, HalfSizes[_kind, 0] * 2 * Scale, HalfSizes[_kind, 1] * 2 * Scale);
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (!character.IsDebug && !character.IsDying && !character.IsObjectControlled && Touches(character))
			{
				Bounce(character);
			}
		}
	}

	/// <summary>The touch test of the original: a box 18 pixels wide around the character against the box of the bumper.</summary>
	private bool Touches(ICharacter character)
	{
		int bx = Position.X / Scale;
		int by = Position.Y / Scale;
		// y_radius - 3, from the collision box of the character ($13 standing and $E rolling for the original Sonic)
		int halfHeight = character.CollisionRadius.Y / Scale - 3;
		int left = character.Position.X / Scale - 9;
		int top = character.Position.Y / Scale - halfHeight;
		int w = HalfSizes[_kind, 0];
		int h = HalfSizes[_kind, 1];
		return bx - w <= left + 0x12 && bx + w >= left && by - h <= top + halfHeight * 2 && by + h >= top;
	}

	private void Bounce(ICharacter character)
	{
		int px = character.Position.X / Scale;
		int py = character.Position.Y / Scale;
		int bx = Position.X / Scale;
		int by = Position.Y / Scale;
		Vector2 velocity = character.Velocity;
		switch (_kind)
		{
			case 0:
			case 1:
			{
				// A triangle: straight out from its flat sides, else off its slant
				bool mirrored = _kind == 1;
				if (py - by >= 0x20)
				{
					velocity.Y = Speed;
					break;
				}
				int side = mirrored ? bx - px : px - bx;
				if (side >= 0x20)
				{
					velocity.X = mirrored ? -Speed : Speed;
					break;
				}
				int slant = Math.Min(mirrored ? px - bx : bx - px, 0x20) + by - 8;
				if (slant - (py + 0x0E) >= 0)
				{
					return;
				}
				velocity = Reflect(velocity, mirrored ? 0x60 : 0x20);
				break;
			}
			case 2:
				if (py - by >= 8)
				{
					velocity.Y = Speed;
				}
				else if (!BounceToSide(ref velocity, bx - px, px - bx))
				{
					velocity = Reflect(velocity, px - bx < 0 ? 0x38 : 0x48);
				}
				break;
			case 3:
				if (by - py >= 8)
				{
					velocity.Y = -Speed;
				}
				else if (!BounceToSide(ref velocity, bx - px, px - bx))
				{
					velocity = Reflect(velocity, px - bx < 0 ? 0xC8 : 0xB8);
				}
				break;
			case 4:
				if (px - bx >= 8)
				{
					velocity.X = Speed;
				}
				else if (!BounceUpOrDown(ref velocity, by - py, py - by))
				{
					velocity = Reflect(velocity, py - by < 0 ? 8 : 0xF8);
				}
				break;
			default:
				// The original sets a speed to the right here too, not to the left
				if (bx - px >= 8)
				{
					velocity.X = Speed;
				}
				else if (!BounceUpOrDown(ref velocity, by - py, py - by))
				{
					velocity = Reflect(velocity, py - by < 0 ? 0x78 : 0x88);
				}
				break;
		}
		character.LeaveGround();
		character.IsAirborne = true;
		if (character is SonicOrca.Core.Objects.Base.Character body)
		{
			body.IsJumping = false;
			body.IsRollJumping = false;
			body.IsPushing = false;
		}
		character.Velocity = velocity;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZMapBumperType.SoundResourceKey));
	}

	/// <summary>A horizontal bar bounces a character beyond its ends to the side.</summary>
	private static bool BounceToSide(ref Vector2 velocity, int leftOfBar, int rightOfBar)
	{
		if (leftOfBar >= 0x40)
		{
			velocity.X = -Speed;
			return true;
		}
		if (rightOfBar >= 0x40)
		{
			velocity.X = Speed;
			return true;
		}
		return false;
	}

	/// <summary>A vertical bar bounces a character beyond its ends up, and (as in the original) to the right below it.</summary>
	private static bool BounceUpOrDown(ref Vector2 velocity, int aboveBar, int belowBar)
	{
		if (aboveBar >= 0x40)
		{
			velocity.Y = -Speed;
			return true;
		}
		if (belowBar >= 0x40)
		{
			velocity.X = Speed;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Reflects the direction of the character in an axis and sends it the opposite way. A direction that is nearly
	/// along the axis goes straight along it. This copies the word arithmetic and the byte compare of the original.
	/// </summary>
	private static Vector2 Reflect(Vector2 velocity, int axis)
	{
		int angle = velocity.X == 0.0 && velocity.Y == 0.0 ? 0x40 : (int)Math.Round(Math.Atan2(velocity.Y, velocity.X) * 128.0 / Math.PI) & 0xFF;
		int difference = angle - axis;
		int result = axis - difference;
		if ((Math.Abs(difference) & 0xFF) >= 0x38)
		{
			result = axis;
		}
		double radians = (result & 0xFF) * Math.PI / 128.0;
		return new Vector2(-Math.Cos(radians) * Speed, -Math.Sin(radians) * Speed);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			return;
		}
		Rectanglei bounds = DesignBounds;
		renderer.Get2dRenderer().RenderRectangle(Colours.White, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), 1.0);
	}
}
