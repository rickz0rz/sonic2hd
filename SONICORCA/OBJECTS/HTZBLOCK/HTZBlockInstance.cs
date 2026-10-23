using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZBLOCK;

/// <summary>The floating block from Hill Top Zone. It is a solid moving platform that characters can stand on.</summary>
public class HTZBlockInstance : Platform
{
	private AnimationInstance _animation;

	public HTZBlockInstance()
	{
		DesignBounds = new Rectanglei(-128, -192, 256, 384);
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-128, -164, 256, 356));
		// The vectors are left, top, right and bottom. Only the top is not ID 1, and it moves characters with the block.
		CollisionVectors[0].Id = 1;
		CollisionVectors[2].Id = 1;
		CollisionVectors[3].Id = 1;
		CollisionVectors[1].Flags = CollisionFlags.Conveyor;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
