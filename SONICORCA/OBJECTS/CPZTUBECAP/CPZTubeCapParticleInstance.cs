// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZTUBECAP.CPZTubeCapParticleInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZTUBECAP;

public class CPZTubeCapParticleInstance : ActiveObject
{
	private const int AnimationParticle = 1;
	private AnimationInstance _animation;
	private Vector2 _velocity;

	public Vector2 Velocity
	{
		get => _velocity;
		set => _velocity = value;
	}

	public CPZTubeCapParticleInstance() => DesignBounds = new Rectanglei(-60, -50, 120, 101);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 1);
		_animation.CurrentFrameIndex = Level.Random.Next(0, _animation.Animation.Frames.Count);
		Priority = 1512;
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		_velocity.Y += 9.0 / 16.0;
		PositionPrecise = PositionPrecise + _velocity;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
