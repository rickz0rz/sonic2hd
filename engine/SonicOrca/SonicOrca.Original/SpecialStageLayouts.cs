using System;
using System.Collections.Generic;
using System.IO;
using SonicOrca.Core.SpecialStage;

namespace SonicOrca.Original;

/// <summary>
/// Reads the special stage layouts of the original game, as the Sonic 2 disassembly (s2disasm) stores them:
/// <list type="bullet">
/// <item>misc/Special stage level layouts.nem (Nemesis): the track segments of each stage, one byte for each
/// segment. Bits 0-6 are the segment type, and bit 7 flips the segment.</item>
/// <item>misc/Special stage object location lists.kos (Kosinski): for each segment, a group of 2-byte entries
/// (ring $00-$3F or bomb $40-$7F with the distance in bits 0-5, then the angle), ended by one marker byte:
/// $FF none, $FE checkpoint, $FD emerald, and any other value shows the "rings to go" counter.</item>
/// </list>
/// Both files start with 7 big-endian words: the offset of each stage's data.
/// </summary>
public static class SpecialStageLayouts
{
	public const string LayoutsFile = "misc/Special stage level layouts.nem";

	public const string ObjectsFile = "misc/Special stage object location lists.kos";

	private const int StageCount = 7;

	/// <summary>Reads the 7 stages from the files in an s2disasm folder.</summary>
	public static List<HalfPipeStageData> ReadFromDisassembly(string disassemblyFolder)
	{
		byte[] layouts = File.ReadAllBytes(Path.Combine(disassemblyFolder, LayoutsFile));
		byte[] objects = File.ReadAllBytes(Path.Combine(disassemblyFolder, ObjectsFile));
		return Read(Nemesis.Decompress(layouts), Kosinski.Decompress(objects));
	}

	/// <summary>Reads the 7 stages from the decompressed layout and object data.</summary>
	public static List<HalfPipeStageData> Read(byte[] layouts, byte[] objects)
	{
		var stages = new List<HalfPipeStageData>();
		for (int stage = 0; stage < StageCount; stage++)
		{
			int layoutStart = ReadWord(layouts, stage * 2);
			int layoutEnd = stage + 1 < StageCount ? ReadWord(layouts, (stage + 1) * 2) : layouts.Length;
			int objectPosition = ReadWord(objects, stage * 2);
			int objectEnd = stage + 1 < StageCount ? ReadWord(objects, (stage + 1) * 2) : objects.Length;
			var data = new HalfPipeStageData();
			for (int i = layoutStart; i < layoutEnd; i++)
			{
				byte segmentByte = layouts[i];
				int type = segmentByte & 0x7F;
				if (type > (int)HalfPipeSegmentType.StraightThenTurn)
				{
					throw new InvalidDataException($"Stage {stage + 1}: unknown segment type {type}.");
				}
				var entries = new List<HalfPipeObjectEntry>();
				HalfPipeMarker marker = HalfPipeMarker.None;
				// The object stream can end before the layout (stage 7). Those segments have no objects.
				while (objectPosition < objectEnd)
				{
					byte first = objects[objectPosition++];
					if (first >= 0x80)
					{
						marker = first switch
						{
							0xFF => HalfPipeMarker.None,
							0xFE => HalfPipeMarker.Checkpoint,
							0xFD => HalfPipeMarker.Emerald,
							_ => HalfPipeMarker.RingsToGo,
						};
						break;
					}
					if (objectPosition >= objectEnd)
					{
						// The data of stage 7 ends inside an entry, after the last segment that the game reaches
						break;
					}
					byte angle = objects[objectPosition++];
					HalfPipeObjectKind kind = first < 0x40 ? HalfPipeObjectKind.Ring : HalfPipeObjectKind.Bomb;
					entries.Add(new HalfPipeObjectEntry(kind, first & 0x3F, angle));
				}
				data.Segments.Add(new HalfPipeSegment
				{
					Type = (HalfPipeSegmentType)type,
					Flip = (segmentByte & 0x80) != 0,
					Objects = entries,
					Marker = marker,
				});
			}
			stages.Add(data);
		}
		return stages;
	}

	private static int ReadWord(byte[] data, int offset)
	{
		if (offset + 1 >= data.Length)
		{
			throw new InvalidDataException("The data is too short for its header.");
		}
		return (data[offset] << 8) | data[offset + 1];
	}
}
