using System.IO;
using System.Linq;
using SonicOrca.Core.Collision;
using SonicOrca.Geometry;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Tests the collision data that the import writes for the level editor. Skipped when the Sonic 2 disassembly is
/// not found.
/// </summary>
public sealed class TileCollisionTests
{
	[Theory]
	[InlineData("ARZ")]
	[InlineData("CNZ")]
	[InlineData("MCZ")]
	[InlineData("OOZ")]
	[InlineData("MTZ")]
	[InlineData("SCZ")]
	[InlineData("WFZ")]
	[InlineData("DEZ")]
	public void Trace_GivesTheCollisionOfTheImport(string zone)
	{
		ImportedLevel level = LevelImport.Import(OriginalZone.All[zone], SpecialStageImportTests.FindDisassembly());
		TileCollision tileCollision = level.TileCollision;

		var traced = tileCollision.Trace(level.Map.Layers[tileCollision.Layer]).ToHashSet();

		// The layout changes add lines with only other path bits. The lines of paths 1 and 2 must be the same.
		var imported = level.Map.CollisionVectors
			.Where(v => (v.Paths & 3) != 0)
			.Select(v => (v.RelativeA, v.RelativeB, v.Paths & 3))
			.ToHashSet();
		Assert.Equal(imported.Count, traced.Count);
		Assert.True(imported.SetEquals(traced));
	}

	[Fact]
	public void Xml_KeepsAllTheData()
	{
		byte[] heights = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
		TileCollision original = new TileCollision(1, 3, 2, heights, new byte[] { 0, 1, 2 }, new byte[] { 0, 2, 1 }, new byte[] { 0, 1, 3, 5, 15, 0 });

		TileCollision copy = TileCollision.FromXml(new MemoryStream(original.ToXml()));

		Assert.Equal(1, copy.Layer);
		Assert.Equal(3, copy.Columns);
		Assert.Equal(2, copy.Rows);
		Assert.Equal(heights, copy.Heights);
		Assert.Equal(new byte[] { 0, 2, 1 }, copy.Secondary);
		Assert.Equal(15, copy.GetSolidity(1, 1));
		Assert.Equal(0, copy.GetSolidity(5, 5));
	}

	[Fact]
	public void Trace_AFullSolidBlock_GivesASquare()
	{
		// Collision number 1 is 16 pixels high in every column
		byte[] heights = new byte[32];
		for (int x = 0; x < 16; x++)
		{
			heights[16 + x] = 16;
		}
		TileCollision collision = new TileCollision(0, 1, 1, heights, new byte[] { 0, 1 }, new byte[] { 0, 1 },
			new byte[] { TileCollision.TopSolidPath1 | TileCollision.SidesSolidPath1 });
		SonicOrca.Core.LevelMap map = new SonicOrca.Core.LevelMap();
		SonicOrca.Core.LevelLayer front = new SonicOrca.Core.LevelLayer(map);
		front.Resize(1, 1);
		front.Tiles[0, 0] = 1;

		var lines = collision.Trace(front);

		// Four sides of 64 units, only on path 1, clockwise
		Assert.Equal(4, lines.Count);
		Assert.All(lines, l => Assert.Equal(1u, l.Paths));
		Assert.Contains(lines, l => l.A == new Vector2i(0, 0) && l.B == new Vector2i(64, 0));
	}
}
