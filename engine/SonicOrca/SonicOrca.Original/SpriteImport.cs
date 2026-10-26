using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Hjg.Pngcs;

namespace SonicOrca.Original;

/// <summary>One piece of a sprite frame of the original game: a block of 1 to 4 by 1 to 4 tiles of 8 x 8 pixels.</summary>
/// <param name="X">The left of the piece, in pixels from the object position.</param>
/// <param name="Y">The top of the piece, in pixels from the object position.</param>
/// <param name="Width">The width in tiles.</param>
/// <param name="Height">The height in tiles.</param>
/// <param name="Tile">The first tile, from the start of the art. The tiles go down each column first.</param>
/// <param name="Palette">The palette line to add to the palette line of the object.</param>
public readonly record struct SpritePiece(int X, int Y, int Width, int Height, int Tile, bool FlipX, bool FlipY, int Palette);

/// <summary>The sprites of an object of the original game: its art, its mappings and its palette lines.</summary>
/// <param name="ObjectKey">The SonicOrca object, for example "SONICORCA/OBJECTS/CNZBUMPER".</param>
/// <param name="ArtFile">
/// The art file in the disassembly: Nemesis, Kosinski for a .kos file (the level art), or uncompressed for a .bin file.
/// </param>
/// <param name="MappingsFile">
/// The sprite mappings file (assembly source) in the disassembly, or "file#label" for mappings inside a larger
/// source file, for example "s2.asm#Obj8D_MapUnc_36CF0".
/// </param>
/// <param name="PaletteLines">
/// The palette line of make_art_tile for the object, then the other lines that the object changes to, if any.
/// </param>
public sealed record OriginalSprite(string ObjectKey, string ArtFile, string MappingsFile, IReadOnlyList<int> PaletteLines)
{
	/// <summary>
	/// Frames whose pieces the object also needs one by one, for example the fragments of a platform that breaks.
	/// After the normal frames, the group has one animation for each piece of these frames, in order. Each piece
	/// keeps its position in the frame.
	/// </summary>
	public IReadOnlyList<int> SplitFrames { get; init; } = Array.Empty<int>();

	/// <summary>
	/// More art files that the mappings use, each at its first tile from the start of <see cref="ArtFile"/>. For
	/// example, a boss has its own art and the art of Eggman's ship, which the original loads at other VRAM addresses.
	/// </summary>
	public IReadOnlyList<(string File, int Tile)> MoreArt { get; init; } = Array.Empty<(string, int)>();

	/// <summary>
	/// The tile of the mappings at which <see cref="ArtFile"/> starts. It is negative when the mappings start inside
	/// the art.
	/// </summary>
	public int ArtTile { get; init; }

	/// <summary>
	/// Palette lines that the object changes, with the palette file for each line. For example, the level events
	/// load a boss palette into line 1 before the boss comes.
	/// </summary>
	public IReadOnlyDictionary<int, string> PaletteOverrides { get; init; } = new Dictionary<int, string>();

	public OriginalSprite(string objectKey, string artFile, string mappingsFile, int paletteLine)
		: this(objectKey, artFile, mappingsFile, new[] { paletteLine })
	{
	}
}

/// <summary>The files of an imported sprite: a texture with all frames in one row, and an animation group with one animation for each frame.</summary>
public sealed record ImportedSprite(string ObjectKey, byte[] Texture, string AnimationGroup);

/// <summary>
/// Converts the sprites of an object of the original game into a texture at four times the size, and an animation
/// group. Animation N of the group shows mapping frame N, so the object code can follow the animation scripts of the
/// original. An object with more than one palette line has the frames again for each line: animation
/// <c>line * frames + N</c>.
/// </summary>
public static class SpriteImport
{
	private const int TileSize = 8;

	private const int BytesPerTile = 32;

	/// <summary>The widest texture that the importer makes, in pixels.</summary>
	private const int MaxTextureWidth = 4096;

