// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.REXON.RexonInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace SONICORCA.OBJECTS.REXON;

public class RexonInstance : ActiveObject
{
	private const int AnimationBody = 0;
	private const int AnimationNeck = 1;
	private const int AnimationMouth = 2;
	private const int AnimationHead = 3;
	private const int AnimationMouthFire = 4;
	private const int AnimationHeadFire = 5;
	private const int AnimationProjectile = 6;
	private AnimationGroup _animationGroup;
	private AnimationInstance _animationBody;
	private AnimationInstance _animationNeck;
	private AnimationInstance _animationMouth;
	private AnimationInstance _animationHead;
	private Vector2 _velocity;
	private bool _headLifted;
	private bool _facingRight;
	private int _turnTicksRemaining;
	private readonly RexonInstance.Neck[] _necks = new RexonInstance.Neck[5];
	private bool _headIsAlive;

	private RexonInstance.HeadBack Head => (RexonInstance.HeadBack)_necks[4];

	public RexonInstance() => DesignBounds = new Rectanglei(-64, -32, 128 /*0x80*/, 64 /*0x40*/);

	protected override void OnStart()
	{
		base.OnStart();
		_animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, "/ANIGROUP");
		_animationBody = new AnimationInstance(_animationGroup);
		_animationNeck = new AnimationInstance(_animationGroup, 1);
		_animationMouth = new AnimationInstance(_animationGroup, 2);
		_animationHead = new AnimationInstance(_animationGroup, 3);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -32, 128 /*0x80*/, 64 /*0x40*/));
		_velocity = new Vector2(-0.5, 0.0);
		_turnTicksRemaining = 128 /*0x80*/;
	}

	protected override void OnUpdate()
	{
		if (!_headLifted)
			WaitForCharacter();
		if (Head == null)
			return;
		_headIsAlive = !Head.Finished;
	}

	private void WaitForCharacter()
	{
		ICharacter closestCharacterTo = Level.ObjectManager.GetClosestCharacterTo((Vector2)Position);
		if (closestCharacterTo != null && Math.Abs(Position.X - closestCharacterTo.Position.X) < 640)
		{
			_facingRight = closestCharacterTo.Position.X > Position.X;
			_velocity = new Vector2();
			CreateNeckAndHead();
		}
		else
		{
			if (_turnTicksRemaining-- <= 0)
			{
				_velocity.X *= -1.0;
				_facingRight = !_facingRight;
				_turnTicksRemaining = 128 /*0x80*/;
			}
			PositionPrecise = PositionPrecise + _velocity;
		}
	}

	private void CreateNeckAndHead()
	{
		_headIsAlive = true;
		_headLifted = true;
		RexonInstance.Neck neck1 = null;
		for (int index = 4; index >= 0; --index)
		{
			RexonInstance.Neck neck2 = index == 4 ? Level.ObjectManager.AddSubObject<RexonInstance.HeadBack>(this) : Level.ObjectManager.AddSubObject<RexonInstance.Neck>(this);
			_necks[index] = neck2;
			neck2.Prepare(index);
			if (neck1 != null)
			{
				neck2.NextNeck = neck1;
				neck1.PreviousNeck = neck2;
			}
			neck1 = neck2;
		}
	}

	protected override void OnAnimate()
	{
		if (_headIsAlive)
		{
			if (Head.NextProjectileTicksRemaining < _animationGroup[4].Duration - 4)
				_animationMouth.Index = 4;
			if (Head.NextProjectileTicksRemaining < _animationGroup[5].Duration - 4)
				_animationHead.Index = 5;
		}
		_animationBody.Animate();
		_animationNeck.Animate();
		_animationMouth.Animate();
		_animationHead.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animationBody, _facingRight);
	}

	private class Neck : Badnik
	{
		private static readonly IReadOnlyList<int> NeckRaiseTicks = new int[6]
		{
	  30,
	  24,
	  18,
	  12,
	  6,
	  0
		};
		private static readonly IReadOnlyList<int> UnknownA = new int[5]
		{
	  0,
	  36,
	  32 /*0x20*/,
	  28,
	  26
		};
		private static readonly IReadOnlyList<double> DeathXVelocities = new double[5]
		{
	  2.0,
	  -4.0,
	  4.0,
	  -2.0,
	  2.0
		};
		private int _index;
		private Vector2 _velocity;
		private int _oscillation;

		public RexonInstance Rexon => (RexonInstance)ParentObject;

		public RexonInstance.Neck PreviousNeck { get; set; }

		public RexonInstance.Neck NextNeck { get; set; }

		protected RexonInstance.Neck.NeckState State { get; set; }

		protected int StateTicksRemaining { get; set; }

		public virtual void Prepare(int index)
		{
			_index = index;
			StateTicksRemaining = RexonInstance.Neck.NeckRaiseTicks[_index];
			_oscillation += _index * 16 /*0x10*/;
			Vector2i vector2i = new Vector2i(-80, 64 /*0x40*/);
			if (Rexon._facingRight)
				vector2i.X *= -1;
			Position = Position + vector2i;
			Priority = ParentObject.Priority + 1 + _index;
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
			};
			CanBeDestoryed = false;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			switch (State)
			{
				case RexonInstance.Neck.NeckState.PreRise:
					CheckHeadIsAlive();
					if (StateTicksRemaining-- >= 0)
						break;
					StartRaise();
					break;
				case RexonInstance.Neck.NeckState.Rise:
					CheckHeadIsAlive();
					_velocity.X += 0.25;
					if (StateTicksRemaining-- < 0)
						StartNormalState();
					PositionPrecise = PositionPrecise + _velocity;
					break;
				case RexonInstance.Neck.NeckState.Oscillate:
					CheckHeadIsAlive();
					Oscillate();
					break;
				case RexonInstance.Neck.NeckState.Drop:
					_velocity.Y += 0.875;
					PositionPrecise = PositionPrecise + _velocity;
					break;
			}
		}

		private void StartRaise()
		{
			State = RexonInstance.Neck.NeckState.Rise;
			_velocity = new Vector2(-0.45, -8.0);
			StateTicksRemaining = RexonInstance.Neck.NeckRaiseTicks[4 - _index];
		}

		private void StartNormalState()
		{
			State = RexonInstance.Neck.NeckState.Oscillate;
			_velocity = new Vector2();
			StateTicksRemaining = 32 /*0x20*/;
			_oscillation = RexonInstance.Neck.UnknownA[_index] * 4;
		}

		private void CheckHeadIsAlive()
		{
			if (Rexon._headIsAlive)
				return;
			State = RexonInstance.Neck.NeckState.Drop;
			_velocity.X = RexonInstance.Neck.DeathXVelocities[_index];
		}

		private void Oscillate()
		{
			_oscillation = (_oscillation + 1) % 128 /*0x80*/;
			double num = _oscillation / 128.0 * (2.0 * Math.PI);
			int x = (int)(Math.Cos(num) * 24.0);
			int y = (int)(-Math.Abs(Math.Sin(num)) * 8.0) - 52;
			if (this is RexonInstance.HeadBack)
				y += 16 /*0x10*/;
			if (PreviousNeck != null)
				Position = PreviousNeck.Position + new Vector2i(x, y);
			if (_index != 4)
				return;
			Position = Position + new Vector2i(Rexon._facingRight ? 18 : -18, 0);
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			Animation animation = Rexon._animationNeck.Animation;
			int count = animation.Frames.Count;
			Animation.Frame frame = animation.Frames[MathX.Clamp(0, (NextNeck._oscillation + (Rexon._facingRight ? 44 : 108)) % 128 /*0x80*/, count - 1)];
			Rectanglei source = frame.Source;
			objectRenderer.Texture = Rexon._animationNeck.AnimationGroup.Textures[frame.TextureIndex];
			objectRenderer.Render((Rectangle)source, (Vector2)new Vector2i(), Rexon._facingRight);
		}

		protected enum NeckState
		{
			PreRise,
			Rise,
			Oscillate,
			Drop,
		}
	}

	private class HeadBack : RexonInstance.Neck
	{
		private RexonInstance.HeadFront _headFront;

		public int NextProjectileTicksRemaining
		{
			get
			{
				return State != RexonInstance.Neck.NeckState.Oscillate ? sbyte.MaxValue : StateTicksRemaining;
			}
		}

		public override void Prepare(int index)
		{
			base.Prepare(index);
			_headFront = Level.ObjectManager.AddSubObject<RexonInstance.HeadFront>(this);
			CanBeDestoryed = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (State == RexonInstance.Neck.NeckState.Oscillate)
			{
				if (StateTicksRemaining-- < 0)
					FireProjectile();
			}
			if (_headFront == null)
				return;
			_headFront.Position = Position;
		}

		private void FireProjectile()
		{
			StateTicksRemaining = sbyte.MaxValue;
			RexonInstance.Projectile projectile = Level.ObjectManager.AddSubObject<RexonInstance.Projectile>(ParentObject);
			projectile.Position = Position + new Vector2i(Rexon._facingRight ? 16 /*0x10*/ : -16, 4);
			projectile.Velocity = new Vector2(4 * (Rexon._facingRight ? 1 : -1), 2.0);
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(Rexon._animationMouth, Rexon._facingRight);
		}
	}

	private class HeadFront : ActiveObject
	{
		public RexonInstance.Neck Parent => ParentObject as RexonInstance.Neck;

		protected override void OnStart()
		{
			LockLifetime = true;
			Priority = 512 /*0x0200*/;
		}

		protected override void OnUpdate()
		{
			if (!Parent.Finished)
				return;
			Finish();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			RexonInstance parentObject = ParentObject.ParentObject as RexonInstance;
			renderer.GetObjectRenderer().Render(parentObject._animationHead, parentObject._facingRight);
		}
	}

	private class Projectile : Enemy
	{
		private AnimationInstance _animation;

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 6);
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -25, -25, 50, 50)
			};
			Priority += 32 /*0x20*/;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			PositionPrecise = PositionPrecise + Velocity;
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation);
		}
	}
}
