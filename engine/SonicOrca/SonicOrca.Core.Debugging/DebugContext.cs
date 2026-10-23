using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace SonicOrca.Core.Debugging;

/// <summary>
/// The in-level debug menu. It has pages of debug options, reads the arrow keys and draws the menu over the level.
/// </summary>
public class DebugContext : IDisposable
{
	private const string FontResourceKey = "SONICORCA/FONTS/HUD";

	private const string FocusResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";

	// SDL scan codes of the keys that operate the menu.
	private const int ScancodeRight = 79;

	private const int ScancodeLeft = 80;

	private const int ScancodeDown = 81;

	private const int ScancodeUp = 82;

	private const int ScancodeLeftCtrl = 224;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly Level _level;

	private readonly List<DebugPage> _pages = new List<DebugPage>();

	private DebugPage _currentPage;

	private DebugOption _currentOption;

	private ResourceSession _resourceSession;

	private Sample _focusSample;

	public const int DebugTextSilver = 0;

	public const int DebugTextGold = 1;

	public const double DebugTextNormal = 1.0;

	public const double DebugTextSmall = 0.75;

	public const double DebugTextXSmall = 0.5;

	public const double DebugTextXXSmall = 0.25;

	public Font Font { get; private set; }

	public bool Visible { get; private set; }

	public Level Level => _level;

	public DebugOption CurrentOption => _currentOption;

	public DebugContext(Level level)
	{
		_gameContext = level.GameContext;
		_level = level;
		_pages.AddRange(from option in DebugOptionDefinitions.CreateOptionsInOrder(this)
						group option by option.Page into pageOptions
						select new DebugPage(this, pageOptions.Key, pageOptions));
		if (_pages.Count > 0)
		{
			SelectPage(_pages.First());
		}
	}

	public async Task LoadAsync(CancellationToken ct = default)
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies(FontResourceKey, FocusResourceKey);
		await _resourceSession.LoadAsync(ct);
		Font = resourceTree.GetLoadedResource<Font>(FontResourceKey);
		_focusSample = resourceTree.GetLoadedResource<Sample>(FocusResourceKey);
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
		if (!_gameContext.Console.IsOpen)
		{
			HandleInput();
		}
	}

	private void SelectPage(DebugPage page)
	{
		_currentPage = page;
		_currentOption = _currentPage.Options.FirstOrDefault(x => x.Selectable);
	}

	private void HandleInput()
	{
		KeyboardState keyboard = _gameContext.Input.Pressed.Keyboard;
		if (Visible)
		{
			if (keyboard[ScancodeLeft])
			{
				OnPressLeft();
			}
			else if (keyboard[ScancodeRight])
			{
				OnPressRight();
			}
			if (keyboard[ScancodeUp])
			{
				OnPressUp();
			}
			else if (keyboard[ScancodeDown])
			{
				OnPressDown();
			}
		}
	}

	/// <summary>
	/// With Ctrl, selects the previous page. Without Ctrl, sends the key to the current option.
	/// </summary>
	private void OnPressLeft()
	{
		if (_gameContext.Input.CurrentState.Keyboard[ScancodeLeftCtrl])
		{
			int pageIndex = _pages.IndexOf(_currentPage);
			if (pageIndex > 0)
			{
				SelectPage(_pages[pageIndex - 1]);
				PlayFocusSound();
			}
		}
		else if (_currentOption != null)
		{
			_currentOption.OnPressLeft();
		}
	}

	/// <summary>
	/// With Ctrl, selects the next page. Without Ctrl, sends the key to the current option.
	/// </summary>
	private void OnPressRight()
	{
		if (_gameContext.Input.CurrentState.Keyboard[ScancodeLeftCtrl])
		{
			int pageIndex = _pages.IndexOf(_currentPage);
			if (pageIndex < _pages.Count - 1)
			{
				SelectPage(_pages[pageIndex + 1]);
				PlayFocusSound();
			}
		}
		else if (_currentOption != null)
		{
			_currentOption.OnPressRight();
		}
	}

	// OnPressUp and OnPressDown always read the option next to the current option, not the option at the loop index.
	// They also do not stop at the first selectable option, so the focus sound can play more than one time.
	// This is possibly a bug in the original code.
	private void OnPressUp()
	{
		if (_currentPage.Options.Count == 0)
		{
			return;
		}
		int currentIndex = _currentPage.Options.IndexOf(_currentOption);
		for (int i = currentIndex - 1; i >= 0; i--)
		{
			DebugOption option = _currentPage.Options[currentIndex - 1];
			if (option.Selectable)
			{
				_currentOption = option;
				PlayFocusSound();
			}
		}
	}

	private void OnPressDown()
	{
		if (_currentPage.Options.Count == 0)
		{
			return;
		}
		int currentIndex = _currentPage.Options.IndexOf(_currentOption);
		for (int i = currentIndex + 1; i < _currentPage.Options.Count; i++)
		{
			DebugOption option = _currentPage.Options[currentIndex + 1];
			if (option.Selectable)
			{
				_currentOption = option;
				PlayFocusSound();
			}
		}
	}

	public void Draw(Renderer renderer)
	{
		if (!Visible)
		{
			return;
		}
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.BlendMode = BlendMode.Alpha;
		i2dRenderer.RenderQuad(new Colour(192, 0, 0, 0), new Rectangle(8.0, 8.0, 1904.0, 1064.0));
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(16.0, 16.0);
			i2dRenderer.ClipRectangle = new Rectangle(16.0, 16.0, 1888.0, 1048.0);
			DrawPageTabs(renderer);
			i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(0.0, Font.Height + 32);
			if (_currentPage != null)
			{
				_currentPage.Draw(renderer);
			}
		}
	}

	private void DrawPageTabs(Renderer renderer)
	{
		int x = 0;
		foreach (DebugPage page in _pages)
		{
			DrawText(renderer, page.Name, FontAlignment.Left, x, 0.0, DebugTextNormal, (_currentPage == page) ? DebugTextGold : DebugTextSilver);
			x += (int)Font.MeasureString(page.Name).Width + 64;
		}
	}

	public double DrawText(Renderer renderer, string text, FontAlignment alignment, double x, double y, double scale, int? overlay)
	{
		return DrawText(renderer, text, alignment, x, y, scale, overlay, Colours.White);
	}

	public double DrawText(Renderer renderer, string text, FontAlignment alignment, double x, double y, double scale, Colour colour)
	{
		return DrawText(renderer, text, alignment, x, y, scale, null, colour);
	}

	/// <summary>
	/// Draws a line of text with a shadow and returns the height of the line, with spacing.
	/// </summary>
	public double DrawText(Renderer renderer, string text, FontAlignment alignment, double x, double y, double scale, int? overlay, Colour colour)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		using (i2dRenderer.BeginMatixState())
		{
			i2dRenderer.ModelMatrix = (i2dRenderer.ModelMatrix * Matrix4.CreateScale(scale, scale)).Translate(x, y);
			renderer.GetFontRenderer().RenderStringWithShadow(text, default, alignment, Font, colour, overlay);
		}
		return Font.Height * scale + 8.0;
	}

	public void PlayFocusSound()
	{
		_gameContext.Audio.PlaySound(_focusSample);
	}
}
