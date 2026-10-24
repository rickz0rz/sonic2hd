using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CRAWL;

/// <summary>
/// Crawl, as in Sonic 2 (ObjC8). It walks at $20 for $200 frames, waits 60 frames, and turns. When a character comes
/// within $40 pixels, it stops and holds up its shield. A character in a ball bounces off the shield at speed $700
/// when it is in the air or in front of Crawl. A character in a ball behind Crawl destroys it. Other characters get
/// hurt.
/// </summary>
public class CrawlInstance : Badnik
{
	private const int Scale = 4;

	/// <summary>$20 (1/8 pixel each frame) at four times the resolution.</summary>
	private const double WalkSpeed = 0.5;

	private const int WalkTicks = 0x200;

	private const int WaitTicks = 0x3C;

	/// <summary>The distance in original pixels at which Crawl sees a character.</summary>
	private const int AlertDistance = 0x40;

	/// <summary>$700 at four times the resolution.</summary>
	private const double BounceSpeed = 28.0;

	/// <summary>The original collision size $17: 8 pixels from the centre.</summary>
	private const int TouchRadius = 32;

	/// <summary>The walk animation: frames 0 and 1, each for 20 frames.</summary>
	private const int WalkFrameTicks = 0x14;

	private const int ShieldFrame = 0;

	private const int GuardFrame = 1;

	private const int BounceGroundFrame = 2;

	private const int BounceAirFrame = 3;

	private AnimationInstance _animation;

	private bool _facingRight;

	private bool _waiting;

	private int _ticks = WalkTicks;

	private bool _alert;

	private int _frame;

	private int _animationTicks;

	/// <summary>Crawl starts to walk to the right (the flip of the original).</summary>
	[StateVariable]
	private bool FacingRight
	{
		get => _facingRight;
		set => _facingRight = value;
	}

	public CrawlInstance() => DesignBounds = new Rectanglei(-96, -96, 192, 192);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CrawlType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
	}

	protected override void OnUpdate()
	{
		bool near = false;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (Math.Abs(character.Position.X - Position.X) < AlertDistance * Scale && Math.Abs(character.Position.Y - Position.Y) < AlertDistance * Scale)
			{
				near = true;
			}
		}
		if (near)
		{
			if (!_alert)
			{
				_alert = true;
				_frame = ShieldFrame;
			}
			return;
		}
		_alert = false;
		if (_waiting)
		{
			if (--_ticks < 0)
			{
				_waiting = false;
				_ticks = WalkTicks;
				_facingRight = !_facingRight;
			}
			return;
		}
		if (--_ticks == 0)
		{
			_waiting = true;
			_ticks = WaitTicks - 1;
			return;
		}
		PositionPrecise += new Vector2(_facingRight ? WalkSpeed : -WalkSpeed, 0.0);
		if (++_animationTicks >= WalkFrameTicks)
		{
			_animationTicks = 0;
			_frame ^= 1;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (Character)e.ActiveObject;
		bool inFront = _facingRight ? character.Position.X >= Position.X : character.Position.X < Position.X;
		if (character.IsSpinball && (character.IsAirborne || inFront))
		{
			Bounce(character);
			return;
		}
		if (!character.IsDeadly)
		{
			_frame = GuardFrame;
		}
		base.OnCollision(e);
	}

	/// <summary>Bounces a character away from the centre, as the round bumper does.</summary>
	private void Bounce(Character character)
	{
		_frame = character.IsAirborne ? BounceAirFrame : BounceGroundFrame;
		double angle = Math.Atan2(character.Position.Y - Position.Y, character.Position.X - Position.X);
		angle += (Level.Ticks & 3) * Math.PI / 128.0;
		character.LeaveGround();
		character.IsAirborne = true;
		character.IsJumping = false;
		character.IsRollJumping = false;
		character.IsPushing = false;
		character.Velocity = new Vector2(Math.Cos(angle) * BounceSpeed, Math.Sin(angle) * BounceSpeed);
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CrawlType.SoundResourceKey));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
	}
}
