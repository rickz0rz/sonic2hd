using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZWHEEL;

/// <summary>
/// The wheel at the end of a conveyor belt in Wing Fortress Zone, as in Sonic 2 (ObjBA). It only shows its one
/// mapping frame (32 by 32 pixels). It does not turn and is not solid.
/// </summary>
public class WFZWheelInstance : ActiveObject
{
	private const int Scale = 4;

	private AnimationInstance _animation;

	public WFZWheelInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, -0x10 * Scale, 0x20 * Scale, 0x20 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZWheelType.AnimationGroupResourceKey));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = 0;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
