// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPIKES.SpikesInstance
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
namespace SONICORCA.OBJECTS.SPIKES;

public class SpikesInstance : ActiveObject
{
	private AnimationInstance _animation;
	private SpikesInstance.SpikesDirection _direction;
	private bool _moving;
	private Vector2i _velocity;
	private Vector2i _currentDisplacement;
	private Vector2i _targetDisplacement;
	private int _movementDelay;
	private bool _shouldBeAtTarget;

	[StateVariable]
	private SpikesInstance.SpikesDirection Direction
	{
		get => _direction;
		set => _direction = value;
	}

	[StateVariable]
	private bool Movement
	{
		get => _moving;
		set => _moving = value;
	}

	public SpikesInstance()
	{
		DesignBounds = new Rectanglei(-64, -64, 128 /*0x80*/, 128 /*0x80*/);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(-64.0, -64.0, 128.0, 128.0));
		int index;
		switch (_direction)
		{
			case SpikesInstance.SpikesDirection.Right:
				index = 2;
				_targetDisplacement = new Vector2i(128 /*0x80*/, 0);
				_velocity = new Vector2i(32 /*0x20*/, 0);
				break;
			case SpikesInstance.SpikesDirection.Down:
				index = 3;
				_targetDisplacement = new Vector2i(0, sbyte.MinValue);
				_velocity = new Vector2i(0, -32);
				break;
			case SpikesInstance.SpikesDirection.Left:
				index = 0;
				_targetDisplacement = new Vector2i(sbyte.MinValue, 0);
				_velocity = new Vector2i(-32, 0);
				break;
			default:
				index = 1;
				_targetDisplacement = new Vector2i(0, 128 /*0x80*/);
				_velocity = new Vector2i(0, 32 /*0x20*/);
				break;
		}
		CollisionVectors[index].Id = 1;
		CollisionVectors[index].Flags |= CollisionFlags.Conveyor | CollisionFlags.Solid;
		_movementDelay = 60;
		_shouldBeAtTarget = true;
		Priority = -256;
	}

	protected override void OnUpdate()
	{
		if (!_moving)
			return;
		--_movementDelay;
		if (_movementDelay <= 0)
		{
			_movementDelay = 60;
			_shouldBeAtTarget = !_shouldBeAtTarget;
			_velocity *= -1;
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/SPIKESLASH"));
		}
		if (_shouldBeAtTarget)
		{
			if (!(_currentDisplacement != _targetDisplacement))
				return;
			_currentDisplacement += _velocity;
			Position = Position + _velocity;
		}
		else
		{
			if (!((Vector2)_currentDisplacement != new Vector2()))
				return;
			_currentDisplacement += _velocity;
			Position = Position + _velocity;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		Vector2i position = activeObject.Position;
		int x1 = position.X;
		position = Position;
		int x2 = position.X;
		activeObject.Hurt(Math.Sign(x1 - x2), PlayerDeathCause.Spikes);
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (_direction != SpikesInstance.SpikesDirection.Up)
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Math.PI / 2.0 * (double)_direction);
			objectRenderer.Render(_animation);
		}
	}

	private enum SpikesDirection
	{
		Up,
		Right,
		Down,
		Left,
	}
}
