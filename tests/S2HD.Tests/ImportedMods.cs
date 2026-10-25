using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// The mods of all the zones that the import makes from the Sonic 2 disassembly, in a temporary folder. The tests
/// make them once, with tools/import-all.sh, the first time that a test needs them. A test that needs them is
/// skipped when the disassembly is not found.
/// </summary>
internal static class ImportedMods
{
	private static readonly Lazy<string> Folder = new Lazy<string>(Import);

	/// <summary>Gives the folder of the imported mods, or skips the test.</summary>
	public static string Get()
	{
		SpecialStageImportTests.FindDisassembly();
		return Folder.Value;
	}

	/// <summary>Copies the imported mods into the mods folder of a user data folder.</summary>
	public static void CopyTo(string userData)
	{
		string mods = Path.Combine(userData, "mods");
		Directory.CreateDirectory(mods);
		foreach (string file in Directory.GetFiles(Get(), "*.dat"))
		{
			File.Copy(file, Path.Combine(mods, Path.GetFileName(file)), overwrite: true);
		}
	}

	private static string Import()
	{
		string folder = Directory.CreateTempSubdirectory("s2hd-imported-mods-").FullName;
		var startInfo = new ProcessStartInfo("/bin/bash", new[] { Path.Combine(TestPaths.RepoRoot, "tools", "import-all.sh"), SpecialStageImportTests.FindDisassembly(), folder })
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		using Process process = Process.Start(startInfo);
		// Read the two streams at the same time, so that a full stream does not stop the import.
		System.Threading.Tasks.Task<string> errors = process.StandardError.ReadToEndAsync();
		string output = process.StandardOutput.ReadToEnd() + errors.Result;
		process.WaitForExit();
		Assert.True(process.ExitCode == 0, "The import failed:" + Environment.NewLine + output);
		return folder;
	}
}
