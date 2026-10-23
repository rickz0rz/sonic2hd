using System;
using SonicOrca.Audio;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>
/// A sound that plays at a position in the level.
/// The pan and volume change with the position of the sound relative to the camera.
/// </summary>
public class LevelSound : IDisposable
{
	/// <summary>
	/// Sounds nearer than this horizontal distance to the camera center play with no pan.
	/// </summary>
	private const double CenterPanDistance = 512.0;

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
		double cameraCenterX = camera.Bounds.X + camera.Bounds.Width / 2.0;
		double offsetFromCenterX = Position.X - cameraCenterX;
		if (Math.Abs(offsetFromCenterX) < CenterPanDistance)
		{
			_instance.Pan = 0.0;
		}
		else
		{
			_instance.Pan = MathX.Clamp(-1.0, offsetFromCenterX / camera.Bounds.Width, 1.0);
		}

		// The volume decreases with the horizontal distance outside the camera bounds.
		double distanceOutsideCamera = 0.0;
		if (Position.X < camera.Bounds.X)
		{
			distanceOutsideCamera = camera.Bounds.X - Position.X;
		}
		else if (Position.X > camera.Bounds.Right)
		{
			distanceOutsideCamera = Position.X - camera.Bounds.Right;
		}
		_instance.Volume = MathX.Clamp(0.0, 1.0 - distanceOutsideCamera / DistanceAudible, 1.0);
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
