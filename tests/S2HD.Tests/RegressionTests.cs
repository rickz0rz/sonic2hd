using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hjg.Pngcs;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Runs the built game, saves screenshots at fixed level ticks and compares them with golden screenshots from a
/// known-good build. With a fixed random seed the game is deterministic, so the screenshots match exactly on the same
/// machine. A small tolerance allows for GPU driver differences.
/// </summary>
/// <remarks>
/// To record golden screenshots, build a known-good version of the game, then run the tests with
/// S2HD_RECORD_GOLDEN=1. Screenshots that do not match are copied to tests/golden/actual.
/// </remarks>
public sealed class RegressionTests
{
	private static readonly int[] DemoTicks = { 60, 300, 600, 1200, 1800 };

	/// <summary>Tick 120 is after the title card starts, tick 480 is after the player gets control.</summary>
	private static readonly int[] LevelTicks = { 120, 480 };

	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

	// A pixel is different when a channel differs by more than this, and an image fails when more than
	// MaxDifferentPixelRatio of its pixels are different
	private const int ChannelTolerance = 8;

	private const double MaxDifferentPixelRatio = 0.001;

	/// <summary>Plays the Emerald Hill attract demo, which moves the player through the first part of the level.</summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void AttractDemo_MatchesGoldenScreenshots()
	{
		RunAndCompare("--demo", DemoTicks, TestPaths.GoldenDirectory, "demo");
	}

	/// <summary>
	/// Starts a level with no input, so the player stands at the start position. These are the levels that the demo
	/// data contains. The other levels do not load, because their music is not in the data.
	/// </summary>
	[Theory]
	[Trait("Category", "Regression")]
	[InlineData("ehz", 1)]
	[InlineData("ehz", 2)]
	[InlineData("cpz", 1)]
	[InlineData("cpz", 2)]
	[InlineData("htz", 1)]
	[InlineData("htz", 2)]
	[InlineData("mtz", 1)]
	[InlineData("mtz", 2)]
	[InlineData("dez", 1)]
	[InlineData("dez", 2)]
	public void LevelStart_MatchesGoldenScreenshots(string level, int act)
	{
		string name = $"{level}-{act}";
		RunAndCompare($"--level {level} --act {act}", LevelTicks, Path.Combine(TestPaths.GoldenDirectory, "levels", name), name);
	}

