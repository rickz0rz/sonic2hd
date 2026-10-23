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

public class DebugContext : IDisposable
{
	private const string FontResourceKey = "SONICORCA/FONTS/HUD";

	private const string FocusResourceKey = "SONICORCA/SOUND/TALLY/SWITCH";

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
		_pages.AddRange(from x in DebugOptionDefinitions.CreateOptionsInOrder(this)
						group x by x.Page into x
						select new DebugPage(this, x.Key, x));
		if (_pages.Count > 0)
		{
			SelectPage(_pages.First());
		}
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependencies("SONICORCA/FONTS/HUD", "SONICORCA/SOUND/TALLY/SWITCH");
		await _resourceSession.LoadAsync(ct);
		Font = resourceTree.GetLoadedResource<Font>("SONICORCA/FONTS/HUD");
		_focusSample = resourceTree.GetLoadedResource<Sample>("SONICORCA/SOUND/TALLY/SWITCH");
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
		_currentOption = _currentPage.Options.FirstOrDefault((DebugOption x) => x.Selectable);
	}

	private void HandleInput()
	{
		KeyboardState keyboard = _gameContext.Input.Pressed.Keyboard;
		if (Visible)
		{
			if (keyboard[80])
			{
				OnPressLeft();
			}
			else if (keyboard[79])
			{
				OnPressRight();
			}
			if (keyboard[82])
			{
				OnPressUp();
			}
			else if (keyboard[81])
			{
				OnPressDown();
			}
		}
	}

	private void OnPressLeft()
	{
		if (_gameContext.Input.CurrentState.Keyboard[224])
		{
			int num = _pages.IndexOf(_currentPage);
			if (num > 0)
			{
				SelectPage(_pages[num - 1]);
				PlayFocusSound();
			}
		}
		else if (_currentOption != null)
		{
			_currentOption.OnPressLeft();
		}
	}

	private void OnPressRight()
	{
		if (_gameContext.Input.CurrentState.Keyboard[224])
		{
			int num = _pages.IndexOf(_currentPage);
			if (num < _pages.Count - 1)
			{
				SelectPage(_pages[num + 1]);
				PlayFocusSound();
			}
		}
		else if (_currentOption != null)
		{
			_currentOption.OnPressRight();
		}
	}

	private void OnPressUp()
	{
		if (_currentPage.Options.Count == 0)
		{
			return;
		}
		int num = _currentPage.Options.IndexOf(_currentOption);
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			DebugOption debugOption = _currentPage.Options[num - 1];
			if (debugOption.Selectable)
			{
				_currentOption = debugOption;
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
		int num = _currentPage.Options.IndexOf(_currentOption);
		for (int i = num + 1; i < _currentPage.Options.Count; i++)
		{
			DebugOption debugOption = _currentPage.Options[num + 1];
			if (debugOption.Selectable)
			{
				_currentOption = debugOption;
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
		int num = 0;
		foreach (DebugPage page in _pages)
		{
			DrawText(renderer, page.Name, FontAlignment.Left, num, 0.0, 1.0, (_currentPage == page) ? 1 : 0);
			num += (int)Font.MeasureString(page.Name).Width + 64;
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
