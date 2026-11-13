using System.Collections.Generic;
using System.Linq;
using System.Xml;
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
	public void Encode_GivesTheTileOfAt_ForTheCopyrightSign()
	{
		Assert.Equal(new List<int> { 0x3B }, OriginalCredits.Encode(OriginalCredits.CopyrightSign.ToString()));
	}

	[Fact]
	public void GetCharacterName_NamesLettersDigitsAndPunctuation()
	{
		Assert.Equal("A", OriginalCredits.GetCharacterName('A'));
		Assert.Equal("9", OriginalCredits.GetCharacterName('9'));
		Assert.Equal("FULLSTOP", OriginalCredits.GetCharacterName('.'));
		Assert.Equal("APOSTROPHE", OriginalCredits.GetCharacterName('\''));
		Assert.Equal("LEFTBRACKET", OriginalCredits.GetCharacterName('('));
		Assert.Equal("RIGHTBRACKET", OriginalCredits.GetCharacterName(')'));
		Assert.Equal("COPYRIGHT", OriginalCredits.GetCharacterName(OriginalCredits.CopyrightSign));
		Assert.Equal("U00C9", OriginalCredits.GetCharacterName('\u00C9'));
	}

	[Fact]
	public void ParseSlides_ReadsWhatWriteSlidesWrites()
	{
		List<OriginalCredits.Slide> slides = new List<OriginalCredits.Slide>
		{
			new OriginalCredits.Slide(new[]
			{
				new OriginalCredits.TextItem(1, "EXECUTIVE", 88, 64),
				new OriginalCredits.TextItem(0, "(" + OriginalCredits.CopyrightSign + "1992 S.O)", 0, 216)
			}),
			new OriginalCredits.Slide(new[] { new OriginalCredits.TextItem(0, "YUJI  NAKA (YU2)", 40, 120) })
		};
		XmlDocument document = new XmlDocument();
		document.LoadXml(OriginalCredits.WriteSlides(slides));

		List<OriginalCredits.Slide> read = OriginalCredits.ParseSlides(document);

		Assert.Equal(2, read.Count);
		Assert.Equal(slides[0].Items, read[0].Items);
		Assert.Equal(slides[1].Items, read[1].Items);
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

	private static List<OriginalCredits.Slide> Slides(params int?[] frames) =>
		frames.Select(f => new OriginalCredits.Slide(new[] { new OriginalCredits.TextItem(0, "A", 0, 0) }, f)).ToList();

	private static int Total(IReadOnlyList<OriginalCredits.Slide> slides, int[] holdFrames) =>
		holdFrames.Sum() + slides.Count * 2 * OriginalCredits.FadeFrames;

	[Fact]
	public void GetHoldFrames_GivesTheOriginalTiming_ToTheOriginalSlides()
	{
		List<OriginalCredits.Slide> slides = Slides(new int?[OriginalCredits.OriginalSlideCount]);

		int[] frames = OriginalCredits.GetHoldFrames(slides);

		Assert.All(frames, f => Assert.Equal(OriginalCredits.OriginalHoldFrames, f));
		Assert.Equal(9303, Total(slides, frames));
	}

	[Fact]
	public void GetHoldFrames_SharesTheTimeOfTheSong_WhenAModAddsSlides()
	{
		List<OriginalCredits.Slide> slides = Slides(new int?[23]);

		int[] frames = OriginalCredits.GetHoldFrames(slides);

		Assert.All(frames, f => Assert.InRange(f, 360, 361));
		Assert.Equal(OriginalCredits.SlideBudgetFrames, Total(slides, frames));
	}

	[Fact]
	public void GetHoldFrames_KeepsTheFramesOfASlide_AndSharesTheRest()
	{
		List<OriginalCredits.Slide> slides = Slides(600, null, null, null);

		int[] frames = OriginalCredits.GetHoldFrames(slides);

		Assert.Equal(600, frames[0]);
		Assert.Equal(OriginalCredits.SlideBudgetFrames, Total(slides, frames));
		Assert.True(frames.Skip(1).Max() - frames.Skip(1).Min() <= 1);
	}

	[Fact]
	public void GetHoldFrames_GivesOneSecondAtLeast_WhenTheFixedSlidesTakeTheTime()
	{
		Assert.Equal(new[] { 9100, 60, 60 }, OriginalCredits.GetHoldFrames(Slides(9100, null, null)));
	}

	[Fact]
	public void ParseSlides_ReadsTheFramesOfASlide()
	{
		List<OriginalCredits.Slide> slides = Slides(600, null);
		XmlDocument document = new XmlDocument();
		document.LoadXml(OriginalCredits.WriteSlides(slides));

		List<OriginalCredits.Slide> read = OriginalCredits.ParseSlides(document);

		Assert.Equal(600, read[0].Frames);
		Assert.Null(read[1].Frames);
	}
}
