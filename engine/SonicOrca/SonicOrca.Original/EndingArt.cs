using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonicOrca.Original;

/// <summary>
/// Makes the art of the ending cutscene of the original (EndingSequence in s2.asm) as normal resources, so that a mod
/// can replace each image. The images are four times the size of the original, as the other imported sprites. The
/// ending has three characters (Ending_Routine): Sonic, Super Sonic with the 7 emeralds, and Tails alone. The art of
/// Sonic is in SONICORCA/ENDING, and the art of the other two is in SUPERSONIC and TAILS in that folder:
/// <list type="bullet">
/// <item>PICTURES/1 to 4: the photos of the start (MapEng_Ending1 to 4 over ArtNem_EndingPics), 96 x 72 pixels in the original.</item>
/// <item>The character group (SONIC, SUPERSONIC/CHARACTER or TAILS/CHARACTER): see <see cref="CharacterAnimations"/>.</item>
/// <item>CLOUDS: the clouds that fly past (ObjCB, with the mappings of ObjB3). Animation N is mapping frame N.</item>
/// <item>TORNADO: the plane that comes (ObjCC with the sprites of ObjB2), without the pilot. Animation N is mapping
/// frame N. TORNADO/PILOT (Tails) and TAILS/PILOT (Sonic): the pilot at his place in the plane, one animation for each
/// pilot frame.</item>
/// <item>BIRDS: the birds that fly with the plane (ObjCD with the mappings of Obj28): the Flicky, the eagle or the chicken.</item>
/// <item>PLANE: the frames of ObjCF (the Tornado that comes near, the characters who jump, and the propeller).
/// Animation N is mapping frame N with palette line 0, and animation <see cref="PlaneFrameCount"/> + N is frame N with line 1.</item>
/// <item>CLOSEUP: the close-up of the plane at the end, 184 x 120 pixels in the original. Sonic and Super Sonic see
/// Tails in the plane, and Tails sees Sonic.</item>
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

	/// <summary>The number of mapping frames of ObjCF.</summary>
	public const int PlaneFrameCount = 26;

	/// <summary>The size of the close-up in the original: 23 x 15 tiles.</summary>
	public const int CloseUpColumns = 23;

	public const int CloseUpRows = 15;

	/// <summary>
	/// The animations of a character group: 0 is the fall (SonAni_Float2 or TailsAni_Float2; for Super Sonic, the run of
	/// SupSonAni_Run), 1 is the wait on the plane, and 2 is the swish of the tails of Tails (Obj05Ani_Swish).
	/// </summary>
	public static readonly string[] CharacterAnimations = { "Fall", "Wait", "Tails of Tails" };

	private const int Scale = 4;

	private const int TileSize = 8;

	private const int BytesPerTile = 32;

	/// <summary>The VRAM tile of the photos (ArtTile_ArtNem_EndingPics). The Enigma maps add it to their entries.</summary>
	private const int PicturesTile = 0x328;

	private const string PicturesArtFile = "art/nemesis/Movie sequence at end of game.nem";

	private const string PictureMapFile = "mappings/misc/End of game sequence frame {0}.eni";

	private const string PhotosPaletteFile = "art/palettes/Ending Photos.bin";

	private const string BackgroundPaletteFile = "art/palettes/Ending Background.bin";

	private const string SonicArtFile = "art/uncompressed/Sonic's art.bin";

	private const string SonicMappingsFile = "mappings/sprite/Sonic.asm";

	private const string SonicDplcFile = "mappings/spriteDPLC/Sonic.asm";

	private const string TailsArtFile = "art/uncompressed/Tails's art.bin";

	private const string TailsMappingsFile = "mappings/sprite/Tails.asm";

	private const string TailsDplcFile = "mappings/spriteDPLC/Tails.asm";

	private const string TornadoMappingsFile = "mappings/sprite/objB2_a.asm";

	/// <summary>
	/// The first tile of the pilots in the mappings of the Tornado: the VRAM of Sonic and of Tails
	/// (ArtTile_ArtUnc_Sonic - ArtTile_ArtNem_Tornado). Frames 0 to 3 have Tails, frames 4 to 7 have Sonic.
	/// </summary>
	private const int SonicPilotTile = 0x280;

	private const int TailsPilotTile = 0x2A0;

	/// <summary>The frames of Tails that ObjB2_Animate_Pilot loads for the pilot (Tails_pilot_frames, without repeats).</summary>
	public static readonly int[] PilotFrames = { 0x10, 0x01, 0x02, 0x03, 0x04 };

	/// <summary>The frames of Sonic for the pilot when Tails plays alone (Sonic_pilot_frames).</summary>
	public static readonly int[] SonicPilotFrames = { 0x2D, 0x2E, 0x2F, 0x30 };

	/// <summary>The VRAM tiles of the art of the ending that does not change with the character (s2.constants.asm).</summary>
	private static readonly (string File, int Tile)[] VramArt =
	{
		("art/nemesis/Final image of Tornado with it and Sonic facing screen.nem", 0x156), // ArtTile_ArtNem_EndingFinalTornado
		("art/nemesis/Movie sequence at end of game.nem", 0x328), // ArtTile_ArtNem_EndingPics
		("art/nemesis/Small pictures of Tornado in final ending sequence.nem", 0x493), // ArtTile_ArtNem_EndingMiniTornado
		("art/nemesis/The Tornado.nem", 0x500), // ArtTile_ArtNem_Tornado
		("art/nemesis/Clouds.nem", 0x54F), // ArtTile_ArtNem_Clouds
	};

	/// <summary>ArtTile_EndingCharacter and ArtTile_ArtNem_Animal_2: the art that each character of the ending loads.</summary>
	private const int CharacterTile = 0x19;

	private const int AnimalTile = 0x594;

	/// <summary>The art and the palette of each character of the ending (EndingSequence_LoadCharacterArt and _LoadFlickyArt).</summary>
	private sealed record Variant(string Folder, string CharacterArtFile, string AnimalArtFile, string CloseUpFile,
		Func<string, uint[]> ReadPalette, Func<string, uint[], ImportedSprite> ImportCharacter, string CharacterKey);

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
		uint[] sonicPalette = ReadSonicPalette(disassembly);
		WriteSprite(output, "CLOUDS", SpriteImport.Import(new OriginalSprite(ResourceKey + "/CLOUDS", "art/nemesis/Clouds.nem", "mappings/sprite/objB3.asm", 3), disassembly, sonicPalette, Scale));
		WriteSprite(output, "TORNADO", ImportTornado(disassembly, sonicPalette));
		WriteSprite(Path.Combine(output, "TORNADO"), "PILOT", ImportPilot(disassembly, sonicPalette, ResourceKey + "/TORNADO/PILOT", TailsArtFile, TailsDplcFile, TailsPilotTile, PilotFrames));

		Variant[] variants =
		{
			new Variant("", "art/nemesis/Small pictures of Sonic and final image of Sonic.nem", "art/nemesis/Flicky.nem",
				"mappings/misc/Closeup of Tails flying plane in ending sequence.eni", ReadSonicPalette, ImportSonic, ResourceKey + "/SONIC"),
			new Variant("SUPERSONIC", "art/nemesis/Small pictures of Sonic and final image of Sonic in Super Sonic mode.nem", "art/nemesis/Eagle.nem",
				"mappings/misc/Closeup of Tails flying plane in ending sequence.eni", ReadSuperSonicPalette, ImportSuperSonic, ResourceKey + "/SUPERSONIC/CHARACTER"),
			new Variant("TAILS", "art/nemesis/Final image of Tails.nem", "art/nemesis/Chicken.nem",
				"mappings/misc/Closeup of Sonic flying plane in ending sequence.eni", ReadTailsPalette, ImportTails, ResourceKey + "/TAILS/CHARACTER"),
		};
		List<List<SpritePiece>> planeFrames = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, "mappings/sprite/objCF.asm")));
		foreach (Variant variant in variants)
		{
			string variantFolder = variant.Folder.Length == 0 ? output : Path.Combine(output, variant.Folder);
			string variantKey = variant.Folder.Length == 0 ? ResourceKey : ResourceKey + "/" + variant.Folder;
			Directory.CreateDirectory(variantFolder);
			uint[] palette = variant.ReadPalette(disassembly);
			ImportedSprite character = variant.ImportCharacter(disassembly, palette);
			string characterFolder = Path.Combine(new[] { folder }.Concat(variant.CharacterKey.Split('/')).ToArray());
			WriteSprite(Path.GetDirectoryName(characterFolder), Path.GetFileName(characterFolder), character);
			WriteSprite(variantFolder, "BIRDS", SpriteImport.Import(new OriginalSprite(variantKey + "/BIRDS", variant.AnimalArtFile, "mappings/sprite/obj28_a.asm", 0), disassembly, palette, Scale));
			byte[] vram = ReadVram(disassembly, variant.CharacterArtFile, variant.AnimalArtFile);
			WriteSprite(variantFolder, "PLANE", SpriteImport.Import(variantKey + "/PLANE", vram, planeFrames, new[] { 0, 1 }, Array.Empty<int>(), palette, Scale));
			File.WriteAllBytes(Path.Combine(variantFolder, "CLOSEUP.png"), RenderCloseUp(disassembly, vram, palette, variant.CloseUpFile));
		}
		// When Tails plays alone, Sonic flies the Tornado
		WriteSprite(Path.Combine(output, "TAILS"), "PILOT", ImportPilot(disassembly, ReadTailsPalette(disassembly), ResourceKey + "/TAILS/PILOT", SonicArtFile, SonicDplcFile, SonicPilotTile, SonicPilotFrames));
	}

	/// <summary>
	/// The palette of the Sonic ending after ObjC9 loads it. Subtype 8 copies $20 colours from Pal_AC7E, which has
	/// only 16, so line 1 gets the first colours of the next palette in the ROM, Pal_AC9E (Tails). Subtype $A loads
	/// the background palette into lines 2 and 3.
	/// </summary>
	public static uint[] ReadSonicPalette(string disassembly)
	{
		uint[] palette = ReadBackgroundPalette(disassembly);
		SpriteImport.ReadPaletteFile(disassembly, "art/palettes/Ending Sonic.bin", palette, 0);
		uint[] tails = SpriteImport.ReadColours(File.ReadAllBytes(Path.Combine(disassembly, "art/palettes/Ending Tails.bin")));
		Array.Copy(tails, 0, palette, 16, 16);
		return palette;
	}

	/// <summary>The palette of the Super Sonic ending: subtype $C loads Pal_AD3E into line 0. Line 1 is the line of the Sonic ending.</summary>
	public static uint[] ReadSuperSonicPalette(string disassembly)
	{
		uint[] palette = ReadSonicPalette(disassembly);
		SpriteImport.ReadPaletteFile(disassembly, "art/palettes/Ending Super Sonic.bin", palette, 0);
		return palette;
	}

	/// <summary>The palette of the Tails ending: subtype $E loads Pal_AC9E into lines 0 and 1.</summary>
	public static uint[] ReadTailsPalette(string disassembly)
	{
		uint[] palette = ReadBackgroundPalette(disassembly);
		SpriteImport.ReadPaletteFile(disassembly, "art/palettes/Ending Tails.bin", palette, 0);
		return palette;
	}

	private static uint[] ReadBackgroundPalette(string disassembly)
	{
		uint[] palette = new uint[64];
		SpriteImport.ReadPaletteFile(disassembly, BackgroundPaletteFile, palette, 2);
		return palette;
	}

	/// <summary>The art of the ending at its places in VRAM, so that the mappings with VRAM tiles can use it.</summary>
	private static byte[] ReadVram(string disassembly, string characterArtFile, string animalArtFile)
	{
		byte[] vram = new byte[0x800 * BytesPerTile];
		foreach ((string file, int tile) in VramArt.Append((characterArtFile, CharacterTile)).Append((animalArtFile, AnimalTile)))
		{
			byte[] art = SpriteImport.ReadArt(disassembly, file);
			Array.Copy(art, 0, vram, tile * BytesPerTile, Math.Min(art.Length, vram.Length - tile * BytesPerTile));
		}
		return vram;
	}

	/// <summary>
	/// The Tornado of ObjB2 without the pilots. A pilot is the piece with the VRAM tiles of Sonic or Tails, which change
	/// on each frame of the pilot, so a pilot is a group of its own.
	/// </summary>
	private static ImportedSprite ImportTornado(string disassembly, uint[] palette)
	{
		byte[] art = SpriteImport.ReadArt(disassembly, "art/nemesis/The Tornado.nem");
		List<List<SpritePiece>> frames = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, TornadoMappingsFile)))
			.Select(frame => frame.Where(piece => piece.Tile < SonicPilotTile).ToList()).ToList();
		return SpriteImport.Import(ResourceKey + "/TORNADO", art, frames, new[] { 0 }, Array.Empty<int>(), palette, Scale);
	}

	/// <summary>A pilot: the piece of the Tornado with the tiles of the character, with each of his pilot frames.</summary>
	private static ImportedSprite ImportPilot(string disassembly, uint[] palette, string key, string artFile, string dplcFile, int pilotTile, int[] pilotFrames)
	{
		SpritePiece pilot = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, TornadoMappingsFile)))
			.SelectMany(frame => frame).First(piece => piece.Tile == pilotTile);
		byte[] characterArt = File.ReadAllBytes(Path.Combine(disassembly, artFile));
		List<List<(int Tiles, int Offset)>> dplcs = SuperSonicArt.ParseDplc(File.ReadAllText(Path.Combine(disassembly, dplcFile)));
		var art = new List<byte>();
		var frames = new List<List<SpritePiece>>();
		foreach (int id in pilotFrames)
		{
			int firstTile = art.Count / BytesPerTile;
			foreach ((int tiles, int offset) in dplcs[id])
			{
				art.AddRange(characterArt.AsSpan(offset * BytesPerTile, tiles * BytesPerTile).ToArray());
			}
			// The DPLC loads the tiles to the start of the VRAM of the character, so the piece shows the first tiles of the frame
			frames.Add(new List<SpritePiece> { pilot with { Tile = firstTile } });
			art.AddRange(new byte[pilot.Width * pilot.Height * BytesPerTile]);
		}
		return SpriteImport.Import(key, art.ToArray(), frames, new[] { 0 }, Array.Empty<int>(), palette, Scale);
	}

	/// <summary>The close-up of the plane: an Enigma map over the art of the final Tornado (make_art_tile(ArtTile_ArtNem_EndingFinalTornado,0,1)).</summary>
	public static byte[] RenderCloseUp(string disassembly, byte[] vram, uint[] palette, string mapFile)
	{
		ushort[] map = Enigma.Decompress(File.ReadAllBytes(Path.Combine(disassembly, mapFile)), 0x8156);
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

	private static ImportedSprite ImportSonic(string disassembly, uint[] palette)
	{
		OriginalScript[] scripts =
		{
			new OriginalScript("Fall (SonAni_Float2)", 7, new[] { 0x54, 0x55, 0x56, 0x57, 0x58 }),
			// loc_A53A sets mapping frame 1 with a long duration: the first frame of SonAni_Wait
			new OriginalScript("Wait (SonAni_Wait)", 5, new[] { 0x01 }),
		};
		return ImportCharacter(disassembly, ResourceKey + "/SONIC", SonicArtFile, SonicMappingsFile, SonicDplcFile, scripts, palette);
	}

	/// <summary>
	/// Super Sonic runs in the air (SonAni_Walk with an inertia of $1000: SupSonAni_Run, a new frame on each frame).
	/// SAnim_SuperWalk adds $20 only to the frames below $B5, so the run has no other frames.
	/// </summary>
	private static ImportedSprite ImportSuperSonic(string disassembly, uint[] palette)
	{
		OriginalScript[] scripts =
		{
			new OriginalScript("Run (SupSonAni_Run)", 0, new[] { 0xB5, 0xB9 }),
			new OriginalScript("Wait (SupSonAni_Stand)", 7, new[] { 0x72 }),
		};
		return ImportCharacter(disassembly, ResourceKey + "/SUPERSONIC/CHARACTER", SonicArtFile, SonicMappingsFile, SonicDplcFile, scripts, palette);
	}

	private static ImportedSprite ImportTails(string disassembly, uint[] palette)
	{
		OriginalScript[] scripts =
		{
			new OriginalScript("Fall (TailsAni_Float2)", 9, new[] { 0x6E, 0x6F, 0x70, 0x71, 0x72 }),
			// loc_A582 sets mapping frame 1 with a long duration: the first frame of TailsAni_Wait
			new OriginalScript("Wait (TailsAni_Wait)", 7, new[] { 0x01 }),
			// Obj05 shows Obj05Ani_Swish while Tails waits, and nothing while he falls
			new OriginalScript("Tails (Obj05Ani_Swish)", 7, new[] { 0x09, 0x0A, 0x0B, 0x0C, 0x0D }),
		};
		return ImportCharacter(disassembly, ResourceKey + "/TAILS/CHARACTER", TailsArtFile, TailsMappingsFile, TailsDplcFile, scripts, palette);
	}

	private static ImportedSprite ImportCharacter(string disassembly, string key, string artFile, string mappingsFile, string dplcFile, OriginalScript[] scripts, uint[] palette)
	{
		byte[] art = File.ReadAllBytes(Path.Combine(disassembly, artFile));
		List<List<SpritePiece>> mappings = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, mappingsFile)));
		List<List<(int Tiles, int Offset)>> dplcs = SuperSonicArt.ParseDplc(File.ReadAllText(Path.Combine(disassembly, dplcFile)));
		return DplcSpriteImport.Import(key, art, mappings, dplcs, scripts, palette, 0, new[] { "//TEXTURE" });
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
