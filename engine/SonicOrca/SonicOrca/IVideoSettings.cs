using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca;

/// <summary>
/// The video settings that the user selected.
/// </summary>
public interface IVideoSettings
{
	VideoMode Mode { get; }

	bool EnableShadows { get; }

	bool EnableHeatEffects { get; }

	bool EnableWaterEffects { get; }

	/// <summary>
	/// The size of the HUD: 1 is the normal size, from <see cref="HudScale.Min"/> to <see cref="HudScale.Max"/>. It
	/// changes the HUD of a level and the ring counters of a special stage.
	/// </summary>
	double HudScale { get; }
}

/// <summary>The limits and the steps of <see cref="IVideoSettings.HudScale"/>.</summary>
public static class HudScale
{
	public const double Min = 0.75;

	public const double Max = 1.5;

	public const double Step = 0.05;

	/// <summary>The nearest size of a step inside the limits. A value that is not a number gives 1.</summary>
	public static double Clamp(double scale)
	{
		if (!double.IsFinite(scale))
		{
			return 1.0;
		}
		return System.Math.Round(System.Math.Clamp(scale, Min, Max) / Step) * Step;
	}

	/// <summary>
	/// The model matrix that draws a group of the HUD at <paramref name="scale"/> about an anchor, for example the
	/// top left corner of the screen. The anchor stays in its place.
	/// </summary>
	public static Matrix4 About(Matrix4 model, double scale, double anchorX, double anchorY)
	{
		if (scale == 1.0)
		{
			return model;
		}
		return model * Matrix4.CreateTranslation(anchorX, anchorY) * Matrix4.CreateScale(scale, scale) * Matrix4.CreateTranslation(-anchorX, -anchorY);
	}
}
