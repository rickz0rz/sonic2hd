using System;
using System.Collections.Generic;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Lighting;

/// <summary>
/// Keeps the list of light sources in a level and combines their shadow offsets.
/// </summary>
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
		// Find the most negative and the most positive offset on each axis.
		Vector2i minOffset = new Vector2i(int.MaxValue, int.MaxValue);
		Vector2i maxOffset = new Vector2i(int.MinValue, int.MinValue);
		bool hasNegativeX = false;
		bool hasNegativeY = false;
		bool hasPositiveX = false;
		bool hasPositiveY = false;
		foreach (ILightSource lightSource in _lightSources)
		{
			Vector2i shadowOffset = lightSource.GetShadowOffset(forPosition, shadowInfo);
			if (shadowOffset.X < 0)
			{
				minOffset.X = Math.Min(minOffset.X, shadowOffset.X);
				hasNegativeX = true;
			}
			else if (shadowOffset.X > 0)
			{
				maxOffset.X = Math.Max(maxOffset.X, shadowOffset.X);
				hasPositiveX = true;
			}
			if (shadowOffset.Y < 0)
			{
				minOffset.Y = Math.Min(minOffset.Y, shadowOffset.Y);
				hasNegativeY = true;
			}
			else if (shadowOffset.Y > 0)
			{
				maxOffset.Y = Math.Max(maxOffset.Y, shadowOffset.Y);
				hasPositiveY = true;
			}
		}

		// When light comes from the two sides of an axis, use the average of the two extremes.
		Vector2i result = default;
		if (hasNegativeX)
		{
			result.X += minOffset.X;
		}
		if (hasPositiveX)
		{
			result.X += maxOffset.X;
		}
		if (hasNegativeX & hasPositiveX)
		{
			result.X /= 2;
		}
		if (hasNegativeY)
		{
			result.Y += minOffset.Y;
		}
		if (hasPositiveY)
		{
			result.Y += maxOffset.Y;
		}
		// This condition uses hasPositiveX, not hasPositiveY. This is possibly a bug in the original code.
		// Do not change it, because a change can change the shadows on the screen.
		if (hasNegativeY & hasPositiveX)
		{
			result.Y /= 2;
		}
		Vector2i maxShadowOffset = shadowInfo.MaxShadowOffset;
		result.X = MathX.Clamp(-maxShadowOffset.X, result.X, maxShadowOffset.X);
		result.Y = MathX.Clamp(-maxShadowOffset.Y, result.Y, maxShadowOffset.Y);
		return result;
	}
}
