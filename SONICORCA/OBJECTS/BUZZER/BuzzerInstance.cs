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

public class BuzzerInstance : Badnik
{
	private const int AnimationMoving = 0;
	private const int AnimationTurning = 1;
	private const int AnimationFiring = 2;
	private AnimationInstance _animation;
	private int _velocityX;
	private BuzzerInstance.StatusType _status;
	private int _flightDuration;
	private int _turnWaitDuration;
	private int _fireDuration;
	private bool _firedThisRound;

	public BuzzerInstance() => DesignBounds = new Rectanglei(-130, -84, 260, 168);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = 0;
		_velocityX = -4;
		_status = BuzzerInstance.StatusType.Moving;
		_flightDuration = 256 /*0x0100*/;
		_turnWaitDuration = 0;
		_fireDuration = 0;
		_firedThisRound = false;
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -64, -32, 128 /*0x80*/, 64 /*0x40*/)
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_status)
		{
			case BuzzerInstance.StatusType.Moving:
				UpdateMoving();
				break;
			case BuzzerInstance.StatusType.Firing:
				UpdateFiring();
				break;
		}
	}

	private void UpdateMoving()
	{
		if (!_firedThisRound && CheckForCharacter())
		{
			_animation.Index = 2;
			_status = BuzzerInstance.StatusType.Firing;
			_fireDuration = 50;
			_firedThisRound = true;
		}
		else if (_turnWaitDuration > 0)
		{
			--_turnWaitDuration;
			if (_turnWaitDuration != 15)
				return;
			_velocityX *= -1;
			_animation.Index = 1;
			_firedThisRound = false;
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
				_flightDuration = 256 /*0x0100*/;
				_turnWaitDuration = 30;
			}
		}
	}

	private void UpdateFiring()
	{
		--_fireDuration;
		if (_fireDuration < 0)
		{
			_status = BuzzerInstance.StatusType.Moving;
		}
		else
		{
			if (_fireDuration != 20)
				return;
			FireProjectile();
		}
	}

	private void FireProjectile()
	{
		BuzzerInstance.Projectile projectile = Level.ObjectManager.AddSubObject<BuzzerInstance.Projectile>(this);
		Vector2i position = Position;
		int x = position.X + Math.Sign(_velocityX) * -52;
		position = Position;
		int y = position.Y + 96 /*0x60*/;
		projectile.Position = new Vector2i(x, y);
		projectile.Direction = Math.Sign(_velocityX);
	}

	private bool CheckForCharacter()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			Vector2i position = Position;
			int y1 = position.Y;
			position = character.Position;
			int y2 = position.Y;
			if (y1 < y2)
			{
				position = Position;
				int y3 = position.Y;
				position = character.Position;
				int num = position.Y - 1024 /*0x0400*/;
				if (y3 >= num)
				{
					position = Position;
					int x1 = position.X;
					position = character.Position;
					int x2 = position.X;
					if (MathX.IsBetween(160.0, Math.Abs(x1 - x2), 192.0))
						return true;
					continue;
				}
			}
			return false;
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

	private class Projectile : Enemy
	{
		private const int AnimationProjectile = 3;
		private const int Speed = 6;
		private AnimationInstance _animation;
		private Vector2i _velocity;

		public int Direction
		{
			get => Math.Sign(_velocity.X);
			set => _velocity.X = 6 * Math.Sign(value);
		}

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 3);
			_velocity = new Vector2i(6);
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
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
