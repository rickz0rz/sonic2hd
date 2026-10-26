using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S2HD;
using S2HD.Title;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca;

/// <summary>
/// The title screen. It shows the intro text, the banner with sparkles and a shooting star, the background,
/// and the menus. <see cref="Result"/> tells the root game state what the player selected.
/// </summary>
internal class TitleGameState : IGameState, IDisposable
{
	/// <summary>The change in fade opacity on each frame when the title screen fades out.</summary>
	private const float FadeOutStep = 1f / 60f;

	private static readonly EaseTimeline IntroTextOpacity = new EaseTimeline(new EaseTimeline.Entry[4]
	{
		new EaseTimeline.Entry(32, 0.0),
		new EaseTimeline.Entry(64, 1.0),
		new EaseTimeline.Entry(144, 1.0),
		new EaseTimeline.Entry(176, 0.0)
	});
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private ResourceSession _resourceSession;
	private Font _font;
	private AnimationGroup _animationGroup;
	private Sample _sparkleSample;
	private Sample _shootingStarSample;
	private Sample _musicSample;
	private SampleInstance _musicInstance;
	/// <summary>The ticks when a sparkle appears, and its position relative to the banner centre.</summary>
	private readonly IReadOnlyCollection<Tuple<int, Vector2i>> SparkleTable = new Tuple<int, Vector2i>[4]
	{
		new Tuple<int, Vector2i>(204, new Vector2i(-126, -320)),
		new Tuple<int, Vector2i>(220, new Vector2i(-280, 366)),
		new Tuple<int, Vector2i>(236, new Vector2i(324, -4)),
		new Tuple<int, Vector2i>(252, new Vector2i(80, 168))
	};
	private int _ticks;
	private Vector2i _sparklePosition;
	private AnimationInstance _sparkleAnimationInstance;
	private float _fadeOutOpacity;
	private bool _fadingOut;
	private AnimationInstance _shootingStarAnimationInstance;
	private Vector2 _shootingStarPosition;
	private IMaskRenderer _maskRenderer;
	private bool _loaded;
	private Task _loadingTask;
	private CancellationTokenSource _loadingCTS;
	private Banner _banner;
	private UserInterface _userInterface;
	private readonly string _versionText;

	public Background Background { get; private set; }

	public LevelPrepareSettings LevelPrepareSettings { get; set; }

	public TitleGameState.ResultType Result { get; set; }

	public TitleGameState(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		Version appVersion = Program.AppVersion;
		_versionText = $"Version {appVersion.Major}.{appVersion.Minor} Build {Program.CommitSha ?? appVersion.Build.ToString()}";
	}

	private void LoadResources()
	{
		_resourceSession = new ResourceSession(_gameContext.ResourceTree);
		_resourceSession.PushDependencies(TitleResources.AllResourceKeys);
		_loadingCTS = new CancellationTokenSource();
		_loadingTask = _resourceSession.LoadAsync(_loadingCTS.Token);
	}

	private void GetResourceReferences()
	{
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_font = resourceTree.GetLoadedResource<Font>(TitleResources.FontResourceKey);
		_animationGroup = resourceTree.GetLoadedResource<AnimationGroup>(TitleResources.AnimationGroupResourceKey);
		_sparkleSample = resourceTree.GetLoadedResource<Sample>(TitleResources.SparkleSoundResourceKey);
		_shootingStarSample = resourceTree.GetLoadedResource<Sample>(TitleResources.ShootingStarSoundResourceKey);
		_musicSample = resourceTree.GetLoadedResource<Sample>(TitleResources.MusicResourceKey);
	}

	public void Dispose() => _resourceSession.Dispose();

