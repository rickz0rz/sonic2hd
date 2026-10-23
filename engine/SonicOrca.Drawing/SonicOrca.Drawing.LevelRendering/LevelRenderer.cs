#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Drawing.Renderers;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Drawing.LevelRendering;

public class LevelRenderer : ILevelRenderer, IDisposable
{
	private readonly SonicOrcaGameContext _gameContext;

	private readonly IGraphicsContext _graphicsContext;

	private readonly IVideoSettings _videoSettings;

	private readonly Level _level;

	private TileSet _tileSet;

	private ParticleManager _particleManager;

	private ResourceSession _resourceSession;

	private IFramebuffer _canvasFramebuffer;

	private IFramebuffer _effectFramebuffer;

	private RenderingLayer[] _renderingLayers;

	private RenderingTileList _renderingTileList;

	private RenderingTiler _tiler;

	private int _numLayers;

	private Rectanglei _previousCanvasViewport;

	private Rectanglei _screenBounds = new Rectanglei(0, 0, 1920, 1080);

	private Viewport _canvasViewport;

	private const string WaterfallDistortionTextureResourceKey = "SONICORCA/PARTICLE/WATERFALL";

	private ITexture _waterfallDistortionTexture;

	private readonly List<Rectanglei> _waterfallRegions = new List<Rectanglei>(16);

	public string[] LastDebugLog { get; private set; }

	public LevelRenderer(Level level, IVideoSettings videoSettings)
	{
		_gameContext = level.GameContext;
		_level = level;
		_graphicsContext = _gameContext.Window.GraphicsContext;
		_videoSettings = videoSettings;
	}

	public void Dispose()
	{
		DisposeCanvas();
		_resourceSession?.Dispose();
	}

	public async Task LoadAsync(CancellationToken ct = default(CancellationToken))
	{
		_resourceSession = new ResourceSession(_gameContext.ResourceTree);
		_resourceSession.PushDependency("SONICORCA/PARTICLE/WATERFALL");
		await _resourceSession.LoadAsync();
		ResourceTree resourceTree = _gameContext.ResourceTree;
		_waterfallDistortionTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/PARTICLE/WATERFALL");
	}

	public void Initialise()
	{
		_tileSet = _level.TileSet;
		_particleManager = _level.ParticleManager;
		_numLayers = _level.Map.Layers.Count;
		_renderingLayers = new RenderingLayer[_numLayers];
		for (int i = 0; i < _numLayers; i++)
		{
			RenderingLayer renderingLayer = new RenderingLayer(_level, i, _videoSettings);
			renderingLayer.UpdateClippingMarkers();
			_renderingLayers[i] = renderingLayer;
			renderingLayer.Layer.Index = i;
		}
		_renderingTileList = new RenderingTileList();
		_tiler = new RenderingTiler(_level, _renderingTileList);
		InitialiseShadowRendering();
		_canvasViewport = new Viewport(_screenBounds);
		_canvasViewport.Bounds = _screenBounds;
		InitialiseCanvas(_canvasViewport.Destination.Width, _canvasViewport.Destination.Height);
	}

	public void Render(Renderer renderer, Viewport screenViewport, LayerViewOptions layerViewOptions)
	{
		renderer.DeativateRenderer();
		IFramebuffer currentFramebuffer = _graphicsContext.CurrentFramebuffer;
		if (_screenBounds != screenViewport.Destination)
		{
			_screenBounds = screenViewport.Destination;
			_canvasViewport = new Viewport(_screenBounds);
			InitialiseCanvas(_canvasViewport.Destination.Width, _canvasViewport.Destination.Height);
		}
		_canvasViewport.Bounds = screenViewport.Bounds;
		if (_canvasViewport.Bounds.X != _previousCanvasViewport.X || _canvasViewport.Bounds.Y != _previousCanvasViewport.Y)
		{
			_previousCanvasViewport = _canvasViewport.Bounds;
		}
		_canvasFramebuffer.Activate();
		_graphicsContext.ClearBuffer();
		PrepareLayers(_canvasViewport, layerViewOptions);
		PrepareObjects();
		DrawLayers(renderer, _canvasViewport, layerViewOptions);
		if (layerViewOptions.ShowLandscapeCollision)
		{
			DrawLandscapeCollision(renderer, layerViewOptions);
		}
		_level.WaterManager.Draw(renderer, _canvasViewport);
		renderer.DeativateRenderer();
		RenderingHelpers.RenderToFramebuffer(renderer, _canvasFramebuffer.Textures[0], currentFramebuffer, _canvasViewport.Destination, BlendMode.Opaque);
		renderer.DeativateRenderer();
	}

