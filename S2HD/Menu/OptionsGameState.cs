using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace S2HD.Menu;

/// <summary>
/// The options screen that the title screen opens. It plays the options music and shows the audio and video
/// settings, with a button bar at the bottom.
/// </summary>
internal class OptionsGameState : IGameState, IDisposable
{
	[ResourcePath("SONICORCA/MUSIC/OPTIONS")]
	private SampleInfo _musicSampleInfo;
	[ResourcePath("SONICORCA/MENU/OPTIONS/MENU3")]
	private ITexture _backgroundTexture;
	/// <summary>The duration of the fade in and of the fade out, in frames.</summary>
	private const int FadeTime = 60;
	/// <summary>The change in opacity on each frame of a fade.</summary>
	private const float FadeStep = 1f / FadeTime;
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly SettingUIResources _settingUIResources = new SettingUIResources();
	private readonly MenuViewPresenterFactory _viewPresenterFactory;
	private readonly MenuViewPresenterHost _viewPresenterHost;
	private readonly ButtonBarUI _buttonBarUI;
	private ResourceSession _resourceSession;
	private bool _initialised;
	private bool _finished;
	private SampleInstance _musicSampleInstance;
	private Rectanglei _bounds;
	private float _fadeOutOpacity;

	public OptionsGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_viewPresenterFactory = new MenuViewPresenterFactory(_gameContext, _settingUIResources);
		_viewPresenterHost = new MenuViewPresenterHost(_viewPresenterFactory, new MenuViewFactory(_gameContext).GetOptionsView(), _settingUIResources);
		_viewPresenterHost.NavigateNext += NavigateNextHandler;
		_viewPresenterHost.NavigateBack += NavigateBackHandler;
		_buttonBarUI = new ButtonBarUI(_settingUIResources);
	}

	public void Dispose()
	{
		_musicSampleInstance?.Dispose();
		_resourceSession?.Dispose();
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_resourceSession = new ResourceSession(resourceTree);
		_resourceSession.PushDependenciesByAttribute(this);
		_settingUIResources.PushDependencies(_resourceSession);
		await _resourceSession.LoadAsync(ct);
		resourceTree.FullfillLoadedResourcesByAttribute(this);
		_settingUIResources.FetchResources(resourceTree);
	}

	private void Initialise()
	{
		_backgroundTexture.Wrapping = TextureWrapping.Repeat;
		_bounds = new Rectanglei(0, 0, 1920, 1080);
		_fadeOutOpacity = 0.0f;
		_viewPresenterHost.Bounds = _bounds.Inflate(new Vector2i(-512, -386));
		_buttonBarUI.Bounds = new Rectanglei(0, _bounds.Height - 100, _bounds.Width, 64);
		_buttonBarUI.Items = ImmutableArray.Create(
			new ButtonBarItem()
			{
				Button = GamePadButton.A,
				Text = "APPLY"
			},
			new ButtonBarItem()
			{
				Button = GamePadButton.B,
				Text = "CANCEL"
			});
		_initialised = true;
	}

	public IEnumerable<UpdateResult> Update()
	{
		Task loadTask = LoadAsync();
		while (!loadTask.IsCompleted)
			yield return UpdateResult.Next();
		if (!loadTask.IsFaulted)
		{
			Initialise();
			_musicSampleInstance = new SampleInstance(_gameContext.Audio, _musicSampleInfo);
			_musicSampleInstance.Classification = SampleInstanceClassification.Music;
			_musicSampleInstance.Play();
			// Fade in.
			while (_fadeOutOpacity < 1.0)
			{
				_fadeOutOpacity += FadeStep;
				_fadeOutOpacity = Math.Min(_fadeOutOpacity, 1f);
				_viewPresenterHost.Update();
				yield return UpdateResult.Next();
			}
			while (!_finished)
			{
				_viewPresenterHost.Update();
				_viewPresenterHost.HandleInput();
				yield return UpdateResult.Next();
			}
			// Fade out the screen and the music.
			while (_fadeOutOpacity > 0.0)
			{
				_musicSampleInstance.Volume = Math.Max(0.0, _musicSampleInstance.Volume - FadeStep);
				_fadeOutOpacity = Math.Max(_fadeOutOpacity - FadeStep, 0f);
				_viewPresenterHost.Update();
				yield return UpdateResult.Next();
			}
			_musicSampleInstance.Stop();
		}
	}

	private void NavigateNextHandler(object sender, NavigateNextEventArgs e)
	{
		if (e.Tag is int tag && tag == MenuTags.APPLY)
			_gameContext.Settings.Apply();
	}

	private void NavigateBackHandler(object sender, EventArgs e) => _finished = true;

	public void Draw()
	{
		if (!_initialised)
			return;
		Renderer renderer = _gameContext.Renderer;
		DrawBackground(renderer);
		DrawViewPresenterHost(renderer);
		DrawButtonBar(renderer);
		DrawFadeOut(renderer);
	}

	private void DrawButtonBar(Renderer renderer) => _buttonBarUI.Draw(renderer);

	private void DrawBackground(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Opaque;
		g.Colour = Colours.White;
		g.ModelMatrix = Matrix4.Identity;
		g.RenderTexture(_backgroundTexture, (Rectangle)_bounds, (Rectangle)_bounds);
	}

	private void DrawViewPresenterHost(Renderer renderer) => _viewPresenterHost.Draw(renderer);

	private void DrawFadeOut(Renderer renderer)
	{
		renderer.DeativateRenderer();
		IFadeTransitionRenderer fadeTransition = SharedRenderers.FadeTransition;
		fadeTransition.Opacity = _fadeOutOpacity - 1f;
		fadeTransition.Render();
	}
}
