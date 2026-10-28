using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SonicOrca.Original;

/// <summary>
/// Makes the sprites of Super Sonic from the art of Sonic in the disassembly. The HD data has no Super Sonic art. The
/// animation group has one animation for each value of CharacterAnimation, in the same order, then the
/// transformation, and then the walk variant. Each animation follows the script of SuperSonicAniData in s2.asm. Where Super Sonic has no script
/// of his own, the original uses the script of Sonic, so the group does the same. The frames use the normal palette
/// of Sonic. The game changes the 4 blue colours to the colours of PalCycle_SuperSonic, which are in the second
/// texture of the group, PALETTE. The stars (Obj7E) are a second group.
/// </summary>
public static class SuperSonicArt
{
	/// <summary>The animation group of the sprites. Its texture is TEXTURE in the same folder.</summary>
	public const string AnimationGroupKey = "SONICORCA/OBJECTS/SONIC/SUPER/ANIGROUP";

	/// <summary>The object folder of the sprites.</summary>
	public const string ObjectKey = "SONICORCA/OBJECTS/SONIC/SUPER";

	/// <summary>
	/// The colours of PalCycle_SuperSonic, one row of 4 colours for each frame of CyclingPal_SSTransformation. Row 0
	/// is the normal palette of Sonic, which the sprites use.
	/// </summary>
	public const string PaletteKey = "SONICORCA/OBJECTS/SONIC/SUPER/PALETTE";

	/// <summary>The object folder of the stars. Animation N of its group shows mapping frame N of Obj7E.</summary>
	public const string StarsObjectKey = "SONICORCA/OBJECTS/SONIC/SUPER/STARS";

	/// <summary>The index of the transformation in the animation group, after the 30 values of CharacterAnimation.</summary>
	public const int TransformAnimation = 30;

	/// <summary>
	/// The index of the walk variant in the animation group. SAnim_SuperWalk adds $20 to the mapping frame of the walk
	/// on each fourth frame. The variant has those frames, in the order of the walk.
	/// </summary>
	public const int WalkVariantAnimation = 31;

	/// <summary>SAnim_SuperWalk adds this number to a walk frame to get its variant.</summary>
	private const int WalkVariantFrameOffset = 0x20;

	private const string ArtFile = "art/uncompressed/Sonic's art.bin";

	private const string MappingsFile = "mappings/sprite/Sonic.asm";

	private const string DplcFile = "mappings/spriteDPLC/Sonic.asm";

	private const string PaletteFile = "art/palettes/SonicAndTails.bin";

	private const string SuperPaletteFile = "art/palettes/Super Sonic transformation.bin";

	private const string StarsArtFile = "art/nemesis/Super Sonic stars.nem";

	private const string StarsMappingsFile = "mappings/sprite/obj7E.asm";

	/// <summary>PalCycle_SuperSonic writes 4 colours from Normal_palette+4: colours 2 to 5 of line 0.</summary>
	public const int CycledColourCount = 4;

	/// <summary>The delay of the frames of a script whose speed depends on the speed of the character ($FF, $FE, $FD).</summary>
	private const int SpeedDelay = 4;

	private const int BytesPerTile = 32;

