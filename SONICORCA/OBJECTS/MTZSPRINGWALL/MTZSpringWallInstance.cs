using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZSPRINGWALL;

/// <summary>
/// An invisible spring wall, as in Sonic 2 (Obj66 in Metropolis Zone). The wall is solid, $13 pixels to each side and
/// $40 pixels up and down ($80 when subtype bits 4-6 are not 0). A character in the air that touches its right side
/// (its left side when flipped) bounces away at $800 to the right (to the left when flipped) and $800 up. The
/// character can not change its speed for $F frames.
/// Subtype bit 7 clears the vertical speed, bit 0 makes the character somersault (three times, or once with bit 1),
/// and bits 2-3 set the collision path (4: path 1, 8: path 2).
/// </summary>
public class MTZSpringWallInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0x13;

	private const int BounceSideId = 1;

	private const double BounceSpeed = 0x800 * Scale / 256.0;

	/// <summary>The character can not change its speed for $F frames (move_lock).</summary>
	private const int MoveLockTicks = 0xF;

	private int _subtype;

	private bool _flipX;

	private int _halfHeight;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The wall bounces from its left side, to the left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MTZSpringWallInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, -0x80 * Scale, 0x40 * Scale, 0x100 * Scale);

	/// <summary>The direction of the bounce: 1 for right, -1 for left.</summary>
	private int Direction => _flipX ? -1 : 1;

	protected override void OnStart()
	{
		_halfHeight = ((_subtype >> 4) & 7) == 0 ? 0x40 : 0x80;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -_halfHeight * Scale, HalfWidth * 2 * Scale, _halfHeight * 2 * Scale));
		// The side that bounces: the right side (vector 2), or the left side (vector 0) when flipped
		CollisionVector side = CollisionVectors[_flipX ? 0 : 2];
		side.Id = BounceSideId;
		side.Flags |= CollisionFlags.Solid;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != BounceSideId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		if (!character.IsAirborne || character.IsHurt || character.IsDying || character.IsDead || character.IsDebug)
		{
			return;
		}
		// The character must be on the side that bounces
		if (Math.Sign(character.Position.X - Position.X) != Direction)
		{
			return;
		}
		Bounce(character);
		e.MaintainVelocity = true;
	}

	/// <summary>Sends the character away (loc_2704C).</summary>
	private void Bounce(ICharacter character)
	{
		int direction = Direction;
		double speedX = BounceSpeed * direction;
		character.Velocity = new Vector2(speedX, (_subtype & 0x80) != 0 ? 0.0 : -BounceSpeed);
		character.GroundVelocity = speedX;
		character.Facing = direction;
		character.SlopeLockTicks = Math.Max(MoveLockTicks, character.SlopeLockTicks);
		character.IsPushing = false;
		character.IsRollJumping = false;
		if ((_subtype & 1) != 0)
		{
			// flips_remaining 3 (or 1 with bit 1): four half turns, or two
			character.TumbleAngle = 0.0;
			character.TumbleTurns = (_subtype & 2) != 0 ? 2 : 4;
		}
		switch (_subtype & 0xC)
		{
			case 4:
				character.Path = 0;
				break;
			case 8:
				character.Path = 1;
				break;
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MTZSpringWallType.SoundResourceKey));
	}

	/// <summary>The wall is invisible. The editor and the collision view show its box.</summary>
	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			return;
		}
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		renderer2d.RenderQuad(Colours.Yellow, new Rectangle(-HalfWidth * Scale * scale.X, -_halfHeight * Scale * scale.Y, HalfWidth * 2 * Scale * scale.X, _halfHeight * 2 * Scale * scale.Y));
	}
}
