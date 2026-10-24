using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonicOrca.Original;

/// <summary>
/// Converts the demos of the original game (Demo_EHZ, Demo_CPZ, Demo_ARZ and Demo_CNZ in s2.asm) into input
/// recordings of SonicOrca. A demo of the original is a list of button states, each with the number of frames that
/// it stays (the demoinput macro stores the duration minus 1, and MoveDemo_On holds it for that value plus 1). The title screen plays the four demos in turn (DemoLevels).
/// </summary>
public static class OriginalDemos
{
	/// <summary>The zone of each demo, in the order of DemoLevels, and the label of its input data.</summary>
	public static IReadOnlyList<(string Zone, string Label)> All { get; } = new[]
	{
		("EHZ", "Demo_EHZ"),
		("CPZ", "Demo_CPZ"),
		("ARZ", "Demo_ARZ"),
		("CNZ", "Demo_CNZ"),
	};

	/// <summary>The length of a demo of the original: 1640 frames (Level: Demo_Time_left).</summary>
	public const int DemoFrames = 1640;

	/// <summary>
	/// The frames that an HD level runs before it starts: the title card. A recording plays from the first frame,
	/// and the original demo starts when the level starts.
	/// </summary>
	public const int LevelStartFrames = 120;

	/// <summary>The size of one frame in a recording (PlayRecorder in SonicOrca.Core).</summary>
	private const int EntrySize = 40;

	private const int ButtonUp = 1;
	private const int ButtonDown = 2;
	private const int ButtonLeft = 4;
	private const int ButtonRight = 8;
	private const int ButtonsJump = 0x70;

	/// <summary>Reads the button state of each frame of a demo, for <see cref="DemoFrames"/> frames.</summary>
	public static byte[] ReadButtons(string disassembly, string label)
	{
		string[] lines = File.ReadAllLines(Path.Combine(disassembly, "s2.asm"));
		int start = Array.FindIndex(lines, l => l.StartsWith(label + ":", StringComparison.Ordinal));
		if (start < 0)
		{
			throw new InvalidDataException("The disassembly has no demo " + label + ".");
		}
		var frames = new List<byte>();
		for (int i = start + 1; i < lines.Length && frames.Count < DemoFrames; i++)
		{
			Match input = Regex.Match(lines[i], @"^\s*demoinput\s+([UDLRABCS]*)\s*,\s*(\$?[0-9A-Fa-f]+)");
			if (!input.Success)
			{
				// The data of the demo ends at the next label
				if (Regex.IsMatch(lines[i], @"^\w+:"))
				{
					break;
				}
				continue;
			}
			int buttons = 0;
			foreach (char button in input.Groups[1].Value)
			{
				buttons |= button switch
				{
					'U' => ButtonUp,
					'D' => ButtonDown,
					'L' => ButtonLeft,
					'R' => ButtonRight,
					'B' => 0x10,
					'C' => 0x20,
					'A' => 0x40,
					_ => 0x80,
				};
			}
			string text = input.Groups[2].Value;
			int duration = text.StartsWith('$')
				? int.Parse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
				: int.Parse(text, CultureInfo.InvariantCulture);
			frames.AddRange(Enumerable.Repeat((byte)buttons, duration));
		}
		while (frames.Count < DemoFrames)
		{
			frames.Add(0);
		}
		return frames.Take(DemoFrames).ToArray();
	}

	/// <summary>
	/// Makes an input recording of a demo: frames without input for the title card, then the buttons of the demo.
	/// </summary>
	public static byte[] ToRecording(byte[] buttons)
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);
		foreach (byte frame in Enumerable.Repeat((byte)0, LevelStartFrames).Concat(buttons))
		{
			double x = ((frame & ButtonRight) != 0 ? 1 : 0) - ((frame & ButtonLeft) != 0 ? 1 : 0);
			double y = ((frame & ButtonDown) != 0 ? 1 : 0) - ((frame & ButtonUp) != 0 ? 1 : 0);
			writer.Write(x);
			writer.Write(y);
			writer.Write((frame & ButtonsJump) != 0);
			// The padding byte, then the state of the player, which a playback does not use
			writer.Write((byte)0);
			writer.Write(new byte[EntrySize - 18]);
		}
		writer.Flush();
		return stream.ToArray();
	}
}
