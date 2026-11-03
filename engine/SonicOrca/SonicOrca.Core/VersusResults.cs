using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Core;

/// <summary>The values of one player at the end of a versus act. The results compare them.</summary>
public sealed class VersusPlayerStats
{
	public int Score { get; init; }

	/// <summary>The time of the player in ticks: the time when they passed the signpost, or the time of the act end.</summary>
	public int Time { get; init; }

	public int Rings { get; init; }

	public int TotalRings { get; init; }

	public int Monitors { get; init; }

	/// <summary>Takes the values of a player at the end of an act. A player who did not finish has the time of the level.</summary>
	public static VersusPlayerStats FromPlayer(Player player, int levelTime) => new VersusPlayerStats
	{
		Score = player.Score,
		Time = player.StoppedTime ?? levelTime,
		Rings = player.CurrentRings,
		TotalRings = player.TotalRings,
		Monitors = player.MonitorsBroken
	};
}

/// <summary>
/// The result of one versus act, as Setup2PResults_Act of the original. Each of the five categories gives a point to
/// the player who did better: the higher score, the lower time, more rings, more total rings and more monitors. A tie
/// in a category gives no point. The player with more points wins the act.
/// </summary>
public sealed class VersusActResult
{
	public const int CategoryCount = 5;

	public static readonly string[] CategoryNames = { "SCORE", "TIME", "RINGS", "TOTAL RINGS", "ITEMS" };

	public VersusPlayerStats Player1 { get; }

	public VersusPlayerStats Player2 { get; }

	/// <summary>The winner of each category: 1 or 2, or 0 for a tie.</summary>
	public IReadOnlyList<int> CategoryWinners { get; }

	public int Player1Points => CategoryWinners.Count(winner => winner == 1);

	public int Player2Points => CategoryWinners.Count(winner => winner == 2);

	/// <summary>The winner of the act: 1 or 2, or 0 for a tie.</summary>
	public int Winner => Compare(Player1Points, Player2Points);

	public VersusActResult(VersusPlayerStats player1, VersusPlayerStats player2)
	{
		Player1 = player1;
		Player2 = player2;
		CategoryWinners = new[]
		{
			Compare(player1.Score, player2.Score),
			Compare(player2.Time, player1.Time),
			Compare(player1.Rings, player2.Rings),
			Compare(player1.TotalRings, player2.TotalRings),
			Compare(player1.Monitors, player2.Monitors)
		};
	}

	/// <summary>The value of a category for a player, as the results screen shows it.</summary>
	public static string FormatCategory(VersusPlayerStats stats, int category) => category switch
	{
		0 => stats.Score.ToString(),
		1 => FormatTime(stats.Time),
		2 => stats.Rings.ToString(),
		3 => stats.TotalRings.ToString(),
		_ => stats.Monitors.ToString()
	};

	/// <summary>A time in ticks as minutes, seconds and hundredths, as the results of the original: 1'23"45.</summary>
	public static string FormatTime(int ticks) => $"{ticks / 3600}'{ticks / 60 % 60:00}\"{ticks % 60 * 100 / 60:00}";

	/// <summary>1 when the first value is higher, 2 when the second value is higher, or 0 when they are equal.</summary>
	internal static int Compare(int first, int second) => first > second ? 1 : first < second ? 2 : 0;
}

/// <summary>
/// The result of the special stage of versus mode, as Setup2PResults_SpecialAct of the original: each checkpoint ends a
/// segment, and the player with more rings in a segment wins it. The player who won more segments wins the stage.
/// </summary>
public sealed class VersusSpecialStageResult
{
	/// <summary>The rings of each player in each segment.</summary>
	public IReadOnlyList<(int Player1, int Player2)> Segments { get; }

	public VersusSpecialStageResult(IReadOnlyList<(int Player1, int Player2)> segments)
	{
		Segments = segments;
	}

	/// <summary>The number of the special stage, from 1, or 0 when it is not known.</summary>
	public int Stage { get; init; }

	/// <summary>The winner of each segment: 1 or 2, or 0 for a tie.</summary>
	public IReadOnlyList<int> SegmentWinners => Segments.Select(s => VersusActResult.Compare(s.Player1, s.Player2)).ToArray();

	public int Player1Points => SegmentWinners.Count(winner => winner == 1);

	public int Player2Points => SegmentWinners.Count(winner => winner == 2);

