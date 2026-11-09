using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SonicOrca.Core;
using SonicOrca.Original;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests of the object layouts of Knuckles mode, from "Knuckles in Sonic 2" (KiS2).</summary>
public sealed class KnucklesLayoutsTests
{
	/// <summary>
	/// The Knuckles binding of EHZ is the HD binding with the changes of KiS2. KiS2 adds 13 invisible blocks to act 1
	/// and 7 to act 2, and it changes some layer switches. The HD objects that KiS2 does not change stay.
	/// </summary>
	[Fact]
	public void EmeraldHill_KeepsTheHdObjects_AndAddsTheInvisibleBlocksOfKis2()
	{
		string kis2 = KnucklesArtTests.FindKis2();
		string s2disasm = SpecialStageImportTests.FindDisassembly();
		string hdBinding = ReadGameDataText("SONICORCA/LEVELS/EHZ/BINDING");
		var addedKeys = new HashSet<string>();
		var notes = new List<string>();

		XDocument knuckles = XDocument.Parse(KnucklesLayouts.MakeHdBinding("EHZ", hdBinding, s2disasm, kis2, notes.Add, addedKeys));

		string[] hdKeys = XDocument.Parse(hdBinding).Descendants("Key").Select(k => k.Value).ToArray();
		string[] keys = knuckles.Descendants("Key").Select(k => k.Value).ToArray();
		Assert.Equal(20, keys.Count(k => k == "SONICORCA/OBJECTS/INVISIBLEBLOCK"));
		Assert.Equal(hdKeys.Count(k => k == "SONICORCA/OBJECTS/RING"), keys.Count(k => k == "SONICORCA/OBJECTS/RING"));
		Assert.Contains("SONICORCA/OBJECTS/INVISIBLEBLOCK", addedKeys);
		Assert.Contains(notes, note => note.Contains("0 removed objects are not in the HD binding"));
		Assert.DoesNotContain(knuckles.Descendants("Position"), p => p.Attribute("Moved") != null);
	}

	/// <summary>
	/// The import of a zone with the folder of KiS2 makes the Knuckles binding from the layouts of KiS2. ARZ act 2 is
	/// the act with the most changes. The Knuckles binding has the same rings as the normal binding.
	/// </summary>
	[Fact]
	public void ImportOfAquaticRuin_MakesTheKnucklesBinding()
	{
		string kis2 = KnucklesArtTests.FindKis2();
		string s2disasm = SpecialStageImportTests.FindDisassembly();

		ImportedLevel level = LevelImport.Import(OriginalZone.All["arz"], s2disasm, kis2);

		Assert.NotNull(level.KnucklesBinding);
		int Count(LevelBinding binding, string key) => binding.ObjectPlacements.Count(p => p.Key == key);
		Assert.Equal(Count(level.Binding, "SONICORCA/OBJECTS/RING"), Count(level.KnucklesBinding, "SONICORCA/OBJECTS/RING"));
		Assert.True(Count(level.KnucklesBinding, "SONICORCA/OBJECTS/INVISIBLEBLOCK") > Count(level.Binding, "SONICORCA/OBJECTS/INVISIBLEBLOCK"));
		Assert.True(level.KnucklesBinding.ObjectPlacements.Count > level.Binding.ObjectPlacements.Count);
	}

	/// <summary>Without the folder of KiS2, the import makes no Knuckles binding.</summary>
	[Fact]
	public void ImportWithoutKis2_MakesNoKnucklesBinding()
	{
		string s2disasm = SpecialStageImportTests.FindDisassembly();

		ImportedLevel level = LevelImport.Import(OriginalZone.All["mcz"], s2disasm);

		Assert.Null(level.KnucklesBinding);
	}

	private static string ReadGameDataText(string key)
	{
		string path = TestPaths.GameDataFile;
		if (!File.Exists(path))
		{
			Assert.Skip($"No game data at {path}.");
		}
		Resource resource = new ResourceFile(path).Scan()[key]?.Resource ?? throw new InvalidOperationException(key + " is not in the game data.");
		using StreamReader reader = new StreamReader(resource.Source.ReadUncompressed());
		return reader.ReadToEnd();
	}
}
