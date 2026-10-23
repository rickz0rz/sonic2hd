// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.MASHER.MasherInstance
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
namespace SONICORCA.OBJECTS.MASHER;

public class MasherInstance : Badnik
{
	private const double InitialVelocity = -1.5625;
	private const double LaunchVelocity = -20.0;
	private const double Acceleration = 0.375;
	private const int AnimationNormal = 0;
	private const int AnimationMunching = 1;
	private AnimationInstance _animation;
	private double _initialY;
	private double _velocityY;

	public MasherInstance() => DesignBounds = new Rectanglei(-51, -76, 102, 152);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -48, -64, 96 /*0x60*/, 128 /*0x80*/)
		};
		_initialY = Position.Y;
		_velocityY = -25.0 / 16.0;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		PositionPrecise = PositionPrecise + new Vector2(0.0, _velocityY);
		_velocityY += 0.375;
		if (Position.Y <= _initialY)
			return;
		PositionPrecise = new Vector2(Position.X, _initialY);
		_velocityY = -20.0;
	}

	protected override void OnAnimate()
	{
		_animation.Index = _velocityY >= 0.0 ? 0 : 1;
		_animation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
