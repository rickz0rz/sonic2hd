using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SonicOrca.Original;

/// <summary>
/// Makes the original credits of the disassembly: the slides of text and the Sonic 2 logo at the end. The original
/// draws each slide on plane A with the credits font, then fades it in and out. The import draws each slide as two
/// masks, one for each palette line of the text, so that the game can fade the colours as the original does. The
/// logo flashes with a cycle of 9 palettes, so the import draws it once for each palette.
/// </summary>
public static class OriginalCredits
{
	/// <summary>The size of the screen of the original, in pixels.</summary>
	public const int ScreenWidth = 320;

	public const int ScreenHeight = 224;

	/// <summary>The resource folder of the original credits.</summary>
	public const string ResourceKey = "SONICORCA/CREDITS/ORIGINAL";

	/// <summary>The palette lines of the text. Line 0 is white and line 1 is yellow.</summary>
	public const int TextLines = 2;

	/// <summary>The number of palettes of the cycle of the logo, in "art/palettes/Ending Cycle.bin".</summary>
	public const int LogoPalettes = 9;

	private const int TileSize = 8;

	private const int BytesPerTile = 32;

	private const int PlaneColumns = ScreenWidth / TileSize;

	private const int PlaneRows = ScreenHeight / TileSize;

	private const string SourceFile = "s2.asm";

	private const string FontFile = "art/nemesis/Credit Text.nem";

	private const string LogoArtFile = "art/nemesis/Sonic the Hedgehog 2 image at end of credits.nem";

	private const string LogoMapFile = "mappings/misc/Sonic 2 end of game logo.eni";

	private const string LogoPaletteFile = "art/palettes/Ending Cycle.bin";

	/// <summary>The VRAM tile of the font in the credits (ArtTile_ArtNem_CreditText_CredScr). The text has VRAM tiles.</summary>
	private const int FontTile = 1;

	/// <summary>The position of the logo on the plane, in tiles (planeLoc(64,12,11)), and its size (16 x 6 tiles).</summary>
	private const int LogoColumn = 12;

	private const int LogoRow = 11;

	private const int LogoColumns = 16;

	private const int LogoRows = 6;

	/// <summary>
	/// The tiles of the characters of the credits, from the "charset" lines before the credits text in s2.asm. A
	/// letter has a left tile (its capital) and a right tile (its lower case). The macro creditText writes the right
	/// tile after the left tile. I has one tile.
	/// </summary>
	private static readonly Dictionary<char, int> Charset = MakeCharset();

	/// <summary>A slide: the text items, each with its palette line, its text and its position in tiles.</summary>
	public sealed record Slide(IReadOnlyList<(int Line, string Text, int Column, int Row)> Items);

