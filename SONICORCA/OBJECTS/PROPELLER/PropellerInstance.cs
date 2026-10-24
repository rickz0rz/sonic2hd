using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.PROPELLER;

/// <summary>
/// A propeller, as in Sonic 2.
/// <list type="bullet">
/// <item>The vertical propeller (ObjB4) turns (frames 0 to 2, 2 frames each) and hurts a player that touches its
/// blade (4 x $40 pixels). It makes the helicopter sound every 32 frames while it is on screen. When it is flipped
/// vertically in the level (Harmless), it does not hurt.</item>
/// <item>The horizontal propeller (ObjB5) turns (frames 0 to 5, 2 frames each). In Wing Fortress Zone (subtype $66,
/// Lift) it lifts the players within $40 pixels on the x axis, from $60 above it to $30 below, and makes them
/// tumble. Global oscillator 5 moves the top of that area. In Sky Chase Zone (subtype $68) it only turns.</item>
/// </list>
/// </summary>
public class PropellerInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The touch size of the vertical blade: 4 x $40 (collision $A8).</summary>
	private const int BladeHalfWidth = 4 * Scale;

	private const int BladeHalfHeight = 0x40 * Scale;

	private const int VerticalFrames = 3;

	private const int HorizontalFrames = 6;

	private const int FrameTicks = 2;

	private const int SoundTicks = 32;

	private const int Oscillator = 5;

	private const int TumbleTurns = 0x7F;

	private readonly HashSet<ICharacter> _tumbling = new HashSet<ICharacter>();

	private AnimationInstance _animation;

	private bool _horizontal;

	private bool _harmless;

	private bool _lift;

	private int _ticks;

	/// <summary>The horizontal propeller (ObjB5) instead of the vertical propeller (ObjB4).</summary>
	[StateVariable]
	private bool Horizontal
	{
		get => _horizontal;
		set => _horizontal = value;
	}

	/// <summary>The vertical propeller does not hurt (the vertical flip of the original).</summary>
	[StateVariable]
	private bool Harmless
	{
		get => _harmless;
		set => _harmless = value;
	}

	/// <summary>The horizontal propeller lifts the players (subtype $66).</summary>
	[StateVariable]
	private bool Lift
	{
		get => _lift;
		set => _lift = value;
	}

	public PropellerInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x40 * Scale, 0x80 * Scale, 0x80 * Scale);

	protected override void OnStart()
	{
		string key = _horizontal ? PropellerType.HorizontalAnimationGroupResourceKey : PropellerType.AnimationGroupResourceKey;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(key));
		CollisionRectangles = _horizontal || _harmless
			? new CollisionRectangle[0]
			: new[] { new CollisionRectangle(this, 0, -BladeHalfWidth, -BladeHalfHeight, BladeHalfWidth * 2, BladeHalfHeight * 2) };
	}

	protected override void OnUpdate()
	{
		_ticks++;
		if (!_horizontal)
		{
			if (Level.Ticks % SoundTicks == 0)
			{
				Level.SoundManager.PlaySoundVisibleOnly(Type.GetAbsolutePath(PropellerType.SoundResourceKey), Position);
			}
			return;
		}
		if (!_lift)
		{
			return;
		}
		UpdateTumbling();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (!character.IsHurt && !character.IsDying && !character.IsDead && !character.IsDebug && !character.IsObjectControlled)
			{
				LiftCharacter(character);
			}
		}
	}

	/// <summary>ObjB5_CheckPlayer: lifts a character, stops its fall, and makes it tumble.</summary>
	private void LiftCharacter(ICharacter character)
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
		// Below the propeller, the lift gets smaller twice as fast
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
			// The original shows the float animation for the tumble, so the character is not a spinball
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
		foreach (ICharacter character in _tumbling)
		{
			character.TumbleTurns = 0;
		}
		_tumbling.Clear();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			var character = (ICharacter)e.ActiveObject;
			character.Hurt(System.Math.Sign(character.Position.X - Position.X));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		int frames = _horizontal ? HorizontalFrames : VerticalFrames;
		_animation.Index = (_ticks / FrameTicks) % frames;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
