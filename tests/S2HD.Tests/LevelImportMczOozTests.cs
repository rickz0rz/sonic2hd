using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Imports Mystic Cave and Oil Ocean from a local Sonic 2 disassembly (s2disasm). Skipped when the disassembly is
/// not found.
/// </summary>
public sealed class LevelImportMczOozTests
{
	private static readonly Lazy<ImportedLevel> Mcz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["MCZ"], SpecialStageImportTests.FindDisassembly()));

	private static readonly Lazy<ImportedLevel> Ooz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["OOZ"], SpecialStageImportTests.FindDisassembly()));

	private static LevelMarker Marker(ImportedLevel level, string name) => level.Map.Markers.Single(m => m.Name == name);

	[Fact]
	public void Mcz_HasTheActsAtTheirPositionsInTheHdMap()
	{
		// Act 1 is 4 blocks lower than in the original, and act 2 starts at block 592
		Assert.Equal(new Vector2i(0, 4 * 16 * 4), Marker(Mcz.Value, "origin_1").Position);
		Assert.Equal(new Vector2i(592 * 16 * 4, 0), Marker(Mcz.Value, "origin_2").Position);
		Assert.Equal((4 * 16 + 0x3C0) * 4, Marker(Mcz.Value, "bounds_1_lt").Position.Y);
		Assert.Equal(new[] { "back", "front", "front_high" }, Mcz.Value.Map.Layers.Select(l => l.Name));
	}

	[Fact]
	public void Mcz_ConvertsEveryObjectType_AndHasTheBossArt()
	{
		ImportedLevel level = Mcz.Value;

		Assert.Empty(level.Notes);
		// The signpost of act 2 is only for two player mode
		Assert.Single(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/SIGNPOST");
		Assert.Equal(12, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/CRAWLTON"));
		Assert.Equal(26, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/FLASHER"));
		// The area makes the boss, so the import writes its object resource without a placement
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/MCZBOSS"));
		Assert.Contains(level.Sprites, s => s.ObjectKey == "SONICORCA/OBJECTS/MCZBOSS/ROCK");
	}

	[Fact]
	public void Ooz_ConvertsEveryObjectType_AndWritesTheOil()
	{
		ImportedLevel level = Ooz.Value;

		Assert.Empty(level.Notes);
		Assert.Equal(new[] { "bg", "fg", "front_high" }, level.Map.Layers.Select(l => l.Name));
		Assert.Equal(new Vector2i(792 * 16 * 4, 0), Marker(level, "origin_2").Position);
		Assert.Equal(30, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/OCTUS"));
		Assert.Equal(48, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/OOZFAN"));
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/OOZOIL"));
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/OOZBOSS"));
	}

	[Fact]
	public void Ooz_TheOilSurface_HasAnimatedTiles()
	{
		ImportedLevel level = Ooz.Value;

		// The oil blocks use the patterns of the animated art (ArtUnc_Oil1 and 2, 6 frames each)
		Assert.Contains("<frame ", level.TileSet);
		Assert.Contains(Enumerable.Range(0, level.Map.Layers[2].Columns), x =>
			Enumerable.Range(0, level.Map.Layers[2].Rows).Any(y => level.TileSet.Contains($"<tile id=\"{level.Map.Layers[2].Tiles[x, y] & 0xFFF}\">")));
	}

	[Fact]
	public void AnimatedArt_ReadsTheOilOfOilOcean()
	{
		var animations = AnimatedArt.Read(SpecialStageImportTests.FindDisassembly(), "OOZ");

		// The pulsing ball, two squares around it, and two parts of the oil
		Assert.Equal(5, animations.Count);
		AnimatedArt oil = animations[3];
		Assert.Equal(0x2C2, oil.Tile);
		Assert.Equal(16, oil.Tiles);
		Assert.Equal(new[] { 0, 16, 32, 48, 32, 16 }, oil.Frames.Select(f => f.Tile));
		Assert.All(oil.Frames, f => Assert.Equal(0x12, f.Duration));
	}
}
