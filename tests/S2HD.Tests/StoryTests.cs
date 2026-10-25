using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Plays the whole story with the imported zones and no player (S2HD_TEST_STORY, see StoryTest in the engine). It
/// tests the joins of the levels: the start of each act, the change from act 1 to act 2, and the change to the next
/// zone. Skipped when the game is not built or the Sonic 2 disassembly is not found. The screenshots of each act
/// stay in the user data folder of the run when the test fails.
/// </summary>
public sealed class StoryTests
{
	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(25);

	/// <summary>The acts of the story, in order. A zone with one act has act 0.</summary>
	private static readonly string[] ExpectedActs =
	{
		"EHZ 1", "EHZ 2", "CPZ 1", "CPZ 2", "ARZ 1", "ARZ 2", "CNZ 1", "CNZ 2", "HTZ 1", "HTZ 2", "MCZ 1", "MCZ 2",
		"OOZ 1", "OOZ 2", "MTZ 1", "MTZ 2", "MTZ 3", "SCZ 0", "WFZ 0", "DEZ 0"
	};

	/// <summary>The player must be on the ground at this tick of an act, after the title card and a short wait.</summary>
	private const int GroundCheckTick = 420;

	/// <summary>Acts where the player does not start on the ground: the Tornado of Sky Chase.</summary>
	private static readonly HashSet<string> AirStarts = new HashSet<string> { "SCZ 0" };

	private sealed record ActLog(string Act, bool Seamless, List<string> Lines);

	[Fact]
	[Trait("Category", "Story")]
	public void Story_PlaysEveryActToTheEnd()
	{
		string exe = TestPaths.GameExecutable;
		if (!File.Exists(exe))
		{
			Assert.Skip($"Game not built: {exe}.");
		}
		ImportedMods.Get();
		string userData = Directory.CreateTempSubdirectory("s2hd-story-").FullName;
		ImportedMods.CopyTo(userData);
		File.WriteAllText(Path.Combine(userData, "sonicorca.cfg"), "[graphics]\nmax_performance=1\n");
		string log = Path.Combine(userData, "story.log");
		var startInfo = new ProcessStartInfo(exe, "--nologos --level ehz --lives 99")
		{
			UseShellExecute = false,
			Environment =
			{
				["S2HD_USER_DATA_DIR"] = userData,
				["S2HD_RANDOM_SEED"] = "1",
				["S2HD_TEST_STORY"] = "1",
				["S2HD_TEST_LOG"] = log,
			},
		};
		using (Process process = Process.Start(startInfo))
		{
			if (!process.WaitForExit(Timeout))
			{
				process.Kill(entireProcessTree: true);
			}
		}

		List<string> problems = FindProblems(File.Exists(log) ? File.ReadAllLines(log) : Array.Empty<string>());
		Assert.True(problems.Count == 0, $"The story test found problems (log and screenshots in {userData}):{Environment.NewLine}" + string.Join(Environment.NewLine, problems));
		Directory.Delete(userData, recursive: true);
	}

	/// <summary>Reads the log and gives a line for each problem.</summary>
	internal static List<string> FindProblems(IReadOnlyList<string> logLines)
	{
		List<string> problems = new List<string>();
		List<ActLog> acts = new List<ActLog>();
		bool finished = false;
		foreach (string line in logLines.Select(l => l.Substring(l.IndexOf(' ') + 1)))
		{
			Match start = Regex.Match(line, @"^start (\w+) (\d+) .*seamless=(\w+)");
			if (start.Success)
			{
				acts.Add(new ActLog($"{start.Groups[1].Value} {start.Groups[2].Value}", start.Groups[3].Value == "True", new List<string>()));
				continue;
			}
			if (line == "finished")
			{
				finished = true;
				continue;
			}
			acts.LastOrDefault()?.Lines.Add(line);
		}

		// The acts in order. An act that starts again (after a death) shows two times.
		string[] actOrder = acts.Select(a => a.Act).Where((act, i) => i == 0 || acts[i - 1].Act != act).ToArray();
		if (!actOrder.SequenceEqual(ExpectedActs))
		{
			problems.Add($"Acts: {string.Join(", ", actOrder)}. Expected: {string.Join(", ", ExpectedActs)}.");
		}
		for (int i = 1; i < acts.Count; i++)
		{
			if (acts[i].Act == acts[i - 1].Act)
			{
				problems.Add($"{acts[i].Act}: the act started again (a death or a fall out of the level).");
			}
		}
		if (!finished)
		{
			problems.Add("The story did not reach its end.");
		}
		foreach (ActLog act in acts)
		{
			problems.AddRange(act.Lines.Where(l => l.StartsWith("death ", StringComparison.Ordinal) || l.StartsWith("stuck ", StringComparison.Ordinal))
				.Select(l => $"{act.Act}: {l}"));
			if (AirStarts.Contains(act.Act))
			{
				continue;
			}
			string groundCheck = act.Lines.FirstOrDefault(l => l.StartsWith("state ", StringComparison.Ordinal) && l.Contains($" t={GroundCheckTick.ToString(CultureInfo.InvariantCulture)} "));
			if (groundCheck != null && groundCheck.Contains("air=True", StringComparison.Ordinal))
			{
				problems.Add($"{act.Act}: the player is not on the ground {GroundCheckTick} ticks after the start ({(act.Seamless ? "seamless" : "new")} start): {groundCheck}");
			}
		}
		return problems;
	}

	[Fact]
	public void FindProblems_ReportsDeathsAndMissingActs()
	{
		string[] log =
		{
			"1 start EHZ 1 x=384 y=2628 seamless=False",
			"2 state EHZ 1 t=420 x=384 y=2628 air=False dead=False",
			"3 death EHZ 1 t=900 x=500 y=3000",
			"4 start EHZ 1 x=384 y=2628 seamless=False",
			"5 start EHZ 2 x=44384 y=3012 seamless=True",
			"6 state EHZ 2 t=420 x=44384 y=3400 air=True dead=False",
		};

		List<string> problems = FindProblems(log);

		Assert.Contains(problems, p => p.StartsWith("Acts: EHZ 1, EHZ 2.", StringComparison.Ordinal));
		Assert.Contains(problems, p => p.Contains("death EHZ 1", StringComparison.Ordinal));
		Assert.Contains("EHZ 1: the act started again (a death or a fall out of the level).", problems);
		Assert.Contains(problems, p => p.StartsWith("EHZ 2: the player is not on the ground", StringComparison.Ordinal));
		Assert.Contains("The story did not reach its end.", problems);
	}
}
