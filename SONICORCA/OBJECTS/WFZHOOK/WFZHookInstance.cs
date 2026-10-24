using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZHOOK;

/// <summary>
/// A hook on a chain, as in Sonic 2 (Obj80 in Wing Fortress Zone). A character that comes near the hook hangs on
/// it. While a character hangs, the hook goes down 2 pixels each frame, up to $A0 pixels ($60 pixels when subtype
/// bits 0-3 are not zero). Without a character, it goes back up. A hook that starts down (subtype bits 4-6) does the
/// opposite. A jump lets go with a speed of $380 up, and $200 to the side when a direction is held. After that, the
/// character cannot hang on the hook again for 18 frames (60 frames when it holds a direction). The vine of Mystic
/// Cave Zone is the same object with other art.
/// </summary>
public class WFZHookInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The character hangs $94 pixels below the object position.</summary>
	private const int HangOffset = 0x94;

	private const int LongTravelDistance = 0xA0;

	private const int ShortTravelDistance = 0x60;

	private const int Speed = 2;

	private const int ReleaseSpeedX = 0x200;

	private const int ReleaseSpeedY = -0x380;

	private const int ReleaseTicks = 18;

	private const int ReleaseDirectionTicks = 60;

	private readonly Dictionary<ICharacter, int> _waitTicks = new Dictionary<ICharacter, int>();

	private readonly HashSet<ICharacter> _hanging = new HashSet<ICharacter>();

	private readonly HashSet<ICharacter> _jumpPressed = new HashSet<ICharacter>();

	private readonly Dictionary<ICharacter, (int Horizontal, int Vertical)> _directions = new Dictionary<ICharacter, (int, int)>();

	private AnimationInstance _animation;

	private bool _shortTravel;

	private bool _startDown;

	private Vector2 _top;

	/// <summary>How far the hook is down, in original pixels.</summary>
	private int _offset;

	/// <summary>The hook goes down $60 pixels instead of $A0 (subtype bits 0-3 not zero).</summary>
	[StateVariable]
	private bool ShortTravel
	{
		get => _shortTravel;
		set => _shortTravel = value;
	}

	/// <summary>The hook starts down and goes up while a character hangs on it (subtype bits 4-6 not zero).</summary>
	[StateVariable]
	private bool StartDown
	{
		get => _startDown;
		set => _startDown = value;
	}

	public WFZHookInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, 0, 0x20 * Scale, 0x140 * Scale);

	private int MaxOffset => _shortTravel ? ShortTravelDistance : LongTravelDistance;

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZHookType.AnimationGroupResourceKey));
		_top = PositionPrecise;
		SetOffset(_startDown ? MaxOffset : 0);
	}

	protected override void OnUpdatePrepare()
	{
		// The character clears its input when an object controls it, so read the buttons before it updates
		_jumpPressed.Clear();
		_directions.Clear();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character is Character c)
			{
				if (c.Input.ABC == CharacterInputButtonState.Pressed)
				{
					_jumpPressed.Add(character);
				}
				_directions[character] = (c.Input.HorizontalDirection, c.Input.VerticalDirection);
			}
		}
	}

	protected override void OnUpdate()
	{
		// The hook goes down while a character hangs on it (up for the hook that starts down)
		bool down = _hanging.Count > 0 != _startDown;
		if (down && _offset != MaxOffset)
		{
			SetOffset(_offset + Speed);
		}
		else if (!down && _offset != 0)
		{
			SetOffset(_offset - Speed);
		}
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

	private void SetOffset(int offset)
	{
		_offset = offset;
		PositionPrecise = _top + new Vector2(0.0, offset * Scale);
	}

	/// <summary>The mapping frame shows a longer chain above the hook as the hook goes down.</summary>
	private int Frame => _offset == 0 ? 0 : (_offset >> 4) + 1;

	private void UpdateHanging(ICharacter character)
	{
		if (character.IsDebug || character.IsHurt || character.IsDying || character.IsDead)
		{
			Release(character, ReleaseDirectionTicks);
			return;
		}
		if (JumpPressed(character))
		{
			_directions.TryGetValue(character, out (int Horizontal, int Vertical) direction);
			if (character.IsSidekick && !_jumpPressed.Contains(character))
			{
				_directions.TryGetValue(Level.Player.Protagonist, out direction);
			}
			Release(character, direction.Horizontal != 0 || direction.Vertical != 0 ? ReleaseDirectionTicks : ReleaseTicks);
			character.Velocity = new Vector2(direction.Horizontal * ReleaseSpeedX * Scale / 256.0, ReleaseSpeedY * Scale / 256.0);
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

	/// <summary>A character hangs on the hook when it is less than $10 pixels to the side and $88 to $A0 pixels below.</summary>
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
		if (offset.X < -0x10 || offset.X >= 0x10 || offset.Y < 0x88 || offset.Y >= 0xA0)
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
		character.SpecialState = CharacterSpecialState.Grabbed;
		character.PositionPrecise = new Vector2(PositionPrecise.X, character.PositionPrecise.Y);
		Hold(character);
	}

	/// <summary>Keeps the character under the hook. Only the height follows the hook, as in the original.</summary>
	private void Hold(ICharacter character)
	{
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.PositionPrecise = new Vector2(character.PositionPrecise.X, PositionPrecise.Y + HangOffset * Scale);
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
		_animation.Index = Frame;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
