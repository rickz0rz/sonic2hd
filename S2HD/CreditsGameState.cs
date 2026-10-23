// Decompiled with JetBrains decompiler
// Type: S2HD.CreditsGameState
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace S2HD;

internal class CreditsGameState : IGameState, IDisposable
{
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
			_musicInstance = new SampleInstance(_gameContext, _musicSample, new int?(458980));
			_fadeOpacity = 0.0;
			bool playedStarSFX = false;
			bool quit = false;
			while (!_creditsFilmInstance.Finished)
			{
				double currentTime = _creditsFilmInstance.CurrentTime;
				if (currentTime < 42.0)
				{
					InputState pressed = _gameContext.Input.Pressed;
					if (pressed.Keyboard[40] || pressed.GamePad[0].Start)
						quit = true;
				}
				if (quit)
				{
					_musicInstance.Volume -= 1.0 / 120.0;
					_fadeOpacity += 1.0 / 120.0;
					if (_fadeOpacity >= 1.0)
						break;
				}
				if (!_musicInstance.Playing && currentTime >= 2.0)
					_musicInstance.Play();
				if (currentTime >= 45.0)
					_musicInstance.Volume = 1.0 - Math.Min((currentTime - 45.0) / 8.0, 1.0);
				if (!playedStarSFX && currentTime >= 52.5)
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
		I2dRenderer obj = renderer.Get2dRenderer();
		Colour colour = new Colour(_fadeOpacity, 0.0, 0.0, 0.0);
		obj.BlendMode = BlendMode.Alpha;
		obj.RenderQuad(colour, new Rectangle(0.0, 0.0, 1920.0, 1080.0));
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
