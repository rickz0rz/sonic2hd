using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZBUMPER;

/// <summary>
/// A round bumper. A character that touches it bounces away from its centre at speed $700, as in Sonic 2 (Obj44).
/// The first 10 bounces on each bumper give 10 points each.
/// </summary>
public class CNZBumperInstance : ActiveObject
{
	/// <summary>$700 (7 pixels each frame) at four times the resolution.</summary>
	private const double BounceSpeed = 28.0;

	/// <summary>The original collision size $17: 8 pixels from the centre.</summary>
	private const int TouchRadius = 32;

	private const int MaxScoringBounces = 10;

	private const int Points = 10;

	/// <summary>The bounce animation of the original: frame 1, 0, 1, each for 4 frames, then back to frame 0.</summary>
	private static readonly int[] BounceFrames = { 1, 0, 1 };

	private const int BounceFrameTicks = 4;

	private AnimationInstance _animation;

	private int _bounceTicks = -1;

	private int _scoringBounces;

	public CNZBumperInstance() => DesignBounds = new Rectanglei(-96, -96, 192, 192);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZBumperType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
	}

	protected override void OnUpdate()
	{
		if (_bounceTicks < 0)
		{
			return;
		}
		_bounceTicks++;
		if (_bounceTicks >= BounceFrames.Length * BounceFrameTicks)
		{
			_bounceTicks = -1;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (Character)e.ActiveObject;
		// Away from the centre, with a small change of 0 to 3 angle units (of 256) that depends on the frame
		double angle = Math.Atan2(character.Position.Y - Position.Y, character.Position.X - Position.X);
		angle += (Level.Ticks & 3) * Math.PI / 128.0;
		character.LeaveGround();
		character.IsAirborne = true;
		character.IsJumping = false;
		character.IsRollJumping = false;
		character.IsPushing = false;
		character.Velocity = new Vector2(Math.Cos(angle) * BounceSpeed, Math.Sin(angle) * BounceSpeed);
		_bounceTicks = 0;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZBumperType.SoundResourceKey));
		if (_scoringBounces < MaxScoringBounces)
		{
			_scoringBounces++;
			Level.Player.GainScore(Points);
			Level.CreateScoreObject(Points, Position);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _bounceTicks < 0 ? 0 : BounceFrames[_bounceTicks / BounceFrameTicks];
		renderer.GetObjectRenderer().Render(_animation);
	}
}
