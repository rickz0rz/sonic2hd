using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonicOrca.Original;

/// <summary>
/// Makes the art of the ending cutscene of the original (EndingSequence in s2.asm) as normal resources, so that a mod
/// can replace each image. The images are four times the size of the original, as the other imported sprites:
/// <list type="bullet">
/// <item>PICTURES/1 to 4: the photos of the start (MapEng_Ending1 to 4 over ArtNem_EndingPics), 96 x 72 pixels in the original.</item>
/// <item>SONIC: the sprites of Sonic in the ending. Animation 0 is the fall (SonAni_Float2), 1 is the wait.</item>
/// <item>CLOUDS: the clouds that fly past (ObjCB, with the mappings of ObjB3). Animation N is mapping frame N.</item>
/// <item>TORNADO: the plane that comes to get Sonic (ObjCC with the sprites of ObjB2), without the pilot. Animation N is
/// mapping frame N. TORNADO/PILOT: the pilot, Tails, at his place in the plane: one animation for each frame of
/// Tails_pilot_frames.</item>
/// <item>BIRDS: the birds that fly with the plane (ObjCD with the mappings of Obj28 and the art of the Flicky).</item>
/// <item>PLANE: the frames of ObjCF (the Tornado that comes near, Sonic and Tails who jump, and the propeller).
/// Animation N is mapping frame N.</item>
/// <item>CLOSEUP: the close-up of the plane with Tails at the end (MapEng_EndingTailsPlane), 184 x 120 pixels in the original.</item>
/// </list>
/// The colours are the colours of the ending palettes, which ObjC9 loads.
/// </summary>
public static class EndingArt
{
	/// <summary>The resource folder of the art of the ending.</summary>
	public const string ResourceKey = "SONICORCA/ENDING";

	/// <summary>The size of a photo in the original, in pixels: 12 x 9 tiles.</summary>
	public const int PictureWidth = 96;

	public const int PictureHeight = 72;

	public const int PictureCount = 4;

	/// <summary>The animations of the group of Sonic.</summary>
	public const int SonicFallAnimation = 0;

	public const int SonicWaitAnimation = 1;

	private const int Scale = 4;

	private const int TileSize = 8;

	private const int BytesPerTile = 32;

	/// <summary>The VRAM tile of the photos (ArtTile_ArtNem_EndingPics). The Enigma maps add it to their entries.</summary>
	private const int PicturesTile = 0x328;

	private const string PicturesArtFile = "art/nemesis/Movie sequence at end of game.nem";

	private const string PictureMapFile = "mappings/misc/End of game sequence frame {0}.eni";

	private const string PhotosPaletteFile = "art/palettes/Ending Photos.bin";

	private const string SonicPaletteFile = "art/palettes/Ending Sonic.bin";

	private const string TailsPaletteFile = "art/palettes/Ending Tails.bin";

	private const string BackgroundPaletteFile = "art/palettes/Ending Background.bin";

	private const string SonicArtFile = "art/uncompressed/Sonic's art.bin";

	private const string SonicMappingsFile = "mappings/sprite/Sonic.asm";

	private const string SonicDplcFile = "mappings/spriteDPLC/Sonic.asm";

	private const string TailsArtFile = "art/uncompressed/Tails's art.bin";

	private const string TailsDplcFile = "mappings/spriteDPLC/Tails.asm";

	/// <summary>The tile of the pilot in the mappings of the Tornado: the VRAM of Tails (ArtTile_ArtUnc_Tails - ArtTile_ArtNem_Tornado).</summary>
	private const int PilotTile = 0x2A0;

	/// <summary>The frames of Tails that ObjB2_Animate_Pilot loads for the pilot (Tails_pilot_frames, without repeats).</summary>
	public static readonly int[] PilotFrames = { 0x10, 0x01, 0x02, 0x03, 0x04 };

	/// <summary>The size of the close-up in the original: 23 x 15 tiles.</summary>
	public const int CloseUpColumns = 23;

	public const int CloseUpRows = 15;

