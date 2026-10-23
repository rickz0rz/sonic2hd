using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZPLATFORM;

/// <summary>The floating platform from Hill Top Zone. Characters can stand on its top surface, and it sags when stood on.</summary>
public class HTZPlatformInstance : Platform
{
	private AnimationInstance _animation;

	public HTZPlatformInstance()
	{
		DesignBounds = new Rectanglei(-128, -64, 256, 128);
		SagWhenStoodOn = true;
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = new CollisionVector[1]
		{
			new CollisionVector(this, new Vector2i(-128, -32), new Vector2i(128, -32), flags: CollisionFlags.Conveyor)
		};
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
