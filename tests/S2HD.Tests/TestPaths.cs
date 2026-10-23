using System;
using System.IO;

namespace S2HD.Tests;

/// <summary>Locations the tests use. Environment variables override the defaults.</summary>
internal static class TestPaths
{
	public static string RepoRoot { get; } = FindRepoRoot();

	/// <summary>
	/// The game's sonicorca.dat: S2HD_GAME_DIR, then data/ in the checkout, then the default download folder
	/// that the build also uses. Null when none of them has the file.
	/// </summary>
	public static string GameDataFile
	{
		get
		{
			string gameDir = Environment.GetEnvironmentVariable("S2HD_GAME_DIR");
			string[] candidates =
			{
				string.IsNullOrEmpty(gameDir) ? null : Path.Combine(gameDir, "data", "sonicorca.dat"),
				Path.Combine(RepoRoot, "data", "sonicorca.dat"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Downloads", "S2HD_2.0.1012-rc2", "S2HD_2.0.1012-rc2", "data", "sonicorca.dat"),
			};
			foreach (string candidate in candidates)
			{
				if (candidate != null && File.Exists(candidate))
				{
					return candidate;
				}
			}
			return null;
		}
	}

	/// <summary>The built game: S2HD_EXE, or the release build for this platform.</summary>
	public static string GameExecutable
	{
		get
		{
			string exe = Environment.GetEnvironmentVariable("S2HD_EXE");
			if (!string.IsNullOrEmpty(exe))
			{
				return exe;
			}
			return OperatingSystem.IsMacOS()
				? Path.Combine(RepoRoot, "publish", "S2HD.app", "Contents", "MacOS", "S2HD")
				: Path.Combine(RepoRoot, "publish", "S2HD-win-x64", "S2HD.exe");
		}
	}

	/// <summary>Golden screenshots. Git ignores this folder, because the images come from the game files.</summary>
	public static string GoldenDirectory => Path.Combine(RepoRoot, "tests", "golden");

	private static string FindRepoRoot()
	{
		for (DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "S2HD.sln")))
			{
				return directory.FullName;
			}
		}
		throw new InvalidOperationException("Cannot find S2HD.sln above " + AppContext.BaseDirectory);
	}
}
