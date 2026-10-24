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

		Assert.Equal(new[] { "back", "front", "front_high" }, map.Layers.Select(l => l.Name));
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
		// Every object type of CNZ has a SonicOrca object now
		Assert.Empty(level.Notes);
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

	[Theory]
	[InlineData("SONICORCA/OBJECTS/CRAWL", 8)]
	[InlineData("SONICORCA/OBJECTS/CNZBIGBLOCK", 23)]
	[InlineData("SONICORCA/OBJECTS/CNZELEVATOR", 16)]
	[InlineData("SONICORCA/OBJECTS/CNZFLASHINGBLOCK", 8)]
	[InlineData("SONICORCA/OBJECTS/CNZCONVEYOR", 8)]
	public void Cnz_HasThePlatformsAndTheBadnik(string key, int count)
	{
		Assert.Equal(count, Cnz.Value.Binding.ObjectPlacements.Count(p => p.Key == key));
		Assert.True(Cnz.Value.NewObjects.ContainsKey(key));
	}

	[Fact]
	public void Cnz_HasTheLandscapeBumpers_TheSlotMachines_AndTheirReels()
	{
		ImportedLevel level = Cnz.Value;

		// 53 and 52 bumpers in "CNZ 1 bumpers.bin" and "CNZ 2 bumpers.bin", without the markers
		Assert.Equal(105, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZMAPBUMPER"));
		Assert.Equal(29, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CNZSLOTCAGE"));
		// One reel window for each of the 10 slot machines. The window of the second machine of act 1 is at (1680, 528).
		var reels = level.Binding.ObjectPlacements.Where(p => p.Key == "SONICORCA/OBJECTS/CNZSLOTREELS").ToList();
		Assert.Equal(10, reels.Count);
		Assert.Contains(reels, r => r.Position == new SonicOrca.Geometry.Vector2i(1680 * 4, 528 * 4));
		Assert.True(level.Textures.ContainsKey("SONICORCA/OBJECTS/CNZSLOTREELS/TEXTURE"));
	}

	[Fact]
	public void Cnz_HighPriorityPatterns_GoOnALayerAboveTheCharacters()
	{
		ImportedLevel level = Cnz.Value;
		LevelLayer high = level.Map.Layers[2];

		Assert.Contains("<texture>/PRIORITY</texture>", level.TileSet);
		Assert.NotNull(level.PriorityTexture);
		// Each tile of the layer is a priority tile, and the front layer has the same block there
		for (int x = 0; x < high.Columns; x += 7)
		{
			for (int y = 0; y < high.Rows; y += 5)
			{
				int tile = high.Tiles[x, y] & 0xFFF;
				if (tile != 0)
				{
					Assert.Equal(PriorityTiles.FirstTile + (level.Map.Layers[1].Tiles[x, y] & 0xFFF) % PriorityTiles.AnimatedFirstTile, tile);
				}
			}
		}
		// A layer switch of CNZ moves a character to the layer of the priority tiles on one side
		Assert.Contains(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/LAYERSWITCH" &&
			p.Behaviour.Any(b => (b.Key == "RightLayer" || b.Key == "LeftLayer") && Convert.ToInt32(b.Value) == 1));
	}

	[Fact]
	public void Cnz_ImportGivesTheSameIdsEachTime()
	{
		ImportedLevel again = LevelImport.Import(OriginalZone.All["CNZ"], SpecialStageImportTests.FindDisassembly());

		Assert.Equal(Cnz.Value.Binding.ObjectPlacements.Select(p => p.Uid), again.Binding.ObjectPlacements.Select(p => p.Uid));
	}
}
