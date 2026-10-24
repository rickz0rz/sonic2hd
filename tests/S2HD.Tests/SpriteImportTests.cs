using System.Collections.Generic;
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
}
