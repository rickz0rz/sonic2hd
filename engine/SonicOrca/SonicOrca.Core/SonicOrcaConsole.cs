using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The developer console. It shows recent output lines and reads commands that the user types,
/// for example "debug", "spawn", "teleport" and "restart".
/// </summary>
public class SonicOrcaConsole : IDisposable
{
	private const string DefaultFontResourceKey = "SONICORCA/FONTS/SEGOEUI";

	/// <summary>
	/// The maximum number of lines that the output buffer and the command history keep.
	/// </summary>
	private const int MaxStoredLines = 128;

	/// <summary>
	/// The number of output lines that the console shows.
	/// </summary>
	private const int VisibleLines = 6;

	/// <summary>
	/// The change in opacity each frame when the console opens or closes (15 frames for a full fade).
	/// </summary>
	private const float FadeStep = 1f / 15f;

	private readonly SonicOrcaGameContext _context;

	private ResourceSession _resourceSession;

	private Font _font;

	private bool _loadingResources;

	private bool _resourcesLoaded;

	private float _opacity;

	private readonly StringBuilder _currentLine = new StringBuilder();

	private readonly List<string> _buffer = new List<string>();

	private readonly List<string> _history = new List<string>();

	private int _historyScrollIndex;

	public bool IsOpen { get; set; }

	public LevelGameState LevelGameState { get; set; }

	public SonicOrcaConsole(SonicOrcaGameContext context)
	{
		_context = context;
	}

	public void LoadResources()
	{
		if (!_loadingResources)
		{
			_loadingResources = true;
			Task.Run(() => LoadResourcesAsync());
		}
	}

	public async Task LoadResourcesAsync()
	{
		ResourceTree resourceTree = _context.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependency(DefaultFontResourceKey);
		await _resourceSession.LoadAsync();
		_font = resourceTree.GetLoadedResource<Font>(DefaultFontResourceKey);
		_loadingResources = false;
		_resourcesLoaded = true;
	}

	public void Dispose()
	{
		if (_resourceSession != null)
		{
			_resourceSession.Dispose();
		}
	}

	public void Update()
	{
		if (!_resourcesLoaded)
		{
			LoadResources();
			return;
		}
		InputContext input = _context.Input;
		KeyboardState keyboard = input.Pressed.Keyboard;
		if (IsOpen)
		{
			_opacity = Math.Min(_opacity + FadeStep, 1f);
			if (keyboard[KeyboardState.KEY_BACKSPACE])
			{
				if (_currentLine.Length > 0)
				{
					_currentLine.Remove(_currentLine.Length - 1, 1);
				}
			}
			else if (keyboard[KeyboardState.KEY_RETURN])
			{
				if (_currentLine.Length > 0)
				{
					string line = _currentLine.ToString();
					_currentLine.Clear();
					if (_history.Count >= MaxStoredLines)
					{
						_history.RemoveAt(0);
					}
					_history.Add(line);
					_historyScrollIndex = _history.Count;
					WriteLine("> " + line);
					ProcessLine(line);
				}
			}
			else if (keyboard[KeyboardState.KEY_UP])
			{
				_currentLine.Clear();
				_historyScrollIndex = Math.Max(0, _historyScrollIndex - 1);
				if (_historyScrollIndex < _history.Count)
				{
					_currentLine.Append(_history[_historyScrollIndex]);
				}
			}
			else if (keyboard[KeyboardState.KEY_DOWN])
			{
				_currentLine.Clear();
				_historyScrollIndex = Math.Min(_history.Count, _historyScrollIndex + 1);
				if (_historyScrollIndex < _history.Count)
				{
					_currentLine.Append(_history[_historyScrollIndex]);
				}
			}
			else if (input.TextInput != null)
			{
				_currentLine.Append(input.TextInput);
			}
		}
		else
		{
			_opacity = Math.Max(_opacity - FadeStep, 0f);
		}
	}

