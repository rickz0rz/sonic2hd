using System.Collections.Generic;

namespace SonicOrca.Graphics.V2.Animation;

public class CompositionLayerTween
{
	public enum Type
	{
		OPACITY,
		POSITION,
		ROTATION,
		SCALE
	}

	private bool _hasKeyframes = true;

	private uint _duration;

	private uint _startFrame;

	private uint _endFrame;

	private uint _currentFrame;

	private uint _ellapsedFrames;

	private Dictionary<string, double> _startValues = new Dictionary<string, double>();

	private Dictionary<string, double> _endValues = new Dictionary<string, double>();

	protected List<string> _valueKeys = new List<string>();

	public Type TweenType { get; set; }

	public Dictionary<string, double> StartValues => _startValues;

	public Dictionary<string, double> EndValues => _endValues;

	public List<string> ValueKeys => _valueKeys;

	public uint Duration => _duration;

	public uint CurrentFrame
	{
		get
		{
			return _currentFrame;
		}
		protected set
		{
			_currentFrame = value;
		}
	}

	public uint StartFrame => _startFrame;

	public uint EndFrame => _endFrame;

	public bool HasKeyFrames
	{
		get
		{
			return _hasKeyframes;
		}
		set
		{
			_hasKeyframes = value;
		}
	}

	public CompositionLayerTween(uint startFrame, uint endFrame)
	{
		_startFrame = startFrame;
		_endFrame = endFrame;
		_currentFrame = 0u;
		_duration = 1 + (_endFrame - _startFrame);
	}

	public void ResetFrame()
	{
		_ellapsedFrames = 0u;
		_currentFrame = 0u;
	}

	public void Animate()
	{
		if (_ellapsedFrames < _endFrame)
		{
			_ellapsedFrames++;
		}
		if (_ellapsedFrames >= _startFrame && _currentFrame < _duration)
		{
			_currentFrame++;
		}
	}

	public Dictionary<string, double> GetValueSet()
	{
		Dictionary<string, double> dictionary = new Dictionary<string, double>();
		foreach (string valueKey in ValueKeys)
		{
			double num = EndValues[valueKey] - StartValues[valueKey];
			if (num != 0.0)
			{
				dictionary[valueKey] = StartValues[valueKey] + num / (double)_duration * (double)_currentFrame;
			}
			else
			{
				dictionary[valueKey] = StartValues[valueKey];
			}
		}
		return dictionary;
	}
}
