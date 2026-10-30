using System.Collections.Generic;
using S2HD;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the text of the original credits and the palette fades of the original.</summary>
public sealed class OriginalCreditsTests
{
	[Fact]
	public void Encode_GivesTwoTilesForALetter_AndOneTileForI()
	{
		// S = $25,$26. I = $12 only. The space is tile 0.
		Assert.Equal(new List<int> { 0x25, 0x26, 0x12, 0x00 }, OriginalCredits.Encode("SI "));
	}

	[Fact]
	public void Encode_GivesTheRightHalfOfTheDigits_AndOneTileForTheBrackets()
	{
		// 1 = $3C,$3D (with "!"), 2 = $35,$36 (with "$"), 9 = $3E,$3F (with "#"), ( = $37, ) = $38
		Assert.Equal(new List<int> { 0x37, 0x3C, 0x3D, 0x35, 0x36, 0x3E, 0x3F, 0x38 }, OriginalCredits.Encode("(129)"));
	}

	[Fact]
	public void FadeIn_RaisesBlueThenGreenThenRed_In21Steps()
	{
		ushort colour = 0;
		List<ushort> steps = new List<ushort>();
		for (int i = 0; i < 22; i++)
		{
			colour = MegaDriveFade.FadeIn(colour, 0x0EEE);
			steps.Add(colour);
		}

		Assert.Equal(0x0200, steps[0]);
		Assert.Equal(0x0E00, steps[6]);
		Assert.Equal(0x0E20, steps[7]);
		Assert.Equal(0x0EE2, steps[14]);
		Assert.Equal(0x0EEE, steps[20]);
		Assert.Equal(0x0EEE, steps[21]);
	}

	[Fact]
	public void FadeIn_ToYellow_RaisesGreenThenRed()
	{
		Assert.Equal(0x0020, MegaDriveFade.FadeIn(0, 0x00EE));
		Assert.Equal(0x00E2, MegaDriveFade.FadeIn(0x00E0, 0x00EE));
	}

	[Fact]
	public void FadeOut_LowersRedThenGreenThenBlue()
	{
		Assert.Equal(0x0EEC, MegaDriveFade.FadeOut(0x0EEE));
		Assert.Equal(0x0EC0, MegaDriveFade.FadeOut(0x0EE0));
		Assert.Equal(0x0C00, MegaDriveFade.FadeOut(0x0E00));
		Assert.Equal(0, MegaDriveFade.FadeOut(0));
	}
}
