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
	/// data contains complete. MTZ and DEZ are only stubs in the data (no markers and no objects), and the tests do
	/// not use the imports, so they are not tested here.
	/// </summary>
	[Theory]
	[Trait("Category", "Regression")]
	[InlineData("ehz", 1)]
	[InlineData("ehz", 2)]
	[InlineData("cpz", 1)]
	[InlineData("cpz", 2)]
	[InlineData("htz", 1)]
	[InlineData("htz", 2)]
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
			"28:mouse 1600 300;29:wheel -30;30:mouse 1600 453;31:down left;32:up left;40:mouse 700 560;41:down left;42:up left;" +
			"50:mouse 1700 725;51:down left;52:up left;53:down left;54:up left;" +
			"70:mouse 560 450;71:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10020, 10060, 300 }, Path.Combine(TestPaths.GoldenDirectory, "editor"), "editor",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// Opens the level editor in Emerald Hill with scripted input. It makes a new enemy object with the form of the
	/// Objects tool, adds it to the level, and plays from the mouse position. The files of the object must be in the
	/// mod folder for new objects.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorNewObject_MatchesGoldenScreenshots()
	{
		// "+ New object" is the first row of the list. Name, Kind and Create are rows 1, 3 and 6 of the properties.
		const string script = "5:mouse 1600 92;6:down left;7:up left;" +
			"10:mouse 1600 546;11:down left;12:up left;14:type Test walker;15:tap 40;" +
			"18:mouse 1600 606;19:down left;20:up left;21:down left;22:up left;23:down left;24:up left;" +
			"28:mouse 1600 696;29:down left;30:up left;" +
			"50:mouse 1100 520;51:down left;52:up left;" +
			"60:mouse 560 450;61:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10026, 10055, 300 }, Path.Combine(TestPaths.GoldenDirectory, "editor-new-object"), "editor-new-object",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData =>
			{
				string folder = Path.Combine(userData, "mods", SonicOrca.Core.Editor.NewObject.DefaultModName, "SONICORCA", "OBJECTS", "TESTWALKER");
				Assert.True(File.Exists(Path.Combine(folder, "_.object.lua")), "The editor did not make the script in " + folder);
				Assert.Contains("classification = \"badnik\"", File.ReadAllText(Path.Combine(folder, "_.object.lua")));
				Assert.True(File.Exists(Path.Combine(folder, "TEXTURE.png")), "The editor did not make the image in " + folder);
			});
	}

	/// <summary>
	/// Adds the moving platform of the example mod lua-item-block to Emerald Hill with the level editor, and plays from
	/// a position over it. Sonic lands on the platform, and the platform carries him to the right and back.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaMovingPlatform_MatchesGoldenScreenshots()
	{
		// LUAPLATFORM is three rows under LOCKY in the list of the objects tool
		const string script = "28:mouse 1600 300;29:wheel -30;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 430;46:down left;47:up left;" +
			"55:mouse 720 250;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10050, 230, 280, 330, 380 }, Path.Combine(TestPaths.GoldenDirectory, "lua-moving-platform"),
			"lua-moving-platform", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			prepareUserData: userData => CopyDirectory(Path.Combine(TestPaths.RepoRoot, "examples", "mods", "lua-item-block", "SONICORCA"),
				Path.Combine(userData, "mods", "lua-item-block", "SONICORCA")));
	}

	/// <summary>
	/// As <see cref="LuaMovingPlatform_MatchesGoldenScreenshots"/>, with the "fall" property of the platform on. The
	/// platform stops when Sonic lands on it, and falls with him after 30 ticks.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaFallingPlatform_MatchesGoldenScreenshots()
	{
		// "fall" is row 9 of the properties of the platform: after the heading, the state values are in name order
		const string script = "28:mouse 1600 300;29:wheel -30;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 200;46:down left;47:up left;" +
			"50:mouse 1600 786;51:down left;52:up left;" +
			"55:mouse 720 60;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10054, 140, 165, 190, 205, 220 }, Path.Combine(TestPaths.GoldenDirectory, "lua-falling-platform"),
			"lua-falling-platform", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			prepareUserData: userData => CopyDirectory(Path.Combine(TestPaths.RepoRoot, "examples", "mods", "lua-item-block", "SONICORCA"),
				Path.Combine(userData, "mods", "lua-item-block", "SONICORCA")));
	}

	private static void CopyDirectory(string source, string destination)
	{
		foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
		{
			string target = Path.Combine(destination, Path.GetRelativePath(source, file));
			Directory.CreateDirectory(Path.GetDirectoryName(target));
			File.Copy(file, target);
		}
	}

	/// <summary>
	/// Opens the animation editor on Sonic with scripted input. It selects the walk animation, clicks its third frame,
	/// types a delay of 9, adds a frame after it and saves. The saved file must have the changes.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void AnimationEditor_MatchesGoldenScreenshots()
	{
		// The walk animation is row 7 of the animations, frame 2 is the third box of the strip, and the delay of the
		// frame is row 14 of the properties. A click in the strip stops the playback.
		const string script = "5:mouse 1600 303;6:down left;7:up left;10:mouse 760 950;11:down left;12:up left;" +
			"14:mouse 1600 935;15:down left;16:up left;18:type 9;19:tap 40;22:tap 73;26:keydown 224;27:tap 22;28:keyup 224";
		RunAndCompare("--animations SONICORCA/OBJECTS/SONIC/ANIGROUP", new[] { 10040 }, Path.Combine(TestPaths.GoldenDirectory, "animation-editor"), "animation-editor",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData =>
			{
				string path = Path.Combine(userData, "mods", "editor", "animations", "SONICORCA", "OBJECTS", "SONIC", "ANIGROUP.anigroup.xml");
				Assert.True(File.Exists(path), "The animation editor did not save " + path);
				SonicOrca.Core.Editor.AnimationGroupDocument document = SonicOrca.Core.Editor.AnimationGroupDocument.Parse(File.ReadAllText(path));
				SonicOrca.Core.Editor.AnimationDocument walk = document.Animations[7];
				Assert.Equal("walk", walk.Name);
				Assert.Equal(17, walk.Frames.Count);
				Assert.Equal(9, walk.Frames[2].Delay);
				Assert.Equal(9, walk.Frames[3].Delay);
			});
	}

	/// <summary>
	/// Opens the animation editor on Sonic with scripted input. In the texture view, it drags the right edge of the
	/// rectangle of a frame 32 pixels to the right. In the preview, it drags the frame by (20, 10), and saves.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void AnimationEditorRectangles_MatchesGoldenScreenshots()
	{
		// T (23) changes the view. The texture view puts the selected rectangle in its centre (960, 461), so its right
		// edge is at x 1088. The position of the object in the preview is at (960, 502).
		const string script = "5:mouse 1600 303;6:down left;7:up left;10:mouse 760 950;11:down left;12:up left;14:tap 23;" +
			"18:mouse 1088 461;19:down left;21:mouse 1104 461;23:mouse 1120 461;25:up left;32:tap 23;" +
			"35:mouse 960 502;36:down left;38:mouse 970 507;40:mouse 980 512;42:up left;46:keydown 224;47:tap 22;48:keyup 224";
		RunAndCompare("--animations SONICORCA/OBJECTS/SONIC/ANIGROUP", new[] { 10030, 10060 }, Path.Combine(TestPaths.GoldenDirectory, "animation-editor-rectangles"),
			"animation-editor-rectangles", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData =>
			{
				string path = Path.Combine(userData, "mods", "editor", "animations", "SONICORCA", "OBJECTS", "SONIC", "ANIGROUP.anigroup.xml");
				Assert.True(File.Exists(path), "The animation editor did not save " + path);
				SonicOrca.Core.Editor.AnimationFrameDocument frame = SonicOrca.Core.Editor.AnimationGroupDocument.Parse(File.ReadAllText(path)).Animations[7].Frames[2];
				Assert.Equal((0, 1400, 288, 280), (frame.X, frame.Y, frame.Width, frame.Height));
				Assert.Equal((20, 6), (frame.OffsetX, frame.OffsetY));
			});
	}

	/// <summary>
	/// Opens the level editor in Emerald Hill with scripted input. It shows the hit boxes (H) and opens the animation
	/// editor on Sonic (N). In the preview, it drags all the frames of the animation 60 pixels up (Ctrl), goes back to
	/// the level (Escape) and plays (F5). The level must show Sonic above his hit box, without a save.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorAnimationPreview_MatchesGoldenScreenshots()
	{
		// H is scancode 11, N 17, Escape 41 and F5 62. The position of the object in the preview is at (960, 502).
		const string script = "5:tap 11;8:tap 17;" +
			"12:mouse 960 502;13:keydown 224;14:down left;16:mouse 960 472;18:mouse 960 442;20:up left;21:keyup 224;" +
			"26:tap 41;32:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10024, 10030, 300 }, Path.Combine(TestPaths.GoldenDirectory, "editor-animation-preview"),
			"editor-animation-preview", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData => Assert.False(Directory.Exists(Path.Combine(userData, "mods", "editor", "animations")), "The animation editor saved without Ctrl+S."));
	}

	/// <summary>
	/// Opens the animation editor on the ring with scripted input. It adds a PNG file as a new texture of the group and
	/// saves. The texture file must be in the folder of the editor, and the saved group must name it.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void AnimationEditorTextures_MatchesGoldenScreenshots()
	{
		string gameData = TestPaths.GameDataFile;
		if (gameData == null)
		{
			Assert.Skip("Game files not found. Set S2HD_GAME_DIR to the S2HD_2.0.1012-rc2 folder.");
		}
		// A PNG file to add: the texture of the ring from the game data
		string folder = Directory.CreateTempSubdirectory("s2hd-texture-").FullName;
		string sheet = Path.Combine(folder, "SHEET.png");
		using (Stream source = new SonicOrca.Resources.ResourceFile(gameData).Scan()["SONICORCA/OBJECTS/RING/TEXTURE"].Resource.Source.ReadUncompressed())
		using (FileStream file = File.Create(sheet))
		{
			source.CopyTo(file);
		}
		try
		{
			// The wheel scrolls the properties to their end, where "Add texture" is the last row
			string script = $"5:mouse 1600 900;6:wheel -30;8:mouse 1600 1029;9:down left;10:up left;12:type {sheet};13:tap 40;" +
				"16:keydown 224;17:tap 22;18:keyup 224";
			RunAndCompare("--animations SONICORCA/OBJECTS/RING/ANIGROUP", new[] { 10030 }, Path.Combine(TestPaths.GoldenDirectory, "animation-editor-textures"),
				"animation-editor-textures", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
				checkUserData: userData =>
				{
					string ring = Path.Combine(userData, "mods", "editor", "animations", "SONICORCA", "OBJECTS", "RING");
					Assert.True(File.Exists(Path.Combine(ring, "SHEET.png")), "The texture file is not in the folder of the editor.");
					string group = Path.Combine(ring, "ANIGROUP.anigroup.xml");
					Assert.True(File.Exists(group), "The animation editor did not save " + group);
					Assert.Equal(new[] { "//TEXTURE", "//SHEET" }, SonicOrca.Core.Editor.AnimationGroupDocument.Parse(File.ReadAllText(group)).Textures);
				});
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	/// <summary>
	/// Opens the markers tool of the level editor in Emerald Hill with scripted input. It moves the start position
	/// marker, adds an area marker and plays from the start.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorMarkers_MatchesGoldenScreenshots()
	{
		const string script = "5:tap 31;12:mouse 385 517;15:down left;18:mouse 450 517;22:mouse 641 517;25:up left;" +
			"32:mouse 1600 123;33:down left;34:up left;36:mouse 900 300;37:down left;38:up left;55:tap 59";
		RunAndCompare("--level ehz --editor", new[] { 10040, 300 }, Path.Combine(TestPaths.GoldenDirectory, "editor-markers"), "editor-markers",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// Opens the tiles and collision tools of the level editor in Emerald Hill with scripted input. It paints tiles from
	/// the palette, draws a collision line in the air and plays on it.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorTilesAndCollision_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 32;6:mouse 1555 771;7:down left;8:up left;" +
			"11:mouse 700 400;12:down left;14:mouse 800 400;16:mouse 900 400;18:up left;" +
			"22:tap 33;24:mouse 1600 93;25:down left;26:up left;28:mouse 300 300;29:down left;30:up left;" +
			"32:mouse 900 300;33:down left;34:up left;35:tap 41;45:mouse 600 150;46:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10020, 10040, 400 }, Path.Combine(TestPaths.GoldenDirectory, "editor-tiles"), "editor-tiles",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// Shows the results screen of the seventh special stage at once (S2HD_TEST_SPECIAL_STAGE_RESULTS), with all the
	/// rings and the last emerald. "PERFECT BONUS" replaces the ring rows, then the Super Sonic message shows. The
	/// screenshots are on odd frames, when the emeralds show.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void SpecialStageResults_MatchesGoldenScreenshots()
	{
		RunAndCompare("--halfpipe 7", new[] { 20375, 20751 }, Path.Combine(TestPaths.GoldenDirectory, "special-stage-results"), "special-stage-results",
			new Dictionary<string, string> { ["S2HD_TEST_SPECIAL_STAGE_RESULTS"] = "20,1,1", ["S2HD_TEST_EMERALDS"] = "6" }, withImportedMods: true);
	}

	/// <summary>
	/// Plays the first special stage alone with no input and the imported art. The messages use the letters of the
	/// original: "GET 30 RINGS!", its letters that fly away, "8 RINGS TO GO!" and "NOT ENOUGH RINGS...". The ring
	/// counter uses the big digits of the original.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void SpecialStageMessages_MatchesGoldenScreenshots()
	{
		RunAndCompare("--halfpipe 1 --sidekick none", new[] { 20380, 20520, 21350, 21650 }, Path.Combine(TestPaths.GoldenDirectory, "special-stage-messages"),
			"special-stage-messages", withImportedMods: true);
	}

	/// <summary>
	/// Plays the first special stage with no input, so that the camera, the sizes and the track stay as in the
	/// original. The special stage ticks are captured as 20000 plus the tick.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void SpecialStage_MatchesGoldenScreenshots()
	{
		RunAndCompare("--halfpipe 1", new[] { 20300, 20600, 20900 }, Path.Combine(TestPaths.GoldenDirectory, "special-stage"), "special-stage");
	}

	/// <summary>
	/// Starts each act of the zones that the import makes from the disassembly, with no input. Skipped when the
	/// disassembly is not found. The goldens are in tests/golden/levels-imported.
	/// </summary>
	[Theory]
	[Trait("Category", "Regression")]
	[InlineData("arz", 1)]
	[InlineData("arz", 2)]
	[InlineData("cnz", 1)]
	[InlineData("cnz", 2)]
	[InlineData("mcz", 1)]
	[InlineData("mcz", 2)]
	[InlineData("ooz", 1)]
	[InlineData("ooz", 2)]
	[InlineData("mtz", 1)]
	[InlineData("mtz", 2)]
	[InlineData("mtz", 3)]
	[InlineData("scz", 1)]
	[InlineData("wfz", 1)]
	[InlineData("dez", 1)]
	public void ImportedLevelStart_MatchesGoldenScreenshots(string level, int act)
	{
		ImportedMods.Get();
		string name = $"{level}-{act}";
		RunAndCompare($"--level {level} --act {act}", LevelTicks, Path.Combine(TestPaths.GoldenDirectory, "levels-imported", name), name, withImportedMods: true);
	}

	/// <param name="checkUserData">Checks the user data folder after the game ends, for example a file that the game saved.</param>
	/// <param name="prepareUserData">Changes the user data folder before the game starts, for example to add a mod.</param>
	private static void RunAndCompare(string arguments, int[] ticks, string goldenDirectory, string name, IReadOnlyDictionary<string, string> environment = null,
		bool withImportedMods = false, Action<string> checkUserData = null, Action<string> prepareUserData = null)
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
		if (withImportedMods)
		{
			ImportedMods.CopyTo(userData);
		}
		try
		{
			prepareUserData?.Invoke(userData);
			RunGame(exe, arguments, ticks, userData, environment);
			checkUserData?.Invoke(userData);
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