	/// <summary>The art in VRAM in the ending, with the first tile of each (s2.constants.asm).</summary>
	private static readonly (string File, int Tile)[] VramArt =
	{
		("art/nemesis/Small pictures of Sonic and final image of Sonic.nem", 0x19), // ArtTile_EndingCharacter
		("art/nemesis/Final image of Tornado with it and Sonic facing screen.nem", 0x156), // ArtTile_ArtNem_EndingFinalTornado
		("art/nemesis/Movie sequence at end of game.nem", 0x328), // ArtTile_ArtNem_EndingPics
		("art/nemesis/Small pictures of Tornado in final ending sequence.nem", 0x493), // ArtTile_ArtNem_EndingMiniTornado
		("art/nemesis/The Tornado.nem", 0x500), // ArtTile_ArtNem_Tornado
		("art/nemesis/Clouds.nem", 0x54F), // ArtTile_ArtNem_Clouds
		("art/nemesis/Flicky.nem", 0x594), // ArtTile_ArtNem_Animal_2
	};

	/// <summary>The animations of Sonic in the ending, from the scripts of Sonic in s2.asm.</summary>
	private static readonly OriginalScript[] SonicScripts =
	{
		new OriginalScript("Fall (SonAni_Float2)", 7, new[] { 0x54, 0x55, 0x56, 0x57, 0x58 }),
		// loc_A53A sets mapping frame 1 with a long duration: the first frame of SonAni_Wait
		new OriginalScript("Wait (SonAni_Wait)", 5, new[] { 0x01 }),
	};