	private void InitialiseCanvas(int width, int height)
	{
		if (_canvasFramebuffer == null || _canvasFramebuffer.Width != width || _canvasFramebuffer.Height != height)
		{
			DisposeCanvas();
			_canvasFramebuffer = _graphicsContext.CreateFrameBuffer(width, height);
			_effectFramebuffer = _graphicsContext.CreateFrameBuffer(width, height);
		}
	}

	private void DisposeCanvas()
	{
		_canvasFramebuffer?.Dispose();
		_effectFramebuffer?.Dispose();
	}

	private void PrepareLayers(Viewport canvasViewport, LayerViewOptions layerViewOptions)
	{
		_renderingTileList.Clear();
		for (int i = 0; i < _numLayers; i++)
		{
			RenderingLayer renderingLayer = _renderingLayers[i];
			renderingLayer.Clear();
			if (renderingLayer.Layer.Visible)
			{
				_tiler.PrepareTiles(renderingLayer, canvasViewport, layerViewOptions);
			}
		}
	}

	private void PrepareObjects()
	{
		List<ActiveObject> list = _level.ObjectManager.ActiveObjects.ToList();
		list.Sort((ActiveObject a, ActiveObject b) => a.Priority - b.Priority);
		for (int num = 0; num < _numLayers; num++)
		{
			_renderingLayers[num].Clear();
		}
		foreach (ActiveObject item in list)
		{
			int index = item.Layer.Index;
			RenderingLayer renderingLayer = _renderingLayers[index];
			int priority = item.Priority;
			if (priority < 0)
			{
				renderingLayer.AddLowObject(item);
			}
			else if (priority > 0)
			{
				renderingLayer.AddHighObject(item);
			}
		}
	}

	private void DrawLayers(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		Matrix4 modelMatrix = i2dRenderer.ModelMatrix;
		i2dRenderer.ModelMatrix = Matrix4.Identity;
		ITileRenderer tileRenderer = renderer.GetTileRenderer();
		tileRenderer.ClipRectangle = viewport.Destination;
		tileRenderer.ModelMatrix = Matrix4.Identity;
		tileRenderer.Textures = _tileSet.Textures.ToArray();
		tileRenderer.Filter = viewOptions.Filter;
		tileRenderer.FilterAmount = viewOptions.FilterAmount;
		Stopwatch.StartNew();
		RenderingTiler tiler = _tiler;
		RenderingLayer[] renderingLayers = _renderingLayers;
		foreach (RenderingLayer renderingLayer in renderingLayers)
		{
			if (renderingLayer.Layer.Visible)
			{
				BeginShadowStenciling(renderingLayer.HasShadows);
				SetLighting(renderingLayer.Layer, viewOptions);
				DrawBackObjects(renderer, viewport, viewOptions, renderingLayer);
				tiler.RenderTiles(i2dRenderer, tileRenderer, renderingLayer.RenderTileIndex, renderingLayer.RenderTileCount);
				DrawFrontObjects(renderer, viewport, viewOptions, renderingLayer);
				if (renderingLayer.HasShadows)
				{
					DrawShadows(renderer, i2dRenderer, tileRenderer, viewport, viewOptions, renderingLayer);
				}
				if (_videoSettings.EnableWaterEffects)
				{
					RenderWaterfallEffects(renderer, viewport, renderingLayer);
				}
				RenderParticles(renderer, viewport, renderingLayer);
			}
		}
		i2dRenderer.ModelMatrix = modelMatrix;
	}

