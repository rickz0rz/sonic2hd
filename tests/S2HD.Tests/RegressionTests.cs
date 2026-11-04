using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Hjg.Pngcs;
using SonicOrca.Resources;
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
public sealed partial class RegressionTests
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
		// LUAPLATFORM is six rows under LOCKY in the list of the objects tool. The list scrolls three more rows for it.
		const string script = "28:mouse 1600 300;29:wheel -33;30:mouse 1600 483;31:down left;32:up left;" +
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
		const string script = "28:mouse 1600 300;29:wheel -33;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 200;46:down left;47:up left;" +
			"50:mouse 1600 786;51:down left;52:up left;" +
			"55:mouse 720 60;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10054, 140, 165, 190, 205, 220 }, Path.Combine(TestPaths.GoldenDirectory, "lua-falling-platform"),
			"lua-falling-platform", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			prepareUserData: userData => CopyDirectory(Path.Combine(TestPaths.RepoRoot, "examples", "mods", "lua-item-block", "SONICORCA"),
				Path.Combine(userData, "mods", "lua-item-block", "SONICORCA")));
	}

	/// <summary>
	/// Adds the zipline handle of the example mod lua-item-block to Emerald Hill with the level editor, and plays from
	/// a position over it. Sonic falls onto the handle and hangs on it. The handle carries him to the right and lets
	/// go at the end of its line.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaZiplineHandle_MatchesGoldenScreenshots()
	{
		// LUAHANDLE is four rows under LOCKY in the list of the objects tool. The list scrolls one more row for it.
		const string script = "28:mouse 1600 300;29:wheel -31;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 260;46:down left;47:up left;" +
			"55:mouse 700 120;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10050, 170, 210, 250, 300, 340 }, Path.Combine(TestPaths.GoldenDirectory, "lua-zipline-handle"),
			"lua-zipline-handle", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			prepareUserData: userData => CopyDirectory(Path.Combine(TestPaths.RepoRoot, "examples", "mods", "lua-item-block", "SONICORCA"),
				Path.Combine(userData, "mods", "lua-item-block", "SONICORCA")));
	}

	/// <summary>
	/// Adds the goal of the example mod lua-item-block to Emerald Hill with the level editor, and plays from a position
	/// over it. Sonic falls onto the goal, and the act ends with the score count.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaGoal_MatchesGoldenScreenshots()
	{
		// LUAGOAL is three rows under LOCKY in the list of the objects tool
		const string script = "28:mouse 1600 300;29:wheel -30;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 570;46:down left;47:up left;" +
			"55:mouse 700 300;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10050, 230, 330, 450, 700, 900 }, Path.Combine(TestPaths.GoldenDirectory, "lua-goal"),
			"lua-goal", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script }, prepareUserData: CopyExampleMod);
	}

	/// <summary>
	/// Adds the water switch of the example mod lua-item-block to Emerald Hill with the level editor. The switch gives
	/// the level water under the ground. Sonic falls onto the switch, and the water rises while the ground shakes.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaWaterSwitch_MatchesGoldenScreenshots()
	{
		// LUAWATERSWITCH is seven rows under LOCKY in the list of the objects tool. The list scrolls four more rows for it.
		const string script = "28:mouse 1600 300;29:wheel -34;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 570;46:down left;47:up left;" +
			"55:mouse 700 300;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10050, 230, 300, 380, 460 }, Path.Combine(TestPaths.GoldenDirectory, "lua-water-switch"),
			"lua-water-switch", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script }, prepareUserData: CopyExampleMod);
	}

	/// <summary>
	/// Adds the wheel of the example mod lua-item-block to Emerald Hill with the level editor. The wheel draws a rope, a
	/// shadow and its image with a rotation and a scale. Sonic falls through it, and it flashes.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaWheel_MatchesGoldenScreenshots()
	{
		// LUAWHEEL is eight rows under LOCKY in the list of the objects tool. The list scrolls five more rows for it.
		const string script = "28:mouse 1600 300;29:wheel -35;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 700 420;46:down left;47:up left;" +
			"55:mouse 700 200;56:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10050, 150, 185, 215 }, Path.Combine(TestPaths.GoldenDirectory, "lua-wheel"),
			"lua-wheel", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script }, prepareUserData: CopyExampleMod);
	}

	/// <summary>
	/// Fights the wisp of the example mod lua-item-block, a Lua boss, in Emerald Hill. The first run places the wisp
	/// with the level editor, sets its health to 2, and saves. The second run plays the level with recorded input:
	/// Sonic holds right and jumps again and again. The camera locks, the wisp comes over Sonic, and Sonic hits it two
	/// times. The wisp explodes and flies away, and the act ends.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaBoss_MatchesGoldenScreenshots()
	{
		// LUAWISP is nine rows under LOCKY in the list of the objects tool. The list scrolls six more rows for it.
		// The wisp floats high, so that only a jump reaches it. "health" is the first state value in the properties.
		// Ctrl+S saves (224 is Ctrl, 22 is S).
		const string editorScript = "28:mouse 1600 300;29:wheel -36;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 1100 240;46:down left;47:up left;" +
			"50:mouse 1600 756;51:down left;52:up left;54:type 2;55:tap 40;" +
			"60:keydown 224;61:tap 22;62:keyup 224";
		string goldenDirectory = Path.Combine(TestPaths.GoldenDirectory, "lua-boss");
		int[] ticks = { 300, 350, 450, 800 };
		RunTwoRuns("lua-boss", goldenDirectory, ticks, userData =>
		{
			CopyExampleMod(userData);
			RunGame(TestPaths.GameExecutable, "--level ehz --editor", new[] { 10066 }, userData,
				new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = editorScript });
			Assert.True(File.Exists(Path.Combine(userData, "mods", "editor", "EHZ.dat")), "The editor did not save the wisp.");
			string recording = Path.Combine(userData, "boss.rec");
			// Sonic holds right and jumps again and again, with full jumps. A jump makes him hit the wisp, not get hurt.
			File.WriteAllBytes(recording, MakeRecording(1000, tick => (1.0, tick % 40 < 25)));
			return $"--level ehz --playback \"{recording}\"";
		});
	}

	/// <summary>
	/// The double jump of the example mod lua-item-block, a Lua ability. The first run places the feather on the start
	/// position with the level editor and saves. The second run plays Sonic alone: he gets the feather, jumps, and
	/// presses jump again in the air. With one jump he lands before tick 290. With the double jump he is still high in
	/// the air at tick 290.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaDoubleJump_MatchesGoldenScreenshots()
	{
		// LUAFEATHER is two rows under LOCKY in the list of the objects tool. The list scrolls one row less for it.
		const string editorScript = "28:mouse 1600 300;29:wheel -29;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 400 520;46:down left;47:up left;" +
			"60:keydown 224;61:tap 22;62:keyup 224";
		RunTwoRuns("lua-double-jump", Path.Combine(TestPaths.GoldenDirectory, "lua-double-jump"), new[] { 248, 262, 290 }, userData =>
		{
			CopyExampleMod(userData);
			RunGame(TestPaths.GameExecutable, "--level ehz --editor", new[] { 10066 }, userData,
				new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = editorScript });
			string recording = Path.Combine(userData, "double-jump.rec");
			// A jump at tick 220, a release at 244, and a second jump in the air at 246
			File.WriteAllBytes(recording, MakeRecording(400, tick => (0.0, (tick >= 220 && tick < 244) || (tick >= 246 && tick < 290))));
			return $"--level ehz --protagonist sonic --sidekick none --playback \"{recording}\"";
		});
	}

	/// <summary>
	/// The list of the objects tool does not show Lua abilities, because an item gives an ability to a character. With
	/// the example mod, the row under LUACOIN is LUAFEATHER, not LUADOUBLEJUMP.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorLuaAbility_IsNotInTheList()
	{
		const string script = "28:mouse 1600 300;29:wheel -30";
		RunAndCompare("--level ehz --editor", new[] { 10050 }, Path.Combine(TestPaths.GoldenDirectory, "editor-lua-ability"), "editor-lua-ability",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script }, prepareUserData: CopyExampleMod);
	}

	/// <summary>
	/// Makes a power-up with the form of the objects tool, places it on the start position, and saves. Then Sonic alone
	/// gets it and jumps. The ability of the power-up makes the jump higher. A normal full jump from tick 220 lands at
	/// about tick 281, the higher jump at about tick 294. So Sonic must still be in the air at tick 288.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorNewPowerUp_MatchesGoldenScreenshots()
	{
		// "+ New object" is the first row of the list. Name, Kind and Create are rows 1, 3 and 6 of the properties.
		// Kind goes from scenery to power-up in six clicks.
		string kind = string.Concat(Enumerable.Range(0, 6).Select(i => $"{19 + i * 2}:down left;{20 + i * 2}:up left;"));
		string editorScript = "5:mouse 1600 92;6:down left;7:up left;" +
			"10:mouse 1600 546;11:down left;12:up left;14:type Spring shoes;15:tap 40;" +
			"18:mouse 1600 606;" + kind +
			"34:mouse 1600 696;35:down left;36:up left;" +
			"55:mouse 400 520;56:down left;57:up left;" +
			"65:keydown 224;66:tap 22;67:keyup 224";
		RunTwoRuns("editor-new-power-up", Path.Combine(TestPaths.GoldenDirectory, "editor-new-power-up"), new[] { 250, 288 }, userData =>
		{
			RunGame(TestPaths.GameExecutable, "--level ehz --editor", new[] { 10070 }, userData,
				new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = editorScript });
			string objects = Path.Combine(userData, "mods", SonicOrca.Core.Editor.NewObject.DefaultModName, "SONICORCA", "OBJECTS");
			Assert.True(File.Exists(Path.Combine(objects, "SPRINGSHOES", "_.object.lua")), "The editor did not make the power-up.");
			Assert.True(File.Exists(Path.Combine(objects, "SPRINGSHOESABILITY.object.lua")), "The editor did not make the ability.");
			Assert.True(File.Exists(Path.Combine(userData, "mods", "editor", "EHZ.dat")), "The editor did not save the level.");
			string recording = Path.Combine(userData, "jump.rec");
			File.WriteAllBytes(recording, MakeRecording(400, tick => (0.0, tick >= 220 && tick < 270)));
			return $"--level ehz --protagonist sonic --sidekick none --playback \"{recording}\"";
		});
	}

	/// <summary>
	/// A character loses its abilities when it gets hurt. The first run places the feather on the start position and
	/// makes a walking enemy with the form of the objects tool, to the right of the start, and saves. In the second run,
	/// Sonic alone gets the feather, stands, and the enemy hurts him before tick 200 (he has 10 rings, so he does not
	/// die). Then he jumps at tick 360 and presses jump again in the air at tick 386, as in the double jump test. Without
	/// the double jump, he is on the ground again at tick 425. With it, he would still be in the air.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaAbility_IsLostOnAHit()
	{
		// LUAFEATHER is two rows under LOCKY in the list of the objects tool. Then the list scrolls back to the top for
		// "+ New object". Kind goes from scenery to enemy in three clicks.
		string kind = string.Concat(Enumerable.Range(0, 3).Select(i => $"{79 + i * 2}:down left;{80 + i * 2}:up left;"));
		string editorScript = "28:mouse 1600 300;29:wheel -29;30:mouse 1600 483;31:down left;32:up left;" +
			"45:mouse 400 520;46:down left;47:up left;" +
			"60:mouse 1600 300;61:wheel 60;65:mouse 1600 92;66:down left;67:up left;" +
			"70:mouse 1600 546;71:down left;72:up left;74:type Test walker;75:tap 40;78:mouse 1600 606;" + kind +
			"90:mouse 1600 696;91:down left;92:up left;" +
			"110:mouse 640 520;111:down left;112:up left;" +
			"120:keydown 224;121:tap 22;122:keyup 224";
		RunTwoRuns("lua-ability-lost", Path.Combine(TestPaths.GoldenDirectory, "lua-ability-lost"), new[] { 200, 388, 425 }, userData =>
		{
			CopyExampleMod(userData);
			RunGame(TestPaths.GameExecutable, "--level ehz --editor", new[] { 10125 }, userData,
				new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = editorScript });
			string recording = Path.Combine(userData, "hit.rec");
			File.WriteAllBytes(recording, MakeRecording(600, tick => (0.0, (tick >= 360 && tick < 384) || (tick >= 386 && tick < 430))));
			return $"--level ehz --protagonist sonic --sidekick none --playback \"{recording}\"";
		}, new Dictionary<string, string> { ["S2HD_TEST_START_RINGS"] = "10" });
	}

	/// <summary>
	/// Co-op: player 2 controls Tails. Sonic gets no input and stands at the start. A recording gives the input of
	/// player 2 (S2HD_TEST_PLAYER2_PLAYBACK, because a test cannot press keys): right from tick 200, and a jump at tick
	/// 260. Tails runs away from Sonic, jumps, and collects rings for both. A Tails of the computer stays near Sonic.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void CoopPlayer2_MatchesGoldenScreenshots()
	{
		RunTwoRuns("coop-player2", Path.Combine(TestPaths.GoldenDirectory, "coop-player2"), new[] { 270, 320 }, userData =>
		{
			File.WriteAllBytes(Path.Combine(userData, "player2.rec"), MakeRecording(600, tick => (tick >= 200 && tick < 400 ? 1.0 : 0.0, tick >= 260 && tick < 285)));
			return "--level ehz";
		}, new Dictionary<string, string> { ["S2HD_TEST_PLAYER2_PLAYBACK"] = Path.Combine("{userData}", "player2.rec") });
	}

	/// <summary>
	/// Makes an input recording: one record of 40 bytes for each level tick, with the horizontal direction and the
	/// action button. The other fields are 0.
	/// </summary>
	private static byte[] MakeRecording(int tickCount, Func<int, (double ThrottleX, bool Action)> input)
	{
		using MemoryStream stream = new MemoryStream();
		using BinaryWriter writer = new BinaryWriter(stream);
		for (int tick = 0; tick < tickCount; tick++)
		{
			(double throttleX, bool action) = input(tick);
			writer.Write(throttleX);
			writer.Write(0.0);
			writer.Write(action);
			writer.Write(false);
			writer.Write(0);
			writer.Write(0);
			writer.Write((short)0);
			writer.Write(0);
			writer.Write((short)0);
			writer.Write((short)0);
			writer.Write(0f);
		}
		writer.Flush();
		return stream.ToArray();
	}

	/// <summary>
	/// Runs a test that needs a preparation run, for example a run of the editor that saves a level. The preparation
	/// gives the arguments of the run that the test compares with the golden screenshots.
	/// </summary>
	/// <param name="environment">
	/// More environment variables for the run that the test compares, or null. "{userData}" in a value is the user data
	/// folder of the test.
	/// </param>
	/// <param name="checkUserData">Checks the user data folder after the run, for example the log, or null.</param>
	private static void RunTwoRuns(string name, string goldenDirectory, int[] ticks, Func<string, string> prepare,
		IReadOnlyDictionary<string, string> environment = null, Action<string> checkUserData = null)
	{
		if (!File.Exists(TestPaths.GameExecutable))
		{
			Assert.Skip($"Game not built: {TestPaths.GameExecutable}.");
		}
		bool record = Environment.GetEnvironmentVariable("S2HD_RECORD_GOLDEN") == "1";
		if (!record && !ticks.All(tick => File.Exists(GoldenPath(goldenDirectory, tick))))
		{
			Assert.Skip($"No golden screenshots in {goldenDirectory}. Record them with S2HD_RECORD_GOLDEN=1.");
		}
		string userData = Directory.CreateTempSubdirectory("s2hd-regression-").FullName;
		try
		{
			string arguments = prepare(userData);
			RunGame(TestPaths.GameExecutable, arguments, ticks, userData,
				environment?.ToDictionary(item => item.Key, item => item.Value.Replace("{userData}", userData)));
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

	/// <summary>
	/// Plays Sunny Meadow, the level of the example mod sunny-meadow, which has a level script. The script makes the
	/// water of the pits go up and down. The captures show the water low, high and low again. Sonic walks to the right
	/// for a short time, so that the walking beetle near the start does not hurt him.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void LuaLevelScript_MatchesGoldenScreenshots()
	{
		RunTwoRuns("lua-level-script", Path.Combine(TestPaths.GoldenDirectory, "lua-level-script"), new[] { 200, 600, 800 }, userData =>
		{
			CopyExampleMod(userData);
			CopyDirectory(Path.Combine(TestPaths.RepoRoot, "examples", "mods", "sunny-meadow", "SONICORCA"), Path.Combine(userData, "mods", "sunny-meadow", "SONICORCA"));
			string recording = Path.Combine(userData, "meadow.rec");
			File.WriteAllBytes(recording, MakeRecording(900, tick => (tick >= 200 && tick < 330 ? 1.0 : 0.0, false)));
			return $"--level meadow --playback \"{recording}\"";
		});
	}

	private static void CopyExampleMod(string userData)
	{
		CopyDirectory(Path.Combine(TestPaths.RepoRoot, "examples", "mods", "lua-item-block", "SONICORCA"), Path.Combine(userData, "mods", "lua-item-block", "SONICORCA"));
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
	/// Opens the animation editor on the image SONICORCA/HUD/LIFE/SONIC, which has no animation group. A click on
	/// "Make a group" makes SONICORCA/HUD/LIFE/ANIGROUP, and Ctrl+S saves it. The group must have the image and the
	/// other image of the folder, TAILS, as its textures, and one frame that covers the image.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void AnimationEditorNewGroup_MatchesGoldenScreenshots()
	{
		// "Make a group" is row 4 of the properties of an image. 224 is Ctrl, 22 is S.
		const string script = "5:mouse 1700 604;6:down left;7:up left;12:keydown 224;13:tap 22;14:keyup 224";
		RunAndCompare("--animations SONICORCA/HUD/LIFE/SONIC", new[] { 10020 }, Path.Combine(TestPaths.GoldenDirectory, "animation-editor-new-group"),
			"animation-editor-new-group", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData =>
			{
				string path = Path.Combine(userData, "mods", "editor", "animations", "SONICORCA", "HUD", "LIFE", "ANIGROUP.anigroup.xml");
				Assert.True(File.Exists(path), "The animation editor did not save " + path);
				SonicOrca.Core.Editor.AnimationGroupDocument document = SonicOrca.Core.Editor.AnimationGroupDocument.Parse(File.ReadAllText(path));
				Assert.Equal(new[] { "//SONIC", "//TAILS" }, document.Textures);
				SonicOrca.Core.Editor.AnimationFrameDocument frame = Assert.Single(Assert.Single(document.Animations).Frames);
				Assert.Equal((0, 0, 68, 68), (frame.X, frame.Y, frame.Width, frame.Height));
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
		const string script = "3:tap 32;6:mouse 1555 921;7:down left;8:up left;" +
			"11:mouse 700 400;12:down left;14:mouse 800 400;16:mouse 900 400;18:up left;" +
			"22:tap 33;24:mouse 1600 93;25:down left;26:up left;28:mouse 300 300;29:down left;30:up left;" +
			"32:mouse 900 300;33:down left;34:up left;35:tap 41;45:mouse 600 150;46:tap 62";
		RunAndCompare("--level ehz --editor", new[] { 10020, 10040, 400 }, Path.Combine(TestPaths.GoldenDirectory, "editor-tiles"), "editor-tiles",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The eye of a layer in the list of the tiles tool hides the layer in the editor. In Emerald Hill, a click on the
	/// eye of layer 1 (background_sky) hides the sky, and its eye shows closed.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorLayerEye_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 32;6:mouse 1478 123;7:down left;8:up left;10:mouse 600 300";
		RunAndCompare("--level ehz --editor", new[] { 10012 }, Path.Combine(TestPaths.GoldenDirectory, "editor-layer-eye"), "editor-layer-eye",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// With all the layers in view, the tiles tool outlines the tiles of a layer with parallax where the game draws them,
	/// and the label at the mouse names the cell of the layer there. In Chemical Plant, layer 5 (BG - Front) shows with
	/// all the layers, then alone with its parallax (a right click on its eye), so that the outlines frame its tiles.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorParallaxOutline_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 32;6:mouse 1600 243;7:down left;8:up left;10:mouse 400 400;" +
			"20:mouse 730 73;21:down left;22:up left;24:mouse 900 600;" +
			"36:mouse 1478 243;37:down right;38:up right;40:mouse 900 600";
		RunAndCompare("--level cpz --editor", new[] { 10030, 10050 }, Path.Combine(TestPaths.GoldenDirectory, "editor-parallax-outline"), "editor-parallax-outline",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The objects tool draws the paths of a selected spin tube in Chemical Plant and edits their points. The box of the
	/// tube is at its entrance, with the tube cap. A click there shows a menu of the two objects, and a click on its row
	/// selects the tube.
	/// The test then drags point 1 of Path0, adds a point with the + of the first segment, turns on its spin sound, and
	/// holds the mouse over the Path0 row for its tooltip.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorSpinTubePath_MatchesGoldenScreenshots()
	{
		const string script = "4:mouse 1000 450;5:down left;6:up left;8:mouse 1050 503;10:down left;11:up left;13:mouse 700 300;" +
			"14:mouse 1034 876;15:down left;17:mouse 1070 876;19:mouse 1100 876;20:up left;" +
			"24:mouse 1067 684;25:down left;26:up left;" +
			"30:mouse 1600 845;31:down left;32:up left;34:mouse 1600 725";
		RunAndCompare("--level cpz --editor --startpos 8700 3700", new[] { 10009, 10022, 10036 }, Path.Combine(TestPaths.GoldenDirectory, "editor-spin-tube-path"),
			"editor-spin-tube-path", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The mini-map in the bottom right corner shows the whole level: the tiles in white, the objects as dots, and the
	/// view as a yellow box. In Emerald Hill, a click in the middle of the map moves the view there, and a drag moves it on.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorMiniMap_MatchesGoldenScreenshots()
	{
		const string script = "4:mouse 600 400;8:mouse 1100 1005;9:down left;11:mouse 1150 1005;13:up left;15:mouse 600 400";
		RunAndCompare("--level ehz --editor", new[] { 10006, 10016 }, Path.Combine(TestPaths.GoldenDirectory, "editor-mini-map"), "editor-mini-map",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// "--level jungle --editor" with a level that does not exist opens the form of a new level with the name "jungle".
	/// A click on "Create" makes the level and opens it in the editor. A new level has no level script.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorNewLevelFromCommandLine_MatchesGoldenScreenshots()
	{
		const string script = "5:mouse 1700 300;8:mouse 1700 665;9:down left;10:up left;12:mouse 600 400";
		RunAndCompare("--level jungle --editor", new[] { 10006, 10060 }, Path.Combine(TestPaths.GoldenDirectory, "editor-new-level-command-line"),
			"editor-new-level-command-line", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData => Assert.True(File.Exists(Path.Combine(userData, "mods", "jungle", "SONICORCA", "LEVELS", "JUNGLE", "MAP.map.xml"))));
	}

	/// <summary>
	/// The form of a new level takes a tile image. The image has 4 x 2 cells of 64 x 64 pixels, and one cell is fully
	/// transparent, so the new level has a tile set of its own with 7 tiles. The tiles tool shows them in its palette.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorNewLevelTileImage_MatchesGoldenScreenshots()
	{
		string image = Path.Combine(Path.GetTempPath(), "s2hd-new-level-tiles.png");
		File.WriteAllBytes(image, NewLevelTests.MakePng(256, 128, (x, y) => x / 64 == 2 && y / 64 == 1 ? (byte)0 : (byte)255));
		try
		{
			string script = $"5:mouse 1700 635;6:down left;7:up left;8:type {image};10:tap 40;11:mouse 600 400;" +
				"14:mouse 1700 665;15:down left;16:up left;60:tap 32;65:mouse 600 400";
			RunAndCompare("--level jungle --editor", new[] { 10012, 10080 }, Path.Combine(TestPaths.GoldenDirectory, "editor-new-level-tile-image"),
				"editor-new-level-tile-image", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
				checkUserData: userData =>
				{
					string tileSet = Path.Combine(userData, "mods", "jungle", "SONICORCA", "LEVELS", "JUNGLE", "TILESET");
					Assert.True(File.Exists(Path.Combine(tileSet, "TEXTURE.png")));
					Assert.Equal(7, System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(Path.Combine(tileSet, "_.tileset.xml")), "<tile ").Count);
				});
		}
		finally
		{
			File.Delete(image);
		}
	}

	/// <summary>
	/// "--tiles" gives the tile image of the form of a new level: "--level jungle --editor --tiles image.png" needs only a
	/// click on "Create". The level has the 7 tiles of the image.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorNewLevelTilesOption_MatchesGoldenScreenshots()
	{
		string image = Path.Combine(Path.GetTempPath(), "s2hd-new-level-tiles-option.png");
		File.WriteAllBytes(image, NewLevelTests.MakePng(256, 128, (x, y) => x / 64 == 2 && y / 64 == 1 ? (byte)0 : (byte)255));
		try
		{
			const string script = "5:mouse 1700 300;8:mouse 1700 665;9:down left;10:up left;50:tap 32;55:mouse 600 400";
			RunAndCompare($"--level jungle --editor --tiles {image}", new[] { 10006, 10070 }, Path.Combine(TestPaths.GoldenDirectory, "editor-new-level-tiles-option"),
				"editor-new-level-tiles-option", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
				checkUserData: userData => Assert.Equal(7, System.Text.RegularExpressions.Regex.Matches(
					File.ReadAllText(Path.Combine(userData, "mods", "jungle", "SONICORCA", "LEVELS", "JUNGLE", "TILESET", "_.tileset.xml")), "<tile ").Count));
		}
		finally
		{
			File.Delete(image);
		}
	}

	/// <summary>
	/// The tile set tool of a new level from a tile image of 4 x 2 cells, with an empty cell at 2, 1. The test selects
	/// tile 1, drags it to the empty cell, adds a tile at the cell that tile 1 left, and saves. The saved tile set has
	/// tile 1 at its new cell and the new tile 8.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorTileSet_MatchesGoldenScreenshots()
	{
		string image = Path.Combine(Path.GetTempPath(), "s2hd-tile-set-tool.png");
		File.WriteAllBytes(image, NewLevelTests.MakePng(256, 128, (x, y) => x / 64 == 2 && y / 64 == 1 ? (byte)0 : (byte)(x + y)));
		try
		{
			const string script = "5:mouse 1700 300;8:mouse 1700 665;9:down left;10:up left;50:tap 35;55:mouse 42 90;56:down left;57:up left;60:mouse 700 600;" +
				"65:mouse 42 90;66:down left;68:mouse 100 120;70:mouse 170 154;72:up left;75:mouse 42 90;76:down left;77:up left;80:mouse 700 600;" +
				"85:keydown 224;86:tap 22;87:keyup 224";
			RunAndCompare($"--level jungle --editor --tiles {image}", new[] { 10062, 10082, 10335 }, Path.Combine(TestPaths.GoldenDirectory, "editor-tile-set"),
				"editor-tile-set", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
				checkUserData: userData =>
				{
					Resource resource = new ResourceFile(Path.Combine(userData, "mods", "editor", "JUNGLE.dat")).Scan()
						.GetResourceListing()["SONICORCA/LEVELS/JUNGLE/TILESET"];
					using Stream stream = resource.Source.ReadUncompressed();
					XElement[] tiles = XDocument.Load(stream).Root.Element("tiles").Elements("tile").ToArray();
					Assert.Equal(8, tiles.Length);
					XElement moved = tiles.Single(t => (string)t.Attribute("id") == "1");
					Assert.Equal(("128", "64"), ((string)moved.Attribute("x"), (string)moved.Attribute("y")));
					XElement added = tiles.Single(t => (string)t.Attribute("id") == "8");
					Assert.Equal(("0", "0"), ((string)added.Attribute("x"), (string)added.Attribute("y")));
				});
		}
		finally
		{
			File.Delete(image);
		}
	}

	/// <summary>
	/// The Size row of the tiles tool changes the size of a layer. A new level from a tile image has a background of
	/// 32 x 24 tiles. The test makes it 40 x 24, paints a row of tiles on it, and saves. The saved map has the new size.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorLayerSize_MatchesGoldenScreenshots()
	{
		string image = Path.Combine(Path.GetTempPath(), "s2hd-layer-size-tiles.png");
		File.WriteAllBytes(image, NewLevelTests.MakePng(256, 128, (_, _) => 255));
		try
		{
			const string script = "5:mouse 1700 665;6:down left;7:up left;50:tap 32;55:mouse 1600 93;56:down left;57:up left;" +
				"60:mouse 1700 785;61:down left;62:up left;63:type 40 x 24;65:tap 40;68:mouse 1555 865;69:down left;70:up left;" +
				"74:mouse 600 400;75:down left;77:mouse 800 400;79:mouse 1000 400;80:up left;82:mouse 600 600;90:keydown 224;91:tap 22;92:keyup 224";
			RunAndCompare($"--level jungle --editor --tiles {image}", new[] { 10085, 10340 }, Path.Combine(TestPaths.GoldenDirectory, "editor-layer-size"),
				"editor-layer-size", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
				checkUserData: userData => Assert.True(File.Exists(Path.Combine(userData, "mods", "editor", "JUNGLE.dat"))));
		}
		finally
		{
			File.Delete(image);
		}
	}

	/// <summary>
	/// The tiles tool outlines the current layer in magenta, with its name and its size. In a new level, the view
	/// zooms out, so that the outline shows the left and the bottom edges of the front layer (128 x 24 tiles).
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorLayerBounds_MatchesGoldenScreenshots()
	{
		const string script = "5:mouse 1700 665;6:down left;7:up left;50:tap 32;55:mouse 600 400;58:tap 45;59:tap 45;62:mouse 600 400";
		RunAndCompare("--level jungle --editor", new[] { 10066 }, Path.Combine(TestPaths.GoldenDirectory, "editor-layer-bounds"), "editor-layer-bounds",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// A background of 1 x 1 tile that repeats to the left and the right. After the change of its size, the view shows
	/// the top left of the layer. A click on a copy of the cell paints the cell, and the cell and its copies show the tile.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorPaintWrappedLayer_MatchesGoldenScreenshots()
	{
		string image = Path.Combine(Path.GetTempPath(), "s2hd-wrapped-layer-tiles.png");
		File.WriteAllBytes(image, NewLevelTests.MakePng(256, 128, (_, _) => 255));
		try
		{
			const string script = "5:mouse 1700 665;6:down left;7:up left;50:tap 32;55:mouse 1600 93;56:down left;57:up left;" +
				"60:mouse 1700 785;61:down left;62:up left;63:type 1 x 1;65:tap 40;68:mouse 1663 865;69:down left;70:up left;" +
				"74:mouse 600 90;75:down left;76:up left;80:mouse 900 700";
			RunAndCompare($"--level jungle --editor --tiles {image}", new[] { 10085 }, Path.Combine(TestPaths.GoldenDirectory, "editor-paint-wrapped-layer"),
				"editor-paint-wrapped-layer", new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
		}
		finally
		{
			File.Delete(image);
		}
	}

	/// <summary>
	/// The tiles tool moves a layer. In a new level, "Move forward" puts the background (layer 0) in front of the solid
	/// front layer, and the message says that the background now covers the level.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorMoveLayer_MatchesGoldenScreenshots()
	{
		const string script = "5:mouse 1700 665;6:down left;7:up left;50:tap 32;55:mouse 1600 93;56:down left;57:up left;" +
			"60:mouse 1600 243;61:down left;62:up left;64:mouse 600 400";
		RunAndCompare("--level jungle --editor", new[] { 10066 }, Path.Combine(TestPaths.GoldenDirectory, "editor-move-layer"), "editor-move-layer",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The Name row of the tiles tool renames a layer. In a new level, the front layer becomes "ground", moves to the
	/// back, and the level is saved. The saved map has the new name and the new order, and its collision paths follow.
	/// The last capture comes after the save message, which has the path of the temporary user data folder.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorRenameLayer_MatchesGoldenScreenshots()
	{
		const string script = "5:mouse 1700 665;6:down left;7:up left;50:tap 32;55:mouse 1700 815;56:down left;57:up left;58:type ground;60:tap 40;" +
			"62:mouse 1600 213;63:down left;64:up left;66:mouse 600 400;68:keydown 224;69:tap 22;70:keyup 224";
		RunAndCompare("--level jungle --editor", new[] { 10067, 10320 }, Path.Combine(TestPaths.GoldenDirectory, "editor-rename-layer"), "editor-rename-layer",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script },
			checkUserData: userData => Assert.True(File.Exists(Path.Combine(userData, "mods", "editor", "JUNGLE.dat"))));
	}

	/// <summary>
	/// In Emerald Hill, the tiles tool clicks between layers. background_far shows alone and the view scrolls to the right.
	/// background_near shows at the same place, because a part of it is in view. fg_main shows the view before the
	/// backgrounds, and background_far then shows again where it was.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorLayerFlip_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 32;6:mouse 1600 183;7:down left;8:up left;10:mouse 700 500;14:hwheel 10;" +
			"20:mouse 1600 213;21:down left;22:up left;24:mouse 700 500;" +
			"30:mouse 1600 333;31:down left;32:up left;34:mouse 700 500;" +
			"40:mouse 1600 183;41:down left;42:up left;44:mouse 700 500";
		RunAndCompare("--level ehz --editor", new[] { 10016, 10028, 10038, 10048 }, Path.Combine(TestPaths.GoldenDirectory, "editor-layer-flip"), "editor-layer-flip",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The start of the player is its start marker. In Emerald Hill, the markers tool drags startpos_stk_1 to the right:
	/// the large cross of the start moves with it during the drag, and the objects tool shows the start at its new place.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorStartMarker_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 31;5:mouse 384 532;6:down left;8:mouse 500 532;10:mouse 640 532;14:up left;16:tap 30;18:mouse 900 300";
		RunAndCompare("--level ehz --editor", new[] { 10012, 10022 }, Path.Combine(TestPaths.GoldenDirectory, "editor-start-marker"), "editor-start-marker",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The collision tool shows a row for each flag of a line. In Emerald Hill, a click on the "Flags" row of a floor
	/// line shows the flags, a click turns on "Force roll", and the mouse over "Prevent jump" shows what it does.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorCollisionFlags_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 33;6:mouse 700 620;7:down left;8:up left;10:mouse 700 300;" +
			"14:mouse 1600 695;15:down left;16:up left;18:mouse 1600 815;19:down left;20:up left;22:mouse 1600 845";
		RunAndCompare("--level ehz --editor", new[] { 10030 }, Path.Combine(TestPaths.GoldenDirectory, "editor-collision-flags"), "editor-collision-flags",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// A trackpad scrolls the view of the editor left and right. In Emerald Hill, the view moves right and then up.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorTrackpadScroll_MatchesGoldenScreenshots()
	{
		const string script = "5:mouse 600 300;6:hwheel 10;8:hwheel 5;10:wheel 3";
		RunAndCompare("--level ehz --editor", new[] { 10012 }, Path.Combine(TestPaths.GoldenDirectory, "editor-trackpad-scroll"), "editor-trackpad-scroll",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// A background layer shows alone while the tiles tool paints on it. In Chemical Plant, the first 14 rows of the
	/// list are background layers, so the list marks them with "parallax" and shows a scroll bar. A banner on the view
	/// tells that the layer shows alone, and a click on it shows all the layers. The "View" row then shows the layer
	/// alone again.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorLayerAloneView_MatchesGoldenScreenshots()
	{
		const string script = "3:tap 32;6:mouse 1600 123;7:down left;8:up left;10:mouse 600 300;" +
			"20:mouse 730 73;21:down left;22:up left;24:mouse 600 300;" +
			"32:mouse 1700 755;33:down left;34:up left;36:mouse 600 300";
		RunAndCompare("--level cpz --editor", new[] { 10012, 10030, 10045 }, Path.Combine(TestPaths.GoldenDirectory, "editor-layer-alone"), "editor-layer-alone",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script });
	}

	/// <summary>
	/// The tiles tool shows the animation of a tile. In Aquatic Ruin, the palette shows only the animated tiles, and the
	/// brush takes the waterfall tile 2078, which the import animates with the palette cycle of the original. The panel
	/// shows its frames, the status line shows where they come from, and the palette marks each animated tile. A click
	/// on the "Animation" row opens the animation of the tile (animation 29 of the ANIGROUP of the tile set) in the
	/// animation editor.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void EditorTileAnimation_MatchesGoldenScreenshots()
	{
		// The "Show" row shows only the animated tiles. The tile is then at index 30: row 3, column 6.
		// A click on the "Animation" row then opens the animation of the tile in the animation editor
		const string script = "3:tap 32;6:mouse 1700 668;7:down left;8:up left;10:mouse 1826 1031;12:down left;13:up left;" +
			"20:mouse 1700 698;21:down left;22:up left";
		RunAndCompare("--level arz --editor", new[] { 10016, 10030, 10060 }, Path.Combine(TestPaths.GoldenDirectory, "editor-tile-animation"), "editor-tile-animation",
			new Dictionary<string, string> { ["S2HD_TEST_EDITOR_INPUT"] = script }, withImportedMods: true);
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
	/// Starts an HD zone with the imported mods. The mod tile-animations moves the frames of the animated tiles into
	/// an ANIGROUP, so the tiles must look the same as without the mods: the test uses the goldens of
	/// <see cref="LevelStart_MatchesGoldenScreenshots"/>. Chemical Plant has fades and additive tiles.
	/// </summary>
	[Theory]
	[Trait("Category", "Regression")]
	[InlineData("ehz", 1)]
	[InlineData("cpz", 1)]
	[InlineData("htz", 1)]
	public void ConvertedTileAnimations_MatchTheGoldenScreenshots(string level, int act)
	{
		ImportedMods.Get();
		string name = $"{level}-{act}";
		RunAndCompare($"--level {level} --act {act}", LevelTicks, Path.Combine(TestPaths.GoldenDirectory, "levels", name), name, withImportedMods: true);
	}

	/// <summary>
	/// An imported object with one frame and the colours of a palette cycle changes colour. The big block of CNZ 1 at
	/// 7552, 6656 shows the next step of PalCycle_CNZ 8 ticks later.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void ImportedObjectPaletteCycle_MatchesGoldenScreenshots()
	{
		ImportedMods.Get();
		RunAndCompare("--level cnz --startpos 7552 6500", new[] { 200, 208 }, Path.Combine(TestPaths.GoldenDirectory, "imported-object-palette-cycle"),
			"imported-object-palette-cycle", withImportedMods: true);
	}

	/// <summary>
	/// The object shader changes the key colours of imported sprites to the colours of the palette cycles of the
	/// original. The bumpers at the start of CNZ 1 show another step of PalCycle_CNZ 8 ticks later.
	/// </summary>
	[Fact]
	[Trait("Category", "Regression")]
	public void ImportedObjectColourSwaps_MatchesGoldenScreenshots()
	{
		ImportedMods.Get();
		RunAndCompare("--level cnz --startpos 416 1500", new[] { 200, 208 }, Path.Combine(TestPaths.GoldenDirectory, "imported-object-colour-swaps"),
			"imported-object-colour-swaps", withImportedMods: true);
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
