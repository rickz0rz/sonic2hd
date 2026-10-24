using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZFAN;

/// <summary>
/// A fan, as in Sonic 2 (Obj3F). It blows for 181 frames, then slows down and stops for 121 frames (subtype bit 1:
/// it always blows). While it blows, it moves the characters near it by up to 10 pixels each frame, less when they
/// are further away. A side fan (bit 7 clear) blows to the left ($A0 pixels in front, $50 behind, from $60 above to
/// $10 below), or to the right when it is flipped. Bit 0 makes it pull instead. An up fan (bit 7 set) lifts the
/// characters within $40 pixels on the x axis, from $60 above it to $30 below, and makes them tumble. Global
/// oscillator 5 moves the top of that area, so the characters float up and down.
/// </summary>
public class OOZFanInstance : ActiveObject
{
	private const int Scale = 4;

	private const int OnTicks = 0xB4;

	private const int OffTicks = 0x78;

	/// <summary>The fan slows down each time by $2A in 1/256 of a frame, until the delay between frames is 4.</summary>
	private const int SlowStep = 0x2A;

	private const int SlowLimit = 0x400;

	private const int Frames = 6;

	private const int Oscillator = 5;

	private const int TumbleTurns = 0x7F;

	private readonly HashSet<ICharacter> _tumbling = new HashSet<ICharacter>();

	private AnimationInstance _animation;

	private bool _vertical;

	private bool _reverse;

	private bool _alwaysOn;

	private bool _flipX;

	private int _timer;

	private bool _off;

	private int _slow;

	/// <summary>The delay before the next frame. It is a byte, as in the original.</summary>
	private sbyte _delay;

	private int _frame;

	/// <summary>The fan blows up (subtype bit 7).</summary>
	[StateVariable]
	private bool Vertical
	{
		get => _vertical;
		set => _vertical = value;
	}

	/// <summary>The fan pulls instead of blows, and turns the other way (subtype bit 0).</summary>
	[StateVariable]
	private bool Reverse
	{
		get => _reverse;
		set => _reverse = value;
	}

	/// <summary>The fan never stops (subtype bit 1).</summary>
	[StateVariable]
	private bool AlwaysOn
	{
		get => _alwaysOn;
		set => _alwaysOn = value;
	}

	/// <summary>The side fan blows to the right (the flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public OOZFanInstance() => DesignBounds = new Rectanglei(-0xA0 * Scale, -0x70 * Scale, 0x140 * Scale, 0xA0 * Scale);

	protected override void OnStart()
	{
		string key = _vertical ? OOZFanType.VerticalAnimationGroupResourceKey : OOZFanType.AnimationGroupResourceKey;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(key));
		_frame = FirstFrame;
	}

	/// <summary>Frames 0 to 5 turn one way, and frames 5 to 10 (the same frames in reverse order) the other way.</summary>
	private int FirstFrame => _reverse ? 5 : 0;

	protected override void OnUpdate()
	{
		UpdateTumbling();
		if (!_alwaysOn && --_timer < 0)
		{
			_slow = 0;
			_timer = OffTicks;
			_off = !_off;
			if (!_off)
			{
				_timer = OnTicks;
			}
		}
		if (_off)
		{
			// The fan slows down: the delay between frames grows until the fan stops
			if (--_delay >= 0 || _slow >= SlowLimit)
			{
				return;
			}
			_slow += SlowStep;
			_delay = (sbyte)(_slow >> 8);
			NextFrame();
			return;
		}
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.IsHurt || character.IsDying || character.IsDead || character.IsDebug || character.IsObjectControlled)
			{
				continue;
			}
			if (_vertical)
			{
				Lift(character);
			}
			else
			{
				Push(character);
			}
		}
		if (--_delay >= 0)
		{
			return;
		}
		_delay = 0;
		NextFrame();
	}

	private void NextFrame()
	{
		int index = (_frame - FirstFrame + 1) % Frames;
		_frame = FirstFrame + index;
	}

	/// <summary>Moves a character on the x axis. The integer steps copy the original, so the edges of the area match.</summary>
	private void Push(ICharacter character)
	{
		int d0 = character.Position.X / Scale - Position.X / Scale;
		if (!_flipX)
		{
			d0 = -d0;
		}
		d0 += 0x50;
		if ((ushort)d0 >= 0xF0)
		{
			return;
		}
		int d1 = character.Position.Y / Scale + 0x60 - Position.Y / Scale;
		if (d1 < 0 || d1 >= 0x70)
		{
			return;
		}
		// Now the distance in front of the fan. Behind the fan, the push grows twice as fast.
		d0 -= 0x50;
		if (d0 < 0)
		{
			d0 = ~d0 * 2;
		}
		d0 += 0x60;
		if (!_flipX)
		{
			d0 = -d0;
		}
		// The original negates only the low byte (neg.b), then divides the word by 16
		d0 = (d0 & ~0xFF) | (-d0 & 0xFF);
		int push = (short)d0 >> 4;
		if (_reverse)
		{
			push = -push;
		}
		character.PositionPrecise += new Vector2(push * Scale, 0.0);
	}

	/// <summary>Lifts a character, stops its fall, and makes it tumble.</summary>
	private void Lift(ICharacter character)
	{
		int dx = character.Position.X / Scale - Position.X / Scale + 0x40;
		if ((ushort)dx >= 0x80)
		{
			return;
		}
		int d1 = Oscillators.For(Level).GetHighByte(Level, Oscillator) + character.Position.Y / Scale + 0x60 - Position.Y / Scale;
		if (d1 < 0 || d1 >= 0x90)
		{
			return;
		}
		// Below the top of the fan, the lift gets smaller twice as fast
		d1 -= 0x60;
		if (d1 >= 0)
		{
			d1 = ~d1 * 2;
		}
		d1 += 0x60;
		int lift = -d1 >> 4;
		character.PositionPrecise += new Vector2(0.0, lift * Scale);
		if (!character.IsAirborne)
		{
			character.LeaveGround();
		}
		character.IsAirborne = true;
		character.Velocity = new Vector2(character.Velocity.X, 0.0);
		character.GroundVelocity = Scale / 256.0;
		if (!_tumbling.Contains(character) && character.TumbleAngle == 0.0)
		{
			// The original shows the walk animation for the tumble, so the character is not a spinball
			character.IsSpinball = false;
			character.TumbleTurns = TumbleTurns;
			_tumbling.Add(character);
		}
	}

	/// <summary>A character tumbles until it lands, as flips_remaining does in the original.</summary>
	private void UpdateTumbling()
	{
		_tumbling.RemoveWhere(character =>
		{
			if (character.IsAirborne && !character.IsHurt && !character.IsDying && !character.IsDead)
			{
				return false;
			}
			character.TumbleTurns = 0;
			return true;
		});
	}

	protected override void OnStop()
	{
		// Let the characters finish the turn that they are in
		foreach (ICharacter character in _tumbling)
		{
			if (character.TumbleTurns > 1)
			{
				character.TumbleTurns = 1;
			}
		}
		_tumbling.Clear();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
