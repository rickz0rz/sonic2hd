using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Imports the original special stage layouts from a local Sonic 2 disassembly (s2disasm). Skipped when the
/// disassembly is not found. Set S2DISASM_DIR, or clone it next to this repository in sonicretro/s2disasm.
/// </summary>
public sealed class SpecialStageImportTests
{
	[Fact]
	public void Import_ReadsSevenStages_WithTheOriginalStage1Segments()
	{
		var stages = SpecialStageLayouts.ReadFromDisassembly(FindDisassembly());

		Assert.Equal(7, stages.Count);
		string stage1 = string.Join(" ", stages[0].Segments.Select(s => ((int)s.Type | (s.Flip ? 0x80 : 0)).ToString("x2")));
		Assert.Equal("03 03 03 04 02 03 03 03 03 03 03 04 02 03 03 03 03 04 00 02 03 03 04 81 02 03 03 04 02 03 04 02 03 84 02 03 04 02 83 84 02 03 03 03 03 00", stage1);
	}

	[Fact]
	public void Import_EveryStage_HasTheMarkersInTheOriginalOrder()
	{
		var stages = SpecialStageLayouts.ReadFromDisassembly(FindDisassembly());

		foreach (HalfPipeStageData stage in stages)
		{
			var markers = stage.Segments.Select(s => s.Marker).Where(m => m != HalfPipeMarker.None).Take(6);
			Assert.Equal(new[]
			{
				HalfPipeMarker.RingsToGo, HalfPipeMarker.Checkpoint,
				HalfPipeMarker.RingsToGo, HalfPipeMarker.Checkpoint,
				HalfPipeMarker.RingsToGo, HalfPipeMarker.Emerald,
			}, markers);
		}
	}

	[Theory]
	[InlineData(0, 206)]
	[InlineData(4, 403)]
	[InlineData(6, 492)]
	public void Import_RingsUpToTheEmerald_MatchTheDisassembly(int stage, int expectedRings)
	{
		HalfPipeStageData data = SpecialStageLayouts.ReadFromDisassembly(FindDisassembly())[stage];

		int emerald = data.Segments.FindIndex(s => s.Marker == HalfPipeMarker.Emerald);
		int rings = data.Segments.Take(emerald + 1).Sum(s => s.Objects.Count(o => o.Kind == HalfPipeObjectKind.Ring));
		Assert.Equal(expectedRings, rings);
	}

	[Fact]
	public void StageData_XmlRoundTrip_KeepsEverything()
	{
		HalfPipeStageData original = SpecialStageLayouts.ReadFromDisassembly(FindDisassembly())[2];
		var text = new StringWriter();
		original.WriteXml(text);
		var document = new XmlDocument();
		document.LoadXml(text.ToString());

		HalfPipeStageData copy = HalfPipeStageData.FromXml(document);

		Assert.Equal(original.Segments.Count, copy.Segments.Count);
		for (int i = 0; i < original.Segments.Count; i++)
		{
			Assert.Equal(original.Segments[i].Type, copy.Segments[i].Type);
			Assert.Equal(original.Segments[i].Flip, copy.Segments[i].Flip);
			Assert.Equal(original.Segments[i].Marker, copy.Segments[i].Marker);
			Assert.Equal(original.Segments[i].Objects, copy.Segments[i].Objects);
		}
	}

	internal static string FindDisassembly()
	{
		string configured = Environment.GetEnvironmentVariable("S2DISASM_DIR");
		string[] candidates =
		{
			configured,
			Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "..", "..", "sonicretro", "s2disasm")),
			Path.GetFullPath(Path.Combine(TestPaths.RepoRoot, "..", "s2disasm")),
		};
		foreach (string candidate in candidates)
		{
			if (!string.IsNullOrEmpty(candidate) && File.Exists(Path.Combine(candidate, SpecialStageLayouts.LayoutsFile)))
			{
				return candidate;
			}
		}
		Assert.Skip("The Sonic 2 disassembly was not found. Set S2DISASM_DIR to an s2disasm checkout.");
		return null;
	}
}
