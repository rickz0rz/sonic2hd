// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.BUZZER.BuzzerInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.BUZZER;

/// <summary>
/// Buzzer, the wasp badnik. It flies from side to side and turns at the end of each flight.
/// When a character is below it at the correct distance, it stops and fires a projectile down at an angle.
/// </summary>
public class BuzzerInstance : Badnik
{
	private const int AnimationMoving = 0;
	private const int AnimationTurning = 1;
	private const int AnimationFiring = 2;
	private const int FlightDuration = 256;
	private const int TurnWaitDuration = 30;

	/// <summary>The value of the turn wait timer at which the Buzzer turns round.</summary>
	private const int TurnTime = 15;

	private const int FireDuration = 50;

	/// <summary>The value of the fire timer at which the Buzzer releases the projectile.</summary>
	private const int FireTime = 20;

	private AnimationInstance _animation;
	private int _velocityX;
	private StatusType _status;
	private int _flightDuration;
	private int _turnWaitDuration;
	private int _fireDuration;
	private bool _firedThisRound;

	public BuzzerInstance() => DesignBounds = new Rectanglei(-130, -84, 260, 168);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = AnimationMoving;
		_velocityX = -4;
		_status = StatusType.Moving;
		_flightDuration = FlightDuration;
		_turnWaitDuration = 0;
		_fireDuration = 0;
		_firedThisRound = false;
		CollisionRectangles = new CollisionRectangle[1]
		{
			new CollisionRectangle(this, 0, -64, -32, 128, 64)
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_status)
		{
			case StatusType.Moving:
				UpdateMoving();
				break;
			case StatusType.Firing:
				UpdateFiring();
				break;
		}
	}

	private void UpdateMoving()
	{
		if (!_firedThisRound && CheckForCharacter())
		{
			_animation.Index = AnimationFiring;
			_status = StatusType.Firing;
			_fireDuration = FireDuration;
			_firedThisRound = true;
		}
		else if (_turnWaitDuration > 0)
		{
			--_turnWaitDuration;
			if (_turnWaitDuration == TurnTime)
			{
				_velocityX *= -1;
				_animation.Index = AnimationTurning;
				_firedThisRound = false;
			}
		}
		else
		{
			--_flightDuration;
			if (_flightDuration > 0)
			{
				Position = Position + new Vector2i(_velocityX, 0);
			}
			else
			{
				_flightDuration = FlightDuration;
				_turnWaitDuration = TurnWaitDuration;
			}
		}
	}

	private void UpdateFiring()
	{
		--_fireDuration;
		if (_fireDuration < 0)
		{
			_status = StatusType.Moving;
		}
		else if (_fireDuration == FireTime)
		{
			FireProjectile();
		}
	}

	private void FireProjectile()
	{
		Projectile projectile = Level.ObjectManager.AddSubObject<Projectile>(this);
		int x = Position.X + Math.Sign(_velocityX) * -52;
		int y = Position.Y + 96;
		projectile.Position = new Vector2i(x, y);
		projectile.Direction = Math.Sign(_velocityX);
	}

	/// <summary>
	/// Returns true when a character is below the Buzzer, not more than 1024 pixels down,
	/// and 160 to 192 pixels away horizontally.
	/// </summary>
	private bool CheckForCharacter()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			// The loop stops at the first character that is not in the vertical range.
			// Thus a character after it in the list is not checked.
			if (Position.Y >= character.Position.Y || Position.Y < character.Position.Y - 1024)
			{
				return false;
			}
			if (MathX.IsBetween(160.0, Math.Abs(Position.X - character.Position.X), 192.0))
			{
				return true;
			}
		}
		return false;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, _velocityX >= 0);
	}

	private enum StatusType
	{
		Moving,
		Firing,
	}

	/// <summary>The projectile that the Buzzer fires. It moves down at an angle and hurts characters.</summary>
	private class Projectile : Enemy
	{
		private const int AnimationProjectile = 3;
		private const int Speed = 6;
		private AnimationInstance _animation;
		private Vector2i _velocity;

		public int Direction
		{
			get => Math.Sign(_velocity.X);
			set => _velocity.X = Speed * Math.Sign(value);
		}

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), AnimationProjectile);
			_velocity = new Vector2i(Speed);
			CollisionRectangles = new CollisionRectangle[1]
			{
				new CollisionRectangle(this, 0, -16, -16, 32, 32)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Position = Position + _velocity;
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation, _velocity.X >= 0);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
