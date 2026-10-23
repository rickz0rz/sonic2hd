using System;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Objects.Base;

public abstract class Animal : ActiveObject
{
	private readonly string _animationGroupResourceKey;

	private AnimationInstance _animationInstance;

	private Vector2 _velocity;

	private bool _jumping;

	private int _delay;

	private int _direction = -1;

	[StateVariable]
	public int Delay
	{
		get
		{
			return _delay;
		}
		set
		{
			_delay = value;
		}
	}

	[StateVariable]
	public int Direction
	{
		get
		{
			return _direction;
		}
		set
		{
			_direction = value;
		}
	}

	protected Vector2 JumpVelocity { get; set; }

	protected double JumpGravity { get; set; }

	protected AnimationInstance AnimationInstance => _animationInstance;

	protected Vector2 Velocity => _velocity;

	public Animal(string animationGroupResourceKey)
	{
		_animationGroupResourceKey = animationGroupResourceKey;
		JumpGravity = 0.375;
	}

	protected override void OnStart()
	{
		_animationInstance = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(_animationGroupResourceKey));
		JumpVelocity = new Vector2(Math.Abs(JumpVelocity.X) * Math.Sign(_direction), JumpVelocity.Y);
		Priority = ((_delay > 0) ? (-256) : 1280);
	}

	protected override void OnUpdate()
	{
		if (_delay > 0)
		{
			Priority = -256;
			_delay--;
			if (_delay == 0)
			{
				_velocity = new Vector2(0.0, -16.0);
			}
			return;
		}
		Priority = 1280;
		PositionPrecise += _velocity;
		bool flag = false;
		foreach (CollisionInfo collision in CollisionVector.GetCollisions(this, 32))
		{
			if (!collision.Vector.IsWall && collision.Vector.Mode != CollisionMode.Bottom)
			{
				ActiveObject owner = collision.Vector.Owner;
				if (owner == null || owner.Type.Classification != ObjectClassification.Capsule)
				{
					PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + collision.Shift);
					flag = true;
				}
			}
		}
		if (flag)
		{
			_jumping = true;
			_velocity = JumpVelocity;
			_animationInstance.Index = 1;
		}
		else
		{
			_velocity.Y += (_jumping ? JumpGravity : 0.875);
		}
	}

	protected override void OnAnimate()
	{
		if (_delay <= 0)
		{
			_animationInstance.Animate();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animationInstance, _velocity.X > 0.0);
	}

	protected override void OnStop()
	{
		FinishForever();
	}
}
