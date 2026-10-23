// Decompiled with JetBrains decompiler
// Type: S2HD.LogosGameState
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Menu;
using SonicOrca.Resources;

#nullable disable
namespace S2HD;

internal class LogosGameState : IGameState, IDisposable
{
	private bool _loaded;
	private Task _loadingTask;
	private CancellationTokenSource _loadingCTS;
	private ResourceSession _resourceSession;
	[ResourcePath("SONICORCA/ENGINE")]
	private ITexture _engineTexture;
	[ResourcePath("SONICORCA/ENGINE/PARTIAL")]
	private ITexture _enginePartialTexture;
	[ResourcePath("SONICORCA/ENGINE/SONIC")]
	private ITexture _engineSonicTexture;
	[ResourcePath("SONICORCA/FONTS/HUD")]
	private Font _font;
	private readonly SonicOrcaGameContext _gameContext;
	private readonly FadeTransition _fadeTransition = new FadeTransition(60);
	private bool _smallSonic;
	private int _sonicX;
	private int _sonicVX;
	private int _sonicFrame;

	public LogosGameState(SonicOrcaGameContext context) => _gameContext = context;

	private void LoadResources()
	{
		_resourceSession = new ResourceSession(_gameContext.ResourceTree);
		_resourceSession.PushDependenciesByAttribute(this);
		_loadingCTS = new CancellationTokenSource();
		_loadingTask = _resourceSession.LoadAsync(_loadingCTS.Token);
	}

	public void Dispose()
	{
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
			_smallSonic = true;
			_sonicX = -256;
			_sonicVX = 100;
			yield return UpdateResult.Wait(8);
			while (_sonicX < 2176)
			{
				_sonicX += _sonicVX;
				_sonicFrame = (_sonicFrame + 1) % 8;
				yield return UpdateResult.Next();
			}
			_smallSonic = false;
			_sonicX = 2944;
			_sonicVX = -266;
			yield return UpdateResult.Wait(8);
			while (_sonicX > -1024)
			{
				_sonicX += _sonicVX;
				_sonicFrame = (_sonicFrame + 1) % 8;
				yield return UpdateResult.Next();
			}
			_sonicX = -1024;
			_sonicVX = 200;
			yield return UpdateResult.Wait(16 /*0x10*/);
			while (_sonicX < 2944)
			{
				_sonicX += _sonicVX;
				_sonicFrame = (_sonicFrame + 1) % 8;
				yield return UpdateResult.Next();
			}
			yield return UpdateResult.Wait(90);
			_fadeTransition.BeginFadeOut();
			while (!_fadeTransition.Finished)
			{
				_fadeTransition.Update();
				yield return UpdateResult.Next();
			}
		}
	}

	public void Draw()
	{
		if (!_loaded)
			return;
		I2dRenderer standard2d = SharedRenderers.Standard2d;
		Renderer renderer = _gameContext.Renderer;
		I2dRenderer g = renderer.Get2dRenderer();
		DrawPoweredBy(g);
		if (_smallSonic)
		{
			DrawSmallSonic(g);
		}
		else
		{
			DrawEngineLogo(g);
			DrawSonic(g);
		}
		_fadeTransition.Draw(renderer);
	}

	private void DrawPoweredBy(I2dRenderer g)
	{
		Rectangle clipRectangle = g.ClipRectangle;
		if (_smallSonic)
			g.ClipRectangle = new Rectangle(0.0, 0.0, _sonicX, 1080.0);
		g.RenderText(new TextRenderInfo()
		{
			Font = _font,
			Text = "POWERED BY",
			Bounds = new Rectangle(0.0, 120.0, 1920.0, 120.0),
			Alignment = FontAlignment.Centre,
			Overlay = new int?(0)
		});
		if (!_smallSonic)
			return;
		g.ClipRectangle = clipRectangle;
	}

	private void DrawEngineLogo(I2dRenderer g)
	{
		Sizei sizei = new Sizei(_engineTexture.Width, _engineTexture.Height);
		Rectanglei destination = new Rectanglei(960 - sizei.Width / 2, 540 - sizei.Height / 2, sizei.Width, sizei.Height);
		destination.Left = Math.Max(destination.Left, _sonicX);
		Rectanglei source;
		if (destination.Width > 0)
		{
			source = new Rectanglei(sizei.Width - destination.Width, 0, destination.Width, sizei.Height);
			g.BlendMode = BlendMode.Alpha;
			g.Colour = Colours.White;
			g.RenderTexture(_enginePartialTexture, (Rectangle)source, (Rectangle)destination);
		}
		if (_sonicVX < 0)
			return;
		destination = new Rectanglei(960 - sizei.Width / 2, 540 - sizei.Height / 2, sizei.Width, sizei.Height);
		destination.Right = Math.Min(destination.Right, _sonicX);
		if (destination.Width <= 0)
			return;
		source = new Rectanglei(0, 0, destination.Width, sizei.Height);
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		g.RenderTexture(_engineTexture, (Rectangle)source, (Rectangle)destination);
	}

	private void DrawSmallSonic(I2dRenderer g)
	{
		Rectanglei source = new Rectanglei(_sonicFrame * 1024 /*0x0400*/, 0, 1024 /*0x0400*/, 1120);
		Rectanglei destination = new Rectanglei(_sonicX - 128 /*0x80*/, 40, 256 /*0x0100*/, 280);
		if (destination.Right <= 0 || destination.Left >= 1920)
			return;
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		g.RenderTexture(_engineSonicTexture, (Rectangle)source, (Rectangle)destination, _sonicVX >= 0);
	}

	private void DrawSonic(I2dRenderer g)
	{
		Rectanglei source = new Rectanglei(_sonicFrame * 1024 /*0x0400*/, 0, 1024 /*0x0400*/, 1120);
		Rectanglei destination = new Rectanglei(_sonicX - 512 /*0x0200*/, -20, 1024 /*0x0400*/, 1120);
		if (destination.Right <= 0 || destination.Left >= 1920)
			return;
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		g.RenderTexture(_engineSonicTexture, (Rectangle)source, (Rectangle)destination, _sonicVX >= 0);
	}

	private static class ResourceKeys
	{
		public const string Engine = "SONICORCA/ENGINE";
		public const string EnginePartial = "SONICORCA/ENGINE/PARTIAL";
		public const string EngineSonic = "SONICORCA/ENGINE/SONIC";
		public const string Font = "SONICORCA/FONTS/HUD";
	}
}
