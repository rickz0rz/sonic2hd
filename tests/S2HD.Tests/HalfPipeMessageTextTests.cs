using System.Linq;
using SonicOrca.Core.SpecialStage;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the places of the letters and digits of the special stage messages, from Obj5A of the original.</summary>
public sealed class HalfPipeMessageTextTests
{
	[Fact]
	public void RingRequirement_PutsGetTheDigitsAndRings_WhereTheOriginalPrintsThem()
	{
		var glyphs = HalfPipeMessageText.RingRequirement(40);

		Assert.Equal(new[] { 0x54, 0x5C, 0x64, 0x70, 0x78, 0x84, 0x8C, 0x94, 0x9C, 0xA4, 0xAC }, glyphs.Select(g => g.X));
		Assert.All(glyphs, g => Assert.Equal(0x6C, g.Y));
		// G E T, then the digits 4 and 0 of the HUD art, then R I N G S !
		Assert.Equal(new[] { (false, 0), (false, 1), (false, 2), (true, 4), (true, 0), (false, 3), (false, 4), (false, 5), (false, 0), (false, 6), (false, 0x11) },
			glyphs.Select(g => (g.Digit, g.Frame)));
	}

	[Fact]
	public void RingRequirement_WithAHundredsDigit_MovesTheDigitsAndRingsRight()
	{
		var glyphs = HalfPipeMessageText.RingRequirement(140);

		Assert.Equal(new[] { 0x70, 0x78, 0x80 }, glyphs.Where(g => g.Digit).Select(g => g.X));
		Assert.Equal(0x8C, glyphs.First(g => !g.Digit && g.X > 0x80).X);
	}

	[Fact]
	public void Checkpoint_PutsCoolUnderTheHand_AndNotEnoughRingsOnTwoLines()
	{
		Assert.Equal(new[] { 0x74, 0x7C, 0x84, 0x8C, 0x94 }, HalfPipeMessageText.Checkpoint(true).Select(g => g.X));

		var failed = HalfPipeMessageText.Checkpoint(false);
		Assert.Equal(9, failed.Count(g => g.Y == 0x68));
		Assert.Equal(new[] { 0x5E, 0x66, 0x6E, 0x76, 0x7E, 0x8E, 0x96, 0x9E }, failed.Where(g => g.Y == 0x7E).Select(g => g.X));
	}

	[Fact]
	public void RingsToGo_HasTheS_OnlyForTwoOrMore_AndMovesToGoLeftWithoutIt()
	{
		var many = HalfPipeMessageText.RingsToGo(25);
		var one = HalfPipeMessageText.RingsToGo(1);

		Assert.Equal(new[] { 0x52, 0x5A }, many.Where(g => g.Digit).Select(g => g.X));
		Assert.Contains(many, g => !g.Digit && g.X == 0x88);
		Assert.Equal(0xC0, many.Last().X);
		Assert.DoesNotContain(one, g => !g.Digit && g.X == 0x88);
		Assert.Equal(0xB8, one.Last().X);
	}
}
