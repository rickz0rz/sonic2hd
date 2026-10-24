using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZSCENERY;

/// <summary>
/// Scenery of Metropolis Zone, as in Sonic 2 (Obj1C). The object only shows a frame of the bolt end and rope art.
/// Subtype 0 is the bolt end (frame 0), subtype 1 the other bolt end (frame 1), both in palette line 2. Subtype 3 is
/// the rope (frame 2) in palette line 1.
/// </summary>
public class MTZSceneryInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The number of frames in the mappings. The animations of palette line 1 follow those of line 2.</summary>
	private const int FrameCount = 3;

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private bool _flipY;

	/// <summary>The subtype of the original: 0, 1 or 3.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The placement is flipped horizontally.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	/// <summary>The placement is flipped vertically.</summary>
	[StateVariable]
	private bool FlipY
	{
		get => _flipY;
		set => _flipY = value;
	}

	public MTZSceneryInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, -0x10 * Scale, 0x20 * Scale, 0x20 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZSceneryType.AnimationGroupResourceKey));
		_animation.Index = _subtype switch
		{
			1 => 1,
			3 => FrameCount + 2,
			_ => 0
		};
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX, _flipY);
	}
}
