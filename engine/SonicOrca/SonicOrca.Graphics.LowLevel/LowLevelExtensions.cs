using SonicOrca.Geometry;

namespace SonicOrca.Graphics.LowLevel;

public static class LowLevelExtensions
{
	public static vec2 ToVec2(this Vector2 v)
	{
		return new vec2
		{
			x = (float)v.X,
			y = (float)v.Y
		};
	}
}
