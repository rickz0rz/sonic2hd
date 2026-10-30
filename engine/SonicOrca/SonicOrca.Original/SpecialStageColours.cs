using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SonicOrca.Original;

/// <summary>
/// Reads the colors of the pipe of each special stage from the palettes of the Sonic 2 disassembly, and writes them
/// for the game. Each stage of the original loads its own palette, "art/palettes/Special Stage N.bin", into palette
/// line 3. Its colors 1 to 6 are the colors of the pipe that change from stage to stage: two families of three
/// shades each. The other colors are the same in all stages.
/// </summary>
public static class SpecialStageColours
{
	/// <summary>The resource with the colors, in the folder of the special stages.</summary>
	public const string FileName = "COLOURS.xml";

	public const int StageCount = 7;

	/// <summary>The colors 1 to 6 of the palette line of a stage.</summary>
	public const int ColoursPerStage = 6;

	/// <summary>Gives the 6 colors of each stage as RGB values, from the palette files of the disassembly.</summary>
	public static List<uint[]> Read(string disassembly)
	{
		List<uint[]> stages = new List<uint[]>();
		for (int stage = 1; stage <= StageCount; stage++)
		{
			byte[] data = File.ReadAllBytes(Path.Combine(disassembly, "art", "palettes", $"Special Stage {stage}.bin"));
			uint[] colours = new uint[ColoursPerStage];
			for (int i = 0; i < ColoursPerStage; i++)
			{
				// A colour of the Mega Drive is the word 0000 BBB0 GGG0 RRR0
				int word = data[(i + 1) * 2] << 8 | data[(i + 1) * 2 + 1];
				uint r = (uint)((word & 0xE) * 255 / 14);
				uint g = (uint)(((word >> 4) & 0xE) * 255 / 14);
				uint b = (uint)(((word >> 8) & 0xE) * 255 / 14);
				colours[i] = r << 16 | g << 8 | b;
			}
			stages.Add(colours);
		}
		return stages;
	}

	/// <summary>Writes the colors as XML: one stage element for each stage, with its colors as RRGGBB values.</summary>
	public static void Write(TextWriter writer, IReadOnlyList<uint[]> stages)
	{
		using XmlWriter xml = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true });
		xml.WriteStartElement("colours");
		for (int i = 0; i < stages.Count; i++)
		{
			xml.WriteStartElement("stage");
			xml.WriteAttributeString("number", (i + 1).ToString(CultureInfo.InvariantCulture));
			xml.WriteString(string.Join(" ", stages[i].Select(c => c.ToString("X6", CultureInfo.InvariantCulture))));
			xml.WriteEndElement();
		}
		xml.WriteEndElement();
	}
}
