using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonicOrca.Original;

/// <summary>
/// A palette cycle of the original (PalCycle_xxx in s2.asm): at each step, the game writes new colours into some
/// entries of the palette, for example the water of Aquatic Ruin or the lights of Casino Night. The tiles do not
/// change, so the importer draws one frame of each block for each step.
/// </summary>
/// <param name="Period">The ticks of one step: the timer of the routine plus 1.</param>
/// <param name="Steps">The colours of each step: the palette entry (from 0 to 63) and its colour.</param>
public sealed record PaletteCycle(int Period, IReadOnlyList<IReadOnlyList<(int Entry, uint Colour)>> Steps)
{
	/// <summary>The palette entries that the cycle changes.</summary>
	public IEnumerable<int> Entries => Steps.SelectMany(step => step.Select(write => write.Entry)).Distinct();

	/// <summary>The ticks of one whole cycle.</summary>
	public int Length => Period * Steps.Count;

	/// <summary>Writes the colours of the step at a tick into the palette.</summary>
	public void Apply(uint[] palette, int tick)
	{
		foreach ((int entry, uint colour) in Steps[tick / Period % Steps.Count])
		{
			palette[entry] = colour;
		}
	}
}

/// <summary>
/// The palette cycles of the zones that the importer makes. Each cycle copies the routine of the original: the
/// colour table, the timer, the number of steps, and the palette entries that each step writes.
/// </summary>
public static class PaletteCycles
{
	/// <summary>The byte offset of a palette line in Normal_palette.</summary>
	private const int Line2 = 0x20;
	private const int Line3 = 0x40;
	private const int Line4 = 0x60;

	/// <summary>
	/// Reads the palette cycles of a zone. Emerald Hill, Chemical Plant and Hill Top have HD art, which animates by
	/// itself, so a zone without an entry here gives no cycles.
	/// </summary>
	public static IReadOnlyList<PaletteCycle> Read(string disassembly, string zone)
	{
		string source = File.ReadAllText(Path.Combine(disassembly, "s2.asm"));
		byte[] Table(string label) => ReadTable(disassembly, source, label);
		switch (zone)
		{
			case "ARZ":
				// PalCycle_ARZ: 4 colours from line 3 + 4, 4 steps of 8 bytes
				return new[] { Cycle(Table("CyclingPal_EHZ_ARZ_Water"), 6, 4, step => Words(Line3 + 4, step * 8, 4)) };
			case "OOZ":
				// PalCycle_OOZ: 4 colours from line 3 + $14, and each step moves 1 colour along the table
				return new[] { Cycle(Table("CyclingPal_Oil"), 8, 4, step => Words(Line3 + 0x14, step * 2, 4)) };
			case "MCZ":
				// PalCycle_MCZ: the lanterns. The original stops the cycle during the boss.
				return new[] { Cycle(Table("CyclingPal_Lantern"), 2, 4, step => Words(Line2 + 0x16, step * 2, 1)) };
			case "MTZ":
				return new[]
				{
					Cycle(Table("CyclingPal_MTZ1"), 0x12, 6, step => Words(Line3 + 0xA, step * 2, 1)),
					Cycle(Table("CyclingPal_MTZ2"), 3, 3, step => Words(Line3 + 2, step * 2, 3)),
					Cycle(Table("CyclingPal_MTZ3"), 10, 10, step => Words(Line3 + 0x1E, step * 2, 1)),
				};
			case "CNZ":
			{
				// PalCycle_CNZ: one timer for three tables. CNZ1 and CNZ3 have the colours of each step 6 bytes apart.
				byte[] cnz1 = Table("CyclingPal_CNZ1");
				byte[] cnz3 = Table("CyclingPal_CNZ3");
				int[] cnz1Entries = { 0x4A, 0x4C, 0x4E, 0x56, 0x58, 0x5A };
				int[] cnz3Entries = { 0x64, 0x66, 0x68 };
				return new[]
				{
					new PaletteCycle(8, Enumerable.Range(0, 3).Select(step =>
						(IReadOnlyList<(int, uint)>)cnz1Entries.Select((entry, i) => (entry / 2, Colour(cnz1, step * 2 + i * 6)))
						.Concat(cnz3Entries.Select((entry, i) => (entry / 2, Colour(cnz3, step * 2 + i * 6)))).ToArray()).ToArray()),
					// CNZ4 writes 3 colours from line 4 + $12 in the other order
					Cycle(Table("CyclingPal_CNZ4"), 8, 18, step => new[] { (Line4 + 0x12, step * 2 + 4), (Line4 + 0x14, step * 2 + 2), (Line4 + 0x16, step * 2) }),
				};
			}
			case "WFZ":
			case "SCZ":
			{
				// PalCycle_WFZ: an object switches the first cycle between the fire (SCZ starts with it) and the
				// conveyor belts. The tiles of the importer cannot switch, so Wing Fortress has the belts.
				PaletteCycle first = zone == "WFZ"
					? Cycle(Table("CyclingPal_WFZBelt"), 6, 4, step => Words(Line3 + 0xE, step * 8, 4))
					: Cycle(Table("CyclingPal_WFZFire"), 2, 4, step => Words(Line3 + 0xE, step * 8, 4));
				return new[]
				{
					first,
					Cycle(Table("CyclingPal_WFZ1"), 4, 0x22, step => Words(Line3 + 0x1C, step * 2, 1)),
					Cycle(Table("CyclingPal_WFZ2"), 6, 12, step => Words(Line3 + 0x1E, step * 2, 1)),
				};
			}
			case "DEZ":
				// Death Egg uses PalCycle_CPZ: one timer for three tables
				return new[]
				{
					Cycle(Table("CyclingPal_CPZ1"), 8, 9, step => Words(Line4 + 0x18, step * 6, 3)),
					Cycle(Table("CyclingPal_CPZ2"), 8, 21, step => Words(Line4 + 0x1E, step * 2, 1)),
					Cycle(Table("CyclingPal_CPZ3"), 8, 16, step => Words(Line3 + 0x1E, step * 2, 1)),
				};
			default:
				return Array.Empty<PaletteCycle>();
		}
	}

	/// <summary>The writes of a step: <paramref name="count"/> words from a table offset to a palette byte offset.</summary>
	private static (int Destination, int Source)[] Words(int destination, int source, int count)
	{
		return Enumerable.Range(0, count).Select(i => (destination + i * 2, source + i * 2)).ToArray();
	}

	private static PaletteCycle Cycle(byte[] table, int period, int steps, Func<int, (int Destination, int Source)[]> writes)
	{
		return new PaletteCycle(period, Enumerable.Range(0, steps).Select(step =>
			(IReadOnlyList<(int, uint)>)writes(step).Select(w => (w.Destination / 2, Colour(table, w.Source))).ToArray()).ToArray());
	}

	private static uint Colour(byte[] table, int offset)
	{
		return SpriteImport.ReadColours(new[] { table[offset], table[offset + 1] })[0];
	}

	/// <summary>Reads the file that s2.asm includes after a label (Label: BINCLUDE "file").</summary>
	private static byte[] ReadTable(string disassembly, string source, string label)
	{
		Match match = Regex.Match(source, "^" + Regex.Escape(label) + @":\s*(?:;[^\n]*)?\s*BINCLUDE\s+""([^""]+)""", RegexOptions.Multiline);
		if (!match.Success)
		{
			throw new InvalidDataException($"s2.asm has no table {label}.");
		}
		return File.ReadAllBytes(Path.Combine(disassembly, match.Groups[1].Value));
	}
}
