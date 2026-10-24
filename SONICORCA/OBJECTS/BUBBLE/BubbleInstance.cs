using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.BUBBLE;

/// <summary>
/// An air bubble that rises through water and moves from side to side.
/// A character that touches a large bubble breathes in air, when the bubble has grown to its full size.
/// The bubble disappears when it leaves the water.
/// </summary>
public class BubbleInstance : ActiveObject
{
	/// <summary>The maximum radius for each <see cref="SizeType"/>.</summary>
	private static IReadOnlyList<int> SizeRadius = new int[3]
	{
		16,
		38,
		112
	};

	/// <summary>The rise speed: a y velocity of -$88 in the original.</summary>
	private const double RiseVelocity = -0x88 * 4 / 256.0;

	/// <summary>
	/// The number of updates until a large bubble can be breathed. The original shows the full-size bubble (mapping
	/// frame 6) after 4 frames of 15 ticks.
	/// </summary>
	private const int BreathableTicks = 60;

	/// <summary>The time in which the character cannot move after it breathes in a bubble (move_lock $23).</summary>
	private const int CatchMoveLockTicks = 0x23;

	private AnimationInstance _animation;
	private Vector2 _velocity;

	/// <summary>The size type of the bubble.</summary>
	private SizeType _size;

	/// <summary>The current radius in pixels.</summary>
	private int _radius;

	private double _angle;
	private int _maxSize;
	private int _ticks;

	[StateVariable]
	private SizeType Size
	{
		get => _size;
		set => _size = value;
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		Priority = 1024;
		_velocity = new Vector2(0.0, RiseVelocity);
		_angle = Level.Random.NextDouble() * (2.0 * Math.PI);
		_maxSize = SizeRadius[(int)_size];
		_radius = (int)_size;
		if (_size != SizeType.Large)
		{
			_maxSize += Level.Random.Next(-4, 4);
		}
		else
		{
			CollisionRectangles = new CollisionRectangle[1]
			{
				new CollisionRectangle(this, 0, -28, -28, 56, 56)
			};
		}
	}

	protected override void OnUpdate()
	{
		_angle = MathX.WrapRadians(_angle + Math.PI / 64.0);
		_velocity.X = Math.Sin(_angle) * 0.5;
		PositionPrecise = PositionPrecise + _velocity;
		++_ticks;
		// The bubble grows by one pixel each frame until it reaches its maximum radius.
		_radius = Math.Min(_radius + 1, _maxSize);
		if (!Level.WaterManager.IsUnderwater(Position))
		{
			FinishForever();
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_size != SizeType.Large || _ticks <= BreathableTicks || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		if (character.IsObjectControlled)
		{
			return;
		}
		// As loc_1FB0C: stop the character, lock its movement and end a jump. InhaleOxygen plays the bubble
		// animation and ends a roll.
		character.Velocity = new Vector2();
		character.GroundVelocity = 0.0;
		character.SlopeLockTicks = CatchMoveLockTicks;
		character.IsJumping = false;
		character.IsRollJumping = false;
		character.IsPushing = false;
		character.InhaleOxygen();
		FinishForever();
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		double scale = (double)_radius / _animation.CurrentTexture.Width;
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			objectRenderer.ModelMatrix *= Matrix4.CreateScale(scale, scale);
			objectRenderer.Render(_animation);
		}
	}

	private enum SizeType
	{
		Small,
		Medium,
		Large,
	}
}
