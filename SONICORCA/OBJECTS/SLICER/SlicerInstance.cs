using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SLICER;

/// <summary>
/// Slicer, as in Sonic 2 (ObjA1 and ObjA2). The mantis walks at $40 along the floor in the direction that it faces.
/// At a ledge or a wall it waits 60 frames and turns. When the nearer player is in front of it, less than $80 pixels
/// away on the x axis and less than $40 pixels on the y axis, it raises its arms for 9 frames and throws its two
/// pincers. After that it stands still without arms. Each pincer flies out at $200 and then homes in on the nearer
/// player for 120 frames: it adds $10 to its speed toward the player each frame, up to $200 on each axis. Then it
/// falls with gravity for 96 frames. The pincers hurt.
/// </summary>
public class SlicerInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int WalkSpeed = 0x40;

	private const int TurnTicks = 0x3B;

	private const int ThrowTicks = 8;

	private const int TriggerDistanceX = 0x80;

	private const int TriggerDistanceY = 0x40;

	private const int YRadius = 0x10;

	/// <summary>Collision size 6: 16 by 16 pixels to each side.</summary>
	private const int TouchRadius = 0x10 * Scale;

	private const int ArmsUpFrame = 3;

	private const int NoArmsFrame = 4;

	/// <summary>The pincers start at these x offsets (for a Slicer that faces left).</summary>
	private static readonly int[] PincerOffsets = { 6, -0x10 };

	private static readonly AnimationScript.Script[] Scripts =
	{
		new AnimationScript.Script(0x13, new[] { 0, 2 })
	};

	private static readonly AnimationScript.Script[] PincerScripts =
	{
		new AnimationScript.Script(3, new[] { 5, 6, 7, 8 })
	};

	private readonly AnimationScript _script = new AnimationScript(Scripts);

	private AnimationInstance _animation;

	private bool _flipX;

	private bool _facingRight;

	private State _state;

	private int _timer;

	private int _velocityX;

	private enum State
	{
		Walk,
		Turn,
		Throw,
		Done
	}

	/// <summary>The Slicer faces right and walks to the right at the start (the flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public SlicerInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, -0x20 * Scale, 0x40 * Scale, 0x40 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SlicerType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
		};
		_facingRight = _flipX;
		_velocityX = _facingRight ? WalkSpeed : -WalkSpeed;
		_state = State.Walk;
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Walk:
				if (PlayerInFront())
				{
					_state = State.Throw;
					_script.Frame = ArmsUpFrame;
					_timer = ThrowTicks;
					break;
				}
				Walk();
				break;
			case State.Turn:
				if (--_timer < 0)
				{
					_state = State.Walk;
					_velocityX = -_velocityX;
					_facingRight = !_facingRight;
				}
				break;
			case State.Throw:
				if (--_timer < 0)
				{
					_state = State.Done;
					_script.Frame = NoArmsFrame;
					ThrowPincers();
				}
				break;
		}
	}

	private bool PlayerInFront()
	{
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		if (orientation.Player == null || orientation.PlayerRight != _facingRight)
		{
			return false;
		}
		return orientation.DeltaX >= -TriggerDistanceX && orientation.DeltaX < TriggerDistanceX &&
			orientation.DeltaY >= -TriggerDistanceY && orientation.DeltaY < TriggerDistanceY;
	}

	private void Walk()
	{
		PositionPrecise += new Vector2(_velocityX * Unit, 0.0);
		int? floor = LandscapeFloor.GetDistance(Level, Position + new Vector2i(0, YRadius * Scale));
		if (floor == null || floor < -8 || floor >= 0xC)
		{
			_state = State.Turn;
			_timer = TurnTicks;
			return;
		}
		PositionPrecise += new Vector2(0.0, floor.Value * Scale);
		_script.Step();
	}

	private void ThrowPincers()
	{
		foreach (int offset in PincerOffsets)
		{
			Pincer pincer = Level.ObjectManager.AddSubObject<Pincer>(this);
			pincer.Setup(this, PositionPrecise + new Vector2((_facingRight ? -offset : offset) * Scale, 0.0));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The art faces left
		_animation.Index = _script.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
	}

	/// <summary>
	/// A pincer (ObjA2). It homes in on the nearer player for 120 frames while its Slicer is there, and then falls with
	/// gravity $38 for 96 frames. Collision $9A: it hurts, 12 by 12 pixels to each side.
	/// </summary>
	private class Pincer : Enemy
	{
		private const int Speed = 0x200;

		private const int Acceleration = 0x10;

		private const int Gravity = 0x38;

		private const int HomeTicks = 0x78;

		private const int FallTicks = 0x60;

		private const int TouchRadius = 0xC * Scale;

		private readonly AnimationScript _script = new AnimationScript(PincerScripts);

		private SlicerInstance _slicer;

		private AnimationInstance _animation;

		private bool _facingRight;

		private bool _falling;

		private int _timer = HomeTicks;

		private int _velocityX;

		private int _velocityY;

		public Pincer() => DesignBounds = new Rectanglei(-64, -64, 128, 128);

		public void Setup(SlicerInstance slicer, Vector2 position)
		{
			_slicer = slicer;
			_facingRight = slicer._facingRight;
			_velocityX = _facingRight ? Speed : -Speed;
			_script.Frame = 5;
			Priority = slicer.Priority + 1;
			PositionPrecise = position;
		}

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SlicerType.AnimationGroupResourceKey));
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
			};
		}

		protected override void OnUpdate()
		{
			if (!_falling && (--_timer < 0 || _slicer == null || _slicer.Finished))
			{
				_falling = true;
				_timer = FallTicks;
			}
			if (_falling)
			{
				if (--_timer < 0)
				{
					FinishForever();
					return;
				}
				Move();
				_velocityY += Gravity;
			}
			else
			{
				PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
				_velocityX = Math.Clamp(_velocityX + (orientation.PlayerRight ? Acceleration : -Acceleration), -Speed, Speed);
				_velocityY = Math.Clamp(_velocityY + (orientation.PlayerBelow ? Acceleration : -Acceleration), -Speed, Speed);
				Move();
			}
			_script.Step();
		}

		private void Move() => PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			_animation.Index = _script.Frame;
			renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
