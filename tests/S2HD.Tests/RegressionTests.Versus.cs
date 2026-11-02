using System.Collections.Generic;
using System.IO;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// The regression tests of versus mode: the split screen, the rules of an act, the monitors, the screens of a match
/// and the effects in the halves. The helpers are in <see cref="RegressionTests"/>.
/// </summary>
public sealed partial class RegressionTests
{
	/// <summary>
	/// Versus mode: player 2 plays Tails as a second protagonist, with their own rings. The input is the same as in
	/// <see cref="CoopPlayer2_MatchesGoldenScreenshots"/>: Tails runs away and collects rings. The HUD shows player 1,
	/// so it shows 0 rings, not the rings of Tails.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusPlayer2_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-player2", Path.Combine(TestPaths.GoldenDirectory, "versus-player2"), new[] { 270, 320 }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(600, tick => (tick >= 200 && tick < 400 ? 1.0 : 0.0, tick >= 260 && tick < 285)));
			return "--level ehz --versus";
		}, new Dictionary<string, string> { ["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec") });
	}

	/// <summary>
	/// Versus mode with the split screen: player 1 (Sonic) in the top half and player 2 (Tails) in the bottom half, each
	/// with their own camera. Both cameras start zoomed out. Tails runs away, so the two halves show different parts of
	/// the level. The second run has player 1 zoomed in and player 2 zoomed out (S2HD_TEST_VERSUS_ZOOM, because a test
	/// cannot press the zoom keys).
	/// </summary>
	[Theory]
	[Trait("Category", "Regression")]
	[InlineData("out,out")]
	[InlineData("in,out")]
	public void VersusSplitScreen_MatchesGoldenScreenshots(string zoom)
	{
		string name = "versus-split-" + zoom.Replace(',', '-');
		RunTwoRuns(name, Path.Combine(TestPaths.GoldenDirectory, name), new[] { 420 }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(600, tick => (tick >= 200 && tick < 400 ? 1.0 : 0.0, tick >= 260 && tick < 285)));
			return "--level ehz --versus";
		}, new Dictionary<string, string>
		{
			["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec"),
			["S2HD_TEST_VERSUS_ZOOM"] = zoom
		});
	}

	/// <summary>
	/// Versus mode: a dead player does not restart the level. The test kills Tails at tick 450
	/// (S2HD_TEST_KILL_PLAYER2_AT_TICK) while he runs away. He loses a life and comes back at the start, next to Sonic,
	/// and his camera goes to him. Sonic plays on.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusRespawnAtStart_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-respawn-start", Path.Combine(TestPaths.GoldenDirectory, "versus-respawn-start"), new[] { 460, 620 }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(800, tick => (tick >= 200 ? 1.0 : 0.0, false)));
			return "--level ehz --versus";
		}, new Dictionary<string, string>
		{
			["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec"),
			["S2HD_TEST_KILL_PLAYER2_AT_TICK"] = "450"
		}, userData => Assert.Contains("Versus: player 2 comes back at", File.ReadAllText(Path.Combine(userData, "sonicorca.log"))));
	}

	/// <summary>
	/// Versus mode: each player has their own star posts. Both players start on the ledge of the first star post of
	/// Emerald Hill 1. Tails walks into the star post, and Sonic does not. The test kills Tails, and he comes back at
	/// the star post, not at the start.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusRespawnAtStarpost_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-respawn-starpost", Path.Combine(TestPaths.GoldenDirectory, "versus-respawn-starpost"), new[] { 650 }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(800, tick => (tick >= 200 && tick < 300 ? 1.0 : 0.0, false)));
			return "--level ehz --versus --startpos 14180 650";
		}, new Dictionary<string, string>
		{
			["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec"),
			["S2HD_TEST_KILL_PLAYER2_AT_TICK"] = "400"
		}, userData => Assert.Contains("Versus: player 2 comes back at 14272, 716 (star post 1)", File.ReadAllText(Path.Combine(userData, "sonicorca.log"))));
	}

	/// <summary>
	/// Versus mode: the end of an act. Both players start just before the signpost of Emerald Hill 1. Tails runs past
	/// it and Sonic stands. Tails finishes: his time stops and the sign shows his face. Sonic gets 60 seconds, and his
	/// half shows the countdown in red. When the time runs out, act 1 ends, and the act results show Tails as the
	/// winner on time.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusSignpost_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-signpost", Path.Combine(TestPaths.GoldenDirectory, "versus-signpost"), new[] { 320, 1000, VersusScreenCapture(0, 60) }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(5000, tick => (tick >= 200 && tick < 300 ? 1.0 : 0.0, false)));
			return "--level ehz --versus --startpos 42900 2800";
		}, new Dictionary<string, string>
		{
			["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec"),
			["S2HD_TEST_VERSUS_RESULTS_FRAMES"] = "120"
		},
		userData =>
		{
			string log = File.ReadAllText(Path.Combine(userData, "sonicorca.log"));
			Assert.Contains("Versus: player 2 finished at", log);
			Assert.Contains("Versus: the time limit ran out", log);
			Assert.Contains("Versus: act 1 points 0 to 1", log);
		});
	}

	/// <summary>
	/// Versus mode: a character behind a waterfall of Emerald Hill 1. The waterfall effect copies the canvas of the
	/// half, which is not as high as the screen. Tails runs on the lower path, and he must show behind the waterfall
	/// at his own height, not lower.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusWaterfall_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-waterfall", Path.Combine(TestPaths.GoldenDirectory, "versus-waterfall"), new[] { 255 }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(400, tick => (tick >= 200 ? 1.0 : 0.0, false)));
			return "--level ehz --versus --startpos 11500 2500";
		}, new Dictionary<string, string> { ["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec") });
	}

	/// <summary>
	/// Versus mode: the end of a zone. Both players start just before the versus signpost of Emerald Hill 2. Tails runs
	/// past it, and the time of Sonic runs out. The act results, the zone results and the game results show Tails as
	/// the winner. A new user folder has only Emerald Hill of the versus zones, so the match ends after one zone.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusZoneResults_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-zone-results", Path.Combine(TestPaths.GoldenDirectory, "versus-zone-results"),
			new[] { VersusScreenCapture(0, 30), VersusScreenCapture(1, 30), VersusScreenCapture(2, 30) }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(5000, tick => (tick >= 200 && tick < 300 ? 1.0 : 0.0, false)));
			return "--level ehz --act 2 --versus --startpos 87100 4300";
		}, new Dictionary<string, string>
		{
			["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec"),
			["S2HD_TEST_VERSUS_RESULTS_FRAMES"] = "60"
		},
		userData =>
		{
			string log = File.ReadAllText(Path.Combine(userData, "sonicorca.log"));
			// The game ends after the last capture, so the log has no line for the end of the game
			Assert.Contains("Versus: zone EMERALD HILL ZONE winner 2", log);
		});
	}

	/// <summary>The setup screen of a versus match, which "--versus" without "--level" opens.</summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusSetup_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-setup", Path.Combine(TestPaths.GoldenDirectory, "versus-setup"), new[] { VersusScreenCapture(0, 30) }, _ => "--versus");
	}

	/// <summary>The capture number of a frame of a versus screen: the screen counts from 0 (see TestHooks).</summary>
	private static int VersusScreenCapture(int screen, int frame) => 100000 + 1000 * screen + frame;

	/// <summary>
	/// Versus mode: the water of Chemical Plant 2 in the halves. Player 1 is zoomed in and above the water. Player 2 is
	/// zoomed out, and the half shows the surface of the water over all its width. The water effect draws into the
	/// canvas of the half, which is not the size of the screen.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusWater_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-water", Path.Combine(TestPaths.GoldenDirectory, "versus-water"), new[] { 300 },
			_ => "--level cpz --act 2 --versus --startpos 73000 7800", new Dictionary<string, string> { ["S2HD_TEST_VERSUS_ZOOM"] = "in,out" });
	}

	/// <summary>
	/// Versus mode: the teleport monitor. Both players start near the second monitor of Emerald Hill 1. Sonic stands on
	/// a ledge, and Tails is on the path below. The monitor shows a question mark. Sonic jumps onto it, and the test
	/// hook makes its item the teleport. The players change places, and the screen flashes white.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void VersusTeleport_MatchesGoldenScreenshots()
	{
		RunTwoRuns("versus-teleport", Path.Combine(TestPaths.GoldenDirectory, "versus-teleport"), new[] { 200, 320, 420 }, userData =>
		{
			string recording = Path.Combine(userData, "player1.rec");
			File.WriteAllBytes(recording, MakeRecording(600, tick => (tick >= 200 && tick < 225 ? 1.0 : 0.0, tick >= 205 && tick < 220)));
			return $"--level ehz --versus --startpos 11500 2500 --playback \"{recording}\"";
		}, new Dictionary<string, string> { ["S2HD_TEST_VERSUS_MONITOR_ITEM"] = "8" },
		userData => Assert.Contains("Versus: the players change places", File.ReadAllText(Path.Combine(userData, "sonicorca.log"))));
	}
}
