using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class WaterManager
{
	private readonly SonicOrcaGameContext _gameContext;

	private readonly Level _level;

	private readonly List<Rectanglei> _waterAreas = new List<Rectanglei>();

	public static float offsetX;

	public static float offsetY;

	public static float viewportWaterLevel;

	public static ITexture waveTexture;

	private AnimationGroup wavesAnimationGroup;

	private int _waveFrame;

	private float _waterTime;

	private bool _loaded;

	private readonly List<Splash> _splashes = new List<Splash>();

	public Level Level => _level;

	public bool Enabled { get; set; }

	public IList<Rectanglei> WaterAreas => _waterAreas;

	public double HueTarget { get; set; }

	public double HueAmount { get; set; }

	public double SaturationChange { get; set; }

	public double LuminosityChange { get; set; }

	public double WavePhase { get; set; }

	public double NumWaves { get; set; }

	public double WaveSize { get; set; }

	public string SurfaceResourceKey { get; set; }

	public double SurfaceOffsetY { get; set; }

	internal AnimationGroup SpashEnterAnimationGroup { get; set; }

	internal AnimationGroup SpashExitAnimationGroup { get; set; }

	public WaterManager(Level level)
	{
		_gameContext = level.GameContext;
		_level = level;
		HueTarget = 0.45;
		HueAmount = 0.4;
		SaturationChange = -0.5;
		LuminosityChange = 0.0;
		NumWaves = 1.5;
		WaveSize = 4.0;
	}

	public void Load()
	{
		if (Enabled)
		{
			ResourceTree resourceTree = _level.GameContext.ResourceTree;
			SpashEnterAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>("SONICORCA/PARTICLE/SPLASH/ENTER");
			SpashExitAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>("SONICORCA/PARTICLE/SPLASH/EXIT");
			waveTexture = resourceTree.GetLoadedResource<ITexture>("SONICORCA/OBJECTS/WAVE/TEXTURE");
			wavesAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>("SONICORCA/OBJECTS/WAVES/ANIGROUP");
			_gameContext.Renderer.GetWaterRenderer();
			_loaded = true;
		}
	}

	public void Update()
	{
		if (!Enabled || !_loaded)
		{
			return;
		}
		foreach (Splash splash in _splashes)
		{
			splash.Animate();
		}
		_splashes.RemoveAll((Splash x) => x.Finished);
	}

	public void Animate()
	{
		if (Enabled && _loaded)
		{
			_waveFrame++;
			_waterTime += 0.0166f;
			WavePhase = 0.0 * MathX.WrapRadians(WavePhase + 1.0 / 32.0);
		}
	}

	public bool IsUnderwater(Vector2i position)
	{
		return _waterAreas.Any((Rectanglei x) => x.Contains(position));
	}

	public void CreateSplash(LevelLayer layer, SplashType splashType, Vector2i position)
	{
		_splashes.Add(new Splash(this, splashType, position));
	}

	public void CreateBubble(int layer, Vector2i position, int size)
	{
		_level.ObjectManager.AddObject(new ObjectPlacement(_level.CommonResources.GetResourcePath("bubbleobject"), layer, position, new
		{
			Size = size
		}));
	}

	public void Draw(Renderer renderer, Viewport viewport)
	{
		if (!Enabled || !_loaded)
		{
			return;
		}
		DrawSplashes(renderer, viewport);
		foreach (Rectanglei waterArea in _waterAreas)
		{
			Rectangle rectangle = waterArea;
			Draw(renderer, viewport, rectangle);
		}
	}

	public void DrawSplashes(Renderer renderer, Viewport viewport)
	{
		if (_splashes.Count <= 0)
		{
			return;
		}
		using (viewport.ApplyRendererState(renderer))
		{
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			i2dRenderer.ModelMatrix = i2dRenderer.ModelMatrix.Translate(-viewport.Bounds.X, -viewport.Bounds.Y);
			foreach (Splash splash in _splashes)
			{
				splash.Draw(i2dRenderer);
			}
		}
		renderer.DeativateRenderer();
	}

	public void Draw(Renderer renderer, Viewport viewport, Rectanglei waterArea)
	{
		waveTexture = wavesAnimationGroup.Textures[_waveFrame / 2 % 32];
		Rectanglei bounds = viewport.Bounds;
		bounds.Left = Math.Max(bounds.Left, waterArea.Left);
		bounds.Top = Math.Max(bounds.Top, waterArea.Top);
		bounds.Right = Math.Min(bounds.Right, waterArea.Right);
		bounds.Bottom = Math.Min(bounds.Bottom, waterArea.Bottom);
		bounds.X -= viewport.Bounds.X;
		bounds.Y -= viewport.Bounds.Y;
		Vector2 scale = viewport.Scale;
		bounds.X = (int)(bounds.X * scale.X);
		bounds.Y = (int)(bounds.Y * scale.Y);
		bounds.Width = (int)(bounds.Width * scale.X);
		bounds.Height = (int)(bounds.Height * scale.Y);
		if (bounds.Width >= 0 && !(bounds.Height < 0.0 - (WavePhase + waterArea.Y / 1080.0 * (Math.PI * 2.0))))
		{
			IWaterRenderer waterRenderer = renderer.GetWaterRenderer();
			waterRenderer.HueTarget = HueTarget;
			waterRenderer.HueAmount = HueAmount;
			waterRenderer.SaturationChange = SaturationChange;
			waterRenderer.LuminosityChange = LuminosityChange;
			waterRenderer.WavePhase = 0.0;
			waterRenderer.NumWaves = NumWaves / scale.Y;
			waterRenderer.WaveSize = WaveSize * scale.X;
			waterRenderer.Time = _waterTime;
			offsetX = viewport.Bounds.Left;
			offsetY = viewport.Bounds.Top;
			viewportWaterLevel = bounds.Y;
			waterRenderer.Render(bounds);
		}
	}
}