	/// <summary>The winner of the stage: 1 or 2, or 0 for a tie.</summary>
	public int Winner => VersusActResult.Compare(Player1Points, Player2Points);
}

/// <summary>
/// The result of one versus zone, as Setup2PResults_Zone of the original: the player who won more acts wins the zone.
/// When the acts end in a tie, a special stage breaks the tie, and it counts as one more act. A game over ends the
/// zone without a winner.
/// </summary>
public sealed class VersusZoneResult
{
	public string Name { get; }

	public List<VersusActResult> Acts { get; } = new List<VersusActResult>();

	public VersusZoneResult(string name)
	{
		Name = name;
	}

	/// <summary>
	/// The zone of special stages, the fourth zone of the original: it has no acts, only special stages, as the
	/// "SPECIAL STAGE" of the level select of two-player mode.
	/// </summary>
	public bool IsSpecialStageZone { get; init; }

	/// <summary>The special stages of the zone: the one that broke a tie of the acts, or the stages of the special stage zone.</summary>
	public List<VersusSpecialStageResult> SpecialStages { get; } = new List<VersusSpecialStageResult>();

	/// <summary>The special stage that broke a tie of the acts, or null.</summary>
	public VersusSpecialStageResult SpecialStage
	{
		get => SpecialStages.FirstOrDefault();
		set
		{
			SpecialStages.Clear();
			if (value != null)
			{
				SpecialStages.Add(value);
			}
		}
	}

	public int Player1Wins => Acts.Count(act => act.Winner == 1) + SpecialStages.Count(stage => stage.Winner == 1);

	public int Player2Wins => Acts.Count(act => act.Winner == 2) + SpecialStages.Count(stage => stage.Winner == 2);

	/// <summary>True when the acts ended in a tie, and no special stage broke it yet.</summary>
	public bool NeedsTieBreak => Winner == 0 && SpecialStages.Count == 0;

	/// <summary>
	/// The special stage zone plays two stages, and a third one only when the first two did not give a winner, as the
	/// original does after its second stage (SS_Total_Won).
	/// </summary>
	public bool NeedsAnotherSpecialStage => IsSpecialStageZone && (SpecialStages.Count < 2 || (SpecialStages.Count == 2 && Winner == 0));

	/// <summary>The winner of the zone: 1 or 2, or 0 for a tie.</summary>
	public int Winner => VersusActResult.Compare(Player1Wins, Player2Wins);
}

/// <summary>
/// A versus match: the zones that the players can choose, and the result of each zone that they played. When the
/// players played all the zones, the player who won more zones wins the game (Setup2PResults_Game of the original).
/// </summary>
public sealed class VersusMatch
{
	/// <summary>The results of the zones, in the order of <see cref="ZoneNames"/>. A zone that the players did not play is null.</summary>
	public VersusZoneResult[] Zones { get; }

	public IReadOnlyList<string> ZoneNames { get; }

	/// <summary>The monitors give only the teleport (the "teleport only" items of the original).</summary>
	public bool TeleportOnly { get; set; }

	/// <summary>Casino Night uses the two-player object layouts of the original, or the layouts of the main game.</summary>
	public bool TwoPlayerLayouts { get; set; } = true;

	/// <summary>A zone of the match has two-player layouts, so the setup screen shows the option.</summary>
	public bool HasTwoPlayerLayouts { get; set; }

	public int Player1Lives { get; set; } = StartLives;

	public int Player2Lives { get; set; } = StartLives;

	public const int StartLives = 3;

	public VersusMatch(IReadOnlyList<string> zoneNames)
	{
		ZoneNames = zoneNames;
		Zones = new VersusZoneResult[zoneNames.Count];
	}

	public bool Finished => Zones.All(zone => zone != null);

	public int Player1Wins => Zones.Count(zone => zone?.Winner == 1);

	public int Player2Wins => Zones.Count(zone => zone?.Winner == 2);

	/// <summary>The winner of the game: 1 or 2, or 0 for a tie.</summary>
	public int Winner => VersusActResult.Compare(Player1Wins, Player2Wins);

	/// <summary>
	/// A game over ends the match, as TwoPlayerResultsDone_ZoneOrSpecialStages of the original: the results of all
	/// the zones go, and both players have the start lives again.
	/// </summary>
	public void Reset()
	{
		Array.Clear(Zones);
		Player1Lives = StartLives;
		Player2Lives = StartLives;
	}
}
