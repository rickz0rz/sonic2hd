using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Imports Casino Night from a local Sonic 2 disassembly (s2disasm). Skipped when the disassembly is not found.
/// </summary>
public sealed class LevelImportTests
{
	private static readonly Lazy<ImportedLevel> Cnz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["CNZ"], SpecialStageImportTests.FindDisassembly()));

	[Fact]
	public void Cnz_HasTheStartPositionsAndBoundsOfBothActs()
	{
		LevelMap map = Cnz.Value.Map;
		LevelMarker Marker(string name) => map.Markers.Single(m => m.Name == name);

		// startpos/CNZ_1.bin is (96, 684). Act 2 starts at (96, 1420) in an act that starts at chunk (85, 2).
		Assert.Equal(new SonicOrca.Geometry.Vector2i(96 * 4, 684 * 4), Marker("startpos_stk_1").Position);
		Assert.Equal(new SonicOrca.Geometry.Vector2i((85 * 128 + 96) * 4, (2 * 128 + 1420) * 4), Marker("startpos_stk_2").Position);
		Assert.Equal(new SonicOrca.Geometry.Vector2i((0x27A0 + 320) * 4, (0x720 + 224) * 4), Marker("bounds_1_rb").Position);
	}

	[Fact]
	public void Cnz_TilesAreTheOriginalBlocks_InTheLayersOfTheHdMap()
	{
		LevelMap map = Cnz.Value.Map;

		Assert.Equal(new[] { "back", "front" }, map.Layers.Select(l => l.Name));
		Assert.Equal(1400, map.Layers[1].Columns);
		Assert.Equal(144, map.Layers[1].Rows);
		Assert.Contains(Enumerable.Range(0, 1400).Select(x => map.Layers[1].Tiles[x, 60]), tile => tile != 0);
	}

	[Fact]
	public void Cnz_CollisionHasNoSmallSteps()
	{
		var vectors = Cnz.Value.Map.CollisionVectors;

		Assert.InRange(vectors.Count, 1000, 20000);
		// A floor that meets a wall line of one original pixel would trip a character
		int steps = vectors.Count(v => (v.RelativeA.X == v.RelativeB.X || v.RelativeA.Y == v.RelativeB.Y) && v.Length <= LevelImport.Scale);
		Assert.True(steps < 10, $"{steps} small steps");
		Assert.All(vectors, v => Assert.InRange(v.Paths, 1u, 3u));
	}

	[Fact]
	public void Cnz_HasTheRingsAndTheSupportedObjects()
	{
		ImportedLevel level = Cnz.Value;

		Assert.True(level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/RING") > 100);
		Assert.Contains(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/STARPOST");
		Assert.Contains(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/SIGNPOST");
		// The CNZ objects that SonicOrca does not have yet are listed, for example the round bumpers ($44)
		Assert.Contains(level.Notes, note => note.StartsWith("Object type $44"));
	}

	[Fact]
	public void Cnz_ImportGivesTheSameIdsEachTime()
	{
		ImportedLevel again = LevelImport.Import(OriginalZone.All["CNZ"], SpecialStageImportTests.FindDisassembly());

		Assert.Equal(Cnz.Value.Binding.ObjectPlacements.Select(p => p.Uid), again.Binding.ObjectPlacements.Select(p => p.Uid));
	}
}
