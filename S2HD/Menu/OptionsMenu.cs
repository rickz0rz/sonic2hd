// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.OptionsMenu
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Drawing.Renderers;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.V2.Animation;
using SonicOrca.Resources;

#nullable disable
namespace S2HD.Menu;

internal class OptionsMenu : IDisposable
{
	[ResourcePath("SONICORCA/MENU/OPTIONS/CIRCLE")]
	private ITexture _circleTexture;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/PAUSE")]
	private CompositionGroup _menuPopInCompositionGroup;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/EXITPAUSE")]
	private CompositionGroup _menuPopOutCompositionGroup;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/AUDIO")]
	private CompositionGroup _subMenuAudioPopInCompositionGroup;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/EXITAUDIO")]
	private CompositionGroup _subMenuAudioPopOutCompositionGroup;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/VIDEO")]
	private CompositionGroup _subMenuVideoPopInCompositionGroup;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/EXITVIDEO")]
	private CompositionGroup _subMenuVideoPopOutCompositionGroup;
	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/OPTIONS")]
	private CompositionGroup _subMenuOptionsPopInCompositionGroup;
	private CompositionInstance _menuPopInComposition;
	private CompositionInstance _menuPopOutComposition;
	private CompositionInstance _subMenuAudioPopInComposition;
	private CompositionInstance _subMenuAudioPopOutComposition;
	private CompositionInstance _subMenuVideoPopInComposition;
	private CompositionInstance _subMenuVideoPopOutComposition;
	private CompositionInstance _subMenuOptionsPopInComposition;
	private CompositionInstance _currentComposition;
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly SettingUIResources _settingUIResources;
	private ResourceSession _resourceSession;
	private bool _loaded;
	private bool _initialised;
	private readonly IGraphicsContext _graphicsContext;
	private float _circleSize;
	private Rectanglei _circleBounds;
	private bool _showing;
	private readonly MenuViewPresenterFactory _viewPresenterFactory;
	private MenuViewPresenterHost _viewPresenterHost;
	private List<CompositionInstance> _compositions = new List<CompositionInstance>();

	public bool CanRestart { get; set; }

	public event EventHandler OnResume;

	public event EventHandler OnRestart;

	public event EventHandler OnQuit;

	public OptionsMenu(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_graphicsContext = gameContext.Window.GraphicsContext;
		_settingUIResources = new SettingUIResources();
		_viewPresenterFactory = new MenuViewPresenterFactory(_gameContext, _settingUIResources);
	}

