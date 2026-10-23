using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics.V2.Animation;

public class CompositionLayerAnimatableTransform
{
	private uint _ellapsedFrames;

	private List<CompositionLayerTween> _tweens = new List<CompositionLayerTween>();

	public List<CompositionLayerTween> Tweens => _tweens;

	public double Opacity
	{
		get
		{
			CompositionLayerTween compositionLayerTween = GetCurrentTween(CompositionLayerTween.Type.OPACITY);
			if (compositionLayerTween == null)
			{
				compositionLayerTween = GetFirstTween(CompositionLayerTween.Type.OPACITY);
				if (compositionLayerTween == null)
				{
					compositionLayerTween = GetLastTween(CompositionLayerTween.Type.OPACITY);
				}
			}
			return compositionLayerTween.GetValueSet()["Opacity"];
		}
	}

	public Vector2 Position
	{
		get
		{
			CompositionLayerTween compositionLayerTween = GetCurrentTween(CompositionLayerTween.Type.POSITION);
			if (compositionLayerTween == null)
			{
				compositionLayerTween = GetFirstTween(CompositionLayerTween.Type.POSITION);
				if (compositionLayerTween == null)
				{
					compositionLayerTween = GetLastTween(CompositionLayerTween.Type.POSITION);
				}
			}
			Dictionary<string, double> valueSet = compositionLayerTween.GetValueSet();
			return new Vector2(valueSet["X"], valueSet["Y"]);
		}
	}

	public double Rotation
	{
		get
		{
			CompositionLayerTween compositionLayerTween = GetCurrentTween(CompositionLayerTween.Type.ROTATION);
			if (compositionLayerTween == null)
			{
				compositionLayerTween = GetFirstTween(CompositionLayerTween.Type.ROTATION);
				if (compositionLayerTween == null)
				{
					compositionLayerTween = GetLastTween(CompositionLayerTween.Type.ROTATION);
				}
			}
			return MathX.ToRadians(compositionLayerTween.GetValueSet()["Rotation"]);
		}
	}

	public Vector2 Scale
	{
		get
		{
			CompositionLayerTween compositionLayerTween = GetCurrentTween(CompositionLayerTween.Type.SCALE);
			if (compositionLayerTween == null)
			{
				compositionLayerTween = GetFirstTween(CompositionLayerTween.Type.SCALE);
				if (compositionLayerTween == null)
				{
					compositionLayerTween = GetLastTween(CompositionLayerTween.Type.SCALE);
				}
			}
			Dictionary<string, double> valueSet = compositionLayerTween.GetValueSet();
			return new Vector2(valueSet["X"], valueSet["Y"]);
		}
	}

	private CompositionLayerTween GetCurrentTween(CompositionLayerTween.Type tweenType)
	{
		return (from t in _tweens
				select (t) into t
				where t.TweenType == tweenType && _ellapsedFrames >= t.StartFrame && _ellapsedFrames <= t.EndFrame
				select t).LastOrDefault();
	}

	public CompositionLayerTween GetFirstTween()
	{
		return (from t in _tweens
				select (t) into t
				orderby t.StartFrame
				select t).First();
	}

	private CompositionLayerTween GetFirstTween(CompositionLayerTween.Type tweenType)
	{
		return (from t in _tweens
				select (t) into t
				where t.TweenType == tweenType && _ellapsedFrames <= t.StartFrame
				orderby t.StartFrame
				select t).FirstOrDefault();
	}

	private CompositionLayerTween GetLastTween(CompositionLayerTween.Type tweenType)
	{
		return (from t in _tweens
				select (t) into t
				where t.TweenType == tweenType
				orderby t.EndFrame
				select t).Last();
	}

	public void ResetFrame()
	{
		_ellapsedFrames = 0u;
		foreach (CompositionLayerTween tween in _tweens)
		{
			tween.ResetFrame();
		}
	}

	public void Animate()
	{
		uint num = 0u;
		uint num2 = 0u;
		foreach (CompositionLayerTween tween in _tweens)
		{
			if (_ellapsedFrames <= tween.EndFrame)
			{
				tween.Animate();
			}
			if (tween.StartFrame <= num)
			{
				num = tween.StartFrame;
			}
			if (tween.EndFrame >= num2)
			{
				num2 = tween.EndFrame;
			}
		}
		if (_ellapsedFrames <= num2)
		{
			_ellapsedFrames++;
		}
	}

	public void AddKeyFrameTween(CompositionLayerTween tween)
	{
		_tweens.Add(tween);
	}
}
