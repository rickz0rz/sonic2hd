using System;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

public class PointLightSource : ILightSource
{
	public int Intensity { get; set; }

	public Vector2i Position { get; set; }

	public PointLightSource(int intensity, Vector2i position)
	{
		Intensity = intensity;
		Position = position;
	}

	public Vector2i GetShadowOffset(Vector2i occlusionPosition, IShadowInfo shadowInfo)
	{
		int num = 25;
		int num2 = 16;
		int num3 = num;
		int num4 = num * num2;
		Vector2i vector2i = occlusionPosition - Position;
		int length = vector2i.Length;
		Vector2i result = default;
		if (length != 0)
		{
			if (length <= num3)
			{
				int num5 = num * length / num3;
				result.X = vector2i.X * num5 / length;
				result.Y = vector2i.Y * num5 / length;
			}
			else if (length < num4)
			{
				int num6 = (num4 - length) / num2;
				result.X = vector2i.X * num6 / length;
				result.Y = vector2i.Y * num6 / length;
			}
		}
		return result;
	}

	private static int ScaleOffset(int distance, int delta, int max)
	{
		if (delta > 0)
		{
			delta = Math.Max(delta, distance);
			int num = delta / 8;
			return Math.Max(max - num, 0);
		}
		delta = Math.Min(delta, -distance);
		int num2 = delta / 8;
		return Math.Min(-max - num2, 0);
	}

	public override string ToString()
	{
		return $"PointLight ({Position.X}, {Position.Y})";
	}
}
