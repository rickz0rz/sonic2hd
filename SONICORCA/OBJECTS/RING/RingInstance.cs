// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.RING.RingInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Lighting;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.RING;

public class RingInstance : ActiveObject
{
	private static readonly IReadOnlyList<Vector2i> SparkleParticleOffsets = new Vector2i[4]
	{
	new Vector2i(16 /*0x10*/, 4),
	new Vector2i(-16, 12),
	new Vector2i(-12, -16),
	new Vector2i(20, -24)
	};
	private bool _scatter;
	private bool _stationary;
	private bool _attracted;
	private AnimationInstance _animation;
	private Vector2 _velocity;
	private int _lifeTicks;
	private ITexture _glowTexture;
	private PointLightSource _lightSource;

	[StateVariable]
	private bool Scatter
	{
		get => _scatter;
		set => _scatter = value;
	}

	[StateVariable]
	private Vector2 Velocity
	{
		get => _velocity;
		set => _velocity = value;
	}

	public RingInstance() => DesignBounds = new Rectanglei(-32, -32, 64 /*0x40*/, 64 /*0x40*/);

	protected override void OnStart()
	{
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
		};
		if (Scatter)
		{
			_stationary = false;
			_lifeTicks = byte.MaxValue;
		}
		else
		{
			_stationary = true;
			_lifeTicks = byte.MaxValue;
			_velocity = new Vector2();
		}
		Priority = 4096 /*0x1000*/;
		_glowTexture = ResourceTree.GetLoadedResource<ITexture>(Type.GetAbsolutePath("SONICORCA/PARTICLE/GLOW"));
		ShadowInfo = null;
		_lightSource = new PointLightSource(8, Position);
		Level.LightingManager.RegisterLightSource(_lightSource);
	}

	protected override void OnUpdate()
	{
		_lightSource.Position = Position;
		ICharacter character1 = null;
		double num1 = double.MaxValue;
		foreach (ICharacter character2 in Level.ObjectManager.Characters)
		{
			if (character2.Barrier == BarrierType.Lightning)
			{
				double length = (character2.Position - Position).Length;
				if (length < num1 && (_attracted || length < 256.0))
				{
					character1 = character2;
					num1 = length;
				}
			}
		}
		if (character1 != null)
		{
			_stationary = false;
			Vector2i position = character1.Position;
			int x1 = position.X;
			position = Position;
			int x2 = position.X;
			int num2 = x1 - x2;
			if (Math.Sign(num2) != Math.Sign(_velocity.X) && _velocity.X != 0.0)
				_velocity.X += 1.5 * Math.Sign(num2);
			else
				_velocity.X += 0.75 * Math.Sign(num2);
			position = character1.Position;
			int y1 = position.Y;
			position = Position;
			int y2 = position.Y;
			int num3 = y1 - y2;
			if (Math.Sign(num3) != Math.Sign(_velocity.Y) && _velocity.Y != 0.0)
				_velocity.Y += 1.5 * Math.Sign(num3);
			else
				_velocity.Y += 0.75 * Math.Sign(num3);
			PositionPrecise = PositionPrecise + _velocity;
			_attracted = true;
		}
		else
		{
			if (_stationary)
				return;
			if (_animation == null)
			{
				_animation = new AnimationInstance((Type as RingType).AnimationInstance.AnimationGroup);
				_animation.OverrideDelay = new int?(0);
			}
			if (_lifeTicks == 0)
			{
				FinishForever();
			}
			else
			{
				_animation.OverrideDelay = new int?((byte.MaxValue - _lifeTicks) / 32 /*0x20*/);
				--_lifeTicks;
				UpdateMovement();
			}
		}
	}

	private void UpdateMovement()
	{
		PositionPrecise = PositionPrecise + _velocity;
		bool flag1 = Math.Abs(_velocity.Y) < 4.0;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32 /*0x20*/))
		{
			if (!(collision.Vector.Mode != CollisionMode.Bottom & flag1))
			{
				if (collision.Vector.IsWall)
				{
					PositionPrecise = new Vector2(PositionPrecise.X + collision.Shift, PositionPrecise.Y);
					flag3 = true;
				}
				else
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					flag2 = collision.Vector.Mode == CollisionMode.Top;
					flag4 = collision.Vector.Mode == CollisionMode.Bottom;
				}
			}
		}
		if (flag3)
			_velocity.X *= -0.25;
		if (flag2)
			_velocity.Y *= -0.75;
		if (flag4)
			_velocity.Y = 0.0;
		if (flag2 || flag4)
			return;
		_velocity.Y += 0.375;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (!_stationary && !_attracted && _lifeTicks >= 175)
			return;
		Collect(activeObject.Player);
	}

	private void Collect(Player player)
	{
		player.GainRings();
		CreateSparkles();
		++Level.RingsCollected;
		if (Level.RingsCollected == Level.RingsPerfectTarget)
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/PERFECT"));
		else
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/RING"));
		FinishForever();
	}

	private void CreateSparkles()
	{
		int ticks = 0;
		foreach (Vector2i sparkleParticleOffset in (IEnumerable<Vector2i>)RingInstance.SparkleParticleOffsets)
		{
			ObjectPlacement objectPlacement = new ObjectPlacement(Type.GetAbsolutePath("/SPARKLE"), Level.Map.Layers.IndexOf(Layer), Position + sparkleParticleOffset);
			Level.SetInterval(ticks, () => Level.ObjectManager.AddObject(objectPlacement));
			ticks += 2;
		}
	}

	protected override void OnAnimate()
	{
		if (_stationary || _animation == null)
			return;
		_animation.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.FilterAmount *= 0.25;
		double a1 = MathX.Clamp(_lifeTicks * (16.0 / byte.MaxValue), 1.0);
		double a2 = 0.25 * a1 * viewOptions.FilterAmount;
		if (a2 > 0.0)
		{
			objectRenderer.BlendMode = BlendMode.Additive;
			objectRenderer.Texture = _glowTexture;
			objectRenderer.MultiplyColour = new Colour(a2, 1.0, 1.0, 0.0);
			objectRenderer.Render();
		}
		AnimationInstance animationInstance;
		if (_stationary || _animation == null)
		{
			animationInstance = (Type as RingType).AnimationInstance;
			objectRenderer.MultiplyColour = Colours.White;
		}
		else
		{
			animationInstance = _animation;
			objectRenderer.MultiplyColour = new Colour(a1, 1.0, 1.0, 1.0);
		}
		objectRenderer.BlendMode = BlendMode.Alpha;
		objectRenderer.Render(animationInstance);
	}

	protected override void OnStop()
	{
		if (!_stationary)
			FinishForever();
		Level.LightingManager.UnregisterLightSource(_lightSource);
	}
}
