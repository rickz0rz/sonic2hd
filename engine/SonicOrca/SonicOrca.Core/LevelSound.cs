using System;
using SonicOrca.Audio;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

public class LevelSound : IDisposable
{
	private readonly Level _level;

	private readonly SampleInstance _instance;

	private readonly bool _autoFinish;

	private bool _paused;

	public Vector2i Position { get; set; }

	public bool Finished { get; private set; }

	public int DistanceAudible { get; set; } = 1000;

	public LevelSound(Level level, Sample sample, Vector2i position = default(Vector2i), bool autoFinish = true)
	{
		_level = level;
		_instance = new SampleInstance(_level.GameContext, sample);
		_instance.Classification = SampleInstanceClassification.Sound;
		_autoFinish = autoFinish;
		Position = position;
	}

	public LevelSound(Level level, SampleInfo sampleInfo, Vector2i position = default(Vector2i), bool autoFinish = true)
	{
		_level = level;
		_instance = new SampleInstance(_level.GameContext, sampleInfo);
		_instance.Classification = SampleInstanceClassification.Sound;
		_autoFinish = autoFinish;
		Position = position;
	}

	public void Dispose()
	{
		_instance.Dispose();
		Finished = true;
	}

	public void Update()
	{
		UpdatePanAndVolume();
		if (_autoFinish && !_instance.Playing)
		{
			Finished = true;
		}
	}

	private void UpdatePanAndVolume()
	{
		Camera camera = _level.Camera;
		double num = camera.Bounds.X + camera.Bounds.Width / 2.0;
		double num2 = (double)Position.X - num;
		if (Math.Abs(num2) < 512.0)
		{
			_instance.Pan = 0.0;
		}
		else
		{
			_instance.Pan = MathX.Clamp(-1.0, num2 / camera.Bounds.Width, 1.0);
		}
		double num3 = 0.0;
		if ((double)Position.X < camera.Bounds.X)
		{
			num3 = camera.Bounds.X - (double)Position.X;
		}
		else if ((double)Position.X > camera.Bounds.Right)
		{
			num3 = (double)Position.X - camera.Bounds.Right;
		}
		_instance.Volume = MathX.Clamp(0.0, 1.0 - num3 / (double)DistanceAudible, 1.0);
	}

	public void Play()
	{
		_instance.Play();
	}

	public void Stop()
	{
		_instance.Stop();
	}

	public void Pause()
	{
		if (_instance.Playing)
		{
			_instance.Stop();
			_paused = true;
		}
	}

	public void Resume()
	{
		if (_paused)
		{
			_paused = false;
			_instance.Play();
		}
	}
}
