using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZSTEAMSPRING;

/// <summary>
/// The steam piston of Metropolis Zone, as in Sonic 2 (Obj42). The solid piston repeats a cycle: it waits $3C frames
/// down, rises $10 pixels at 8 pixels each frame, waits $3C frames up, and goes down again at 8 pixels each frame.
/// While it rises, it sends the characters on it up at $A00. When it is up, it blows a puff of steam $28 pixels to
/// each side. The puffs show 7 frames of 8 frames each, and hurt during the fourth frame.
/// Subtype bit 7 clears the horizontal speed of the character, bit 0 makes it somersault (one time, or two times when
/// bit 1 is clear), and bits 2-3 set the collision path (4: path 1, 8: path 2).
/// </summary>
public class MTZSteamSpringInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The solid box: $1B pixels to each side and $10 up and down.</summary>
	private const int HalfWidth = 0x1B;

	private const int HalfHeight = 0x10;

	/// <summary>The piston is $10 pixels below its placement when it is down.</summary>
	private const int DownOffset = 0x10;

	private const int Speed = 8;

	private const int WaitFrames = 0x3C;

	private const int LaunchSpeed = 0xA00;

	private const int PistonFrame = 7;

	private const int SteamOffset = 0x28;

	private const int SteamFrames = 7;

	private const int SteamFrameDuration = 8;

	/// <summary>The puff hurts during this frame (collision flags $A6: $10 pixels to each side and 4 up and down).</summary>
	private const int SteamHurtFrame = 3;

	private const int SteamHalfWidth = 0x10;

	private const int SteamHalfHeight = 4;

	/// <summary>The Character animation of a spring jump. It stays until the character lands.</summary>
	private const int CharacterAnimationSpringLaunch = 12;

	private AnimationInstance _animation;

	private AnimationInstance _steamAnimation;

	private int _subtype;

	private Vector2 _origin;

	private Phase _phase;

	private int _timer;

	private int _offset = DownOffset;

	private bool _launching;

	/// <summary>The number of frames since the puffs of steam started, or -1 when there are no puffs.</summary>
	private int _steamTicks = -1;

	private enum Phase
	{
		WaitDown,
		Rise,
		WaitUp,
		Lower
	}

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public MTZSteamSpringInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x10 * Scale, 0x80 * Scale, 0x30 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZSteamSpringType.AnimationGroupResourceKey));
		_animation.Index = PistonFrame;
		_steamAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZSteamSpringType.SteamAnimationGroupResourceKey));
		_origin = PositionPrecise;
		PositionPrecise = _origin + new Vector2(0.0, _offset * Scale);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		CollisionVectors[0].Flags |= CollisionFlags.Solid;
		CollisionVectors[2].Flags |= CollisionFlags.Solid;
		CollisionRectangles = new CollisionRectangle[0];
	}

	protected override void UpdateMovement()
	{
		// The original checks the characters on the piston before it moves the piston
		_launching = _phase == Phase.Rise;
		switch (_phase)
		{
			case Phase.WaitDown:
			case Phase.WaitUp:
				if (--_timer < 0)
				{
					_timer = WaitFrames - 1;
					_phase = _phase == Phase.WaitDown ? Phase.Rise : Phase.Lower;
				}
				break;
			case Phase.Rise:
				_offset -= Speed;
				if (_offset == 0)
				{
					_phase = Phase.WaitUp;
					_steamTicks = 0;
				}
				break;
			case Phase.Lower:
				_offset += Speed;
				if (_offset == DownOffset)
				{
					_phase = Phase.WaitDown;
				}
				break;
		}
		PositionPrecise = _origin + new Vector2(0.0, _offset * Scale);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_launching)
		{
			Launch();
		}
		UpdateSteam();
	}

	private void Launch()
	{
		bool launched = false;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink != this || character.IsDebug)
			{
				continue;
			}
			launched = true;
			character.LeaveGround();
			character.IsAirborne = true;
			character.IsSpinball = false;
			character.Velocity = new Vector2((_subtype & 0x80) != 0 ? 0.0 : character.Velocity.X, -LaunchSpeed * Scale / 256.0);
			if ((_subtype & 1) != 0)
			{
				// Two somersaults, or one when bit 1 is set
				character.TumbleAngle = 0.0;
				character.TumbleTurns = (_subtype & 2) != 0 ? 1 : 2;
				character.GroundVelocity = character.Facing;
			}
			else
			{
				character.Animation.Index = CharacterAnimationSpringLaunch;
			}
			switch (_subtype & 0xC)
			{
				case 4:
					character.Path = 0;
					break;
				case 8:
					character.Path = 1;
					break;
			}
		}
		if (launched)
		{
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MTZSteamSpringType.SoundResourceKey));
		}
	}

	private void UpdateSteam()
	{
		if (_steamTicks >= 0 && ++_steamTicks >= SteamFrames * SteamFrameDuration)
		{
			_steamTicks = -1;
		}
		if (_steamTicks / SteamFrameDuration != SteamHurtFrame)
		{
			CollisionRectangles = new CollisionRectangle[0];
			return;
		}
		// The puffs are at the placement height, not at the height of the piston
		int y = (int)Math.Round((_origin.Y - PositionPrecise.Y) / Scale);
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 1, (-SteamOffset - SteamHalfWidth) * Scale, (y - SteamHalfHeight) * Scale, SteamHalfWidth * 2 * Scale, SteamHalfHeight * 2 * Scale),
			new CollisionRectangle(this, 2, (SteamOffset - SteamHalfWidth) * Scale, (y - SteamHalfHeight) * Scale, SteamHalfWidth * 2 * Scale, SteamHalfHeight * 2 * Scale)
		};
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if ((e.Id != 1 && e.Id != 2) || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		int puffX = Position.X + (e.Id == 1 ? -SteamOffset : SteamOffset) * Scale;
		character.Hurt(character.Position.X >= puffX ? 1 : -1);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (_steamTicks >= 0)
		{
			// The right puff faces right, the left puff is the mirror image
			_steamAnimation.Index = _steamTicks / SteamFrameDuration;
			double y = _origin.Y - PositionPrecise.Y;
			objectRenderer.RenderOriginal(_steamAnimation, new Vector2(SteamOffset * Scale, y));
			objectRenderer.RenderOriginal(_steamAnimation, new Vector2(-SteamOffset * Scale, y), true);
		}
		objectRenderer.RenderOriginal(_animation);
	}
}
