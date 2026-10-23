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

namespace S2HD;

/// <summary>
/// Shows the "powered by SonicOrca" engine logo at start-up.
/// A small Sonic runs across the screen and shows the text, then a large Sonic runs back and forth and shows the logo.
/// </summary>
internal class LogosGameState : IGameState, IDisposable
{
	/// <summary>The number of frames in the running Sonic animation.</summary>
	private const int SonicFrameCount = 8;
	/// <summary>The width and the height of one frame in the running Sonic texture.</summary>
	private const int SonicFrameWidth = 1024;
	private const int SonicFrameHeight = 1120;

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
			// The small Sonic runs from left to right and shows "POWERED BY".
			_smallSonic = true;
			_sonicX = -256;
			_sonicVX = 100;
			yield return UpdateResult.Wait(8);
			while (_sonicX < 2176)
			{
				_sonicX += _sonicVX;
				_sonicFrame = (_sonicFrame + 1) % SonicFrameCount;
				yield return UpdateResult.Next();
			}
			// The large Sonic runs from right to left, then from left to right, and shows the logo.
			_smallSonic = false;
			_sonicX = 2944;
			_sonicVX = -266;
			yield return UpdateResult.Wait(8);
			while (_sonicX > -1024)
			{
				_sonicX += _sonicVX;
				_sonicFrame = (_sonicFrame + 1) % SonicFrameCount;
				yield return UpdateResult.Next();
			}
			_sonicX = -1024;
			_sonicVX = 200;
			yield return UpdateResult.Wait(16);
			while (_sonicX < 2944)
			{
				_sonicX += _sonicVX;
				_sonicFrame = (_sonicFrame + 1) % SonicFrameCount;
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
		// While the small Sonic runs, the text shows only to the left of him.
		Rectangle clipRectangle = g.ClipRectangle;
		if (_smallSonic)
			g.ClipRectangle = new Rectangle(0.0, 0.0, _sonicX, 1080.0);
		g.RenderText(new TextRenderInfo()
		{
			Font = _font,
			Text = "POWERED BY",
			Bounds = new Rectangle(0.0, 120.0, 1920.0, 120.0),
			Alignment = FontAlignment.Centre,
			Overlay = 0
		});
		if (!_smallSonic)
			return;
		g.ClipRectangle = clipRectangle;
	}

	private void DrawEngineLogo(I2dRenderer g)
	{
		Sizei logoSize = new Sizei(_engineTexture.Width, _engineTexture.Height);
		// The partial logo shows to the right of Sonic.
		Rectanglei destination = new Rectanglei(960 - logoSize.Width / 2, 540 - logoSize.Height / 2, logoSize.Width, logoSize.Height);
		destination.Left = Math.Max(destination.Left, _sonicX);
		Rectanglei source;
		if (destination.Width > 0)
		{
			source = new Rectanglei(logoSize.Width - destination.Width, 0, destination.Width, logoSize.Height);
			g.BlendMode = BlendMode.Alpha;
			g.Colour = Colours.White;
			g.RenderTexture(_enginePartialTexture, (Rectangle)source, (Rectangle)destination);
		}
		// When Sonic runs to the right, the full logo shows to the left of him.
		if (_sonicVX < 0)
			return;
		destination = new Rectanglei(960 - logoSize.Width / 2, 540 - logoSize.Height / 2, logoSize.Width, logoSize.Height);
		destination.Right = Math.Min(destination.Right, _sonicX);
		if (destination.Width <= 0)
			return;
		source = new Rectanglei(0, 0, destination.Width, logoSize.Height);
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		g.RenderTexture(_engineTexture, (Rectangle)source, (Rectangle)destination);
	}

	private void DrawSmallSonic(I2dRenderer g)
	{
		Rectanglei source = new Rectanglei(_sonicFrame * SonicFrameWidth, 0, SonicFrameWidth, SonicFrameHeight);
		Rectanglei destination = new Rectanglei(_sonicX - 128, 40, 256, 280);
		if (destination.Right <= 0 || destination.Left >= 1920)
			return;
		g.BlendMode = BlendMode.Alpha;
		g.Colour = Colours.White;
		g.RenderTexture(_engineSonicTexture, (Rectangle)source, (Rectangle)destination, _sonicVX >= 0);
	}

	private void DrawSonic(I2dRenderer g)
	{
		Rectanglei source = new Rectanglei(_sonicFrame * SonicFrameWidth, 0, SonicFrameWidth, SonicFrameHeight);
		Rectanglei destination = new Rectanglei(_sonicX - SonicFrameWidth / 2, -20, SonicFrameWidth, SonicFrameHeight);
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
