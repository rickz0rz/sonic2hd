using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZBONUSBLOCK;

/// <summary>
/// A bonus block, as in Sonic 2 (ObjD8). It bounces a character at speed $700: up or down, to the left or the right,
/// or off its diagonal side. Each hit changes its colour and gives 10 points. The third hit destroys it. When the
/// players destroy three blocks of one group, the last one gives 500 points instead.
/// </summary>
public class CNZBonusBlockInstance : ActiveObject
{
	/// <summary>$700 at four times the resolution.</summary>
	private const double BounceSpeed = 28.0;

	/// <summary>The original collision size $17: 8 pixels from the centre.</summary>
	private const int TouchRadius = 32;

	private const int Hits = 3;

	private const int HitPoints = 10;

	private const int GroupPoints = 500;

	private const int BlocksForGroupPoints = 3;

	/// <summary>The frames after a hit during which the same character cannot hit the block again.</summary>
	private const int HitDelayTicks = 4;

	private const int BounceFrameTicks = 4;

	/// <summary>The mapping frames: 0 to 2 are the normal frames of the three kinds, 3 to 5 the squashed frames.</summary>
	private const int FramesPerColour = 6;

	private const int SquashedFrameOffset = 3;

	private AnimationInstance _animation;

	private BlockKind _kind;

	private int _group;

	private bool _flipX;

	private int _hits;

	private int _bounceTicks = -1;

	private bool _destroyed;

	private int _mainDelay;

	private int _sidekickDelay;

	private enum BlockKind
	{
		Vertical,
		Diagonal,
		Horizontal
	}

	[StateVariable]
	private BlockKind Kind
	{
		get => _kind;
		set => _kind = value;
	}

	/// <summary>The group of the block (subtype & $3F). Three destroyed blocks of a group give 500 points.</summary>
	[StateVariable]
	private int Group
	{
		get => _group;
		set => _group = value;
	}

	/// <summary>For a diagonal block, the side faces up and to the left instead of up and to the right.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public CNZBonusBlockInstance() => DesignBounds = new Rectanglei(-64, -64, 128, 128);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZBonusBlockType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
	}

	protected override void OnUpdate()
	{
		_mainDelay = Math.Max(0, _mainDelay - 1);
		_sidekickDelay = Math.Max(0, _sidekickDelay - 1);
		if (_bounceTicks >= 0 && ++_bounceTicks >= 3 * BounceFrameTicks)
		{
			_bounceTicks = -1;
			if (_destroyed)
			{
				FinishForever();
			}
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_destroyed || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (Character)e.ActiveObject;
		bool isMain = character == Level.Player.Protagonist;
		if ((isMain ? _mainDelay : _sidekickDelay) > 0)
		{
			return;
		}
		if (isMain)
		{
			_mainDelay = HitDelayTicks;
		}
		else
		{
			_sidekickDelay = HitDelayTicks;
		}
		character.Velocity = GetBounce(character);
		character.LeaveGround();
		character.IsAirborne = true;
		character.IsJumping = false;
		character.IsRollJumping = false;
		character.IsPushing = false;
		_bounceTicks = 0;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZBonusBlockType.SoundResourceKey));
		int points = HitPoints;
		if (++_hits >= Hits)
		{
			_destroyed = true;
			_hits = Hits - 1;
			int[] destroyed = ((CNZBonusBlockType)Type).DestroyedInGroup;
			int group = _group & (destroyed.Length - 1);
			if (++destroyed[group] >= BlocksForGroupPoints)
			{
				points = GroupPoints;
			}
		}
		Level.Player.GainScore(points);
		Level.CreateScoreObject(points, Position);
	}

	private Vector2 GetBounce(Character character)
	{
		Vector2 velocity = character.Velocity;
		switch (_kind)
		{
			case BlockKind.Vertical:
				velocity.Y = character.Position.Y <= Position.Y ? -BounceSpeed : BounceSpeed;
				return velocity;
			case BlockKind.Horizontal:
				velocity.X = character.Position.X <= Position.X ? -BounceSpeed : BounceSpeed;
				return velocity;
		}
		// A diagonal block reflects the direction of the character in its axis (angle $20 or $60), then sends the
		// character the opposite way. This copies the word arithmetic and the byte compares of the original.
		int side = _flipX ? 0x20 : 0x60;
		int angle = velocity.X == 0.0 && velocity.Y == 0.0 ? 0x40 : (int)Math.Round(Math.Atan2(velocity.Y, velocity.X) * 128.0 / Math.PI) & 0xFF;
		int difference = angle - side;
		int distance = Math.Abs(difference);
		int result = side - difference;
		if ((distance & 0xFF) >= 0x40)
		{
			if (((0x80 - distance) & 0xFF) >= 0x38)
			{
				result = side + 0x80;
			}
		}
		else if ((distance & 0xFF) >= 0x38)
		{
			result = side;
		}
		double radians = (result & 0xFF) * Math.PI / 128.0;
		return new Vector2(-Math.Cos(radians) * BounceSpeed, -Math.Sin(radians) * BounceSpeed);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The bounce animation of the original: squashed, normal, squashed. Each hit uses the next colour.
		int frame = (int)_kind;
		if (_bounceTicks >= 0 && _bounceTicks / BounceFrameTicks != 1)
		{
			frame += SquashedFrameOffset;
		}
		_animation.Index = _hits * FramesPerColour + frame;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
