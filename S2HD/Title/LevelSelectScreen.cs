using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Menu;
using SonicOrca.Resources;

namespace S2HD.Title;

/// <summary>
/// The level select: a screen of its own that the code on the title screen opens. The zones are in two columns, with
/// the acts of each zone after its name. A panel shows the selected zone, and a sound test plays the music of the
/// game. The background is rows of the names of the characters, which change from one name to the other.
/// </summary>
internal sealed class LevelSelectScreen : Screen
{
	private const string FontResourceKey = "SONICORCA/FONTS/IMPACT/REGULAR";
	private const string CursorSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/CURSOR";
	private const string YesSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/YES";
	private const string BackSoundResourceKey = "SONICORCA/SOUND/NAVIGATE/BACK";
	private const string MusicFolder = "SONICORCA/MUSIC/";

	private const int EscapeScancode = 41;

	/// <summary>The number of zones in one column. The other zones go in the second column.</summary>
	private const int RowsPerColumn = 9;

	private const double RowHeight = 62.0;

	/// <summary>The ticks of one name of the background, and the ticks of the change to the other name.</summary>
	private const int NameTicks = 150;

	private const int NameFadeTicks = 30;

	private static readonly string[] BackgroundNames = { "SONIC", "TAILS" };

	private static readonly Colour TextColour = new Colour(1.0, 0.75, 0.85, 1.0);

	private static readonly Colour SelectedColour = new Colour(1.0, 1.0, 0.85, 0.2);

	private static readonly Colour PanelColour = new Colour(0.72, 0.04, 0.08, 0.22);

	private readonly SonicOrcaGameContext _gameContext;

	private readonly ResourceSession _resourceSession;

	private readonly LevelPrepareSettings _characters;

	private readonly List<Zone> _zones = new List<Zone>();

	private string[] _music = Array.Empty<string>();

	private Font _font;

	private Sample _cursorSound;

	private Sample _yesSound;

	private Sample _backSound;

	/// <summary>The selected zone, or the sound test when it is the number of zones.</summary>
	private int _zoneIndex;

	private int _act = 1;

	private int _musicIndex;

	private ResourceSession _musicSession;

	private Task _musicLoad;

	private string _musicLoadKey;

	private Sample _playingMusic;

	private int _ticks;

	/// <summary>The level to play, or null when the player went back to the title screen.</summary>
	public LevelPrepareSettings Result { get; private set; }

	/// <summary>A zone of the list: its name, its acts, and the area and number for the level settings.</summary>
	private sealed class Zone
	{
		public string Name;

		public string Mnemonic;

		public int Acts;

		/// <summary>The number of the level in the story, or 0 for a level that is not in the story (a mod level).</summary>
		public int Number;

		public bool IsMod;
	}

	/// <param name="characters">The characters of the levels that the player selects.</param>
	public LevelSelectScreen(SonicOrcaGameContext gameContext, LevelPrepareSettings characters)
	{
		_gameContext = gameContext;
		_characters = characters;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
	}

	public override async Task LoadAsync(ScreenLoadingProgress progress, CancellationToken ct = default)
	{
		foreach (string key in new[] { FontResourceKey, CursorSoundResourceKey, YesSoundResourceKey, BackSoundResourceKey })
		{
			_resourceSession.PushDependency(key);
		}
		await _resourceSession.LoadAsync(ct);
		ResourceTree tree = _gameContext.ResourceTree;
		_font = tree.GetLoadedResource<Font>(FontResourceKey);
		_cursorSound = tree.GetLoadedResource<Sample>(CursorSoundResourceKey);
		_yesSound = tree.GetLoadedResource<Sample>(YesSoundResourceKey);
		_backSound = tree.GetLoadedResource<Sample>(BackSoundResourceKey);
		int number = 1;
		foreach (LevelInfo level in Levels.GetAvailableLevels(tree))
		{
			_zones.Add(new Zone { Name = level.Name.ToUpperInvariant(), Mnemonic = level.Mnemonic, Acts = level.Acts, Number = number++ });
		}
		foreach (LevelInfo level in Levels.GetModLevels(tree))
		{
			_zones.Add(new Zone { Name = level.Name.ToUpperInvariant(), Mnemonic = level.Mnemonic, Acts = level.Acts, Number = 0, IsMod = true });
		}
		// The sound test plays each music resource of the game and of the mods, in the order of its key
		_music = tree.GetResourceListing()
			.Where(x => x.Key.StartsWith(MusicFolder, StringComparison.Ordinal) && x.Value.Identifier == ResourceTypeIdentifier.SampleInfo)
			.Select(x => x.Key)
			.OrderBy(x => x, StringComparer.Ordinal)
			.ToArray();
	}

