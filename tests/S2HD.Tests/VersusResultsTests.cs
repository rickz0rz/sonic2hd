using SonicOrca.Core;
using Xunit;

namespace S2HD.Tests;

/// <summary>The rules of the versus results: the points of an act, the winner of a zone and the winner of the game.</summary>
public sealed class VersusResultsTests
{
	private static VersusPlayerStats Stats(int score = 0, int time = 0, int rings = 0, int totalRings = 0, int monitors = 0) =>
		new VersusPlayerStats { Score = score, Time = time, Rings = rings, TotalRings = totalRings, Monitors = monitors };

	[Fact]
	public void Act_GivesAPointForEachCategory_AndTheLowerTimeWins()
	{
		VersusActResult result = new VersusActResult(
			Stats(score: 500, time: 3000, rings: 10, totalRings: 40, monitors: 2),
			Stats(score: 400, time: 2000, rings: 12, totalRings: 40, monitors: 1));

		Assert.Equal(new[] { 1, 2, 2, 0, 1 }, result.CategoryWinners);
		Assert.Equal(2, result.Player1Points);
		Assert.Equal(2, result.Player2Points);
		Assert.Equal(0, result.Winner);
	}

	[Fact]
	public void Zone_GoesToThePlayerWhoWonMoreActs()
	{
		VersusZoneResult zone = new VersusZoneResult("EMERALD HILL ZONE");
		zone.Acts.Add(new VersusActResult(Stats(score: 100), Stats()));
		// A tie in every category: nobody wins the act
		zone.Acts.Add(new VersusActResult(Stats(), Stats()));

		Assert.Equal(1, zone.Player1Wins);
		Assert.Equal(1, zone.Winner);
	}

	[Fact]
	public void Match_EndsWhenAllZonesArePlayed_AndAResetClearsTheResults()
	{
		VersusMatch match = new VersusMatch(new[] { "A", "B" });
		VersusZoneResult won = new VersusZoneResult("A");
		won.Acts.Add(new VersusActResult(Stats(), Stats(score: 100)));
		match.Zones[0] = won;

		Assert.False(match.Finished);
		match.Zones[1] = new VersusZoneResult("B");
		Assert.True(match.Finished);
		Assert.Equal(2, match.Winner);

		match.Player1Lives = 0;
		match.Reset();
		Assert.False(match.Finished);
		Assert.Equal(VersusMatch.StartLives, match.Player1Lives);
	}

	[Fact]
	public void Time_ShowsMinutesSecondsAndHundredths()
	{
		Assert.Equal("1'02\"43", VersusActResult.FormatTime(3746));
	}
}
