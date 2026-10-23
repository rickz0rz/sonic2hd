using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Keeps the water areas of a level, and draws the water effect and the splashes.
/// </summary>
public class WaterManager
{
	private const string SplashEnterResourceKey = "SONICORCA/PARTICLE/SPLASH/ENTER";

	private const string SplashExitResourceKey = "SONICORCA/PARTICLE/SPLASH/EXIT";

	private const string WaveTextureResourceKey = "SONICORCA/OBJECTS/WAVE/TEXTURE";

	private const string WavesAnimationGroupResourceKey = "SONICORCA/OBJECTS/WAVES/ANIGROUP";

	/// <summary>
	/// The number of frames in the waves animation group.
	/// </summary>
	private const int WaveFrameCount = 32;

	/// <summary>
	/// The number of game frames that each wave frame shows.
	/// </summary>
	private const int GameFramesPerWaveFrame = 2;

	private readonly SonicOrcaGameContext _gameContext;

	private readonly Level _level;

	private readonly List<Rectanglei> _waterAreas = new List<Rectanglei>();

	public static float offsetX;

	public static float offsetY;

	public static float viewportWaterLevel;

	public static ITexture waveTexture;

	private AnimationGroup _wavesAnimationGroup;

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
			SpashEnterAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>(SplashEnterResourceKey);
			SpashExitAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>(SplashExitResourceKey);
			waveTexture = resourceTree.GetLoadedResource<ITexture>(WaveTextureResourceKey);
			_wavesAnimationGroup = resourceTree.GetLoadedResource<AnimationGroup>(WavesAnimationGroupResourceKey);
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
		_splashes.RemoveAll(x => x.Finished);
	}

	public void Animate()
	{
		if (Enabled && _loaded)
		{
			_waveFrame++;
			_waterTime += 0.0166f;
			// The multiplication by zero keeps the wave phase at zero. The wave phase does not move.
			WavePhase = 0.0 * MathX.WrapRadians(WavePhase + 1.0 / 32.0);
		}
	}

	public bool IsUnderwater(Vector2i position)
	{
		return _waterAreas.Any(x => x.Contains(position));
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
			Draw(renderer, viewport, waterArea);
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
			I2dRenderer renderer2d = renderer.Get2dRenderer();
			renderer2d.ModelMatrix = renderer2d.ModelMatrix.Translate(-viewport.Bounds.X, -viewport.Bounds.Y);
			foreach (Splash splash in _splashes)
			{
				splash.Draw(renderer2d);
			}
		}
		renderer.DeativateRenderer();
	}

	public void Draw(Renderer renderer, Viewport viewport, Rectanglei waterArea)
	{
		waveTexture = _wavesAnimationGroup.Textures[_waveFrame / GameFramesPerWaveFrame % WaveFrameCount];

		// Clip the water area to the viewport, then convert it to screen coordinates.
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
