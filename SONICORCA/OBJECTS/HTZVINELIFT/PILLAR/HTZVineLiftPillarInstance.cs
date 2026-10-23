using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZVINELIFT.PILLAR;

/// <summary>The pillar at the top or the bottom end of a vine lift path in Hill Top Zone. It is scenery only.</summary>
public class HTZVineLiftPillarInstance : ActiveObject
{
	private const int TopAnimationIndex = 2;
	private const int BottomAnimationIndex = 3;
	private AnimationInstance _animation;
	private bool _flipX;
	private bool _bottom;

	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	[StateVariable]
	private bool Bottom
	{
		get => _bottom;
		set => _bottom = value;
	}

	public HTZVineLiftPillarInstance()
	{
		DesignBounds = new Rectanglei(-64, -170, 128, 340);
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("//ANIGROUP"), _bottom ? BottomAnimationIndex : TopAnimationIndex);
		Priority = -256;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, _flipX);
	}
}
