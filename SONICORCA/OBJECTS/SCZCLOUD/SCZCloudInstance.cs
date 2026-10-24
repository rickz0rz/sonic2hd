using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SCZCLOUD;

/// <summary>
/// A cloud of Sky Chase Zone, as in Sonic 2 (ObjB3). The kind (subtype $5E, $60 or $62) gives the frame (big, middle
/// or small) and the speed to the left ($80, $40 or $20). The cloud also moves with the x part of the Tornado
/// velocity, but not with the y part. It is drawn behind the other objects, and goes when it leaves the screen on
/// the left.
/// </summary>
public class SCZCloudInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>Speeds in 1/256 original pixels for each frame, at four times the resolution.</summary>
	private const double Unit = Scale / 256.0;

	private static readonly int[] Speeds = { -0x80, -0x40, -0x20 };

	private AnimationInstance _animation;

	private SkyChaseScroll _scroll;

	private int _kind;

	/// <summary>The kind: 0 (big), 1 (middle) or 2 (small), from the subtype ($5E + kind * 2).</summary>
	[StateVariable]
	private int Kind
	{
		get => _kind;
		set => _kind = value;
	}

	public SCZCloudInstance() => DesignBounds = new Rectanglei(-0x30 * Scale, -0x10 * Scale, 0x60 * Scale, 0x20 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SCZCloudType.AnimationGroupResourceKey));
		_scroll = SkyChaseScroll.For(Level);
		_kind = System.Math.Clamp(_kind, 0, Speeds.Length - 1);
		// Priority 6 of the original: behind the other objects
		Priority = 16;
	}

	protected override void OnUpdate()
	{
		PositionPrecise += new Vector2(Speeds[_kind] * Unit + _scroll.Velocity.X, 0.0);
		if (Position.X + DesignBounds.Right < Level.Camera.Bounds.Left)
		{
			Finish();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _kind;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
