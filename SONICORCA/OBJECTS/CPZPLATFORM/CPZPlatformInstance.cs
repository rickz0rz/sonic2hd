// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZPLATFORM.CPZPlatformInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZPLATFORM;

public class CPZPlatformInstance : Platform
{
	private const int AnimationBase = 0;
	private const int AnimationStripGlow = 1;
	private const int AnimationBarometerStill = 2;
	private const int AnimationBarometerTwitching = 3;
	private const int AnimationBarometerStillToTwitching = 4;
	private const int AnimationBarometerTwitchingToStill = 5;
	private const int AnimationLiquid = 6;
	private const int AnimationSmallBase = 7;
	private const int AnimationSmallStripGlow = 8;
	private const int AnimationSmallStripGlowBlue = 9;
	private const int AnimationSmallStripGlowGreen = 10;
	private const int AnimationSmallLiquid = 11;
	private AnimationInstance _animationBase;
	private AnimationInstance _animationStripGlow;
	private AnimationInstance _animationStripGlowBlue;
	private AnimationInstance _animationStripGlowGreen;
	private AnimationInstance _animationStripBarometerA;
	private AnimationInstance _animationStripBarometerB;
	private AnimationInstance _animationLiquid;
	private CPZPlatformInstance.PlatformSize _size;
	private int _lastY;
	private bool _rising;
	private double _glowOpacity;
	private double _blueGreenBalance;
	private int _switchBlueGreenBalanceTimePeriod;
	private EaseTimeline _switchBlueGreenBalanceEaseTimeline;

	[StateVariable]
	private CPZPlatformInstance.PlatformSize Size
	{
		get => _size;
		set => _size = value;
	}

	public CPZPlatformInstance() => DesignBounds = new Rectanglei(-130, -74, 260, 148);

	protected override void OnStart()
	{
		base.OnStart();
		AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		if (Size == CPZPlatformInstance.PlatformSize.Large)
		{
			_animationBase = new AnimationInstance(loadedResource);
			_animationStripGlow = new AnimationInstance(loadedResource, 1);
			_animationStripBarometerA = new AnimationInstance(loadedResource, 3);
			_animationStripBarometerB = new AnimationInstance(loadedResource, 2);
			_animationLiquid = new AnimationInstance(loadedResource, 6);
		}
		else
		{
			_animationBase = new AnimationInstance(loadedResource, 7);
			_animationStripGlow = new AnimationInstance(loadedResource, 8);
			_animationStripGlowBlue = new AnimationInstance(loadedResource, 9);
			_animationStripGlowGreen = new AnimationInstance(loadedResource, 10);
			_animationStripBarometerA = new AnimationInstance(loadedResource, 3);
			_animationStripBarometerB = new AnimationInstance(loadedResource, 2);
			_animationLiquid = new AnimationInstance(loadedResource, 11);
		}
		int x = Size == CPZPlatformInstance.PlatformSize.Small ? 96 /*0x60*/ : 128 /*0x80*/;
		CollisionVectors = new CollisionVector[1]
		{
	  new CollisionVector( this, new Vector2i(-x, -64), new Vector2i(x, -64), flags: CollisionFlags.Conveyor | CollisionFlags.Solid)
		};
	}

	protected override void OnAnimate()
	{
		if (TimePeriod != _switchBlueGreenBalanceTimePeriod)
		{
			_switchBlueGreenBalanceTimePeriod = TimePeriod;
			_switchBlueGreenBalanceEaseTimeline = new EaseTimeline(new EaseTimeline.Entry[4]
			{
		new EaseTimeline.Entry(TimePeriod / 4 - 8, 1.0),
		new EaseTimeline.Entry(TimePeriod / 4 + 8, -1.0),
		new EaseTimeline.Entry(TimePeriod * 3 / 4 - 8, -1.0),
		new EaseTimeline.Entry(TimePeriod * 3 / 4 + 8, 1.0)
			});
		}
		_blueGreenBalance = TimePeriod != 0 ? _switchBlueGreenBalanceEaseTimeline.GetValueAt(CurrentTime) : 0.0;
		_glowOpacity = MathX.GoTowards(_glowOpacity, IsCharacterInteractingWithPlatform() ? 1.0 : 0.0, 1.0 / 30.0);
		bool rising = _rising;
		Vector2i position = Position;
		if (position.Y != _lastY)
		{
			position = Position;
			_rising = position.Y <= _lastY;
		}
		position = Position;
		_lastY = position.Y;
		if (!rising && _rising)
			_animationStripBarometerB.Index = 4;
		else if (rising && !_rising)
			_animationStripBarometerB.Index = 5;
		_animationBase.Animate();
		_animationStripGlow.Animate();
		_animationStripBarometerA.Animate();
		_animationStripBarometerB.Animate();
		_animationLiquid.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (Size == CPZPlatformInstance.PlatformSize.Large)
		{
			objectRenderer.Render(_animationBase);
			if (_glowOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(_glowOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlow);
				objectRenderer.MultiplyColour = Colours.White;
			}
			objectRenderer.Render(_animationStripBarometerA, new Vector2(-15.0, -14.0), true);
			objectRenderer.Render(_animationStripBarometerB, new Vector2(15.0, -14.0));
			objectRenderer.Render(_animationLiquid);
		}
		else
		{
			objectRenderer.Render(_animationBase);
			double a1 = (_blueGreenBalance + 1.0) / 2.0;
			double a2 = 1.0 - a1;
			if (a2 > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(a2, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlowBlue);
			}
			if (a1 > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(a1, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlowGreen);
			}
			if (_glowOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(_glowOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlow);
			}
			objectRenderer.MultiplyColour = Colours.White;
			objectRenderer.Render(_animationStripBarometerA, new Vector2(-15.0, -14.0), true);
			objectRenderer.Render(_animationStripBarometerB, new Vector2(15.0, -14.0));
			objectRenderer.Render(_animationLiquid);
		}
	}

	private enum PlatformSize
	{
		Small,
		Large,
	}
}
