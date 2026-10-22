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

public class SonicOrcaConsole : IDisposable
{
	private const string DefaultFontResourceKey = "SONICORCA/FONTS/SEGOEUI";

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
		_resourceSession.PushDependency("SONICORCA/FONTS/SEGOEUI");
		await _resourceSession.LoadAsync();
		_font = resourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/SEGOEUI");
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
			_opacity = Math.Min(_opacity + 1f / 15f, 1f);
			if (keyboard[42])
			{
				if (_currentLine.Length > 0)
				{
					_currentLine.Remove(_currentLine.Length - 1, 1);
				}
			}
			else if (keyboard[40])
			{
				if (_currentLine.Length > 0)
				{
					string text = _currentLine.ToString();
					_currentLine.Clear();
					if (_history.Count >= 128)
					{
						_history.RemoveAt(0);
					}
					_history.Add(text);
					_historyScrollIndex = _history.Count;
					WriteLine("> " + text);
					ProcessLine(text);
				}
			}
			else if (keyboard[82])
			{
				_currentLine.Clear();
				_historyScrollIndex = Math.Max(0, _historyScrollIndex - 1);
				if (_historyScrollIndex < _history.Count)
				{
					_currentLine.Append(_history[_historyScrollIndex]);
				}
			}
			else if (keyboard[81])
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
			_opacity = Math.Max(_opacity - 1f / 15f, 0f);
		}
	}

	public void Draw(Renderer renderer)
	{
		if (_resourcesLoaded && _opacity != 0f)
		{
			int height = _font.Height;
			Colour colour = new Colour(0.8 * (double)_opacity, 0.1, 0.1, 0.1);
			Colour colour2 = new Colour(1.0 * (double)_opacity, 1.0, 1.0, 1.0);
			Vector2i clientSize = renderer.Window.ClientSize;
			Rectanglei rectanglei = new Rectanglei(8, 8, clientSize.X - 16, 6 * height);
			Rectanglei rectanglei2 = new Rectangle(8.0, rectanglei.Bottom + 8, clientSize.X - 16, height);
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			i2dRenderer.RenderQuad(colour, rectanglei);
			i2dRenderer.RenderQuad(colour, rectanglei2);
			IFontRenderer fontRenderer = renderer.GetFontRenderer();
			int num = rectanglei.Bottom;
			for (int i = 0; i < Math.Min(_buffer.Count, 6); i++)
			{
				string text = _buffer[_buffer.Count - 1 - i];
				num -= height;
				fontRenderer.RenderString(text, new Rectangle(rectanglei.X + 4, num, rectanglei.Width, height), FontAlignment.MiddleY, _font, colour2);
			}
			fontRenderer.RenderString("> " + _currentLine.ToString(), rectanglei2.Inflate(new Vector2i(-4, 0)), FontAlignment.MiddleY, _font, colour2);
		}
	}

	public void WriteLine(string line)
	{
		if (_buffer.Count >= 128)
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
			WriteLine("usage: barrier <classic|bubble|fire|lightning>");
			return;
		}
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
		string text = args[0].ToLower();
		if (!(text == "collision"))
		{
			if (text == "invulnerable")
			{
				level.Player.Protagonist.IsInvincible = !level.Player.Protagonist.IsInvincible;
			}
		}
		else if (level.LayerViewOptions.ShowLandscapeCollision)
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
		if (!level.ObjectManager.RegisteredTypes.Any((ObjectType x) => x.ResourceKey == objectType))
		{
			return;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		for (int num = 1; num < args.Length; num++)
		{
			if (args[num].StartsWith("-") || num >= args.Length - 2)
			{
				string key = args[num].Substring(1);
				object value = ParseBehaviourValue(args[num + 1]);
				dictionary.Add(key, value);
				num++;
			}
		}
		Vector2i position = level.Player.Protagonist.Position;
		LevelLayer item = level.Map.Layers[level.Map.CollisionPathLayers.Last()];
		ObjectPlacement objectPlacement = new ObjectPlacement(objectType, level.Map.Layers.IndexOf(item), position, dictionary);
		level.ObjectManager.AddObject(objectPlacement);
	}

	private static object ParseBehaviourValue(string value)
	{
		if (int.TryParse(value, out var result))
		{
			return result;
		}
		if (double.TryParse(value, out var result2))
		{
			return result2;
		}
		if (bool.TryParse(value, out var result3))
		{
			return result3;
		}
		return value;
	}

	private void ProcessCommandTeleport(string[] args)
	{
		if (LevelGameState != null && LevelGameState.Level != null)
		{
			Level level = LevelGameState.Level;
			if (args.Length >= 2 && int.TryParse(args[0], out var result) && int.TryParse(args[1], out var result2))
			{
				level.Player.Protagonist.Position = new Vector2i(result, result2);
			}
		}
	}

	private void ProcessCommandRestart(string[] args)
	{
		if (LevelGameState != null && LevelGameState.Level != null)
		{
			Level level = LevelGameState.Level;
			string text = string.Empty;
			if (args.Length >= 1)
			{
				text = args[0].ToLower();
			}
			if (!(text == "checkpoint") && !(text == "starpost"))
			{
				level.FadeOut(LevelState.Restart);
			}
			else
			{
				level.FadeOut(LevelState.RestartCheckpoint);
			}
		}
	}

	private static string[] GetTokens(string line)
	{
		List<string> list = new List<string>();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (char c in line)
		{
			if (c == '"')
			{
				flag = !flag;
			}
			else if (char.IsWhiteSpace(c) && !flag)
			{
				if (stringBuilder.Length > 0)
				{
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
				}
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		if (stringBuilder.Length > 0)
		{
			list.Add(stringBuilder.ToString());
		}
		return list.ToArray();
	}
}
