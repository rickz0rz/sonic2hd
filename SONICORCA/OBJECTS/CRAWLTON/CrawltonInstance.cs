using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CRAWLTON;

/// <summary>
/// Crawlton, as in Sonic 2 (Obj9E). The snake waits until the nearer player is less than $80 pixels away on both
/// axes. It turns to that player and waits 17 frames. Then the head lunges toward the position of the player for
/// $1B frames, at 1/32 of the distance each frame (in whole pixels). It waits $20 frames, goes back the same way, and
/// starts again. The 7 body segments stay behind the head: segment N moves only while fewer than $18 - 4N frames
/// of the lunge are left, so the last segment stays at the start position. Only the head can be hit.
/// </summary>
public class CrawltonInstance : Badnik
{
	private const int Scale = 4;

	private const int TriggerDistance = 0x80;

	private const int AimTicks = 0x10;

	private const int MoveTicks = 0x1C;

	private const int PauseTicks = 0x20;

	private const int Segments = 7;

	/// <summary>Touch size $B: 8 pixels to each side.</summary>
	private const int TouchSize = 8 * Scale;

	private const int HeadFrame = 0;

	private const int SegmentFrame = 2;

	private readonly Vector2i[] _segments = new Vector2i[Segments];

	private AnimationInstance _animation;

	private Vector2 _home;

	/// <summary>The offset of the head from the start position, in original pixels.</summary>
	private Vector2i _head;

	/// <summary>The speed of the head, in whole original pixels each frame.</summary>
	private Vector2i _velocity;

	private State _state;

	/// <summary>The state after the current lunge: Pause after the lunge out, Wait after the lunge back.</summary>
	private State _nextState;

	private int _timer;

	private bool _facingRight;

	private enum State
	{
		Wait,
		Aim,
		Move,
		Pause
	}

	public CrawltonInstance() => DesignBounds = new Rectanglei(-0x80 * Scale, -0x80 * Scale, 0x100 * Scale, 0x100 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CrawltonType.AnimationGroupResourceKey));
		_home = PositionPrecise;
		CollisionRectangles = new[] { new CollisionRectangle(this, 0, -TouchSize, -TouchSize, TouchSize * 2, TouchSize * 2) };
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Wait:
			{
				PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
				if (orientation.Player == null || Math.Abs(orientation.DeltaX) >= TriggerDistance || Math.Abs(orientation.DeltaY) >= TriggerDistance)
				{
					break;
				}
				_state = State.Aim;
				_timer = AimTicks;
				_facingRight = orientation.PlayerRight;
				// x_vel = (-dx << 3) & $FF00: whole pixels, rounded down
				_velocity = new Vector2i((-orientation.DeltaX * 8) >> 8, (-orientation.DeltaY * 8) >> 8);
				break;
			}
			case State.Aim:
				if (--_timer < 0)
				{
					StartMove(State.Pause);
				}
				break;
			case State.Move:
				if (--_timer == 0)
				{
					_state = _nextState;
					_timer = PauseTicks;
					break;
				}
				_head += _velocity;
				for (int i = 0; i < Segments; i++)
				{
					if (_timer < 0x18 - i * 4)
					{
						_segments[i] += _velocity;
					}
				}
				break;
			case State.Pause:
				if (--_timer == 0)
				{
					_velocity = new Vector2i(-_velocity.X, -_velocity.Y);
					StartMove(State.Wait);
				}
				break;
		}
		PositionPrecise = _home + new Vector2(_head.X * Scale, _head.Y * Scale);
	}

	private void StartMove(State next)
	{
		_state = State.Move;
		_nextState = next;
		_timer = MoveTicks;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The body is behind the head. The art faces left.
		_animation.Index = SegmentFrame;
		for (int i = Segments - 1; i >= 0; i--)
		{
			Vector2 segment = new Vector2((_segments[i].X - _head.X) * Scale, (_segments[i].Y - _head.Y) * Scale);
			objectRenderer.Render(_animation, segment + Mirror(), _facingRight);
		}
		_animation.Index = HeadFrame;
		objectRenderer.Render(_animation, Mirror(), _facingRight);
	}

	/// <summary>The extra offset that mirrors the offset of the current frame when the art faces right.</summary>
	private Vector2 Mirror() => _facingRight ? new Vector2(-2.0 * _animation.CurrentFrame.Offset.X, 0.0) : new Vector2();
}
