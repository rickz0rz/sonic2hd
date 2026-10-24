using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

public sealed class CollisionTracerTests
{
	private const byte Solid = CollisionTracer.TopSolid | CollisionTracer.SidesSolid;

	private static byte[] Grid(int width, int height, System.Func<int, int, byte> pixel)
	{
		var grid = new byte[width * height];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				grid[y * width + x] = pixel(x, y);
			}
		}
		return grid;
	}

	// The lines are in half pixels (scale 2)
	private static HashSet<(Vector2i, Vector2i)> AsSet(List<(Vector2i A, Vector2i B)> lines) => lines.Select(l => (l.A, l.B)).ToHashSet();

	[Fact]
	public void SolidSquare_GivesFourLines_Clockwise()
	{
		byte[] grid = Grid(10, 10, (x, y) => x >= 2 && x < 6 && y >= 3 && y < 7 ? Solid : (byte)0);

		var lines = AsSet(CollisionTracer.Trace(grid, 10, 10, scale: 2));

		// Top left to right, right side down, bottom right to left, left side up: the solid side is on the right
		Assert.Equal(4, lines.Count);
		Assert.Contains((new Vector2i(4, 6), new Vector2i(12, 6)), lines);
		Assert.Contains((new Vector2i(12, 6), new Vector2i(12, 14)), lines);
		Assert.Contains((new Vector2i(12, 14), new Vector2i(4, 14)), lines);
		Assert.Contains((new Vector2i(4, 14), new Vector2i(4, 6)), lines);
	}

	[Fact]
	public void TopSolidPlatform_GivesOnlyItsTop()
	{
		byte[] grid = Grid(10, 10, (x, y) => x >= 1 && x < 9 && y >= 4 && y < 6 ? CollisionTracer.TopSolid : (byte)0);

		var lines = CollisionTracer.Trace(grid, 10, 10, scale: 2);

		Assert.Single(lines);
		Assert.Equal((new Vector2i(2, 8), new Vector2i(18, 8)), lines[0]);
	}

	[Fact]
	public void Slope_IsOneLine_NotAStaircase()
	{
		// A 45 degree slope that goes up to the right: column x is solid from row 16 - x down
		byte[] grid = Grid(16, 17, (x, y) => y >= 16 - x ? Solid : (byte)0);

		var lines = CollisionTracer.Trace(grid, 16, 17, scale: 2);

		// The surface is one diagonal line, then the right side, the bottom and the start of the slope
		Assert.Contains(lines, l => l.A.X <= 2 && l.B.X >= 30 && l.A.Y > l.B.Y);
		Assert.True(lines.Count <= 5, $"{lines.Count} lines");
	}

	[Fact]
	public void PixelsThatTouchAtACorner_StaySeparate()
	{
		// Two 2 x 2 squares: one from (1, 1) to (3, 3), one from (3, 3) to (5, 5)
		byte[] grid = Grid(6, 6, (x, y) => (x is 1 or 2 && y is 1 or 2) || (x is 3 or 4 && y is 3 or 4) ? Solid : (byte)0);

		var lines = AsSet(CollisionTracer.Trace(grid, 6, 6, scale: 2));

		Assert.Equal(8, lines.Count);
		Assert.Contains((new Vector2i(6, 6), new Vector2i(2, 6)), lines);
		Assert.Contains((new Vector2i(6, 6), new Vector2i(10, 6)), lines);
	}
}
