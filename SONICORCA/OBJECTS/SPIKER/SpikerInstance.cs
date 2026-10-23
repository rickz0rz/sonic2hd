// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.SPIKER.SpikerInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.SPIKER;

public class SpikerInstance : Badnik
{
	private const int AnimationHead = 2;
	private const int AnimationBodyTurn = 3;
	private AnimationInstance _animation;
	private bool _flipX;
	private bool _flipY;
	private bool _turning;
	private int _velocityX;
	private bool _resting;
	private bool _spikeFired;
	private int _stateTimer;
	private SpikerInstance.Spike _spike;

	[StateVariable]
	private bool UpsideDown
	{
		get => _flipY;
		set => _flipY = value;
	}

	public SpikerInstance() => DesignBounds = new Rectanglei(-115, -165, 230, 330);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"), 2);
		_resting = false;
		_velocityX = 2;
		_stateTimer = 64 /*0x40*/;
		CollisionRectangles = new CollisionRectangle[1]
		{
	  new CollisionRectangle( this, 0, -32, _flipY ?  sbyte.MinValue : 48 /*0x30*/, 64 /*0x40*/, 64 /*0x40*/)
		};
		_spike = Level.ObjectManager.AddSubObject<SpikerInstance.Spike>(this);
		_spike.Position = Position;
		_spike.FlipY = _flipY;
		_spike.LockLifetime = true;
	}

	protected override void Rebound(ICharacter character)
	{
		Vector2 velocity = character.Velocity;
		if (velocity.Y < 0.0)
			velocity.Y += 4.0;
		else if (character.Position.Y >= Position.Y)
			velocity.Y *= -1.0;
		else
			velocity.Y *= -1.0;
		character.Velocity = velocity;
	}

	protected override void OnUpdateEditor()
	{
		base.OnUpdateEditor();
		_spike.Position = Position;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_resting)
		{
			if (_stateTimer-- <= 0)
			{
				_resting = false;
				_stateTimer = 64 /*0x40*/;
				_velocityX *= -1;
			}
		}
		else
		{
			Position = Position + new Vector2i(_velocityX, 0);
			if (!_spikeFired)
			{
				SpikerInstance.Spike spike = _spike;
				spike.Position = spike.Position + new Vector2i(_velocityX, 0);
				_spike._velocityX = _velocityX;
			}
			if (_stateTimer-- <= 0)
			{
				_resting = true;
				_stateTimer = 16 /*0x10*/;
			}
		}
		if (!CheckForCharacter())
			return;
		_spike.Fire();
		_spikeFired = true;
		_spike.LockLifetime = false;
		_spike._velocityX = 0;
	}

	private bool CheckForCharacter()
	{
		ICharacter closestCharacterTo = Level.ObjectManager.GetClosestCharacterTo((Vector2)Position);
		return closestCharacterTo != null && Math.Abs(Position.X - closestCharacterTo.Position.X) < 128 /*0x80*/ && Math.Abs(Position.Y - closestCharacterTo.Position.Y) < 384;
	}

	protected override void OnStop()
	{
		base.OnStop();
		if (_spikeFired)
			return;
		_spike.FinishForever();
	}

	protected override void OnAnimate()
	{
		if (!_resting)
			_animation.Animate();
		int num1 = _flipX ? 1 : 0;
		bool flag = _velocityX > 0;
		int num2 = flag ? 1 : 0;
		if (num1 == num2)
			return;
		if (_animation.Index == 2 && !_turning)
		{
			_animation.Index = 3;
			_animation.Cycles = 0;
			_animation.ResetFrame();
			_turning = true;
		}
		else
		{
			if (_animation.Index != 3 || _animation.Cycles < 1)
				return;
			_turning = false;
			_animation.Index = 2;
			_animation.Cycles = 0;
			_animation.ResetFrame();
			_flipX = flag;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			objectRenderer.ModelMatrix *= Matrix4.CreateScale(_flipX ? -1.0 : 1.0, _flipY ? -1.0 : 1.0);
			if (_animation.Index == 3)
			{
				objectRenderer.Render(_animation, new Vector2(0.0, 8.0), true);
			}
			else
			{
				if (_animation.Index != 2)
					return;
				if (_animation.CurrentFrameIndex == 0)
					objectRenderer.Render(_animation, new Vector2(0.0, 0.0));
				else
					objectRenderer.Render(_animation);
			}
		}
	}

	private class Spike : Enemy
	{
		public const int AnimationSpikeLeft = 0;
		public const int AnimationSpikeRight = 4;
		private const int AnimationSpikeSpinningLeft = 1;
		private const int AnimationSpikeSpinningRight = 6;
		public const int AnimationSpikeTurnToRight = 5;
		public const int AnimationSpikeTurnToLeft = 7;
		public AnimationInstance _animation;
		private bool _fired;
		private bool _turning;
		public int _velocityX;

		public bool FlipX { get; set; }

		public bool FlipY { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -32, -72, 64 /*0x40*/, 144 /*0x90*/)
			};
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (!_fired)
				return;
			Position = Position + new Vector2i(0, -8 * (FlipY ? -1 : 1));
		}

		public void Fire()
		{
			_animation.Index = FlipX ? 6 : 1;
			_fired = true;
		}

		protected override void OnStop()
		{
			base.OnStop();
			if (!_fired)
				return;
			FinishForever();
		}

		protected override void OnAnimate()
		{
			base.OnAnimate();
			_animation.Animate();
			int num1 = FlipX ? 1 : 0;
			bool flag = _velocityX > 0;
			int num2 = flag ? 1 : 0;
			if (num1 == num2)
				return;
			if ((_animation.Index == 4 || _animation.Index == 0) && !_turning)
			{
				_animation.Index = !flag ? 7 : 5;
				_animation.Cycles = 0;
				_animation.ResetFrame();
				_turning = true;
			}
			else
			{
				if (_animation.Index != 7 && _animation.Index != 5 || _animation.Cycles < 1)
					return;
				_turning = false;
				FlipX = flag;
				_animation.Index = !FlipX ? 0 : 4;
				_animation.Cycles = 0;
				_animation.ResetFrame();
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			if (_animation.Index == 7 || _animation.Index == 5)
			{
				int x = _animation.Index == 7 ? -2 : 2;
				objectRenderer.Render(_animation, (Vector2)new Vector2i(x, 0), flipY: FlipY);
			}
			else
			{
				int x = 0;
				if (_animation.Index == 0 || _animation.Index == 4)
					x = _animation.Index == 0 ? 1 : -2;
				objectRenderer.Render(_animation, new Vector2(x, 0.0), flipY: FlipY);
			}
		}
	}
}
