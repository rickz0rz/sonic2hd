using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The base class for an effect that plays an animation a number of times and then removes itself.
/// </summary>
public class ParticleObject : ActiveObject
{
	private readonly string _animationGroupResourceKey;

	private readonly int _animationIndex;

	private readonly int _animationCycles;

	protected AnimationInstance _animationInstance;

	protected bool AdditiveBlending { get; set; }

	protected double FilterMultiplier { get; set; }

	public bool FlipX { get; set; }

	public bool FlipY { get; set; }

	public ParticleObject(string animationGroupResourceKey, int animationIndex = 0, int animationCycles = 1)
	{
		FilterMultiplier = 1.0;
		_animationGroupResourceKey = animationGroupResourceKey;
		_animationIndex = animationIndex;
		_animationCycles = animationCycles;
	}

	protected override void OnStart()
	{
		_animationInstance = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(_animationGroupResourceKey), _animationIndex);
		Priority = 2048;
	}

	protected override void OnStop()
	{
		FinishForever();
	}

	protected override void OnUpdate()
	{
		if (_animationCycles != 0 && _animationInstance.Cycles >= _animationCycles)
		{
			FinishForever();
		}
	}

	protected override void OnAnimate()
	{
		_animationInstance.Animate();
	}

	protected bool CanDraw()
	{
		return _animationCycles == 0 || _animationInstance.Cycles < _animationCycles;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!CanDraw())
		{
			return;
		}
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (FlipX)
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			}
			if (FlipY)
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateScale(1.0, -1.0);
			}
			if (FilterMultiplier == 0.0)
			{
				objectRenderer.Filter = 0;
			}
			else
			{
				objectRenderer.FilterAmount *= FilterMultiplier;
			}
			objectRenderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
			objectRenderer.Render(_animationInstance);
		}
	}
}