	public void Draw(Renderer renderer)
	{
		if (_resourcesLoaded && _opacity != 0f)
		{
			int lineHeight = _font.Height;
			Colour backgroundColour = new Colour(0.8 * _opacity, 0.1, 0.1, 0.1);
			Colour textColour = new Colour(1.0 * _opacity, 1.0, 1.0, 1.0);
			Vector2i clientSize = renderer.Window.ClientSize;
			Rectanglei outputBounds = new Rectanglei(8, 8, clientSize.X - 16, VisibleLines * lineHeight);
			Rectanglei inputBounds = new Rectangle(8.0, outputBounds.Bottom + 8, clientSize.X - 16, lineHeight);
			I2dRenderer renderer2d = renderer.Get2dRenderer();
			renderer2d.RenderQuad(backgroundColour, outputBounds);
			renderer2d.RenderQuad(backgroundColour, inputBounds);
			IFontRenderer fontRenderer = renderer.GetFontRenderer();
			// Draw the newest lines from the bottom of the output area upward.
			int lineY = outputBounds.Bottom;
			for (int i = 0; i < Math.Min(_buffer.Count, VisibleLines); i++)
			{
				string line = _buffer[_buffer.Count - 1 - i];
				lineY -= lineHeight;
				fontRenderer.RenderString(line, new Rectangle(outputBounds.X + 4, lineY, outputBounds.Width, lineHeight), FontAlignment.MiddleY, _font, textColour);
			}
			fontRenderer.RenderString("> " + _currentLine.ToString(), inputBounds.Inflate(new Vector2i(-4, 0)), FontAlignment.MiddleY, _font, textColour);
		}
	}

	public void WriteLine(string line)
	{
		if (_buffer.Count >= MaxStoredLines)
		{
			_buffer.RemoveAt(0);
		}
		_buffer.Add(line);
	}

	private void ProcessLine(string line)
	{
		string[] tokens = GetTokens(line);
		if (tokens.Length != 0)
		{
			string[] args = tokens.Skip(1).ToArray();
			switch (tokens[0].ToLower())
			{
				case "barrier":
					ProcessCommandBarrier(args);
					break;
				case "debug":
					ProcessCommandDebug(args);
					break;
				case "spawn":
					ProcessCommandSpawn(args);
					break;
				case "teleport":
					ProcessCommandTeleport(args);
					break;
				case "restart":
					ProcessCommandRestart(args);
					break;
			}
		}
	}

	private void ProcessCommandBarrier(string[] args)
	{
		if (args.Length < 1)
		{
			WriteLine("usage: barrier <none|classic|bubble|fire|lightning>");
			return;
		}
		BarrierType barrierType = BarrierType.None;
		switch (args[0].ToLower())
		{
			case "none":
				barrierType = BarrierType.None;
				break;
			case "classic":
				barrierType = BarrierType.Classic;
				break;
			case "bubble":
				barrierType = BarrierType.Bubble;
				break;
			case "fire":
				barrierType = BarrierType.Fire;
				break;
			case "lightning":
				barrierType = BarrierType.Lightning;
				break;
			default:
				WriteLine("usage: barrier <none|classic|bubble|fire|lightning>");
				return;
		}
		// The local variable hides the LevelGameState property and is always null, so this command has no effect.
		LevelGameState levelGameState = null;
		if (levelGameState != null && levelGameState.Level != null)
		{
			ICharacter protagonist = levelGameState.Player.Protagonist;
			if (protagonist != null)
			{
				protagonist.Barrier = barrierType;
			}
		}
	}

