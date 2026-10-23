using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// The animated transform of a composition layer. It holds the opacity, rotation, position and scale tweens
/// and gives the value of each property at the current frame.
/// </summary>
public class CompositionLayerAnimatableTransform
{
	private uint _ellapsedFrames;

	private List<CompositionLayerTween> _tweens = new List<CompositionLayerTween>();

	public List<CompositionLayerTween> Tweens => _tweens;

	public double Opacity => GetTweenForCurrentFrame(CompositionLayerTween.Type.OPACITY).GetValueSet()["Opacity"];

	public Vector2 Position
	{
		get
		{
			Dictionary<string, double> valueSet = GetTweenForCurrentFrame(CompositionLayerTween.Type.POSITION).GetValueSet();
			return new Vector2(valueSet["X"], valueSet["Y"]);
		}
	}

	public double Rotation => MathX.ToRadians(GetTweenForCurrentFrame(CompositionLayerTween.Type.ROTATION).GetValueSet()["Rotation"]);

	public Vector2 Scale
	{
		get
		{
			Dictionary<string, double> valueSet = GetTweenForCurrentFrame(CompositionLayerTween.Type.SCALE).GetValueSet();
			return new Vector2(valueSet["X"], valueSet["Y"]);
		}
	}

	/// <summary>
	/// Gets the tween that gives the value of a property at the current frame. This is the tween that contains the
	/// current frame. Before the first tween it is the next tween, and after the last tween it is the last tween.
	/// </summary>
	private CompositionLayerTween GetTweenForCurrentFrame(CompositionLayerTween.Type tweenType)
	{
		return GetCurrentTween(tweenType) ?? GetFirstTween(tweenType) ?? GetLastTween(tweenType);
	}

	private CompositionLayerTween GetCurrentTween(CompositionLayerTween.Type tweenType)
	{
		return _tweens.Where(t => t.TweenType == tweenType && _ellapsedFrames >= t.StartFrame && _ellapsedFrames <= t.EndFrame).LastOrDefault();
	}

	public CompositionLayerTween GetFirstTween()
	{
		return _tweens.OrderBy(t => t.StartFrame).First();
	}

	private CompositionLayerTween GetFirstTween(CompositionLayerTween.Type tweenType)
	{
		return _tweens.Where(t => t.TweenType == tweenType && _ellapsedFrames <= t.StartFrame).OrderBy(t => t.StartFrame).FirstOrDefault();
	}

	private CompositionLayerTween GetLastTween(CompositionLayerTween.Type tweenType)
	{
		return _tweens.Where(t => t.TweenType == tweenType).OrderBy(t => t.EndFrame).Last();
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
		uint lastEndFrame = 0u;
		foreach (CompositionLayerTween tween in _tweens)
		{
			if (_ellapsedFrames <= tween.EndFrame)
			{
				tween.Animate();
			}
			if (tween.EndFrame >= lastEndFrame)
			{
				lastEndFrame = tween.EndFrame;
			}
		}
		if (_ellapsedFrames <= lastEndFrame)
		{
			_ellapsedFrames++;
		}
	}

	public void AddKeyFrameTween(CompositionLayerTween tween)
	{
		_tweens.Add(tween);
	}
}
