using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using SonicOrca;
using SonicOrca.Graphics;
using SonicOrca.Web;

namespace S2HD;

/// <summary>
/// Web entry point, standing in for the desktop Program (same members used by the game code).
/// The page writes the game data into the in-memory file system, calls <see cref="Start"/>, then calls
/// <see cref="Frame"/> once per game frame from requestAnimationFrame.
/// </summary>
public static partial class Program
{
	private const string ContentPath = "/content";

	private static S2HDSonicOrcaGameContext _game;

	private static WebAudioContext _audio;

	public static Version AppVersion = typeof(Program).Assembly.GetName().Version;

	/// <summary>The short commit SHA of the build (see CommitSha.targets), or null when the build had no git.</summary>
	public static readonly string CommitSha = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
		.FirstOrDefault(attribute => attribute.Key == "CommitSha")?.Value;

	public static string AppArchitecture = "wasm";

	public static Version AppMinOpenGLVersion = new Version(3, 3);

	public static IReadOnlyList<string> CommandLineArguments { get; private set; } = Array.Empty<string>();

	public static IniConfiguration Configuration { get; private set; }

	public static string UserDataDirectory => "/userdata";

	public static void Main()
	{
		Trace.AutoFlush = true;
		Trace.Listeners.Add(new ConsoleTraceListener());
		Trace.WriteLine($"SonicOrca {AppVersion} {CommitSha} [web {AppArchitecture}], .NET {Environment.Version}");
	}

	public static void ShowErrorMessageBox(string text)
	{
		Console.Error.WriteLine(text);
		Host.Alert(text);
	}

	/// <summary>
	/// Extracts data/*.dat and credits.film from an S2HD release zip already written to the file system.
	/// Returns the number of files extracted.
	/// </summary>
	[JSExport]
	public static int ImportZip(string zipPath)
	{
		int count = 0;
		using (ZipArchive zip = ZipFile.OpenRead(zipPath))
		{
			foreach (ZipArchiveEntry entry in zip.Entries)
			{
				string name = entry.FullName.Replace('\\', '/');
				string destination = null;
				string folder = GetDatFolder(name);
				if (folder != null)
					destination = Path.Combine(ContentPath, folder, entry.Name);
				else if (entry.Name.Equals("credits.film", StringComparison.OrdinalIgnoreCase))
					destination = Path.Combine(ContentPath, entry.Name);
				if (destination == null)
					continue;
				Directory.CreateDirectory(Path.GetDirectoryName(destination));
				entry.ExtractToFile(destination, overwrite: true);
				count++;
			}
		}
		File.Delete(zipPath);
		return count;
	}

	/// <summary>"data" or "mods" for a .dat file in one of those folders (at any depth in the zip), else null.</summary>
	private static string GetDatFolder(string path)
	{
		if (!path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
			return null;
		foreach (string folder in new[] { "data", "mods" })
		{
			if (path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase) || path.Contains("/" + folder + "/", StringComparison.OrdinalIgnoreCase))
				return folder;
		}
		return null;
	}

	/// <summary>
	/// Initialises the game. Returns an error message, or null on success. The arguments are the options of the
	/// desktop game, separated by spaces, for example "--halfpipe 1" (from ?args= of the page). With profile, the
	/// frame profiler writes its summary to the console, as S2HD_PROFILE=1 does on the desktop.
	/// </summary>
	[JSExport]
	public static string Start(string arguments, bool profile)
	{
		try
		{
			CommandLineArguments = (arguments ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (profile)
			{
				Environment.SetEnvironmentVariable("S2HD_PROFILE", "1");
			}
			Directory.CreateDirectory(UserDataDirectory);
			ContentDirectory.Override(ContentPath);
			if (!Directory.Exists(Path.Combine(ContentPath, "data")) || Directory.GetFiles(Path.Combine(ContentPath, "data"), "*.dat").Length == 0)
				return "No game data found. Choose the S2HD 2.0.1012-rc2 zip or its extracted folder.";
			ExtractShaders();
			Configuration = new IniConfiguration();
			Configuration.SetProperty("audio", "volume", "1.0");
			Configuration.SetProperty("audio", "music_volume", "0.5");
			Configuration.SetProperty("audio", "sound_volume", "1.0");
			var platform = new WebPlatform();
			platform.Initialise();
			_audio = platform.WebAudio;
			Trace.WriteLine($"Graphics: {platform.GraphicsAPI} ({platform.GraphicsVendor})");
			_game = new S2HDSonicOrcaGameContext(platform) { ExternalPacing = true };
			_game.Initialise();
			_game.ResetCatchup();
			return null;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
			return ex.Message;
		}
	}

	/// <summary>Runs one game update and draw. Returns an error message, or null to keep going.</summary>
	[JSExport]
	public static string Frame()
	{
		try
		{
			_game.Step();
			return _game.Finish ? "finished" : null;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
			return ex.Message;
		}
	}

	/// <summary>
	/// Mixes <paramref name="frames"/> frames of audio at 44.1 kHz and passes them to the page's queueAudio, or
	/// discards them when <paramref name="output"/> is false (before the browser allows audio).
	/// </summary>
	[JSExport]
	public static void MixAudio(int frames, bool output)
	{
		try
		{
			_audio?.Mix(frames, output);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
		}
	}

	/// <summary>
	/// Diagnostic: compiles every shader with WebGL and returns one line per failure (empty if all compile).
	/// Includes are only compiled as part of the shaders that use them.
	/// </summary>
	[JSExport]
	public static string CompileAllShaders()
	{
		ContentDirectory.Override(ContentPath);
		ExtractShaders();
		var context = new WebWindowContext().GraphicsContext;
		var failures = new List<string>();
		int count = 0;
		foreach (string file in Directory.GetFiles(Path.Combine(ContentPath, "shaders"), "*.shader", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(ContentPath, file).Replace('\\', '/');
			if (relative.StartsWith("shaders/include/"))
				continue;
			count++;
			try
			{
				OrcaShader.CreateFromFile(context, relative).Dispose();
			}
			catch (Exception ex)
			{
				failures.Add($"{relative}: {(ex.InnerException ?? ex).Message.Replace('\n', ' ')}");
			}
		}
		return $"{count - failures.Count}/{count} shaders compiled" + (failures.Count > 0 ? "\n" + string.Join("\n", failures) : "");
	}

	private static void ExtractShaders()
	{
		Assembly assembly = typeof(Program).Assembly;
		foreach (string name in assembly.GetManifestResourceNames())
		{
			if (!name.StartsWith("shaders/"))
				continue;
			// Resource names built on Windows carry '\' from MSBuild's RecursiveDir
			string path = Path.Combine(ContentPath, name.Replace('\\', '/'));
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using Stream source = assembly.GetManifestResourceStream(name);
			using FileStream destination = File.Create(path);
			source.CopyTo(destination);
		}
	}

	private static partial class Host
	{
		[JSImport("alert", "host")] public static partial void Alert(string text);
	}
}
