using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZLONGPLATFORM;

/// <summary>
/// A long moving platform or a cog, as in Sonic 2 (Obj65 in Metropolis Zone). Subtype bits 4-6 select the art and
/// the size: 0 and 3 are the long platform ($80 pixels), 1 is the short platform ($40 pixels), and 2 is a free cog.
/// The free cog turns with the x position of the riding platform (subtype 4).
/// Subtype bits 0-3 select the movement:
/// 1 waits for the switch with the index of the subtype, then slides left $80 pixels at 2 pixels each frame.
/// After $B4 frames it slides back (2), and waits for the switch again.
/// 7 starts slid out, waits for the switch, then slides back. After $B4 frames it slides out again (6).
/// 3 slides left $40 pixels at $10 pixels each frame while a character is near it, and back when no character is near.
/// 4 waits until the main character stands on it. Then (5) it moves right at 2 pixels each frame. In act 3 it stops
/// at x $1CC0 or $2940. In the other acts it goes between x $1880 and $1BC0.
/// With subtype bit 7, bits 0-3 are the index of the switch, and the art selects the movement (1 or 7). The
/// platform then has a cog that turns as it slides. A flipped platform slides right instead of left.
/// </summary>
public class MTZLongPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int CogArt = 2;

	/// <summary>The platform waits $B4 frames before it slides back.</summary>
	private const int WaitTicks = 0xB4;

	/// <summary>The positions that the riding platform (subtype 5) stops at or turns at, in original pixels.</summary>
	private const int Act3StopX1 = 0x1CC0;

	private const int Act3StopX2 = 0x2940;

	private const int TurnRightX = 0x1880;

	private const int TurnLeftX = 0x1BC0;

	/// <summary>Obj65_Properties: width_pixels, y_radius, the slide distance and the movement with subtype bit 7, by art.</summary>
	private static readonly (int Width, int Height, int Distance, int SwitchMovement)[] Properties =
	{
		(0x40, 0xC, 0x80, 1),
		(0x20, 0xC, 0x40, 3),
		(0x10, 0x10, 0x20, 0),
		(0x40, 0xC, 0x80, 7),
	};

	private AnimationInstance _animation;

	private AnimationInstance _cogAnimation;

	private int _subtype;

	private bool _flipX;

	private int _originalX;

	private int _art;

	private int _movement;

	private int _switchIndex;

	private int _distance;

	private Vector2 _start;

	/// <summary>The distance that the platform has slid (objoff_3A).</summary>
	private int _offset;

	private bool _active;

	private int _timer;

	private bool _hasCog;

	private Vector2 _cogPosition;

	/// <summary>The x position of the riding platform, in original pixels.</summary>
	private int _x;

	private bool _goingLeft;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The platform slides right instead of left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>The x position of the original placement, for the riding platform that stops at fixed positions.</summary>
	[StateVariable]
	private int OriginalX
	{
		get => _originalX;
		set => _originalX = value;
	}

	public MTZLongPlatformInstance() => DesignBounds = new Rectanglei(-0x100 * Scale, -0x40 * Scale, 0x200 * Scale, 0x80 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZLongPlatformType.AnimationGroupResourceKey));
		_cogAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZLongPlatformType.CogAnimationGroupResourceKey));
		_art = (_subtype >> 4) & 3;
		_start = PositionPrecise;
		_x = _originalX;
		if (_art == CogArt)
		{
			return;
		}
		(int width, int height, int distance, int switchMovement) = Properties[_art];
		_distance = distance;
		if ((_subtype & 0x80) != 0)
		{
			_switchIndex = _subtype & 0xF;
			_movement = switchMovement;
			if (_movement == 7)
			{
				_offset = _distance;
			}
			// The cog stays where the platform starts
			_hasCog = true;
			_cogPosition = _start + new Vector2((_flipX ? -0x4C : -0x34) * Scale, 0x14 * Scale);
		}
		else
		{
			_movement = _subtype & 0xF;
		}
		int halfWidth = (width + 5) * Scale;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-halfWidth, -height * Scale, halfWidth * 2, height * 2 * Scale));
	}

	protected override void UpdateMovement()
	{
		if (_art == CogArt)
		{
			return;
		}
		switch (_movement)
		{
			case 1:
				if (!_active && ButtonVineTriggers.For(Level).IsSet(_switchIndex))
				{
					_active = true;
				}
				if (_active)
				{
					SlideOut();
				}
				break;
			case 6:
				if (!_active && --_timer == 0)
				{
					_active = true;
				}
				if (_active)
				{
					SlideOut();
				}
				break;
			case 2:
				if (!_active && --_timer == 0)
				{
					_active = true;
				}
				if (_active)
				{
					SlideBack();
				}
				break;
			case 7:
				if (!_active && ButtonVineTriggers.For(Level).IsSet(_switchIndex))
				{
					_active = true;
				}
				if (_active)
				{
					SlideBack();
				}
				break;
			case 3:
				UpdateNear();
				break;
			case 4:
				if (Level.ObjectManager.Characters.Any(c => c.ObjectLink == this && !c.IsSidekick))
				{
					_movement = 5;
				}
				break;
			case 5:
				Ride();
				break;
		}
		UpdatePosition();
	}

	/// <summary>Slides out 2 pixels each frame. At the end, waits and then slides back (loc_26CC2).</summary>
	private void SlideOut()
	{
		if (_offset != _distance)
		{
			_offset += 2;
			return;
		}
		_movement++;
		_timer = WaitTicks;
		_active = false;
	}

	/// <summary>Slides back 2 pixels each frame. At the end, waits again (loc_26D46).</summary>
	private void SlideBack()
	{
		if (_offset != 0)
		{
			_offset -= 2;
			return;
		}
		_movement--;
		_timer = WaitTicks;
		_active = false;
	}

	/// <summary>Slides out while a character is near, and back when no character is near (loc_26D94).</summary>
	private void UpdateNear()
	{
		int left = (int)_start.X + (_flipX ? -0xA0 : -0x20) * Scale;
		int right = (int)_start.X + (_flipX ? -0x20 : 0x60) * Scale;
		int top = Position.Y - 0x10 * Scale;
		int bottom = Position.Y + 0x40 * Scale;
		bool near = Level.ObjectManager.Characters.Any(c => !c.IsDebug &&
			c.Position.X >= left && c.Position.X < right && c.Position.Y >= top && c.Position.Y < bottom);
		if (near)
		{
			if (_offset != _distance)
			{
				_offset += 0x10;
			}
		}
		else if (_offset != 0)
		{
			_offset -= 0x10;
		}
	}

	/// <summary>Moves right 2 pixels each frame, and turns the free cogs (loc_26E4A).</summary>
	private void Ride()
	{
		if (!_goingLeft)
		{
			_x += 2;
			if (Level.CurrentAct == 3)
			{
				if (_x == Act3StopX1 || _x == Act3StopX2)
				{
					_movement = 0;
				}
			}
			else if (_x == TurnLeftX)
			{
				_goingLeft = true;
			}
		}
		else
		{
			_x -= 2;
			if (_x == TurnRightX)
			{
				_goingLeft = false;
			}
		}
		MTZCogPosition.For(Level).X = _x;
	}

	private void UpdatePosition()
	{
		if (_movement == 0 || _movement == 4 || _movement == 5)
		{
			PositionPrecise = _start + new Vector2((_x - _originalX) * Scale, 0.0);
			return;
		}
		// The short platform (subtype 3) slides $40 pixels, the others $80
		int full = _movement == 3 ? 0x40 : 0x80;
		int slide = _flipX ? full - _offset : _offset;
		PositionPrecise = _start - new Vector2(slide * Scale, 0.0);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (_art == CogArt)
		{
			_cogAnimation.Index = MTZCogPosition.GetFrame(MTZCogPosition.For(Level).X);
			objectRenderer.RenderOriginal(_cogAnimation, _flipX);
			return;
		}
		if (_hasCog)
		{
			// The cog of a platform that slides left faces the other way
			_cogAnimation.Index = MTZCogPosition.GetFrame(_offset);
			objectRenderer.RenderOriginal(_cogAnimation, _cogPosition - PositionPrecise, !_flipX);
		}
		_animation.Index = _art;
		objectRenderer.RenderOriginal(_animation, _flipX);
	}
}
