using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SPIKES;

/// <summary>
/// A solid block of spikes that points in one of four directions. The pointed side hurts a character. A block has
/// one to four spike groups side by side. Moving spikes go back into their base and come out again at a fixed
/// interval.
/// </summary>
public class SpikesInstance : ActiveObject
{
	/// <summary>The collision vector ID of the pointed side.</summary>
	private const int SpikeCollisionId = 1;

	/// <summary>
	/// The number of ticks between two movements of moving spikes: 4 ticks of movement and 60 ticks of wait. One
	/// cycle (out and back) is 128 ticks, as in the original game.
	/// </summary>
	private const int MovementInterval = 64;

	/// <summary>The size of one spike group.</summary>
	private const int GroupSize = 128;

	private AnimationInstance _animation;
	private SpikesDirection _direction;
	private bool _moving;
	private SpikesMovementAxis _movementAxis;
	private int _count = 1;
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

	/// <summary>
	/// The movement of the original game (subtype bits 0-3): the spikes move 128 units down (vertical) or to the
	/// right (horizontal), whatever their direction, and back again. This replaces <see cref="Movement"/>.
	/// </summary>
	[StateVariable]
	private SpikesMovementAxis MovementAxis
	{
		get => _movementAxis;
		set => _movementAxis = value;
	}

	/// <summary>The number of spike groups side by side, from 1 to 4.</summary>
	[StateVariable]
	private int Count
	{
		get => _count;
		set => _count = value;
	}

	public SpikesInstance()
	{
		DesignBounds = new Rectanglei(-64, -64, 128, 128);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpikesType.AnimationGroupResourceKey));
		_count = Math.Clamp(_count, 1, 4);
		int length = GroupSize * _count;
		Rectanglei bounds = _direction == SpikesDirection.Up || _direction == SpikesDirection.Down
			? new Rectanglei(-length / 2, -GroupSize / 2, length, GroupSize)
			: new Rectanglei(-GroupSize / 2, -length / 2, GroupSize, length);
		DesignBounds = bounds;
		CollisionVectors = CollisionVector.FromRectangle(this, bounds);

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
		if (_movementAxis != SpikesMovementAxis.None)
		{
			_moving = true;
			_targetDisplacement = _movementAxis == SpikesMovementAxis.Vertical ? new Vector2i(0, 128) : new Vector2i(128, 0);
			_velocity = _movementAxis == SpikesMovementAxis.Vertical ? new Vector2i(0, 32) : new Vector2i(32, 0);
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
			// The model matrix turns the offset of each group with the spikes.
			for (int i = 0; i < _count; i++)
				objectRenderer.Render(_animation, new Vector2((i - (_count - 1) / 2.0) * GroupSize, 0.0));
		}
	}

	private enum SpikesDirection
	{
		Up,
		Right,
		Down,
		Left,
	}

	private enum SpikesMovementAxis
	{
		None,
		Vertical,
		Horizontal,
	}
}
