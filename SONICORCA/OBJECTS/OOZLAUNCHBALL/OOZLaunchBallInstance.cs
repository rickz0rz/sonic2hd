using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZLAUNCHBALL;

/// <summary>
/// A launcher ball, as in Sonic 2 (Obj48). It takes a character that comes within $10 pixels of its centre and holds
/// it as a spinball. The ball turns one frame each 8 frames, from its open frame to the frame that points out, and
/// then fires the character at $1000: up, right, down or left by subtype + 1 (minus 2 when flipped). The ball moves
/// the character in a straight line through the landscape until another launcher takes it or it leaves the screen.
/// Subtype bit 7 lets the character go at once, with that velocity. The ball turns back to its open frame one frame
/// each 2 frames.
/// </summary>
public class OOZLaunchBallInstance : ActiveObject
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int LaunchSpeed = 0x1000;

	private const int CaptureRange = 0x10;

	private const int LastFrame = 7;

	private const int TurnTicks = 7;

	private const int TurnBackTicks = 1;

	private const int LaunchDelay = 3;

	private const int CooldownTicks = 7;

	/// <summary>Obj48_Properties: the flips of the art and the open frame, by (subtype and $F) + 4 when flipped.</summary>
	private static readonly (bool FlipX, bool FlipY, int OpenFrame)[] Properties =
	{
		(false, false, 0), (false, true, 7), (true, true, 0), (true, false, 7),
		(true, false, 0), (false, false, 7), (false, true, 0), (true, true, 7)
	};

	/// <summary>The directions of word_25464: up, right, down and left.</summary>
	private static readonly Vector2[] Directions = { new Vector2(0.0, -1.0), new Vector2(1.0, 0.0), new Vector2(0.0, 1.0), new Vector2(-1.0, 0.0) };

	private readonly Dictionary<ICharacter, Rider> _riders = new Dictionary<ICharacter, Rider>();

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private bool _artFlipX;

	private bool _artFlipY;

	private int _openFrame;

	private int _frame;

	private int _delay;

	private enum Mode
	{
		Hold,
		Move,
		Cooldown
	}

	private sealed class Rider
	{
		public Mode Mode;

		public Vector2 Position;

		/// <summary>The velocity in HD pixels for each frame.</summary>
		public Vector2 Velocity;

		public int Cooldown;
	}

	/// <summary>The subtype of the original: bits 0 and 1 give the direction, and bit 7 lets the character go at once.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The flip of the original placement.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public OOZLaunchBallInstance() => DesignBounds = new Rectanglei(-192, -192, 384, 384);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZLaunchBallType.AnimationGroupResourceKey));
		(_artFlipX, _artFlipY, _openFrame) = Properties[((_subtype & 0xF) + (_flipX ? 4 : 0)) & 7];
		_frame = _openFrame;
	}

	/// <summary>The ball turns up from frame 0 to 7, or down from frame 7 to 0.</summary>
	private int ClosedFrame => _openFrame == 0 ? LastFrame : 0;

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters.ToArray())
		{
			UpdateCharacter(character);
		}
		LockLifetime = _riders.Count > 0;
	}

	private void UpdateCharacter(ICharacter character)
	{
		if (!_riders.TryGetValue(character, out Rider rider))
		{
			TryTake(character);
			return;
		}
		if (rider.Mode == Mode.Cooldown)
		{
			if (--rider.Cooldown < 0)
			{
				_riders.Remove(character);
			}
			return;
		}
		if (!OOZTransport.IsCarriedBy(this, character))
		{
			// Another launcher took the character
			_riders.Remove(character);
			return;
		}
		if (character.IsDebug || character.IsDying || character.IsDead)
		{
			OOZTransport.Drop(character);
			_riders.Remove(character);
			return;
		}
		if (rider.Mode == Mode.Hold)
		{
			OOZTransport.Hold(character, (Vector2)Position, new Vector2());
			if (Turn())
			{
				Launch(character, rider);
			}
			return;
		}
		if (!OOZTransport.IsOnScreen(Level, character))
		{
			OOZTransport.Release(character, rider.Velocity);
			_riders.Remove(character);
			return;
		}
		if (!_riders.Values.Any(r => r.Mode == Mode.Hold) && --_delay < 0)
		{
			_delay = TurnBackTicks;
			_frame += _openFrame > _frame ? 1 : _openFrame < _frame ? -1 : 0;
		}
		rider.Position += rider.Velocity;
		OOZTransport.Hold(character, rider.Position, rider.Velocity);
	}

	private void TryTake(ICharacter character)
	{
		if (!OOZTransport.CanTake(character) ||
			(ushort)(character.Position.X / Scale - Position.X / Scale + CaptureRange) >= CaptureRange * 2 ||
			(ushort)(character.Position.Y / Scale - Position.Y / Scale + CaptureRange) >= CaptureRange * 2)
		{
			return;
		}
		OOZTransport.Take(this, character);
		OOZTransport.Hold(character, (Vector2)Position, new Vector2());
		character.GroundVelocity = LaunchSpeed * Unit;
		_riders[character] = new Rider { Mode = Mode.Hold };
		_frame = _openFrame;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(OOZLaunchBallType.SoundResourceKey));
	}

	/// <summary>Turns the ball one frame each 8 frames. Returns true when it points out.</summary>
	private bool Turn()
	{
		int closed = ClosedFrame;
		if (_frame == closed)
		{
			return true;
		}
		if (--_delay >= 0)
		{
			return false;
		}
		_delay = TurnTicks;
		_frame += closed > _frame ? 1 : -1;
		return _frame == closed;
	}

	private void Launch(ICharacter character, Rider rider)
	{
		int direction = (_subtype + 1 - (_flipX ? 2 : 0)) & 3;
		rider.Velocity = Directions[direction] * (LaunchSpeed * Unit);
		rider.Position = (Vector2)Position;
		_delay = LaunchDelay;
		if ((_subtype & 0x80) != 0)
		{
			OOZTransport.Release(character, rider.Velocity);
			rider.Mode = Mode.Cooldown;
			rider.Cooldown = CooldownTicks;
		}
		else
		{
			rider.Mode = Mode.Move;
		}
	}

	protected override void OnStop()
	{
		foreach (KeyValuePair<ICharacter, Rider> pair in _riders)
		{
			if (pair.Value.Mode != Mode.Cooldown && OOZTransport.IsCarriedBy(this, pair.Key))
			{
				OOZTransport.Release(pair.Key, pair.Value.Velocity);
			}
		}
		_riders.Clear();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().Render(_animation, _artFlipX, _artFlipY);
	}
}
