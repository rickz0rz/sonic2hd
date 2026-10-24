using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.COCONUTS;

/// <summary>
/// Coconuts, the monkey badnik. It climbs up and down a palm tree and turns to face the closest character.
/// When the character comes near, it throws a coconut.
/// </summary>
public class CoconutsInstance : Badnik
{
	/// <summary>
	/// The climb pattern. Each step moves up or down by one original pixel per frame, for a number of frames. The
	/// original writes the speed to the high byte of y_vel, so the speed is $100.
	/// </summary>
	private static readonly IReadOnlyList<ClimbDataItem> ClimbData = new ClimbDataItem[6]
	{
		new ClimbDataItem(-1, 32),
		new ClimbDataItem(1, 24),
		new ClimbDataItem(-1, 16),
		new ClimbDataItem(1, 40),
		new ClimbDataItem(-1, 32),
		new ClimbDataItem(1, 16)
	};

	private const int AnimationResting = 0;
	private const int AnimationClimbing = 1;
	private const int AnimationTurning = 2;
	private const int AnimationThrowing = 3;
	private const int RestDuration = 16;

	/// <summary>The number of units in one original pixel.</summary>
	private const int OriginalPixel = 4;
	private const int ThrowDuration = 8;
	private const int ThrowWaitDuration = 32;
	private const double ThrowRangeX = 384.0;
	private const double ThrowRangeY = 400.0;

	private AnimationInstance _animation;
	private StateType _state;
	private int _stateDuration;
	private int _throwWaitDuration;
	/// <summary>The current step of the climb pattern. The first climb uses step 0, as in the original.</summary>
	private int _climbDataIndex = -1;
	private bool _facingRight;

	public CoconutsInstance() => DesignBounds = new Rectanglei(-96, -100, 192, 200);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = AnimationResting;
		CollisionRectangles = new CollisionRectangle[1]
		{
			new CollisionRectangle(this, 0, -48, -64, 96, 128)
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_state)
		{
			case StateType.Resting:
				UpdateResting();
				break;
			case StateType.Climbing:
				UpdateClimbing();
				break;
			case StateType.PreThrowing:
				UpdatePreThrowing();
				break;
			case StateType.PostThrowing:
				UpdatePostThrowing();
				break;
		}
	}

	private void UpdateResting()
	{
		ICharacter closestCharacter = Level.ObjectManager.GetClosestCharacterTo((Vector2)Position);
		if (closestCharacter != null)
		{
			bool characterIsRight = closestCharacter.Position.X > Position.X;
			if (_facingRight != characterIsRight)
			{
				_facingRight = characterIsRight;
				_animation.Index = AnimationTurning;
			}
			double distanceX = Math.Abs(Position.X - closestCharacter.Position.X);
			double distanceY = Math.Abs(Position.Y - closestCharacter.Position.Y);
			// The original (Obj9D_Idle) checks only the horizontal distance.
			if (distanceX < ThrowRangeX && (Level.OriginalPhysics || distanceY < ThrowRangeY))
			{
				if (_throwWaitDuration == 0)
				{
					_state = StateType.PreThrowing;
					_stateDuration = ThrowDuration;
					_throwWaitDuration = ThrowWaitDuration;
					_animation.Index = AnimationThrowing;
					return;
				}
				--_throwWaitDuration;
			}
		}
		--_stateDuration;
		if (_stateDuration < 0)
		{
			_state = StateType.Climbing;
			_animation.Index = AnimationClimbing;
			UpdateClimbIndex();
		}
	}

	private void UpdateClimbing()
	{
		--_stateDuration;
		if (_stateDuration <= 0)
		{
			_state = StateType.Resting;
			_animation.Index = AnimationResting;
			_stateDuration = RestDuration;
		}
		else
		{
			Position = Position + new Vector2i(0, ClimbData[_climbDataIndex].VelocityY * OriginalPixel);
		}
	}

	private void UpdatePreThrowing()
	{
		--_stateDuration;
		if (_stateDuration < 0)
		{
			FireProjectile();
			_state = StateType.PostThrowing;
			_stateDuration = ThrowDuration;
		}
	}

	private void UpdatePostThrowing()
	{
		--_stateDuration;
		if (_stateDuration < 0)
		{
			_state = StateType.Climbing;
			_stateDuration = ThrowDuration;
			_animation.Index = AnimationClimbing;
			UpdateClimbIndex();
		}
	}

	/// <summary>Moves to the next step of the climb pattern and sets the duration of that step.</summary>
	private void UpdateClimbIndex()
	{
		++_climbDataIndex;
		if (_climbDataIndex >= ClimbData.Count)
		{
			_climbDataIndex = 0;
		}
		_stateDuration = ClimbData[_climbDataIndex].Duration;
	}

	private void FireProjectile()
	{
		Projectile projectile = Level.ObjectManager.AddSubObject<Projectile>(this);
		int x = Position.X + (_facingRight ? -44 : 44);
		int y = Position.Y - 52;
		projectile.Position = new Vector2i(x, y);
		projectile.Direction = _facingRight ? 1 : -1;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, _facingRight);
	}

	/// <summary>One step of the climb pattern.</summary>
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

	/// <summary>The coconut that Coconuts throws. It moves up in an arc and falls with gravity.</summary>
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
			set => _velocity.X = InitialSpeed * Math.Sign(value);
		}

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), AnimationProjectile);
			_velocity = new Vector2(InitialSpeed, -InitialSpeed);
			// The original box (size $B) is 16 by 16 pixels. The HD box is half that size.
			int size = Level.OriginalPhysics ? 64 : 32;
			CollisionRectangles = new CollisionRectangle[1]
			{
				new CollisionRectangle(this, 0, -size / 2, -size / 2, size, size)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			PositionPrecise = PositionPrecise + _velocity;
			_velocity.Y += Gravity;
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
