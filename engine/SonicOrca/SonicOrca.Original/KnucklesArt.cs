using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonicOrca.Original;

/// <summary>
/// Makes the sprites of Knuckles from the disassembly of "Knuckles in Sonic 2" (the knuckles-in-sonic-2 branch of
/// s2disasm, KiS2). The HD data has no Knuckles art. The animation group has one animation for each value of
/// CharacterAnimation, in the same order. Then come the animations that only Knuckles has: the transformation, the
/// glide, the fall and the slide after a glide, the hard landing, the climb on a wall, and the climb onto a ledge.
/// Each animation follows the script of KnucklesAni_* in s2.asm of KiS2. The frames use palette line 0 of
/// SonicAndTails.bin of KiS2, which has the colours of Knuckles.
/// </summary>
public static class KnucklesArt
{
	/// <summary>The object folder of Knuckles. The animation group is ANIGROUP and its texture is TEXTURE.</summary>
	public const string ObjectKey = "SONICORCA/OBJECTS/KNUCKLES";

	/// <summary>The index of the transformation into Super Knuckles, after the 30 values of CharacterAnimation.</summary>
	public const int TransformAnimation = 30;

	/// <summary>
	/// The glide (KnucklesAni_Gliding). Its frames are the frames of the turn of Knuckles_DoGlidingAnimation: frame 0
	/// faces right, and frame 4 faces the camera. The game picks the frame from the direction of the glide.
	/// </summary>
	public const int GlideAnimation = 31;

	/// <summary>The fall after the player lets go of the glide (KnucklesAni_FallFromGlide).</summary>
	public const int FallFromGlideAnimation = 32;

	/// <summary>The landing after a fall from a glide (KnucklesAni_HardFall).</summary>
	public const int HardFallAnimation = 33;

	/// <summary>The slide on the ground at the end of a glide (frame $CC of Knuckles_BeginSlide).</summary>
	public const int SlideAnimation = 34;

	/// <summary>
	/// The climb on a wall: the frames of Knuckles_Climbing_Wall, $B7 to $BC. The game picks the next frame each 4
	/// frames while Knuckles moves up or down.
	/// </summary>
	public const int ClimbAnimation = 35;

	/// <summary>The climb onto a ledge: the frames of Knuckles_DoLedgeClimbingAnimation, 6 frames each.</summary>
	public const int ClimbLedgeAnimation = 36;

	/// <summary>Knuckles gets up after a slide (KnucklesAni_GetUp, which the original calls KnuxAni_ClimbLedge).</summary>
	public const int GetUpAnimation = 37;

	/// <summary>The files of KiS2 that the import needs. The folder of the import must have each of them.</summary>
	public static readonly IReadOnlyList<string> RequiredFiles = new[]
	{
		ArtFile, MappingsFile, DplcFile, PaletteFile, SuperPaletteFile, SpecialStageArtFile, SpecialStageMappingsFile, SpecialStageDplcFile
	};

	private const string ArtFile = "art/uncompressed/Knuckles' art.bin";

	private const string MappingsFile = "mappings/sprite/Knuckles.asm";

	private const string DplcFile = "mappings/spriteDPLC/Knuckles.asm";

	private const string PaletteFile = "art/palettes/SonicAndTails.bin";

	private const string SuperPaletteFile = "art/palettes/Super Knuckles transformation.bin";

	/// <summary>
	/// The colours of the palette cycle of Super Knuckles (PalCycle_SuperSonic in KiS2): one row of 3 colours for each
	/// frame of CyclingPal_SKTransformation, after row 0, which has the normal colours of Knuckles. The cycle writes
	/// the entries 2, 3 and 5 of palette line 0.
	/// </summary>
	public const string PaletteKey = "SONICORCA/OBJECTS/KNUCKLES/PALETTE";

	/// <summary>
	/// The stars of Super Knuckles (Obj7E). KiS2 keeps the art of the stars of Super Sonic, with palette line 0, which
	/// has the colours of Knuckles.
	/// </summary>
	public const string StarsObjectKey = "SONICORCA/OBJECTS/KNUCKLES/STARS";

	/// <summary>The life icon of the HUD. The HD life icons of Sonic and Tails are SONICORCA/HUD/LIFE/SONIC and TAILS.</summary>
	public const string LifeIconKey = "SONICORCA/HUD/LIFE/KNUCKLES";

	private const string LifeIconFile = "art/nemesis/Knuckles lives counter.nem";

	/// <summary>The HD life icons are 68 pixels wide and high. The icon of the original is 16 pixels, 64 at four times the size.</summary>
	private const int LifeIconSize = 68;

