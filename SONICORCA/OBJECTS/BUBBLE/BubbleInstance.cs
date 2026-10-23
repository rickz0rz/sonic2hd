// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.BUBBLE.BubbleInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

#nullable disable
namespace SONICORCA.OBJECTS.BUBBLE;

public class BubbleInstance : ActiveObject
{
	private static IReadOnlyList<int> SizeRadius = new int[3]
	{
	16 /*0x10*/,
	38,
	112 /*0x70*/
	};
	private AnimationInstance _animation;
	private Vector2 _velocity;
	private BubbleInstance.SizeType _size;
	private double _angle;
	private int _maxSize;

	[StateVariable]
	private BubbleInstance.SizeType Size
	{
		get => _size;
		set => _size = value;
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		Priority = 1024 /*0x0400*/;
		_velocity = new Vector2(0.0, -17.0 / 32.0);
		_angle = Level.Random.NextDouble() * (2.0 * Math.PI);
		_maxSize = BubbleInstance.SizeRadius[(int)_size];
		if (_size != BubbleInstance.SizeType.Large)
			_maxSize += Level.Random.Next(-4, 4);
		else
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -28, -28, 56, 56)
			};
	}

	protected override void OnUpdate()
	{
		_angle = MathX.WrapRadians(_angle + Math.PI / 64.0);
		_velocity.X = Math.Sin(_angle) * 0.5;
		PositionPrecise = PositionPrecise + _velocity;
		_size = (BubbleInstance.SizeType)Math.Min((int)(_size + 1), _maxSize);
		if (Level.WaterManager.IsUnderwater(Position))
			return;
		FinishForever();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (_size != BubbleInstance.SizeType.Large || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		activeObject.Velocity = new Vector2();
		activeObject.InhaleOxygen();
		FinishForever();
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		double num = (double)_size / _animation.CurrentTexture.Width;
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			objectRenderer.ModelMatrix *= Matrix4.CreateScale(num, num);
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
