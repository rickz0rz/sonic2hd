// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZBLOCK.CPZBlockInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZBLOCK;

public class CPZBlockInstance : Platform
{
	private AnimationInstance _animation;
	private CPZBlockInstance.BlockType _blockType = CPZBlockInstance.BlockType.Single;
	private int _movementLength = 384;
	private bool _flipX;
	private CPZBlockInstance.TurnDirection _turnDirection = CPZBlockInstance.TurnDirection.Clockwise;
	private int _initialState;
	private Vector2i _state0position;
	private CPZBlockInstance[] _siblings = new CPZBlockInstance[0];
	private bool _activated;
	private int _ticks;

	[StateVariable]
	private CPZBlockInstance.BlockType BlockKind
	{
		get => _blockType;
		set => _blockType = value;
	}

	[StateVariable]
	private CPZBlockInstance.TurnDirection Direction
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
		if (_blockType == CPZBlockInstance.BlockType.Single)
		{
			_state0position = Position - GetStatePosition(_initialState);
			DesignBounds = new Rectanglei(-64, -64, 128 /*0x80*/, 128 /*0x80*/);
		}
		else
		{
			_state0position = Position;
			_siblings = new CPZBlockInstance[3];
			for (int index = 0; index < 3; ++index)
			{
				CPZBlockInstance cpzBlockInstance = Level.ObjectManager.AddSubObject<CPZBlockInstance>(this);
				cpzBlockInstance._blockType = CPZBlockInstance.BlockType.None;
				cpzBlockInstance.Position = Position + new Vector2i((index + 1) * 128 /*0x80*/, 0);
				cpzBlockInstance.LockLifetime = true;
				_siblings[index] = cpzBlockInstance;
			}
			if (_flipX)
			{
				Move(384, 0);
				_siblings[0].Move(128 /*0x80*/, 0);
				_siblings[1].Move(sbyte.MinValue, 0);
				_siblings[2].Move(-384, 0);
			}
			DesignBounds = new Rectanglei(-64, -64, 512 /*0x0200*/, 128 /*0x80*/);
		}
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -64, 128 /*0x80*/, 128 /*0x80*/));
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
			Finish();
		else
			base.OnUpdate();
	}

	protected override void OnUpdateEditor()
	{
		if (ParentObject != null && ParentObject.Finished)
		{
			Finish();
		}
		else
		{
			if (_siblings == null)
				return;
			for (int index = 0; index < _siblings.Length; ++index)
				_siblings[index].Position = Position + new Vector2i((index + 1) * 128 /*0x80*/, 0);
		}
	}

	protected override void OnUpdatePrepare()
	{
		if (ParentObject is CPZBlockInstance)
			return;
		Vector2 positionPrecise = PositionPrecise;
		Vector2[] vector2Array = new Vector2[_siblings.Length];
		for (int index = 0; index < _siblings.Length; ++index)
			vector2Array[index] = _siblings[index].PositionPrecise;
		UpdateMovement();
		for (int index = 0; index < _siblings.Length; ++index)
		{
			_siblings[index]._nextPositionPrecise = _siblings[index].PositionPrecise;
			_siblings[index]._velocityBasedOnNextPosition = _siblings[index]._nextPositionPrecise - vector2Array[index];
		}
		_nextPositionPrecise = PositionPrecise;
		_velocityBasedOnNextPosition = _nextPositionPrecise - positionPrecise;
		PositionPrecise = positionPrecise;
	}

	protected override void UpdateMovement()
	{
		switch (_blockType)
		{
			case CPZBlockInstance.BlockType.Single:
				UpdateMovementSingle();
				break;
			case CPZBlockInstance.BlockType.Stairs:
				UpdateMovementStairs();
				break;
		}
	}

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
		int num = Level.Ticks % 720 / 180;
		int state1;
		int state2;
		if (_turnDirection == CPZBlockInstance.TurnDirection.Clockwise)
		{
			state1 = (_initialState + num) % 4;
			state2 = (state1 + 1) % 4;
		}
		else
		{
			state1 = (_initialState - num + 4) % 4;
			state2 = (state1 + 3) % 4;
		}
		Vector2i statePosition1 = GetStatePosition(state1);
		Vector2i statePosition2 = GetStatePosition(state2);
		double t = 0.5 - Math.Cos(Level.Ticks % 180 / 180.0 * Math.PI) / 2.0;
		PositionPrecise = new Vector2(statePosition1.X == statePosition2.X ? statePosition1.X : MathX.Lerp(statePosition1.X, statePosition2.X, t), statePosition1.Y == statePosition2.Y ? statePosition1.Y : MathX.Lerp(statePosition1.Y, statePosition2.Y, t));
	}

	private void UpdateMovementStairs()
	{
		if (!_activated)
			return;
		int num = _turnDirection == CPZBlockInstance.TurnDirection.Up ? -1 : 1;
		if (_ticks < 30)
		{
			++_ticks;
		}
		else
		{
			if (_ticks > 158)
				return;
			double t = (_ticks - 30) / 128.0;
			PositionPrecise = PositionPrecise with
			{
				Y = MathX.Lerp(_state0position.Y, _state0position.Y + num * 128 /*0x80*/, t)
			};
			for (int index = 0; index < 3; ++index)
			{
				Vector2 positionPrecise = _siblings[index].PositionPrecise with
				{
					Y = MathX.Lerp(_state0position.Y, _state0position.Y + num * ((index + 2) * 128 /*0x80*/), t)
				};
				_siblings[index].PositionPrecise = positionPrecise;
			}
			++_ticks;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id == 1 && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			_activated = true;
			if (ParentObject is CPZBlockInstance parentObject)
				parentObject._activated = true;
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