	/// <summary>
	/// The sprites of Knuckles in the special stage, from behind: animation 0 runs (Obj09 animation 0, the upright
	/// frames 0 to 3), and animation 1 is the ball (Obj09 animation 3, the frames $10 and $11). The HD special stage
	/// turns the sprite with the pipe, so it needs only the upright frames, as the HD group of Sonic has.
	/// </summary>
	public const string SpecialStageObjectKey = "SONICORCA/SPECIALSTAGE/KNUCKLESANIM";

	private const string SpecialStageArtFile = "art/nemesis/Knuckles animation frames in special stage.nem";

	private const string SpecialStageMappingsFile = "mappings/sprite/obj09.asm";

	private const string SpecialStageDplcFile = "mappings/spriteDPLC/obj09.asm";

	private const string SpecialStagePaletteFile = "art/palettes/Special Stage Main.bin";

	/// <summary>The entries of palette line 0 that the palette cycle of Super Knuckles changes.</summary>
	private static readonly int[] CycledEntries = { 2, 3, 5 };

	/// <summary>The delay of the frames of a script whose speed depends on the speed of the character ($FF, $FE, $FD).</summary>
	private const int SpeedDelay = 4;

	/// <summary>The first frame of the tumble of Knuckles. Sonic starts at $5F (SAnim_Tumble).</summary>
	private const int FirstTumbleFrame = 0x31;

	/// <summary>The idle animation of KnucklesAni_Wait: he stands, taps his foot, and then looks around.</summary>
	private static readonly int[] WaitFrames = Enumerable.Repeat(0x56, 48)
		.Concat(Enumerable.Range(0, 8).SelectMany(_ => new[] { 0xD2, 0xD2, 0xD2, 0xD3, 0xD3, 0xD3 }))
		.Concat(new[] { 0xD4, 0xD4, 0xD4, 0xD4, 0xD4, 0xD7 })
		.Concat(Enumerable.Range(0, 7).SelectMany(_ => new[] { 0xD8, 0xD9, 0xDA, 0xDB }))
		.Concat(new[] { 0xDC, 0xDD, 0xDC, 0xDD, 0xDE, 0xDE, 0xD8, 0xD7 })
		.ToArray();

	private static readonly int[] BalanceFrames =
	{
		0x9F, 0x9F, 0xA0, 0xA0, 0xA1, 0xA1, 0xA2, 0xA2, 0xA3, 0xA3, 0xA4, 0xA4, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5,
		0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA6, 0xA6, 0xA6, 0xA7, 0xA7, 0xA7, 0xA8, 0xA8, 0xA9, 0xA9, 0xAA, 0xAA
	};

	private static readonly int[] Balance2Frames =
	{
		0xA1, 0xA1, 0xA2, 0xA2, 0xA3, 0xA3, 0xA4, 0xA4, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5, 0xA5,
		0xA5, 0xA5, 0xA5, 0xA5, 0xA6, 0xA6, 0xA6, 0xA7, 0xA7, 0xA7, 0xA8, 0xA8, 0xA9, 0xA9, 0xAA, 0xAA
	};

