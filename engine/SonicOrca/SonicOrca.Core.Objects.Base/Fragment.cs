using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

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
			objectRenderer.BlendMode = ((!AdditiveBlending) ? BlendMode.Alpha : BlendMode.Additive);
			objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(Angle);
			Rectanglei source = _animationInstance.CurrentFrame.Source;
			int num = (int)(source.Width * Scale);
			int num2 = (int)(source.Height * Scale);
			Rectanglei rectanglei = new Rectanglei(-num / 2, -num2 / 2, num, num2);
			objectRenderer.Texture = _animationInstance.CurrentTexture;
			objectRenderer.Render(source, rectanglei, FlipX, FlipY);
		}
	}
}
