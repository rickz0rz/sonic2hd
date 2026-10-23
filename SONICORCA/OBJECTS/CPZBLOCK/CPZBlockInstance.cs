using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CPZBLOCK;

/// <summary>
/// A solid block in Chemical Plant Zone. A single block moves round the four corners of a square.
/// A stairs block makes three more blocks in a row. When a character stands on the stairs, the blocks move
/// up or down by different distances to make steps.
/// </summary>
public class CPZBlockInstance : Platform
{
	private const int BlockSize = 128;
	private const int StairsSiblingCount = 3;

	/// <summary>The number of frames to move from one corner to the next.</summary>
	private const int CornerMoveDuration = 180;

	/// <summary>The number of frames that the stairs wait after a character steps on them.</summary>
	private const int StairsWaitDuration = 30;

	/// <summary>The number of frames that the stairs move.</summary>
	private const int StairsMoveDuration = 128;

	private AnimationInstance _animation;
	private BlockType _blockType = BlockType.Single;
	private int _movementLength = 384;
	private bool _flipX;
	private TurnDirection _turnDirection = TurnDirection.Clockwise;
	private int _initialState;

	/// <summary>The position at corner 0 (the top left corner of the movement).</summary>
	private Vector2i _state0position;

	private CPZBlockInstance[] _siblings = new CPZBlockInstance[0];
	private bool _activated;
	private int _ticks;

	[StateVariable]
	private BlockType BlockKind
	{
		get => _blockType;
		set => _blockType = value;
	}

	[StateVariable]
	private TurnDirection Direction
	{
		get => _turnDirection;
		set => _turnDirection = value;
	}

	[StateVariable]
	private int Length
	{
		get => _movementLength;
		set => _movementLength = value;
	}

