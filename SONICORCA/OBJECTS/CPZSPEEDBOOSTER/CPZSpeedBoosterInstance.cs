// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZSPEEDBOOSTER.CPZSpeedBoosterInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZSPEEDBOOSTER;

public class CPZSpeedBoosterInstance : ActiveObject
{
	private const int AnimationFrame = 0;
	private const int AnimationCore = 1;
	private const int AnimationArrows = 2;
	private const int AnimationSpinner = 3;
	private const int AnimationSmallWheel = 4;
	private AnimationInstance _animationFrame;
	private AnimationInstance _animationCore;
	private AnimationInstance _animationArrows;
	private AnimationInstance _animationSpinner;
	private AnimationInstance _animationSmallWheel;
	private int _strength = 128 /*0x80*/;

	[StateVariable]
	private int Strength
	{
		get => _strength;
		set => _strength = value;
	}

	protected override void OnStart()
	{
		AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationFrame = new AnimationInstance(loadedResource);
		_animationCore = new AnimationInstance(loadedResource, 1);
		_animationArrows = new AnimationInstance(loadedResource, 2);
		_animationSpinner = new AnimationInstance(loadedResource, 3);
		_animationSmallWheel = new AnimationInstance(loadedResource, 4);
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -64, -64, 128 /*0x80*/, 32 /*0x20*/)
		};
		DesignBounds = new Rectanglei(-158, -188, 316, 376);
		Priority = 1512;
		Level.ObjectManager.AddSubObject<CPZSpeedBoosterInstance.BackWheel>(this);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (activeObject.IsAirborne || _strength == 0)
			return;
		if (activeObject.GroundVelocity < Math.Abs(_strength) || Math.Sign(activeObject.GroundVelocity) != Math.Sign(_strength))
		{
			activeObject.GroundVelocity = _strength;
			activeObject.SlopeLockTicks = 15;
			activeObject.Facing = Math.Sign(_strength);
			bool flag = false;
			double x = Level.Camera.Velocity.X;
			if (_strength < 0)
				flag = x >= 0.0;
			else if (_strength > 0)
				flag = x <= 0.0;
			if (flag)
				activeObject.CameraProperties.Delay = new Vector2i(16 /*0x10*/, CameraProperties.Delay.Y);
		}
		Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPRING");
	}

	protected override void OnAnimate()
	{
		if (_strength == 0)
			return;
		_animationFrame.Animate();
		_animationCore.Animate();
		_animationArrows.Animate();
		_animationSpinner.Animate();
		_animationSmallWheel.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_strength == 0)
			return;
		bool flipX = _strength < 0;
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_animationFrame, new Vector2(0.0, 0.0));
		objectRenderer.Render(_animationCore, new Vector2(0.0, 0.0));
		objectRenderer.Render(_animationArrows, new Vector2(0.0, 0.0), flipX);
		objectRenderer.Render(_animationSpinner, new Vector2(64.0, 0.0), !flipX);
		objectRenderer.Render(_animationSmallWheel, new Vector2(-112.0, 0.0), flipX);
		objectRenderer.Render(_animationSmallWheel, new Vector2(112.0, 0.0), flipX);
	}

	private class BackWheel : ActiveObject
	{
		protected override void OnUpdate()
		{
			if (ParentObject is CPZSpeedBoosterInstance parentObject && !parentObject.Finished)
				return;
			Finish();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (!(ParentObject is CPZSpeedBoosterInstance parentObject) || parentObject._strength == 0)
				return;
			bool flipX = parentObject._strength < 0;
			renderer.GetObjectRenderer().Render(parentObject._animationSpinner, new Vector2(-64.0, 0.0), flipX);
		}
	}
}
