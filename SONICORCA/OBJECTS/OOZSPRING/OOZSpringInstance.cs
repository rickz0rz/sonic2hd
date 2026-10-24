using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZSPRING;

/// <summary>
/// A pressure spring of Oil Ocean Zone, as in Sonic 2 (Obj45). Subtype bits 4-6 select the kind: 0 is the vertical
/// spring, all other values are the horizontal spring (the original layout also uses $30, which the original
/// treats as horizontal by luck).
/// The vertical spring goes down 2 pixels each frame while a character stands on it. After 9 frames it sends the
/// characters on it up at $1000 (subtype bit 1 clear) or $A00 (bit 1 set), and then comes back up 2 pixels each frame.
/// A character that pushes against the face of the horizontal spring pushes it in 1 pixel each frame, up to $12
/// pixels. When the character stops pushing, the spring goes back 4 pixels each frame and sends the characters at
/// its face away at (compression + $A) * $80.
/// Subtype bit 7 clears the other speed of the character, bit 0 makes the character somersault, and bits 2-3 set
/// the collision path (4: path 1, 8: path 2). The horizontal spring faces right, or left when flipped.
/// </summary>
public class OOZSpringInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The vertical spring: $10 pixels to each side and $14 pixels up and down.</summary>
	private const int VerticalHalfWidth = 0x10;

	private const int VerticalHalfHeight = 0x14;

	private const int VerticalLaunchFrame = 9;

	/// <summary>The horizontal spring: $14 pixels to each side, $D pixels up and $C pixels down.</summary>
	private const int HorizontalHalfWidth = 0x14;

	private const int HorizontalTop = 0xD;

	private const int HorizontalBottom = 0xC;

	/// <summary>The first mapping frame of the horizontal spring. Each pixel of compression adds 1.</summary>
	private const int HorizontalFrame = 0xA;

	private const int MaxCompression = 0x12;

	private const int ReturnSpeed = 4;

	/// <summary>A pushing character moves at $40 (inertia) while the spring goes in.</summary>
	private const double PushSpeed = 0x40 * Scale / 256.0;

	/// <summary>The character can not change its speed for $F frames after the horizontal spring (move_lock).</summary>
	private const int MoveLockTicks = 0xF;

	/// <summary>The Character animation of a spring jump. It stays until the character lands.</summary>
	private const int CharacterAnimationSpringLaunch = 12;

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private bool _horizontal;

	/// <summary>The compression of the vertical spring in frames (0 to 9), or of the horizontal spring in pixels (0 to $12).</summary>
	private int _compression;

	private int _originX;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The placement is flipped: the horizontal spring faces left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public OOZSpringInstance() => DesignBounds = new Rectanglei(-0x28 * Scale, -0x18 * Scale, 0x50 * Scale, 0x30 * Scale);

	/// <summary>The side of the face of the horizontal spring: 1 for right, -1 for left.</summary>
	private int FaceDirection => _flipX ? -1 : 1;

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZSpringType.AnimationGroupResourceKey));
		_horizontal = (_subtype & 0x70) != 0;
		_originX = Position.X;
		CollisionVectors = CollisionVector.FromRectangle(this, GetSolidBox());
		if (!_horizontal)
		{
			// A character on the top goes down with it
			CollisionVectors[1].Flags = CollisionFlags.Conveyor;
		}
	}

	private Rectanglei GetSolidBox()
	{
		if (_horizontal)
		{
			return new Rectanglei(-HorizontalHalfWidth * Scale, -HorizontalTop * Scale, HorizontalHalfWidth * 2 * Scale, (HorizontalTop + HorizontalBottom) * Scale);
		}
		int top = -VerticalHalfHeight + _compression * 2;
		return new Rectanglei(-VerticalHalfWidth * Scale, top * Scale, VerticalHalfWidth * 2 * Scale, (VerticalHalfHeight - top) * Scale);
	}

	protected override void OnUpdate()
	{
		if (_horizontal)
		{
			UpdateHorizontal();
		}
		else
		{
			UpdateVertical();
			CollisionVector.UpdateRectangle(CollisionVectors, GetSolidBox());
			RegisterCollisionUpdate();
		}
	}

	private void UpdateVertical()
	{
		var standing = new List<ICharacter>();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this && !character.IsDebug)
			{
				standing.Add(character);
			}
		}
		if (standing.Count == 0)
		{
			_compression = Math.Max(0, _compression - 1);
			return;
		}
		if (_compression < VerticalLaunchFrame)
		{
			_compression++;
			return;
		}
		double speed = ((_subtype & 2) != 0 ? 0xA00 : 0x1000) * Scale / 256.0;
		foreach (ICharacter character in standing)
		{
			character.LeaveGround();
			character.IsAirborne = true;
			character.IsSpinball = false;
			character.Velocity = new Vector2((_subtype & 0x80) != 0 ? 0.0 : character.Velocity.X, -speed);
			if ((_subtype & 1) != 0)
			{
				// One somersault, or two from the strong spring
				character.TumbleAngle = 0.0;
				character.TumbleTurns = (_subtype & 2) != 0 ? 1 : 2;
				character.GroundVelocity = character.Facing;
			}
			else
			{
				character.Animation.Index = CharacterAnimationSpringLaunch;
			}
			SetPath(character);
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(OOZSpringType.SoundResourceKey));
	}

	private void UpdateHorizontal()
	{
		int direction = FaceDirection;
		bool compress = false;
		var atFace = new List<ICharacter>();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (!IsAtFace(character))
			{
				continue;
			}
			atFace.Add(character);
			// Only a character that faces the spring pushes it
			if (character.Facing != -direction)
			{
				continue;
			}
			if (character.Input.HorizontalDirection == -direction)
			{
				compress = true;
				if (_compression < MaxCompression)
				{
					_compression++;
					character.Position += new Vector2i(-direction * Scale, 0);
					character.GroundVelocity = -direction * PushSpeed;
					character.Velocity = new Vector2(0.0, character.Velocity.Y);
				}
			}
			else if (Math.Sign(character.GroundVelocity) == -direction)
			{
				compress = true;
			}
		}
		if (!compress && _compression > 0)
		{
			_compression = Math.Max(0, _compression - ReturnSpeed);
			Launch(atFace);
		}
		Position = new Vector2i(_originX - direction * _compression * Scale, Position.Y);
	}

	/// <summary>True when a character on the ground touches the face of the horizontal spring.</summary>
	private bool IsAtFace(ICharacter character)
	{
		if (character.IsAirborne || character.IsDebug || character.IsDying || character.IsObjectControlled || character.ObjectLink == this)
		{
			return false;
		}
		int dx = (character.Position.X - Position.X) * FaceDirection;
		int reach = HorizontalHalfWidth * Scale + character.CollisionRadius.X + Scale * 2;
		if (dx < HorizontalHalfWidth * Scale || dx > reach)
		{
			return false;
		}
		int dy = character.Position.Y - Position.Y;
		return dy + character.CollisionRadius.Y > -HorizontalTop * Scale && dy - character.CollisionRadius.Y < HorizontalBottom * Scale;
	}

	/// <summary>Sends the characters at the face away at (compression + $A) * $80.</summary>
	private void Launch(List<ICharacter> characters)
	{
		if (characters.Count == 0)
		{
			return;
		}
		int direction = FaceDirection;
		double speed = ((_compression + 0xA) << 7) * Scale / 256.0 * direction;
		foreach (ICharacter character in characters)
		{
			character.Position += new Vector2i(direction * 4 * Scale, 0);
			character.Velocity = new Vector2(speed, (_subtype & 0x80) != 0 ? 0.0 : character.Velocity.Y);
			character.GroundVelocity = speed;
			character.Facing = direction;
			character.SlopeLockTicks = Math.Max(MoveLockTicks, character.SlopeLockTicks);
			character.IsPushing = false;
			if ((_subtype & 1) != 0)
			{
				// Two somersaults, or four from the strong spring
				character.TumbleAngle = 0.0;
				character.TumbleTurns = (_subtype & 2) != 0 ? 2 : 4;
			}
			SetPath(character);
		}
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(OOZSpringType.SoundResourceKey));
	}

	/// <summary>Subtype bits 2-3: 4 puts the character on the first collision path, 8 on the second.</summary>
	private void SetPath(ICharacter character)
	{
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

	protected override void OnAnimate()
	{
		_animation.Index = _horizontal ? HorizontalFrame + _compression : _compression;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation, _horizontal && _flipX);
	}
}