	/// <summary>
	/// Opens the level editor in Emerald Hill with scripted input: it selects a ring, adds a monitor with rings, and
	/// plays from the mouse position. The editor frames are captured as 10000 plus the frame.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void Editor_MatchesGoldenScreenshots()
	{
		const string script = "5:mouse 1167 215;12:down left;13:up left;" +
			"30:mouse 1600 423;31:down left;32:up left;40:mouse 700 560;41:down left;42:up left;" +
			"50:mouse 1700 725;51:down left;52:up left;53:down left;54:up left;" +
			"70:mouse 560 450;71:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10020, 10060, 300 }, Path.Combine(TestPaths.GoldenDirectory, "editor"), "editor",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	private static void RunAndCompare(string arguments, int[] ticks, string goldenDirectory, string name, IReadOnlyDictionary<string, string> environment = null)
	{
		string exe = TestPaths.GameExecutable;
		if (!File.Exists(exe))
		{
			Assert.Skip($"Game not built: {exe}. Build it as the README describes, or set S2HD_EXE.");
		}
		bool record = Environment.GetEnvironmentVariable("S2HD_RECORD_GOLDEN") == "1";
		if (!record && !ticks.All(tick => File.Exists(GoldenPath(goldenDirectory, tick))))
		{
			Assert.Skip($"No golden screenshots in {goldenDirectory}. Record them with S2HD_RECORD_GOLDEN=1.");
		}

		string userData = Directory.CreateTempSubdirectory("s2hd-regression-").FullName;
		try
		{
			RunGame(exe, arguments, ticks, userData, environment);
			if (record)
			{
				Directory.CreateDirectory(goldenDirectory);
				foreach (int tick in ticks)
				{
					File.Copy(CapturePath(userData, tick), GoldenPath(goldenDirectory, tick), overwrite: true);
				}
				return;
			}
			string[] failures = ticks
				.Select(tick => CompareWithGolden(CapturePath(userData, tick), GoldenPath(goldenDirectory, tick), name))
				.Where(failure => failure != null)
				.ToArray();
			Assert.True(failures.Length == 0, string.Join(Environment.NewLine, failures));
		}
		finally
		{
			Directory.Delete(userData, recursive: true);
		}
	}

	private static void RunGame(string exe, string arguments, int[] ticks, string userData, IReadOnlyDictionary<string, string> environment)
	{
		// No frame limiter, so the game runs as fast as the machine allows
		File.WriteAllText(Path.Combine(userData, "sonicorca.cfg"), "[graphics]\nmax_performance=1\n");
		var startInfo = new ProcessStartInfo(exe, arguments)
		{
			UseShellExecute = false,
			Environment =
			{
				["S2HD_USER_DATA_DIR"] = userData,
				["S2HD_RANDOM_SEED"] = "1",
				["S2HD_CAPTURE_LEVEL_TICKS"] = string.Join(",", ticks),
				["S2HD_EXIT_AFTER_CAPTURES"] = "1",
			},
		};
		foreach (KeyValuePair<string, string> variable in environment ?? new Dictionary<string, string>())
		{
			startInfo.Environment[variable.Key] = variable.Value;
		}
		using Process process = Process.Start(startInfo);
		if (!process.WaitForExit(Timeout))
		{
			process.Kill(entireProcessTree: true);
			Assert.Fail($"The game did not finish in {Timeout.TotalMinutes} minutes ({arguments}). See {userData}/sonicorca.log.");
		}
		foreach (int tick in ticks)
		{
			Assert.True(File.Exists(CapturePath(userData, tick)),
				$"No screenshot for level tick {tick} ({arguments}, game exit code {process.ExitCode}).");
		}
	}

	/// <summary>Returns null when the images match, or a description of the difference.</summary>
	private static string CompareWithGolden(string actualPath, string goldenPath, string name)
	{
		(int width, int height, byte[] pixels) actual = ReadRgb(actualPath);
		(int width, int height, byte[] pixels) golden = ReadRgb(goldenPath);
		string label = $"{name}/{Path.GetFileName(actualPath)}";
		if (actual.width != golden.width || actual.height != golden.height)
		{
			return KeepActual(actualPath, name, $"{label}: size {actual.width}x{actual.height}, expected {golden.width}x{golden.height}");
		}
		int differentPixels = 0;
		for (int i = 0; i < actual.pixels.Length; i += 3)
		{
			if (Math.Abs(actual.pixels[i] - golden.pixels[i]) > ChannelTolerance
				|| Math.Abs(actual.pixels[i + 1] - golden.pixels[i + 1]) > ChannelTolerance
				|| Math.Abs(actual.pixels[i + 2] - golden.pixels[i + 2]) > ChannelTolerance)
			{
				differentPixels++;
			}
		}
		double ratio = differentPixels / (double)(actual.width * actual.height);
		return ratio <= MaxDifferentPixelRatio
			? null
			: KeepActual(actualPath, name, $"{label}: {differentPixels} pixels differ ({ratio:P2})");
	}

	private static string KeepActual(string actualPath, string name, string message)
	{
		string directory = Path.Combine(TestPaths.GoldenDirectory, "actual", name);
		Directory.CreateDirectory(directory);
		string copy = Path.Combine(directory, Path.GetFileName(actualPath));
		File.Copy(actualPath, copy, overwrite: true);
		return $"{message}. Actual screenshot: {copy}";
	}

	private static (int width, int height, byte[] pixels) ReadRgb(string path)
	{
		using FileStream stream = File.OpenRead(path);
		var reader = new PngReader(stream);
		try
		{
			int width = reader.ImgInfo.Cols;
			int height = reader.ImgInfo.Rows;
			int channels = reader.ImgInfo.Channels;
			byte[] rgb = new byte[width * height * 3];
			for (int row = 0; row < height; row++)
			{
				byte[] line = reader.ReadRowByte(row).ScanlineB;
				for (int x = 0; x < width; x++)
				{
					Buffer.BlockCopy(line, x * channels, rgb, (row * width + x) * 3, 3);
				}
			}
			return (width, height, rgb);
		}
		finally
		{
			reader.End();
		}
	}

	private static string CapturePath(string userData, int tick) => Path.Combine(userData, $"level-tick-{tick:D5}.png");

	private static string GoldenPath(string goldenDirectory, int tick) => Path.Combine(goldenDirectory, $"level-tick-{tick:D5}.png");
}
