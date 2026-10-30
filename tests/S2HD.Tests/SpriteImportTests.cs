using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

public sealed class SpriteImportTests
{
	private const string Mappings = """
		Map_obj44:	mappingsTable
			mappingsTableEntry.w	Map_obj44_0016
			mappingsTableEntry.w	Map_obj44_0004

		Map_obj44_0004:	spriteHeader
			spritePiece	-$10, -$10, 2, 4, 0, 0, 0, 0, 0 ; a comment
			spritePiece	0, -$10, 2, 4, 0, 1, 0, 0, 0
		Map_obj44_0004_End

		Map_obj44_0016:	spriteHeader
			spritePiece2P	-$18, -$14, 3, 4, 8, 0, 0, 1, 0, 4, 0, 0, 1, 0
		Map_obj44_0016_End
		""";

	[Fact]
	public void ParseMappings_ReadsTheFramesInTableOrder()
	{
		List<List<SpritePiece>> frames = SpriteImport.ParseMappings(Mappings);

		Assert.Equal(2, frames.Count);
		Assert.Equal(new SpritePiece(-0x18, -0x14, 3, 4, 8, false, false, 1), frames[0][0]);
		Assert.Equal(2, frames[1].Count);
		Assert.Equal(new SpritePiece(0, -0x10, 2, 4, 0, true, false, 0), frames[1][1]);
	}

	[Fact]
	public void Render_DrawsTilesDownEachColumn_WithFlipsAndThePaletteLine()
	{
		// Tile 0 is colour 1, tile 1 is colour 2: a piece of 1 x 2 tiles has tile 0 on top of tile 1
		var art = new byte[64];
		for (int i = 0; i < 32; i++)
		{
			art[i] = 0x11;
			art[32 + i] = 0x22;
		}
		var palette = new uint[64];
		palette[16 + 1] = 0xFF0000FF;
		palette[16 + 2] = 0x00FF00FF;
		var frame = new List<SpritePiece> { new SpritePiece(-4, -8, 1, 2, 0, false, true, 0) };

		(uint[] pixels, int width, int height, int originX, int originY) = SpriteImport.Render(frame, art, palette, paletteLine: 1);

		Assert.Equal((8, 16, -4, -8), (width, height, originX, originY));
		// The vertical flip puts tile 1 on top
		Assert.Equal(0x00FF00FFu, pixels[0]);
		Assert.Equal(0xFF0000FFu, pixels[15 * width]);
	}

	[Fact]
	public void ParseDplc_ReadsTheEntriesInTableOrder()
	{
		const string dplc = """
			.offsets:	mappingsTable
				mappingsTableEntry.w	.frame1
				mappingsTableEntry.w	.frame0

			.frame0:	dplcHeader
			.frame0_End

			.frame1:	dplcHeader
				dplcEntry	$C, $7E2
				dplcEntry	2, 4 ; a comment
			.frame1_End
			""";

		List<List<(int Tiles, int Offset)>> frames = SuperSonicArt.ParseDplc(dplc);

		Assert.Equal(2, frames.Count);
		Assert.Equal(new[] { (0xC, 0x7E2), (2, 4) }, frames[0]);
		Assert.Empty(frames[1]);
	}

	[Fact]
	public void ParseDplc_GivesALabelAloneOnItsLine_TheEntriesOfTheNextHeader()
	{
		// Sonic.asm has ".frame86:" before ".frame84: dplcHeader": both frames load the same tiles
		const string dplc = """
			.offsets:	mappingsTable
				mappingsTableEntry.w	.frame0
				mappingsTableEntry.w	.frame1

			.frame1:
			.frame0:	dplcHeader
				dplcEntry	4, $548
			.frame0_End
			""";

		List<List<(int Tiles, int Offset)>> frames = SuperSonicArt.ParseDplc(dplc);

		Assert.Equal(new[] { (4, 0x548) }, frames[0]);
		Assert.Equal(new[] { (4, 0x548) }, frames[1]);
	}

	[Fact]
	public void SuperSonicArt_HasAnAnimationForEachCharacterAnimationTheTransformationAndTheWalkVariant()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();

		ImportedSprite sprite = SuperSonicArt.Import(disassembly);

		XElement[] animations = XDocument.Parse(sprite.AnimationGroup).Descendants("animation").ToArray();
		Assert.Equal(System.Enum.GetValues<SonicOrca.Core.Objects.Base.CharacterAnimation>().Length, SuperSonicArt.TransformAnimation);
		Assert.Equal(SuperSonicArt.TransformAnimation + 1, SuperSonicArt.WalkVariantAnimation);
		Assert.Equal(SuperSonicArt.WalkVariantAnimation + 1, animations.Length);
		int walk = (int)SonicOrca.Core.Objects.Base.CharacterAnimation.Walk;
		Assert.Equal(animations[walk].Elements("frame").Count(), animations[SuperSonicArt.WalkVariantAnimation].Elements("frame").Count());
		Assert.All(animations, animation => Assert.NotEmpty(animation.Elements("frame")));
		// Each frame has pixels: its tiles come from its DPLC
		Assert.All(animations.SelectMany(animation => animation.Elements("frame")), frame => Assert.True(int.Parse(frame.Attribute("w").Value) > 4));
		Assert.Equal("SONICORCA/OBJECTS/SONIC/SUPER", sprite.ObjectKey);
		Assert.Equal(new[] { "//TEXTURE", "//PALETTE" }, XDocument.Parse(sprite.AnimationGroup).Descendants("texture").Select(texture => texture.Value));
	}

	[Fact]
	public void SuperSonicArt_PaletteHasARowForEachFrameOfTheCycle()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();

		byte[] png = SuperSonicArt.ImportPalette(disassembly);

		var reader = new Hjg.Pngcs.PngReader(new MemoryStream(png));
		Assert.Equal(SuperSonicArt.CycledColourCount, reader.ImgInfo.Cols);
		// CyclingPal_SSTransformation has 16 frames, from $00 to $78
		Assert.Equal(16, reader.ImgInfo.Rows);
		// Row 0 is the blue of Sonic, which the sprites have. The game finds the colours by their value, so they must differ.
		byte[] row = reader.ReadRowByte(0).ScanlineB;
		reader.End();
		var colours = Enumerable.Range(0, SuperSonicArt.CycledColourCount).Select(i => (row[i * 4], row[i * 4 + 1], row[i * 4 + 2])).ToArray();
		Assert.Equal(colours.Length, colours.Distinct().Count());
		Assert.Equal((36, 36, 182), ((int)colours[0].Item1, (int)colours[0].Item2, (int)colours[0].Item3));
	}

	[Fact]
	public void SuperSonicArt_StarsHaveTheSixFramesOfObj7E()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();

		ImportedSprite stars = SuperSonicArt.ImportStars(disassembly);

		Assert.Equal(SuperSonicArt.StarsObjectKey, stars.ObjectKey);
		Assert.Equal(6, XDocument.Parse(stars.AnimationGroup).Descendants("animation").Count());
	}
}
