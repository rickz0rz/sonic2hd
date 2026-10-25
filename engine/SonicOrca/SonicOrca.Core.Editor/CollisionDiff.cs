using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Compares the collision lines of a map with a new trace of the whole map, in an area of changed cells. Only the
/// lines of paths 1 and 2 that cross the area change. The other lines stay, also the lines that the user drew by
/// hand in other places. A line that stays keeps its object, so the marks of the layout changes stay.
/// </summary>
public static class CollisionDiff
{
	private const int CellSize = 64;

	private const uint PathMask = 3;

	/// <param name="vectors">The lines of the map.</param>
	/// <param name="traced">A trace of the whole map: each line and its paths (1, 2 or 3).</param>
	/// <param name="cells">The cells that changed. The area has a margin of one cell around them.</param>
	/// <returns>The lines to remove, and the traced lines to add.</returns>
	public static (List<CollisionVector> Removed, List<(Vector2i A, Vector2i B, uint Paths)> Added) Get(
		IEnumerable<CollisionVector> vectors, IReadOnlyCollection<(Vector2i A, Vector2i B, uint Paths)> traced, Rectanglei cells)
	{
		Rectangle area = new Rectangle((cells.X - 1) * CellSize, (cells.Y - 1) * CellSize, (cells.Width + 2) * CellSize, (cells.Height + 2) * CellSize);
		bool Crosses(Vector2i a, Vector2i b) =>
			Math.Max(a.X, b.X) >= area.X && Math.Min(a.X, b.X) <= area.Right && Math.Max(a.Y, b.Y) >= area.Y && Math.Min(a.Y, b.Y) <= area.Bottom;

		CollisionVector[] mapLines = vectors.Where(v => v.Owner == null && (v.Paths & PathMask) != 0).ToArray();
		HashSet<(Vector2i A, Vector2i B, uint Paths)> tracedSet = traced.ToHashSet();
		HashSet<(Vector2i A, Vector2i B, uint Paths)> existing = mapLines.Select(v => (v.RelativeA, v.RelativeB, v.Paths & PathMask)).ToHashSet();
		List<(Vector2i A, Vector2i B, uint Paths)> added = traced.Where(l => Crosses(l.A, l.B) && !existing.Contains(l)).ToList();
		// A line outside the area can be a part of a new line that crosses the area, because the trace joins
		// straight runs. Such a part goes away too.
		List<CollisionVector> removed = mapLines
			.Where(v => !tracedSet.Contains((v.RelativeA, v.RelativeB, v.Paths & PathMask)))
			.Where(v => Crosses(v.RelativeA, v.RelativeB) ||
				added.Any(l => (l.Paths & v.Paths & PathMask) != 0 && IsOnSegment(v.RelativeA, l.A, l.B) && IsOnSegment(v.RelativeB, l.A, l.B)))
			.ToList();
		return (removed, added);
	}

	/// <summary>True when a point is on a segment, with a tolerance of about one unit.</summary>
	private static bool IsOnSegment(Vector2i p, Vector2i a, Vector2i b)
	{
		long cross = (long)(b.X - a.X) * (p.Y - a.Y) - (long)(b.Y - a.Y) * (p.X - a.X);
		return Math.Abs(cross) <= Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y)) &&
			p.X >= Math.Min(a.X, b.X) - 1 && p.X <= Math.Max(a.X, b.X) + 1 && p.Y >= Math.Min(a.Y, b.Y) - 1 && p.Y <= Math.Max(a.Y, b.Y) + 1;
	}
}
