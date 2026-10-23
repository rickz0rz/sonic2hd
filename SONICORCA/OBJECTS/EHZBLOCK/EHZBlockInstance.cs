// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.EHZBLOCK.EHZBlockInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.EHZBLOCK;

/// <summary>A large solid block in Emerald Hill Zone that floats and moves as a platform.</summary>
public class EHZBlockInstance : Platform
{
	private AnimationInstance _animation;

	public EHZBlockInstance()
	{
		DesignBounds = new Rectanglei(-128, -192, 256, 384);
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-128, -164, 256, 356));
		// The left, right and bottom sides have Id 1. The top keeps Id 0.
		CollisionVectors[0].Id = 1;
		CollisionVectors[2].Id = 1;
		CollisionVectors[3].Id = 1;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
