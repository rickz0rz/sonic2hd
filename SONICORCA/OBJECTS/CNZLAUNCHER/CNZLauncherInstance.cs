using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CNZLAUNCHER;

/// <summary>
/// A spring launcher, as in Sonic 2 (Obj85). It holds a character that lands on it (or pushes the diagonal one) in a
/// ball. While the main player holds jump, the plunger pulls back one pixel each 4 frames, up to $20 pixels. When
/// the player lets go, the launcher sends each character on it away: up at speed (pull + $10) * $80 for the vertical
/// launcher, or up and to the right at (pull + 4) * $80 for the diagonal launcher.
/// </summary>
public class CNZLauncherInstance : ActiveObject
{
	private const int Scale = 4;

	private const int MaxPull = 0x20;

	private const int PullTicks = 4;

	/// <summary>Without a character, the plunger goes back 4 pixels each frame.</summary>
	private const int ReturnSpeed = 4;

	/// <summary>The position of the held character from the plunger: above it, or up and to the right of the diagonal plunger.</summary>
	private static readonly Vector2i VerticalHold = new Vector2i(0, -0x2E);

	private static readonly Vector2i DiagonalHold = new Vector2i(0x13, -0x13);

	/// <summary>The mapping frames: the plunger top, the long spring, the short spring, and the flash of the top.</summary>
	private const int TopFrame = 1;

	private const int LongSpringFrame = 2;

	private const int ShortSpringFrame = 3;

	private const int FlashFrame = 5;

	/// <summary>The spring of the vertical launcher is $20 pixels below its start.</summary>
	private const int VerticalSpringOffset = 0x20;

	/// <summary>A sidekick on the launcher without the main player goes after this many frames, at the lowest speed.</summary>
	private const int SidekickWaitTicks = 180;

	private const int ReleaseTicks = 16;

	private readonly HashSet<ICharacter> _held = new HashSet<ICharacter>();

	private readonly Dictionary<ICharacter, int> _released = new Dictionary<ICharacter, int>();

	private AnimationInstance _animation;

	private bool _diagonal;

	private bool _groundLaunch;

	private Vector2i _start;

	/// <summary>The pull of the plunger in original pixels, from 0 to $20.</summary>
	private int _pull;

	private int _pullCounter;

	private int _flashCounter;

	private bool _flash;

	private bool _playerHoldsJump;

	private int _sidekickTicks;

	/// <summary>The diagonal launcher (subtype not 0) instead of the vertical launcher.</summary>
	[StateVariable]
	private bool Diagonal
	{
		get => _diagonal;
		set => _diagonal = value;
	}

	/// <summary>The diagonal launcher sends the character along the ground up a slope (subtype bit 7).</summary>
	[StateVariable]
	private bool GroundLaunch
	{
		get => _groundLaunch;
		set => _groundLaunch = value;
	}

	public CNZLauncherInstance() => DesignBounds = new Rectanglei(-160, -256, 320, 448);

