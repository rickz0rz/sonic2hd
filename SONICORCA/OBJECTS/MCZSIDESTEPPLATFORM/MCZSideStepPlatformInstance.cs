using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZSIDESTEPPLATFORM;

/// <summary>
/// A platform that moves to the left and the right, as in Sonic 2 (Obj7A with one platform, subtypes $00 and $12).
/// It moves 1 pixel each frame between $68 pixels to the left and $68 pixels to the right of its position. Subtype
/// $00 starts at the left end, and subtype $12 starts 1 pixel before the right end. Both move to the right first.
/// </summary>
public class MCZSideStepPlatformInstance : Platform
{
	private const int Scale = 4;

	private const int HalfWidth = 0x18 * Scale;

	private const int TopHeight = 8 * Scale;

	/// <summary>The distance from the centre to each end of the path.</summary>
	private const int Range = 0x68;

	private AnimationInstance _animation;

	private bool _startRight;

	private Vector2 _centre;

	/// <summary>The offset from the centre, in original pixels.</summary>
	private int _offset;

	private bool _movingLeft;

	/// <summary>The platform starts at the right end (subtype $12) instead of the left end (subtype $00).</summary>
	[StateVariable]
	private bool StartRight
	{
		get => _startRight;
		set => _startRight = value;
	}

	public MCZSideStepPlatformInstance() => DesignBounds = new Rectanglei(-(Range + 0x18) * Scale, -0x18 * Scale, (Range + 0x18) * 2 * Scale, 0x30 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZSideStepPlatformType.AnimationGroupResourceKey));
		_centre = PositionPrecise;
		_offset = _startRight ? Range - 1 : -Range;
		PositionPrecise = GetPosition();
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
	}

	protected override void UpdateMovement()
	{
		if (_movingLeft)
		{
			if (--_offset == -Range)
			{
				_movingLeft = false;
			}
		}
		else if (++_offset == Range)
		{
			_movingLeft = true;
		}
		PositionPrecise = GetPosition();
	}

	private Vector2 GetPosition() => _centre + new Vector2(_offset * Scale, 0.0);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = 0;
		renderer.GetObjectRenderer().Render(_animation);
	}
}
