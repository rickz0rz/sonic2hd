using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace SonicOrca.Original;

/// <summary>
/// Makes the original credits of the disassembly: the slides of text and the Sonic 2 logo at the end. The original
/// draws each slide on plane A with the credits font, then fades it in and out. The import writes the font as one
/// white image for each character (<see cref="FontFolder"/>) and the text of the slides as an XML file
/// (<see cref="SlidesKey"/>). The game makes each line of text from the images of its characters and gives each line
/// the colour of its palette line, so that it can fade the colours as the original does. A modder can change one
/// character or one line of text. The logo flashes with a cycle of 9 palettes, so the import draws it once for each
/// palette.
/// </summary>
public static class OriginalCredits
{
	/// <summary>The size of the screen of the original, in pixels.</summary>
	public const int ScreenWidth = 320;

	public const int ScreenHeight = 224;

	/// <summary>The resource folder of the original credits.</summary>
	public const string ResourceKey = "SONICORCA/CREDITS/ORIGINAL";

	/// <summary>The folder of the images of the characters, below <see cref="ResourceKey"/>.</summary>
	public const string FontFolder = "FONT";

	/// <summary>The XML file of the text of the slides, below <see cref="ResourceKey"/>.</summary>
	public const string SlidesKey = "SLIDES";

	/// <summary>The height of a character of the font, in pixels of the original. The images of the font can be larger.</summary>
	public const int CharacterHeight = TileSize;

	/// <summary>The width of a space, in pixels of the original.</summary>
	public const int SpaceWidth = TileSize;

	/// <summary>The copyright sign. The text of the disassembly has '@' for it.</summary>
	public const char CopyrightSign = '\u00A9';

	/// <summary>The palette lines of the text. Line 0 is white and line 1 is yellow.</summary>
	public const int TextLines = 2;

	/// <summary>The number of palettes of the cycle of the logo, in "art/palettes/Ending Cycle.bin".</summary>
	public const int LogoPalettes = 9;

	public const int TileSize = 8;

	private const int BytesPerTile = 32;

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

	/// <summary>A line of text of a slide: its palette line, its text, and the position of its top left corner in pixels of the original.</summary>
	public sealed record TextItem(int Line, string Text, int X, int Y);

	/// <summary>
	/// A slide: its lines of text, and optionally the frames that it shows at full colour. Without frames, the slide
	/// gets an equal part of the time that the other slides leave (see <see cref="GetHoldFrames"/>).
	/// </summary>
	public sealed record Slide(IReadOnlyList<TextItem> Items, int? Frames = null);

	/// <summary>The number of slides of the original credits.</summary>
	public const int OriginalSlideCount = 21;

	/// <summary>A fade of the original (Pal_FadeFromBlack or Pal_FadeToBlack) takes 22 frames.</summary>
	public const int FadeFrames = 22;

	/// <summary>The frames that a slide of the original shows at full colour: $18E + 1 at 60 frames per second.</summary>
	public const int OriginalHoldFrames = 0x18E + 1;

	/// <summary>
	/// The frames of all the slides of the original, with their fades: 9303. The logo then comes at the end of the
	/// song, so the slides of a mod share this time.
	/// </summary>
	public const int SlideBudgetFrames = OriginalSlideCount * (FadeFrames + OriginalHoldFrames + FadeFrames);

	/// <summary>The fewest frames at full colour of a slide without frames: one second.</summary>
	public const int MinimumHoldFrames = 60;

	/// <summary>
	/// Gives the frames at full colour of each slide. A slide with frames keeps them. The other slides share the rest
	/// of <see cref="SlideBudgetFrames"/> equally, after the fades. Thus with the 21 slides of the original, each slide
	/// gets 399 frames, as in the original. A slide without frames gets at least <see cref="MinimumHoldFrames"/>. Then
	/// the slides can take more than the budget, and the logo comes after the end of the song.
	/// </summary>
	public static int[] GetHoldFrames(IReadOnlyList<Slide> slides)
	{
		int[] frames = new int[slides.Count];
		int rest = SlideBudgetFrames - slides.Count * 2 * FadeFrames;
		List<int> shared = new List<int>();
		for (int i = 0; i < slides.Count; i++)
		{
			if (slides[i].Frames is int fixedFrames)
			{
				frames[i] = Math.Max(fixedFrames, 0);
				rest -= frames[i];
			}
			else
			{
				shared.Add(i);
			}
		}
		if (shared.Count == 0)
		{
			return frames;
		}
		int part = rest / shared.Count;
		int extra = rest % shared.Count;
		if (part < MinimumHoldFrames)
		{
			part = MinimumHoldFrames;
			extra = 0;
		}
		// The frames that do not divide equally go to the first slides, one frame each
		for (int j = 0; j < shared.Count; j++)
		{
			frames[shared[j]] = part + (j < extra ? 1 : 0);
		}
		return frames;
	}

