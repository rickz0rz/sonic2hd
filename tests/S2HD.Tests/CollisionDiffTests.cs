using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Editor;
using SonicOrca.Geometry;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests how the level editor makes the collision follow the tiles in an area of changed cells.</summary>
public sealed class CollisionDiffTests
{
	private static (Vector2i, Vector2i, uint) Line(int ax, int ay, int bx, int by, uint paths = 3) => (new Vector2i(ax, ay), new Vector2i(bx, by), paths);

	[Fact]
	public void SameLines_ChangeNothing()
	{
		var vectors = new List<CollisionVector> { new CollisionVector(new Vector2i(0, 64), new Vector2i(128, 64), 3) };

		var (removed, added) = CollisionDiff.Get(vectors, new[] { Line(0, 64, 128, 64) }, new Rectanglei(0, 0, 1, 1));

		Assert.Empty(removed);
		Assert.Empty(added);
	}

	[Fact]
	public void ALineInTheArea_IsReplaced()
	{
		CollisionVector old = new CollisionVector(new Vector2i(0, 64), new Vector2i(128, 64), 3);

		var (removed, added) = CollisionDiff.Get(new[] { old }, new[] { Line(0, 32, 128, 32) }, new Rectanglei(0, 0, 2, 1));

		Assert.Equal(new[] { old }, removed);
		Assert.Equal(new[] { Line(0, 32, 128, 32) }, added);
	}

	[Fact]
	public void ALineDrawnByHand_FarFromTheArea_Stays()
	{
		CollisionVector handDrawn = new CollisionVector(new Vector2i(6400, 640), new Vector2i(6464, 600), 1);

		var (removed, added) = CollisionDiff.Get(new[] { handDrawn }, new[] { Line(0, 64, 128, 64) }, new Rectanglei(0, 0, 2, 1));

		Assert.Empty(removed);
		Assert.Equal(new[] { Line(0, 64, 128, 64) }, added);
	}

	[Fact]
	public void APartOfALongerNewLine_OutsideTheArea_IsRemoved()
	{
		// The old trace had two lines. The new trace joins them into one line that crosses the changed cell.
		CollisionVector inside = new CollisionVector(new Vector2i(0, 64), new Vector2i(64, 64), 3);
		CollisionVector outside = new CollisionVector(new Vector2i(640, 64), new Vector2i(1280, 64), 3);

		var (removed, added) = CollisionDiff.Get(new[] { inside, outside }, new[] { Line(0, 64, 1280, 64) }, new Rectanglei(0, 0, 1, 1));

		Assert.Equal(2, removed.Count);
		Assert.Single(added);
	}

	[Fact]
	public void LinesOfOtherPathsAndObjects_Stay()
	{
		// A line of a layout change (only path bits above 1) is not a line of paths 1 and 2
		CollisionVector layoutChange = new CollisionVector(new Vector2i(0, 64), new Vector2i(64, 64), 1u << 2);

		var (removed, _) = CollisionDiff.Get(new[] { layoutChange }, new (Vector2i, Vector2i, uint)[0], new Rectanglei(0, 0, 1, 1));

		Assert.Empty(removed);
	}
}
