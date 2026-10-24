using System;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>Helpers for objects that copy Sonic 2: its sine table and a test against the landscape.</summary>
public static class OriginalMath
{
	/// <summary>CalcSine: the sine of a byte angle (256 for each circle, $40 is down), scaled so that 1 is $100.</summary>
	public static int Sin(int angle) => (int)Math.Round(Math.Sin((angle & 0xFF) * Math.PI / 128.0) * 256.0);

	public static int Cos(int angle) => Sin(angle + 0x40);

	/// <summary>True when a line between two points crosses a landscape collision line of the path.</summary>
	public static bool HitsLandscape(Level level, Vector2i a, Vector2i b, int path = 0)
	{
		var bounds = Rectanglei.FromLTRB(Math.Min(a.X, b.X) - 1, Math.Min(a.Y, b.Y) - 1, Math.Max(a.X, b.X) + 1, Math.Max(a.Y, b.Y) + 1);
		foreach (CollisionVector vector in level.CollisionTable.GetPossibleCollisionIntersections(bounds, landscape: true, objects: false))
		{
			if (vector.Owner == null && vector.HasPath(path) && Crosses(a, b, vector.AbsoluteA, vector.AbsoluteB))
			{
				return true;
			}
		}
		return false;
	}

	private static bool Crosses(Vector2i p1, Vector2i p2, Vector2i q1, Vector2i q2)
	{
		long d1 = Cross(q1, q2, p1);
		long d2 = Cross(q1, q2, p2);
		long d3 = Cross(p1, p2, q1);
		long d4 = Cross(p1, p2, q2);
		return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
	}

	private static long Cross(Vector2i a, Vector2i b, Vector2i c) => (long)(b.X - a.X) * (c.Y - a.Y) - (long)(b.Y - a.Y) * (c.X - a.X);
}
