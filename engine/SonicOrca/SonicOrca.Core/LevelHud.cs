using System;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core.Objects;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

internal class LevelHud : IDisposable
{
	private const string HudFontResourceKey = "SONICORCA/FONTS/HUD";

	private const string HudItalicFontResourceKey = "SONICORCA/FONTS/HUDITALIC";

	private const string SonicCheckeredResourceKey = "SONICORCA/HUD/CHECKERED";

	private const string TailsCheckeredResourceKey = "SONICORCA/HUD/CHECKERED/TAILS";

	private const string SonicTriangleResourceKey = "SONICORCA/HUD/TRIANGLE";

	private const string TailsTriangleResourceKey = "SONICORCA/HUD/TRIANGLE/TAILS";

	private const string KnucklesTriangleResourceKey = "SONICORCA/HUD/TRIANGLE/KNUCKLES";

	private const string SonicLifeResourceKey = "SONICORCA/HUD/LIFE/SONIC";

	private const string TailsLifeResourceKey = "SONICORCA/HUD/LIFE/TAILS";

	private const string szScore = "SCORE";

	private const string szTime = "TIME";

	private const string szRings = "RINGS";

	private readonly Level _level;

	private ResourceSession _resourceSession;

	private string _fontResourceKey;

	private Font _font;

	private ITexture _checkeredTextureSonic;

	private ITexture _checkeredTextureTails;

	private ITexture _triangleTextureSonic;

	private ITexture _triangleTextureTails;

	private ITexture _triangleTextureKnuckles;

	private ITexture _lifeTextureSonic;

	private ITexture _lifeTextureTails;

	private double _redAnimation;

	public bool ItalicFont { get; set; }

	public bool ShowMiliseconds { get; set; }

	public LevelHud(Level level)
	{
		_level = level;
		ItalicFont = true;
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _level.GameContext.ResourceTree;
		_fontResourceKey = (ItalicFont ? "SONICORCA/FONTS/HUDITALIC" : "SONICORCA/FONTS/HUD");
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies(_fontResourceKey, "SONICORCA/HUD/CHECKERED", "SONICORCA/HUD/CHECKERED/TAILS", "SONICORCA/HUD/TRIANGLE", "SONICORCA/HUD/TRIANGLE/TAILS", "SONICORCA/HUD/TRIANGLE/KNUCKLES", "SONICORCA/HUD/LIFE/SONIC", "SONICORCA/HUD/LIFE/TAILS");
		await _resourceSession.LoadAsync(ct);
		_font = resourceTree.GetLoadedResource<Font>(_fontResourceKey);
		_checkeredTextureSonic = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/CHECKERED");
		_checkeredTextureTails = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/CHECKERED/TAILS");
		_triangleTextureSonic = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TRIANGLE");
		_triangleTextureTails = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TRIANGLE/TAILS");
		_triangleTextureKnuckles = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/TRIANGLE/KNUCKLES");
		_lifeTextureSonic = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/LIFE/SONIC");
		_lifeTextureTails = resourceTree.GetLoadedResource<ITexture>("SONICORCA/HUD/LIFE/TAILS");
	}

	public void Dispose()
	{
		if (_resourceSession != null)
		{
			_resourceSession.Dispose();
		}
	}

	public void Animate()
	{
		_redAnimation = (_redAnimation + 0.05) % 2.0;
	}

	public void Draw(Renderer renderer)
	{
		ICharacter protagonist = _level.Player.Protagonist;
		if (protagonist != null && protagonist.IsDebug)
		{
			DrawDebug(renderer);
		}
		else
		{
			DrawNormal(renderer);
		}
	}

	private void DrawNormal(Renderer renderer)
	{
		DrawTLInfo(renderer, 226.0, 610.0, 98.0, "SCORE", _level.Player.Score.ToString());
		int num = _level.Time / 60 / 60;
		int num2 = _level.Time / 60 % 60;
		int num3 = _level.Time % 60 * 100 / 60;
		if (ShowMiliseconds)
		{
			DrawTLInfo(renderer, 226.0, 400.0, 162.0, "TIME", $"{num}'{num2:00}\"{num3:00}", num >= 9, rightAligned: false);
		}
		else
		{
			DrawTLInfo(renderer, 226.0, 400.0, 162.0, "TIME", $"{num}:{num2:00}", num >= 9, rightAligned: false);
		}
		DrawTLInfo(renderer, 226.0, 516.0, 226.0, "RINGS", _level.Player.CurrentRings.ToString(), _level.Player.CurrentRings == 0);
		if (_level.Player.Lives >= 0)
		{
			DrawLives(renderer);
		}
	}

	private void DrawDebug(Renderer renderer)
	{
		ICharacter protagonist = _level.Player.Protagonist;
		DrawTLInfoHexCoordinate(renderer, 226.0, 98.0, "PLAYER", protagonist.Position);
		DrawTLInfoHexCoordinate(renderer, 226.0, 162.0, "SCROLL", (Vector2i)_level.Camera.Bounds.TopLeft);
		DrawBottomText(renderer, "DEBUG MODE");
	}

