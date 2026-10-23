// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CAPSULE.CapsuleInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.OBJECTS.CAPSULE;

public class CapsuleInstance : ActiveObject
{
	private const int AnimationMain = 0;
	private const int AnimationLights = 1;
	private const int AnimationDoor = 2;
	private const int AnimationDoorOpening = 3;
	private const int AnimationDoorLights = 4;
	private const int AnimationMiddle = 5;
	private const int AnimationMiddleOpening = 6;
	private const int AnimationButton = 7;
	private const int AnimationLock = 8;
	private const int AnimationLockLight = 9;
	private const int AnimationLockBroken = 10;
	private AnimationInstance _animationButton;
	private AnimationInstance _animationMain;
	private AnimationInstance _animationLights;
	private AnimationInstance _animationDoor;
	private AnimationInstance _animationDoorLights;
	private AnimationInstance _animationMiddle;
	private int _lastButtonY;
	private int _buttonY;
	private bool _buttonPressed;
	private CapsuleInstance.Lock _lock;
	private bool _doorOpened;
	private int _openDoorDelay;
	private int _completeLevelDelay;
	private bool _unlocked;

	[StateVariable]
	private bool Unlocked
	{
		get => _unlocked;
		set => _unlocked = value;
	}

	public CapsuleInstance()
	{
		DesignBounds = new Rectanglei(sbyte.MinValue, -160, 256 /*0x0100*/, 320);
	}

	protected override void OnStart()
	{
		AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_animationButton = new AnimationInstance(loadedResource, 7);
		_animationMain = new AnimationInstance(loadedResource);
		if ((Entry.State as CapsuleInstance).Unlocked)
		{
			_buttonPressed = true;
			_doorOpened = true;
			_animationLights = null;
			_animationDoorLights = null;
		}
		else
		{
			_animationLights = new AnimationInstance(loadedResource, 1);
			_animationDoor = new AnimationInstance(loadedResource, 2);
			_animationDoorLights = new AnimationInstance(loadedResource, 4);
			_animationMiddle = new AnimationInstance(loadedResource, 5);
		}
		_lock = Level.ObjectManager.AddSubObject<CapsuleInstance.Lock>(this);
		_lock.Position = Position + new Vector2i(0, -46);
		_lock.LockLifetime = true;
		CollisionVector[] second = CollisionVector.FromRectangle(this, new Rectanglei(-64, _buttonY - 158, 128 /*0x80*/, 64 /*0x40*/));
		second[1].Flags = CollisionFlags.Conveyor;
		CollisionVectors = (new CollisionVector[10]
		{
	  new CollisionVector( this, new Vector2i( sbyte.MinValue, -42), new Vector2i(-100, -64)),
	  new CollisionVector( this, new Vector2i(-100, -64), new Vector2i(-64, -82)),
	  new CollisionVector( this, new Vector2i(-64, -82), new Vector2i(-28, -94)),
	  new CollisionVector( this, new Vector2i(-28, -94), new Vector2i(28, -94)),
	  new CollisionVector( this, new Vector2i(28, -94), new Vector2i(64 /*0x40*/, -82)),
	  new CollisionVector( this, new Vector2i(64 /*0x40*/, -82), new Vector2i(100, -64)),
	  new CollisionVector( this, new Vector2i(100, -64), new Vector2i(128 /*0x80*/, -42)),
	  new CollisionVector( this, new Vector2i(128 /*0x80*/, -42), new Vector2i(128 /*0x80*/, 160 /*0xA0*/)),
	  new CollisionVector( this, new Vector2i(128 /*0x80*/, 160 /*0xA0*/), new Vector2i( sbyte.MinValue, 160 /*0xA0*/)),
	  new CollisionVector( this, new Vector2i( sbyte.MinValue, 160 /*0xA0*/), new Vector2i( sbyte.MinValue, -42))
		}).Concat<CollisionVector>(second).ToArray<CollisionVector>();
		for (int index = 0; index < 10; ++index)
			CollisionVectors[index].Flags |= CollisionFlags.NoAngle;
		CollisionVectors[7].Flags |= CollisionFlags.NoPathFollowing;
		CollisionVectors[9].Flags |= CollisionFlags.NoPathFollowing;
	}

	protected override void OnStop()
	{
		if (_buttonPressed)
			return;
		_lock.Finish();
	}

	protected override void OnUpdate()
	{
		UpdateButton();
		UpdateDoor();
		UpdateButtonCollision();
	}

	private void UpdateButtonCollision()
	{
		Vector2i vector2i = new Vector2i(0, _buttonY - _lastButtonY);
		for (int index = 10; index < 14; ++index)
		{
			CollisionVector collisionVector = CollisionVectors[index];
			collisionVector.RelativeA += vector2i;
			collisionVector.RelativeB += vector2i;
		}
	}

