using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SHELLCRACKER;

/// <summary>
/// Shellcracker, as in Sonic 2 (Obj9F and ObjA0). The crab walks at $40 along the floor. It always starts to the
/// left, and it does not turn its body when it turns. It walks for $140 frames, or until a ledge or a wall, and then
/// waits 60 frames. When the nearer player is less than $60 pixels away on the x axis (and not behind a crab that
/// faces left), it waits 9 frames and shoots its claw. The claw and 7 chain links fly out at $400 one after the other,
/// wait 8 frames, and come back. The crab waits $20 frames more and then walks again. The claw and the links hurt.
/// When the crab is destroyed, they fall.
/// </summary>
public class ShellcrackerInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int WalkSpeed = 0x40;

	private const int WalkTicks = 0x140;

	private const int PauseTicks = 0x3B;

	private const int AimTicks = 8;

	private const int RecoverTicks = 0x20;

	private const int TriggerDistance = 0x60;

	private const int YRadius = 0xC;

	/// <summary>Collision size $A: 16 by 8 pixels to each side.</summary>
	private const int TouchWidth = 0x10 * Scale;

	private const int TouchHeight = 8 * Scale;

	private const int ClawPieces = 8;

	private const int BodyFrame = 3;

	private static readonly byte[][] Scripts =
	{
		new byte[] { 0xE, 0, 1, 2, 0xFF }
	};

	private readonly AnimateSpriteScript _script = new AnimateSpriteScript(Scripts);

	private AnimationInstance _animation;

	private bool _flipX;

	private State _state;

	private int _timer;

	private int _velocityX;

	private bool _clawBack;

	private enum State
	{
		Walk,
		Pause,
		Aim,
		ClawOut,
		Recover
	}

	/// <summary>The crab faces right and shoots its claw to the right (the flip of the original).</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public ShellcrackerInstance() => DesignBounds = new Rectanglei(-0x60 * Scale, -0x20 * Scale, 0xC0 * Scale, 0x40 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ShellcrackerType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchWidth, -TouchHeight, TouchWidth * 2, TouchHeight * 2)
		};
		_velocityX = -WalkSpeed;
		_timer = WalkTicks;
		_state = State.Walk;
	}

	protected override void OnUpdate()
	{
		switch (_state)
		{
			case State.Walk:
				if (PlayerInReach())
				{
					StartAim();
					break;
				}
				Walk();
				break;
			case State.Pause:
				if (PlayerInReach())
				{
					StartAim();
					break;
				}
				if (--_timer < 0)
				{
					_state = State.Walk;
					_timer = WalkTicks;
				}
				break;
			case State.Aim:
				if (--_timer < 0)
				{
					_state = State.ClawOut;
					_script.Frame = BodyFrame;
					ShootClaw();
				}
				break;
			case State.ClawOut:
				if (_clawBack)
				{
					_state = State.Recover;
					_timer = RecoverTicks;
				}
				break;
			case State.Recover:
				if (--_timer < 0)
				{
					_clawBack = false;
					_state = State.Walk;
					_timer = WalkTicks;
				}
				break;
		}
	}

	/// <summary>
	/// The nearer player is less than $60 pixels away on the x axis. The original ignores a player to the right of a
	/// crab that faces left, but not a player to the left of a crab that faces right.
	/// </summary>
	private bool PlayerInReach()
	{
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		if (orientation.Player == null || (orientation.PlayerRight && !_flipX))
		{
			return false;
		}
		return orientation.DeltaX >= -TriggerDistance && orientation.DeltaX < TriggerDistance;
	}

	private void StartAim()
	{
		_state = State.Aim;
		_script.Frame = 0;
		_timer = AimTicks;
	}

	private void Walk()
	{
		PositionPrecise += new Vector2(_velocityX * Unit, 0.0);
		int? floor = LandscapeFloor.GetDistance(Level, Position + new Vector2i(0, YRadius * Scale));
		if (floor == null || floor < -8 || floor >= 0xC)
		{
			_velocityX = -_velocityX;
			StartPause();
			return;
		}
		PositionPrecise += new Vector2(0.0, floor.Value * Scale);
		if (--_timer < 0)
		{
			StartPause();
			return;
		}
		_script.Step();
	}

	private void StartPause()
	{
		_state = State.Pause;
		_script.Frame = 0;
		_timer = PauseTicks;
	}

	/// <summary>The claw (piece 0) and the 7 links start at the shoulder, 8 pixels up.</summary>
	private void ShootClaw()
	{
		for (int i = 0; i < ClawPieces; i++)
		{
			int x = _flipX ? (i == 0 ? 0x14 : 8) : -0x14;
			ClawPiece piece = Level.ObjectManager.AddSubObject<ClawPiece>(this);
			piece.Setup(this, i, PositionPrecise + new Vector2(x * Scale, -8 * Scale));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		// The art faces left
		_animation.Index = _script.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}

	/// <summary>
	/// A piece of the claw (ObjA0): the claw or a chain link. It waits, flies out at $400, waits 8 frames and flies
	/// back. The last link does not move. When the claw is back, the crab shows its claw again. Collision $9A: it
	/// hurts, 12 by 12 pixels to each side. When the crab is gone, the piece falls with gravity for 64 frames.
	/// </summary>
	private class ClawPiece : Enemy
	{
		private const int Speed = 0x400;

		private const int Gravity = 0x38;

		private const int HoldTicks = 8;

		private const int FallTicks = 0x40;

		private const int TouchRadius = 0xC * Scale;

		private const int ClawFrame = 5;

		private const int LinkFrame = 4;

		/// <summary>The frames that each piece waits before it moves.</summary>
		private static readonly int[] Delays = { 0, 3, 5, 7, 9, 0xB, 0xD, 0xF };

		/// <summary>The frames that each piece moves out, and back.</summary>
		private static readonly int[] Distances = { 0xD, 0xC, 0xA, 8, 6, 4, 2 };

		private ShellcrackerInstance _crab;

		private AnimationInstance _animation;

		private int _index;

		private bool _facingRight;

		private PieceState _state;

		private int _timer;

		private int _distance;

		private int _velocityX;

		private int _velocityY;

		private enum PieceState
		{
			Wait,
			Out,
			Hold,
			Back,
			Fall
		}

		public ClawPiece() => DesignBounds = new Rectanglei(-64, -64, 128, 128);

		public void Setup(ShellcrackerInstance crab, int index, Vector2 position)
		{
			_crab = crab;
			_index = index;
			_facingRight = crab._flipX;
			_timer = Delays[index];
			Priority = crab.Priority + 1;
			// A link is 6 pixels right and down from the claw
			PositionPrecise = index == 0 ? position : position + new Vector2(6 * Scale, 6 * Scale);
		}

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ShellcrackerType.AnimationGroupResourceKey));
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2)
			};
		}

		protected override void OnUpdate()
		{
			if (_state != PieceState.Fall && (_crab == null || _crab.Finished))
			{
				_state = PieceState.Fall;
				_timer = FallTicks;
			}
			switch (_state)
			{
				case PieceState.Wait:
					if (--_timer > 0)
					{
						break;
					}
					_state = PieceState.Out;
					if (_index == ClawPieces - 1)
					{
						// The last link stays at the shoulder
						_timer = 0;
						_distance = 0xB;
					}
					else
					{
						_velocityX = _facingRight ? Speed : -Speed;
						_timer = Distances[_index];
						_distance = _timer;
					}
					break;
				case PieceState.Out:
					Move();
					if (--_timer <= 0)
					{
						_state = PieceState.Hold;
						_timer = HoldTicks;
					}
					break;
				case PieceState.Hold:
					if (--_timer <= 0)
					{
						_state = PieceState.Back;
						_velocityX = -_velocityX;
					}
					break;
				case PieceState.Back:
					Move();
					if (--_distance <= 0)
					{
						if (_index == 0)
						{
							_crab._script.Frame = 0;
							_crab._clawBack = true;
						}
						FinishForever();
					}
					break;
				case PieceState.Fall:
					Move();
					_velocityY += Gravity;
					if (--_timer < 0)
					{
						FinishForever();
					}
					break;
			}
		}

		private void Move() => PositionPrecise += new Vector2(_velocityX * Unit, _velocityY * Unit);

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			_animation.Index = _index == 0 ? ClawFrame : LinkFrame;
			renderer.GetObjectRenderer().RenderOriginal(_animation, _facingRight);
		}

		protected override void OnStop()
		{
			base.OnStop();
			FinishForever();
		}
	}
}
