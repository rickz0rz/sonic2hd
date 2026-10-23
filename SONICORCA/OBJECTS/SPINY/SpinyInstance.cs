using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SPINY;

/// <summary>
/// Spiny, a badnik that crawls back and forth on a floor or a wall. When a character comes near, it stops and fires a
/// plasma sphere. On a floor, it fires up toward the nearest character. On a wall, it fires to the left.
/// </summary>
public class SpinyInstance : Badnik
{
	private const int AnimationMovingLeft = 0;
	private const int AnimationMovingRight = 1;
	private const int AnimationShooting = 2;
	private const int AnimationPlasma = 3;
	private const int MoveTicks = 128;
	private const int FiringTicks = 40;

	/// <summary>The tick of the firing state on which the plasma sphere is fired.</summary>
	private const int FireTick = 20;

	/// <summary>The number of ticks after firing before the Spiny can fire again.</summary>
	private const int FireCooldownTicks = 64;

	private bool _wall;
	private AnimationInstance _animation;
	private Vector2 _velocity;
	private StatusType _status;
	private int _moveTimeLeft;
	private int _holdFireTimeLeft;

	[StateVariable]
	private bool Wall
	{
		get => _wall;
		set => _wall = value;
	}

	public SpinyInstance() => DesignBounds = new Rectanglei(-110, -63, 220, 126);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpinyType.AnimationGroupResourceKey));
		_velocity = _wall ? new Vector2(0.0, -1.0) : new Vector2(-1.0, 0.0);
		_status = StatusType.Moving;
		_moveTimeLeft = MoveTicks;
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -16, -16, 32, 32)
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_status)
		{
			case StatusType.Moving:
				UpdateMove();
				break;
			case StatusType.Firing:
				UpdateFiring();
				break;
		}
	}

	private void UpdateMove()
	{
		if (_holdFireTimeLeft > 0)
			--_holdFireTimeLeft;
		else if (DetectPlayer())
		{
			_holdFireTimeLeft = FiringTicks;
			_status = StatusType.Firing;
			_animation.Index = AnimationShooting;
			return;
		}
		--_moveTimeLeft;
		if (_moveTimeLeft <= 0)
		{
			_moveTimeLeft = MoveTicks;
			_velocity.X *= -1.0;
			_velocity.Y *= -1.0;
		}
		PositionPrecise = PositionPrecise + _velocity;
		_animation.Index = _wall ? (_velocity.Y >= 0.0 ? AnimationMovingLeft : AnimationMovingRight) : (_velocity.X < 0.0 ? AnimationMovingLeft : AnimationMovingRight);
	}

	private void UpdateFiring()
	{
		--_holdFireTimeLeft;
		if (_holdFireTimeLeft < 0)
		{
			_status = StatusType.Moving;
			_holdFireTimeLeft = FireCooldownTicks;
		}
		else
		{
			if (_holdFireTimeLeft != FireTick)
				return;
			FireProjectile();
		}
	}

	private bool DetectPlayer()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ShouldReactToLevel)
			{
				double distanceY = character.Position.Y - Position.Y;
				if (distanceY >= -540.0 && distanceY <= 280.0 && Math.Abs(character.Position.X - Position.X) < 384.0)
					return true;
			}
		}
		return false;
	}

	private void FireProjectile()
	{
		if (_wall)
		{
			Level.ObjectManager.AddSubObject<PlasmaSphere>(this).Velocity = new Vector2(-12.0, 0.0);
		}
		else
		{
			// Fire toward the nearest character on the X axis. Fire to the left when there is no character.
			int direction = -1;
			double nearestDistance = double.MaxValue;
			foreach (IActiveObject character in Level.ObjectManager.Characters)
			{
				double deltaX = character.Position.X - Position.X;
				double distance = Math.Abs(deltaX);
				if (distance < nearestDistance)
				{
					nearestDistance = distance;
					direction = Math.Sign(deltaX);
				}
			}
			if (direction == 0)
				direction = -1;
			Level.ObjectManager.AddSubObject<PlasmaSphere>(this).Velocity = new Vector2(4 * Math.Sign(direction), -12.0);
		}
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (_wall)
		{
			using (objectRenderer.BeginMatixState())
			{
				objectRenderer.ModelMatrix = objectRenderer.ModelMatrix.RotateZ(MathX.ToRadians(-90.0));
				objectRenderer.Render(_animation);
			}
		}
		else
			objectRenderer.Render(_animation);
	}

	private enum StatusType
	{
		Moving,
		Firing,
	}

	/// <summary>A plasma sphere that a Spiny fires. Gravity pulls it down.</summary>
	private class PlasmaSphere : Enemy
	{
		private const double Gravity = 0.5;

		/// <summary>The sphere is not drawn in its first ticks, so that it comes out from inside the Spiny.</summary>
		private const int HiddenTicks = 3;

		private AnimationInstance _animation;
		private int _ticks;

		public int Direction { get; set; }

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpinyType.AnimationGroupResourceKey), AnimationPlasma);
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -8, -8, 16, 16)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Velocity += new Vector2(0.0, Gravity);
			PositionPrecise = PositionPrecise + Velocity;
			++_ticks;
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (_ticks < HiddenTicks)
				return;
			renderer.GetObjectRenderer().Render(_animation);
		}
	}
}
