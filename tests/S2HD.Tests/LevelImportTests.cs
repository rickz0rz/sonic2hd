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
		// The CNZ objects that SonicOrca does not have yet are listed, for example the slot machine cages ($D6)
		Assert.Contains(level.Notes, note => note.StartsWith("Object type $D6"));
	}

	[Fact]
	public void Cnz_HasTheBumpers_WithTheirArtAndTheirObjectResource()
	{
		ImportedLevel level = Cnz.Value;

		Assert.Equal(117, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZBUMPER"));
		ImportedSprite bumper = Assert.Single(level.Sprites, s => s.ObjectKey == "SONICORCA/OBJECTS/CNZBUMPER");
		// A PNG file, and two frames: the bumper and the squashed bumper
		Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, bumper.Texture.Take(4));
		Assert.Equal(2, bumper.AnimationGroup.Split("<animation>").Length - 1);
		Assert.Equal("SONICORCA.OBJECTS.CNZBUMPER", level.NewObjects["SONICORCA/OBJECTS/CNZBUMPER"]);
		Assert.Equal("SONICORCA.OBJECTS.INVISIBLEBLOCK", level.NewObjects["SONICORCA/OBJECTS/INVISIBLEBLOCK"]);
	}

	[Fact]
	public void Cnz_BonusBlocks_HaveTheFramesForEachOfTheirThreeColours()
	{
		ImportedLevel level = Cnz.Value;

		Assert.Equal(98, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZBONUSBLOCK"));
		ImportedSprite block = Assert.Single(level.Sprites, s => s.ObjectKey == "SONICORCA/OBJECTS/CNZBONUSBLOCK");
		// 6 mapping frames for each of the palette lines 2, 1 and 0
		Assert.Equal(18, block.AnimationGroup.Split("<animation>").Length - 1);
		Assert.Equal(18, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZHEXBUMPER"));
	}

	[Fact]
	public void Cnz_HasTheFlippersAndTheLaunchers_WithTheDiagonalLauncherArt()
	{
		ImportedLevel level = Cnz.Value;

		Assert.Equal(63, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZFLIPPER"));
		Assert.Equal(14, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZLAUNCHER"));
		Assert.Contains(level.Sprites, s => s.ObjectKey == "SONICORCA/OBJECTS/CNZLAUNCHER/DIAGONAL");
		Assert.Equal("SONICORCA.OBJECTS.CNZLAUNCHER", level.NewObjects["SONICORCA/OBJECTS/CNZLAUNCHER"]);
	}

	[Fact]
	public void Cnz_ImportGivesTheSameIdsEachTime()
	{
		ImportedLevel again = LevelImport.Import(OriginalZone.All["CNZ"], SpecialStageImportTests.FindDisassembly());

		Assert.Equal(Cnz.Value.Binding.ObjectPlacements.Select(p => p.Uid), again.Binding.ObjectPlacements.Select(p => p.Uid));
	}
}
