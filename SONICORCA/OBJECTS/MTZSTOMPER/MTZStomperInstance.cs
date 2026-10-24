using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZSTOMPER;

/// <summary>
/// A twin stomper, as in Sonic 2 (Obj64 in Metropolis Zone). Subtype bits 4-6 select the art: 0 is a wide bar on two
/// pillars, 1 is a tall block. Subtype bits 0-3 select the movement: 0 stays, 1 moves. The moving stomper goes down
/// $40 pixels at 8 pixels each frame, waits $5B frames, goes back up at 8 pixels each frame and waits again. A
/// flipped stomper starts $40 pixels down and goes up first. Only the bar (or the block) is solid: the pillars are
/// not.
/// </summary>
public class MTZStomperInstance : Platform
{
	private const int Scale = 4;

	private const int Speed = 8;

	/// <summary>The stomper waits $5A + 1 frames at each end.</summary>
	private const int WaitTicks = 0x5A;

	/// <summary>Obj64_Properties: width_pixels, the half height of the solid box and the distance, by art.</summary>
	private static readonly (int Width, int Height, int Distance)[] Properties =
	{
		(0x40, 0xC, 0x40),
		(0x10, 0x20, 0x40),
	};

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private bool _flipY;

	private int _frame;

	private int _distance;

	private Vector2 _start;

	/// <summary>The distance below the start position, in original pixels (objoff_3A).</summary>
	private int _offset;

	private bool _goingDown;

	private int _timer;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The stomper starts down and goes up first.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>The art is upside down: the pillars go up.</summary>
	[StateVariable]
	private bool FlipY
	{
		get => _flipY;
		set => _flipY = value;
	}

	public MTZStomperInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x70 * Scale, 0x80 * Scale, 0x120 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZStomperType.AnimationGroupResourceKey));
		_frame = (_subtype >> 4) & 7;
		if (_frame >= Properties.Length)
		{
			_frame = 0;
		}
		(int width, int height, int distance) = Properties[_frame];
		_distance = distance;
		_start = PositionPrecise;
		int halfWidth = (width + 0xB) * Scale;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-halfWidth, -height * Scale, halfWidth * 2, height * 2 * Scale));
		PositionPrecise = _start + new Vector2(0.0, Depth * Scale);
	}

	/// <summary>The distance below the start position, with the flip.</summary>
	private int Depth => _flipX ? 0x40 - _offset : _offset;

	protected override void UpdateMovement()
	{
		if ((_subtype & 0xF) == 1)
		{
			Move();
		}
		PositionPrecise = _start + new Vector2(0.0, Depth * Scale);
	}

	/// <summary>The movement of subtype 1 (loc_269FA).</summary>
	private void Move()
	{
		if (!_goingDown)
		{
			if (_offset != 0)
			{
				_offset -= Speed;
				return;
			}
			if (--_timer >= 0)
			{
				return;
			}
			_timer = WaitTicks;
			_goingDown = true;
		}
		if (_offset != _distance)
		{
			_offset += Speed;
		}
		else if (--_timer < 0)
		{
			_timer = WaitTicks;
			_goingDown = false;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX, _flipY);
	}
}