	/// <summary>
	/// The animation scripts, in the order of CharacterAnimation: the delay, the mapping frames, and the end. The
	/// walk and run scripts have only the upright frames, because the game turns the sprite with the ground. The
	/// tumble has the 12 frames of the flip, and the game picks one from the tumble angle.
	/// </summary>
	private static readonly (string Name, int Delay, int[] Frames, AnimationEnd End)[] Scripts =
	{
		("Wait", 7, new[] { 0x72, 0x73, 0x74, 0x73 }, AnimationEnd.Loop),                    // SupSonAni_Stand
		("Lookup", 5, new[] { 0x0B, 0x0C }, AnimationEnd.HoldLast),                          // SonAni_LookUp
		("Duck", 5, new[] { 0xC1 }, AnimationEnd.Loop),                                      // SupSonAni_Duck
		("BalanceClose", 9, new[] { 0xC2, 0xC3, 0xC4, 0xC3, 0xC5, 0xC6, 0xC7, 0xC6 }, AnimationEnd.Loop), // SupSonAni_Balance
		("BalanceFar", 9, new[] { 0xC2, 0xC3, 0xC4, 0xC3, 0xC5, 0xC6, 0xC7, 0xC6 }, AnimationEnd.Loop),
		("BalanceFarReverse", 9, new[] { 0xC2, 0xC3, 0xC4, 0xC3, 0xC5, 0xC6, 0xC7, 0xC6 }, AnimationEnd.Loop),
		("Push", SpeedDelay, new[] { 0xBD, 0xBE, 0xBF, 0xC0 }, AnimationEnd.Loop),           // SupSonAni_Push
		("Walk", SpeedDelay, new[] { 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x75, 0x76 }, AnimationEnd.Loop), // SupSonAni_Walk
		("Run", SpeedDelay, new[] { 0xB5, 0xB9 }, AnimationEnd.Loop),                        // SupSonAni_Run
		("Stop", 5, new[] { 0xD2, 0xD3, 0xD4, 0xD5 }, AnimationEnd.HoldLast),                // SonAni_Stop
		("Roll", SpeedDelay, new[] { 0x3D, 0x41, 0x3E, 0x41, 0x3F, 0x41, 0x40, 0x41 }, AnimationEnd.Loop), // SonAni_Roll
		("Spindash", 0, new[] { 0x42, 0x43, 0x42, 0x44, 0x42, 0x45, 0x42, 0x46, 0x42, 0x47 }, AnimationEnd.Loop), // SonAni_Spindash
		("Spring", 0x2F, new[] { 0x5B }, AnimationEnd.HoldLast),                             // SonAni_Spring
		("Postspring", SpeedDelay, new[] { 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x75, 0x76 }, AnimationEnd.Loop),
		("Hurt", 0x40, new[] { 0x4E }, AnimationEnd.Loop),                                   // SonAni_Hurt
		("Death", 0x20, new[] { 0x5C }, AnimationEnd.Loop),                                  // SonAni_Death
		("Tumble", 0, new[] { 0x5F, 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A }, AnimationEnd.Loop),
		("Flying", SpeedDelay, new[] { 0xB5, 0xB9 }, AnimationEnd.Loop),
		("Inhale", 0x0B, new[] { 0x5A }, AnimationEnd.HoldLast),                             // SonAni_Bubble
		("Win", 7, new[] { 0x72, 0x73, 0x74, 0x73 }, AnimationEnd.Loop),
		("Drown", 0x20, new[] { 0x5D }, AnimationEnd.Loop),                                  // SonAni_Drown
		("Skydiving", 7, new[] { 0x54, 0x55, 0x56, 0x57, 0x58 }, AnimationEnd.Loop),         // SonAni_Float2
		("Sliding", 9, new[] { 0x4E, 0x4F }, AnimationEnd.Loop),                             // SonAni_Slide
		("Sucked", 7, new[] { 0x54, 0x59 }, AnimationEnd.Loop),                              // SonAni_Float
		("Grabbed", 7, new[] { 0x54, 0x59 }, AnimationEnd.Loop),
		("Hanging", 1, new[] { 0x50, 0x51 }, AnimationEnd.Loop),                             // SonAni_Hang
		("WinToStand", 7, new[] { 0x72, 0x73, 0x74, 0x73 }, AnimationEnd.Loop),
		("WaitToStand", 7, new[] { 0x72, 0x73, 0x74, 0x73 }, AnimationEnd.Loop),
		("Mach1", SpeedDelay, new[] { 0xB5, 0xB9 }, AnimationEnd.Loop),
		("Mach1ToRun", SpeedDelay, new[] { 0xB5, 0xB9 }, AnimationEnd.Loop),
		("Transform", 2, new[] { 0x6D, 0x6D, 0x6E, 0x6E, 0x6F, 0x70, 0x71, 0x70, 0x71, 0x70, 0x71, 0x70, 0x71 }, AnimationEnd.HoldLast), // SupSonAni_Transform
		("WalkVariant", SpeedDelay, new[] { 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x75, 0x76 }.Select(id => id + WalkVariantFrameOffset).ToArray(), AnimationEnd.Loop),
	};

	private enum AnimationEnd
	{
		/// <summary>$FF: start again at the first frame.</summary>
		Loop,

		/// <summary>$FE 1 or $FD: stay on the last frame.</summary>
		HoldLast
	}