	private void UpdateButton()
	{
		_lastButtonY = _buttonY;
		if (Level.ObjectManager.IsCharacterStandingOn(CollisionVectors[11]))
		{
			int buttonY = _buttonY;
			_buttonY = Math.Min(_buttonY + 8, 24);
			int num = _buttonY - buttonY;
			if (_buttonY != 24 || _buttonPressed)
				return;
			OnButtonPress();
		}
		else
			_buttonY = Math.Max(0, _buttonY - 8);
	}

	private void OnButtonPress()
	{
		Level.JustAboutToCompleteLevel();
		(Entry.State as CapsuleInstance).Unlocked = true;
		_animationLights = null;
		_animationDoorLights = null;
		_lock.Break();
		_lock.LockLifetime = false;
		_buttonPressed = true;
		_openDoorDelay = 60;
		LockLifetime = true;
	}

	private void UpdateDoor()
	{
		if (!_doorOpened)
		{
			if (!_buttonPressed || _openDoorDelay <= 0)
				return;
			--_openDoorDelay;
			if (_openDoorDelay != 0)
				return;
			OpenDoor();
		}
		else
		{
			if (_completeLevelDelay <= 0)
				return;
			--_completeLevelDelay;
			if (_completeLevelDelay != 0)
				return;
			Level.CompleteLevel();
			LockLifetime = false;
		}
	}

	private void OpenDoor()
	{
		_doorOpened = true;
		_animationDoor.Index = 3;
		_animationMiddle.Index = 6;
		_animationDoor.Cycles = 0;
		_animationMiddle.Cycles = 0;
		CreateAnimals();
	}

	private void CreateAnimals()
	{
		int[] array = Enumerable.Range(0, 32 /*0x20*/).Select<int, int>(x => (int)(x * 6.0) - 96 /*0x60*/).OrderBy<int, int>(x => Level.Random.Next()).ToArray<int>();
		int delay = 60;
		for (int index = 0; index < 32 /*0x20*/; ++index)
		{
			Vector2i vector2i = new Vector2i(array[index], 32 /*0x20*/);
			Level.CreateRandomAnimalObject(Level.Map.Layers.IndexOf(Layer), Position + vector2i, vector2i.X <= 0 ? -1 : 1, delay);
			delay += Level.Random.Next(5, 10);
		}
		_completeLevelDelay = delay + 120;
	}

	protected override void OnAnimate()
	{
		_animationButton.Animate();
		_animationMain.Animate();
		if (_animationLights != null)
			_animationLights.Animate();
		if (_animationDoor != null)
			_animationDoor.Animate();
		if (_animationDoorLights != null)
			_animationDoorLights.Animate();
		if (_animationMiddle == null)
			return;
		_animationMiddle.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_animationButton, new Vector2(0.0, _buttonY - 128 /*0x80*/));
		objectRenderer.Render(_animationMain);
		if (!viewOptions.Shadows && _animationLights != null)
			objectRenderer.Render(_animationLights);
		if (_animationDoor != null && _animationDoor.Cycles == 0 || !_doorOpened)
			objectRenderer.Render(_animationDoor, new Vector2(0.0, 16.0));
		if (!viewOptions.Shadows && _animationDoorLights != null)
		{
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animationDoorLights, new Vector2(0.0, 16.0));
			objectRenderer.EmitsLight = false;
		}
		if ((_animationMiddle == null || _animationMiddle.Cycles != 0) && _doorOpened)
			return;
		objectRenderer.Render(_animationMiddle, new Vector2(0.0, 16.0));
	}

	private class Lock : ActiveObject
	{
		private AnimationInstance _animation;
		private AnimationInstance _animationLight;
		private Vector2 _velocity = new Vector2(32.0, -16.0);
		private double _rotation;
		private bool _broken;

		protected override void OnStart()
		{
			AnimationGroup loadedResource = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
			_animation = new AnimationInstance(loadedResource, 8);
			_animationLight = new AnimationInstance(loadedResource, 9);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (!_broken)
				return;
			PositionPrecise = PositionPrecise + _velocity;
			_velocity += new Vector2(0.0, 0.875);
		}

		public void Break()
		{
			_broken = true;
			_animation.Index = 10;
			CreateExplosionObject();
		}

		private void CreateExplosionObject()
		{
			Level.ObjectManager.AddObject(new ObjectPlacement(Level.CommonResources.GetResourcePath("badnikexplosionobject"), Level.Map.Layers.IndexOf(Layer), Position));
		}

		protected override void OnAnimate()
		{
			_animation.Animate();
			_animationLight.Animate();
			if (!_broken)
				return;
			_rotation += Math.PI / 32.0;
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				if (_rotation != 0.0)
					objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(_rotation);
				objectRenderer.Render(_animation, (Vector2)new Vector2i(-2, 0));
				if (viewOptions.Shadows || _broken)
					return;
				objectRenderer.EmitsLight = true;
				objectRenderer.Render(_animationLight, (Vector2)new Vector2i(-2, 0));
			}
		}
	}
}
