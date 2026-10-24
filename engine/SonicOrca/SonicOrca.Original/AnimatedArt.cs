using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonicOrca.Original;

/// <summary>One frame of animated level art: the first tile in the art data, and the time in frames.</summary>
public readonly record struct AnimatedArtFrame(int Tile, int Duration);

/// <summary>
/// Animated level art of the original (a zoneanimdecl of Animated_xxx in s2.asm): the game copies a frame of the art
/// data over the level patterns from <paramref name="Tile"/>, for example the oil of Oil Ocean.
/// </summary>
/// <param name="Tile">The first level pattern that the animation changes.</param>
/// <param name="Tiles">The number of patterns in each frame.</param>
/// <param name="Data">The uncompressed art of all frames.</param>
public sealed record AnimatedArt(int Tile, int Tiles, byte[] Data, IReadOnlyList<AnimatedArtFrame> Frames)
{
	private const int BytesPerPattern = 32;

	/// <summary>True when the animation changes the pattern.</summary>
	public bool Contains(int pattern) => pattern >= Tile && pattern < Tile + Tiles;

	/// <summary>The level art with frame <paramref name="frame"/> of this animation in its patterns.</summary>
	public byte[] Apply(byte[] art, int frame)
	{
		int length = (Tile + Tiles) * BytesPerPattern;
		byte[] result = art.Length >= length ? (byte[])art.Clone() : art.Concat(new byte[length - art.Length]).ToArray();
		int source = Frames[frame].Tile * BytesPerPattern;
		Array.Copy(Data, source, result, Tile * BytesPerPattern, Math.Min(Tiles * BytesPerPattern, Data.Length - source));
		return result;
	}

	/// <summary>
	/// Reads the animated art of a zone from Animated_<paramref name="zone"/> in s2.asm. A zone without animated art
	/// gives an empty list.
	/// </summary>
	public static IReadOnlyList<AnimatedArt> Read(string disassembly, string zone)
	{
		string[] lines = File.ReadAllLines(Path.Combine(disassembly, "s2.asm"));
		string constants = File.ReadAllText(Path.Combine(disassembly, "s2.constants.asm"));
		int start = Array.FindIndex(lines, l => l.StartsWith("Animated_" + zone + ":", StringComparison.Ordinal));
		var result = new List<AnimatedArt>();
		if (start < 0)
		{
			return result;
		}
		for (int i = start + 1; i < lines.Length && !lines[i].Contains("zoneanimend"); i++)
		{
			Match declaration = Regex.Match(lines[i], @"^\s*zoneanimdecl\s+(-?[$\w]+)\s*,\s*(\w+)\s*,\s*(\w+)\s*,\s*([$\w]+)\s*,\s*([$\w]+)");
			if (!declaration.Success)
			{
				continue;
			}
			int duration = ParseNumber(declaration.Groups[1].Value);
			string label = declaration.Groups[2].Value;
			int tile = ParseNumber(Regex.Match(constants, "^" + declaration.Groups[3].Value + @"\s*=\s*(\$?[0-9A-Fa-f]+)", RegexOptions.Multiline).Groups[1].Value);
			int count = ParseNumber(declaration.Groups[4].Value);
			int tiles = ParseNumber(declaration.Groups[5].Value);
			// The frame data: the first tile of each frame, and its time when the duration of the declaration is -1
			var values = new List<int>();
			for (i++; i < lines.Length && !lines[i].Trim().StartsWith("even", StringComparison.Ordinal); i++)
			{
				string text = lines[i].Split(';')[0].Trim();
				if (text.StartsWith("dc.b", StringComparison.Ordinal))
				{
					values.AddRange(text.Substring(4).Split(',').Select(v => ParseNumber(v.Trim())));
				}
			}
			var frames = new List<AnimatedArtFrame>();
			for (int f = 0; f < count; f++)
			{
				frames.Add(duration < 0
					? new AnimatedArtFrame(values[f * 2], values[f * 2 + 1] + 1)
					: new AnimatedArtFrame(values[f], duration + 1));
			}
			string file = lines.Select(l => Regex.Match(l, "^" + label + @":\s*BINCLUDE\s*""([^""]+)""")).First(m => m.Success).Groups[1].Value;
			result.Add(new AnimatedArt(tile, tiles, File.ReadAllBytes(Path.Combine(disassembly, file)), frames));
		}
		return result;
	}

	/// <summary>
	/// The block mappings with the animated blocks of the zone (APM_xxx in s2.asm, LoadAnimatedBlocks). The game copies
	/// them to the end of the $1800 byte block table. The blocks use the patterns of the animated art.
	/// </summary>
	public static byte[] AddAnimatedBlocks(string disassembly, string zone, byte[] blockMappings)
	{
		const int BlockTableSize = 0x1800;
		string[] lines = File.ReadAllLines(Path.Combine(disassembly, "s2.asm"));
		string constants = File.ReadAllText(Path.Combine(disassembly, "s2.constants.asm"));
		int start = Array.FindIndex(lines, l => l.StartsWith("APM_" + zone + ":", StringComparison.Ordinal));
		if (start < 0)
		{
			return blockMappings;
		}
		var words = new List<int>();
		// s2disasm builds revision 1 by default: keep the "if gameRevision<2" parts and skip their "else" parts
		bool skip = false;
		for (int i = start + 1; i < lines.Length && !lines[i].StartsWith("APM_" + zone + "_End", StringComparison.Ordinal); i++)
		{
			string text = lines[i].Split(';')[0].Trim();
			if (text.StartsWith("if ", StringComparison.Ordinal) || text == "endif")
			{
				skip = false;
				continue;
			}
			if (text == "else")
			{
				skip = true;
				continue;
			}
			if (skip)
			{
				continue;
			}
			foreach (Match tile in Regex.Matches(text, @"make_block_tile\((\w+)(?:\+(\$?[0-9A-Fa-f]+))?,(\d),(\d),(\d),(\d)\)"))
			{
				int pattern = ParseNumber(Regex.Match(constants, "^" + tile.Groups[1].Value + @"\s*=\s*(\$?[0-9A-Fa-f]+)", RegexOptions.Multiline).Groups[1].Value);
				if (tile.Groups[2].Success)
				{
					pattern += ParseNumber(tile.Groups[2].Value);
				}
				words.Add((int.Parse(tile.Groups[6].Value) << 15) | (int.Parse(tile.Groups[5].Value) << 13) |
					(int.Parse(tile.Groups[4].Value) << 12) | (int.Parse(tile.Groups[3].Value) << 11) | pattern);
			}
		}
		byte[] result = new byte[Math.Max(BlockTableSize, blockMappings.Length)];
		blockMappings.CopyTo(result, 0);
		int offset = BlockTableSize - words.Count * 2;
		foreach (int word in words)
		{
			result[offset++] = (byte)(word >> 8);
			result[offset++] = (byte)word;
		}
		return result;
	}

	private static int ParseNumber(string text)
	{
		return text.StartsWith('$')
			? int.Parse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
			: int.Parse(text, CultureInfo.InvariantCulture);
	}
}