	/// <summary>Makes the texture and the animation group of Super Sonic, at four times the size of the original.</summary>
	public static ImportedSprite Import(string disassembly)
	{
		byte[] art = File.ReadAllBytes(Path.Combine(disassembly, ArtFile));
		List<List<SpritePiece>> mappings = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, MappingsFile)));
		List<List<(int Tiles, int Offset)>> dplcs = ParseDplc(File.ReadAllText(Path.Combine(disassembly, DplcFile)));
		uint[] palette = SpriteImport.ReadPalette(disassembly, PaletteFile);

		// Each frame gets its own tiles: the DPLC of the frame loads them to the start of the video memory of Sonic
		int[] frameIds = Scripts.SelectMany(script => script.Frames).Distinct().OrderBy(id => id).ToArray();
		var frameArt = new List<byte>();
		var frames = new List<List<SpritePiece>>();
		foreach (int id in frameIds)
		{
			int firstTile = frameArt.Count / BytesPerTile;
			foreach ((int tiles, int offset) in dplcs[id])
			{
				frameArt.AddRange(art.AsSpan(offset * BytesPerTile, tiles * BytesPerTile).ToArray());
			}
			frames.Add(mappings[id].Select(piece => piece with { Tile = piece.Tile + firstTile }).ToList());
		}
		ImportedSprite sprite = SpriteImport.Import(ObjectKey, frameArt.ToArray(), frames, new[] { 0 }, Array.Empty<int>(), palette, 4);

		// The group of SpriteImport has one animation for each frame. Put the frames of each script together.
		XElement[] frameElements = XDocument.Parse(sprite.AnimationGroup).Descendants("frame").ToArray();
		var anigroup = new StringBuilder();
		anigroup.AppendLine("<?xml version=\"1.0\"?>");
		anigroup.AppendLine("<anigroup>");
		anigroup.AppendLine("  <textures>");
		anigroup.AppendLine("    <texture>//TEXTURE</texture>");
		anigroup.AppendLine("    <texture>//PALETTE</texture>");
		anigroup.AppendLine("  </textures>");
		anigroup.AppendLine("  <animations>");
		foreach ((string name, int delay, int[] scriptFrames, AnimationEnd end) in Scripts)
		{
			string loop = end == AnimationEnd.HoldLast ? $" loop=\"{scriptFrames.Length - 1}\"" : "";
			anigroup.AppendLine($"    <!-- {name} -->");
			anigroup.AppendLine($"    <animation delay=\"{delay}\"{loop}>");
			foreach (int id in scriptFrames)
			{
				XElement frame = frameElements[Array.IndexOf(frameIds, id)];
				anigroup.AppendLine($"      <frame x=\"{frame.Attribute("x").Value}\" y=\"{frame.Attribute("y").Value}\" w=\"{frame.Attribute("w").Value}\" " +
					$"h=\"{frame.Attribute("h").Value}\" offset_x=\"{frame.Attribute("offset_x").Value}\" offset_y=\"{frame.Attribute("offset_y").Value}\" />");
			}
			anigroup.AppendLine("    </animation>");
		}
		anigroup.AppendLine("  </animations>");
		anigroup.AppendLine("</anigroup>");
		return sprite with { AnimationGroup = anigroup.ToString() };
	}

	/// <summary>Makes the image of the colours of PalCycle_SuperSonic: 4 colours wide, with one row for each frame.</summary>
	public static byte[] ImportPalette(string disassembly)
	{
		uint[] colours = SpriteImport.ReadColours(File.ReadAllBytes(Path.Combine(disassembly, SuperPaletteFile)));
		int rows = colours.Length / CycledColourCount;
		var pixels = new byte[rows * CycledColourCount * 4];
		for (int i = 0; i < rows * CycledColourCount; i++)
		{
			pixels[i * 4] = (byte)(colours[i] >> 24);
			pixels[i * 4 + 1] = (byte)(colours[i] >> 16);
			pixels[i * 4 + 2] = (byte)(colours[i] >> 8);
			pixels[i * 4 + 3] = (byte)colours[i];
		}
		return SpriteImport.EncodePng(pixels, CycledColourCount, rows);
	}

	/// <summary>Makes the texture and the animation group of the stars of Super Sonic (Obj7E), at four times the size.</summary>
	public static ImportedSprite ImportStars(string disassembly)
	{
		byte[] art = SpriteImport.ReadArt(disassembly, StarsArtFile);
		List<List<SpritePiece>> frames = SpriteImport.ParseMappings(File.ReadAllText(Path.Combine(disassembly, StarsMappingsFile)));
		uint[] palette = SpriteImport.ReadPalette(disassembly, PaletteFile);
		return SpriteImport.Import(StarsObjectKey, art, frames, new[] { 0 }, Array.Empty<int>(), palette, 4);
	}

	/// <summary>Reads the frames of a DPLC file of s2disasm (dplcHeader and dplcEntry macros): the tiles and the first tile of each entry.</summary>
	public static List<List<(int Tiles, int Offset)>> ParseDplc(string source)
	{
		var order = new List<string>();
		var frames = new Dictionary<string, List<(int, int)>>(StringComparer.Ordinal);
		List<(int, int)> current = null;
		foreach (string rawLine in source.Split('\n'))
		{
			string line = rawLine.Split(';')[0].Trim();
			Match entry = Regex.Match(line, @"^mappingsTableEntry\.w\s+([.\w]+)");
			if (entry.Success)
			{
				order.Add(entry.Groups[1].Value);
				continue;
			}
			Match header = Regex.Match(line, @"^([.\w]+):\s*dplcHeader");
			if (header.Success)
			{
				current = new List<(int, int)>();
				frames[header.Groups[1].Value] = current;
				continue;
			}
			Match load = Regex.Match(line, @"^dplcEntry\s+([$\w]+)\s*,\s*([$\w]+)");
			if (load.Success && current != null)
			{
				current.Add((ParseNumber(load.Groups[1].Value), ParseNumber(load.Groups[2].Value)));
			}
		}
		return order.Select(label => frames.TryGetValue(label, out List<(int, int)> frame) ? frame : new List<(int, int)>()).ToList();
	}

	private static int ParseNumber(string text)
	{
		return text.StartsWith('$') ? int.Parse(text.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture) : int.Parse(text, CultureInfo.InvariantCulture);
	}
}