	public override void Update()
	{
		_ticks++;
		UpdateMusic();
		Controller pressed = _gameContext.Pressed[0];
		InputState input = _gameContext.Input.Pressed;
		if (input.Keyboard[EscapeScancode] || input.GamePad[0].Select || input.GamePad[0].East)
		{
			_gameContext.Audio.PlaySound(_backSound);
			Finish();
			return;
		}
		bool soundTest = _zoneIndex == _zones.Count;
		if (pressed.DirectionLeft.Y <= -0.5 || pressed.DirectionLeft.Y >= 0.5)
		{
			int count = _zones.Count + 1;
			_zoneIndex = (_zoneIndex + (pressed.DirectionLeft.Y < 0 ? count - 1 : 1)) % count;
			_act = 1;
			_gameContext.Audio.PlaySound(_cursorSound);
		}
		else if (pressed.DirectionLeft.X <= -0.5 || pressed.DirectionLeft.X >= 0.5)
		{
			MoveSideways(pressed.DirectionLeft.X < 0 ? -1 : 1);
			_gameContext.Audio.PlaySound(_cursorSound);
		}
		else if (pressed.Start || pressed.Action1)
		{
			_gameContext.Audio.PlaySound(_yesSound);
			if (soundTest)
			{
				PlayMusic(_music.Length > 0 ? _music[_musicIndex] : null);
			}
			else
			{
				SelectLevel(_zones[_zoneIndex], _act);
			}
		}
	}

	/// <summary>
	/// Left and right change the act of the selected zone. Past the first or the last act, the selection goes to the
	/// zone in the same row of the other column. On the sound test, they change the music.
	/// </summary>
	private void MoveSideways(int direction)
	{
		if (_zoneIndex == _zones.Count)
		{
			if (_music.Length > 0)
			{
				_musicIndex = (_musicIndex + direction + _music.Length) % _music.Length;
			}
			return;
		}
		int act = _act + direction;
		if (act >= 1 && act <= _zones[_zoneIndex].Acts)
		{
			_act = act;
			return;
		}
		int row = _zoneIndex % RowsPerColumn;
		int column = _zoneIndex / RowsPerColumn;
		int columns = (_zones.Count + RowsPerColumn - 1) / RowsPerColumn;
		int other = ((column + direction) % columns + columns) % columns * RowsPerColumn + row;
		if (other < _zones.Count && other != _zoneIndex)
		{
			_zoneIndex = other;
			_act = direction > 0 ? 1 : _zones[other].Acts;
		}
	}

	private void SelectLevel(Zone zone, int act)
	{
		Result = new LevelPrepareSettings
		{
			AreaResourceKey = Levels.GetAreaResourceKey(zone.Mnemonic, _gameContext.ResourceTree),
			Act = act,
			LevelNumber = zone.Number,
			ProtagonistCharacter = _characters.ProtagonistCharacter,
			SidekickCharacter = _characters.SidekickCharacter
		};
		Finish();
	}

	#region Sound test

	/// <summary>Loads the music in the background, then plays it. A new choice stops the music that plays.</summary>
	private void PlayMusic(string key)
	{
		StopMusic();
		if (key == null)
		{
			return;
		}
		_musicSession = new ResourceSession(_gameContext.ResourceTree);
		_musicSession.PushDependency(key);
		_musicLoadKey = key;
		_musicLoad = _musicSession.LoadAsync();
	}

