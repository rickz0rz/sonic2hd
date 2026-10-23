using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Graphics.V2.Video;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace S2HD;

/// <summary>
/// Plays the credits film with its music.
/// The player can skip the credits with Enter or Start during the first 42 seconds.
/// </summary>
internal class CreditsGameState : IGameState, IDisposable
{
	/// <summary>SDL scancode of the Enter key.</summary>
	private const int EnterScancode = 40;
	/// <summary>The sample index where the credits music starts again when it loops.</summary>
	private const int MusicLoopSampleIndex = 458980;
	/// <summary>The film time, in seconds, after which the player can no longer skip the credits.</summary>
	private const double SkipEndTime = 42.0;
	private const double MusicStartTime = 2.0;
	private const double MusicFadeOutStartTime = 45.0;
	private const double MusicFadeOutDuration = 8.0;
	private const double ShootingStarTime = 52.5;
	/// <summary>The change in volume and fade opacity on each frame after the player skips. The fade takes 2 seconds.</summary>
	private const double SkipFadeStep = 1.0 / 120.0;

	private readonly SonicOrcaGameContext _gameContext;
	private bool _loaded;
	private Task _loadingTask;
	private CancellationTokenSource _loadingCTS;
	private ResourceSession _resourceSession;
	[ResourcePath("SONICORCA/MUSIC/CREDITS")]
	private Sample _musicSample;
	[ResourcePath("SONICORCA/SOUND/SHOOTINGSTAR")]
	private Sample _shootingStarSample;
	[ResourcePath("SONICORCA/CREDITS/SEQUENCE/GROUP")]
	private FilmGroup _creditsFilmGroup;
	private FilmInstance _creditsFilmInstance;
	private SampleInstance _musicInstance;
	private double _fadeOpacity;

	public CreditsGameState(SonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_resourceSession = new ResourceSession(gameContext.ResourceTree);
	}

	private void LoadResources()
	{
		_resourceSession = new ResourceSession(_gameContext.ResourceTree);
		_resourceSession.PushDependenciesByAttribute(this);
		_loadingCTS = new CancellationTokenSource();
		_loadingTask = _resourceSession.LoadAsync(_loadingCTS.Token);
	}

	public void Dispose()
	{
		_musicInstance?.Dispose();
		_musicInstance = null;
		if (_loadingTask != null && !_loadingTask.IsCompleted)
		{
			_loadingCTS.Cancel();
			_loadingTask.Wait();
		}
		if (_resourceSession == null)
			return;
		_resourceSession.Dispose();
		_resourceSession = null;
	}

	public IEnumerable<UpdateResult> Update()
	{
		LoadResources();
		while (!_loadingTask.IsCompleted)
			yield return UpdateResult.Next();
		if (!_loadingTask.IsFaulted)
		{
			_gameContext.ResourceTree.FullfillLoadedResourcesByAttribute(this);
			_loaded = true;
			_creditsFilmInstance = new FilmInstance(_creditsFilmGroup);
			_musicInstance = new SampleInstance(_gameContext, _musicSample, MusicLoopSampleIndex);
			_fadeOpacity = 0.0;
			bool playedStarSFX = false;
			bool quit = false;
			while (!_creditsFilmInstance.Finished)
			{
				double currentTime = _creditsFilmInstance.CurrentTime;
				if (currentTime < SkipEndTime)
				{
					InputState pressed = _gameContext.Input.Pressed;
					if (pressed.Keyboard[EnterScancode] || pressed.GamePad[0].Start)
						quit = true;
				}
				if (quit)
				{
					_musicInstance.Volume -= SkipFadeStep;
					_fadeOpacity += SkipFadeStep;
					if (_fadeOpacity >= 1.0)
						break;
				}
				if (!_musicInstance.Playing && currentTime >= MusicStartTime)
					_musicInstance.Play();
				if (currentTime >= MusicFadeOutStartTime)
					_musicInstance.Volume = 1.0 - Math.Min((currentTime - MusicFadeOutStartTime) / MusicFadeOutDuration, 1.0);
				if (!playedStarSFX && currentTime >= ShootingStarTime)
				{
					playedStarSFX = true;
					_gameContext.Audio.PlaySound(_shootingStarSample);
				}
				_creditsFilmInstance.Animate();
				yield return UpdateResult.Next();
			}
		}
	}

	public void Draw()
	{
		if (!_loaded)
			return;
		Renderer renderer = _gameContext.Renderer;
		_creditsFilmInstance.Draw(renderer);
		if (_fadeOpacity >= 1.0)
			return;
		I2dRenderer g = renderer.Get2dRenderer();
		Colour fadeColour = new Colour(_fadeOpacity, 0.0, 0.0, 0.0);
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(fadeColour, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
	}

	private static class ResourceKeys
	{
		public const string RoleFont = "SONICORCA/FONTS/TITLE/S2/NAME";
		public const string NameFont = "SONICORCA/FONTS/HUD";
		public const string Logo = "SONICORCA/LOGO";
		public const string Music = "SONICORCA/MUSIC/CREDITS";
		public const string ShootingStarSample = "SONICORCA/SOUND/SHOOTINGSTAR";
	}
}
