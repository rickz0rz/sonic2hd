using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZLAVABUBBLE;

/// <summary>
/// A lava bubble of Metropolis Zone, as in Sonic 2 (Obj71 with subtype $22). It is scenery. The bubble grows and
/// bursts in frames 0 to 5 (12 frames each), then shows nothing for 128 frames, and starts again (scripts 2 and 3 of
/// Ani_obj71).
/// </summary>
public class MTZLavaBubbleInstance : ActiveObject
{
	private const int Scale = 4;

	private const int EmptyFrame = 6;

	private readonly AnimationScript _script = new AnimationScript(
		new AnimationScript.Script(0xB, new[] { 0, 1, 2, 3, 4, 5 }, 1),
		new AnimationScript.Script(0x7F, new[] { EmptyFrame }, 0));

	private AnimationInstance _animation;

	public MTZLavaBubbleInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, -8 * Scale, 0x20 * Scale, 0x10 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZLavaBubbleType.AnimationGroupResourceKey));
	}

	protected override void OnUpdate()
	{
		_script.Step();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_script.Frame == EmptyFrame)
		{
			return;
		}
		_animation.Index = _script.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
