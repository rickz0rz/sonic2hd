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

/// <summary>
/// A solid block of spikes that points in one of four directions. The pointed side hurts a character. Moving spikes
/// go back into their base and come out again at a fixed interval.
/// </summary>
public class SpikesInstance : ActiveObject
{
	/// <summary>The collision vector ID of the pointed side.</summary>
	private const int SpikeCollisionId = 1;

	/// <summary>The number of ticks between two movements of moving spikes.</summary>
	private const int MovementInterval = 60;

	private AnimationInstance _animation;
	private SpikesDirection _direction;
	private bool _moving;
	private Vector2i _velocity;
	private Vector2i _currentDisplacement;
	private Vector2i _targetDisplacement;
	private int _movementDelay;
	private bool _shouldBeAtTarget;

	[StateVariable]
	private SpikesDirection Direction
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
		DesignBounds = new Rectanglei(-64, -64, 128, 128);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpikesType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -64, 128, 128));

		// Select the collision vector of the pointed side (0 = left, 1 = top, 2 = right, 3 = bottom).
		// The target displacement is the position where the spikes are back in their base.
		int spikeVectorIndex;
		switch (_direction)
		{
			case SpikesDirection.Right:
				spikeVectorIndex = 2;
				_targetDisplacement = new Vector2i(128, 0);
				_velocity = new Vector2i(32, 0);
				break;
			case SpikesDirection.Down:
				spikeVectorIndex = 3;
				_targetDisplacement = new Vector2i(0, -128);
				_velocity = new Vector2i(0, -32);
				break;
			case SpikesDirection.Left:
				spikeVectorIndex = 0;
				_targetDisplacement = new Vector2i(-128, 0);
				_velocity = new Vector2i(-32, 0);
				break;
			default:
				spikeVectorIndex = 1;
				_targetDisplacement = new Vector2i(0, 128);
				_velocity = new Vector2i(0, 32);
				break;
		}
		CollisionVectors[spikeVectorIndex].Id = SpikeCollisionId;
		CollisionVectors[spikeVectorIndex].Flags |= CollisionFlags.Conveyor | CollisionFlags.Solid;
		_movementDelay = MovementInterval;
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
			_movementDelay = MovementInterval;
			_shouldBeAtTarget = !_shouldBeAtTarget;
			_velocity *= -1;
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(SpikesType.SpikeMovementSoundResourceKey));
		}
		if (_shouldBeAtTarget)
		{
			if (_currentDisplacement == _targetDisplacement)
				return;
			_currentDisplacement += _velocity;
			Position = Position + _velocity;
		}
		else
		{
			if (_currentDisplacement == new Vector2i())
				return;
			_currentDisplacement += _velocity;
			Position = Position + _velocity;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != SpikeCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Hurt(Math.Sign(character.Position.X - Position.X), PlayerDeathCause.Spikes);
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (_direction != SpikesDirection.Up)
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