	/// <summary>Reads the slides of the credits from s2.asm, in their order.</summary>
	public static List<Slide> ReadSlides(string disassembly)
	{
		string source = File.ReadAllText(Path.Combine(disassembly, SourceFile));
		Dictionary<string, (int Line, string Text)> texts = new Dictionary<string, (int, string)>();
		// A label can have two versions for the two revisions of the game. The last one is the revision 1 text.
		foreach (Match match in Regex.Matches(source, @"^(\w+):\s*creditText\s+(\d)\s*,\s*""([^""]*)""", RegexOptions.Multiline))
		{
			texts[match.Groups[1].Value] = (int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), match.Groups[3].Value);
		}
		Match table = Regex.Match(source, @"^off_B2CA:\s*\n((?:\s*dc\.l[^\n]*\n)+)", RegexOptions.Multiline);
		if (!table.Success)
		{
			throw new InvalidDataException("s2.asm has no table of the credits screens (off_B2CA).");
		}
		List<Slide> slides = new List<Slide>();
		foreach (Match label in Regex.Matches(table.Groups[1].Value, @"\b(off_\w+)|(-1)"))
		{
			if (label.Groups[2].Success)
			{
				break;
			}
			Match screen = Regex.Match(source, "^" + label.Groups[1].Value + @":\s*creditsPtrs\s*([^\n]*)", RegexOptions.Multiline);
			if (!screen.Success)
			{
				throw new InvalidDataException($"s2.asm has no credits screen {label.Groups[1].Value}.");
			}
			List<(int, string, int, int)> items = new List<(int, string, int, int)>();
			foreach (Match item in Regex.Matches(screen.Groups[1].Value, @"(\w+)\s*,\s*textLoc\(\s*\$?([0-9A-Fa-f]+)\s*,\s*\$?([0-9A-Fa-f]+)\s*\)"))
			{
				(int line, string text) = texts[item.Groups[1].Value];
				items.Add((line, text, int.Parse(item.Groups[2].Value, NumberStyles.HexNumber), int.Parse(item.Groups[3].Value, NumberStyles.HexNumber)));
			}
			slides.Add(new Slide(items));
		}
		return slides;
	}

	/// <summary>Gives the tiles of a text, as the macro creditText writes them.</summary>
	public static List<int> Encode(string text)
	{
		List<int> tiles = new List<int>();
		foreach (char c in text)
		{
			tiles.Add(GetTile(c));
			// In the macro, "case "I"" has no body of its own: AS does not go on to the next case. So I is one tile wide.
			char? second = c switch
			{
				'I' => null,
				'1' => '!',
				'2' => '$',
				'9' => '#',
				_ => char.ToLowerInvariant(c) != c ? char.ToLowerInvariant(c) : null
			};
			if (second is char right)
			{
				tiles.Add(GetTile(right));
			}
		}
		return tiles;
	}

	/// <summary>Draws the masks of a slide: a white image of the text of each palette line, as PNG files.</summary>
	public static byte[][] RenderSlide(Slide slide, byte[] font)
	{
		byte[][] masks = new byte[TextLines][];
		for (int line = 0; line < TextLines; line++)
		{
			byte[] pixels = new byte[ScreenWidth * ScreenHeight * 4];
			foreach ((int itemLine, string text, int column, int row) in slide.Items.Where(item => item.Line == line))
			{
				List<int> tiles = Encode(text);
				for (int i = 0; i < tiles.Count && column + i < PlaneColumns; i++)
				{
					// Tile 0 is empty: the space
					if (tiles[i] >= FontTile)
					{
						DrawTile(pixels, font, tiles[i] - FontTile, column + i, row, (x, y, index) => 0xFFFFFFFFu);
					}
				}
			}
			masks[line] = SpriteImport.EncodePng(pixels, ScreenWidth, ScreenHeight);
		}
		return masks;
	}

	/// <summary>Draws the logo with each palette of its cycle, as PNG files of the full screen.</summary>
	public static byte[][] RenderLogo(string disassembly)
	{
		byte[] art = SpriteImport.ReadArt(disassembly, LogoArtFile);
		ushort[] map = Enigma.Decompress(File.ReadAllBytes(Path.Combine(disassembly, LogoMapFile)));
		uint[] cycle = SpriteImport.ReadColours(File.ReadAllBytes(Path.Combine(disassembly, LogoPaletteFile)));
		byte[][] images = new byte[LogoPalettes][];
		for (int p = 0; p < LogoPalettes; p++)
		{
			// EndgameLogoFlash writes 12 colours to colours 1 to 12 of line 0. The fade to black before the logo made
			// all the other colours black.
			uint[] palette = new uint[64];
			for (int i = 0; i < 12; i++)
			{
				palette[1 + i] = cycle[p * 12 + i];
			}
			byte[] pixels = new byte[ScreenWidth * ScreenHeight * 4];
			for (int row = 0; row < LogoRows; row++)
			{
				for (int column = 0; column < LogoColumns; column++)
				{
					ushort entry = map[row * LogoColumns + column];
					int line = (entry >> 13) & 3;
					DrawTile(pixels, art, entry & 0x7FF, LogoColumn + column, LogoRow + row, (x, y, index) => palette[line * 16 + index],
						(entry & 0x800) != 0, (entry & 0x1000) != 0);
				}
			}
			images[p] = SpriteImport.EncodePng(pixels, ScreenWidth, ScreenHeight);
		}
		return images;
	}

	/// <summary>Writes the slides and the logo into a folder, in the layout of "s2hd-data unpack". Returns the number of slides.</summary>
	public static int Import(string disassembly, string folder)
	{
		byte[] font = SpriteImport.ReadArt(disassembly, FontFile);
		List<Slide> slides = ReadSlides(disassembly);
		string output = Path.Combine(new[] { folder }.Concat(ResourceKey.Split('/')).ToArray());
		for (int i = 0; i < slides.Count; i++)
		{
			string slideFolder = Path.Combine(output, GetSlideName(i));
			Directory.CreateDirectory(slideFolder);
			byte[][] masks = RenderSlide(slides[i], font);
			for (int line = 0; line < masks.Length; line++)
			{
				File.WriteAllBytes(Path.Combine(slideFolder, $"LINE{line}.png"), masks[line]);
			}
		}
		string logoFolder = Path.Combine(output, "LOGO");
		Directory.CreateDirectory(logoFolder);
		byte[][] logo = RenderLogo(disassembly);
		for (int p = 0; p < logo.Length; p++)
		{
			File.WriteAllBytes(Path.Combine(logoFolder, $"PALETTE{p}.png"), logo[p]);
		}
		return slides.Count;
	}

	/// <summary>The name of the folder of a slide, from 0: SLIDE01, SLIDE02 and so on.</summary>
	public static string GetSlideName(int index) => $"SLIDE{index + 1:00}";

	private static int GetTile(char c)
	{
		if (!Charset.TryGetValue(c, out int tile))
		{
			throw new InvalidDataException($"The credits font has no character '{c}'.");
		}
		return tile;
	}

	/// <summary>Draws a tile of 8 x 8 pixels at a position in tiles. Colour 0 is transparent.</summary>
	private static void DrawTile(byte[] pixels, byte[] art, int tile, int column, int row, Func<int, int, int, uint> colour, bool flipX = false, bool flipY = false)
	{
		for (int y = 0; y < TileSize; y++)
		{
			for (int x = 0; x < TileSize; x++)
			{
				int tx = flipX ? TileSize - 1 - x : x;
				int ty = flipY ? TileSize - 1 - y : y;
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
				uint rgba = colour(x, y, index);
				int p = ((row * TileSize + y) * ScreenWidth + column * TileSize + x) * 4;
				pixels[p] = (byte)(rgba >> 24);
				pixels[p + 1] = (byte)(rgba >> 16);
				pixels[p + 2] = (byte)(rgba >> 8);
				pixels[p + 3] = (byte)rgba;
			}
		}
	}

	private static Dictionary<char, int> MakeCharset()
	{
		Dictionary<char, int> charset = new Dictionary<char, int>();
		void Add(char first, params int[] tiles)
		{
			for (int i = 0; i < tiles.Length; i++)
			{
				charset[(char)(first + i)] = tiles[i];
			}
		}
		// charset '@',"\x3B\2\4\6\8\xA\xC\xE\x10\x12\x13\x15\x17\x19\x1B\x1D\x1F\x21\x23\x25\x27\x29\x2B\x2D\x2F\x31\x33"
		Add('@', 0x3B, 0x02, 0x04, 0x06, 0x08, 0x0A, 0x0C, 0x0E, 0x10, 0x12, 0x13, 0x15, 0x17, 0x19, 0x1B, 0x1D, 0x1F, 0x21,
			0x23, 0x25, 0x27, 0x29, 0x2B, 0x2D, 0x2F, 0x31, 0x33);
		// charset 'a',"\3\5\7\9\xB\xD\xF\x11\x12\x14\x16\x18\x1A\x1C\x1E\x20\x22\x24\x26\x28\x2A\x2C\x2E\x30\x32\x34"
		Add('a', 0x03, 0x05, 0x07, 0x09, 0x0B, 0x0D, 0x0F, 0x11, 0x12, 0x14, 0x16, 0x18, 0x1A, 0x1C, 0x1E, 0x20, 0x22, 0x24,
			0x26, 0x28, 0x2A, 0x2C, 0x2E, 0x30, 0x32, 0x34);
		// charset '!',"\x3D\x39\x3F\x36"
		Add('!', 0x3D, 0x39, 0x3F, 0x36);
		// charset '\H',"\x39\x37\x38": the apostrophe and the brackets
		Add('\'', 0x39, 0x37, 0x38);
		// charset '9',"\x3E\x40\x41"
		Add('9', 0x3E, 0x40, 0x41);
		// charset '1',"\x3C\x35"
		Add('1', 0x3C, 0x35);
		// charset '.',"\x3A"
		Add('.', 0x3A);
		// charset ' ',0
		Add(' ', 0);
		return charset;
	}
}
