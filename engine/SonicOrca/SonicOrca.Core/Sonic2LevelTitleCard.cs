using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

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
		_resourceSession.PushDependencies("SONICORCA/HUD/TITLECARD/LOGO", "SONICORCA/HUD/TITLECARD/LOGODARK", "SONICORCA/HUD/TITLECARD/TRIANGLE", "SONICORCA/HUD/TITLECARD/PATTERNA", "SONICORCA/HUD/TITLECARD/PATTERNB", "SONICORCA/HUD/TITLECARD/GAME", "SONICORCA/FONTS/TITLE/S2/NAME", "SONICORCA/FONTS/TITLE/S2/ACT");
		await _resourceSession.LoadAsync(ct);
		_logoTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TITLECARD/LOGO");
		_logoDarkTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TITLECARD/LOGODARK");
		_triangleTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TITLECARD/TRIANGLE");
		_patternATexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TITLECARD/PATTERNA");
		_patternBTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TITLECARD/PATTERNB");
		_gameTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TITLECARD/GAME");
		_levelNameFont = resourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/TITLE/S2/NAME");
		_levelActFont = resourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/TITLE/S2/ACT");
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
			if (_ticks >= 90)
			{
				UnlockCamera = true;
			}
		}
		if (_ticks >= 120)
		{
			AllowLevelToStart = true;
		}
		if (_ticks >= 145)
		{
			AllowCharacterControl = true;
		}
		if (_ticks >= 250)
		{
			Finished = true;
		}
		_ticks++;
	}

	public void Draw(Renderer renderer)
	{
		if (!Finished)
		{
			if (_ticks < 60 && !Seamless)
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
		int num = (int)BlueBlockY.GetValueAt(_ticks);
		int num2 = 63 - _ticks % 64;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.RenderQuad(new Colour(0, 61, 155), new Rectangle(1502.0, num - 1080, 418.0, 1080.0));
		i2dRenderer.RenderQuad(new Colour(0, 80, 170), new Rectangle(0.0, num - 1080, 1502.0, 1080.0));
		i2dRenderer.Colour = new Colour(0, 80, 170);
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, num);
			for (int i = -1; i < 17; i++)
			{
				i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(1502.0, num - 1080 + i * 64 + num2, 32.0, 64.0));
			}
		}
		i2dRenderer.Colour = Colours.White;
		i2dRenderer.RenderTexture(_logoTexture, new Vector2(1466.0, num - 734));
	}

	private void DrawYellowBlock(Renderer renderer)
	{
		int num = (int)YellowBlockX.GetValueAt(_ticks);
		int num2 = 127 - _ticks % 128;
		int num3 = _ticks % 128;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.RenderQuad(new Colour(byte.MaxValue, byte.MaxValue, 0), new Rectangle(num, 736.0, 1920.0, 344.0));
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ClipRectangle = new Rectangle(num, 736.0, 1920 - num, 344.0);
			i2dRenderer.Colour = Colours.White;
			for (int i = -1; i < 16; i++)
			{
				i2dRenderer.RenderTexture(_patternATexture, new Rectangle(num + i * 128 + num2, 736.0, 128.0, 128.0));
			}
			i2dRenderer.Colour = new Colour(byte.MaxValue, 234, 0);
			for (int j = -1; j < 16; j++)
			{
				i2dRenderer.RenderTexture(_patternBTexture, new Rectangle(num + j * 128 + num3, 892.0, 128.0, 128.0));
			}
			i2dRenderer.Colour = Colours.White;
			i2dRenderer.RenderTexture(_gameTexture, new Vector2(num + 1347, 800.0));
		}
	}

	private void DrawRedBlock(Renderer renderer)
	{
		int num = (int)RedBlockX.GetValueAt(_ticks);
		int[] array = new int[3]
		{
			_ticks * 4 % 256,
			_ticks * 2 % 128,
			_ticks % 64
		};
		int num2 = num - 808;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.RenderQuad(new Colour(224, 0, 0), new Rectangle(num2 + 454, 0.0, 320.0, 1080.0));
		i2dRenderer.RenderQuad(new Colour(191, 0, 0), new Rectangle(num2 + 126, 0.0, 328.0, 1080.0));
		i2dRenderer.RenderQuad(new Colour(168, 0, 0), new Rectangle(num2, 0.0, 126.0, 1080.0));
		for (int i = -1; i < 17; i++)
		{
			i2dRenderer.Colour = Colours.Black;
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 774, i * 64 + 16 + array[2], 32.0, 64.0));
			i2dRenderer.Colour = new Colour(224, 0, 0);
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 774, i * 64 + array[2], 32.0, 64.0));
		}
		i2dRenderer.Colour = new Colour(191, 0, 0);
		for (int j = -1; j < 9; j++)
		{
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 454, j * 128 + array[1], 64.0, 128.0));
		}
		i2dRenderer.Colour = new Colour(168, 0, 0);
		for (int k = -1; k < 5; k++)
		{
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 126, k * 256 + array[0], 128.0, 256.0));
		}
	}

	private void DrawBlueBlockSeamless(Renderer renderer)
	{
		int num = (int)BlueBlockY.GetValueAt(_ticks);
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.BlendMode = BlendMode.Additive;
		i2dRenderer.Colour = Colours.White;
		i2dRenderer.RenderTexture(_logoDarkTexture, new Vector2(1466.0, num - 734));
		i2dRenderer.BlendMode = BlendMode.Alpha;
	}

	private void DrawYellowBlockSeamless(Renderer renderer)
	{
		int num = (int)YellowBlockX.GetValueAt(_ticks) + 600;
		int num2 = 127 - _ticks % 128;
		int num3 = _ticks % 128;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.RenderQuad(Colours.Black, new Rectangle(num, 816.0, 1920.0, 16.0));
		i2dRenderer.RenderQuad(Colours.Yellow, new Rectangle(num, 736.0, 1920.0, 80.0));
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ClipRectangle = new Rectangle(num, 736.0, 1920 - num, 128.0);
			i2dRenderer.Colour = Colours.White;
			for (int i = -1; i < 16; i++)
			{
				i2dRenderer.RenderTexture(_patternATexture, new Rectangle(num + i * 128 + num2, 718.0, 128.0, 128.0));
			}
			i2dRenderer.Colour = new Colour(byte.MaxValue, 234, 0);
			for (int j = -1; j < 16; j++)
			{
				i2dRenderer.RenderTexture(_patternBTexture, new Rectangle(num + j * 128 + num3, 874.0, 128.0, 128.0));
			}
			i2dRenderer.Colour = Colours.White;
			i2dRenderer.RenderTexture(_gameTexture, new Vector2(num + 770, 782.0));
		}
	}

	private void DrawRedBlockSeamless(Renderer renderer)
	{
		int num = (int)RedBlockX.GetValueAt(_ticks);
		int[] array = new int[3]
		{
			63 - _ticks % 64,
			_ticks * 2 % 128,
			_ticks % 64
		};
		int num2 = num - 808;
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.RenderQuad(new Colour(224, 0, 0), new Rectangle(num2 + 454, 0.0, 320.0, 1080.0));
		i2dRenderer.RenderQuad(new Colour(168, 0, 0), new Rectangle(num2 + 432, 0.0, 168.0, 1080.0));
		for (int i = -1; i < 17; i++)
		{
			i2dRenderer.Colour = Colours.Black;
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 774, i * 64 + 16 + array[2], 32.0, 64.0));
			i2dRenderer.Colour = new Colour(224, 0, 0);
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 774, i * 64 + array[2], 32.0, 64.0));
		}
		i2dRenderer.Colour = new Colour(168, 0, 0);
		for (int j = -1; j < 9; j++)
		{
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 600, j * 128 + array[1], 64.0, 128.0));
		}
		for (int k = -1; k < 17; k++)
		{
			i2dRenderer.Colour = Colours.Black;
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 400, k * 64 + 16 + array[0], 32.0, 64.0), flipx: true);
			i2dRenderer.Colour = new Colour(168, 0, 0);
			i2dRenderer.RenderTexture(_triangleTexture, new Rectangle(num2 + 400, k * 64 + array[0], 32.0, 64.0), flipx: true);
		}
	}

	private void DrawLevelName(Renderer renderer)
	{
		int num = (int)LevelNameX.GetValueAt(_ticks);
		byte b = (byte)MathX.Clamp(0.0, LevelNameHighlight.GetValueAt(_ticks), 255.0);
		renderer.GetFontRenderer().RenderStringWithShadow(boundary: new Rectangle(num - (int)_levelNameFont.MeasureString(_level.Name.ToUpper(), new Rectangle(num, 342.0, 0.0, 0.0), FontAlignment.Left).Width, 342.0, 0.0, 0.0), text: _level.Name.ToUpper(), fontAlignment: FontAlignment.Left, font: _levelNameFont, colour: Colours.White, overlay: (b > 0) ? ((int?)null) : new int?(0), shadow: _levelNameFont.DefaultShadow, shadowColour: new Colour(b, b, b));
	}

	private void DrawZoneAct(Renderer renderer)
	{
		Rectangle boundary = new Rectangle(ZoneActX.GetValueAt(_ticks) - 16.0, 486.0, 256.0, 256.0);
		byte b = (byte)MathX.Clamp(0.0, ZoneActHighlight.GetValueAt(_ticks), 255.0);
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		if (_level.ShowAsAct)
		{
			boundary.X -= _levelActFont.MeasureString(_level.CurrentAct.ToString(), boundary, FontAlignment.Left).Width;
			fontRenderer.RenderStringWithShadow(_level.CurrentAct.ToString(), boundary, FontAlignment.Left, _levelActFont, Colours.White, 0, _levelActFont.DefaultShadow, new Colour(b, b, b));
			if (b > 0)
			{
				fontRenderer.RenderString(_level.CurrentAct.ToString(), boundary, FontAlignment.Left, _levelActFont, new Colour(b, byte.MaxValue, byte.MaxValue, byte.MaxValue));
			}
		}
		if (_level.ShowAsZone)
		{
			boundary.X -= _levelNameFont.MeasureString("ZONE", boundary, FontAlignment.Left).Width;
			fontRenderer.RenderStringWithShadow("ZONE", boundary, FontAlignment.Left, _levelNameFont, Colours.White, (b > 0) ? ((int?)null) : new int?(0), _levelNameFont.DefaultShadow, new Colour(b, b, b));
		}
	}
}
