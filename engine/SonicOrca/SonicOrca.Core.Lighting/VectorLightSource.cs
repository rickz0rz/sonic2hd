using System;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

public class VectorLightSource : ILightSource
{
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
		Rectanglei rectanglei = new Rectanglei(occlusionPosition.X - occlusionSize.X, occlusionPosition.Y - occlusionSize.Y, occlusionSize.X * 2, occlusionSize.Y * 2);
		if (rectanglei.Top > A.Y)
		{
			return default;
		}
		int num = 0;
		if (rectanglei.Right < A.X)
		{
			num += A.X - rectanglei.Right;
		}
		if (rectanglei.Left > B.X)
		{
			num += rectanglei.Left - B.X;
		}
		num /= 4;
		int num2 = Math.Max(0, A.Y - occlusionPosition.Y) / 16;
		int num3 = -Math.Max(0, 48 - num2);
		num3 = Math.Min(0, num3 + num);
		return new Vector2i(0, num3);
	}
}
