using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using SDL2;
using SonicOrca;
using SonicOrca.SDL2;

namespace S2HD;

/// <summary>
/// The entry point of the desktop game. It sets up the log and the configuration, and then runs the game.
/// </summary>
public static class Program
{
	public const string BuildConfigurationName = "RELEASE";
	public static Version AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
	public static string AppArchitecture = Environment.Is64BitProcess ? "x64" : "x86";
	public static Version AppMinOpenGLVersion = new Version(3, 3);
	private static string IniConfigurationPath = "sonicorca.cfg";

	/// <summary>Brings a window to the front. Windows only.</summary>
	[DllImport("user32.dll")]
	private static extern int SetForegroundWindow(IntPtr hWnd);

	public static IniConfiguration Configuration { get; private set; }

	public static string UserDataDirectory
	{
		get
		{
			// Tests point this at a scratch directory so they do not touch the player's settings
			string overridePath = Environment.GetEnvironmentVariable("S2HD_USER_DATA_DIR");
			if (!string.IsNullOrEmpty(overridePath))
				return overridePath;
			// ~/Library/Application Support on macOS; Documents on Windows (unchanged from the original)
			Environment.SpecialFolder folder = OperatingSystem.IsMacOS() ? Environment.SpecialFolder.LocalApplicationData : Environment.SpecialFolder.Personal;
			return Path.Combine(Environment.GetFolderPath(folder), "SonicOrca");
		}
	}

	private static string LogPath => Path.Combine(UserDataDirectory, "sonicorca.log");

	public static IReadOnlyList<string> CommandLineArguments { get; private set; }

	[STAThread]
	private static void Main(string[] args)
	{
		Program.CommandLineArguments = args;
		EnsureUserDataDirectoryExists();
		WriteLogHeader();
		LoadConfiguration();
		RunOrFocusGame();
	}

	private static void EnsureUserDataDirectoryExists()
	{
		if (Directory.Exists(UserDataDirectory))
			return;
		Directory.CreateDirectory(UserDataDirectory);
	}

	private static void LoadConfiguration()
	{
		string path = Path.Combine(UserDataDirectory, IniConfigurationPath);
		if (File.Exists(path))
		{
			Configuration = new IniConfiguration(path);
		}
		else
		{
			Configuration = new IniConfiguration();
			Configuration.SetProperty("video", "fullscreen", "0");
			Configuration.SetProperty("audio", "volume", "1.0");
			Configuration.SetProperty("audio", "music_volume", "0.5");
			Configuration.SetProperty("audio", "sound_volume", "1.0");
			Configuration.Save(path);
		}
	}

	/// <summary>
	/// Runs the game, or brings the window of the game to the front when another instance is already running.
	/// </summary>
	private static void RunOrFocusGame()
	{
		using (new Mutex(true, "SonicOrca", out bool createdNew))
		{
			if (createdNew || Configuration.GetPropertyBoolean("debug", "allow_multiple_instances"))
				RunGame();
			else
				FocusGame();
		}
	}

	private static IPlatform GetPlatform() => SDL2Platform.Instance;

	private static void RunGame()
	{
		using (IPlatform platform = GetPlatform())
		{
			try
			{
				platform.Initialise();
			}
			catch (Exception ex)
			{
				LogException(ex);
				ShowErrorMessageBox(ex.Message);
				return;
			}
			if (!CheckOpenGL(platform))
				return;
			using (S2HDSonicOrcaGameContext gameContext = new S2HDSonicOrcaGameContext(platform))
			{
				try
				{
					Trace.WriteLine("Initialising game");
					Trace.Indent();
					gameContext.Initialise();
					Trace.Unindent();
					Trace.WriteLine("Running game");
					Trace.Indent();
					gameContext.Run();
				}
				catch (Exception ex)
				{
					if (Debugger.IsAttached)
						throw;
					LogException(ex);
					ShowErrorMessageBox(ex.Message);
				}
				finally
				{
					Trace.Unindent();
				}
			}
		}
		Trace.Unindent();
		Trace.WriteLine(new string('-', 80));
		Trace.WriteLine(string.Empty);
	}

	private static void FocusGame()
	{
		Process current = Process.GetCurrentProcess();
		Process process = Process.GetProcessesByName(current.ProcessName).Where(x => x.Id != current.Id).FirstOrDefault();
		if (process == null || !OperatingSystem.IsWindows())
			return;
		SetForegroundWindow(process.MainWindowHandle);
	}

	private static bool CheckOpenGL(IPlatform platform)
	{
		Trace.WriteLine("Verifying OpenGL version");
		Version openGlVersion = platform.GetOpenGLVersion();
		Trace.WriteLine($"OpenGL {openGlVersion.Major}.{openGlVersion.Minor}");
		if (!(openGlVersion < AppMinOpenGLVersion))
			return true;
		Trace.WriteLine("OpenGL version too low");
		ShowErrorMessageBox($"OpenGL {AppMinOpenGLVersion.Major}.{AppMinOpenGLVersion.Minor} or later is required.");
		return false;
	}

	public static void ShowErrorMessageBox(string text)
	{
		SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING, "SonicOrca", text, IntPtr.Zero);
	}

	private static void WriteLogHeader()
	{
		Trace.AutoFlush = true;
		Trace.Listeners.Add(new TextWriterTraceListener(LogPath));
		Trace.Listeners.Add(new ConsoleTraceListener());
		Trace.WriteLine(Environment.OSVersion);
		Trace.WriteLine($"SonicOrca {AppVersion} [{BuildConfigurationName} {AppArchitecture}]");
		Trace.WriteLine(DateTime.Now.ToString("dd MMMM yyyy @ hh:mm tt"));
		Trace.WriteLine(new string('-', 80));
		Trace.Indent();
	}

	public static void LogException(Exception ex, bool logStackTrace = true)
	{
		string[] messageLines = ex.Message.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		if (messageLines.Length <= 1)
		{
			Trace.WriteLine("EXCEPTION: " + messageLines[0]);
		}
		else
		{
			Trace.WriteLine("EXCEPTION:");
			Trace.Indent();
			foreach (string message in messageLines)
				Trace.WriteLine(message);
			Trace.Unindent();
		}
		if (ex.InnerException != null)
		{
			Trace.Indent();
			LogException(ex.InnerException);
			Trace.Unindent();
		}
		if (!logStackTrace || string.IsNullOrEmpty(ex.StackTrace))
			return;
		string[] stackTraceLines = ex.StackTrace.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		Trace.WriteLine("STACK TRACE:");
		Trace.Indent();
		foreach (string line in stackTraceLines)
			Trace.WriteLine(line.Trim());
		Trace.Unindent();
	}
}
