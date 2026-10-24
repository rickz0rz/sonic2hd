using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZSTOMPER;

/// <summary>
/// A stomper, as in Sonic 2 (Obj2A). The solid pillar rises 1 pixel each frame for $60 frames. Then it drops back
/// to its start position at 8 pixels each frame, and rises again.
/// </summary>
public class MCZStomperInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The solid box: width_pixels + $B to each side, $40 up and down.</summary>
	private const int HalfWidth = (0x10 + 0xB) * Scale;

	private const int HalfHeight = 0x40 * Scale;

	/// <summary>The pillar rises this number of pixels before it drops.</summary>
	private const int Rise = 0x60;

	private const int DropSpeed = 8;

	private AnimationInstance _animation;

	private Vector2 _start;

	/// <summary>The distance above the start position, in original pixels.</summary>
	private int _offset;

	private bool _dropping;

	public MCZStomperInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, (-0x50 - Rise) * Scale, 0x40 * Scale, (0xA0 + Rise) * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZStomperType.AnimationGroupResourceKey));
		_start = PositionPrecise;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2));
	}

	protected override void UpdateMovement()
	{
		if (!_dropping)
		{
			if (++_offset == Rise)
			{
				_dropping = true;
			}
		}
		else
		{
			_offset -= DropSpeed;
			if (_offset <= 0)
			{
				_offset = 0;
				_dropping = false;
			}
		}
		PositionPrecise = _start - new Vector2(0.0, _offset * Scale);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
