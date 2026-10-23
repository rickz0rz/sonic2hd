// Decompiled with JetBrains decompiler
// Type: S2HD.Title.Background
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace S2HD.Title;

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
		_backgroundSkyCentreX = 1260.0;
		_backgroundIslandCentreX = 1088.0;
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
			_waterSparkleAnimationInstance = new AnimationInstance(_animationGroup, 10);
			_waterSparkleAnimationInstance.AdditiveBlending = true;
		}
		else
			_backgroundFlash = Math.Max(0.0, _backgroundFlash - 1.0 / 32.0);
		_backgroundSkyCentreX += -0.05;
		_backgroundIslandCentreX += -0.1;
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
		I2dRenderer renderer1 = renderer.Get2dRenderer();
		renderer1.BlendMode = BlendMode.Alpha;
		if (Visible)
		{
			renderer1.Colour = Colours.White;
			int backgroundSkyCentreX = (int)_backgroundSkyCentreX;
			do
			{
				renderer1.RenderTexture(_textureBackgroundSky, (Vector2)new Vector2i(backgroundSkyCentreX, 540));
				backgroundSkyCentreX += _textureBackgroundSky.Width;
			}
			while (backgroundSkyCentreX - _textureBackgroundSky.Width / 2 < 1920);
			renderer1.RenderTexture(_textureBackgroundDeathEgg, new Vector2(1750.0, 192.0));
			renderer1.RenderTexture(_textureBackgroundIsland, new Vector2(_backgroundIslandCentreX, 540.0));
			_waterSparkleAnimationInstance.Draw(renderer1, new Vector2(_backgroundIslandCentreX + 38.0, 892.0));
		}
		if (_backgroundFlash > 0.0)
			renderer1.RenderQuad(new Colour(_backgroundFlash, 1.0, 1.0, 1.0), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		if (_wipeHeight <= 0)
			return;
		Rectanglei destination = new Rectanglei(0, _wipeHeight - _textureWipe.Height, 1920, _textureWipe.Height);
		renderer1.RenderTexture(_textureWipe, (Rectangle)destination);
		destination = new Rectanglei(0, 1080 - _wipeHeight, 1920, _textureWipe.Height);
		renderer1.RenderTexture(_textureWipe, (Rectangle)destination, flipy: true);
	}
}
