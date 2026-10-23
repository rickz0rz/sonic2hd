using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SONICORCA.OBJECTS.CAPSULE;

/// <summary>
/// The animal capsule at the end of a boss act. When a character pushes the button on top, the lock breaks,
/// the door opens and the animals come out. Then the level is complete.
/// </summary>
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

	/// <summary>The number of collision vectors for the body. The button collision vectors come after them.</summary>
	private const int BodyCollisionVectorCount = 10;

	/// <summary>The index of the top collision vector of the button.</summary>
	private const int ButtonTopCollisionVectorIndex = BodyCollisionVectorCount + 1;

	private const int ButtonSpeed = 8;
	private const int ButtonPressedY = 24;
	private const int AnimalCount = 32;

	private AnimationInstance _animationButton;
	private AnimationInstance _animationMain;
	private AnimationInstance _animationLights;
	private AnimationInstance _animationDoor;
	private AnimationInstance _animationDoorLights;
	private AnimationInstance _animationMiddle;
	private int _lastButtonY;
	private int _buttonY;
	private bool _buttonPressed;
	private Lock _lock;
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
		DesignBounds = new Rectanglei(-128, -160, 256, 320);
	}

	protected override void OnStart()
	{
		AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_animationButton = new AnimationInstance(animationGroup, AnimationButton);
		_animationMain = new AnimationInstance(animationGroup);
		if ((Entry.State as CapsuleInstance).Unlocked)
		{
			_buttonPressed = true;
			_doorOpened = true;
			_animationLights = null;
			_animationDoorLights = null;
		}
		else
		{
			_animationLights = new AnimationInstance(animationGroup, AnimationLights);
			_animationDoor = new AnimationInstance(animationGroup, AnimationDoor);
			_animationDoorLights = new AnimationInstance(animationGroup, AnimationDoorLights);
			_animationMiddle = new AnimationInstance(animationGroup, AnimationMiddle);
		}
		_lock = Level.ObjectManager.AddSubObject<Lock>(this);
		_lock.Position = Position + new Vector2i(0, -46);
		_lock.LockLifetime = true;
		CollisionVector[] buttonCollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, _buttonY - 158, 128, 64));
		buttonCollisionVectors[1].Flags = CollisionFlags.Conveyor;
		CollisionVectors = new CollisionVector[BodyCollisionVectorCount]
		{
			new CollisionVector(this, new Vector2i(-128, -42), new Vector2i(-100, -64)),
			new CollisionVector(this, new Vector2i(-100, -64), new Vector2i(-64, -82)),
			new CollisionVector(this, new Vector2i(-64, -82), new Vector2i(-28, -94)),
			new CollisionVector(this, new Vector2i(-28, -94), new Vector2i(28, -94)),
			new CollisionVector(this, new Vector2i(28, -94), new Vector2i(64, -82)),
			new CollisionVector(this, new Vector2i(64, -82), new Vector2i(100, -64)),
			new CollisionVector(this, new Vector2i(100, -64), new Vector2i(128, -42)),
			new CollisionVector(this, new Vector2i(128, -42), new Vector2i(128, 160)),
			new CollisionVector(this, new Vector2i(128, 160), new Vector2i(-128, 160)),
			new CollisionVector(this, new Vector2i(-128, 160), new Vector2i(-128, -42))
		}.Concat(buttonCollisionVectors).ToArray();
		for (int index = 0; index < BodyCollisionVectorCount; ++index)
		{
			CollisionVectors[index].Flags |= CollisionFlags.NoAngle;
		}
		// The two vertical sides.
		CollisionVectors[7].Flags |= CollisionFlags.NoPathFollowing;
		CollisionVectors[9].Flags |= CollisionFlags.NoPathFollowing;
	}

	protected override void OnStop()
	{
		if (!_buttonPressed)
		{
			_lock.Finish();
		}
	}

	protected override void OnUpdate()
	{
		UpdateButton();
		UpdateDoor();
		UpdateButtonCollision();
	}

	/// <summary>Moves the four button collision vectors by the distance that the button moved in this frame.</summary>
	private void UpdateButtonCollision()
	{
		Vector2i offset = new Vector2i(0, _buttonY - _lastButtonY);
		for (int index = BodyCollisionVectorCount; index < BodyCollisionVectorCount + 4; ++index)
		{
			CollisionVector collisionVector = CollisionVectors[index];
			collisionVector.RelativeA += offset;
			collisionVector.RelativeB += offset;
		}
	}

	private void UpdateButton()
	{
		_lastButtonY = _buttonY;
		if (Level.ObjectManager.IsCharacterStandingOn(CollisionVectors[ButtonTopCollisionVectorIndex]))
		{
			_buttonY = Math.Min(_buttonY + ButtonSpeed, ButtonPressedY);
			if (_buttonY == ButtonPressedY && !_buttonPressed)
			{
				OnButtonPress();
			}
		}
		else
		{
			_buttonY = Math.Max(0, _buttonY - ButtonSpeed);
		}
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
			{
				return;
			}
			--_openDoorDelay;
			if (_openDoorDelay == 0)
			{
				OpenDoor();
			}
		}
		else
		{
			if (_completeLevelDelay <= 0)
			{
				return;
			}
			--_completeLevelDelay;
			if (_completeLevelDelay == 0)
			{
				Level.CompleteLevel();
				LockLifetime = false;
			}
		}
	}

	private void OpenDoor()
	{
		_doorOpened = true;
		_animationDoor.Index = AnimationDoorOpening;
		_animationMiddle.Index = AnimationMiddleOpening;
		_animationDoor.Cycles = 0;
		_animationMiddle.Cycles = 0;
		CreateAnimals();
	}

	/// <summary>
	/// Releases the animals at random positions along the bottom of the capsule, each after a short random delay.
	/// Sets the delay before the level is complete.
	/// </summary>
	private void CreateAnimals()
	{
		int[] offsetsX = Enumerable.Range(0, AnimalCount).Select(x => (int)(x * 6.0) - 96).OrderBy(x => Level.Random.Next()).ToArray();
		int delay = 60;
		for (int index = 0; index < AnimalCount; ++index)
		{
			Vector2i offset = new Vector2i(offsetsX[index], 32);
			Level.CreateRandomAnimalObject(Level.Map.Layers.IndexOf(Layer), Position + offset, offset.X <= 0 ? -1 : 1, delay);
			delay += Level.Random.Next(5, 10);
		}
		_completeLevelDelay = delay + 120;
	}

	protected override void OnAnimate()
	{
		_animationButton.Animate();
		_animationMain.Animate();
		_animationLights?.Animate();
		_animationDoor?.Animate();
		_animationDoorLights?.Animate();
		_animationMiddle?.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.Render(_animationButton, new Vector2(0.0, _buttonY - 128));
		objectRenderer.Render(_animationMain);
		if (!viewOptions.Shadows && _animationLights != null)
		{
			objectRenderer.Render(_animationLights);
		}
		if (_animationDoor != null && _animationDoor.Cycles == 0 || !_doorOpened)
		{
			objectRenderer.Render(_animationDoor, new Vector2(0.0, 16.0));
		}
		if (!viewOptions.Shadows && _animationDoorLights != null)
		{
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animationDoorLights, new Vector2(0.0, 16.0));
			objectRenderer.EmitsLight = false;
		}
		if (_animationMiddle != null && _animationMiddle.Cycles == 0 || !_doorOpened)
		{
			objectRenderer.Render(_animationMiddle, new Vector2(0.0, 16.0));
		}
	}

	/// <summary>The lock on the front of the capsule. It breaks and falls away when the button is pressed.</summary>
	private class Lock : ActiveObject
	{
		private AnimationInstance _animation;
		private AnimationInstance _animationLight;
		private Vector2 _velocity = new Vector2(32.0, -16.0);
		private double _rotation;
		private bool _broken;

		protected override void OnStart()
		{
			AnimationGroup animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
			_animation = new AnimationInstance(animationGroup, AnimationLock);
			_animationLight = new AnimationInstance(animationGroup, AnimationLockLight);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (_broken)
			{
				PositionPrecise = PositionPrecise + _velocity;
				_velocity += new Vector2(0.0, 0.875);
			}
		}

		public void Break()
		{
			_broken = true;
			_animation.Index = AnimationLockBroken;
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
			if (_broken)
			{
				_rotation += Math.PI / 32.0;
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				if (_rotation != 0.0)
				{
					objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(_rotation);
				}
				objectRenderer.Render(_animation, (Vector2)new Vector2i(-2, 0));
				if (!viewOptions.Shadows && !_broken)
				{
					objectRenderer.EmitsLight = true;
					objectRenderer.Render(_animationLight, (Vector2)new Vector2i(-2, 0));
				}
			}
		}
	}
}
