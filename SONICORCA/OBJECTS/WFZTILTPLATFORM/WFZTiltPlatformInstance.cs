using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZTILTPLATFORM;

/// <summary>
/// A tilting platform, as in Sonic 2 (ObjB6). The platform is a solid block of 48 by 8 pixels. When it tilts, the
/// characters on it fall, and it is not solid until it is flat again. Bits 1-2 of the subtype give the kind, and bits
/// 4-7 give a time:
/// <list type="bullet">
/// <item>Timed: it tilts when bits 4-7 of the frame counter equal the time, so once in each 256 frames. It stays
/// tilted for 64 frames.</item>
/// <item>Spin: it waits for the time, then spins for 32 frames, then waits $C0 frames for each next spin. The original
/// also fires a large vertical laser (ObjB7) from it, which is not in this object. WFZ does not use this kind.</item>
/// <item>Stand: it tilts 17 frames after a character stands on it, away from the nearer player.</item>
/// <item>Vertical: it stands up (8 by 48 pixels). It waits for the time, then lies down for 64 frames and stands up
/// again, then waits $C0 frames for the next time. WFZ does not use this kind.</item>
/// </list>
/// </summary>
public class WFZTiltPlatformInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>SolidObject with $23, 4 and 4: 24 by 4 pixels from the centre.</summary>
	private const int FlatHalfWidth = (0x23 - 0xB) * Scale;

	private const int FlatHalfHeight = 4 * Scale;

	/// <summary>SolidObject with $F, $18 and $18: 4 by 24 pixels from the centre.</summary>
	private const int VerticalHalfWidth = (0xF - 0xB) * Scale;

	private const int VerticalHalfHeight = 0x18 * Scale;

	private const int StandDelay = 0x10;

	private const int SpinTicks = 0x20;

	private const int RepeatDelay = 0xC0;

	private const int TiltScript = 0;

	private const int SpinScript = 3;

	private const int LieDownScript = 4;

	private const int VerticalFrame = 2;

	private readonly SpriteAnimator _animator = new SpriteAnimator(
		new byte[] { 3, 1, 2, 0xFD, 1, 0 },
		new byte[] { 0x3F, 2, 0xFD, 2 },
		new byte[] { 3, 2, 1, 0, 0xFA, 0 },
		new byte[] { 1, 0, 1, 2, 3, 0xFF },
		new byte[] { 3, 1, 0, 0xFD, 5, 0 },
		new byte[] { 0x3F, 0, 0xFD, 6 },
		new byte[] { 3, 0, 1, 2, 0xFA, 0 });

	private AnimationInstance _animation;

	private TiltKind _kind;

	private int _time;

	private int _secondary;

	private int _timer;

	private bool _flipX;

	private bool _solid;

	public enum TiltKind
	{
		Timed,
		Spin,
		Stand,
		Vertical
	}

	/// <summary>The kind of platform (subtype bits 1-2).</summary>
	[StateVariable]
	private TiltKind Kind
	{
		get => _kind;
		set => _kind = value;
	}

	/// <summary>The time (subtype bits 4-7, as a multiple of 16 frames).</summary>
	[StateVariable]
	private int Time
	{
		get => _time;
		set => _time = value;
	}

	public WFZTiltPlatformInstance() => DesignBounds = new Rectanglei(-0x18 * Scale, -0x18 * Scale, 0x30 * Scale, 0x30 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZTiltPlatformType.AnimationGroupResourceKey));
	}

	protected override void OnUpdate()
	{
		switch (_kind)
		{
			case TiltKind.Timed:
				UpdateTimed();
				break;
			case TiltKind.Spin:
				UpdateSpin();
				break;
			case TiltKind.Stand:
				UpdateStand();
				break;
			case TiltKind.Vertical:
				UpdateVertical();
				break;
		}
	}

	private void UpdateTimed()
	{
		switch (_secondary)
		{
			case 0:
				_secondary = 1;
				break;
			case 1:
				SetSolid(true);
				if ((Level.Ticks & 0xF0) == _time)
				{
					_secondary = 2;
					_animator.Animation = TiltScript;
					DropCharacters();
				}
				break;
			case 2:
				if (_animator.Step() == SpriteAnimator.Result.NextSecondaryRoutine)
				{
					_secondary = 3;
				}
				break;
			case 3:
				_secondary = 1;
				break;
		}
	}

	private void UpdateSpin()
	{
		switch (_secondary)
		{
			case 0:
				_timer = _time;
				_secondary = 1;
				break;
			case 1:
				SetSolid(true);
				if (--_timer < 0)
				{
					_secondary = 2;
					_timer = SpinTicks;
					_animator.Reset(_animator.Frame);
					_animator.Animation = SpinScript;
					DropCharacters();
				}
				break;
			case 2:
				if (--_timer >= 0)
				{
					_animator.Step();
					break;
				}
				_secondary = 1;
				_animator.Frame = 0;
				_timer = RepeatDelay;
				break;
		}
	}

	private void UpdateStand()
	{
		switch (_secondary)
		{
			case 0:
				SetSolid(true);
				if (IsStoodOn())
				{
					_secondary = 1;
					_timer = StandDelay;
				}
				break;
			case 1:
				SetSolid(true);
				if (--_timer < 0)
				{
					_secondary = 2;
					_animator.Animation = TiltScript;
					// The platform tilts down away from the nearer player
					_flipX = !PlayerOrientation.Get(Level, Position).PlayerRight;
					DropCharacters();
				}
				break;
			case 2:
				if (_animator.Step() == SpriteAnimator.Result.NextSecondaryRoutine)
				{
					_secondary = 3;
				}
				break;
			case 3:
				_secondary = 0;
				break;
		}
	}

	private void UpdateVertical()
	{
		switch (_secondary)
		{
			case 0:
				_animator.Frame = VerticalFrame;
				_timer = _time;
				_secondary = 1;
				break;
			case 1:
				SetSolid(true);
				if (--_timer < 0)
				{
					_secondary = 2;
					_animator.Animation = LieDownScript;
					DropCharacters();
				}
				break;
			case 2:
				if (_animator.Step() == SpriteAnimator.Result.NextSecondaryRoutine)
				{
					_secondary = 3;
				}
				break;
			case 3:
				_secondary = 1;
				_timer = RepeatDelay;
				break;
		}
	}

	private bool IsStoodOn()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>The characters on the platform fall, and the platform is not solid while it moves (loc_3B7BC).</summary>
	private void DropCharacters()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				character.LeaveGround();
				character.IsAirborne = true;
			}
		}
		SetSolid(false);
	}

	private void SetSolid(bool solid)
	{
		if (solid == _solid)
		{
			return;
		}
		_solid = solid;
		if (!solid)
		{
			CollisionVectors = new CollisionVector[0];
		}
		else if (_kind == TiltKind.Vertical)
		{
			CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-VerticalHalfWidth, -VerticalHalfHeight, VerticalHalfWidth * 2, VerticalHalfHeight * 2));
		}
		else
		{
			CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-FlatHalfWidth, -FlatHalfHeight, FlatHalfWidth * 2, FlatHalfHeight * 2));
		}
		RegisterCollisionUpdate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _animator.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
