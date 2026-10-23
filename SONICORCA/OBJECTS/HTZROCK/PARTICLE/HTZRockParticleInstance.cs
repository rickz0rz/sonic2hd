using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZROCK.PARTICLE;

/// <summary>
/// A rock fragment from Hill Top Zone. It starts with a velocity that the object that creates it gives, falls with
/// gravity and has a random frame and size.
/// </summary>
public class HTZRockParticleInstance : ActiveObject
{
	private const int AnimationRockParticle = 1;
	private const double Gravity = 9.0 / 16.0;
	private AnimationInstance _animation;
	private Vector2 _velocity;
	private double _scale;

	[StateVariable]
	private Vector2 Velocity
	{
		get => _velocity;
		set => _velocity = value;
	}

	public HTZRockParticleInstance() => DesignBounds = new Rectanglei(-60, -60, 120, 120);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("//ANIGROUP"), AnimationRockParticle);
		_animation.CurrentFrameIndex = Level.Random.Next(0, _animation.Animation.Frames.Count);
		_scale = Level.Random.NextDouble() * 0.5 + 0.5;
		Priority = 1512;
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		_velocity.Y += Gravity;
		PositionPrecise += _velocity;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			objectRenderer.ModelMatrix *= Matrix4.CreateScale(_scale, _scale);
			objectRenderer.Render(_animation);
		}
	}
}