	/// <summary>Reads the frames of a mappings file of s2disasm (mappingsTable, spriteHeader and spritePiece macros).</summary>
	/// <param name="tableLabel">The label of the table to read, when the source has more than one table, or null.</param>
	public static List<List<SpritePiece>> ParseMappings(string source, string tableLabel = null)
	{
		var order = new List<string>();
		// With a table label, read only the entries that follow that label
		bool inTable = tableLabel == null;
		var frames = new Dictionary<string, List<SpritePiece>>(StringComparer.Ordinal);
		List<SpritePiece> current = null;
		var aliases = new List<string>();
		foreach (string rawLine in source.Split('\n'))
		{
			string line = rawLine.Split(';')[0].Trim();
			if (line.Length == 0)
			{
				continue;
			}
			if (tableLabel != null && Regex.IsMatch(line, @"^\w+:"))
			{
				inTable = line.StartsWith(tableLabel + ":", StringComparison.Ordinal);
			}
			// Labels can be local labels, which start with a dot
			Match entry = Regex.Match(line, @"^(?:[.\w]+:\s*)?mappingsTableEntry\.w\s+([.\w]+)");
			if (entry.Success)
			{
				if (inTable)
				{
					order.Add(entry.Groups[1].Value);
				}
				continue;
			}
			Match header = Regex.Match(line, @"^([.\w]+):\s*spriteHeader");
			if (header.Success)
			{
				current = new List<SpritePiece>();
				frames[header.Groups[1].Value] = current;
				foreach (string alias in aliases)
				{
					frames[alias] = current;
				}
				aliases.Clear();
				continue;
			}
			// A label alone on its line names the frame that follows it, for example ".frame2:" before ".frame1:"
			Match label = Regex.Match(line, @"^([.\w]+):$");
			if (label.Success)
			{
				aliases.Add(label.Groups[1].Value);
				continue;
			}
			Match piece = Regex.Match(line, @"^spritePiece(2P)?\s+(.*)$");
			if (piece.Success && current != null)
			{
				int[] values = piece.Groups[2].Value.Split(',').Select(v => ParseNumber(v.Trim())).ToArray();
				current.Add(new SpritePiece(values[0], values[1], values[2], values[3], values[4], values[5] != 0, values[6] != 0, values[7]));
			}
		}
		return order.Select(label => frames.TryGetValue(label, out List<SpritePiece> frame) ? frame : new List<SpritePiece>()).ToList();
	}

	private static int ParseNumber(string text)
	{
		bool negative = text.StartsWith('-');
		text = text.TrimStart('-', '+');
		int value = text.StartsWith('$') ? int.Parse(text.Substring(1), NumberStyles.HexNumber) : int.Parse(text, CultureInfo.InvariantCulture);
		return negative ? -value : value;
	}

	/// <summary>Reads the palette of a zone: line 0 is Sonic and Tails, lines 1 to 3 the zone palette. Mega Drive colours, 0BGR.</summary>
	public static uint[] ReadPalette(string disassembly, string zonePaletteFile)
	{
		byte[] data = File.ReadAllBytes(Path.Combine(disassembly, "art/palettes/SonicAndTails.bin"))
			.Concat(File.ReadAllBytes(Path.Combine(disassembly, zonePaletteFile))).ToArray();
		uint[] palette = new uint[64];
		ReadColours(data).AsSpan(0, Math.Min(64, data.Length / 2)).CopyTo(palette);
		return palette;
	}

	/// <summary>Reads a palette file of the disassembly into the palette, from the start of a palette line.</summary>
	public static void ReadPaletteFile(string disassembly, string file, uint[] palette, int line)
	{
		uint[] colours = ReadColours(File.ReadAllBytes(Path.Combine(disassembly, file)));
		Array.Copy(colours, 0, palette, line * 16, Math.Min(colours.Length, palette.Length - line * 16));
	}

	/// <summary>Converts Mega Drive colours (0BGR) to RGBA colours.</summary>
	private static uint[] ReadColours(byte[] data)
	{
		var palette = new uint[data.Length / 2];
		for (int i = 0; i < palette.Length; i++)
		{
			int colour = (data[i * 2] << 8) | data[i * 2 + 1];
			// Each channel has 3 bits, in bits 1 to 3 of its nibble
			int r = (colour >> 1) & 7;
			int g = (colour >> 5) & 7;
			int b = (colour >> 9) & 7;
			palette[i] = (uint)((r * 255 / 7) << 24 | (g * 255 / 7) << 16 | (b * 255 / 7) << 8 | 0xFF);
		}
		return palette;
	}

	/// <summary>Reads Nemesis art, Kosinski art (.kos) or uncompressed art (.bin).</summary>
	public static byte[] ReadArt(string disassembly, string file)
	{
		byte[] data = File.ReadAllBytes(Path.Combine(disassembly, file));
		if (file.EndsWith(".kos", StringComparison.OrdinalIgnoreCase))
		{
			return Kosinski.Decompress(data);
		}
		return file.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ? data : Nemesis.Decompress(data);
	}

