using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZROTATINGPLATFORM;

/// <summary>
/// One of three platforms that turn around a centre, as in Sonic 2 (Obj83). The importer places three of these on
/// the same centre, a third of a circle apart. Each platform is 64 pixels from the centre, on an arm of 3 links. The
/// angle grows by the speed each frame (256 units for each circle). The angle comes from the level time, so the three
/// platforms always agree.
/// </summary>
public class ARZRotatingPlatformInstance : Platform
{
	private const int Scale = 4;

	/// <summary>PlatformObject uses $20 + $B pixels from the centre, and the top is 9 pixels up.</summary>
	private const int HalfWidth = 0x2B * Scale;

	private const int TopHeight = 9 * Scale;

	private const int Radius = 0x40;

	private const int PlatformFrame = 0;

	private const int LinkFrame = 1;

	private AnimationInstance _animation;

	private int _arm;

	private int _speed;

	private int _startAngle;

	private Vector2 _centre;

	/// <summary>The arm: 0, 1 ($55 ahead) or 2 ($55 behind).</summary>
	[StateVariable]
	private int Arm
	{
		get => _arm;
		set => _arm = value;
	}

	/// <summary>The change of the angle each frame, in 1/256 angle units (subtype bits 4-7, times 8).</summary>
	[StateVariable]
	private int Speed
	{
		get => _speed;
		set => _speed = value;
	}

	/// <summary>The angle at the start (from the flips of the original).</summary>
	[StateVariable]
	private int StartAngle
	{
		get => _startAngle;
		set => _startAngle = value;
	}

	public ARZRotatingPlatformInstance() => DesignBounds = new Rectanglei(-(Radius + 0x30) * Scale, -(Radius + 0x30) * Scale, (Radius + 0x30) * 2 * Scale, (Radius + 0x30) * 2 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZRotatingPlatformType.AnimationGroupResourceKey));
		_centre = PositionPrecise;
		PositionPrecise = PointOnArm(Radius);
		CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-HalfWidth, -TopHeight), new Vector2i(HalfWidth, -TopHeight)) };
	}

	private int Angle
	{
		get
		{
			int armOffset = _arm == 1 ? 0x55 : _arm == 2 ? -0x55 : 0;
			return ((((_startAngle << 8) + _speed * Level.Ticks) >> 8) + armOffset) & 0xFF;
		}
	}

	private Vector2 PointOnArm(int distance)
	{
		int angle = Angle;
		return _centre + new Vector2(distance * OriginalMath.Cos(angle) / 256.0 * Scale, distance * OriginalMath.Sin(angle) / 256.0 * Scale);
	}

	protected override void UpdateMovement()
	{
		PositionPrecise = PointOnArm(Radius);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = LinkFrame;
		for (int i = 1; i <= 3; i++)
		{
			objectRenderer.Render(_animation, PointOnArm(i * 0x10) - PositionPrecise);
		}
		_animation.Index = PlatformFrame;
		objectRenderer.Render(_animation);
	}
}