	private void DrawLandscapeCollision(Renderer renderer, LayerViewOptions layerViewOptions)
	{
		foreach (CollisionVector item in _level.CollisionTable.InternalTree.Query(_canvasViewport.Bounds))
		{
			item.Draw(renderer, _canvasViewport);
		}
	}

	private void SetLighting(LevelLayer layer, LayerViewOptions viewOptions)
	{
		viewOptions.Filter = 0;
		viewOptions.FilterAmount = 0.0;
		switch (layer.Lighting.Type)
		{
			case LevelLayerLightingType.Outside:
				if (_level.NightMode != 0.0)
				{
					viewOptions.Filter = 1;
					viewOptions.FilterAmount = _level.NightMode;
				}
				break;
			case LevelLayerLightingType.Inside:
				viewOptions.Filter = 2;
				viewOptions.FilterAmount = 1.0 - layer.Lighting.Light;
				break;
		}
	}

	// Stencil state goes through IGraphicsContext (not OpenTK directly) so the web build can implement it with WebGL

	private void InitialiseShadowRendering()
	{
		_graphicsContext.SetStencilWriteMask(255);
		_graphicsContext.SetStencilOperation(StencilOperation.Keep, StencilOperation.Replace, StencilOperation.Replace);
	}

	private void BeginShadowStenciling(bool shadowsEnabled)
	{
		if (shadowsEnabled)
		{
			_graphicsContext.SetStencilTest(enabled: true);
			_graphicsContext.ClearStencil();
			_graphicsContext.SetStencilFunction(SonicOrca.Graphics.StencilFunction.Always, 1, 255);
			_graphicsContext.SetStencilOperation(StencilOperation.Keep, StencilOperation.Replace, StencilOperation.Replace);
		}
		else
		{
			_graphicsContext.SetStencilTest(enabled: false);
		}
	}

	private void DrawShadows(Renderer renderer, I2dRenderer g, ITileRenderer tr, Viewport viewport, LayerViewOptions viewOptions, RenderingLayer rlayer)
	{
		_graphicsContext.SetStencilFunction(SonicOrca.Graphics.StencilFunction.Equal, 1, 255);
		_graphicsContext.SetStencilOperation(StencilOperation.Keep, StencilOperation.Keep, StencilOperation.Zero);
		viewOptions.Shadows = true;
		ShadowRenderer.IsShadowing = true;
		Matrix4 modelMatrix = tr.ModelMatrix;
		RenderingTiler tiler = _tiler;
		foreach (LevelLayerShadow shadow in rlayer.Layer.Shadows)
		{
			int num = rlayer.LayerIndex + shadow.LayerIndexOffset;
			RenderingLayer renderingLayer = _renderingLayers[num];
			TileRenderer.ShadowColour = shadow.Colour;
			DrawBackObjects(renderer, viewport, viewOptions, renderingLayer);
			tr.ModelMatrix = Matrix4.CreateTranslation(shadow.Displacement);
			tiler.RenderTiles(g, tr, renderingLayer.RenderTileIndex, renderingLayer.RenderTileCount);
			DrawFrontObjects(renderer, viewport, viewOptions, renderingLayer);
		}
		tr.ModelMatrix = modelMatrix;
		ShadowRenderer.IsShadowing = false;
		viewOptions.Shadows = false;
	}

	private void DrawBackObjects(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions, RenderingLayer rlayer)
	{
		if (rlayer.LowObjects != null)
		{
			foreach (ActiveObject lowObject in rlayer.LowObjects)
			{
				lowObject.Draw(renderer, viewport, viewOptions);
			}
		}
		renderer.DeativateRenderer();
	}

	private void DrawFrontObjects(Renderer renderer, Viewport viewport, LayerViewOptions viewOptions, RenderingLayer rlayer)
	{
		if (rlayer.HighObjects != null)
		{
			foreach (ActiveObject highObject in rlayer.HighObjects)
			{
				highObject.Draw(renderer, viewport, viewOptions);
			}
		}
		renderer.DeativateRenderer();
	}

