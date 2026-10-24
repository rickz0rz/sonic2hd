using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Imports Aquatic Ruin from a local Sonic 2 disassembly (s2disasm). Skipped when the disassembly is not found.
/// </summary>
public sealed class LevelImportArzTests
{
	private static readonly Lazy<ImportedLevel> Arz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["ARZ"], SpecialStageImportTests.FindDisassembly()));

	private static LevelMarker Marker(string name) => Arz.Value.Map.Markers.Single(m => m.Name == name);

	[Fact]
	public void Arz_KeepsTheLayersOfTheHdMap_AndHasThePriorityLayer()
	{
		LevelMap map = Arz.Value.Map;

		Assert.Equal(new[] { "bg", "fg", "front_high" }, map.Layers.Select(l => l.Name));
		Assert.Equal(96, map.Layers[0].Rows);
		Assert.Equal(1448, map.Layers[1].Columns);
	}

	[Fact]
	public void Arz_HasTheStartPositions_TheBounds_AndTheWaterOfBothActs()
	{
		// Both acts start at (96, 894). Act 2 is at block (704, 4) of the map.
		Assert.Equal(new Vector2i(96 * 4, 894 * 4), Marker("startpos_stk_1").Position);
		Assert.Equal(new Vector2i((704 * 16 + 96) * 4, (4 * 16 + 894) * 4), Marker("startpos_stk_2").Position);
		Assert.Equal(new Vector2i(0, 0x200 * 4), Marker("bounds_1_lt").Position);
		Assert.Equal(0x410 * 4, Marker("water_1").Position.Y);
		Assert.Equal((4 * 16 + 0x510) * 4, Marker("water_2").Position.Y);
	}

	[Fact]
	public void Arz_ConvertsEveryObjectType()
	{
		ImportedLevel level = Arz.Value;

		Assert.Empty(level.Notes);
		Assert.True(level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/RING") > 300);
		// The platform of ARZ is its own object, not the EHZ platform with its art
		Assert.DoesNotContain(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/EHZPLATFORM");
		Assert.Equal(21, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/ARZPLATFORM"));
		// Each of the 4 rotating platform objects of the original gives 3 platforms
		Assert.Equal(12, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/ARZROTATINGPLATFORM"));
		Assert.Equal(28, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/GROUNDER"));
	}

	[Fact]
	public void Arz_SplitFrames_GiveOneAnimationForEachPiece()
	{
		ImportedSprite pillar = Assert.Single(Arz.Value.Sprites, s => s.ObjectKey == "SONICORCA/OBJECTS/ARZRISINGPILLAR");

		// 14 frames, then the pieces of frames 7 to 13: 8 + 10 + 10 + 12 + 12 + 14 + 14
		Assert.Equal(14 + 80, pillar.AnimationGroup.Split("<animation>").Length - 1);
	}
}
