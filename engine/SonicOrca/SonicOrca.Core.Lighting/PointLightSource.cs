using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// A light source at one point. The shadow offset increases near the light and decreases with distance.
/// </summary>
public class PointLightSource : ILightSource
{
	/// <summary>The largest shadow offset, in pixels.</summary>
	private const int MaxOffset = 25;

	/// <summary>The distance for each pixel of offset decrease, after the peak distance.</summary>
	private const int FalloffScale = 16;

	/// <summary>The distance where the offset is at its maximum.</summary>
	private const int PeakDistance = MaxOffset;

	/// <summary>The distance where the offset becomes zero.</summary>
	private const int CutoffDistance = MaxOffset * FalloffScale;

	public int Intensity { get; set; }

	public Vector2i Position { get; set; }

	public PointLightSource(int intensity, Vector2i position)
	{
		Intensity = intensity;
		Position = position;
	}

	public Vector2i GetShadowOffset(Vector2i occlusionPosition, IShadowInfo shadowInfo)
	{
		Vector2i delta = occlusionPosition - Position;
		int length = delta.Length;
		Vector2i result = default;
		if (length != 0)
		{
			if (length <= PeakDistance)
			{
				int magnitude = MaxOffset * length / PeakDistance;
				result.X = delta.X * magnitude / length;
				result.Y = delta.Y * magnitude / length;
			}
			else if (length < CutoffDistance)
			{
				int magnitude = (CutoffDistance - length) / FalloffScale;
				result.X = delta.X * magnitude / length;
				result.Y = delta.Y * magnitude / length;
			}
		}
		return result;
	}

	public override string ToString()
	{
		return $"PointLight ({Position.X}, {Position.Y})";
	}
}
