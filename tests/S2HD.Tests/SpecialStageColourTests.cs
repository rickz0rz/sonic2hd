using System;
using System.Collections.Generic;
using System.IO;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Tests the colors of the special stages: the reader of the stage palettes and the change of the floor colors. The
/// tests use made-up colors, not the colors of the game.
/// </summary>
public sealed class SpecialStageColourTests
{
	// Made-up colors 1 to 6 of two stages. Family one is 0, 2 and 5, family two is 1, 3 and 4. The middle shades are 2 and 3.
	private static readonly uint[] TealAndRed = { 0x40C0C0, 0xFF4040, 0x208080, 0xC02020, 0x801010, 0x104040 };

	private static readonly uint[] GreenAndPurple = { 0x40C040, 0xC040FF, 0x208020, 0x8020C0, 0x401080, 0x104010 };

	[Fact]
	public void Recolour_ChangesEachFamily_ToTheMiddleShadeOfTheNewFamily()
	{
		byte[] pixels =
		{
			0x20, 0x80, 0x80, 255, // the middle shade of family one
			0xC0, 0x20, 0x20, 255, // the middle shade of family two
		};

		HalfPipeFloorColours.Recolour(pixels, 4, TealAndRed, GreenAndPurple);

		Assert.Equal(new byte[] { 0x20, 0x80, 0x20, 255 }, pixels[0..4]);
		Assert.Equal(new byte[] { 0x80, 0x20, 0xC0, 255 }, pixels[4..8]);
	}

	[Fact]
	public void Recolour_KeepsTheBrightnessOfThePixel()
	{
		// Family one at half the brightness of its middle shade
		byte[] pixels = { 0x10, 0x40, 0x40 };

		HalfPipeFloorColours.Recolour(pixels, 3, TealAndRed, GreenAndPurple);

		Assert.Equal(new byte[] { 0x10, 0x40, 0x10 }, pixels);
	}

	[Theory]
	[InlineData(255, 255, 255)] // white
	[InlineData(128, 128, 128)] // gray
	[InlineData(255, 255, 0)] // yellow: not the hue of a family
	[InlineData(40, 40, 200)] // blue: not the hue of a family
	public void Recolour_KeepsThePixelsOfOtherColours(byte r, byte g, byte b)
	{
		byte[] pixels = { r, g, b, 200 };

		HalfPipeFloorColours.Recolour(pixels, 4, TealAndRed, GreenAndPurple);

		Assert.Equal(new byte[] { r, g, b, 200 }, pixels);
	}

	[Fact]
	public void Read_DecodesTheColours1To6_OfEachStagePalette()
	{
		string folder = Path.Combine(Path.GetTempPath(), "s2hd-palettes-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Path.Combine(folder, "art", "palettes"));
		try
		{
			for (int stage = 1; stage <= SpecialStageColours.StageCount; stage++)
			{
				// 16 words of 0000 BBB0 GGG0 RRR0. Colour 1 is full red, colour 2 full green, colour 3 full blue.
				byte[] palette = new byte[32];
				palette[2] = 0x00; palette[3] = 0x0E;
				palette[4] = 0x00; palette[5] = 0xE0;
				palette[6] = 0x0E; palette[7] = 0x00;
				palette[8] = 0x0E; palette[9] = (byte)(stage * 2);
				File.WriteAllBytes(Path.Combine(folder, "art", "palettes", $"Special Stage {stage}.bin"), palette);
			}

			List<uint[]> stages = SpecialStageColours.Read(folder);

			Assert.Equal(SpecialStageColours.StageCount, stages.Count);
			Assert.Equal(new uint[] { 0xFF0000, 0x00FF00, 0x0000FF }, stages[0][0..3]);
			// Colour 4 of stage 3 has red 6 of 14 and blue 14 of 14
			Assert.Equal(0x6D00FFu, stages[2][3]);

			StringWriter xml = new StringWriter();
			SpecialStageColours.Write(xml, stages);
			Assert.Contains("<stage number=\"1\">FF0000 00FF00 0000FF", xml.ToString());
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}
}
