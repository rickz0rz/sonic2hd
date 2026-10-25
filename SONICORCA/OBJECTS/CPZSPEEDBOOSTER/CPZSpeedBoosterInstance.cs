using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CPZSPEEDBOOSTER;

/// <summary>
/// A speed booster in Chemical Plant Zone. A character on the ground that touches it moves at a fixed speed
/// in the direction of the booster. A strength of 0 makes the booster invisible and inactive.
/// </summary>
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

	/// <summary>The ground speed that the booster gives. A negative value pushes to the left.</summary>
	private int _strength = 128;

	[StateVariable]
	private int Strength
	{
		get => _strength;
		set => _strength = value;
	}

	protected override void OnStart()
	{
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationFrame = new AnimationInstance(animationGroup);
		_animationCore = new AnimationInstance(animationGroup, AnimationCore);
		_animationArrows = new AnimationInstance(animationGroup, AnimationArrows);
		_animationSpinner = new AnimationInstance(animationGroup, AnimationSpinner);
		_animationSmallWheel = new AnimationInstance(animationGroup, AnimationSmallWheel);
		CollisionRectangles = new CollisionRectangle[1]
		{
			new CollisionRectangle(this, 0, -64, -64, 128, 32)
		};
		DesignBounds = new Rectanglei(-158, -188, 316, 376);
		Priority = 1512;
		Level.ObjectManager.AddSubObject<BackWheel>(this);
	}

	protected override void OnCollision(CollisionEvent e)
	{
		ICharacter character = (ICharacter)e.ActiveObject;
		if (character.IsAirborne || _strength == 0)
		{
			return;
		}
		if (character.GroundVelocity < Math.Abs(_strength) || Math.Sign(character.GroundVelocity) != Math.Sign(_strength))
		{
			character.GroundVelocity = _strength;
			character.SlopeLockTicks = 15;
			character.Facing = Math.Sign(_strength);
			// Delay the camera when it does not already move in the direction of the boost.
			bool delayCamera = false;
			double cameraVelocityX = Level.Camera.Velocity.X;
			if (_strength < 0)
			{
				delayCamera = cameraVelocityX >= 0.0;
			}
			else if (_strength > 0)
			{
				delayCamera = cameraVelocityX <= 0.0;
			}
			if (delayCamera)
			{
				character.CameraProperties.Delay = new Vector2i(16, character.CameraProperties.Delay.Y);
			}
		}
		Level.SoundManager.PlaySound(this, "SONICORCA/SOUND/SPRING");
	}

	protected override void OnAnimate()
	{
		if (_strength == 0)
		{
			return;
		}
		_animationFrame.Animate();
		_animationCore.Animate();
		_animationArrows.Animate();
		_animationSpinner.Animate();
		_animationSmallWheel.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_strength == 0)
		{
			return;
		}
		bool flipX = _strength < 0;
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_animationFrame, new Vector2(0.0, 0.0));
		objectRenderer.Render(_animationCore, new Vector2(0.0, 0.0));
		objectRenderer.Render(_animationArrows, new Vector2(0.0, 0.0), flipX);
		objectRenderer.Render(_animationSpinner, new Vector2(64.0, 0.0), !flipX);
		objectRenderer.Render(_animationSmallWheel, new Vector2(-112.0, 0.0), flipX);
		objectRenderer.Render(_animationSmallWheel, new Vector2(112.0, 0.0), flipX);
	}

	/// <summary>
	/// Draws the back spinner of the booster. It is a separate object so that it draws with a different priority.
	/// It ends when the booster ends.
	/// </summary>
	private class BackWheel : ActiveObject
	{
		protected override void OnUpdate()
		{
			if (ParentObject is not CPZSpeedBoosterInstance booster || booster.Finished)
			{
				Finish();
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (ParentObject is not CPZSpeedBoosterInstance booster || booster._strength == 0)
			{
				return;
			}
			bool flipX = booster._strength < 0;
			renderer.GetObjectRenderer().Render(booster._animationSpinner, new Vector2(-64.0, 0.0), flipX);
		}
	}
}
