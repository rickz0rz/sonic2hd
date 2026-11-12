using System;

namespace SonicOrca.Core;

/// <summary>The curve of a change of the zoom.</summary>
public enum ZoomEasing
{
	/// <summary>The zoom starts slowly, goes faster, and stops slowly: 0.5 - 0.5 cos(pi t).</summary>
	Sine,

	/// <summary>The zoom changes by the same amount on each tick.</summary>
	Linear
}

/// <summary>
/// The zoom of a level. A zoom of 2 shows the level two times larger, and a zoom of 0.5 shows two times more of the
/// level. The HUD keeps its size. A script or the level can change the zoom at once or in a number of ticks.
/// </summary>
public sealed class LevelZoom
{
	/// <summary>The smallest zoom. A smaller zoom draws the tiles too small, and the level is slow to draw.</summary>
	public const double Min = 0.5;

	/// <summary>The largest zoom.</summary>
	public const double Max = 2.0;

	private double _start = 1.0;

	private int _ticks;

	private int _elapsed;

	private ZoomEasing _easing;

	/// <summary>The zoom on this tick.</summary>
	public double Current { get; private set; } = 1.0;

	/// <summary>The zoom at the end of the change, or the current zoom when the zoom does not change.</summary>
	public double Target { get; private set; } = 1.0;

	/// <summary>True while the zoom changes.</summary>
	public bool Changing => _elapsed < _ticks;

	/// <summary>Gives a zoom the limits <see cref="Min"/> and <see cref="Max"/>. A value that is not a number gives 1.</summary>
	public static double Clamp(double zoom)
	{
		return double.IsFinite(zoom) ? Math.Clamp(zoom, Min, Max) : 1.0;
	}

	/// <summary>The part of a change at a time <paramref name="t"/> from 0 to 1, with the curve of the easing.</summary>
	public static double Ease(double t, ZoomEasing easing)
	{
		t = Math.Clamp(t, 0.0, 1.0);
		return easing == ZoomEasing.Linear ? t : 0.5 - 0.5 * Math.Cos(Math.PI * t);
	}

	/// <summary>Sets the zoom at once, and stops a change.</summary>
	public void Set(double zoom)
	{
		Current = Target = Clamp(zoom);
		_start = Current;
		_ticks = _elapsed = 0;
	}

	/// <summary>
	/// Changes the zoom to <paramref name="target"/> in a number of ticks. A change that is not complete starts from
	/// the zoom of this tick, so the zoom does not jump. Zero ticks or less sets the zoom at once.
	/// </summary>
	public void ZoomTo(double target, int ticks, ZoomEasing easing = ZoomEasing.Sine)
	{
		if (ticks <= 0)
		{
			Set(target);
			return;
		}
		_start = Current;
		Target = Clamp(target);
		_ticks = ticks;
		_elapsed = 0;
		_easing = easing;
	}

	/// <summary>Moves the change of the zoom on by one tick.</summary>
	public void Update()
	{
		if (!Changing)
		{
			return;
		}
		_elapsed++;
		Current = _elapsed >= _ticks ? Target : _start + (Target - _start) * Ease(_elapsed / (double)_ticks, _easing);
	}
}
