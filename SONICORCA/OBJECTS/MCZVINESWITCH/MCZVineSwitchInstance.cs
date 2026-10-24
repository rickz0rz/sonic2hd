using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZVINESWITCH;

/// <summary>
/// A vine switch, as in Sonic 2 (Obj7F in Mystic Cave Zone). A character that comes near the handle hangs on it, and
/// the vine pulls down a little. While a character hangs, the trigger of the switch (subtype bits 0-3) is set, so a
/// bridge with the same trigger opens. A jump lets go of the switch with a speed of $300 up and clears the trigger.
/// After that, the character cannot hang on the switch again for 18 frames (60 frames when it holds a direction).
/// </summary>
public class MCZVineSwitchInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The character hangs $30 pixels below the switch.</summary>
	private const int HangOffset = 0x30;

	private const int ReleaseSpeed = -0x300;

	private const int ReleaseTicks = 18;

	private const int ReleaseDirectionTicks = 60;

	private const int UpFrame = 0;

	private const int PulledFrame = 1;

	private readonly Dictionary<ICharacter, int> _waitTicks = new Dictionary<ICharacter, int>();

	private readonly HashSet<ICharacter> _hanging = new HashSet<ICharacter>();

	private readonly HashSet<ICharacter> _jumpPressed = new HashSet<ICharacter>();

	private readonly HashSet<ICharacter> _directionHeld = new HashSet<ICharacter>();

	private AnimationInstance _animation;

	private int _trigger;

	/// <summary>The index of the trigger that the switch sets (subtype bits 0-3).</summary>
	[StateVariable]
	private int Trigger
	{
		get => _trigger;
		set => _trigger = value;
	}

	public MCZVineSwitchInstance() => DesignBounds = new Rectanglei(-8 * Scale, -0x30 * Scale, 0x10 * Scale, 0x68 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZVineSwitchType.AnimationGroupResourceKey));
	}

	protected override void OnUpdatePrepare()
	{
		// The character clears its input when an object controls it, so read the buttons before it updates
		_jumpPressed.Clear();
		_directionHeld.Clear();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character is Character c)
			{
				if (c.Input.ABC == CharacterInputButtonState.Pressed)
				{
					_jumpPressed.Add(character);
				}
				if (c.Input.HorizontalDirection != 0 || c.Input.VerticalDirection != 0)
				{
					_directionHeld.Add(character);
				}
			}
		}
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (_hanging.Contains(character))
			{
				UpdateHanging(character);
			}
			else
			{
				CheckGrab(character);
			}
		}
	}

	private void UpdateHanging(ICharacter character)
	{
		if (character.IsDebug || character.IsDying || character.IsDead)
		{
			Release(character, ReleaseDirectionTicks);
			return;
		}
		if (JumpPressed(character))
		{
			Release(character, _directionHeld.Contains(character) ? ReleaseDirectionTicks : ReleaseTicks);
			character.Velocity = new Vector2(0.0, ReleaseSpeed * Scale / 256.0);
			// As in the original, the trigger clears also when the other character still hangs
			ButtonVineTriggers.For(Level).Clear(_trigger);
			return;
		}
		Hold(character);
	}

	/// <summary>
	/// The sidekick of the computer does not get input while an object holds it. Like the original, where the
	/// sidekick copies the buttons of the main player, it lets go when the main player presses jump.
	/// </summary>
	private bool JumpPressed(ICharacter character)
	{
		return _jumpPressed.Contains(character) || (character.IsSidekick && _jumpPressed.Contains(Level.Player.Protagonist));
	}

	private void CheckGrab(ICharacter character)
	{
		if (_waitTicks.TryGetValue(character, out int ticks))
		{
			if (--ticks > 0)
			{
				_waitTicks[character] = ticks;
				return;
			}
			_waitTicks.Remove(character);
		}
		Vector2 offset = (character.PositionPrecise - PositionPrecise) / Scale;
		if (offset.X < -0xC || offset.X >= 0xC || offset.Y < 0x28 || offset.Y >= 0x38)
		{
			return;
		}
		if (character.IsObjectControlled || character.IsDebug || character.IsHurt || character.IsDying || character.IsDead)
		{
			return;
		}
		_hanging.Add(character);
		character.GroundVelocity = 0.0;
		character.IsSpinball = false;
		character.IsJumping = false;
		character.SpecialState = CharacterSpecialState.Hanging;
		Hold(character);
		ButtonVineTriggers.For(Level).Set(_trigger);
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MCZVineSwitchType.SwitchSoundResourceKey));
	}

	private void Hold(ICharacter character)
	{
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.Position = Position + new Vector2i(0, HangOffset * Scale);
		character.Velocity = new Vector2();
	}

	private void Release(ICharacter character, int waitTicks)
	{
		_hanging.Remove(character);
		_waitTicks[character] = waitTicks;
		character.IsObjectControlled = false;
		character.SpecialState = CharacterSpecialState.Normal;
		character.LeaveGround();
		character.IsAirborne = true;
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in _hanging.ToArray())
		{
			Release(character, ReleaseTicks);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _hanging.Count > 0 ? PulledFrame : UpFrame;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
