using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hjg.Pngcs;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Plays the Emerald Hill attract demo in the built game, saves screenshots at fixed level ticks and compares them
/// with golden screenshots from a known-good build. The demo is deterministic with a fixed random seed, so the
/// screenshots match exactly on the same machine. A small tolerance allows for GPU driver differences.
/// </summary>
/// <remarks>
/// To record golden screenshots, build a known-good version of the game, then run the tests with
/// S2HD_RECORD_GOLDEN=1. Screenshots that do not match are copied to tests/golden/actual.
/// </remarks>
public sealed class RegressionTests
{
	private static readonly int[] CaptureTicks = { 60, 300, 600, 1200, 1800 };

	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);

	// A pixel is different when a channel differs by more than this, and an image fails when more than
	// MaxDifferentPixelRatio of its pixels are different
	private const int ChannelTolerance = 8;

	private const double MaxDifferentPixelRatio = 0.001;

	[Fact]
	[Trait("Category", "Regression")]
	public void AttractDemo_MatchesGoldenScreenshots()
	{
		string exe = TestPaths.GameExecutable;
		if (!File.Exists(exe))
		{
			Assert.Skip($"Game not built: {exe}. Build it as the README describes, or set S2HD_EXE.");
		}
		bool record = Environment.GetEnvironmentVariable("S2HD_RECORD_GOLDEN") == "1";
		if (!record && !CaptureTicks.All(tick => File.Exists(GoldenPath(tick))))
		{
			Assert.Skip($"No golden screenshots in {TestPaths.GoldenDirectory}. Record them with S2HD_RECORD_GOLDEN=1.");
		}

		string userData = Directory.CreateTempSubdirectory("s2hd-regression-").FullName;
		try
		{
			RunDemo(exe, userData);
			if (record)
			{
				Directory.CreateDirectory(TestPaths.GoldenDirectory);
				foreach (int tick in CaptureTicks)
				{
					File.Copy(CapturePath(userData, tick), GoldenPath(tick), overwrite: true);
				}
				return;
			}
			string[] failures = CaptureTicks
				.Select(tick => CompareWithGolden(CapturePath(userData, tick), GoldenPath(tick)))
				.Where(failure => failure != null)
				.ToArray();
			Assert.True(failures.Length == 0, string.Join(Environment.NewLine, failures));
		}
		finally
		{
			Directory.Delete(userData, recursive: true);
		}
	}

	private static void RunDemo(string exe, string userData)
	{
		// No frame limiter, so the demo runs as fast as the machine allows
		File.WriteAllText(Path.Combine(userData, "sonicorca.cfg"), "[graphics]\nmax_performance=1\n");
		var startInfo = new ProcessStartInfo(exe, "--demo")
		{
			UseShellExecute = false,
			Environment =
			{
				["S2HD_USER_DATA_DIR"] = userData,
				["S2HD_RANDOM_SEED"] = "1",
				["S2HD_CAPTURE_LEVEL_TICKS"] = string.Join(",", CaptureTicks),
				["S2HD_EXIT_AFTER_CAPTURES"] = "1",
			},
		};
		using Process process = Process.Start(startInfo);
		if (!process.WaitForExit(Timeout))
		{
			process.Kill(entireProcessTree: true);
			Assert.Fail($"The game did not finish the demo in {Timeout.TotalMinutes} minutes. See {userData}/sonicorca.log.");
		}
		foreach (int tick in CaptureTicks)
		{
			Assert.True(File.Exists(CapturePath(userData, tick)),
				$"No screenshot for level tick {tick} (game exit code {process.ExitCode}).");
		}
	}

	/// <summary>Returns null when the images match, or a description of the difference.</summary>
	private static string CompareWithGolden(string actualPath, string goldenPath)
	{
		(int width, int height, byte[] pixels) actual = ReadRgb(actualPath);
		(int width, int height, byte[] pixels) golden = ReadRgb(goldenPath);
		string name = Path.GetFileName(actualPath);
		if (actual.width != golden.width || actual.height != golden.height)
		{
			return KeepActual(actualPath, $"{name}: size {actual.width}x{actual.height}, expected {golden.width}x{golden.height}");
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
			: KeepActual(actualPath, $"{name}: {differentPixels} pixels differ ({ratio:P2})");
	}

	private static string KeepActual(string actualPath, string message)
	{
		string directory = Path.Combine(TestPaths.GoldenDirectory, "actual");
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

	private static string GoldenPath(int tick) => Path.Combine(TestPaths.GoldenDirectory, $"level-tick-{tick:D5}.png");
}
