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

/// <summary>
/// A floating platform in Chemical Plant Zone, in a small or a large size. The strip on the platform glows when a
/// character stands on it. The barometer twitches while the platform goes up. The small platform also fades
/// between a blue and a green glow.
/// </summary>
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
	private PlatformSize _size;
	private int _lastY;
	private bool _rising;
	private double _glowOpacity;

	/// <summary>1 for a full green glow, -1 for a full blue glow.</summary>
	private double _blueGreenBalance;

	private int _switchBlueGreenBalanceTimePeriod;
	private EaseTimeline _switchBlueGreenBalanceEaseTimeline;

	[StateVariable]
	private PlatformSize Size
	{
		get => _size;
		set => _size = value;
	}

	public CPZPlatformInstance() => DesignBounds = new Rectanglei(-130, -74, 260, 148);

	protected override void OnStart()
	{
		base.OnStart();
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		if (Size == PlatformSize.Large)
		{
			_animationBase = new AnimationInstance(animationGroup);
			_animationStripGlow = new AnimationInstance(animationGroup, AnimationStripGlow);
			_animationStripBarometerA = new AnimationInstance(animationGroup, AnimationBarometerTwitching);
			_animationStripBarometerB = new AnimationInstance(animationGroup, AnimationBarometerStill);
			_animationLiquid = new AnimationInstance(animationGroup, AnimationLiquid);
		}
		else
		{
			_animationBase = new AnimationInstance(animationGroup, AnimationSmallBase);
			_animationStripGlow = new AnimationInstance(animationGroup, AnimationSmallStripGlow);
			_animationStripGlowBlue = new AnimationInstance(animationGroup, AnimationSmallStripGlowBlue);
			_animationStripGlowGreen = new AnimationInstance(animationGroup, AnimationSmallStripGlowGreen);
			_animationStripBarometerA = new AnimationInstance(animationGroup, AnimationBarometerTwitching);
			_animationStripBarometerB = new AnimationInstance(animationGroup, AnimationBarometerStill);
			_animationLiquid = new AnimationInstance(animationGroup, AnimationSmallLiquid);
		}
		int halfWidth = Size == PlatformSize.Small ? 96 : 128;
		CollisionVectors = new CollisionVector[1]
		{
			new CollisionVector(this, new Vector2i(-halfWidth, -64), new Vector2i(halfWidth, -64), flags: CollisionFlags.Conveyor | CollisionFlags.Solid)
		};
	}

	protected override void OnAnimate()
	{
		// The glow is green in the first and last quarter of the movement period, and blue in the middle half.
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

		bool wasRising = _rising;
		if (Position.Y != _lastY)
		{
			_rising = Position.Y <= _lastY;
		}
		_lastY = Position.Y;
		if (!wasRising && _rising)
		{
			_animationStripBarometerB.Index = AnimationBarometerStillToTwitching;
		}
		else if (wasRising && !_rising)
		{
			_animationStripBarometerB.Index = AnimationBarometerTwitchingToStill;
		}

		_animationBase.Animate();
		_animationStripGlow.Animate();
		_animationStripBarometerA.Animate();
		_animationStripBarometerB.Animate();
		_animationLiquid.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (Size == PlatformSize.Large)
		{
			objectRenderer.Render(_animationBase);
			if (_glowOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(_glowOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlow);
				objectRenderer.MultiplyColour = Colours.White;
			}
		}
		else
		{
			objectRenderer.Render(_animationBase);
			double greenOpacity = (_blueGreenBalance + 1.0) / 2.0;
			double blueOpacity = 1.0 - greenOpacity;
			if (blueOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(blueOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlowBlue);
			}
			if (greenOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(greenOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlowGreen);
			}
			if (_glowOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(_glowOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationStripGlow);
			}
			objectRenderer.MultiplyColour = Colours.White;
		}
		objectRenderer.Render(_animationStripBarometerA, new Vector2(-15.0, -14.0), true);
		objectRenderer.Render(_animationStripBarometerB, new Vector2(15.0, -14.0));
		objectRenderer.Render(_animationLiquid);
	}

	private enum PlatformSize
	{
		Small,
		Large,
	}
}
