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

/// <summary>The sprites of an object of the original game: its art, its mappings and its palette line.</summary>
/// <param name="ObjectKey">The SonicOrca object, for example "SONICORCA/OBJECTS/CNZBUMPER".</param>
/// <param name="ArtFile">The Nemesis art file in the disassembly.</param>
/// <param name="MappingsFile">The sprite mappings file (assembly source) in the disassembly.</param>
/// <param name="PaletteLine">The palette line of make_art_tile for the object.</param>
public sealed record OriginalSprite(string ObjectKey, string ArtFile, string MappingsFile, int PaletteLine);

/// <summary>The files of an imported sprite: a texture with all frames in one row, and an animation group with one animation for each frame.</summary>
public sealed record ImportedSprite(string ObjectKey, byte[] Texture, string AnimationGroup);

/// <summary>
/// Converts the sprites of an object of the original game into a texture at four times the size, and an animation
/// group. Animation N of the group shows mapping frame N, so the object code can follow the animation scripts of the
/// original.
/// </summary>
public static class SpriteImport
{
	private const int TileSize = 8;

	private const int BytesPerTile = 32;

	/// <summary>Reads the frames of a mappings file of s2disasm (mappingsTable, spriteHeader and spritePiece macros).</summary>
	public static List<List<SpritePiece>> ParseMappings(string source)
	{
		var order = new List<string>();
		var frames = new Dictionary<string, List<SpritePiece>>(StringComparer.Ordinal);
		List<SpritePiece> current = null;
		foreach (string rawLine in source.Split('\n'))
		{
			string line = rawLine.Split(';')[0].Trim();
			if (line.Length == 0)
			{
				continue;
			}
			Match entry = Regex.Match(line, @"^mappingsTableEntry\.w\s+(\w+)");
			if (entry.Success)
			{
				order.Add(entry.Groups[1].Value);
				continue;
			}
			Match header = Regex.Match(line, @"^(\w+):\s*spriteHeader");
			if (header.Success)
			{
				current = new List<SpritePiece>();
				frames[header.Groups[1].Value] = current;
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
		var palette = new uint[64];
		for (int i = 0; i < palette.Length && i * 2 + 1 < data.Length; i++)
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

	public static ImportedSprite Import(OriginalSprite sprite, string disassembly, uint[] palette, int scale)
	{
		byte[] art = Nemesis.Decompress(File.ReadAllBytes(Path.Combine(disassembly, sprite.ArtFile)));
		List<List<SpritePiece>> frames = ParseMappings(File.ReadAllText(Path.Combine(disassembly, sprite.MappingsFile)));
		var images = frames.Select(frame => Render(frame, art, palette, sprite.PaletteLine)).ToList();
		int width = Math.Max(1, images.Sum(i => i.Width)) * scale;
		int height = Math.Max(1, images.Max(i => i.Height)) * scale;
		var pixels = new byte[width * height * 4];
		var anigroup = new StringBuilder();
		anigroup.AppendLine("<?xml version=\"1.0\"?>");
		anigroup.AppendLine("<anigroup>");
		anigroup.AppendLine("  <textures>");
		anigroup.AppendLine("    <texture>//TEXTURE</texture>");
		anigroup.AppendLine("  </textures>");
		anigroup.AppendLine("  <animations>");
		int left = 0;
		for (int f = 0; f < images.Count; f++)
		{
			(uint[] image, int w, int h, int originX, int originY) = images[f];
			for (int y = 0; y < h * scale; y++)
			{
				for (int x = 0; x < w * scale; x++)
				{
					uint colour = image[(y / scale) * w + x / scale];
					int offset = (y * width + left * scale + x) * 4;
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
			anigroup.AppendLine($"      <frame x=\"{left * scale}\" y=\"0\" w=\"{Math.Max(1, w * scale)}\" h=\"{Math.Max(1, h * scale)}\" offset_x=\"{offsetX}\" offset_y=\"{offsetY}\" />");
			anigroup.AppendLine("    </animation>");
			left += w;
		}
		anigroup.AppendLine("  </animations>");
		anigroup.AppendLine("</anigroup>");
		return new ImportedSprite(sprite.ObjectKey, EncodePng(pixels, width, height), anigroup.ToString());
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

	private static byte[] EncodePng(byte[] pixels, int width, int height)
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