	public void RenderToClipboard(Viewport viewport, LayerViewOptions layerViewOptions)
	{
		int num = viewport.Destination.Width * viewport.Destination.Height;
		int num2 = 4194304;
		if (num > num2)
		{
			Trace.WriteLine($"Copying {num} pixels, Max copy image limit: {num2} pixels");
			return;
		}
		int width;
		int height;
		byte[] argbData;
		try
		{
			using IFramebuffer framebuffer = _gameContext.Window.GraphicsContext.CreateFrameBuffer(viewport.Destination.Width, viewport.Destination.Height);
			using (Renderer renderer = new TheRenderer(_gameContext.Window))
			{
				framebuffer.Activate();
				Render(renderer, viewport, layerViewOptions);
			}
			ITexture texture = framebuffer.Textures[0];
			width = texture.Width;
			height = texture.Height;
			argbData = texture.GetArgbData();
		}
		catch (Exception)
		{
			return;
		}
		// The WinForms clipboard copy was dropped for portability; the image is still saved to the user data directory
		try
		{
			string path = Path.Combine(_gameContext.UserDataDirectory, "copiedtiles.png");
			PngExport.Save(path, argbData, width, height, alpha: true, bgr: true);
			Trace.WriteLine("Saved level render to " + path);
		}
		catch (Exception)
		{
		}
	}

	private void RenderWaterfallEffects(Renderer renderer, Viewport viewport, RenderingLayer rlayer)
	{
		GetWaterfallRegions(rlayer.Layer, viewport);
		if (_waterfallRegions.Count <= 0)
		{
			return;
		}
		IFramebuffer canvasFramebuffer = _canvasFramebuffer;
		ITexture texture = canvasFramebuffer.Textures[0];
		_effectFramebuffer.Activate();
		_graphicsContext.ClearBuffer();
		RenderingHelpers.RenderToFramebuffer(renderer, texture, _effectFramebuffer, BlendMode.Opaque);
		_waterfallDistortionTexture.Wrapping = TextureWrapping.Repeat;
		WaterfallRenderer waterfallRenderer = WaterfallRenderer.FromRenderer(renderer);
		waterfallRenderer.DistortionOffset = new Vector2(0.0, _level.Ticks % 60 / -60.0);
		waterfallRenderer.DistortionTexture = _waterfallDistortionTexture;
		waterfallRenderer.DistortionAmount = 16.0;
		waterfallRenderer.NonDistortionRadius = new Vector2i(4, 24);
		foreach (Rectanglei waterfallRegion in _waterfallRegions)
		{
			Vector2i amount = new Vector2i(0, 1080 - waterfallRegion.Bottom - waterfallRegion.Top);
			Rectanglei source = waterfallRegion.OffsetBy(amount);
			waterfallRenderer.Render(texture, source, waterfallRegion, flipX: false, flipY: true);
		}
		RenderingHelpers.RenderToFramebuffer(renderer, _effectFramebuffer.Textures[0], canvasFramebuffer);
	}

	private void GetWaterfallRegions(LevelLayer layer, Viewport viewport)
	{
		_waterfallRegions.Clear();
		if (layer.WaterfallEffects == null)
		{
			return;
		}
		Rectanglei bounds = viewport.Bounds;
		Vector2i amount = new Vector2i(-bounds.X, -bounds.Y);
		foreach (Rectanglei waterfallEffect in layer.WaterfallEffects)
		{
			Rectanglei a = new Rectanglei(waterfallEffect.X * 64, waterfallEffect.Y * 64, waterfallEffect.Width * 64, waterfallEffect.Height * 64);
			a = Rectanglei.Intersect(a, bounds);
			if (a.Width > 0 && a.Height > 0)
			{
				a = a.OffsetBy(amount);
				_waterfallRegions.Add(a);
			}
		}
	}

	private void RenderParticles(Renderer renderer, Viewport viewport, RenderingLayer rlayer)
	{
		renderer.DeativateRenderer();
		_particleManager.Draw(renderer, viewport, rlayer.Layer, _videoSettings);
	}
}