	[StateVariable]
	private int InitialState
	{
		get => _initialState;
		set => _initialState = value;
	}

	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	protected override void OnStart()
	{
		base.OnStart();
		if (_blockType == BlockType.Single)
		{
			_state0position = Position - GetStatePosition(_initialState);
			DesignBounds = new Rectanglei(-64, -64, BlockSize, BlockSize);
		}
		else
		{
			_state0position = Position;
			_siblings = new CPZBlockInstance[StairsSiblingCount];
			for (int index = 0; index < StairsSiblingCount; ++index)
			{
				CPZBlockInstance sibling = Level.ObjectManager.AddSubObject<CPZBlockInstance>(this);
				sibling._blockType = BlockType.None;
				sibling.Position = Position + new Vector2i((index + 1) * BlockSize, 0);
				sibling.LockLifetime = true;
				_siblings[index] = sibling;
			}
			if (_flipX)
			{
				// Reverse the order of the four blocks.
				Move(384, 0);
				_siblings[0].Move(128, 0);
				_siblings[1].Move(-128, 0);
				_siblings[2].Move(-384, 0);
			}
			DesignBounds = new Rectanglei(-64, -64, 512, BlockSize);
		}
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -64, BlockSize, BlockSize));
		CollisionVectors[0].Id = 0;
		CollisionVectors[1].Id = 1;
		CollisionVectors[2].Id = 2;
		CollisionVectors[3].Id = 3;
		CollisionVectors[1].Flags |= CollisionFlags.Conveyor;
		for (int index = 0; index < 4; ++index)
		{
			CollisionVectors[index].Flags |= CollisionFlags.Solid;
			CollisionVectors[index].Flags |= CollisionFlags.NoBalance;
		}
	}

	protected override void OnUpdate()
	{
		if (ParentObject != null && ParentObject.Finished)
		{
			Finish();
		}
		else
		{
			base.OnUpdate();
		}
	}

	protected override void OnUpdateEditor()
	{
		if (ParentObject != null && ParentObject.Finished)
		{
			Finish();
		}
		else if (_siblings != null)
		{
			for (int index = 0; index < _siblings.Length; ++index)
			{
				_siblings[index].Position = Position + new Vector2i((index + 1) * BlockSize, 0);
			}
		}
	}

	/// <summary>
	/// Calculates the next position of this block and its siblings, and the velocity of each.
	/// Then puts this block back at its current position. The siblings do not do this themselves.
	/// </summary>
	protected override void OnUpdatePrepare()
	{
		if (ParentObject is CPZBlockInstance)
		{
			return;
		}
		Vector2 previousPosition = PositionPrecise;
		Vector2[] previousSiblingPositions = new Vector2[_siblings.Length];
		for (int index = 0; index < _siblings.Length; ++index)
		{
			previousSiblingPositions[index] = _siblings[index].PositionPrecise;
		}
		UpdateMovement();
		for (int index = 0; index < _siblings.Length; ++index)
		{
			_siblings[index]._nextPositionPrecise = _siblings[index].PositionPrecise;
			_siblings[index]._velocityBasedOnNextPosition = _siblings[index]._nextPositionPrecise - previousSiblingPositions[index];
		}
		_nextPositionPrecise = PositionPrecise;
		_velocityBasedOnNextPosition = _nextPositionPrecise - previousPosition;
		PositionPrecise = previousPosition;
	}

	protected override void UpdateMovement()
	{
		switch (_blockType)
		{
			case BlockType.Single:
				UpdateMovementSingle();
				break;
			case BlockType.Stairs:
				UpdateMovementStairs();
				break;
		}
	}

	/// <summary>
	/// Returns the position at a corner of the square. 0 is top left, 1 is top right, 2 is bottom right and 3 is bottom left.
	/// </summary>
	private Vector2i GetStatePosition(int state)
	{
		switch (state)
		{
			case 1:
				return _state0position + new Vector2i(_movementLength, 0);
			case 2:
				return _state0position + new Vector2i(_movementLength, _movementLength);
			case 3:
				return _state0position + new Vector2i(0, _movementLength);
			default:
				return _state0position + new Vector2i(0, 0);
		}
	}

	private void UpdateMovementSingle()
	{
		int cornersPassed = Level.Ticks % (4 * CornerMoveDuration) / CornerMoveDuration;
		int fromState;
		int toState;
		if (_turnDirection == TurnDirection.Clockwise)
		{
			fromState = (_initialState + cornersPassed) % 4;
			toState = (fromState + 1) % 4;
		}
		else
		{
			fromState = (_initialState - cornersPassed + 4) % 4;
			toState = (fromState + 3) % 4;
		}
		Vector2i fromPosition = GetStatePosition(fromState);
		Vector2i toPosition = GetStatePosition(toState);
		// Ease in and out with a cosine curve.
		double t = 0.5 - Math.Cos(Level.Ticks % CornerMoveDuration / 180.0 * Math.PI) / 2.0;
		PositionPrecise = new Vector2(
			fromPosition.X == toPosition.X ? fromPosition.X : MathX.Lerp(fromPosition.X, toPosition.X, t),
			fromPosition.Y == toPosition.Y ? fromPosition.Y : MathX.Lerp(fromPosition.Y, toPosition.Y, t));
	}

	private void UpdateMovementStairs()
	{
		if (!_activated)
		{
			return;
		}
		int directionY = _turnDirection == TurnDirection.Up ? -1 : 1;
		if (_ticks < StairsWaitDuration)
		{
			++_ticks;
		}
		else if (_ticks <= StairsWaitDuration + StairsMoveDuration)
		{
			double t = (_ticks - StairsWaitDuration) / 128.0;
			PositionPrecise = PositionPrecise with
			{
				Y = MathX.Lerp(_state0position.Y, _state0position.Y + directionY * BlockSize, t)
			};
			// Each sibling moves one block further than the block before it.
			for (int index = 0; index < StairsSiblingCount; ++index)
			{
				Vector2 siblingPosition = _siblings[index].PositionPrecise with
				{
					Y = MathX.Lerp(_state0position.Y, _state0position.Y + directionY * ((index + 2) * BlockSize), t)
				};
				_siblings[index].PositionPrecise = siblingPosition;
			}
			++_ticks;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		// Id 1 is the top of the block.
		if (e.Id == 1 && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			_activated = true;
			if (ParentObject is CPZBlockInstance parentBlock)
			{
				parentBlock._activated = true;
			}
		}
		base.OnCollision(e);
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}

	private enum BlockType
	{
		None,
		Single,
		Stairs,
	}

	private enum TurnDirection
	{
		Up,
		Down,
		Clockwise,
		AntiClockwise,
	}
}
