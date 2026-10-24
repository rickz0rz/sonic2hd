using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.FLASHER;

/// <summary>
/// Flasher, as in Sonic 2 (ObjA3). The firefly waits $41 frames. Then it flies for $80 frames, flickers for 31
/// frames, glows for $80 frames and fades for 20 frames, and flies again. While it flickers, glows and fades, it
/// cannot be destroyed and it hurts a player that touches it. It starts to fly at (-$100, $40). Each frame $2 is
/// added to the x speed. At fixed counts of flight frames, the sign of this change and the sign of the y speed
/// change, so the firefly follows the same path each time. After $8000 flight frames it goes away.
/// </summary>
public class FlasherInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int WaitTicks = 0x40;

	private const int FlyTicks = 0x80;

	private const int GlowTicks = 0x80;

	/// <summary>Touch size 6: $10 pixels to each side.</summary>
	private const int TouchSize = 0x10 * Scale;

	/// <summary>The flight frame counts at which the path turns (word_38810). The last value is never reached.</summary>
	private static readonly int[] TurnCounts = { 0x100, 0x1A0, 0x208, 0x285, 0x300, 0x340, 0x390, 0x440, 0xF000 };

	/// <summary>For each turn: change the sign of the x acceleration, change the sign of the y speed (byte_38820).</summary>
	private static readonly bool[,] Turns =
	{
		{ true, false }, { true, true }, { false, true }, { true, true }, { false, true },
		{ false, true }, { true, false }, { false, true }, { false, true }
	};

	/// <summary>The frames while it flickers, one each frame (Ani_objA3_a).</summary>
	private static readonly int[] FlickerFrames = { 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 2, 3, 4 };

	/// <summary>The frames while it glows, one each frame (Ani_objA3_b).</summary>
	private static readonly int[] GlowFrames = { 2, 0, 3, 0, 4, 0, 3, 0 };

	/// <summary>The frames while it fades, 4 frames each (Ani_objA3_c).</summary>
	private static readonly int[] FadeFrames = { 4, 3, 2, 1, 0 };

	private const int FadeFrameTicks = 4;

	private AnimationInstance _animation;

	private State _state;

	private int _timer = WaitTicks;

	private int _ticks;

	private int _frame;

	private bool _facingRight;

	/// <summary>The speed and the x acceleration, in 1/256 of an original pixel each frame.</summary>
	private int _velocityX;

	private int _velocityY;

	private int _accelerationX;

	/// <summary>The number of flight frames, and the next turn.</summary>
	private int _flightCount;

	private int _turn;

	private enum State
	{
		Wait,
		Fly,
		Flicker,
		Glow,
		Fade,
		Restart
	}

	public FlasherInstance() => DesignBounds = new Rectanglei(-TouchSize, -TouchSize, TouchSize * 2, TouchSize * 2);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(FlasherType.AnimationGroupResourceKey));
		CollisionRectangles = new[] { new CollisionRectangle(this, 0, -TouchSize, -TouchSize, TouchSize * 2, TouchSize * 2) };
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Wait:
				if (--_timer < 0)
				{
					_state = State.Fly;
					_velocityX = -0x100;
					_velocityY = 0x40;
					_accelerationX = 2;
					_flightCount = 0;
					_timer = FlyTicks;
				}
				break;
			case State.Fly:
				Fly();
				break;
			case State.Flicker:
				if (_ticks < FlickerFrames.Length)
				{
					_frame = FlickerFrames[_ticks++];
					break;
				}
				_state = State.Glow;
				_frame = 3;
				_ticks = 0;
				_timer = GlowTicks;
				break;
			case State.Glow:
				if (--_timer < 0)
				{
					_state = State.Fade;
					_frame = 0;
					_ticks = 0;
					break;
				}
				_frame = GlowFrames[_ticks++ % GlowFrames.Length];
				break;
			case State.Fade:
				if (_ticks < FadeFrames.Length * FadeFrameTicks)
				{
					_frame = FadeFrames[_ticks++ / FadeFrameTicks];
					break;
				}
				_state = State.Restart;
				break;
			case State.Restart:
				_state = State.Fly;
				_timer = FlyTicks;
				_frame = 0;
				CanBeDestoryed = true;
				break;
		}
	}

	private void Fly()
	{
		if (--_timer < 0)
		{
			// It lights up and hurts from now on
			_state = State.Flicker;
			_ticks = 0;
			CanBeDestoryed = false;
			return;
		}
		if (_flightCount >= 0x8000)
		{
			FinishForever();
			return;
		}
		_facingRight = _velocityX >= 0;
		_flightCount++;
		if (_flightCount >= TurnCounts[_turn])
		{
			_turn++;
			if (Turns[_turn, 0])
			{
				_accelerationX = -_accelerationX;
			}
			if (Turns[_turn, 1])
			{
				_velocityY = -_velocityY;
			}
		}
		_velocityX += _accelerationX;
		PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The art faces left
		_animation.Index = _frame;
		Vector2 mirror = _facingRight ? new Vector2(-2.0 * _animation.CurrentFrame.Offset.X, 0.0) : new Vector2();
		renderer.GetObjectRenderer().Render(_animation, mirror, _facingRight);
	}
}