	protected override void OnStart()
	{
		string key = _diagonal ? CNZLauncherType.DiagonalAnimationGroupResourceKey : CNZLauncherType.AnimationGroupResourceKey;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(key));
		_start = Position;
		// The solid box of the plunger (SolidObject: $18 pixels to each side, and the top $20 or 8 pixels up)
		CollisionVectors = _diagonal
			? CollisionVector.FromRectangle(this, new Rectanglei(-0x18 * Scale, -8 * Scale, 0x30 * Scale, 13 * Scale))
			: CollisionVector.FromRectangle(this, new Rectanglei(-0x18 * Scale, -0x20 * Scale, 0x30 * Scale, 0x3D * Scale));
		if (_diagonal)
		{
			// The diagonal launcher also takes a character that pushes it
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -0x18 * Scale - 8, -8 * Scale, 0x30 * Scale + 16, 13 * Scale)
			};
		}
	}

	private Vector2i PlungerPosition => _diagonal
		? _start + new Vector2i(-(_pull >> 1), _pull >> 1) * Scale
		: _start + new Vector2i(0, _pull) * Scale;

	protected override void OnUpdatePrepare()
	{
		// The character clears its input when an object controls it, so read the button before it updates
		ICharacter player = Level.Player.Protagonist;
		_playerHoldsJump = _held.Contains(player) && player is Character character &&
			(character.Input.ABC == CharacterInputButtonState.Down || character.Input.ABC == CharacterInputButtonState.Pressed);
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in _released.Keys.ToArray())
		{
			if (--_released[character] <= 0)
			{
				_released.Remove(character);
			}
		}
		Position = PlungerPosition;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (!_held.Contains(character) && character.ObjectLink == this && !character.IsDebug && !_released.ContainsKey(character))
			{
				Take(character);
			}
		}
		if (_held.Count == 0)
		{
			_flash = false;
			_pull = Math.Max(0, _pull - ReturnSpeed);
			return;
		}
		if (_playerHoldsJump)
		{
			Pull();
		}
		else if (_pull > 0)
		{
			Launch();
			return;
		}
		else if (!_held.Contains(Level.Player.Protagonist) && ++_sidekickTicks >= SidekickWaitTicks)
		{
			Launch();
			return;
		}
		Position = PlungerPosition;
		foreach (ICharacter character in _held.ToArray())
		{
			if (character.IsDebug || character.IsDying)
			{
				Release(character);
				continue;
			}
			HoldInPlace(character);
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_diagonal && e.ActiveObject is ICharacter character && !character.IsAirborne && !_held.Contains(character) &&
			!character.IsDebug && !_released.ContainsKey(character))
		{
			Take(character);
		}
	}

	private void Take(ICharacter character)
	{
		_held.Add(character);
		_sidekickTicks = 0;
		character.GroundVelocity = 0.0;
		HoldInPlace(character);
	}

	private void HoldInPlace(ICharacter character)
	{
		character.IsObjectControlled = true;
		character.IsAirborne = true;
		character.IsSpinball = true;
		character.Position = Position + (_diagonal ? DiagonalHold : VerticalHold) * Scale;
		character.Velocity = new Vector2(0.0, 0.001);
	}

	/// <summary>Pulls the plunger back one pixel each 4 frames, and flashes its top faster as it pulls further.</summary>
	private void Pull()
	{
		if (--_pullCounter < 0)
		{
			_pullCounter = PullTicks - 1;
			if (_pull < MaxPull)
			{
				_pull++;
			}
		}
		_flash = false;
		if (--_flashCounter < 0)
		{
			_flashCounter = (MaxPull - _pull) >> 1;
			_flash = true;
		}
	}

	private void Launch()
	{
		double speed = (_diagonal ? (_pull + 4) << 7 : (_pull + 0x10) << 7) / 256.0 * Scale;
		foreach (ICharacter character in _held.ToArray())
		{
			Release(character);
			if (_diagonal)
			{
				character.Velocity = new Vector2(speed, -speed);
				character.GroundVelocity = _groundLaunch ? speed : 0x800 / 256.0 * Scale;
				character.Facing = 1;
			}
			else
			{
				character.Velocity = new Vector2(0.0, -speed);
				character.GroundVelocity = 0x800 / 256.0 * Scale;
			}
		}
		_flash = false;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZLauncherType.SoundResourceKey));
	}

	private void Release(ICharacter character)
	{
		_held.Remove(character);
		_released[character] = ReleaseTicks;
		character.IsObjectControlled = false;
		character.LeaveGround();
		character.IsAirborne = true;
	}

	protected override void OnStop()
	{
		foreach (ICharacter character in _held.ToArray())
		{
			Release(character);
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The spring stays at the start (the vertical one $20 pixels lower), and the plunger top moves with the pull
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		Vector2 spring = (Vector2)(_start - Position) + (_diagonal ? new Vector2() : new Vector2(0.0, VerticalSpringOffset * Scale));
		_animation.Index = _pull >= 0x10 ? ShortSpringFrame : LongSpringFrame;
		objectRenderer.Render(_animation, spring);
		_animation.Index = _flash ? FlashFrame : TopFrame;
		objectRenderer.Render(_animation);
	}
}
