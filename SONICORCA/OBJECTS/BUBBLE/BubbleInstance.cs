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
/// A character that touches a large bubble breathes in air.
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

	private AnimationInstance _animation;
	private Vector2 _velocity;

	/// <summary>
	/// The size type at the start. After the first update, this field holds the current radius in pixels.
	/// </summary>
	private SizeType _size;

	private double _angle;
	private int _maxSize;

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
		_velocity = new Vector2(0.0, -17.0 / 32.0);
		_angle = Level.Random.NextDouble() * (2.0 * Math.PI);
		_maxSize = SizeRadius[(int)_size];
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
		// The bubble grows by one pixel each frame until it reaches its maximum radius.
		_size = (SizeType)Math.Min((int)(_size + 1), _maxSize);
		if (!Level.WaterManager.IsUnderwater(Position))
		{
			FinishForever();
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_size != SizeType.Large || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Velocity = new Vector2();
		character.InhaleOxygen();
		FinishForever();
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		double scale = (double)_size / _animation.CurrentTexture.Width;
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