	public void Dispose() => _resourceSession?.Dispose();

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependenciesByAttribute(this);
		_settingUIResources.PushDependencies(_resourceSession);
		await _resourceSession.LoadAsync(ct);
		resourceTree.FullfillLoadedResourcesByAttribute(this);
		_settingUIResources.FetchResources(resourceTree);
		_loaded = true;
	}

	private void Initialise()
	{
		_menuPopInComposition = new CompositionInstance(_menuPopInCompositionGroup);
		_menuPopOutComposition = new CompositionInstance(_menuPopOutCompositionGroup);
		_subMenuAudioPopInComposition = new CompositionInstance(_subMenuAudioPopInCompositionGroup);
		_subMenuAudioPopOutComposition = new CompositionInstance(_subMenuAudioPopOutCompositionGroup);
		_subMenuVideoPopInComposition = new CompositionInstance(_subMenuVideoPopInCompositionGroup);
		_subMenuVideoPopOutComposition = new CompositionInstance(_subMenuVideoPopOutCompositionGroup);
		_subMenuOptionsPopInComposition = new CompositionInstance(_subMenuOptionsPopInCompositionGroup);
		_currentComposition = _menuPopInComposition;
		_compositions.Add(_menuPopInComposition);
		_compositions.Add(_menuPopOutComposition);
		_compositions.Add(_subMenuAudioPopInComposition);
		_compositions.Add(_subMenuAudioPopOutComposition);
		_compositions.Add(_subMenuVideoPopInComposition);
		_compositions.Add(_subMenuVideoPopOutComposition);
		_compositions.Add(_subMenuOptionsPopInComposition);
	}

	public void Update()
	{
		if (!_loaded)
			return;
		if (!_initialised)
		{
			_initialised = true;
			Initialise();
		}
		if (!_showing || _currentComposition == null)
			return;
		_currentComposition.Animate();
		if (!_currentComposition.Finished || _currentComposition == _menuPopOutComposition)
			return;
		_circleSize = _circleTexture.Width;
		int circleSize = (int)_circleSize;
		_circleBounds = new Rectanglei(0, 0, circleSize, circleSize);
		if (_currentComposition == _subMenuVideoPopInComposition)
		{
			_circleBounds.X = (1920 - circleSize) / 2;
			_circleBounds.Y = 337;
		}
		else if (_currentComposition == _subMenuAudioPopInComposition)
		{
			_circleBounds.X = (1920 - circleSize) / 2;
			_circleBounds.Y = 400;
		}
		else
		{
			_circleBounds.X = (1920 - circleSize) / 2;
			_circleBounds.Y = (1080 - circleSize) / 2;
		}
		_viewPresenterHost.Bounds = Rectanglei.FromLTRB(_circleBounds.Left, _circleBounds.Top + 60, _circleBounds.Right, _circleBounds.Bottom);
		_viewPresenterHost.Update();
		_viewPresenterHost.HandleInput();
	}

	private void NavigateNextHandler(object sender, NavigateNextEventArgs e)
	{
		if (!(e.Tag is int))
			return;
		switch ((int)e.Tag)
		{
			case 1:
				EventHandler onQuit = OnQuit;
				if (onQuit == null)
					break;
				onQuit(this, EventArgs.Empty);
				break;
			case 2:
				EventHandler onRestart = OnRestart;
				if (onRestart == null)
					break;
				onRestart(this, EventArgs.Empty);
				break;
			case 3:
				EventHandler onResume = OnResume;
				if (onResume == null)
					break;
				onResume(this, EventArgs.Empty);
				break;
			case 4:
				_gameContext.Settings.Apply();
				break;
			case 5:
				ResetCompositions();
				_currentComposition = _subMenuOptionsPopInComposition;
				break;
			case 6:
				ResetCompositions();
				_currentComposition = _subMenuAudioPopInComposition;
				break;
			case 7:
				ResetCompositions();
				_currentComposition = _subMenuVideoPopInComposition;
				break;
		}
	}

	private void NavigateBackHandler(object sender, EventArgs e)
	{
		if (_currentComposition == _menuPopInComposition || _currentComposition == _subMenuOptionsPopInComposition)
			return;
		ResetCompositions();
		_currentComposition = _subMenuOptionsPopInComposition;
	}

	private void ResetCompositions()
	{
		foreach (CompositionInstance composition in _compositions)
			composition.ResetFrame();
	}

	public void Draw(Renderer renderer)
	{
		if (!_loaded || !_showing)
			return;
		I2dRenderer g = renderer.Get2dRenderer();
		BlurGame(renderer, g);
		if (_currentComposition == null)
			return;
		_currentComposition.Draw(renderer);
		if (!_currentComposition.Finished)
			return;
		DrawViewPresenterHost(renderer);
	}

	private void BlurGame(Renderer renderer, I2dRenderer g)
	{
		Rectanglei destination = new Rectanglei(0, 0, 1920, 1080);
		renderer.DeativateRenderer();
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		GaussianBlurRenderer gaussianBlurRenderer = GaussianBlurRenderer.FromRenderer(renderer);
		gaussianBlurRenderer.Softness = 4.0;
		gaussianBlurRenderer.Render(currentFramebuffer.Textures[0], destination);
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour(0.5, 0.0, 0.0, 0.0), (Rectangle)destination);
		renderer.DeativateRenderer();
	}

	private void DrawViewPresenterHost(Renderer renderer) => _viewPresenterHost.Draw(renderer);

	public void Show()
	{
		ResetCompositions();
		_currentComposition = _menuPopInComposition;
		_showing = true;
		_viewPresenterHost = new MenuViewPresenterHost(_viewPresenterFactory, new MenuViewFactory(_gameContext).GetInGameOptionsView(CanRestart), _settingUIResources);
		_viewPresenterHost.NavigateNext += new EventHandler<NavigateNextEventArgs>(NavigateNextHandler);
		_viewPresenterHost.NavigateBack += new EventHandler(NavigateBackHandler);
	}

	public void Hide()
	{
		_showing = false;
		_viewPresenterHost = null;
	}
}
