using System;
using SonicOrca;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace S2HD.Title;

/// <summary>
/// The background of the title screen: a scrolling sky, an island with water sparkles, and the Death Egg.
/// It also draws the white flash when it appears, and the wipe transition when the title screen closes.
/// </summary>
internal class Background
{
	private const int BackgroundSkyStartX = 1260;
	private const int BackgroundIslandStartX = 1088;
	private const double BackgroundSkyVelocity = -0.05;
	private const double BackgroundIslandVelocity = -0.1;
	private readonly S2HDSonicOrcaGameContext _gameContext;
	[ResourcePath("SONICORCA/TITLE/BACKGROUND/SKY")]
	private ITexture _textureBackgroundSky;
	[ResourcePath("SONICORCA/TITLE/BACKGROUND/ISLAND")]
	private ITexture _textureBackgroundIsland;
	[ResourcePath("SONICORCA/TITLE/BACKGROUND/DEATHEGG")]
	private ITexture _textureBackgroundDeathEgg;
	[ResourcePath("SONICORCA/TITLE/WIPE")]
	private ITexture _textureWipe;
	[ResourcePath("SONICORCA/TITLE/ANIGROUP")]
	private AnimationGroup _animationGroup;
	private AnimationInstance _waterSparkleAnimationInstance;
	private double _backgroundFlash;
	private double _backgroundSkyCentreX;
	private double _backgroundIslandCentreX;
	private int _ticks;
	private int _wipeHeight;
	private bool _wipeTransitionActive;

	public bool Visible { get; set; }

	public Background(S2HDSonicOrcaGameContext gameContext)
	{
		_gameContext = gameContext;
		_gameContext.ResourceTree.FullfillLoadedResourcesByAttribute(this);
	}

	public void Reset()
	{
		_ticks = 0;
		_backgroundFlash = 0.0;
		_backgroundSkyCentreX = BackgroundSkyStartX;
		_backgroundIslandCentreX = BackgroundIslandStartX;
		_waterSparkleAnimationInstance = null;
		Visible = false;
	}

	public void WipeOut() => _wipeTransitionActive = true;

	public void Update()
	{
		if (_ticks == 0)
		{
			Visible = true;
			_backgroundFlash = 1.0;
			_waterSparkleAnimationInstance = new AnimationInstance(_animationGroup, TitleResources.WaterSparkleAnimationIndex);
			_waterSparkleAnimationInstance.AdditiveBlending = true;
		}
		else
		{
			_backgroundFlash = Math.Max(0.0, _backgroundFlash - 1.0 / 32.0);
		}
		_backgroundSkyCentreX += BackgroundSkyVelocity;
		_backgroundIslandCentreX += BackgroundIslandVelocity;
		// Wrap the sky and the island around when they go off the left edge of the screen.
		if (_backgroundSkyCentreX + _textureBackgroundSky.Width / 2 < 0.0)
			_backgroundSkyCentreX = _textureBackgroundSky.Width / 2;
		if (_backgroundIslandCentreX < -_textureBackgroundIsland.Width)
			_backgroundIslandCentreX = 1920 + _textureBackgroundIsland.Width;
		_waterSparkleAnimationInstance.Animate();
		if (_wipeTransitionActive)
		{
			_wipeHeight += 20;
			if (_wipeHeight >= 600)
				_wipeTransitionActive = false;
		}
		++_ticks;
	}

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		g.BlendMode = BlendMode.Alpha;
		if (Visible)
		{
			g.Colour = Colours.White;
			// Repeat the sky texture until it fills the width of the screen.
			int skyCentreX = (int)_backgroundSkyCentreX;
			do
			{
				g.RenderTexture(_textureBackgroundSky, (Vector2)new Vector2i(skyCentreX, 540));
				skyCentreX += _textureBackgroundSky.Width;
			}
			while (skyCentreX - _textureBackgroundSky.Width / 2 < 1920);
			g.RenderTexture(_textureBackgroundDeathEgg, new Vector2(1750.0, 192.0));
			g.RenderTexture(_textureBackgroundIsland, new Vector2(_backgroundIslandCentreX, 540.0));
			_waterSparkleAnimationInstance.Draw(g, new Vector2(_backgroundIslandCentreX + 38.0, 892.0));
		}
		if (_backgroundFlash > 0.0)
			g.RenderQuad(new Colour(_backgroundFlash, 1.0, 1.0, 1.0), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		if (_wipeHeight <= 0)
			return;
		// The wipe closes from the top and from the bottom of the screen.
		Rectanglei destination = new Rectanglei(0, _wipeHeight - _textureWipe.Height, 1920, _textureWipe.Height);
		g.RenderTexture(_textureWipe, (Rectangle)destination);
		destination = new Rectanglei(0, 1080 - _wipeHeight, 1920, _textureWipe.Height);
		g.RenderTexture(_textureWipe, (Rectangle)destination, flipy: true);
	}
}
