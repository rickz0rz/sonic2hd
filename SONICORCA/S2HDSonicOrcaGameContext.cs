using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using S2HD;
using SonicOrca.Core;
using SonicOrca.Core.Editor;
using SonicOrca.Drawing;
using SonicOrca.Drawing.LevelRendering;
using SonicOrca.Drawing.Renderers;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace SonicOrca;

/// <summary>
/// The game context of Sonic 2 HD. It loads the configuration and the resource files, creates the root game state,
/// and runs it one step for each update.
/// </summary>
internal class S2HDSonicOrcaGameContext(IPlatform platform) : SonicOrcaGameContext(platform)
{
	// SDL scancodes of the keys that the game context uses.
	private const int EnterScancode = 40;
	private const int F10Scancode = 67;
	private const int LeftAltScancode = 226;
	private const int RightAltScancode = 230;

	private IGameState _rootGameState;
	private Updater _gameStateUpdater;
	private readonly IReadOnlyList<string> CharacterNames = new string[3]
	{
		"sonic",
		"tails",
		"knuckles"
	};

	public CommandLineArguments CommandLineArguments { get; private set; }

	public LevelGameState CurrentLevelScreen { get; set; }

	public IRendererFactory RenderFactory { get; private set; }

	public S2HDSettings Settings { get; private set; }

	public override IAudioSettings AudioSettings => Settings;

	public override IVideoSettings VideoSettings => Settings;

	public override PhysicsMode Physics => Settings.Physics;

	public override void Initialise()
	{
		base.Initialise();
		CommandLineArguments = new CommandLineArguments(Program.CommandLineArguments);
		Configuration = Program.Configuration;
		UserDataDirectory = Program.UserDataDirectory;
		SonicOrca.Core.Debugging.FrameProfiler.Initialise(UserDataDirectory);
		_canvas = Window.GraphicsContext.CreateFrameBuffer(1920, 1080);
		IsMaxPerformance = Configuration.GetPropertyBoolean("graphics", "max_performance");
		if (Configuration.GetPropertyBoolean("graphics", "vsync"))
			Window.VSync = true;
		Audio.Volume = Configuration.GetPropertyDouble("audio", "volume", 1.0);
		Audio.MusicVolume = Configuration.GetPropertyDouble("audio", "music_volume", 0.2);
		Audio.SoundVolume = Configuration.GetPropertyDouble("audio", "sound_volume", 1.0);
		Input.IsVibrationEnabled = Configuration.GetPropertyBoolean("input", "vibration", true);
		Settings = new S2HDSettings(Configuration, Audio, Window);
		Settings.Apply();
		Window.WindowTitle = "Sonic 2 HD";
		Window.AspectRatio = new Vector2i(16, 9);
		string contentDirectory = ContentDirectory.Path;
		LoadResourceFiles(Path.Combine(contentDirectory, "data"));
		if (bool.Parse(Configuration.GetProperty("general", "use_mods", "true")))
		{
			// Mods next to the game data, then mods in the user data folder (outside the macOS app bundle)
			LoadResourceFiles(Path.Combine(contentDirectory, "mods"), folders: true);
			LoadResourceFiles(Path.Combine(UserDataDirectory, "mods"), folders: true);
			// The changes of the level editor load last. They load into memory, because the editor writes the files again.
			LoadResourceFiles(EditorModFile.GetFolder(UserDataDirectory), inMemory: true, folders: true);
		}
		RenderFactory = DefaultRendererFactory.Create(Window.GraphicsContext);
		_rootGameState = new RootGameState(this);
		_gameStateUpdater = new Updater(_rootGameState.Update());
		Begin();
	}

	public override void Dispose()
	{
		_rootGameState.Dispose();
		SonicOrca.Core.Debugging.FrameProfiler.Shutdown();
		base.Dispose();
	}