	private static readonly int[] WalkFrames = { 0x07, 0x08, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

	private static readonly int[] RunFrames = { 0x21, 0x22, 0x23, 0x24 };

	private static readonly int[] Float2Frames = { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9 };

	/// <summary>
	/// The animation scripts, in the order of CharacterAnimation, then the animations of Knuckles. The walk and run
	/// scripts have only the upright frames, because the game turns the sprite with the ground. The tumble has the 12
	/// frames of the flip, and the game picks one from the tumble angle. "$FE n" goes back n frames: the loop starts
	/// n frames before the end.
	/// </summary>
	private static readonly OriginalScript[] Scripts =
	{
		new OriginalScript("Wait", 5, WaitFrames),                                            // KnucklesAni_Wait
		new OriginalScript("Lookup", 5, new[] { 0xD5, 0xD6 }, HoldLast: true),               // KnucklesAni_LookUp
		new OriginalScript("Duck", 5, new[] { 0x9B, 0x9C }, HoldLast: true),                 // KnucklesAni_Duck
		new OriginalScript("BalanceClose", 3, BalanceFrames, Loop: BalanceFrames.Length - 6), // KnucklesAni_Balance
		new OriginalScript("BalanceFar", 3, Balance2Frames, Loop: Balance2Frames.Length - 6), // KnucklesAni_Balance2
		new OriginalScript("BalanceFarReverse", 3, Balance2Frames, Loop: Balance2Frames.Length - 6),
		new OriginalScript("Push", SpeedDelay, new[] { 0xCE, 0xCF, 0xD0, 0xD1 }),           // KnucklesAni_Push
		new OriginalScript("Walk", SpeedDelay, WalkFrames),                                   // KnucklesAni_Walk
		new OriginalScript("Run", SpeedDelay, RunFrames),                                     // KnucklesAni_Run
		new OriginalScript("Stop", 3, new[] { 0x9D, 0x9E, 0x9F, 0xA0 }, HoldLast: true),     // KnucklesAni_Stop
		new OriginalScript("Roll", SpeedDelay, new[] { 0x9A, 0x96, 0x9A, 0x97, 0x9A, 0x98, 0x9A, 0x99 }), // KnucklesAni_Roll
		new OriginalScript("Spindash", 0, new[] { 0x86, 0x87, 0x86, 0x88, 0x86, 0x89, 0x86, 0x8A, 0x86, 0x8B }), // KnucklesAni_Spindash
		new OriginalScript("Spring", 0x2F, new[] { 0x8E }, HoldLast: true),                  // KnucklesAni_Spring
		new OriginalScript("Postspring", SpeedDelay, WalkFrames),
		new OriginalScript("Hurt", 0x40, new[] { 0x8D }),                                     // KnucklesAni_Hurt
		new OriginalScript("Death", 0x20, new[] { 0xAB }),                                    // KnucklesAni_Death
		new OriginalScript("Tumble", 0, Enumerable.Range(FirstTumbleFrame, 12).ToArray()),   // SAnim_Tumble
		new OriginalScript("Flying", 5, Float2Frames),                                        // KnucklesAni_Float2
		new OriginalScript("Inhale", 0x0B, new[] { 0xB0, 0xB0, 0x03, 0x04 }, HoldLast: true), // KnucklesAni_Bubble
		new OriginalScript("Win", 5, new[] { 0x56 }),
		new OriginalScript("Drown", 0x20, new[] { 0xAD }),                                    // KnucklesAni_Drown
		new OriginalScript("Skydiving", 5, Float2Frames),                                     // KnucklesAni_Float2
		new OriginalScript("Sliding", 9, new[] { 0x8C }),                                     // KnucklesAni_OilSlide
		new OriginalScript("Sucked", 5, Float2Frames),
		new OriginalScript("Grabbed", 5, Float2Frames),
		new OriginalScript("Hanging", 1, new[] { 0xAE, 0xAF }),                               // KnucklesAni_Hang
		new OriginalScript("WinToStand", 5, new[] { 0x56 }),
		new OriginalScript("WaitToStand", 5, new[] { 0x56 }),
		new OriginalScript("Mach1", SpeedDelay, RunFrames),
		new OriginalScript("Mach1ToRun", SpeedDelay, RunFrames),
		new OriginalScript("Transform", 2, new[] { 0xEB, 0xEB, 0xEC, 0xED, 0xEC, 0xED, 0xEC, 0xED, 0xEC, 0xED, 0xEC, 0xED }, HoldLast: true), // KnucklesAni_Transform
		new OriginalScript("Glide", 0x1F, new[] { 0xC0, 0xC1, 0xC2, 0xC3, 0xC4 }),           // Knuckles_DoGlidingAnimation
		new OriginalScript("FallFromGlide", 7, new[] { 0xCA, 0xCB }, HoldLast: true),        // KnucklesAni_FallFromGlide
		new OriginalScript("HardFall", 0x0F, new[] { 0x9C }, HoldLast: true),                // KnucklesAni_HardFall
		new OriginalScript("Slide", 0x1F, new[] { 0xCC }),                                    // Knuckles_BeginSlide
		new OriginalScript("Climb", 3, new[] { 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC }),        // Knuckles_Climbing_Wall
		new OriginalScript("ClimbLedge", 6, new[] { 0xBD, 0xBE, 0xBF }, HoldLast: true),     // Knuckles_DoLedgeClimbingAnimation
		new OriginalScript("GetUp", 0x0F, new[] { 0xCD }, HoldLast: true),                   // KnucklesAni_GetUp
	};

	/// <summary>
	/// Makes the sprites of Knuckles in the special stage, at four times the size. KiS2 loads them with standard DPLCs.
	/// Obj09 has palette line 1 in its art tile, and KiS2 changed that line of the special stage palette to the
	/// colours of Knuckles.
	/// </summary>
	public static ImportedSprite ImportSpecialStage(string kis2)
	{
		byte[] art = SpriteImport.ReadArt(kis2, SpecialStageArtFile);
		List<List<SpritePiece>> mappings = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(kis2, SpecialStageMappingsFile)));
		List<List<(int Tiles, int Offset)>> dplcs = SuperSonicArt.ParseDplc(File.ReadAllText(Path.Combine(kis2, SpecialStageDplcFile)));
		uint[] palette = new uint[64];
		SpriteImport.ReadPaletteFile(kis2, SpecialStagePaletteFile, palette, 0);
		OriginalScript[] scripts =
		{
			new OriginalScript("Run", 1, new[] { 0, 1, 2, 3 }),
			new OriginalScript("Ball", 1, new[] { 0x10, 0x11 }),
		};
		return DplcSpriteImport.Import(SpecialStageObjectKey, art, mappings, dplcs, scripts, palette, 1, new[] { "//TEXTURE" });
	}

	/// <summary>
	/// Makes the life icon of the HUD: the head of Knuckles, the first 2 x 2 tiles of the lives counter of KiS2 (tiles in
	/// columns), with palette line 0, at four times the size, in the middle of an image of the size of the HD icons.
	/// </summary>
	public static byte[] ImportLifeIcon(string kis2)
	{
		byte[] art = SpriteImport.ReadArt(kis2, LifeIconFile);
		uint[] palette = SpriteImport.ReadPalette(kis2, PaletteFile);
		const int scale = 4;
		const int tilePixels = 8;
		int margin = (LifeIconSize - 2 * tilePixels * scale) / 2;
		var pixels = new byte[LifeIconSize * LifeIconSize * 4];
		for (int y = 0; y < 2 * tilePixels; y++)
		{
			for (int x = 0; x < 2 * tilePixels; x++)
			{
				// A sprite piece of 2 x 2 tiles has its tiles in columns: 0 and 1 on the left, 2 and 3 on the right
				int tile = (x / tilePixels) * 2 + y / tilePixels;
				int offset = tile * 32 + (y % tilePixels) * 4 + (x % tilePixels) / 2;
				int index = (x % 2 == 0) ? art[offset] >> 4 : art[offset] & 0xF;
				if (index == 0)
				{
					continue;
				}
				uint colour = palette[index];
				for (int dy = 0; dy < scale; dy++)
				{
					for (int dx = 0; dx < scale; dx++)
					{
						int at = ((margin + y * scale + dy) * LifeIconSize + margin + x * scale + dx) * 4;
						pixels[at] = (byte)(colour >> 24);
						pixels[at + 1] = (byte)(colour >> 16);
						pixels[at + 2] = (byte)(colour >> 8);
						pixels[at + 3] = 0xFF;
					}
				}
			}
		}
		return SpriteImport.EncodePng(pixels, LifeIconSize, LifeIconSize);
	}

	/// <summary>True when the folder has the files of KiS2 that the import needs.</summary>
	public static bool IsKis2Folder(string folder) => RequiredFiles.All(file => File.Exists(Path.Combine(folder, file)));

	/// <summary>Makes the texture and the animation group of Knuckles, at four times the size of the original.</summary>
	/// <param name="kis2">The folder of the knuckles-in-sonic-2 branch of s2disasm.</param>
	public static ImportedSprite Import(string kis2)
	{
		byte[] art = File.ReadAllBytes(Path.Combine(kis2, ArtFile));
		List<List<SpritePiece>> mappings = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(kis2, MappingsFile)));
		List<List<(int Tiles, int Offset)>> dplcs = SuperSonicArt.ParseDplc(File.ReadAllText(Path.Combine(kis2, DplcFile)));
		uint[] palette = SpriteImport.ReadPalette(kis2, PaletteFile);
		return DplcSpriteImport.Import(ObjectKey, art, mappings, dplcs, Scripts, palette, 0, new[] { "//TEXTURE", "//PALETTE" });
	}

	/// <summary>
	/// Makes the image of the colours of the palette cycle of Super Knuckles: 3 colours wide, with the normal colours
	/// in row 0 and one row for each frame of the cycle.
	/// </summary>
	public static byte[] ImportPalette(string kis2)
	{
		uint[] normal = SpriteImport.ReadPalette(kis2, PaletteFile);
		uint[] cycle = SpriteImport.ReadColours(File.ReadAllBytes(Path.Combine(kis2, SuperPaletteFile)));
		uint[] colours = CycledEntries.Select(entry => normal[entry]).Concat(cycle).ToArray();
		int width = CycledEntries.Length;
		int rows = colours.Length / width;
		var pixels = new byte[rows * width * 4];
		for (int i = 0; i < rows * width; i++)
		{
			pixels[i * 4] = (byte)(colours[i] >> 24);
			pixels[i * 4 + 1] = (byte)(colours[i] >> 16);
			pixels[i * 4 + 2] = (byte)(colours[i] >> 8);
			pixels[i * 4 + 3] = (byte)colours[i];
		}
		return SpriteImport.EncodePng(pixels, width, rows);
	}
}
