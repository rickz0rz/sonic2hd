using System;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// A horizontal light source from point <see cref="A"/> to point <see cref="B"/>. It moves the shadows of objects below it up.
/// </summary>
public class VectorLightSource : ILightSource
{
	private const int MaxVerticalOffset = 48;

	public int Intensity { get; set; }

	public Vector2i A { get; set; }

	public Vector2i B { get; set; }

	public VectorLightSource(int intensity, Vector2i a, Vector2i b)
	{
		Intensity = intensity;
		A = a;
		B = b;
	}

	public Vector2i GetShadowOffset(Vector2i occlusionPosition, IShadowInfo shadowInfo)
	{
		Vector2i occlusionSize = shadowInfo.OcclusionSize;
		Rectanglei occlusionBounds = new Rectanglei(occlusionPosition.X - occlusionSize.X, occlusionPosition.Y - occlusionSize.Y, occlusionSize.X * 2, occlusionSize.Y * 2);
		if (occlusionBounds.Top > A.Y)
		{
			return default;
		}

		// The horizontal distance from the light segment makes the offset smaller.
		int horizontalDistance = 0;
		if (occlusionBounds.Right < A.X)
		{
			horizontalDistance += A.X - occlusionBounds.Right;
		}
		if (occlusionBounds.Left > B.X)
		{
			horizontalDistance += occlusionBounds.Left - B.X;
		}
		horizontalDistance /= 4;
		int verticalDistance = Math.Max(0, A.Y - occlusionPosition.Y) / 16;
		int offsetY = -Math.Max(0, MaxVerticalOffset - verticalDistance);
		offsetY = Math.Min(0, offsetY + horizontalDistance);
		return new Vector2i(0, offsetY);
	}
}
