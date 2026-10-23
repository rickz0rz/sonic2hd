// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZWATERPLATFORM.CPZWaterPlatformInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZWATERPLATFORM;

/// <summary>
/// A platform on wheels in Chemical Plant Zone that moves in a line. It glows on a fixed cycle.
/// The wheels turn in the direction of movement.
/// </summary>
public class CPZWaterPlatformInstance : Platform
{
	private const int AnimationPlatform = 0;
	private const int AnimationPlatformGlow = 1;
	private const int AnimationWheelsMoveRight = 2;
	private const int AnimationWheelsMoveLeft = 3;

	/// <summary>The number of frames in one glow cycle.</summary>
	private const int GlowPeriod = 120;

	private AnimationInstance _animationPlatform;
	private AnimationInstance _animationPlatformGlow;
	private AnimationInstance _animationWheel;
	private double _glow;

	public CPZWaterPlatformInstance()
	{
		DesignBounds = new Rectanglei(-132, -62, 265, 125);
		Linear = true;
	}

	protected override void OnStart()
	{
		base.OnStart();
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationPlatform = new AnimationInstance(animationGroup);
		_animationPlatformGlow = new AnimationInstance(animationGroup, AnimationPlatformGlow);
		_animationWheel = new AnimationInstance(animationGroup, AnimationWheelsMoveLeft);
		CollisionVectors = new CollisionVector[1]
		{
			new CollisionVector(this, new Vector2i(-96, -32), new Vector2i(96, -32), flags: CollisionFlags.Conveyor)
		};
	}

	protected override void OnAnimate()
	{
		_glow = MathX.Clamp(0.0, (1.0 - Math.Cos(2.0 * Math.PI * (Level.Ticks % GlowPeriod) / (GlowPeriod - 1.0))) / 2.0, 1.0);
		_animationPlatform.Animate();
		_animationPlatformGlow.Animate();
		// The wheels turn only while the platform moves.
		switch (Math.Sign(Position.X - LastPosition.X))
		{
			case -1:
				_animationWheel.Index = AnimationWheelsMoveLeft;
				_animationWheel.Animate();
				break;
			case 1:
				_animationWheel.Index = AnimationWheelsMoveRight;
				_animationWheel.Animate();
				break;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_animationPlatform);
		objectRenderer.MultiplyColour = new Colour(_glow, 1.0, 1.0, 1.0);
		objectRenderer.Render(_animationPlatformGlow);
		objectRenderer.MultiplyColour = Colours.White;
		objectRenderer.Render(_animationWheel, (Vector2)new Vector2i(-78, 0));
		objectRenderer.Render(_animationWheel, (Vector2)new Vector2i(78, 0));
	}
}