	/// <summary>
	/// Reads the old-style single-dash command line options (for example "-level ehz").
	/// Most of the results are not used: <see cref="RootGameState"/> reads the "--" options and starts the game.
	/// The window, network and level state changes below still happen, so this method stays.
	/// </summary>
	private void Begin()
	{
		string startMode = null;
		LevelPrepareSettings prepareSettings = new LevelPrepareSettings()
		{
			Act = 1,
			Lives = 3
		};
		IReadOnlyList<string> commandLineArguments = Program.CommandLineArguments;
		for (int index = 0; index < commandLineArguments.Count; ++index)
		{
			bool isLastArgument = index == commandLineArguments.Count - 1;
			string argument = commandLineArguments[index];
			switch (argument.ToLower())
			{
				case "-act":
					if (!isLastArgument)
					{
						int.TryParse(commandLineArguments[++index], out int act);
						prepareSettings.Act = act;
					}
					break;
				case "-credits":
					startMode = "credits";
					break;
				case "-debug":
					prepareSettings.Debugging = true;
					break;
				case "-edit":
				case "-editor":
					startMode = "editor";
					break;
				case "-fullscreen":
					Window.FullScreen = true;
					break;
				case "-ghost":
					if (!isLastArgument)
						prepareSettings.RecordedPlayGhostReadPath = commandLineArguments[++index];
					break;
				case "-halfpipe":
					startMode = "halfpipe";
					break;
				case "-host":
					Trace.WriteLine("Waiting for client");
					NetworkManager.Host();
					while (!NetworkManager.AllConnected)
						NetworkManager.Update();
					break;
				case "-join":
					if (!isLastArgument)
					{
						string serverHost = commandLineArguments[++index];
						Trace.WriteLine("Joining " + serverHost);
						NetworkManager.Join(serverHost);
						while (!NetworkManager.AllConnected)
							NetworkManager.Update();
					}
					break;
				case "-level":
					if (!isLastArgument)
					{
						string areaResourceKey = Levels.GetAreaResourceKey(commandLineArguments[++index].ToLower());
						if (areaResourceKey != null)
						{
							prepareSettings.AreaResourceKey = areaResourceKey;
							startMode = "level";
						}
					}
					break;
				case "-lives":
					if (!isLastArgument)
					{
						int.TryParse(commandLineArguments[++index], out int lives);
						prepareSettings.Lives = MathX.Clamp(1, lives, 99);
					}
					break;
				case "-night":
					if (!isLastArgument)
					{
						double.TryParse(commandLineArguments[++index], NumberStyles.Float, CultureInfo.InvariantCulture, out double nightMode);
						prepareSettings.NightMode = nightMode;
					}
					break;
				case "-playback":
					if (!isLastArgument)
						prepareSettings.RecordedPlayReadPath = commandLineArguments[++index];
					break;
				case "-protagonist":
					if (!isLastArgument)
					{
						argument = commandLineArguments[++index].ToLower();
						prepareSettings.ProtagonistCharacter = (CharacterType)(CharacterNames.IndexOf(x => x.Equals(argument)) + 1);
					}
					break;
				case "-record":
					if (!isLastArgument)
						prepareSettings.RecordedPlayWritePath = commandLineArguments[++index];
					break;
				case "-sidekick":
					if (!isLastArgument)
					{
						argument = commandLineArguments[++index].ToLower();
						prepareSettings.SidekickCharacter = (CharacterType)(CharacterNames.IndexOf(x => x.Equals(argument)) + 1);
					}
					break;
				case "-startpos":
					if (!isLastArgument && int.TryParse(commandLineArguments[++index], out int startX) && index != commandLineArguments.Count - 1 && int.TryParse(commandLineArguments[++index], out int startY))
						prepareSettings.StartPosition = new Vector2i(startX, startY);
					break;
				case "-timetrial":
					prepareSettings.TimeTrial = true;
					break;
				case "-window":
					Window.FullScreen = false;
					break;
			}
		}
		switch (startMode)
		{
			case "credits":
				// The new game state is not used. Its constructor creates a resource session.
				new CreditsGameState(this);
				break;
			case "level":
				// The new game state is not used, but StartFrom sets Console.LevelGameState.
				new StoryPlaythroughGameState(this).StartFrom(prepareSettings);
				break;
		}
	}

	/// <summary>Alt+Enter switches between full screen and a window.</summary>
	protected override void OnUpdate()
	{
		bool altDown = Input.CurrentState.Keyboard[LeftAltScancode] || Input.CurrentState.Keyboard[RightAltScancode];
		if (!altDown || !Input.Pressed.Keyboard[EnterScancode])
			return;
		Window.FullScreen = !Window.FullScreen;
	}

