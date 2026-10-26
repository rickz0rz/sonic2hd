using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonicOrca.Original;

/// <summary>
/// The art of the special stage of the original game that the game data does not have: the winged hand of the
/// checkpoint messages, the START banner, the results screen and its digits. It comes from the Sonic 2 disassembly,
/// so it stays local. Each sprite is a texture at four times the size and an animation group with one animation for
/// each mapping frame (see <see cref="SpriteImport"/>).
/// </summary>
public static class SpecialStageArt
{
	/// <summary>The checkpoint messages (Obj5A): frame $14 is the wings and frame $15 the hand. Palette lines 1 and 2.</summary>
	public const string MessagesKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/MESSAGES";

	/// <summary>The START banner (Obj5F): frame 0 is the banner with the flags, frames 2 to 8 its pieces.</summary>
	public const string StartKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/START";

	/// <summary>The results screen (Obj6F): the titles, the emeralds and the labels.</summary>
	public const string ResultsKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/RESULTS";

	/// <summary>The digits 0 to 9 of the counters of the results screen, in the colours of the results screen.</summary>
	public const string DigitsKey = "SONICORCA/SPECIALSTAGE/ORIGINAL/DIGITS";

	private const int Scale = 4;

	private const int BytesPerTile = 32;

	/// <summary>
	/// The video memory tiles of the art of the results screen (s2.constants.asm). The special stage results load the
	/// title card art (LoadTitleCardSS), the results art and the HUD art of the standard pattern load list.
	/// </summary>
	private const int TitleCardTile = 0x580;
	private const int ResultsArtTile = 0x590;
	private const int HudTile = 0x6CA;

	/// <summary>
	/// The letters that the results screen copies from the font of large broken letters to tile 2 (SpecialStage_ResultsLetters
	/// and LoadTitleCardSS): the first tile of each letter in the font, and its width in tiles. E, N, O and Z are in the
	/// title card art.
	/// </summary>
	private static readonly (int Tile, int Size)[] ResultsLetters =
	{
		(0x00, 4), (0x08, 4), (0x0C, 4), (0x14, 4), (0x18, 4), (0x1C, 2), (0x26, 4), (0x2A, 6), (0x30, 4), (0x38, 4),
		(0x3C, 4), (0x40, 4), (0x44, 4), (0x4C, 6), (0x5A, 2)
	};

	/// <summary>Makes all the sprites.</summary>
	public static List<ImportedSprite> Import(string disassembly)
	{
		uint[] stagePalette = new uint[64];
		SpriteImport.ReadPaletteFile(disassembly, "art/palettes/Special Stage Main.bin", stagePalette, 0);
		SpriteImport.ReadPaletteFile(disassembly, "art/palettes/Special Stage 1.bin", stagePalette, 3);
		uint[] resultsPalette = new uint[64];
		SpriteImport.ReadPaletteFile(disassembly, "art/palettes/Special Stage Results Screen.bin", resultsPalette, 0);
		return new List<ImportedSprite>
		{
			SpriteImport.Import(new OriginalSprite(MessagesKey, "art/nemesis/Special stage messages and icons.nem", "mappings/sprite/obj5A.asm", new[] { 1, 2 }),
				disassembly, stagePalette, Scale),
			SpriteImport.Import(new OriginalSprite(StartKey, "art/nemesis/Start text from special stage.nem", "mappings/sprite/obj5F_a.asm", 0),
				disassembly, stagePalette, Scale),
			SpriteImport.Import(ResultsKey, GetResultsArt(disassembly),
				SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, "mappings/sprite/obj6F.asm"))), new[] { 0 },
				Array.Empty<int>(), resultsPalette, Scale),
			ImportDigits(disassembly, resultsPalette)
		};
	}

	/// <summary>
	/// The video memory of the results screen: the art at the tiles where the original loads it. The counters at tile
	/// $520 are digits that the HUD code writes while the screen runs, so the game draws them with <see cref="DigitsKey"/>.
	/// </summary>
	private static byte[] GetResultsArt(string disassembly)
	{
		byte[] art = new byte[(HudTile + 0x40) * BytesPerTile];
		void Load(byte[] data, int tile) => data.AsSpan(0, Math.Min(data.Length, art.Length - tile * BytesPerTile)).CopyTo(art.AsSpan(tile * BytesPerTile));
		byte[] font = SpriteImport.ReadArt(disassembly, "art/nemesis/Font using large broken letters.nem");
		int letterTile = 2;
		foreach ((int tile, int size) in ResultsLetters)
		{
			Load(font.AsSpan(tile * BytesPerTile, size * BytesPerTile).ToArray(), letterTile);
			letterTile += size;
		}
		Load(SpriteImport.ReadArt(disassembly, "art/nemesis/Title card.nem"), TitleCardTile);
		Load(SpriteImport.ReadArt(disassembly, "art/nemesis/Special stage results screen art and some emeralds.nem"), ResultsArtTile);
		// The HUD art of the standard pattern load list: the words SCORE and RINGS
		Load(SpriteImport.ReadArt(disassembly, "art/nemesis/HUD.nem"), HudTile);
		return art;
	}

	/// <summary>The big digits of the counters (Art_Hud): each digit is one tile wide and two tiles high.</summary>
	private static ImportedSprite ImportDigits(string disassembly, uint[] palette)
	{
		byte[] art = SpriteImport.ReadArt(disassembly, "art/uncompressed/Big and small numbers used on counters - 1.bin");
		List<List<SpritePiece>> frames = Enumerable.Range(0, 10)
			.Select(digit => new List<SpritePiece> { new SpritePiece(0, 0, 1, 2, digit * 2, false, false, 0) })
			.ToList();
		return SpriteImport.Import(DigitsKey, art, frames, new[] { 0 }, Array.Empty<int>(), palette, Scale);
	}
}
