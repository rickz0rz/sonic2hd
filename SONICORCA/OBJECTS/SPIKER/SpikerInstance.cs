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

/// <summary>
/// Spiker, a drill badnik with a spike on its back. It moves left and right with rests between. When a character
/// comes near, the spike fires up (or down, when the Spiker is upside down).
/// </summary>
public class SpikerInstance : Badnik
{
	private const int AnimationHead = 2;
	private const int AnimationBodyTurn = 3;
	private const int Speed = 2;
	private const int MoveTicks = 64;
	private const int RestTicks = 16;
	private const int DetectDistanceX = 128;
	private const int DetectDistanceY = 384;
	private AnimationInstance _animation;
	private bool _flipX;
	private bool _flipY;
	private bool _turning;
	private int _velocityX;
	private bool _resting;
	private bool _spikeFired;
	private int _stateTimer;
	private Spike _spike;

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
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpikerType.AnimationGroupResourceKey), AnimationHead);
		_resting = false;
		_velocityX = Speed;
		_stateTimer = MoveTicks;
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -32, _flipY ? -128 : 48, 64, 64)
		};
		_spike = Level.ObjectManager.AddSubObject<Spike>(this);
		_spike.Position = Position;
		_spike.FlipY = _flipY;
		_spike.LockLifetime = true;
	}

	protected override void Rebound(ICharacter character)
	{
		// The decompiled code had two branches here (character below or above the Spiker), and both did the same.
		Vector2 velocity = character.Velocity;
		if (velocity.Y < 0.0)
			velocity.Y += 4.0;
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
				_stateTimer = MoveTicks;
				_velocityX *= -1;
			}
		}
		else
		{
			Position = Position + new Vector2i(_velocityX, 0);
			if (!_spikeFired)
			{
				_spike.Position = _spike.Position + new Vector2i(_velocityX, 0);
				_spike._velocityX = _velocityX;
			}
			if (_stateTimer-- <= 0)
			{
				_resting = true;
				_stateTimer = RestTicks;
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
		ICharacter closestCharacter = Level.ObjectManager.GetClosestCharacterTo((Vector2)Position);
		return closestCharacter != null && Math.Abs(Position.X - closestCharacter.Position.X) < DetectDistanceX && Math.Abs(Position.Y - closestCharacter.Position.Y) < DetectDistanceY;
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
		bool movingRight = _velocityX > 0;
		if (_flipX == movingRight)
			return;

		// The direction changed: play the turn animation, then flip the body when it ends.
		if (_animation.Index == AnimationHead && !_turning)
		{
			_animation.Index = AnimationBodyTurn;
			_animation.Cycles = 0;
			_animation.ResetFrame();
			_turning = true;
		}
		else
		{
			if (_animation.Index != AnimationBodyTurn || _animation.Cycles < 1)
				return;
			_turning = false;
			_animation.Index = AnimationHead;
			_animation.Cycles = 0;
			_animation.ResetFrame();
			_flipX = movingRight;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			objectRenderer.ModelMatrix *= Matrix4.CreateScale(_flipX ? -1.0 : 1.0, _flipY ? -1.0 : 1.0);
			if (_animation.Index == AnimationBodyTurn)
			{
				objectRenderer.Render(_animation, new Vector2(0.0, 8.0), true);
			}
			else
			{
				if (_animation.Index != AnimationHead)
					return;
				if (_animation.CurrentFrameIndex == 0)
					objectRenderer.Render(_animation, new Vector2(0.0, 0.0));
				else
					objectRenderer.Render(_animation);
			}
		}
	}

	/// <summary>
	/// The spike on the back of a Spiker. It moves with the Spiker, turns with it, and flies straight up (or down)
	/// when it is fired.
	/// </summary>
	private class Spike : Enemy
	{
		public const int AnimationSpikeLeft = 0;
		public const int AnimationSpikeRight = 4;
		private const int AnimationSpikeSpinningLeft = 1;
		private const int AnimationSpikeSpinningRight = 6;
		public const int AnimationSpikeTurnToRight = 5;
		public const int AnimationSpikeTurnToLeft = 7;
		private const int FireSpeed = 8;
		public AnimationInstance _animation;
		private bool _fired;
		private bool _turning;
		public int _velocityX;

		public bool FlipX { get; set; }

		public bool FlipY { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -32, -72, 64, 144)
			};
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SpikerType.AnimationGroupResourceKey));
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (!_fired)
				return;
			Position = Position + new Vector2i(0, -FireSpeed * (FlipY ? -1 : 1));
		}

		public void Fire()
		{
			_animation.Index = FlipX ? AnimationSpikeSpinningRight : AnimationSpikeSpinningLeft;
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
			bool movingRight = _velocityX > 0;
			if (FlipX == movingRight)
				return;

			// The direction changed: play the turn animation, then flip the spike when it ends.
			if ((_animation.Index == AnimationSpikeRight || _animation.Index == AnimationSpikeLeft) && !_turning)
			{
				_animation.Index = !movingRight ? AnimationSpikeTurnToLeft : AnimationSpikeTurnToRight;
				_animation.Cycles = 0;
				_animation.ResetFrame();
				_turning = true;
			}
			else
			{
				if (_animation.Index != AnimationSpikeTurnToLeft && _animation.Index != AnimationSpikeTurnToRight || _animation.Cycles < 1)
					return;
				_turning = false;
				FlipX = movingRight;
				_animation.Index = !FlipX ? AnimationSpikeLeft : AnimationSpikeRight;
				_animation.Cycles = 0;
				_animation.ResetFrame();
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			if (_animation.Index == AnimationSpikeTurnToLeft || _animation.Index == AnimationSpikeTurnToRight)
			{
				int x = _animation.Index == AnimationSpikeTurnToLeft ? -2 : 2;
				objectRenderer.Render(_animation, (Vector2)new Vector2i(x, 0), flipY: FlipY);
			}
			else
			{
				int x = 0;
				if (_animation.Index == AnimationSpikeLeft || _animation.Index == AnimationSpikeRight)
					x = _animation.Index == AnimationSpikeLeft ? 1 : -2;
				objectRenderer.Render(_animation, new Vector2(x, 0.0), flipY: FlipY);
			}
		}
	}
}