	private void ProcessCommandDebug(string[] args)
	{
		if (LevelGameState == null || LevelGameState.Level == null)
		{
			return;
		}
		Level level = LevelGameState.Level;
		if (args.Length == 0)
		{
			level.ClassicDebugMode = !level.ClassicDebugMode;
			return;
		}
		switch (args[0].ToLower())
		{
			case "collision":
				// Toggle between the normal view and the collision view.
				if (level.LayerViewOptions.ShowLandscapeCollision)
				{
					level.LayerViewOptions.ShowLandscape = true;
					level.LayerViewOptions.ShowLandscapeCollision = false;
					level.LayerViewOptions.ShowObjectCollision = false;
					level.ShowCharacterInfo = false;
				}
				else
				{
					level.LayerViewOptions.ShowLandscape = false;
					level.LayerViewOptions.ShowLandscapeCollision = true;
					level.LayerViewOptions.ShowObjectCollision = true;
					level.ShowCharacterInfo = true;
				}
				break;
			case "invulnerable":
				level.Player.Protagonist.IsInvincible = !level.Player.Protagonist.IsInvincible;
				break;
		}
	}

	private void ProcessCommandSpawn(string[] args)
	{
		if (LevelGameState == null || LevelGameState.Level == null)
		{
			return;
		}
		Level level = LevelGameState.Level;
		if (args.Length < 1)
		{
			return;
		}
		string objectType = "SONICORCA/OBJECTS/" + args[0].ToUpper();
		if (!level.ObjectManager.RegisteredTypes.Any(x => x.ResourceKey == objectType))
		{
			return;
		}
		// The other arguments are "-name value" pairs that set the behaviour of the new object.
		Dictionary<string, object> behaviour = new Dictionary<string, object>();
		for (int i = 1; i < args.Length; i++)
		{
			if (args[i].StartsWith("-") || i >= args.Length - 2)
			{
				string key = args[i].Substring(1);
				object value = ParseBehaviourValue(args[i + 1]);
				behaviour.Add(key, value);
				i++;
			}
		}
		Vector2i position = level.Player.Protagonist.Position;
		LevelLayer layer = level.Map.Layers[level.Map.CollisionPathLayers.Last()];
		ObjectPlacement objectPlacement = new ObjectPlacement(objectType, level.Map.Layers.IndexOf(layer), position, behaviour);
		level.ObjectManager.AddObject(objectPlacement);
	}

	private static object ParseBehaviourValue(string value)
	{
		if (int.TryParse(value, out int intValue))
		{
			return intValue;
		}
		if (double.TryParse(value, out double doubleValue))
		{
			return doubleValue;
		}
		if (bool.TryParse(value, out bool boolValue))
		{
			return boolValue;
		}
		return value;
	}

	private void ProcessCommandTeleport(string[] args)
	{
		if (LevelGameState != null && LevelGameState.Level != null)
		{
			Level level = LevelGameState.Level;
			if (args.Length >= 2 && int.TryParse(args[0], out int x) && int.TryParse(args[1], out int y))
			{
				level.Player.Protagonist.Position = new Vector2i(x, y);
			}
		}
	}

	private void ProcessCommandRestart(string[] args)
	{
		if (LevelGameState != null && LevelGameState.Level != null)
		{
			Level level = LevelGameState.Level;
			string mode = string.Empty;
			if (args.Length >= 1)
			{
				mode = args[0].ToLower();
			}
			if (mode == "checkpoint" || mode == "starpost")
			{
				level.FadeOut(LevelState.RestartCheckpoint);
			}
			else
			{
				level.FadeOut(LevelState.Restart);
			}
		}
	}

	/// <summary>
	/// Splits a command line into tokens at white space. Text in double quotes stays in one token.
	/// </summary>
	private static string[] GetTokens(string line)
	{
		List<string> tokens = new List<string>();
		StringBuilder currentToken = new StringBuilder();
		bool inQuotes = false;
		foreach (char c in line)
		{
			if (c == '"')
			{
				inQuotes = !inQuotes;
			}
			else if (char.IsWhiteSpace(c) && !inQuotes)
			{
				if (currentToken.Length > 0)
				{
					tokens.Add(currentToken.ToString());
					currentToken.Clear();
				}
			}
			else
			{
				currentToken.Append(c);
			}
		}
		if (currentToken.Length > 0)
		{
			tokens.Add(currentToken.ToString());
		}
		return tokens.ToArray();
	}
}
