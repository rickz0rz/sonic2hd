using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// The Sonic 2 style title card that shows the level name and act at the start of a level.
/// Blue, yellow and red blocks move onto the screen and then move off again.
/// </summary>
internal class Sonic2LevelTitleCard : ILevelTitleCard, IDisposable
{
	private const string LogoResourceKey = "SONICORCA/HUD/TITLECARD/LOGO";

	private const string LogoDarkResourceKey = "SONICORCA/HUD/TITLECARD/LOGODARK";

	private const string TriangleResourceKey = "SONICORCA/HUD/TITLECARD/TRIANGLE";

	private const string PatternAResourceKey = "SONICORCA/HUD/TITLECARD/PATTERNA";

	private const string PatternBResourceKey = "SONICORCA/HUD/TITLECARD/PATTERNB";

	private const string GameResourceKey = "SONICORCA/HUD/TITLECARD/GAME";

	private const string LevelNameFontResourceKey = "SONICORCA/FONTS/TITLE/S2/NAME";

	private const string ActFontResourceKey = "SONICORCA/FONTS/TITLE/S2/ACT";

	// Tick numbers in the title card sequence.
	private const int BlackScreenEndTick = 60;

	private const int SeamlessUnlockCameraTick = 90;

	private const int AllowLevelToStartTick = 120;

	private const int AllowCharacterControlTick = 145;

	private const int FinishedTick = 250;

	private readonly Level _level;

	private ResourceSession _resourceSession;

	private ITexture _logoTexture;

	private ITexture _logoDarkTexture;

	private ITexture _triangleTexture;

	private ITexture _patternATexture;

	private ITexture _patternBTexture;

	private ITexture _gameTexture;

	private Font _levelNameFont;

	private Font _levelActFont;

	private bool _disposed;

	private int _ticks;

	private static readonly EaseTimeline BlueBlockY = new EaseTimeline(new EaseTimeline.Entry(0, 0.0), new EaseTimeline.Entry(18, 1080.0), new EaseTimeline.Entry(146, 1080.0), new EaseTimeline.Entry(156, 0.0));

	private static readonly EaseTimeline YellowBlockX = new EaseTimeline(new EaseTimeline.Entry(12, 1920.0), new EaseTimeline.Entry(42, 0.0), new EaseTimeline.Entry(134, 0.0), new EaseTimeline.Entry(150, 1920.0));

	private static readonly EaseTimeline RedBlockX = new EaseTimeline(new EaseTimeline.Entry(36, 0.0), new EaseTimeline.Entry(50, 808.0), new EaseTimeline.Entry(132, 808.0), new EaseTimeline.Entry(140, 0.0));

	private static readonly EaseTimeline LevelNameX = new EaseTimeline(new EaseTimeline.Entry(32, 3840.0), new EaseTimeline.Entry(72, 1680.0), new EaseTimeline.Entry(190, 1440.0), new EaseTimeline.Entry(210, -16.0));

	private static readonly EaseTimeline ZoneActX = new EaseTimeline(new EaseTimeline.Entry(42, 0.0), new EaseTimeline.Entry(72, 1440.0), new EaseTimeline.Entry(190, 1680.0), new EaseTimeline.Entry(210, 3840.0));

	private static readonly EaseTimeline LevelNameHighlight = new EaseTimeline(new EaseTimeline.Entry(90, 0.0), new EaseTimeline.Entry(100, 255.0), new EaseTimeline.Entry(110, 0.0));

	private static readonly EaseTimeline ZoneActHighlight = new EaseTimeline(new EaseTimeline.Entry(110, 0.0), new EaseTimeline.Entry(120, 255.0), new EaseTimeline.Entry(130, 0.0));

	public bool AllowLevelToStart { get; private set; }

	public bool AllowCharacterControl { get; private set; }

	public bool Finished { get; private set; }

	public bool Seamless { get; set; }

	public bool UnlockCamera { get; set; }

	public Sonic2LevelTitleCard(Level level)
	{
		_level = level;
	}