	/// <summary>Writes the art into a folder, in the layout of "s2hd-data unpack".</summary>
	public static void Import(string disassembly, string folder)
	{
		string output = Path.Combine(new[] { folder }.Concat(ResourceKey.Split('/')).ToArray());
		string pictures = Path.Combine(output, "PICTURES");
		Directory.CreateDirectory(pictures);
		for (int i = 1; i <= PictureCount; i++)
		{
			File.WriteAllBytes(Path.Combine(pictures, $"{i}.png"), RenderPicture(disassembly, i));
		}
		WriteSprite(output, "SONIC", ImportSonic(disassembly));
		WriteSprite(output, "CLOUDS", ImportClouds(disassembly));
		uint[] palette = ReadSonicPalette(disassembly);
		WriteSprite(output, "TORNADO", ImportTornado(disassembly, palette));
		WriteSprite(Path.Combine(output, "TORNADO"), "PILOT", ImportPilot(disassembly, palette));
		WriteSprite(output, "BIRDS", SpriteImport.Import(new OriginalSprite(ResourceKey + "/BIRDS", "art/nemesis/Flicky.nem", "mappings/sprite/obj28_a.asm", 0), disassembly, palette, Scale));
		byte[] vram = ReadVram(disassembly);
		List<List<SpritePiece>> planeFrames = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, "mappings/sprite/objCF.asm")));
		WriteSprite(output, "PLANE", SpriteImport.Import(ResourceKey + "/PLANE", vram, planeFrames, new[] { 0 }, Array.Empty<int>(), palette, Scale));
		File.WriteAllBytes(Path.Combine(output, "CLOSEUP.png"), RenderCloseUp(disassembly, vram, palette));
	}

	/// <summary>The art of the ending at its places in VRAM, so that the mappings with VRAM tiles can use it.</summary>
	private static byte[] ReadVram(string disassembly)
	{
		byte[] vram = new byte[0x800 * BytesPerTile];
		foreach ((string file, int tile) in VramArt)
		{
			byte[] art = SpriteImport.ReadArt(disassembly, file);
			Array.Copy(art, 0, vram, tile * BytesPerTile, Math.Min(art.Length, vram.Length - tile * BytesPerTile));
		}
		return vram;
	}

	/// <summary>
	/// The Tornado of ObjB2 without the pilot. The pilot is the piece with the VRAM tiles of Tails, which change on each
	/// frame of the pilot, so it is a group of its own.
	/// </summary>
	private static ImportedSprite ImportTornado(string disassembly, uint[] palette)
	{
		byte[] art = SpriteImport.ReadArt(disassembly, "art/nemesis/The Tornado.nem");
		List<List<SpritePiece>> frames = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, "mappings/sprite/objB2_a.asm")))
			.Select(frame => frame.Where(piece => piece.Tile < PilotTile).ToList()).ToList();
		return SpriteImport.Import(ResourceKey + "/TORNADO", art, frames, new[] { 0 }, Array.Empty<int>(), palette, Scale);
	}

	/// <summary>The pilot: the piece of the Tornado with the tiles of Tails, with each frame of Tails_pilot_frames.</summary>
	private static ImportedSprite ImportPilot(string disassembly, uint[] palette)
	{
		SpritePiece pilot = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, "mappings/sprite/objB2_a.asm")))[0]
			.First(piece => piece.Tile == PilotTile);
		byte[] tails = File.ReadAllBytes(Path.Combine(disassembly, TailsArtFile));
		List<List<(int Tiles, int Offset)>> dplcs = SuperSonicArt.ParseDplc(File.ReadAllText(Path.Combine(disassembly, TailsDplcFile)));
		var art = new List<byte>();
		var frames = new List<List<SpritePiece>>();
		foreach (int id in PilotFrames)
		{
			int firstTile = art.Count / BytesPerTile;
			foreach ((int tiles, int offset) in dplcs[id])
			{
				art.AddRange(tails.AsSpan(offset * BytesPerTile, tiles * BytesPerTile).ToArray());
			}
			// The DPLC loads the tiles to the start of the VRAM of Tails, so the piece shows the first tiles of the frame
			frames.Add(new List<SpritePiece> { pilot with { Tile = firstTile } });
			art.AddRange(new byte[pilot.Width * pilot.Height * BytesPerTile]);
		}
		return SpriteImport.Import(ResourceKey + "/TORNADO/PILOT", art.ToArray(), frames, new[] { 0 }, Array.Empty<int>(), palette, Scale);
	}

	/// <summary>
	/// The close-up of the plane: MapEng_EndingTailsPlane over the art of the final Tornado. The ending of Sonic and
	/// of Super Sonic shows Tails in the plane.
	/// </summary>
	public static byte[] RenderCloseUp(string disassembly, byte[] vram, uint[] palette)
	{
		ushort[] map = Enigma.Decompress(File.ReadAllBytes(Path.Combine(disassembly, "mappings/misc/Closeup of Tails flying plane in ending sequence.eni")), 0x8156);
		int width = CloseUpColumns * TileSize;
		int height = CloseUpRows * TileSize;
		uint[] pixels = new uint[width * height];
		for (int row = 0; row < CloseUpRows; row++)
		{
			for (int column = 0; column < CloseUpColumns; column++)
			{
				ushort entry = map[row * CloseUpColumns + column];
				DrawTile(pixels, width, vram, entry & 0x7FF, column, row, palette, (entry >> 13) & 3, (entry & 0x800) != 0, (entry & 0x1000) != 0);
			}
		}
		return EncodeScaled(pixels, width, height);
	}

	/// <summary>
	/// The palette of the Sonic ending after ObjC9 loads it. Subtype 8 copies $20 colours from Pal_AC7E, which has
	/// only 16, so line 1 gets the first colours of the next palette in the ROM, Pal_AC9E (Tails). Subtype $A loads
	/// the background palette into lines 2 and 3.
	/// </summary>
	public static uint[] ReadSonicPalette(string disassembly)
	{
		uint[] palette = new uint[64];
		SpriteImport.ReadPaletteFile(disassembly, SonicPaletteFile, palette, 0);
		uint[] tails = SpriteImport.ReadColours(File.ReadAllBytes(Path.Combine(disassembly, TailsPaletteFile)));
		Array.Copy(tails, 0, palette, 16, 16);
		SpriteImport.ReadPaletteFile(disassembly, BackgroundPaletteFile, palette, 2);
		return palette;
	}

	/// <summary>Draws a photo: its Enigma map of 12 x 9 tiles over the art of the photos, with the palette of the photos.</summary>
	public static byte[] RenderPicture(string disassembly, int number)
	{
		byte[] art = SpriteImport.ReadArt(disassembly, PicturesArtFile);
		ushort[] map = Enigma.Decompress(File.ReadAllBytes(Path.Combine(disassembly, string.Format(PictureMapFile, number))), PicturesTile);
		uint[] palette = new uint[64];
		SpriteImport.ReadPaletteFile(disassembly, PhotosPaletteFile, palette, 0);
		const int columns = PictureWidth / TileSize;
		const int rows = PictureHeight / TileSize;
		uint[] pixels = new uint[PictureWidth * PictureHeight];
		for (int row = 0; row < rows; row++)
		{
			for (int column = 0; column < columns; column++)
			{
				ushort entry = map[row * columns + column];
				DrawTile(pixels, PictureWidth, art, (entry & 0x7FF) - PicturesTile, column, row, palette, (entry >> 13) & 3,
					(entry & 0x800) != 0, (entry & 0x1000) != 0);
			}
		}
		return EncodeScaled(pixels, PictureWidth, PictureHeight);
	}

	private static ImportedSprite ImportSonic(string disassembly)
	{
		byte[] art = File.ReadAllBytes(Path.Combine(disassembly, SonicArtFile));
		List<List<SpritePiece>> mappings = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, SonicMappingsFile)));
		List<List<(int Tiles, int Offset)>> dplcs = SuperSonicArt.ParseDplc(File.ReadAllText(Path.Combine(disassembly, SonicDplcFile)));
		return DplcSpriteImport.Import(ResourceKey + "/SONIC", art, mappings, dplcs, SonicScripts, ReadSonicPalette(disassembly), 0, new[] { "//TEXTURE" });
	}

	/// <summary>ObjCB uses the sub object data of ObjB3, but with palette line 3 (palette_mask).</summary>
	private static ImportedSprite ImportClouds(string disassembly)
	{
		OriginalSprite clouds = new OriginalSprite(ResourceKey + "/CLOUDS", "art/nemesis/Clouds.nem", "mappings/sprite/objB3.asm", 3);
		return SpriteImport.Import(clouds, disassembly, ReadSonicPalette(disassembly), Scale);
	}

	private static void WriteSprite(string output, string name, ImportedSprite sprite)
	{
		string spriteFolder = Path.Combine(output, name);
		Directory.CreateDirectory(spriteFolder);
		File.WriteAllBytes(Path.Combine(spriteFolder, "TEXTURE.png"), sprite.Texture);
		File.WriteAllText(Path.Combine(spriteFolder, "ANIGROUP.anigroup.xml"), sprite.AnimationGroup);
	}

	/// <summary>Draws a tile of 8 x 8 pixels at a position in tiles. Colour 0 is transparent.</summary>
	private static void DrawTile(uint[] pixels, int width, byte[] art, int tile, int column, int row, uint[] palette, int line, bool flipX, bool flipY)
	{
		for (int y = 0; y < TileSize; y++)
		{
			for (int x = 0; x < TileSize; x++)
			{
				int tx = flipX ? TileSize - 1 - x : x;
				int ty = flipY ? TileSize - 1 - y : y;
				int offset = tile * BytesPerTile + ty * 4 + tx / 2;
				if (tile < 0 || offset >= art.Length)
				{
					continue;
				}
				int index = (tx % 2 == 0 ? art[offset] >> 4 : art[offset]) & 0xF;
				if (index != 0)
				{
					pixels[(row * TileSize + y) * width + column * TileSize + x] = palette[line * 16 + index];
				}
			}
		}
	}

	/// <summary>Encodes RGBA pixels (red in the high byte) as a PNG at <see cref="Scale"/> times the size.</summary>
	private static byte[] EncodeScaled(uint[] pixels, int width, int height)
	{
		byte[] bytes = new byte[width * Scale * height * Scale * 4];
		for (int y = 0; y < height * Scale; y++)
		{
			for (int x = 0; x < width * Scale; x++)
			{
				uint colour = pixels[(y / Scale) * width + x / Scale];
				int offset = (y * width * Scale + x) * 4;
				bytes[offset] = (byte)(colour >> 24);
				bytes[offset + 1] = (byte)(colour >> 16);
				bytes[offset + 2] = (byte)(colour >> 8);
				bytes[offset + 3] = (byte)colour;
			}
		}
		return SpriteImport.EncodePng(bytes, width * Scale, height * Scale);
	}
}
