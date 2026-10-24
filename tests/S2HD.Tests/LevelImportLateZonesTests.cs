using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Imports Metropolis, Sky Chase, Wing Fortress and Death Egg from a local Sonic 2 disassembly (s2disasm). Skipped when the
/// disassembly is not found.
/// </summary>
public sealed class LevelImportLateZonesTests
{
	private static readonly Lazy<ImportedLevel> Mtz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["MTZ"], SpecialStageImportTests.FindDisassembly()));

	private static readonly Lazy<ImportedLevel> Scz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["SCZ"], SpecialStageImportTests.FindDisassembly()));

	private static readonly Lazy<ImportedLevel> Wfz = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["WFZ"], SpecialStageImportTests.FindDisassembly()));

	private static readonly Lazy<ImportedLevel> Dez = new Lazy<ImportedLevel>(() =>
		LevelImport.Import(OriginalZone.All["DEZ"], SpecialStageImportTests.FindDisassembly()));

	private static LevelMarker Marker(ImportedLevel level, string name) => level.Map.Markers.Single(m => m.Name == name);

	[Fact]
	public void Mtz_HasThreeActs_AndConvertsEveryObjectType()
	{
		ImportedLevel level = Mtz.Value;

		Assert.Empty(level.Notes);
		Assert.Equal(new Vector2i(0, 66 * 16 * 4), Marker(level, "origin_1").Position);
		Assert.Equal(new Vector2i(576 * 16 * 4, 6 * 16 * 4), Marker(level, "origin_2").Position);
		Assert.Equal(new Vector2i(1088 * 16 * 4, 0), Marker(level, "origin_3").Position);
		// The camera of the original can go 256 pixels above the top of each act
		Assert.Equal((66 * 16 - 0x100) * 4, Marker(level, "bounds_1_lt").Position.Y);
		Assert.Equal(90, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/ASTERON"));
		// The block of MTZ is its own object, not the CPZ block
		Assert.DoesNotContain(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/CPZBLOCK");
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/MTZBOSS"));
	}

	[Fact]
	public void Scz_HasTheTornado_AndConvertsEveryObjectType()
	{
		ImportedLevel level = Scz.Value;

		Assert.Empty(level.Notes);
		Assert.Single(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/TORNADO");
		Assert.Equal(19, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/BALKIRY"));
	}

	[Fact]
	public void Wfz_AddsTheArtSupplement_AndConvertsEveryObjectType()
	{
		ImportedLevel level = Wfz.Value;

		// $8B and the boss are left out on purpose
		Assert.Empty(level.Notes);
		Assert.Equal(2, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/TORNADO"));
		Assert.DoesNotContain(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/CPZPLATFORM");
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/WFZBOSS"));
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/CLUCKER/SHOT"));
	}

	[Fact]
	public void Dez_HasTheRobot_Eggman_AndTheDoors()
	{
		ImportedLevel level = Dez.Value;

		Assert.Empty(level.Notes);
		Assert.Single(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/DEATHEGGROBOT");
		Assert.Single(level.Binding.ObjectPlacements, p => p.Key == "SONICORCA/OBJECTS/DEZEGGMAN");
		Assert.Equal(3, level.Binding.ObjectPlacements.Count(p => p.Key == "SONICORCA/OBJECTS/DEZDOOR"));
		// The area makes Silver Sonic
		Assert.True(level.NewObjects.ContainsKey("SONICORCA/OBJECTS/SILVERSONIC"));
	}
}