	protected override void OnUpdateStep()
	{
		Console.Update();
		NetworkManager.Update();
		if (!_gameStateUpdater.Update())
			Finish = true;
		foreach (Controller controller in Controllers)
			controller.Update();
		Input.OutputState.GamePad = Output.ToArray();
	}

	protected override void OnDraw()
	{
		SonicOrca.Core.Debugging.TestHooks.DrawnLevelTick = -1;
		I2dRenderer g = Renderer.Get2dRenderer();
		g.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		// With ForceHD, draw to a 1920 x 1080 canvas first, and then scale the canvas to the window.
		if (ForceHD)
			_canvas.Activate();
		else
			Window.GraphicsContext.RenderToBackBuffer();
		Window.GraphicsContext.ClearBuffer();
		_rootGameState.Draw();
		Renderer.DeativateRenderer();
		Console.Draw(Renderer);
		Renderer.DeativateRenderer();
		// F10 takes a screenshot.
		if (Singleton.Input.Pressed.Keyboard[F10Scancode] || SonicOrca.Core.Debugging.FrameProfiler.IsCaptureFrame(DrawCount))
			ScreenshotRenderer.GrabScreenshot(UserDataDirectory);
		if (SonicOrca.Core.Debugging.TestHooks.TakeLevelTickCapture(out string captureFilename))
		{
			ScreenshotRenderer.GrabScreenshot(UserDataDirectory, captureFilename);
			if (SonicOrca.Core.Debugging.TestHooks.ExitAfterCaptures && SonicOrca.Core.Debugging.TestHooks.AllCapturesTaken)
				Finish = true;
		}
		if (!ForceHD)
			return;
		Window.GraphicsContext.RenderToBackBuffer();
		g.BlendMode = BlendMode.Opaque;
		g.Colour = Colours.White;
		Vector2i clientSize = Window.ClientSize;
		g.ClipRectangle = new Rectangle(0.0, 0.0, clientSize.X, clientSize.Y);
		g.RenderTexture(_canvas.Textures[0], new Rectangle(0.0, 0.0, clientSize.X, clientSize.Y), flipy: true);
		g.Deactivate();
	}

	/// <summary>
	/// Merges each .dat file in the folder into the resource tree, in name order. A later file replaces the
	/// resources with the same key, so mods/ overrides data/, and mods/20-x.dat overrides mods/10-y.dat.
	/// With <paramref name="folders"/>, each subfolder is also a mod: a folder of resource files as "s2hd-data
	/// unpack" writes them, for example mods/my-mod/SONICORCA/OBJECTS/RING/TEXTURE.png. The game reads these files
	/// from the disk, so a changed file needs no pack step, only a restart. The editor folder, its folder of unsaved
	/// changes and the folders whose names start with a dot are not mods. The editor folder loads last, with its
	/// folders: for example the folder of the animation editor.
	/// </summary>
	private void LoadResourceFiles(string inputDirectory, bool inMemory = false, bool folders = false)
	{
		if (!Directory.Exists(inputDirectory))
			return;
		IEnumerable<string> paths = Directory.GetFiles(inputDirectory, "*.dat");
		if (folders)
		{
			paths = paths.Concat(Directory.GetDirectories(inputDirectory).Where(d =>
			{
				string name = Path.GetFileName(d);
				// The editor folder loads last; in it, the changes that the user did not save do not load
				return !name.StartsWith('.') && !name.Equals(EditorModFile.FolderName, StringComparison.OrdinalIgnoreCase) &&
					!name.Equals(EditorModFile.UnsavedFolderName, StringComparison.OrdinalIgnoreCase);
			}));
		}
		foreach (string path in paths.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
		{
			if (Directory.Exists(path))
			{
				var skipped = new List<string>();
				ResourceTree folderTree = ResourceDirectory.Import(path, skipped);
				Trace.WriteLine($"Loading resource folder {path}: {folderTree.GetResourceListing().Count} resources, {skipped.Count} files of unknown type");
				ResourceTree.MergeWith(folderTree);
				continue;
			}
			Trace.WriteLine("Loading resource file " + path);
			ResourceTree.MergeWith(inMemory ? EditorModFile.ReadIntoMemory(path) : new ResourceFile(path).Scan());
		}
	}

	protected override Renderer CreateRenderer() => new TheRenderer(Window);

	protected override ILevelRenderer CreateLevelRenderer(Level level)
	{
		return new LevelRenderer(level, VideoSettings);
	}
}