	public static ImportedSprite Import(OriginalSprite sprite, string disassembly, uint[] palette, int scale)
	{
		byte[] art = Array.Empty<byte>();
		foreach ((string file, int artTile) in sprite.MoreArt.Prepend((sprite.ArtFile, sprite.ArtTile)))
		{
			byte[] more = ReadArt(disassembly, file);
			int tile = artTile;
			if (tile < 0)
			{
				// The mappings start after the start of the art: the object points into the middle of the art
				more = more.AsSpan(Math.Min(more.Length, -tile * BytesPerTile)).ToArray();
				tile = 0;
			}
			if (art.Length < (tile * BytesPerTile) + more.Length)
			{
				Array.Resize(ref art, (tile * BytesPerTile) + more.Length);
			}
			more.CopyTo(art, tile * BytesPerTile);
		}
		if (sprite.PaletteOverrides.Count > 0)
		{
			palette = (uint[])palette.Clone();
			foreach ((int line, string file) in sprite.PaletteOverrides)
			{
				uint[] colours = ReadColours(File.ReadAllBytes(Path.Combine(disassembly, file)));
				Array.Copy(colours, 0, palette, line * 16, Math.Min(16, colours.Length));
			}
		}
		string[] mappings = sprite.MappingsFile.Split('#');
		List<List<SpritePiece>> frames = ParseMappings(File.ReadAllText(Path.Combine(disassembly, mappings[0])), mappings.Length > 1 ? mappings[1] : null);
		return Import(sprite.ObjectKey, art, frames, sprite.PaletteLines, sprite.SplitFrames, palette, scale);
	}

	/// <summary>
	/// Converts frames of pieces into a texture at <paramref name="scale"/> times the size, and an animation group with
	/// one animation for each frame and palette line. The art is the tiles as the video memory has them.
	/// </summary>
	public static ImportedSprite Import(string objectKey, byte[] art, List<List<SpritePiece>> frames, IReadOnlyList<int> paletteLines,
		IReadOnlyList<int> splitFrames, uint[] palette, int scale)
	{
		OriginalSprite sprite = new OriginalSprite(objectKey, null, null, paletteLines) { SplitFrames = splitFrames };
		// One row of the texture for each palette line, and one more for the pieces of the split frames
		var rows = new List<List<(uint[] Pixels, int Width, int Height, int OriginX, int OriginY)>>();
		foreach (int line in sprite.PaletteLines)
		{
			rows.Add(frames.Select(frame => Render(frame, art, palette, line)).ToList());
		}
		if (sprite.SplitFrames.Count > 0)
		{
			rows.Add(sprite.SplitFrames.SelectMany(f => frames[f]).Select(piece => Render(new List<SpritePiece> { piece }, art, palette, sprite.PaletteLines[0])).ToList());
		}
		// Long rows go on more rows, because GPUs limit the width of a texture. The frames keep their order.
		rows = rows.SelectMany(row => SplitRow(row, MaxTextureWidth / scale)).ToList();
		var images = rows.SelectMany(row => row).ToList();
		int rowHeight = Math.Max(1, images.Max(i => i.Height));
		int width = Math.Max(1, rows.Max(row => row.Sum(i => i.Width))) * scale;
		int height = rowHeight * rows.Count * scale;
		var pixels = new byte[width * height * 4];
		var anigroup = new StringBuilder();
		anigroup.AppendLine("<?xml version=\"1.0\"?>");
		anigroup.AppendLine("<anigroup>");
		anigroup.AppendLine("  <textures>");
		anigroup.AppendLine("    <texture>//TEXTURE</texture>");
		anigroup.AppendLine("  </textures>");
		anigroup.AppendLine("  <animations>");
		int f = 0;
		for (int r = 0; r < rows.Count; r++)
		{
			int left = 0;
			int top = r * rowHeight;
			foreach ((uint[] image, int w, int h, int originX, int originY) in rows[r])
			{
				for (int y = 0; y < h * scale; y++)
				{
					for (int x = 0; x < w * scale; x++)
					{
						uint colour = image[(y / scale) * w + x / scale];
						int offset = ((top * scale + y) * width + left * scale + x) * 4;
						pixels[offset] = (byte)(colour >> 24);
						pixels[offset + 1] = (byte)(colour >> 16);
						pixels[offset + 2] = (byte)(colour >> 8);
						pixels[offset + 3] = (byte)colour;
					}
				}
				// A frame is drawn with its centre at the object position plus the offset
				int offsetX = (originX * 2 + w) * scale / 2;
				int offsetY = (originY * 2 + h) * scale / 2;
				anigroup.AppendLine($"    <!-- frame {f} -->");
				anigroup.AppendLine("    <animation>");
				anigroup.AppendLine($"      <frame x=\"{left * scale}\" y=\"{top * scale}\" w=\"{Math.Max(1, w * scale)}\" h=\"{Math.Max(1, h * scale)}\" offset_x=\"{offsetX}\" offset_y=\"{offsetY}\" />");
				anigroup.AppendLine("    </animation>");
				left += w;
				f++;
			}
		}
		anigroup.AppendLine("  </animations>");
		anigroup.AppendLine("</anigroup>");
		return new ImportedSprite(sprite.ObjectKey, EncodePng(pixels, width, height), anigroup.ToString());
	}

