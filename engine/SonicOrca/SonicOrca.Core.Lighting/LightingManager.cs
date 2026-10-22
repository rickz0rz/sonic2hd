using System;
using System.Collections.Generic;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

internal class LightingManager : ILightingManager
{
	private readonly LinkedList<ILightSource> _lightSources = new LinkedList<ILightSource>();

	public void RegisterLightSource(ILightSource lightSource)
	{
		_lightSources.AddLast(lightSource);
	}

	public void UnregisterLightSource(ILightSource lightSource)
	{
		_lightSources.Remove(lightSource);
	}

	public Vector2i GetShadowOffset(Vector2i forPosition, IShadowInfo shadowInfo)
	{
		Vector2i vector2i = new Vector2i(int.MaxValue, int.MaxValue);
		Vector2i vector2i2 = new Vector2i(int.MinValue, int.MinValue);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		foreach (ILightSource lightSource in _lightSources)
		{
			Vector2i shadowOffset = lightSource.GetShadowOffset(forPosition, shadowInfo);
			if (shadowOffset.X < 0)
			{
				vector2i.X = Math.Min(vector2i.X, shadowOffset.X);
				flag = true;
			}
			else if (shadowOffset.X > 0)
			{
				vector2i2.X = Math.Max(vector2i2.X, shadowOffset.X);
				flag3 = true;
			}
			if (shadowOffset.Y < 0)
			{
				vector2i.Y = Math.Min(vector2i.Y, shadowOffset.Y);
				flag2 = true;
			}
			else if (shadowOffset.Y > 0)
			{
				vector2i2.Y = Math.Max(vector2i2.Y, shadowOffset.Y);
				flag4 = true;
			}
		}
		Vector2i result = default;
		if (flag)
		{
			result.X += vector2i.X;
		}
		if (flag3)
		{
			result.X += vector2i2.X;
		}
		if (flag & flag3)
		{
			result.X /= 2;
		}
		if (flag2)
		{
			result.Y += vector2i.Y;
		}
		if (flag4)
		{
			result.Y += vector2i2.Y;
		}
		if (flag2 & flag3)
		{
			result.Y /= 2;
		}
		Vector2i maxShadowOffset = shadowInfo.MaxShadowOffset;
		result.X = MathX.Clamp(-maxShadowOffset.X, result.X, maxShadowOffset.X);
		result.Y = MathX.Clamp(-maxShadowOffset.Y, result.Y, maxShadowOffset.Y);
		return result;
	}

	private static int UpdateShadowOffset(int globalOffset, int localOffset)
	{
		int num = globalOffset;
		if (num < 0)
		{
			num = Math.Min(num, localOffset);
			return Math.Min(num, 0);
		}
		num = Math.Max(num, localOffset);
		return Math.Max(num, 0);
	}
}
