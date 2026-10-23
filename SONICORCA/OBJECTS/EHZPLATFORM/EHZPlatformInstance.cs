// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.EHZPLATFORM.EHZPlatformInstance
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
namespace SONICORCA.OBJECTS.EHZPLATFORM;

public class EHZPlatformInstance : Platform
{
	private AnimationInstance _animation;

	public EHZPlatformInstance()
	{
		DesignBounds = new Rectanglei(sbyte.MinValue, -64, 256 /*0x0100*/, 128 /*0x80*/);
		SagWhenStoodOn = true;
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = new CollisionVector[1]
		{
	  new CollisionVector( this, new Vector2i( sbyte.MinValue, -32), new Vector2i(128 /*0x80*/, -32), flags: CollisionFlags.Conveyor)
		};
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
