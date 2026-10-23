using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// A piece of debris that falls with gravity and turns. It removes itself after a number of animation cycles.
/// </summary>
public class Fragment : ActiveObject
{
	private AnimationInstance _animationInstance;

	public AnimationGroup AnimationGroup { get; set; }

	public string AnimationGroupResourceKey { get; set; }

	public int AnimationIndex { get; set; }

	public int AnimationCycles { get; set; }

	protected bool AdditiveBlending { get; set; }

	protected double FilterMultiplier { get; set; }

	public bool FlipX { get; set; }

	public bool FlipY { get; set; }

	public double Angle { get; set; }

	public double Scale { get; set; }

	public double AngularVelocity { get; set; }

	public Vector2 Velocity { get; set; }

	public double Gravity { get; set; }

	public Fragment()
	{
		FilterMultiplier = 1.0;
		Gravity = 0.875;
		Scale = 1.0;
	}

	protected override void OnStart()
	{
		Priority = 2048;
	}

	protected override void OnStop()
	{
		FinishForever();
	}

	public void Initialise()
	{
		if (AnimationGroup == null)
		{
			AnimationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(AnimationGroupResourceKey);
		}
		_animationInstance = new AnimationInstance(AnimationGroup, AnimationIndex);
	}

	protected override void OnUpdate()
	{
		if (_animationInstance == null)
		{
			Initialise();
		}
		if (AnimationCycles != 0 && _animationInstance.Cycles >= AnimationCycles)
		{
			FinishForever();
			return;
		}
		MovePrecise(Velocity);
		Velocity += new Vector2(0.0, Gravity);
		Angle += AngularVelocity;
	}

	protected override void OnAnimate()
	{
		if (_animationInstance != null)
		{
			_animationInstance.Animate();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_animationInstance != null && (AnimationCycles == 0 || _animationInstance.Cycles < AnimationCycles))
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			if (FilterMultiplier == 0.0)
			{
				objectRenderer.Filter = 0;
			}
			else
			{
				objectRenderer.FilterAmount *= FilterMultiplier;
			}
			objectRenderer.BlendMode = AdditiveBlending ? BlendMode.Additive : BlendMode.Alpha;
			objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(Angle);
			Rectanglei source = _animationInstance.CurrentFrame.Source;
			int width = (int)(source.Width * Scale);
			int height = (int)(source.Height * Scale);
			Rectanglei destination = new Rectanglei(-width / 2, -height / 2, width, height);
			objectRenderer.Texture = _animationInstance.CurrentTexture;
			objectRenderer.Render(source, destination, FlipX, FlipY);
		}
	}
}
