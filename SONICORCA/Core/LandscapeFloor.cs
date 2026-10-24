using SonicOrca.Core.Collision;
using SonicOrca.Geometry;

namespace SonicOrca.Core;

/// <summary>Finds the landscape floor under a point, as ObjCheckFloorDist of Sonic 2 does for objects.</summary>
public static class LandscapeFloor
{
	private const int Scale = 4;

	/// <summary>The search goes 16 original pixels up (into the floor) and 32 pixels down.</summary>
	private const int SearchUp = 16 * Scale;

	private const int SearchDown = 32 * Scale;

	/// <summary>
	/// The distance in original pixels from a point down to the floor on a path: negative when the point is inside
	/// the floor. Returns null when no floor is near.
	/// </summary>
	public static int? GetDistance(Level level, Vector2i point, int path = 0)
	{
		var search = new Rectanglei(point.X - 1, point.Y - SearchUp, 2, SearchUp + SearchDown);
		double? best = null;
		foreach (CollisionVector vector in level.CollisionTable.GetPossibleCollisionIntersections(search, landscape: true, objects: false))
		{
			Vector2i a = vector.AbsoluteA;
			Vector2i b = vector.AbsoluteB;
			// A floor goes to the right, with the solid side below it
			if (vector.Owner != null || !vector.HasPath(path) || a.X >= b.X || point.X < a.X || point.X > b.X)
			{
				continue;
			}
			double y = a.Y + (point.X - a.X) * (b.Y - a.Y) / (double)(b.X - a.X);
			double distance = y - point.Y;
			if (distance >= -SearchUp && distance <= SearchDown && (best == null || distance < best))
			{
				best = distance;
			}
		}
		return best == null ? null : (int)System.Math.Floor(best.Value / Scale);
	}
}
