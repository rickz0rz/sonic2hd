using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.EHZTWISTEDPATHWAY;

/// <summary>
/// The twisted pathway (corkscrew) from Emerald Hill Zone. A fast character that runs into it follows a cosine curve
/// and tumbles until the character leaves the other end or becomes too slow.
/// </summary>
public class EHZTwistedPathwayInstance : ActiveObject
{
	/// <summary>The minimum ground speed that a character must have to stay on the pathway.</summary>
	private const double MinimumGroundSpeed = 20.0;

	/// <summary>The distance past each end of the pathway at which a linked character is released.</summary>
	private const int ExitMargin = 44;

	/// <summary>The vertical distance from the bottom of the pathway in which a character can engage.</summary>
	private const int EntryMargin = 32;

	/// <summary>Tumble angles nearer to 0 than this value keep the previous tumble angle.</summary>
	private const double TumbleAngleDeadZone = 3.0 / 128.0;

	private Vector2i _radius = new Vector2i(832, 200);
	private double _previousTumbleAngle;

	public EHZTwistedPathwayInstance()
	{
		DesignBounds = new Rectanglei(-_radius.X, -_radius.Y, _radius.X * 2, _radius.Y * 2);
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
				DisengageCharacter(character);
		}
	}

	protected override void OnUpdate()
	{
		// The design bounds use a larger radius than the radius that the game logic uses.
		_radius.X = 768;
		_radius.Y = 192;
		foreach (ICharacter character in Level.ObjectManager.Characters)
			UpdateCharacter(character);
	}

	private void UpdateCharacter(ICharacter character)
	{
		if (character.ObjectLink == this)
		{
			character.CheckLandscapeCollision = false;
			character.IsAirborne = false;
			if (Math.Abs(character.GroundVelocity) < MinimumGroundSpeed)
			{
				DisengageCharacter(character);
			}
			else if (character.IsAirborne || character.IsDebug)
			{
				DisengageCharacter(character);
			}
			else if (character.Position.X + character.CollisionRadius.X >= Position.X - _radius.X - ExitMargin &&
				character.Position.X - character.CollisionRadius.X < Position.X + _radius.X + ExitMargin)
			{
				UpdateTumble(character);
			}
			else
			{
				DisengageCharacter(character);
			}
		}
		else
		{
			if (Math.Abs(character.GroundVelocity) < MinimumGroundSpeed || character.IsAirborne || character.IsDebug)
				return;
			if (character.Position.X + character.CollisionRadius.X < Position.X - _radius.X)
				return;
			if (character.Position.X - character.CollisionRadius.X >= Position.X + _radius.X)
				return;
			if (character.Position.Y + character.CollisionRadius.Y < Position.Y + _radius.Y - EntryMargin)
				return;
			if (character.Position.Y + character.CollisionRadius.Y >= Position.Y + _radius.Y + EntryMargin)
				return;
			EngageCharacter(character);
			UpdateTumble(character);
		}
	}

	private void UpdateTumble(ICharacter character)
	{
		// Progress goes from 0 at the left end to 1 at the right end of the pathway.
		double progress = (character.Position.X - (Position.X - _radius.X)) / (double)(_radius.X * 2);
		double offsetY = Math.Cos(progress * (2.0 * Math.PI)) * (_radius.Y - character.CollisionRadius.Y);
		character.PositionPrecise = new Vector2(character.Position.X, Position.Y + offsetY);
		character.TumbleAngle = progress < 0.5 ? progress * 2.0 : (1.0 - progress) * -2.0;
		if (character.TumbleAngle <= 0.0)
		{
			if (character.TumbleAngle + TumbleAngleDeadZone > 0.0)
				character.TumbleAngle = _previousTumbleAngle;
			else
				_previousTumbleAngle = character.TumbleAngle;
		}
		else if (character.TumbleAngle - TumbleAngleDeadZone < 0.0)
			character.TumbleAngle = _previousTumbleAngle;
		else
			_previousTumbleAngle = character.TumbleAngle;
	}

	private void EngageCharacter(ICharacter character)
	{
		if (character.ObjectLink != null)
		{
			// Continue the tumble from the previous pathway when two pathways connect.
			if (character.ObjectLink.Type.Name == "Twisted pathway")
				_previousTumbleAngle = (character.ObjectLink as EHZTwistedPathwayInstance)._previousTumbleAngle;
		}
		else
			_previousTumbleAngle = character.TumbleAngle;
		character.LeaveGround();
		character.ObjectLink = this;
		character.CheckLandscapeCollision = false;
		character.ShowAngle = 0.0;
		character.Velocity = new Vector2(character.Velocity.X, 0.0);
		character.Mode = CollisionMode.Top;
	}

	private void DisengageCharacter(ICharacter character)
	{
		_previousTumbleAngle = 0.0;
		character.ObjectLink = null;
		character.CheckLandscapeCollision = true;
		character.IsAirborne = true;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!Level.LayerViewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing) || viewOptions.Shadows)
			return;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		renderer2d.RenderLine(Colours.White, new Vector2(-_radius.X * scale.X, 0.0), new Vector2(_radius.X * scale.X, 0.0), 2.0);
		renderer2d.RenderLine(Colours.White, new Vector2(0.0, -_radius.Y * scale.Y), new Vector2(0.0, _radius.Y * scale.Y), 2.0);
		Rectangle destination = new Rectangle(
			-_radius.X * scale.X,
			-_radius.Y * scale.Y,
			_radius.X * 2 * scale.X,
			_radius.Y * 2 * scale.Y);
		renderer2d.RenderRectangle(Colours.White, destination, 2.0);
	}
}
