// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPRING.SpringInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.SPRING;

public class SpringInstance : ActiveObject
{
	private SpringDirection _direction = SpringDirection.Right;
	private int _strength = 40;
	private AnimationInstance _animation;
	private int _bounceAnimationIndex;
	private Vector2 _bounceVelocity;

	[StateVariable]
	private SpringDirection Direction
	{
		get => _direction;
		set => _direction = value;
	}

	[StateVariable]
	private int Strength
	{
		get => _strength;
		set => _strength = value;
	}

	public SpringInstance() => DesignBounds = new Rectanglei(-101, -104, 202, 208 /*0xD0*/);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = _strength >= 52 ? 0 : 2;
		if ((int)_direction % 2 == 1)
			_animation.Index += 4;
		_bounceAnimationIndex = _animation.Index + 1;
		switch (_direction)
		{
			case SpringDirection.Up:
				CollisionVectors = new CollisionVector[6]
				{
		  new CollisionVector( this, new Vector2i(-64, 64 /*0x40*/), new Vector2i(-64, 8)),
		  new CollisionVector( this, new Vector2i(-64, 8), new Vector2i(-56, 0)),
		  new CollisionVector( this, new Vector2i(-56, 0), new Vector2i(56, 0)),
		  new CollisionVector( this, new Vector2i(56, 0), new Vector2i(64 /*0x40*/, 8)),
		  new CollisionVector( this, new Vector2i(64 /*0x40*/, 8), new Vector2i(64 /*0x40*/, 64 /*0x40*/)),
		  new CollisionVector( this, new Vector2i(-64, 64 /*0x40*/), new Vector2i(64 /*0x40*/, 64 /*0x40*/))
				};
				CollisionVectors[1].Id = 1;
				CollisionVectors[2].Id = 1;
				CollisionVectors[3].Id = 1;
				_bounceVelocity = new Vector2(0.0, -_strength);
				break;
			case SpringDirection.UpRight:
				CollisionVectors = new CollisionVector[6]
				{
		  new CollisionVector( this, new Vector2i(-48, -48), new Vector2i(48 /*0x30*/, 48 /*0x30*/)),
		  new CollisionVector( this, new Vector2i(-48, -84), new Vector2i(-48, -48)),
		  new CollisionVector( this, new Vector2i(-84, -84), new Vector2i(-48, -84)),
		  new CollisionVector( this, new Vector2i(-84, 80 /*0x50*/), new Vector2i(-84, -48)),
		  new CollisionVector( this, new Vector2i(48 /*0x30*/, 80 /*0x50*/), new Vector2i(-84, 80 /*0x50*/)),
		  new CollisionVector( this, new Vector2i(48 /*0x30*/, 48 /*0x30*/), new Vector2i(48 /*0x30*/, 80 /*0x50*/))
				};
				CollisionVectors[0].Id = 1;
				CollisionVectors[1].Id = 1;
				CollisionVectors[2].Id = 1;
				CollisionVectors[5].Id = 1;
				_bounceVelocity = new Vector2(_strength, -_strength);
				break;
			case SpringDirection.Right:
				CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(-64.0, -60.0, 64.0, 128.0));
				CollisionVectors[2].Id = 1;
				_bounceVelocity = new Vector2(_strength, 0.0);
				break;
			case SpringDirection.Down:
				CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(-64.0, -69.0, 128.0, 64.0));
				CollisionVectors[3].Id = 1;
				_bounceVelocity = new Vector2(0.0, _strength);
				break;
			case SpringDirection.Left:
				CollisionVectors = CollisionVector.FromRectangle(this, (Rectanglei)new Rectangle(5.0, -64.0, 64.0, 128.0));
				CollisionVectors[0].Id = 1;
				_bounceVelocity = new Vector2(-_strength, 0.0);
				break;
			case SpringDirection.UpLeft:
				CollisionVectors = new CollisionVector[5]
				{
		  new CollisionVector( this, new Vector2i(-48, 48 /*0x30*/), new Vector2i(48 /*0x30*/, -48)),
		  new CollisionVector( this, new Vector2i(48 /*0x30*/, -48), new Vector2i(84, -48)),
		  new CollisionVector( this, new Vector2i(84, -48), new Vector2i(84, 80 /*0x50*/)),
		  new CollisionVector( this, new Vector2i(84, 80 /*0x50*/), new Vector2i(-48, 80 /*0x50*/)),
		  new CollisionVector( this, new Vector2i(-48, 80 /*0x50*/), new Vector2i(-48, 48 /*0x30*/))
				};
				CollisionVectors[0].Id = 1;
				CollisionVectors[1].Id = 1;
				_bounceVelocity = new Vector2(-_strength, -_strength);
				break;
		}
		Priority = -256;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != 1 || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		Character activeObject = (Character)e.ActiveObject;
		switch (Direction)
		{
			case SpringDirection.Up:
			case SpringDirection.UpRight:
			case SpringDirection.UpLeft:
				activeObject.LeaveGround();
				break;
		}
		if (_direction == SpringDirection.Up || _direction == SpringDirection.UpRight || _direction == SpringDirection.UpLeft)
		{
			activeObject.IsAirborne = true;
			activeObject.IsSpinball = false;
			if (_direction == SpringDirection.Up)
			{
				activeObject.TumbleAngle = 0.0;
				activeObject.TumbleTurns = 1;
				activeObject.GroundVelocity = 1.0;
			}
			else
				activeObject.Animation.Index = 12;
		}
		if (activeObject.IsAirborne)
		{
			Character character = activeObject;
			Vector2 velocity;
			double x;
			if (_bounceVelocity.X == 0.0)
			{
				velocity = activeObject.Velocity;
				x = velocity.X;
			}
			else
				x = _bounceVelocity.X;
			double y;
			if (_bounceVelocity.Y == 0.0)
			{
				velocity = activeObject.Velocity;
				y = velocity.Y;
			}
			else
				y = _bounceVelocity.Y;
			Vector2 vector2 = new Vector2(x, y);
			character.Velocity = vector2;
			activeObject.IsHurt = false;
		}
		else
		{
			activeObject.GroundVelocity = _bounceVelocity.X;
			activeObject.Facing = Math.Sign(_bounceVelocity.X);
			activeObject.SlopeLockTicks = Math.Max(16 /*0x10*/, activeObject.SlopeLockTicks);
			activeObject.IsPushing = false;
		}
		_animation.Index = _bounceAnimationIndex;
		e.MaintainVelocity = true;
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath("SONICORCA/SOUND/SPRING"));
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			switch (_direction)
			{
				case SpringDirection.Up:
					objectRenderer.Render(_animation);
					break;
				case SpringDirection.UpRight:
					objectRenderer.Render(_animation);
					break;
				case SpringDirection.Right:
					objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Math.PI / 2.0);
					objectRenderer.Render(_animation, true);
					break;
				case SpringDirection.DownRight:
					objectRenderer.Render(_animation);
					break;
				case SpringDirection.Down:
					objectRenderer.Render(_animation, flipY: true);
					break;
				case SpringDirection.DownLeft:
					objectRenderer.Render(_animation, flipY: true);
					break;
				case SpringDirection.Left:
					objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(Math.PI / 2.0);
					objectRenderer.Render(_animation, true, true);
					break;
				case SpringDirection.UpLeft:
					objectRenderer.Render(_animation, true);
					break;
			}
		}
	}
}