	public async Task LoadAsync(ResourceTree resourceTree, CancellationToken ct = default(CancellationToken))
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("Sonic2LevelTitleCard");
		}
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies(LogoResourceKey, LogoDarkResourceKey, TriangleResourceKey, PatternAResourceKey, PatternBResourceKey, GameResourceKey, LevelNameFontResourceKey, ActFontResourceKey);
		await _resourceSession.LoadAsync(ct);
		_logoTexture = resourceTree.GetLoadedResource<ITexture>(LogoResourceKey);
		_logoDarkTexture = resourceTree.GetLoadedResource<ITexture>(LogoDarkResourceKey);
		_triangleTexture = resourceTree.GetLoadedResource<ITexture>(TriangleResourceKey);
		_patternATexture = resourceTree.GetLoadedResource<ITexture>(PatternAResourceKey);
		_patternBTexture = resourceTree.GetLoadedResource<ITexture>(PatternBResourceKey);
		_gameTexture = resourceTree.GetLoadedResource<ITexture>(GameResourceKey);
		_levelNameFont = resourceTree.GetLoadedResource<Font>(LevelNameFontResourceKey);
		_levelActFont = resourceTree.GetLoadedResource<Font>(ActFontResourceKey);
	}

	public void Dispose()
	{
		if (_resourceSession != null)
		{
			_resourceSession.Unload();
		}
		_disposed = true;
	}

	public void Start()
	{
		_ticks = 0;
		AllowLevelToStart = false;
		AllowCharacterControl = false;
		UnlockCamera = false;
		Finished = false;
	}

	public void Update()
	{
		if (Finished)
		{
			return;
		}
		if (Seamless)
		{
			AllowLevelToStart = true;
			if (_ticks >= SeamlessUnlockCameraTick)
			{
				UnlockCamera = true;
			}
		}
		if (_ticks >= AllowLevelToStartTick)
		{
			AllowLevelToStart = true;
		}
		if (_ticks >= AllowCharacterControlTick)
		{
			AllowCharacterControl = true;
		}
		if (_ticks >= FinishedTick)
		{
			Finished = true;
		}
		_ticks++;
	}

	public void Draw(Renderer renderer)
	{
		if (!Finished)
		{
			if (_ticks < BlackScreenEndTick && !Seamless)
			{
				renderer.Get2dRenderer().RenderQuad(Colours.Black, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
			}
			if (!Seamless)
			{
				DrawBlueBlock(renderer);
				DrawYellowBlock(renderer);
				DrawRedBlock(renderer);
			}
			else
			{
				// A seamless title card shows over the level that is already visible, so it uses smaller blocks.
				DrawBlueBlockSeamless(renderer);
				DrawYellowBlockSeamless(renderer);
				DrawRedBlockSeamless(renderer);
			}
			DrawLevelName(renderer);
			DrawZoneAct(renderer);
		}
	}

	private void DrawBlueBlock(Renderer renderer)
	{
		int bottom = (int)BlueBlockY.GetValueAt(_ticks);
		int triangleScroll = 63 - _ticks % 64;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		renderer2d.RenderQuad(new Colour(0, 61, 155), new Rectangle(1502.0, bottom - 1080, 418.0, 1080.0));
		renderer2d.RenderQuad(new Colour(0, 80, 170), new Rectangle(0.0, bottom - 1080, 1502.0, 1080.0));
		renderer2d.Colour = new Colour(0, 80, 170);
		using (renderer2d.BeginMatixState())
		{
			renderer2d.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, bottom);
			for (int i = -1; i < 17; i++)
			{
				renderer2d.RenderTexture(_triangleTexture, new Rectangle(1502.0, bottom - 1080 + i * 64 + triangleScroll, 32.0, 64.0));
			}
		}
		renderer2d.Colour = Colours.White;
		renderer2d.RenderTexture(_logoTexture, new Vector2(1466.0, bottom - 734));
	}

	private void DrawYellowBlock(Renderer renderer)
	{
		int left = (int)YellowBlockX.GetValueAt(_ticks);
		int patternAScroll = 127 - _ticks % 128;
		int patternBScroll = _ticks % 128;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		renderer2d.RenderQuad(new Colour(byte.MaxValue, byte.MaxValue, 0), new Rectangle(left, 736.0, 1920.0, 344.0));
		using (renderer2d.BeginMatixState())
		{
			renderer2d.ClipRectangle = new Rectangle(left, 736.0, 1920 - left, 344.0);
			renderer2d.Colour = Colours.White;
			for (int i = -1; i < 16; i++)
			{
				renderer2d.RenderTexture(_patternATexture, new Rectangle(left + i * 128 + patternAScroll, 736.0, 128.0, 128.0));
			}
			renderer2d.Colour = new Colour(byte.MaxValue, 234, 0);
			for (int i = -1; i < 16; i++)
			{
				renderer2d.RenderTexture(_patternBTexture, new Rectangle(left + i * 128 + patternBScroll, 892.0, 128.0, 128.0));
			}
			renderer2d.Colour = Colours.White;
			renderer2d.RenderTexture(_gameTexture, new Vector2(left + 1347, 800.0));
		}
	}

	private void DrawRedBlock(Renderer renderer)
	{
		int offset = (int)RedBlockX.GetValueAt(_ticks);
		// Each column of triangles scrolls at a different speed.
		int largeTriangleScroll = _ticks * 4 % 256;
		int mediumTriangleScroll = _ticks * 2 % 128;
		int smallTriangleScroll = _ticks % 64;
		int left = offset - 808;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		renderer2d.RenderQuad(new Colour(224, 0, 0), new Rectangle(left + 454, 0.0, 320.0, 1080.0));
		renderer2d.RenderQuad(new Colour(191, 0, 0), new Rectangle(left + 126, 0.0, 328.0, 1080.0));
		renderer2d.RenderQuad(new Colour(168, 0, 0), new Rectangle(left, 0.0, 126.0, 1080.0));
		for (int i = -1; i < 17; i++)
		{
			renderer2d.Colour = Colours.Black;
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 774, i * 64 + 16 + smallTriangleScroll, 32.0, 64.0));
			renderer2d.Colour = new Colour(224, 0, 0);
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 774, i * 64 + smallTriangleScroll, 32.0, 64.0));
		}
		renderer2d.Colour = new Colour(191, 0, 0);
		for (int i = -1; i < 9; i++)
		{
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 454, i * 128 + mediumTriangleScroll, 64.0, 128.0));
		}
		renderer2d.Colour = new Colour(168, 0, 0);
		for (int i = -1; i < 5; i++)
		{
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 126, i * 256 + largeTriangleScroll, 128.0, 256.0));
		}
	}

	private void DrawBlueBlockSeamless(Renderer renderer)
	{
		int bottom = (int)BlueBlockY.GetValueAt(_ticks);
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		renderer2d.BlendMode = BlendMode.Additive;
		renderer2d.Colour = Colours.White;
		renderer2d.RenderTexture(_logoDarkTexture, new Vector2(1466.0, bottom - 734));
		renderer2d.BlendMode = BlendMode.Alpha;
	}

	private void DrawYellowBlockSeamless(Renderer renderer)
	{
		int left = (int)YellowBlockX.GetValueAt(_ticks) + 600;
		int patternAScroll = 127 - _ticks % 128;
		int patternBScroll = _ticks % 128;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		renderer2d.RenderQuad(Colours.Black, new Rectangle(left, 816.0, 1920.0, 16.0));
		renderer2d.RenderQuad(Colours.Yellow, new Rectangle(left, 736.0, 1920.0, 80.0));
		using (renderer2d.BeginMatixState())
		{
			renderer2d.ClipRectangle = new Rectangle(left, 736.0, 1920 - left, 128.0);
			renderer2d.Colour = Colours.White;
			for (int i = -1; i < 16; i++)
			{
				renderer2d.RenderTexture(_patternATexture, new Rectangle(left + i * 128 + patternAScroll, 718.0, 128.0, 128.0));
			}
			renderer2d.Colour = new Colour(byte.MaxValue, 234, 0);
			for (int i = -1; i < 16; i++)
			{
				renderer2d.RenderTexture(_patternBTexture, new Rectangle(left + i * 128 + patternBScroll, 874.0, 128.0, 128.0));
			}
			renderer2d.Colour = Colours.White;
			renderer2d.RenderTexture(_gameTexture, new Vector2(left + 770, 782.0));
		}
	}

	private void DrawRedBlockSeamless(Renderer renderer)
	{
		int offset = (int)RedBlockX.GetValueAt(_ticks);
		// Each column of triangles scrolls at a different speed or direction.
		int reverseTriangleScroll = 63 - _ticks % 64;
		int mediumTriangleScroll = _ticks * 2 % 128;
		int smallTriangleScroll = _ticks % 64;
		int left = offset - 808;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		renderer2d.RenderQuad(new Colour(224, 0, 0), new Rectangle(left + 454, 0.0, 320.0, 1080.0));
		renderer2d.RenderQuad(new Colour(168, 0, 0), new Rectangle(left + 432, 0.0, 168.0, 1080.0));
		for (int i = -1; i < 17; i++)
		{
			renderer2d.Colour = Colours.Black;
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 774, i * 64 + 16 + smallTriangleScroll, 32.0, 64.0));
			renderer2d.Colour = new Colour(224, 0, 0);
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 774, i * 64 + smallTriangleScroll, 32.0, 64.0));
		}
		renderer2d.Colour = new Colour(168, 0, 0);
		for (int i = -1; i < 9; i++)
		{
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 600, i * 128 + mediumTriangleScroll, 64.0, 128.0));
		}
		for (int i = -1; i < 17; i++)
		{
			renderer2d.Colour = Colours.Black;
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 400, i * 64 + 16 + reverseTriangleScroll, 32.0, 64.0), flipx: true);
			renderer2d.Colour = new Colour(168, 0, 0);
			renderer2d.RenderTexture(_triangleTexture, new Rectangle(left + 400, i * 64 + reverseTriangleScroll, 32.0, 64.0), flipx: true);
		}
	}

	private void DrawLevelName(Renderer renderer)
	{
		int right = (int)LevelNameX.GetValueAt(_ticks);
		byte highlight = (byte)MathX.Clamp(0.0, LevelNameHighlight.GetValueAt(_ticks), 255.0);
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		// The level name is right-aligned to the timeline position.
		string levelName = _level.Name.ToUpper();
		int textWidth = (int)_levelNameFont.MeasureString(levelName, new Rectangle(right, 342.0, 0.0, 0.0), FontAlignment.Left).Width;
		Rectangle boundary = new Rectangle(right - textWidth, 342.0, 0.0, 0.0);
		fontRenderer.RenderStringWithShadow(boundary: boundary, text: levelName, fontAlignment: FontAlignment.Left, font: _levelNameFont, colour: Colours.White, overlay: (highlight > 0) ? null : new int?(0), shadow: _levelNameFont.DefaultShadow, shadowColour: new Colour(highlight, highlight, highlight));
	}

	private void DrawZoneAct(Renderer renderer)
	{
		Rectangle boundary = new Rectangle(ZoneActX.GetValueAt(_ticks) - 16.0, 486.0, 256.0, 256.0);
		byte highlight = (byte)MathX.Clamp(0.0, ZoneActHighlight.GetValueAt(_ticks), 255.0);
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		// The act number and then the word "ZONE" are drawn from right to left.
		if (_level.ShowAsAct)
		{
			boundary.X -= _levelActFont.MeasureString(_level.CurrentAct.ToString(), boundary, FontAlignment.Left).Width;
			fontRenderer.RenderStringWithShadow(_level.CurrentAct.ToString(), boundary, FontAlignment.Left, _levelActFont, Colours.White, 0, _levelActFont.DefaultShadow, new Colour(highlight, highlight, highlight));
			if (highlight > 0)
			{
				fontRenderer.RenderString(_level.CurrentAct.ToString(), boundary, FontAlignment.Left, _levelActFont, new Colour(highlight, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			}
		}
		if (_level.ShowAsZone)
		{
			boundary.X -= _levelNameFont.MeasureString("ZONE", boundary, FontAlignment.Left).Width;
			fontRenderer.RenderStringWithShadow("ZONE", boundary, FontAlignment.Left, _levelNameFont, Colours.White, (highlight > 0) ? null : new int?(0), _levelNameFont.DefaultShadow, new Colour(highlight, highlight, highlight));
		}
	}
}
