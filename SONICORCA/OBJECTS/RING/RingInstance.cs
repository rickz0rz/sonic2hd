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

namespace SONICORCA.OBJECTS.RING;

/// <summary>
/// A ring that a character collects. A placed ring stays in position. A scattered ring (from a hurt character)
/// bounces on the terrain and goes away after a time. A character with a lightning barrier pulls rings near it.
/// </summary>
public class RingInstance : ActiveObject
{
	private static readonly IReadOnlyList<Vector2i> SparkleParticleOffsets = new Vector2i[]
	{
		new Vector2i(16, 4),
		new Vector2i(-16, 12),
		new Vector2i(-12, -16),
		new Vector2i(20, -24)
	};

	/// <summary>The number of ticks that a scattered ring stays.</summary>
	private const int MaxLifeTicks = byte.MaxValue;

	private const double AttractDistance = 256.0;
	private const double Gravity = 0.375;

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

	public RingInstance() => DesignBounds = new Rectanglei(-32, -32, 64, 64);

	protected override void OnStart()
	{
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -16, -16, 32, 32)
		};
		if (Scatter)
		{
			_stationary = false;
		}
		else
		{
			_stationary = true;
			_velocity = new Vector2();
		}
		_lifeTicks = MaxLifeTicks;
		Priority = 4096;
		_glowTexture = ResourceTree.GetLoadedResource<ITexture>(Type.GetAbsolutePath(RingType.GlowParticleResourceKey));
		ShadowInfo = null;
		_lightSource = new PointLightSource(8, Position);
		Level.LightingManager.RegisterLightSource(_lightSource);
	}

	protected override void OnUpdate()
	{
		_lightSource.Position = Position;

		// Find the nearest character with a lightning barrier. After a ring is attracted, it follows at any distance.
		ICharacter attractor = null;
		double attractorDistance = double.MaxValue;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.Barrier == BarrierType.Lightning)
			{
				double distance = (character.Position - Position).Length;
				if (distance < attractorDistance && (_attracted || distance < AttractDistance))
				{
					attractor = character;
					attractorDistance = distance;
				}
			}
		}
		if (attractor != null)
		{
			_stationary = false;

			// Accelerate toward the character. Turn faster when the ring moves away from it.
			int deltaX = attractor.Position.X - Position.X;
			if (Math.Sign(deltaX) != Math.Sign(_velocity.X) && _velocity.X != 0.0)
				_velocity.X += 1.5 * Math.Sign(deltaX);
			else
				_velocity.X += 0.75 * Math.Sign(deltaX);
			int deltaY = attractor.Position.Y - Position.Y;
			if (Math.Sign(deltaY) != Math.Sign(_velocity.Y) && _velocity.Y != 0.0)
				_velocity.Y += 1.5 * Math.Sign(deltaY);
			else
				_velocity.Y += 0.75 * Math.Sign(deltaY);
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
				_animation.OverrideDelay = 0;
			}
			if (_lifeTicks == 0)
			{
				FinishForever();
			}
			else
			{
				// The ring spins slower as it gets older.
				_animation.OverrideDelay = (MaxLifeTicks - _lifeTicks) / 32;
				--_lifeTicks;
				UpdateMovement();
			}
		}
	}

	/// <summary>Moves a scattered ring and bounces it on the terrain.</summary>
	private void UpdateMovement()
	{
		PositionPrecise = PositionPrecise + _velocity;
		bool isSlow = Math.Abs(_velocity.Y) < 4.0;
		bool hitCeiling = false;
		bool hitWall = false;
		bool hitFloor = false;
		foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32))
		{
			// A slow ring reacts only to the bottom of the terrain.
			if (collision.Vector.Mode != CollisionMode.Bottom && isSlow)
				continue;
			if (collision.Vector.IsWall)
			{
				PositionPrecise = new Vector2(PositionPrecise.X + collision.Shift, PositionPrecise.Y);
				hitWall = true;
			}
			else
			{
				PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
				hitCeiling = collision.Vector.Mode == CollisionMode.Top;
				hitFloor = collision.Vector.Mode == CollisionMode.Bottom;
			}
		}
		if (hitWall)
			_velocity.X *= -0.25;
		if (hitCeiling)
			_velocity.Y *= -0.75;
		if (hitFloor)
			_velocity.Y = 0.0;
		if (hitCeiling || hitFloor)
			return;
		_velocity.Y += Gravity;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		// The original blocks placed and scattered rings in the first 30 frames of the invulnerability after a hit
		// (Touch_Rings: invulnerable_time >= 90)
		if (!character.CanCollectRings)
			return;
		Collect(character.Player);
	}

	private void Collect(Player player)
	{
		player.GainRings();
		CreateSparkles();
		// Only placed rings count for the perfect bonus, as Perfect_rings_left in the original.
		if (!_scatter)
			++Level.RingsCollected;
		if (!_scatter && Level.RingsCollected == Level.RingsPerfectTarget)
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(RingType.PerfectSoundResourceKey));
		else
			Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(RingType.CollectSoundResourceKey));
		FinishForever();
	}

	/// <summary>Creates four sparkle objects around the ring, one every two ticks.</summary>
	private void CreateSparkles()
	{
		int ticks = 0;
		foreach (Vector2i sparkleParticleOffset in SparkleParticleOffsets)
		{
			ObjectPlacement objectPlacement = new ObjectPlacement(Type.GetAbsolutePath(RingType.SparkleObjectResourceKey), Level.Map.Layers.IndexOf(Layer), Position + sparkleParticleOffset);
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

		// A scattered ring fades out in its last ticks.
		double opacity = MathX.Clamp(_lifeTicks * (16.0 / byte.MaxValue), 1.0);
		double glowOpacity = 0.25 * opacity * viewOptions.FilterAmount;
		if (glowOpacity > 0.0)
		{
			objectRenderer.BlendMode = BlendMode.Additive;
			objectRenderer.Texture = _glowTexture;
			objectRenderer.MultiplyColour = new Colour(glowOpacity, 1.0, 1.0, 0.0);
			objectRenderer.Render();
		}

		// Placed rings use the shared animation of the ring type, so that all of them spin together.
		AnimationInstance animationInstance;
		if (_stationary || _animation == null)
		{
			animationInstance = (Type as RingType).AnimationInstance;
			objectRenderer.MultiplyColour = Colours.White;
		}
		else
		{
			animationInstance = _animation;
			objectRenderer.MultiplyColour = new Colour(opacity, 1.0, 1.0, 1.0);
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