	/// <summary>
	/// Reads the slides of the credits from s2.asm, in their order. The text has <see cref="CopyrightSign"/> in place
	/// of '@'.
	/// </summary>
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
			List<TextItem> items = new List<TextItem>();
			foreach (Match item in Regex.Matches(screen.Groups[1].Value, @"(\w+)\s*,\s*textLoc\(\s*\$?([0-9A-Fa-f]+)\s*,\s*\$?([0-9A-Fa-f]+)\s*\)"))
			{
				(int line, string text) = texts[item.Groups[1].Value];
				int column = int.Parse(item.Groups[2].Value, NumberStyles.HexNumber);
				int row = int.Parse(item.Groups[3].Value, NumberStyles.HexNumber);
				items.Add(new TextItem(line, text.Replace('@', CopyrightSign), column * TileSize, row * TileSize));
			}
			slides.Add(new Slide(items));
		}
		return slides;
	}

	/// <summary>Gives the tiles of a text, as the macro creditText writes them.</summary>
	public static List<int> Encode(string text)
	{
		List<int> tiles = new List<int>();
		foreach (char c in text.Replace(CopyrightSign, '@'))
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

	/// <summary>
	/// Draws a character of the font as a white image (PNG), as the macro creditText writes it: one or two tiles
	/// wide, one tile high.
	/// </summary>
	public static byte[] RenderCharacter(char c, byte[] font)
	{
		List<int> tiles = Encode(c.ToString());
		int width = tiles.Count * TileSize;
		byte[] pixels = new byte[width * CharacterHeight * 4];
		for (int i = 0; i < tiles.Count; i++)
		{
			// Tile 0 is empty: the space
			if (tiles[i] >= FontTile)
			{
				DrawTileAt(pixels, width, CharacterHeight, font, tiles[i] - FontTile, i * TileSize, 0, (x, y, index) => 0xFFFFFFFFu);
			}
		}
		return SpriteImport.EncodePng(pixels, width, CharacterHeight);
	}

	/// <summary>
	/// The name of the image of a character in <see cref="FontFolder"/>: the letter or the digit, a name for the
	/// punctuation of the credits, or U and the code of the character (U00E9 for e acute).
	/// </summary>
	public static string GetCharacterName(char c)
	{
		if (c is >= 'A' and <= 'Z' or >= '0' and <= '9')
		{
			return c.ToString();
		}
		return c switch
		{
			'.' => "FULLSTOP",
			'\'' => "APOSTROPHE",
			'(' => "LEFTBRACKET",
			')' => "RIGHTBRACKET",
			CopyrightSign => "COPYRIGHT",
			_ => "U" + ((int)c).ToString("X4", CultureInfo.InvariantCulture)
		};
	}

	/// <summary>Writes the slides as XML: a slide element for each slide, with a text element for each line of text.</summary>
	public static string WriteSlides(IEnumerable<Slide> slides)
	{
		XElement root = new XElement("credits",
			new XComment(" x and y are in pixels of the original screen (320 x 224). line 0 is white, line 1 is yellow. "),
			new XComment(" frames on a slide sets the frames at full colour (60 a second). The slides without frames share the rest of the time of the song. "),
			slides.Select(slide => new XElement("slide",
				slide.Frames is int frames ? new XAttribute("frames", frames) : null,
				slide.Items.Select(item => new XElement("text",
					new XAttribute("line", item.Line),
					new XAttribute("x", item.X),
					new XAttribute("y", item.Y),
					item.Text)))));
		return new XDocument(new XDeclaration("1.0", "utf-8", null), root).Declaration + "\n" + root + "\n";
	}

	/// <summary>Reads the slides from the XML that <see cref="WriteSlides"/> makes.</summary>
	public static List<Slide> ParseSlides(XmlDocument document)
	{
		List<Slide> slides = new List<Slide>();
		foreach (XmlElement slide in document.SelectNodes("credits/slide").OfType<XmlElement>())
		{
			slides.Add(new Slide(slide.SelectNodes("text").OfType<XmlElement>()
				.Select(text => new TextItem(
					int.Parse(text.GetAttribute("line"), CultureInfo.InvariantCulture),
					text.InnerText,
					int.Parse(text.GetAttribute("x"), CultureInfo.InvariantCulture),
					int.Parse(text.GetAttribute("y"), CultureInfo.InvariantCulture)))
				.ToList(),
				int.TryParse(slide.GetAttribute("frames"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int frames) ? frames : null));
		}
		return slides;
	}

	/// <summary>Draws the logo with each palette of its cycle, as PNG files of the full screen.</summary>
	/// <param name="logoRow">The row of the logo on the plane: KiS2 moved it down to row 15.</param>
	/// <param name="overlay">Draws more art on each image, or null.</param>
	public static byte[][] RenderLogo(string disassembly, int logoRow = LogoRow, Action<byte[]> overlay = null)
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
					DrawTile(pixels, art, entry & 0x7FF, LogoColumn + column, logoRow + row, (x, y, index) => palette[line * 16 + index],
						(entry & 0x800) != 0, (entry & 0x1000) != 0);
				}
			}
			overlay?.Invoke(pixels);
			images[p] = SpriteImport.EncodePng(pixels, ScreenWidth, ScreenHeight);
		}
		return images;
	}

	/// <summary>The prefix of the names of the HD glyphs in the resources of the assembly (see SonicOrca.csproj).</summary>
	private const string HdGlyphPrefix = "CreditsFont/";

	/// <summary>
	/// Gives the names of the HD glyphs of this repository, for example "A" or "FULLSTOP". The import writes an HD
	/// glyph in place of the glyph of the ROM. The ROM gives the glyphs that do not have an HD glyph.
	/// </summary>
	public static IEnumerable<string> GetHdGlyphNames() =>
		typeof(OriginalCredits).Assembly.GetManifestResourceNames()
			.Where(name => name.StartsWith(HdGlyphPrefix, StringComparison.Ordinal) && name.EndsWith(".png", StringComparison.Ordinal))
			.Select(name => name.Substring(HdGlyphPrefix.Length, name.Length - HdGlyphPrefix.Length - ".png".Length));

	/// <summary>Gives the PNG file of the HD glyph with a name of <see cref="GetCharacterName"/>, or null.</summary>
	public static byte[] GetHdGlyph(string name)
	{
		using Stream stream = typeof(OriginalCredits).Assembly.GetManifestResourceStream(HdGlyphPrefix + name + ".png");
		if (stream == null)
		{
			return null;
		}
		using MemoryStream memory = new MemoryStream();
		stream.CopyTo(memory);
		return memory.ToArray();
	}

	/// <summary>Writes the slides and the logo into a folder, in the layout of "s2hd-data unpack". Returns the number of slides.</summary>
	public static int Import(string disassembly, string folder)
	{
		byte[] font = SpriteImport.ReadArt(disassembly, FontFile);
		List<Slide> slides = ReadSlides(disassembly);
		string output = Path.Combine(new[] { folder }.Concat(ResourceKey.Split('/')).ToArray());
		Directory.CreateDirectory(output);
		File.WriteAllText(Path.Combine(output, SlidesKey + ".xml"), WriteSlides(slides));
		// All the capital letters, so that a modder can change the text, and all the other characters of the credits
		string fontFolder = Path.Combine(output, FontFolder);
		Directory.CreateDirectory(fontFolder);
		IEnumerable<char> characters = Enumerable.Range('A', 26).Select(c => (char)c)
			.Concat(slides.SelectMany(slide => slide.Items).SelectMany(item => item.Text))
			.Where(c => c != ' ')
			.Distinct();
		foreach (char c in characters)
		{
			string name = GetCharacterName(c);
			File.WriteAllBytes(Path.Combine(fontFolder, name + ".png"), GetHdGlyph(name) ?? RenderCharacter(c, font));
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

	/// <summary>The logo of Knuckles in Sonic 2, next to LOGO: the logo 4 rows lower, with the banner above it.</summary>
	public const string KnucklesLogoFolder = "LOGOK";

	/// <summary>
	/// Writes the logo of the end of the credits of Knuckles in Sonic 2 (KiS2) from the folder of KiS2, into the folder
	/// <see cref="KnucklesLogoFolder"/>. KiS2 moves the logo down from row 11 to row 15 (planeLoc(64,12,15)), and shows
	/// the banner "KNUCKLES THE ECHIDNA IN" above it (Obj0E_CreditsBanner at 160, 68) and the trademark sign
	/// (Obj0E_CreditsTrademark at 292, 48). They use palette line 3, Pal_KiS2_Ending.
	/// </summary>
	public static void ImportKnucklesLogo(string kis2, string folder)
	{
		const int otherTextTile = 0x3DD; // ArtTile_ArtNem_TitleOtherText
		byte[] vram = new byte[0x500 * BytesPerTile];
		void Load(string file, int tile)
		{
			byte[] data = SpriteImport.ReadArt(kis2, file);
			Array.Copy(data, 0, vram, tile * BytesPerTile, Math.Min(data.Length, vram.Length - tile * BytesPerTile));
		}
		Load("art/nemesis/Other text from title screen.nem", otherTextTile);
		Load("art/nemesis/Giant banner from title screen.nem", 0x3FE); // ArtTile_ArtNem_TitleBanner
		Load("art/nemesis/'THE ECHIDNA IN' from title screen.nem", 0x488); // ArtTile_ArtNem_TitleTheEchidnaIn
		uint[] banner = SpriteImport.ReadColours(File.ReadAllBytes(Path.Combine(kis2, "art/palettes/Ending Knuckles Banner.bin")));
		List<SpritePiece> bannerPieces = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(kis2, "mappings/sprite/obj0E_banner.asm")))[0];
		List<SpritePiece> trademark = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(kis2, "mappings/sprite/obj0E_other_text.asm")))[2];
		void Overlay(byte[] pixels)
		{
			// The banner has its tiles from tile 0 (ArtTile_ArtNem_Title), the trademark from the other text
			DrawSprite(pixels, vram, bannerPieces, 0, 160, 68, index => banner[index]);
			DrawSprite(pixels, vram, trademark, otherTextTile, 292, 48, index => banner[index]);
		}
		string logoFolder = Path.Combine(new[] { folder }.Concat(ResourceKey.Split('/')).Append(KnucklesLogoFolder).ToArray());
		Directory.CreateDirectory(logoFolder);
		byte[][] logo = RenderLogo(kis2, 15, Overlay);
		for (int p = 0; p < logo.Length; p++)
		{
			File.WriteAllBytes(Path.Combine(logoFolder, $"PALETTE{p}.png"), logo[p]);
		}
	}

	/// <summary>Draws the pieces of a sprite frame at a position on the screen. The tiles of a piece go down its columns.</summary>
	private static void DrawSprite(byte[] pixels, byte[] vram, IEnumerable<SpritePiece> pieces, int baseTile, int x, int y, Func<int, uint> colour)
	{
		foreach (SpritePiece piece in pieces)
		{
			for (int column = 0; column < piece.Width; column++)
			{
				for (int row = 0; row < piece.Height; row++)
				{
					int tile = baseTile + piece.Tile + column * piece.Height + row;
					DrawTileAt(pixels, vram, tile, x + piece.X + column * TileSize, y + piece.Y + row * TileSize, (px, py, index) => colour(index));
				}
			}
		}
	}

	private static int GetTile(char c)
	{
		if (!Charset.TryGetValue(c, out int tile))
		{
			throw new InvalidDataException($"The credits font has no character '{c}'.");
		}
		return tile;
	}

	/// <summary>Draws a tile of 8 x 8 pixels on the screen at a position in tiles. Colour 0 is transparent.</summary>
	private static void DrawTile(byte[] pixels, byte[] art, int tile, int column, int row, Func<int, int, int, uint> colour, bool flipX = false, bool flipY = false)
	{
		DrawTileAt(pixels, ScreenWidth, ScreenHeight, art, tile, column * TileSize, row * TileSize, colour, flipX, flipY);
	}

	/// <summary>Draws a tile of 8 x 8 pixels on the screen at a position in pixels.</summary>
	private static void DrawTileAt(byte[] pixels, byte[] art, int tile, int left, int top, Func<int, int, int, uint> colour, bool flipX = false, bool flipY = false)
	{
		DrawTileAt(pixels, ScreenWidth, ScreenHeight, art, tile, left, top, colour, flipX, flipY);
	}

	/// <summary>
	/// Draws a tile of 8 x 8 pixels at a position in pixels on an image. Colour 0 is transparent, and pixels off the
	/// image do not draw.
	/// </summary>
	private static void DrawTileAt(byte[] pixels, int width, int height, byte[] art, int tile, int left, int top, Func<int, int, int, uint> colour, bool flipX = false, bool flipY = false)
	{
		for (int y = 0; y < TileSize; y++)
		{
			for (int x = 0; x < TileSize; x++)
			{
				if (left + x < 0 || left + x >= width || top + y < 0 || top + y >= height)
				{
					continue;
				}
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
				int p = ((top + y) * width + left + x) * 4;
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