	private void DrawTLInfoHexCoordinate(Renderer renderer, double captionLeft, double captionTop, string caption, Vector2i coordinate)
	{
		DrawTLInfo(renderer, captionLeft, captionLeft + 630.0, captionTop, caption, $"{coordinate.X & 0xFFFFF:X5} {coordinate.Y & 0xFFFFF:X5}");
	}

	private void DrawTLInfo(Renderer renderer, double captionLeft, double valueRight, double top, string caption, string value, bool redAnimate = false, bool rightAligned = true)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		i2dRenderer.BlendMode = BlendMode.Alpha;
		i2dRenderer.Colour = new Colour(96, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		if (_level.Player.ProtagonistCharacterType == CharacterType.Sonic)
		{
			i2dRenderer.RenderTexture(_checkeredTextureSonic, new Vector2(captionLeft - (double)(_checkeredTextureSonic.Width / 2) - 8.0, top + (double)_font.Height / 2.0));
		}
		else
		{
			if (_level.Player.ProtagonistCharacterType != CharacterType.Tails)
			{
				throw new NotImplementedException();
			}
			i2dRenderer.RenderTexture(_checkeredTextureTails, new Vector2(captionLeft - (double)(_checkeredTextureTails.Width / 2) - 8.0, top + (double)_font.Height / 2.0));
		}
		if (rightAligned)
		{
			fontRenderer.RenderStringWithShadow(value, new Rectangle(0.0, top, valueRight, 0.0), FontAlignment.Right, _font, 0);
		}
		else
		{
			fontRenderer.RenderStringWithShadow(value, new Rectangle(valueRight, top, 0.0, 0.0), FontAlignment.Left, _font, 0);
		}
		Rectangle boundary = new Rectangle(captionLeft, top, 0.0, 0.0);
		FontAlignment fontAlignment = FontAlignment.Left;
		Rectangle rectangle = _font.MeasureString(caption, boundary, fontAlignment);
		i2dRenderer.Colour = new Colour(155, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		if (_level.Player.ProtagonistCharacterType == CharacterType.Sonic)
		{
			i2dRenderer.RenderTexture(_triangleTextureSonic, new Vector2(rectangle.Right - 8.0, rectangle.Bottom - 4.0));
		}
		else if (_level.Player.ProtagonistCharacterType == CharacterType.Tails)
		{
			i2dRenderer.RenderTexture(_triangleTextureTails, new Vector2(rectangle.Right - 8.0, rectangle.Bottom - 4.0));
		}
		else
		{
			i2dRenderer.RenderTexture(_triangleTextureKnuckles, new Vector2(rectangle.Right - 8.0, rectangle.Bottom - 4.0));
		}
		Colour colour = Colours.White;
		if (redAnimate)
		{
			double num = ((_redAnimation > 1.0) ? (2.0 - _redAnimation) : _redAnimation);
			colour = new Colour(1.0, 1.0 - num, 1.0 - num);
		}
		fontRenderer.RenderStringWithShadow(caption, new Rectangle(captionLeft, top, 0.0, 0.0), fontAlignment, _font, colour, 1);
	}

	private void DrawLives(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		i2dRenderer.Colour = Colours.White;
		if (_level.Player.ProtagonistCharacterType == CharacterType.Sonic)
		{
			i2dRenderer.RenderTexture(_lifeTextureSonic, new Vector2(264.0, 958.0));
		}
		else
		{
			if (_level.Player.ProtagonistCharacterType != CharacterType.Tails)
			{
				throw new NotImplementedException();
			}
			i2dRenderer.RenderTexture(_lifeTextureTails, new Vector2(264.0, 958.0));
		}
		fontRenderer.RenderStringWithShadow("×", new Rectangle(300.0, 934.0, 0.0, 0.0), FontAlignment.Left, _font, 1);
		fontRenderer.RenderStringWithShadow(_level.Player.Lives.ToString(), new Rectangle(370.0, 934.0, 0.0, 0.0), FontAlignment.Left, _font, 0);
	}

	private void DrawBottomText(Renderer renderer, string text)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		i2dRenderer.Colour = Colours.White;
		if (_level.Player.ProtagonistCharacterType == CharacterType.Sonic)
		{
			i2dRenderer.RenderTexture(_lifeTextureSonic, new Vector2(264.0, 958.0));
		}
		else
		{
			if (_level.Player.ProtagonistCharacterType != CharacterType.Tails)
			{
				throw new NotImplementedException();
			}
			i2dRenderer.RenderTexture(_lifeTextureTails, new Vector2(264.0, 958.0));
		}
		fontRenderer.RenderStringWithShadow(text, new Rectangle(310.0, 934.0, 0.0, 0.0), FontAlignment.Left, _font, 1);
	}
}
