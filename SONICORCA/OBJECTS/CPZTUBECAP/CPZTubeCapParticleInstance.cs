using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CPZTUBECAP;

/// <summary>A piece of a broken tube cap. It flies out and falls with gravity, then disappears.</summary>
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
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), AnimationParticle);
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
