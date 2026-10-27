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
using SonicOrca.HelperLibraries.Png;
using SonicOrca.Original;
using SonicOrca.Resources;
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

	/// <summary>The folder for the files of the Sonic 2 disassembly during an import.</summary>
	private const string DisassemblyPath = "/s2disasm";

	/// <summary>The folder for the resource files of the mods during an import.</summary>
	private const string ImportPath = "/tmp/imports";

	private static string _disassembly;

	private static IReadOnlyList<OriginalImportStep> _importSteps;

	/// <summary>The resources of the game data, which the packed mods are compared with.</summary>
	private static ResourceTree _baseTree;

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

	/// <summary>The version of the import. The page makes its stored mods again when the version changes.</summary>
	[JSExport]
	public static int GetImportVersion() => OriginalImport.Version;

	/// <summary>
	/// Returns the names of the mods that the import from the disassembly makes and that the game data does not have,
	/// separated by spaces. The page writes the mods into /content/mods/NAME.dat.
	/// </summary>
	[JSExport]
	public static string GetMissingMods()
	{
		return string.Join(" ", ImportModNames().Where(mod => !File.Exists(GetModPath(mod))));
	}

	/// <summary>Returns the names of all the mods that the import from the disassembly makes, separated by spaces.</summary>
	[JSExport]
	public static string GetImportMods() => string.Join(" ", ImportModNames());

	/// <summary>
	/// Extracts the zip of the Sonic 2 disassembly (for example a GitHub download of sonicretro/s2disasm), which the
	/// page wrote to the file system, and prepares the steps that make the missing mods. Returns the number of steps,
	/// or -1 when the zip does not contain the disassembly. The game data must be in place first.
	/// </summary>
	[JSExport]
	public static int PrepareImport(string zipPath)
	{
		if (Directory.Exists(DisassemblyPath))
			Directory.Delete(DisassemblyPath, recursive: true);
		ZipFile.ExtractToDirectory(zipPath, DisassemblyPath);
		File.Delete(zipPath);
		_disassembly = OriginalImport.FindDisassembly(DisassemblyPath);
		if (_disassembly == null)
		{
			FinishImport();
			return -1;
		}
		ResourceType.RegisterTypesFrom(typeof(PngResourceType).Assembly);
		_baseTree = new ResourceFile(Path.Combine(ContentPath, "data", "sonicorca.dat")).Scan();
		HashSet<string> missing = new HashSet<string>(GetMissingMods().Split(' ', StringSplitOptions.RemoveEmptyEntries));
		// Without ffmpeg, the music is WAVE samples at half the sample rate
		_importSteps = OriginalImport.GetSteps(null, line => Trace.WriteLine(line)).Where(step => missing.Contains(step.Mod)).ToArray();
		return _importSteps.Count;
	}

	/// <summary>Returns the text for the progress of an import step.</summary>
	[JSExport]
	public static string GetImportStepDescription(int index) => _importSteps[index].Description;

	/// <summary>
	/// Runs one step of the import. After the last step of a mod, packs the mod into /content/mods. Returns an error
	/// message, or null on success.
	/// </summary>
	[JSExport]
	public static string RunImportStep(int index)
	{
		try
		{
			OriginalImportStep step = _importSteps[index];
			string folder = Path.Combine(ImportPath, step.Mod);
			step.Run(_disassembly, folder);
			if (index == _importSteps.Count - 1 || _importSteps[index + 1].Mod != step.Mod)
			{
				Directory.CreateDirectory(Path.Combine(ContentPath, "mods"));
				int count = OriginalImport.Pack(folder, GetModPath(step.Mod), _baseTree);
				Trace.WriteLine($"Packed {count} resources into {step.Mod}.dat");
				Directory.Delete(folder, recursive: true);
			}
			return null;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex);
			return ex.Message;
		}
	}

	/// <summary>Deletes the files of the disassembly and of the import, to free the memory.</summary>
	[JSExport]
	public static void FinishImport()
	{
		foreach (string folder in new[] { DisassemblyPath, ImportPath })
		{
			if (Directory.Exists(folder))
				Directory.Delete(folder, recursive: true);
		}
		_disassembly = null;
		_importSteps = null;
		_baseTree = null;
	}

	/// <summary>The names of the mods of a full import, in order.</summary>
	private static IEnumerable<string> ImportModNames() => OriginalImport.GetSteps(null, _ => { }).Select(step => step.Mod).Distinct();

	private static string GetModPath(string mod) => Path.Combine(ContentPath, "mods", mod + ".dat");

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