	/// <summary>Splits a row of frames into rows that are not wider than the width, except a frame that is wider alone.</summary>
	private static IEnumerable<List<(uint[] Pixels, int Width, int Height, int OriginX, int OriginY)>> SplitRow(
		List<(uint[] Pixels, int Width, int Height, int OriginX, int OriginY)> row, int maxWidth)
	{
		var current = new List<(uint[] Pixels, int Width, int Height, int OriginX, int OriginY)>();
		int width = 0;
		foreach (var image in row)
		{
			if (current.Count > 0 && width + image.Width > maxWidth)
			{
				yield return current;
				current = new List<(uint[] Pixels, int Width, int Height, int OriginX, int OriginY)>();
				width = 0;
			}
			current.Add(image);
			width += image.Width;
		}
		yield return current;
	}

	/// <summary>Draws one frame. The first piece is in front, as on the Mega Drive.</summary>
	/// <returns>The pixels (RGBA), the size, and the position of the top left corner from the object position.</returns>
	public static (uint[] Pixels, int Width, int Height, int OriginX, int OriginY) Render(List<SpritePiece> frame, byte[] art, uint[] palette, int paletteLine)
	{
		if (frame.Count == 0)
		{
			return (new uint[0], 0, 0, 0, 0);
		}
		int minX = frame.Min(p => p.X);
		int minY = frame.Min(p => p.Y);
		int width = frame.Max(p => p.X + p.Width * TileSize) - minX;
		int height = frame.Max(p => p.Y + p.Height * TileSize) - minY;
		var pixels = new uint[width * height];
		for (int i = frame.Count - 1; i >= 0; i--)
		{
			SpritePiece piece = frame[i];
			int line = (paletteLine + piece.Palette) & 3;
			for (int column = 0; column < piece.Width; column++)
			{
				for (int row = 0; row < piece.Height; row++)
				{
					int sourceColumn = piece.FlipX ? piece.Width - 1 - column : column;
					int sourceRow = piece.FlipY ? piece.Height - 1 - row : row;
					int tile = piece.Tile + sourceColumn * piece.Height + sourceRow;
					for (int y = 0; y < TileSize; y++)
					{
						for (int x = 0; x < TileSize; x++)
						{
							int tx = piece.FlipX ? TileSize - 1 - x : x;
							int ty = piece.FlipY ? TileSize - 1 - y : y;
							int offset = tile * BytesPerTile + ty * 4 + tx / 2;
							if (offset >= art.Length)
							{
								continue;
							}
							int index = (tx % 2 == 0 ? art[offset] >> 4 : art[offset]) & 0xF;
							if (index == 0)
							{
								continue;
							}
							int px = piece.X - minX + column * TileSize + x;
							int py = piece.Y - minY + row * TileSize + y;
							pixels[py * width + px] = palette[line * 16 + index];
						}
					}
				}
			}
		}
		return (pixels, width, height, minX, minY);
	}

	internal static byte[] EncodePng(byte[] pixels, int width, int height)
	{
		var stream = new MemoryStream();
		var writer = new PngWriter(stream, new ImageInfo(width, height, 8, true), "sprite.png");
		writer.ShouldCloseStream = false;
		byte[] row = new byte[width * 4];
		for (int y = 0; y < height; y++)
		{
			Array.Copy(pixels, y * width * 4, row, 0, row.Length);
			writer.WriteRowByte(row, y);
		}
		writer.End();
		return stream.ToArray();
	}
}
