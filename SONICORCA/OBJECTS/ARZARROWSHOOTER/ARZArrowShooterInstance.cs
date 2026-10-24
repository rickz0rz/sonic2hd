using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZARROWSHOOTER;

/// <summary>
/// An arrow shooter, as in Sonic 2 (Obj22). While a player is within $40 pixels on the x axis, its light flashes.
/// When the player leaves that range, it fires an arrow at $400. The arrow hurts a character that it touches, and
/// goes when its back end meets the landscape.
/// </summary>
public class ARZArrowShooterInstance : ActiveObject
{
	private const int Scale = 4;

	private const int TriggerDistance = 0x40 * Scale;

	/// <summary>$400 at four times the resolution.</summary>
	private const double ArrowSpeed = 16.0;

	private const int ArrowHalfWidth = 8 * Scale;

	private const int ArrowHalfHeight = 4 * Scale;

	/// <summary>The arrow goes when the point 8 pixels behind its centre meets the landscape.</summary>
	private const int ArrowTail = 8 * Scale;

	private const int ArrowLifeTicks = 600;

	private const int ArrowFrame = 0;

	private const int IdleFrame = 1;

	private const int LightFrame = 2;

	/// <summary>The fire animation: frame 3, frame 4, the arrow leaves, frame 4, frame 3, frame 1, 8 frames each.</summary>
	private const int FireTick = 17;

	private const int FireEndTick = 42;

	private readonly List<Arrow> _arrows = new List<Arrow>();

	private AnimationInstance _animation;

	private bool _flipX;

	private int _mode;

	private int _modeTicks;

	private sealed class Arrow
	{
		public Vector2 Position;

		public int Ticks;
	}

	/// <summary>The shooter fires to the left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public ARZArrowShooterInstance() => DesignBounds = new Rectanglei(-64, -32, 128, 64);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZArrowShooterType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[0];
	}

	protected override void OnUpdate()
	{
		if (_mode != 2)
		{
			bool near = false;
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				near |= Math.Abs(character.Position.X - Position.X) < TriggerDistance;
			}
			int mode = near ? 1 : _mode != 0 ? 2 : 0;
			if (mode != _mode)
			{
				_mode = mode;
				_modeTicks = 0;
			}
		}
		_modeTicks++;
		if (_mode == 2)
		{
			if (_modeTicks == FireTick)
			{
				_arrows.Add(new Arrow { Position = (Vector2)Position });
			}
			if (_modeTicks >= FireEndTick)
			{
				_mode = 0;
				_modeTicks = 0;
			}
		}
		UpdateArrows();
	}

	private void UpdateArrows()
	{
		double velocity = _flipX ? -ArrowSpeed : ArrowSpeed;
		var rectangles = new List<CollisionRectangle>();
		for (int i = _arrows.Count - 1; i >= 0; i--)
		{
			Arrow arrow = _arrows[i];
			Vector2 tail = arrow.Position + new Vector2(_flipX ? ArrowTail : -ArrowTail, 0.0);
			arrow.Position += new Vector2(velocity, 0.0);
			Vector2 newTail = arrow.Position + new Vector2(_flipX ? ArrowTail : -ArrowTail, 0.0);
			if (++arrow.Ticks > ArrowLifeTicks || OriginalMath.HitsLandscape(Level, (Vector2i)tail, (Vector2i)newTail))
			{
				_arrows.RemoveAt(i);
			}
		}
		for (int i = 0; i < _arrows.Count; i++)
		{
			Vector2 offset = _arrows[i].Position - (Vector2)Position;
			rectangles.Add(new CollisionRectangle(this, i + 1, (int)offset.X - ArrowHalfWidth, (int)offset.Y - ArrowHalfHeight, ArrowHalfWidth * 2, ArrowHalfHeight * 2));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id > 0 && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			var character = (ICharacter)e.ActiveObject;
			character.Hurt(Math.Sign(character.Position.X - Position.X));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		int frame = IdleFrame;
		if (_mode == 1)
		{
			// The light flashes: frames 1 and 2, 4 frames each
			frame = (_modeTicks / 4) % 2 == 0 ? IdleFrame : LightFrame;
		}
		else if (_mode == 2)
		{
			frame = _modeTicks <= 8 ? 3 : _modeTicks <= 25 ? 4 : _modeTicks <= 33 ? 3 : IdleFrame;
		}
		_animation.Index = frame;
		objectRenderer.Render(_animation, _flipX);
		_animation.Index = ArrowFrame;
		foreach (Arrow arrow in _arrows)
		{
			objectRenderer.Render(_animation, arrow.Position - PositionPrecise, _flipX);
		}
	}
}
