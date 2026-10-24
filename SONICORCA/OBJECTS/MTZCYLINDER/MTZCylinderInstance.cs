using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZCYLINDER;

/// <summary>
/// The rotating mesh cylinder of Metropolis Zone, as in Sonic 2 (Obj06, Obj06_Cylinder). A character whose feet come
/// near the bottom of the cylinder ($38 to $48 pixels below its centre, and less than $C0 pixels to each side) rides
/// it. The character then goes around the cylinder at a fixed rate: its angle starts at 0 (the bottom) and adds 4 each
/// frame. Its feet are at $13 + cos(angle) * $28 pixels below the centre, and the character tumbles with the angle.
/// The character keeps its ground speed. It leaves the cylinder at the ends. When it jumps, its vertical speed is
/// halved near the bottom of the cylinder and cleared elsewhere.
/// </summary>
public class MTZCylinderInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = 0xC0;

	/// <summary>The mount line is 60 pixels below the centre. The feet (plus 4) must be up to $10 pixels below it.</summary>
	private const int MountLine = 60;

	private const int MountRange = 0x10;

	/// <summary>The feet are $13 pixels below the centre, plus the cosine of the angle times $28.</summary>
	private const int FeetOffset = 0x13;

	private const int Radius = 0x2800;

	private const int AngleStep = 4;

	/// <summary>The angle of each character on the cylinder (MTZCylinder_Angle_Sonic and MTZCylinder_Angle_Tails).</summary>
	private readonly Dictionary<ICharacter, int> _angles = new Dictionary<ICharacter, int>();

	public MTZCylinderInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, -0x40 * Scale, HalfWidth * 2 * Scale, 0x90 * Scale);

	protected override void OnStop()
	{
		foreach (ICharacter character in _angles.Keys.ToArray())
		{
			if (character.ObjectLink == this)
			{
				Release(character);
			}
		}
		_angles.Clear();
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (_angles.ContainsKey(character) && character.ObjectLink == this)
			{
				UpdateRiding(character);
			}
			else
			{
				_angles.Remove(character);
				CheckMount(character);
			}
		}
	}

	private void CheckMount(ICharacter character)
	{
		if (character.IsDebug || character.IsDying || character.IsDead || character.IsObjectControlled || character.ObjectLink != null)
		{
			return;
		}
		int dx = (int)Math.Floor((character.PositionPrecise.X - PositionPrecise.X) / Scale);
		if (dx < -HalfWidth || dx >= HalfWidth)
		{
			return;
		}
		// d0 = y + 60 - (feet + 4), from -$10 to 0
		double feet = (character.PositionPrecise.Y + character.CollisionRadius.Y) / Scale;
		double distance = PositionPrecise.Y / Scale + MountLine - (feet + 4);
		if (distance > 0 || distance < -MountRange)
		{
			return;
		}
		// The original sets the ground speed to the horizontal speed (RideObject_SetRide)
		double groundVelocity = character.IsAirborne ? character.Velocity.X : character.GroundVelocity;
		if (character.IsAirborne)
		{
			character.IsSpinball = false;
		}
		character.LeaveGround();
		character.ObjectLink = this;
		character.CheckLandscapeCollision = false;
		character.IsAirborne = false;
		character.Mode = CollisionMode.Top;
		character.ShowAngle = 0.0;
		character.GroundVelocity = groundVelocity != 0.0 ? groundVelocity : Scale / 256.0;
		character.Velocity = new Vector2(character.Velocity.X, 0.0);
		_angles[character] = 0;
		Place(character, 0);
	}

	private void UpdateRiding(ICharacter character)
	{
		int angle = _angles[character];
		if (character.IsAirborne || character.Mode == CollisionMode.Air || character.IsHurt || character.IsDebug || character.IsDying)
		{
			// A jump: the vertical speed is halved near the bottom and cleared elsewhere
			Vector2 velocity = character.Velocity;
			velocity.Y = ((angle + 0x20) & 0xFF) < 0x40 ? velocity.Y / 2.0 : 0.0;
			character.Velocity = velocity;
			Release(character);
			return;
		}
		int dx = (int)Math.Floor((character.PositionPrecise.X - PositionPrecise.X) / Scale) + HalfWidth;
		if (dx < 0 || dx >= HalfWidth * 2)
		{
			character.Velocity = new Vector2(character.GroundVelocity, 0.0);
			Release(character);
			return;
		}
		character.CheckLandscapeCollision = false;
		character.IsAirborne = false;
		character.Mode = CollisionMode.Top;
		if (character.GroundVelocity == 0.0)
		{
			character.GroundVelocity = Scale / 256.0;
		}
		Place(character, angle);
		_angles[character] = (angle + AngleStep) & 0xFF;
	}

	/// <summary>Puts the feet of the character on the cylinder at the angle, and shows the angle as a tumble.</summary>
	private void Place(ICharacter character, int angle)
	{
		int offset = (int)Math.Floor(OriginalMath.Cos(angle) * Radius / 65536.0);
		double y = PositionPrecise.Y + (FeetOffset + offset) * Scale - character.CollisionRadius.Y;
		character.PositionPrecise = new Vector2(character.PositionPrecise.X, y);
		character.Velocity = new Vector2(character.Velocity.X, 0.0);
		character.TumbleAngle = angle < 0x80 ? angle / 128.0 : (angle - 0x100) / 128.0;
	}

	private void Release(ICharacter character)
	{
		_angles.Remove(character);
		character.ObjectLink = null;
		character.CheckLandscapeCollision = true;
		character.IsAirborne = true;
		character.Mode = CollisionMode.Air;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!Level.LayerViewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing) || viewOptions.Shadows)
		{
			return;
		}
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		Rectangle destination = new Rectangle(
			-HalfWidth * Scale * scale.X,
			(FeetOffset - 0x28) * Scale * scale.Y,
			HalfWidth * 2 * Scale * scale.X,
			0x50 * Scale * scale.Y);
		renderer2d.RenderRectangle(Colours.White, destination, 2.0);
	}
}
