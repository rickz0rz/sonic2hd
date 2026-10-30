using System;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// Changes the colors of the floor of the half pipe for a stage. The floor texture has the colors of stage 1. The
/// palette of a stage has two families of colors for the pipe, of three shades each. A pixel of the texture that has
/// the hue of a family of stage 1 gets the middle shade of the same family of the new stage, at the brightness of the
/// pixel. The other pixels, for example white, yellow and gray, do not change.
/// </summary>
public static class HalfPipeFloorColours
{
	/// <summary>The colors 1 to 6 of a stage palette: the shades of the first family are 0, 2 and 5, and of the second family 1, 3 and 4.</summary>
	private static readonly int[][] Families = { new[] { 0, 2, 5 }, new[] { 1, 3, 4 } };

	/// <summary>The middle shade of each family in the 6 colors.</summary>
	private static readonly int[] MiddleShades = { 2, 3 };

	/// <summary>
	/// A pixel is in a family when its hue is this close to the hue of the family, in degrees. The art of the floor uses a
	/// wide range of each hue, and the two families are far apart.
	/// </summary>
	private const double HueTolerance = 45.0;

	/// <summary>A pixel with less saturation than this is gray or white, and it does not change.</summary>
	private const double MinimumSaturation = 0.25;

	/// <summary>
	/// Changes the pixels from the colors of one stage to the colors of another stage.
	/// </summary>
	/// <param name="pixels">The pixels, with <paramref name="channels"/> bytes each (3 or 4), red first.</param>
	/// <param name="from">The 6 colors of the stage of the texture, as RRGGBB values.</param>
	/// <param name="to">The 6 colors of the new stage.</param>
	public static void Recolour(byte[] pixels, int channels, uint[] from, uint[] to)
	{
		double[] familyHues = new double[Families.Length];
		double[] familyLuma = new double[Families.Length];
		for (int f = 0; f < Families.Length; f++)
		{
			(double r, double g, double b) = ToRgb(from[MiddleShades[f]]);
			familyHues[f] = GetHue(r, g, b);
			familyLuma[f] = Math.Max(1.0, GetLuma(r, g, b));
		}
		for (int i = 0; i + 2 < pixels.Length; i += channels)
		{
			double r = pixels[i], g = pixels[i + 1], b = pixels[i + 2];
			if (GetSaturation(r, g, b) < MinimumSaturation)
			{
				continue;
			}
			double hue = GetHue(r, g, b);
			for (int f = 0; f < Families.Length; f++)
			{
				if (GetHueDistance(hue, familyHues[f]) > HueTolerance)
				{
					continue;
				}
				double scale = GetLuma(r, g, b) / familyLuma[f];
				(double tr, double tg, double tb) = ToRgb(to[MiddleShades[f]]);
				pixels[i] = ToByte(tr * scale);
				pixels[i + 1] = ToByte(tg * scale);
				pixels[i + 2] = ToByte(tb * scale);
				break;
			}
		}
	}

	/// <summary>True when the colors of the pipe of two stages are the same.</summary>
	public static bool AreSame(uint[] a, uint[] b) => a.AsSpan().SequenceEqual(b);

	private static (double R, double G, double B) ToRgb(uint colour) => ((colour >> 16) & 0xFF, (colour >> 8) & 0xFF, colour & 0xFF);

	private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value), 0.0, 255.0);

	private static double GetLuma(double r, double g, double b) => 0.299 * r + 0.587 * g + 0.114 * b;

	private static double GetSaturation(double r, double g, double b)
	{
		double max = Math.Max(r, Math.Max(g, b));
		double min = Math.Min(r, Math.Min(g, b));
		return max <= 0.0 ? 0.0 : (max - min) / max;
	}

	/// <summary>The hue of a color, from 0 to 360 degrees.</summary>
	private static double GetHue(double r, double g, double b)
	{
		double max = Math.Max(r, Math.Max(g, b));
		double min = Math.Min(r, Math.Min(g, b));
		double delta = max - min;
		if (delta <= 0.0)
		{
			return 0.0;
		}
		double hue = max == r ? (g - b) / delta % 6.0 : max == g ? (b - r) / delta + 2.0 : (r - g) / delta + 4.0;
		hue *= 60.0;
		return hue < 0.0 ? hue + 360.0 : hue;
	}

	private static double GetHueDistance(double a, double b)
	{
		double distance = Math.Abs(a - b) % 360.0;
		return distance > 180.0 ? 360.0 - distance : distance;
	}
}