	private void UpdateMusic()
	{
		if (_musicLoad == null || !_musicLoad.IsCompleted)
		{
			return;
		}
		if (!_musicLoad.IsFaulted)
		{
			_playingMusic = _gameContext.ResourceTree.GetLoadedResource<Sample>(_musicLoadKey);
			if (_playingMusic != null)
			{
				_gameContext.Audio.PlaySound(_playingMusic);
			}
		}
		_musicLoad = null;
	}

	private void StopMusic()
	{
		_gameContext.Audio.StopAll();
		_playingMusic = null;
		_musicLoad = null;
		_musicSession?.Dispose();
		_musicSession = null;
	}

	#endregion

	#region Drawing

	public override void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour(1.0, 0.05, 0.12, 0.35), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		DrawBackground(fontRenderer);
		fontRenderer.RenderStringWithShadow("LEVEL SELECT", new Rectangle(960.0, 90.0, 0.0, 0.0), FontAlignment.MiddleX | FontAlignment.MiddleY, _font, Colours.White);
		DrawZones(g, fontRenderer);
		DrawPreview(g, fontRenderer);
		DrawSoundTest(g, fontRenderer);
		fontRenderer.RenderString("UP, DOWN: ZONE    LEFT, RIGHT: ACT    START: PLAY    ESC: BACK", new Rectangle(960.0, 1040.0, 0.0, 0.0),
			FontAlignment.MiddleX | FontAlignment.MiddleY, _font, new Colour(0.6, 1.0, 1.0, 1.0));
	}

	/// <summary>
	/// Rows of the names of the characters. The rows move to the left, and the odd rows move in the other direction.
	/// The name changes to the other name after <see cref="NameTicks"/>, with a fade.
	/// </summary>
	private void DrawBackground(IFontRenderer fontRenderer)
	{
		int period = NameTicks + NameFadeTicks;
		int phase = _ticks % period;
		int nameIndex = _ticks / period % BackgroundNames.Length;
		double fade = phase < NameTicks ? 1.0 : 1.0 - (phase - NameTicks) / (double)NameFadeTicks;
		string name = BackgroundNames[nameIndex];
		string next = BackgroundNames[(nameIndex + 1) % BackgroundNames.Length];
		double width = _font.MeasureString(name + "   ").Width;
		for (int row = 0; row < 12; row++)
		{
			double y = 30.0 + row * 92.0;
			double direction = row % 2 == 0 ? -1.0 : 1.0;
			double offset = (_ticks * 0.6 * direction % width + width) % width;
			for (double x = -width + offset; x < 1920.0; x += width)
			{
				fontRenderer.RenderString(name, new Rectangle(x, y, 0.0, 0.0), FontAlignment.MiddleY, _font, new Colour(0.035 * fade, 0.7, 0.85, 1.0));
				if (fade < 1.0)
				{
					fontRenderer.RenderString(next, new Rectangle(x, y, 0.0, 0.0), FontAlignment.MiddleY, _font, new Colour(0.035 * (1.0 - fade), 0.7, 0.85, 1.0));
				}
			}
		}
	}

	private void DrawZones(I2dRenderer g, IFontRenderer fontRenderer)
	{
		for (int i = 0; i < _zones.Count; i++)
		{
			Zone zone = _zones[i];
			int column = i / RowsPerColumn;
			double x = 120.0 + column * 600.0;
			double y = 200.0 + i % RowsPerColumn * RowHeight;
			bool selected = i == _zoneIndex;
			if (selected)
			{
				g.RenderQuad(PanelColour, new Rectangle(x - 16.0, y - RowHeight / 2.0 + 4.0, 580.0, RowHeight - 8.0));
			}
			string name = zone.IsMod ? "* " + zone.Name : zone.Name;
			fontRenderer.RenderStringWithShadow(name, new Rectangle(x, y, 0.0, 0.0), FontAlignment.MiddleY, _font, selected ? SelectedColour : TextColour);
			for (int act = 1; act <= zone.Acts; act++)
			{
				bool actSelected = selected && act == _act;
				fontRenderer.RenderStringWithShadow(act.ToString(), new Rectangle(x + 440.0 + (act - 1) * 40.0, y, 0.0, 0.0), FontAlignment.MiddleY, _font,
					actSelected ? SelectedColour : new Colour(selected ? 1.0 : 0.6, 0.75, 0.85, 1.0));
			}
		}
	}

	/// <summary>A panel with the name of the selected zone and its act, in a color that each zone keeps.</summary>
	private void DrawPreview(I2dRenderer g, IFontRenderer fontRenderer)
	{
		Rectangle panel = new Rectangle(1340.0, 200.0, 480.0, 300.0);
		g.RenderQuad(PanelColour, panel);
		if (_zoneIndex >= _zones.Count)
		{
			fontRenderer.RenderStringWithShadow("SOUND TEST", new Rectangle(panel.X + panel.Width / 2.0, panel.Y + panel.Height / 2.0, 0.0, 0.0),
				FontAlignment.MiddleX | FontAlignment.MiddleY, _font, Colours.White);
			return;
		}
		Zone zone = _zones[_zoneIndex];
		double hue = (zone.Name.Aggregate(0, (h, c) => h * 31 + c) & 0xffff) / 65536.0;
		g.RenderQuad(FromHue(hue, 0.55), new Rectangle(panel.X + 16.0, panel.Y + 16.0, panel.Width - 32.0, 150.0));
		g.RenderQuad(FromHue(hue + 0.08, 0.35), new Rectangle(panel.X + 16.0, panel.Y + 110.0, panel.Width - 32.0, 56.0));
		string[] words = zone.Name.Split(' ');
		string first = string.Join(" ", words.TakeWhile(w => w != "ZONE"));
		fontRenderer.RenderStringWithShadow(first, new Rectangle(panel.X + panel.Width / 2.0, panel.Y + 90.0, 0.0, 0.0), FontAlignment.MiddleX | FontAlignment.MiddleY, _font, Colours.White);
		string detail = zone.IsMod ? $"MOD LEVEL    ACT {_act}" : $"ACT {_act} OF {zone.Acts}";
		fontRenderer.RenderStringWithShadow(detail, new Rectangle(panel.X + panel.Width / 2.0, panel.Y + 230.0, 0.0, 0.0), FontAlignment.MiddleX | FontAlignment.MiddleY, _font, TextColour);
	}

	private void DrawSoundTest(I2dRenderer g, IFontRenderer fontRenderer)
	{
		double y = 200.0 + RowsPerColumn * RowHeight + 40.0;
		bool selected = _zoneIndex == _zones.Count;
		if (selected)
		{
			g.RenderQuad(PanelColour, new Rectangle(104.0, y - RowHeight / 2.0 + 4.0, 1180.0, RowHeight - 8.0));
		}
		string music = _music.Length == 0 ? "NO MUSIC" : _music[_musicIndex].Substring(MusicFolder.Length).Replace('/', ' ');
		string state = _musicLoad != null ? "  (LOADING)" : _playingMusic != null ? "  (PLAYING)" : "";
		fontRenderer.RenderStringWithShadow($"SOUND TEST    {_musicIndex + 1:00}  {music}{state}", new Rectangle(120.0, y, 0.0, 0.0), FontAlignment.MiddleY, _font,
			selected ? SelectedColour : TextColour);
	}

	/// <summary>A color from a hue from 0 to 1, with a fixed saturation and the given lightness.</summary>
	private static Colour FromHue(double hue, double lightness)
	{
		hue = (hue % 1.0 + 1.0) % 1.0 * 6.0;
		double c = (1.0 - Math.Abs(2.0 * lightness - 1.0)) * 0.6;
		double x = c * (1.0 - Math.Abs(hue % 2.0 - 1.0));
		double m = lightness - c / 2.0;
		(double r, double gr, double b) = (int)hue switch
		{
			0 => (c, x, 0.0),
			1 => (x, c, 0.0),
			2 => (0.0, c, x),
			3 => (0.0, x, c),
			4 => (x, 0.0, c),
			_ => (c, 0.0, x)
		};
		return new Colour(1.0, r + m, gr + m, b + m);
	}

	#endregion

	public override void Unload()
	{
		StopMusic();
		_resourceSession.Dispose();
	}
}
