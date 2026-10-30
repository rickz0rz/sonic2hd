using S2HD.Ending;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the logic of the ending cutscene: RandomNumber, the rows of SwScrl_DEZ, and the fades of ObjC9.</summary>
public sealed class EndingLogicTests
{
	[Fact]
	public void Random_FromZero_UsesTheResetValue_AndMultipliesBy41()
	{
		EndingRandom random = new EndingRandom();

		random.Next();
		Assert.Equal(0x7FE7B46Au, random.Seed);
		random.Next();
		Assert.Equal(0x6115E4FAu, random.Seed);
	}

	[Fact]
	public void Random_RotateRight_TurnsTheSeed()
	{
		EndingRandom random = new EndingRandom { Seed = 0x00000003 };

		Assert.Equal(0x80000001u, random.RotateRight(1));
	}

	[Fact]
	public void StarRows_MoveAtTheirSpeeds_AndTheEarthFollowsRow24()
	{
		EndingStarRows rows = new EndingStarRows();
		for (int i = 0; i < 16; i++)
		{
			rows.Update();
		}

		Assert.Equal(0, rows.Values[0]);
		Assert.Equal(3 * 16, rows.Values[1]);
		Assert.Equal(6 * 16, rows.Values[10]);
		// Row 24 moves 1 pixel each frame, row 25 at half of it, and the edge of the Earth at 5/8, 6/8 and 7/8 of it
		Assert.Equal(16, rows.Values[24]);
		Assert.Equal(8, rows.Values[25]);
		Assert.Equal(10, rows.Values[29]);
		Assert.Equal(12, rows.Values[30]);
		Assert.Equal(14, rows.Values[31]);
		Assert.Equal(16, rows.Values[32]);
		Assert.Equal(0, rows.Values[35]);
	}

	[Fact]
	public void Palette_StartsWhite_AndAFadeTakes7StepsOfItsTime()
	{
		EndingPalette palette = new EndingPalette();
		Assert.Equal(1.0, palette.GetWhiteness(0));

		// Subtype 8: lines 0 and 1, one step each 5 frames, after the frame of ObjC9_Init
		palette.Start(8);
		palette.Update();
		for (int i = 0; i < 5; i++)
		{
			palette.Update();
		}
		Assert.Equal(6 / 7.0, palette.GetWhiteness(0), 6);
		Assert.Equal(6 / 7.0, palette.GetWhiteness(1), 6);
		Assert.Equal(1.0, palette.GetWhiteness(2));
		for (int i = 0; i < 40; i++)
		{
			palette.Update();
		}
		Assert.Equal(0.0, palette.GetWhiteness(0));

		// Subtype 6 fades line 0 to white again
		palette.Start(6);
		for (int i = 0; i < 100; i++)
		{
			palette.Update();
		}
		Assert.Equal(1.0, palette.GetWhiteness(0));
		Assert.Equal(0.0, palette.GetWhiteness(1));
	}
}
