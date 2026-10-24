using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

/// <summary>
/// Makes collision lines from a grid of solid pixels, as the collision of the original game gives it. The lines go
/// around the solid areas in the clockwise direction (with Y down), so the solid side is on the right of each line,
/// as SonicOrca expects. The top edges of a pixel come from its top solidity. The side and bottom edges come from
/// its side solidity, so a platform that is solid only from the top gives only its top surface.
/// <para>
/// A slope in a height map is a staircase of pixel edges. Each straight run of edges is replaced by its midpoint,
/// so the staircase becomes a straight line. A corner stays sharp only where both runs next to it are at least two
/// pixels long. Then the points are simplified. The result has no small steps, which a character would trip on.
/// </para>
/// </summary>
public static class CollisionTracer
{
	/// <summary>The pixel is solid from the top: floors.</summary>
	public const byte TopSolid = 1;

	/// <summary>The pixel is solid from the left, the right and the bottom: walls and ceilings.</summary>
	public const byte SidesSolid = 2;

	/// <param name="grid">The solidity of each pixel, row by row: a combination of <see cref="TopSolid"/> and <see cref="SidesSolid"/>.</param>
	/// <param name="scale">The units of the result for each pixel. It must be even, because midpoints are half pixels.</param>
	/// <param name="tolerance">The largest distance in pixels between a line and the points that it replaces.</param>
	/// <returns>The lines, in pixels multiplied by <paramref name="scale"/>.</returns>
	public static List<(Vector2i A, Vector2i B)> Trace(byte[] grid, int width, int height, int scale = 2, double tolerance = 0.75)
	{
		if (scale <= 0 || scale % 2 != 0)
		{
			throw new ArgumentException("The scale must be even.", nameof(scale));
		}
		if (grid.Length != width * height)
		{
			throw new ArgumentException("The grid does not have width * height pixels.", nameof(grid));
		}
		var edges = new EdgeGraph(width + 1);
		bool Solid(int x, int y) => x >= 0 && y >= 0 && x < width && y < height && grid[y * width + x] != 0;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				byte flags = grid[y * width + x];
				if (flags == 0)
				{
					continue;
				}
				if ((flags & TopSolid) != 0 && !Solid(x, y - 1))
				{
					edges.Add(x, y, x + 1, y);
				}
				if ((flags & SidesSolid) == 0)
				{
					continue;
				}
				if (!Solid(x + 1, y))
				{
					edges.Add(x + 1, y, x + 1, y + 1);
				}
				if (!Solid(x, y + 1))
				{
					edges.Add(x + 1, y + 1, x, y + 1);
				}
				if (!Solid(x - 1, y))
				{
					edges.Add(x, y + 1, x, y);
				}
			}
		}
		var lines = new List<(Vector2i A, Vector2i B)>();
		int half = scale / 2;
		foreach (List<Vector2i> chain in edges.GetChains())
		{
			// Work in half pixels, so that midpoints are whole numbers
			List<Vector2i> points = Simplify(ToRunPoints(chain), tolerance * 2.0);
			for (int i = 0; i + 1 < points.Count; i++)
			{
				lines.Add((points[i] * half, points[i + 1] * half));
			}
		}
		return lines;
	}

	/// <summary>
	/// Replaces the corners of a chain of unit edges by the midpoints of its straight runs, except the corners
	/// between two runs of two pixels or more. The result is in half pixels.
	/// </summary>
	private static List<Vector2i> ToRunPoints(List<Vector2i> chain)
	{
		bool closed = chain.Count > 2 && chain[0] == chain[^1];
		// The corners where the direction changes, so each run goes from runs[i] to runs[i + 1]
		var corners = new List<Vector2i> { chain[0] };
		for (int i = 1; i + 1 < chain.Count; i++)
		{
			if (chain[i] - chain[i - 1] != chain[i + 1] - chain[i])
			{
				corners.Add(chain[i]);
			}
		}
		corners.Add(chain[^1]);
		if (closed && corners.Count > 2 && corners[1] - corners[0] is var first && corners[^1] - corners[^2] is var last &&
			Math.Sign(first.X) == Math.Sign(last.X) && Math.Sign(first.Y) == Math.Sign(last.Y))
		{
			// The start of a closed chain is in the middle of a run. Start at the next corner instead.
			corners.RemoveAt(corners.Count - 1);
			corners.RemoveAt(0);
			corners.Add(corners[0]);
		}
		int Length(int run) => Math.Abs(corners[run + 1].X - corners[run].X) + Math.Abs(corners[run + 1].Y - corners[run].Y);
		int runs = corners.Count - 1;
		var points = new List<Vector2i>();
		int firstCorner = -1;
		if (!closed)
		{
			points.Add(corners[0] * 2);
		}
		for (int run = 0; run < runs; run++)
		{
			points.Add(corners[run] + corners[run + 1]);
			// The corner at the end of this run, unless it is the end of an open chain
			int next = run + 1;
			if (next == runs)
			{
				if (!closed)
				{
					break;
				}
				next = 0;
			}
			if (Length(run) >= 2 && Length(next) >= 2)
			{
				if (firstCorner < 0)
				{
					firstCorner = points.Count;
				}
				points.Add(corners[run + 1] * 2);
			}
		}
		if (closed)
		{
			// Start a closed chain at a sharp corner, so that the simplification does not keep a midpoint as a corner
			if (firstCorner > 0)
			{
				points = points.Skip(firstCorner).Concat(points.Take(firstCorner)).ToList();
			}
			points.Add(points[0]);
		}
		else
		{
			points.Add(corners[^1] * 2);
		}
		return points;
	}

	/// <summary>Removes the points that are nearer than the tolerance to the line through their neighbours (Douglas-Peucker).</summary>
	private static List<Vector2i> Simplify(List<Vector2i> chain, double tolerance)
	{
		bool closed = chain.Count > 2 && chain[0] == chain[^1];
		var keep = new bool[chain.Count];
		keep[0] = true;
		keep[^1] = true;
		if (closed)
		{
			// A closed chain starts and ends at the same point. Also keep the point that is farthest from it.
			int farthest = 0;
			long best = -1;
			for (int i = 1; i < chain.Count - 1; i++)
			{
				long dx = chain[i].X - chain[0].X;
				long dy = chain[i].Y - chain[0].Y;
				if (dx * dx + dy * dy > best)
				{
					best = dx * dx + dy * dy;
					farthest = i;
				}
			}
			keep[farthest] = true;
			SimplifyRange(chain, 0, farthest, tolerance, keep);
			SimplifyRange(chain, farthest, chain.Count - 1, tolerance, keep);
		}
		else
		{
			SimplifyRange(chain, 0, chain.Count - 1, tolerance, keep);
		}
		var result = new List<Vector2i>();
		for (int i = 0; i < chain.Count; i++)
		{
			if (keep[i])
			{
				result.Add(chain[i]);
			}
		}
		return result;
	}

	private static void SimplifyRange(List<Vector2i> chain, int first, int last, double tolerance, bool[] keep)
	{
		// An explicit stack, because a long chain can be thousands of points
		var ranges = new Stack<(int First, int Last)>();
		ranges.Push((first, last));
		while (ranges.Count > 0)
		{
			(int a, int b) = ranges.Pop();
			if (b - a < 2)
			{
				continue;
			}
			Vector2i start = chain[a];
			Vector2i end = chain[b];
			double dx = end.X - start.X;
			double dy = end.Y - start.Y;
			double length = Math.Sqrt(dx * dx + dy * dy);
			int farthest = -1;
			double best = tolerance;
			for (int i = a + 1; i < b; i++)
			{
				double px = chain[i].X - start.X;
				double py = chain[i].Y - start.Y;
				double distance = length == 0.0 ? Math.Sqrt(px * px + py * py) : Math.Abs(px * dy - py * dx) / length;
				if (distance > best)
				{
					best = distance;
					farthest = i;
				}
			}
			if (farthest >= 0)
			{
				keep[farthest] = true;
				ranges.Push((a, farthest));
				ranges.Push((farthest, b));
			}
		}
	}

	/// <summary>The unit edges between pixels, joined into chains.</summary>
	private sealed class EdgeGraph
	{
		private readonly int _stride;

		// The one or two edges that start at each corner, and the number of edges that end at each corner
		private readonly Dictionary<long, (long First, long Second)> _outgoing = new Dictionary<long, (long, long)>();

		private readonly Dictionary<long, int> _incoming = new Dictionary<long, int>();

		private const long None = -1;

		public EdgeGraph(int stride)
		{
			_stride = stride;
		}

		private long Key(int x, int y) => (long)y * _stride + x;

		private Vector2i Point(long key) => new Vector2i((int)(key % _stride), (int)(key / _stride));

		public void Add(int x1, int y1, int x2, int y2)
		{
			long from = Key(x1, y1);
			long to = Key(x2, y2);
			if (_outgoing.TryGetValue(from, out (long First, long Second) outs))
			{
				_outgoing[from] = (outs.First, to);
			}
			else
			{
				_outgoing[from] = (to, None);
			}
			_incoming[to] = _incoming.GetValueOrDefault(to) + 1;
		}

		/// <summary>Follows the edges. Open chains come first, from the corners where more edges start than end.</summary>
		public IEnumerable<List<Vector2i>> GetChains()
		{
			var starts = new List<long>();
			foreach (KeyValuePair<long, (long First, long Second)> pair in _outgoing)
			{
				int outCount = pair.Value.Second == None ? 1 : 2;
				if (outCount > _incoming.GetValueOrDefault(pair.Key))
				{
					starts.Add(pair.Key);
				}
			}
			starts.Sort();
			foreach (long start in starts)
			{
				while (_outgoing.ContainsKey(start) && _outgoing[start].First != None)
				{
					yield return Follow(start, None);
				}
			}
			var remaining = new List<long>(_outgoing.Keys);
			remaining.Sort();
			foreach (long start in remaining)
			{
				while (_outgoing.ContainsKey(start) && _outgoing[start].First != None)
				{
					yield return Follow(start, None);
				}
			}
		}

		private List<Vector2i> Follow(long start, long previous)
		{
			var chain = new List<Vector2i> { Point(start) };
			long current = start;
			while (true)
			{
				long next = Take(current, previous);
				if (next == None)
				{
					break;
				}
				chain.Add(Point(next));
				previous = current;
				current = next;
				if (current == start)
				{
					break;
				}
			}
			return chain;
		}

		/// <summary>
		/// Removes and returns an edge that starts at the corner. Where two edges start (two solid pixels that touch
		/// only at the corner), it takes the right turn, so that the chain stays around one of the pixels.
		/// </summary>
		private long Take(long corner, long previous)
		{
			if (!_outgoing.TryGetValue(corner, out (long First, long Second) outs) || outs.First == None)
			{
				return None;
			}
			long chosen = outs.First;
			if (outs.Second != None && previous != None)
			{
				Vector2i here = Point(corner);
				Vector2i incoming = here - Point(previous);
				Vector2i first = Point(outs.First) - here;
				Vector2i second = Point(outs.Second) - here;
				// With Y down, a positive cross product is a turn to the right on the screen
				if (Cross(incoming, second) > Cross(incoming, first))
				{
					chosen = outs.Second;
				}
			}
			if (chosen == outs.First)
			{
				_outgoing[corner] = (outs.Second, None);
			}
			else
			{
				_outgoing[corner] = (outs.First, None);
			}
			if (_outgoing[corner].First == None)
			{
				_outgoing.Remove(corner);
			}
			return chosen;
		}

		private static long Cross(Vector2i a, Vector2i b) => (long)a.X * b.Y - (long)a.Y * b.X;
	}
}
