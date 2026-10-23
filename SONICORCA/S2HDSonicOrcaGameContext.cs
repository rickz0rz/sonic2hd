// Decompiled with JetBrains decompiler
// Type: SonicOrca.S2HDSonicOrcaGameContext
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using S2HD;
using SonicOrca.Core;
using SonicOrca.Drawing;
using SonicOrca.Drawing.LevelRendering;
using SonicOrca.Drawing.Renderers;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

#nullable disable
namespace SonicOrca;

internal class S2HDSonicOrcaGameContext(IPlatform platform) : SonicOrcaGameContext(platform)
{
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

	public override void Initialise()
	{
		base.Initialise();
		CommandLineArguments = new CommandLineArguments(Program.CommandLineArguments);
		Configuration = Program.Configuration;
		UserDataDirectory = Program.UserDataDirectory;
		SonicOrca.Core.Debugging.FrameProfiler.Initialise(UserDataDirectory);
		_canvas = Window.GraphicsContext.CreateFrameBuffer(1920, 1080);
		SonicOrcaGameContext.IsMaxPerformance = Configuration.GetPropertyBoolean("graphics", "max_performance");
		if (Configuration.GetPropertyBoolean("graphics", "vsync"))
			Window.VSync = true;
		Audio.Volume = Configuration.GetPropertyDouble("audio", "volume", 1.0);
		Audio.MusicVolume = Configuration.GetPropertyDouble("audio", "music_volume", 0.2);
		Audio.SoundVolume = Configuration.GetPropertyDouble("audio", "sound_volume", 1.0);
		Input.IsVibrationEnabled = Configuration.GetPropertyBoolean("input", "vibration", true);
		Settings = new S2HDSettings(Configuration, Audio, Window);
		Settings.Apply();
		Window.WindowTitle = "Sonic 2 HD";
		Window.AspectRatio = new Vector2i(16 /*0x10*/, 9);
		string directoryName = ContentDirectory.Path;
		LoadResourceFiles(Path.Combine(directoryName, "data"));
		if (bool.Parse(Configuration.GetProperty("general", "use_mods", "true")))
		{
			// Mods next to the game data, then mods in the user data folder (outside the macOS app bundle)
			LoadResourceFiles(Path.Combine(directoryName, "mods"));
			LoadResourceFiles(Path.Combine(UserDataDirectory, "mods"));
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

	private void Begin()
	{
		string str = null;
		LevelPrepareSettings prepareSettings = new LevelPrepareSettings()
		{
			Act = 1,
			Lives = 3
		};
		IReadOnlyList<string> commandLineArguments = Program.CommandLineArguments;
		for (int index = 0; index < commandLineArguments.Count; ++index)
		{
			bool flag = index == commandLineArguments.Count - 1;
			string argument = commandLineArguments[index];
			switch (argument.ToLower())
			{
				case "-act":
					if (!flag)
					{
						int result;
						int.TryParse(commandLineArguments[++index], out result);
						prepareSettings.Act = result;
						break;
					}
					break;
				case "-credits":
					str = "credits";
					break;
				case "-debug":
					prepareSettings.Debugging = true;
					break;
				case "-edit":
				case "-editor":
					str = "editor";
					break;
				case "-fullscreen":
					Window.FullScreen = true;
					break;
				case "-ghost":
					if (!flag)
					{
						prepareSettings.RecordedPlayGhostReadPath = commandLineArguments[++index];
						break;
					}
					break;
				case "-halfpipe":
					str = "halfpipe";
					break;
				case "-host":
					Trace.WriteLine("Waiting for client");
					NetworkManager.Host();
					while (!NetworkManager.AllConnected)
						NetworkManager.Update();
					break;
				case "-join":
					string serverHost = commandLineArguments[++index];
					Trace.WriteLine("Joining " + serverHost);
					NetworkManager.Join(serverHost);
					while (!NetworkManager.AllConnected)
						NetworkManager.Update();
					break;
				case "-level":
					if (!flag)
					{
						string areaResourceKey = Levels.GetAreaResourceKey(commandLineArguments[++index].ToLower());
						if (areaResourceKey != null)
						{
							prepareSettings.AreaResourceKey = areaResourceKey;
							str = "level";
							break;
						}
						break;
					}
					break;
				case "-lives":
					if (!flag)
					{
						int result;
						int.TryParse(commandLineArguments[++index], out result);
						prepareSettings.Lives = MathX.Clamp(1, result, 99);
						break;
					}
					break;
				case "-night":
					if (!flag)
					{
						double result;
						double.TryParse(commandLineArguments[++index], NumberStyles.Float, CultureInfo.InvariantCulture, out result);
						prepareSettings.NightMode = result;
						break;
					}
					break;
				case "-playback":
					if (!flag)
					{
						prepareSettings.RecordedPlayReadPath = commandLineArguments[++index];
						break;
					}
					break;
				case "-protagonist":
					if (!flag)
					{
						argument = commandLineArguments[++index].ToLower();
						prepareSettings.ProtagonistCharacter = (CharacterType)(CharacterNames.IndexOf<string>(x => x.Equals(argument)) + 1);
						break;
					}
					break;
				case "-record":
					if (!flag)
					{
						prepareSettings.RecordedPlayWritePath = commandLineArguments[++index];
						break;
					}
					break;
				case "-sidekick":
					if (!flag)
					{
						argument = commandLineArguments[++index].ToLower();
						prepareSettings.SidekickCharacter = (CharacterType)(CharacterNames.IndexOf<string>(x => x.Equals(argument)) + 1);
						break;
					}
					break;
				case "-startpos":
					int result1;
					int result2;
					if (!flag && int.TryParse(commandLineArguments[++index], out result1) && index != commandLineArguments.Count - 1 && int.TryParse(commandLineArguments[++index], out result2))
					{
						prepareSettings.StartPosition = new Vector2i?(new Vector2i(result1, result2));
						break;
					}
					break;
				case "-timetrial":
					prepareSettings.TimeTrial = true;
					break;
				case "-window":
					Window.FullScreen = false;
					break;
			}
		}
		switch (str)
		{
			case "credits":
				CreditsGameState creditsGameState = new CreditsGameState(this);
				break;
			case "level":
				new StoryPlaythroughGameState(this).StartFrom(prepareSettings);
				break;
			case "halfpipe":
				break;
			default:
				Program.CommandLineArguments.Contains<string>("-soundtrack");
				break;
		}
	}

	protected override void OnUpdate()
	{
		if (!Input.CurrentState.Keyboard[226] && !Input.CurrentState.Keyboard[230] || !Input.Pressed.Keyboard[40])
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
		Input.OutputState.GamePad = Output.ToArray<GamePadOutputState>();
	}

	protected override void OnDraw()
	{
		SonicOrca.Core.Debugging.TestHooks.DrawnLevelTick = -1;
		I2dRenderer obj1 = Renderer.Get2dRenderer();
		obj1.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
		if (ForceHD)
			_canvas.Activate();
		else
			Window.GraphicsContext.RenderToBackBuffer();
		Window.GraphicsContext.ClearBuffer();
		_rootGameState.Draw();
		Renderer.DeativateRenderer();
		Console.Draw(Renderer);
		Renderer.DeativateRenderer();
		if (SonicOrcaGameContext.Singleton.Input.Pressed.Keyboard[67] || SonicOrca.Core.Debugging.FrameProfiler.IsCaptureFrame(DrawCount))
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
		obj1.BlendMode = BlendMode.Opaque;
		obj1.Colour = Colours.White;
		I2dRenderer obj2 = obj1;
		Vector2i clientSize = Window.ClientSize;
		double x1 = clientSize.X;
		clientSize = Window.ClientSize;
		double y1 = clientSize.Y;
		Rectangle rectangle = new Rectangle(0.0, 0.0, x1, y1);
		obj2.ClipRectangle = rectangle;
		I2dRenderer obj3 = obj1;
		ITexture texture = _canvas.Textures[0];
		clientSize = Window.ClientSize;
		double x2 = clientSize.X;
		clientSize = Window.ClientSize;
		double y2 = clientSize.Y;
		Rectangle destination = new Rectangle(0.0, 0.0, x2, y2);
		obj3.RenderTexture(texture, destination, flipy: true);
		obj1.Deactivate();
	}

	/// <summary>
	/// Merges each .dat file in the folder into the resource tree, in name order. A later file replaces the
	/// resources with the same key, so mods/ overrides data/, and mods/20-x.dat overrides mods/10-y.dat.
	/// </summary>
	private void LoadResourceFiles(string inputDirectory)
	{
		if (!Directory.Exists(inputDirectory))
			return;
		foreach (string file in Directory.GetFiles(inputDirectory, "*.dat").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
		{
			Trace.WriteLine("Loading resource file " + file);
			ResourceTree.MergeWith(new ResourceFile(file).Scan());
		}
	}

	protected override Renderer CreateRenderer() => new TheRenderer(Window);

	protected override ILevelRenderer CreateLevelRenderer(Level level)
	{
		return new LevelRenderer(level, VideoSettings);
	}
}
