// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.COCONUTS.CoconutsInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.COCONUTS;

public class CoconutsInstance : Badnik
{
	private static readonly IReadOnlyList<CoconutsInstance.ClimbDataItem> ClimbData = new CoconutsInstance.ClimbDataItem[6]
	{
	new CoconutsInstance.ClimbDataItem(-1, 32 /*0x20*/),
	new CoconutsInstance.ClimbDataItem(1, 24),
	new CoconutsInstance.ClimbDataItem(-1, 16 /*0x10*/),
	new CoconutsInstance.ClimbDataItem(1, 40),
	new CoconutsInstance.ClimbDataItem(-1, 32 /*0x20*/),
	new CoconutsInstance.ClimbDataItem(1, 16 /*0x10*/)
	};
	private const int AnimationResting = 0;
	private const int AnimationClimbing = 1;
	private const int AnimationTurning = 2;
	private const int AnimationThrowing = 3;
	private AnimationInstance _animation;
	private CoconutsInstance.StateType _state;
	private int _stateDuration;
	private int _throwWaitDuration;
	private int _climbDataIndex;
	private bool _facingRight;

	public CoconutsInstance() => DesignBounds = new Rectanglei(-96, -100, 192 /*0xC0*/, 200);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = 0;
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -48, -64, 96 /*0x60*/, 128 /*0x80*/)
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_state)
		{
			case CoconutsInstance.StateType.Resting:
				UpdateResting();
				break;
			case CoconutsInstance.StateType.Climbing:
				UpdateClimbing();
				break;
			case CoconutsInstance.StateType.PreThrowing:
				UpdatePreThrowing();
				break;
			case CoconutsInstance.StateType.PostThrowing:
				UpdatePostThrowing();
				break;
		}
	}

	private void UpdateResting()
	{
		ICharacter closestCharacterTo = Level.ObjectManager.GetClosestCharacterTo((Vector2)Position);
		if (closestCharacterTo != null)
		{
			bool flag = closestCharacterTo.Position.X > Position.X;
			if (_facingRight != flag)
			{
				_facingRight = flag;
				_animation.Index = 2;
			}
			double num1 = Math.Abs(Position.X - closestCharacterTo.Position.X);
			double num2 = Math.Abs(Position.Y - closestCharacterTo.Position.Y);
			if (num1 < 384.0 && num2 < 400.0)
			{
				if (_throwWaitDuration == 0)
				{
					_state = CoconutsInstance.StateType.PreThrowing;
					_stateDuration = 8;
					_throwWaitDuration = 32 /*0x20*/;
					_animation.Index = 3;
					return;
				}
				--_throwWaitDuration;
			}
		}
		--_stateDuration;
		if (_stateDuration >= 0)
			return;
		_state = CoconutsInstance.StateType.Climbing;
		_animation.Index = 1;
		UpdateClimbIndex();
	}

	private void UpdateClimbing()
	{
		--_stateDuration;
		if (_stateDuration <= 0)
		{
			_state = CoconutsInstance.StateType.Resting;
			_animation.Index = 0;
			_stateDuration = 16 /*0x10*/;
		}
		else
			Position = Position + new Vector2i(0, CoconutsInstance.ClimbData[_climbDataIndex].VelocityY);
	}

	private void UpdatePreThrowing()
	{
		--_stateDuration;
		if (_stateDuration >= 0)
			return;
		FireProjectile();
		_state = CoconutsInstance.StateType.PostThrowing;
		_stateDuration = 8;
	}

	private void UpdatePostThrowing()
	{
		--_stateDuration;
		if (_stateDuration >= 0)
			return;
		_state = CoconutsInstance.StateType.Climbing;
		_stateDuration = 8;
		_animation.Index = 1;
		UpdateClimbIndex();
	}

	private void UpdateClimbIndex()
	{
		++_climbDataIndex;
		if (_climbDataIndex >= CoconutsInstance.ClimbData.Count)
			_climbDataIndex = 0;
		_stateDuration = CoconutsInstance.ClimbData[_climbDataIndex].Duration;
	}

	private void FireProjectile()
	{
		CoconutsInstance.Projectile projectile = Level.ObjectManager.AddSubObject<CoconutsInstance.Projectile>(this);
		Vector2i position = Position;
		int x = position.X + (_facingRight ? -44 : 44);
		position = Position;
		int y = position.Y - 52;
		projectile.Position = new Vector2i(x, y);
		projectile.Direction = _facingRight ? 1 : -1;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, _facingRight);
	}

	private struct ClimbDataItem(int velocityY, int duration)
	{
		private readonly int _velocityY = velocityY;
		private readonly int _duration = duration;

		public int VelocityY => _velocityY;

		public int Duration => _duration;

		public override string ToString()
		{
			return $"Velocity Y = {_velocityY} Duration = {_duration}";
		}
	}

	private enum StateType
	{
		Resting,
		Climbing,
		PreThrowing,
		PostThrowing,
	}

	private class Projectile : Enemy
	{
		private const int AnimationProjectile = 4;
		private const double InitialSpeed = 4.0;
		private const double Gravity = 0.5;
		private AnimationInstance _animation;
		private Vector2 _velocity;

		public int Direction
		{
			get => Math.Sign(_velocity.X);
			set => _velocity.X = 4.0 * Math.Sign(value);
		}

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 4);
			_velocity = new Vector2(4.0, -4.0);
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			PositionPrecise = PositionPrecise + _velocity;
			_velocity.Y += 0.5;
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(_animation, _velocity.X >= 0.0);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
