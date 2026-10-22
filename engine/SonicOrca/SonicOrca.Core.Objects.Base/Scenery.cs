using SonicOrca.Extensions;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

public class Scenery : ActiveObject
{
	private readonly string _animationGroupResourceKey;

	private AnimationInstance _animationInstance;

	protected bool AdditiveBlending { get; set; }

	public Scenery(string animationGroupResourceKey)
	{
		_animationGroupResourceKey = animationGroupResourceKey;
	}

	protected override void OnStart()
	{
		_animationInstance = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(_animationGroupResourceKey));
	}

	protected override void OnAnimate()
	{
		_animationInstance.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.BlendMode = ((!AdditiveBlending) ? BlendMode.Alpha : BlendMode.Additive);
		objectRenderer.Render(_animationInstance);
	}
}
