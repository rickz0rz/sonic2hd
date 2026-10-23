// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPINY.SpinyInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace SONICORCA.OBJECTS.SPINY;

public class SpinyInstance : Badnik
{
	private const int AnimationMovingLeft = 0;
	private const int AnimationMovingRight = 1;
	private const int AnimationShooting = 2;
	private const int AnimationPlasma = 3;
	private bool _wall;
	private AnimationInstance _animation;
	private Vector2 _velocity;
	private SpinyInstance.StatusType _status;
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
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_velocity = _wall ? new Vector2(0.0, -1.0) : new Vector2(-1.0, 0.0);
		_status = SpinyInstance.StatusType.Moving;
		_moveTimeLeft = 128 /*0x80*/;
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_status)
		{
			case SpinyInstance.StatusType.Moving:
				UpdateMove();
				break;
			case SpinyInstance.StatusType.Firing:
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
			_holdFireTimeLeft = 40;
			_status = SpinyInstance.StatusType.Firing;
			_animation.Index = 2;
			return;
		}
		--_moveTimeLeft;
		if (_moveTimeLeft <= 0)
		{
			_moveTimeLeft = 128 /*0x80*/;
			_velocity.X *= -1.0;
			_velocity.Y *= -1.0;
		}
		PositionPrecise = PositionPrecise + _velocity;
		_animation.Index = _wall ? (_velocity.Y >= 0.0 ? 0 : 1) : (_velocity.X < 0.0 ? 0 : 1);
	}

	private void UpdateFiring()
	{
		--_holdFireTimeLeft;
		if (_holdFireTimeLeft < 0)
		{
			_status = SpinyInstance.StatusType.Moving;
			_holdFireTimeLeft = 64 /*0x40*/;
		}
		else
		{
			if (_holdFireTimeLeft != 20)
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
				double num = character.Position.Y - Position.Y;
				if (num >= -540.0 && num <= 280.0 && Math.Abs(character.Position.X - Position.X) < 384.0)
					return true;
			}
		}
		return false;
	}

	private void FireProjectile()
	{
		if (_wall)
		{
			Level.ObjectManager.AddSubObject<SpinyInstance.PlasmaSphere>(this).Velocity = new Vector2(-12.0, 0.0);
		}
		else
		{
			int num1 = -1;
			double num2 = double.MaxValue;
			foreach (IActiveObject character in Level.ObjectManager.Characters)
			{
				Vector2i position = character.Position;
				int x1 = position.X;
				position = Position;
				int x2 = position.X;
				double num3 = x1 - x2;
				double num4 = Math.Abs(num3);
				if (num4 < num2)
				{
					num2 = num4;
					num1 = Math.Sign(num3);
				}
			}
			if (num1 == 0)
				num1 = -1;
			Level.ObjectManager.AddSubObject<SpinyInstance.PlasmaSphere>(this).Velocity = new Vector2(4 * Math.Sign(num1), -12.0);
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

	private class PlasmaSphere : Enemy
	{
		private AnimationInstance _animation;
		private int _ticks;

		public int Direction { get; set; }

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 3);
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -8, -8, 16 /*0x10*/, 16 /*0x10*/)
			};
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			Velocity += new Vector2(0.0, 0.5);
			PositionPrecise = PositionPrecise + Velocity;
			++_ticks;
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (_ticks < 3)
				return;
			renderer.GetObjectRenderer().Render(_animation);
		}
	}
}