	public IEnumerable<UpdateResult> Update()
	{
		_maskRenderer = _gameContext.Renderer.GetMaskRenderer();
		LoadResources();
		while (!_loadingTask.IsCompleted)
			yield return UpdateResult.Next();
		if (!_loadingTask.IsFaulted)
		{
			GetResourceReferences();
			_loaded = true;
			Background = new Background(_gameContext);
			_banner = new Banner(_gameContext, _maskRenderer);
			_userInterface = new UserInterface(_gameContext, this, _maskRenderer);
			RestartEvents();
			while (true)
			{
				if (_fadingOut)
				{
					if (_fadeOutOpacity > 0.0)
					{
						_fadeOutOpacity -= FadeOutStep;
						_musicInstance.Volume = _fadeOutOpacity;
					}
					else
						break;
				}
				// The timeline of the intro, in ticks.
				if (_ticks == 268)
				{
					_musicInstance = new SampleInstance(_gameContext, _musicSample);
					_musicInstance.Play();
				}
				if (_ticks == 458)
				{
					Background.Visible = true;
					_banner.ShowStarLensFare = true;
					_userInterface.Visible = true;
				}
				_banner.Update();
				if (Background.Visible)
					Background.Update();
				_userInterface.Update();
				if (_ticks >= 662)
				{
					if (_shootingStarAnimationInstance == null)
					{
						_shootingStarAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.ShootingStarAnimationIndex);
						_shootingStarAnimationInstance.AdditiveBlending = true;
						_shootingStarPosition = new Vector2(1440.0, 0.0);
						_gameContext.Audio.PlaySound(_shootingStarSample);
					}
					_shootingStarAnimationInstance.Animate();
					_shootingStarPosition += new Vector2(-16.0, 8.0);
				}
				UpdateSparkle();
				++_ticks;
				yield return UpdateResult.Next();
			}
			_gameContext.Audio.StopAll();
		}
	}

	private void RestartEvents()
	{
		_ticks = 0;
		_sparkleAnimationInstance = null;
		_fadeOutOpacity = 1f;
		_fadingOut = false;
		Background.Reset();
		_banner.Reset();
		_userInterface.Reset();
		_gameContext.Audio.StopAll();
	}

	public void FadeOut()
	{
		_fadingOut = true;
		_banner.DoShine(210);
	}

	private void CreateSparkle(Vector2i position)
	{
		_sparkleAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.SparkleAnimationIndex);
		_sparkleAnimationInstance.AdditiveBlending = true;
		_sparklePosition = position;
		_gameContext.Audio.PlaySound(_sparkleSample);
	}

	private void UpdateSparkle()
	{
		foreach (Tuple<int, Vector2i> sparkle in SparkleTable)
		{
			if (sparkle.Item1 == _ticks)
				CreateSparkle(_banner.Position + sparkle.Item2);
		}
		if (_sparkleAnimationInstance == null)
			return;
		_sparkleAnimationInstance.Animate();
	}

	public void Draw()
	{
		if (!_loaded)
			return;
		Renderer renderer = _gameContext.Renderer;
		DrawIntroText(renderer);
		Background.Draw(renderer);
		DrawShootingStar(renderer);
		_banner.Draw(renderer);
		DrawSparkle(renderer);
		_userInterface.Draw(renderer);
		if (Background.Visible)
			DrawVersion(renderer);
		if (_fadeOutOpacity != 1.0)
		{
			renderer.DeativateRenderer();
			IFadeTransitionRenderer fadeTransition = SharedRenderers.FadeTransition;
			fadeTransition.Opacity = _fadeOutOpacity - 1f;
			fadeTransition.Render();
		}
		_banner.DrawUnfaded(renderer);
	}

	private void DrawIntroText(Renderer renderer)
	{
		double opacity = IntroTextOpacity.GetValueAt(_ticks);
		if (opacity <= 0.0)
			return;
		IReadOnlyList<string> lines = new string[4]
		{
			"SONIC",
			"AND",
			"MILES \"TAILS\" PROWER",
			"IN"
		};
		const int lineHeight = 128;
		int y = 540 - lines.Count * lineHeight / 2;
		Colour colour = new Colour(opacity, 1.0, 1.0, 1.0);
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		foreach (string text in lines)
		{
			fontRenderer.RenderStringWithShadow(text, new Rectangle(0.0, y, 1920.0, 0.0), FontAlignment.Centre, _font, colour, new int?(0));
			y += lineHeight;
		}
	}

	private void DrawSparkle(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		if (_sparkleAnimationInstance == null || _sparkleAnimationInstance.Cycles != 0)
			return;
		_sparkleAnimationInstance.Draw(g, (Vector2)_sparklePosition);
	}

	private void DrawShootingStar(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		if (_shootingStarAnimationInstance == null)
			return;
		_shootingStarAnimationInstance.Draw(g, _shootingStarPosition);
	}

	private void DrawVersion(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Colour colour = new Colour(_fadeOutOpacity / 2.0, 1.0, 1.0, 1.0);
		// Draw the version text at half size in the bottom-left corner.
		using (g.BeginMatixState())
		{
			g.ModelMatrix *= Matrix4.CreateTranslation(8.0, 1072.0);
			g.ModelMatrix *= Matrix4.CreateScale(0.5, 0.5);
			Rectangle boundary = new Rectangle(0.0, 0.0, 0.0, 0.0);
			fontRenderer.RenderStringWithShadow(_versionText.ToUpper(), boundary, FontAlignment.Bottom, _font, colour, new int?(0));
		}
	}

	/// <summary>The item that the player selected on the title screen.</summary>
	public enum ResultType
	{
		NewGame,
		LevelSelect,
		ShowOptions,
		ShowAchievements,
		StartDemo,
		Quit,
	}
}
