using System;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// The movement of a player in the half-pipe, as in the Sonic 2 special stage (s2disasm: SSPlayer_Move,
/// SSPlayer_Traction, SSObjectMove, SSPlayer_Jump, SSObjectMoveAndFall, SSPlayer_JumpAngle,
/// SSPlayer_DoLevelCollision and SSHurt_Animation). It uses the units of the original so that it moves the same:
/// <list type="bullet">
/// <item>Angle: one byte, 256 units for each circle. $40 is the bottom of the pipe, $00 the right wall, $80 the left
/// wall and $C0 the top. The angle goes up toward the left.</item>
/// <item>Inertia: the speed around the pipe, in 1/256 angle units for each frame.</item>
/// <item>Position in the air: pixels from the centre of the pipe, with Y down. Velocities in 1/256 pixels.</item>
/// </list>
/// Call <see cref="Update"/> once for each frame (60 each second).
/// </summary>
public sealed class HalfPipePlayer
{
	/// <summary>The pipe radius in pixels (ss_z_pos) of the inner lane. Sonic starts in this lane.</summary>
	public const int InnerRadius = 0x6E;

	/// <summary>The pipe radius in pixels of the outer lane, nearer to the camera. Tails starts in this lane.</summary>
	public const int OuterRadius = 0x80;

	/// <summary>The radius at which the outer lane starts: a player there shows in front of the other player.</summary>
	private const int FrontRadius = 0x77;

	public const int BottomAngle = 0x40;

	private const int Acceleration = 0x60;

	private const int MaxInertia = 0x600;

	/// <summary>The frames after the pad is released during which the slope does not pull the player.</summary>
	private const int SlideFrames = 0x1E;

	private const int Traction = 0x50;

	/// <summary>The slowest speed upside down at which the player stays on the pipe.</summary>
	private const int FallInertia = 0x100;

	private const int JumpSpeed = 0x780;

	private const int Gravity = 0xA8;

	private const int AirControl = 0x40;

	/// <summary>The hurt timer goes up by this amount each frame, for 32 frames.</summary>
	private const int HurtTimerStep = 8;

	/// <summary>The frames of flashing after the hurt animation, during which bombs do no damage.</summary>
	public const int FlashFrames = 0x1E;

	private enum Mode
	{
		Ground,
		Jump,
		Fall
	}

	private Mode _mode = Mode.Ground;

	private bool _slowing;

	private int _slideTimer;

	// Position and velocity in the air: 16.16 fixed-point pixels, and 1/256 pixels for each frame
	private int _x;

	private int _y;

	private int _xVelocity;

	private int _yVelocity;

	// The hurt state (routine_secondary) and its timer, which goes up by 8 each frame until it wraps to 0
	private bool _hurt;

	private int _hurtTimer;

	public int Angle { get; private set; } = BottomAngle;

	/// <summary>The pipe radius of the player in pixels. It moves one pixel each frame toward the lane.</summary>
	public int Radius { get; private set; }

	/// <summary>The lane that the player moves to: true for the outer lane, false for the inner lane.</summary>
	public bool OuterLane { get; set; }

	/// <summary>True when the player is in front of a player in the other lane.</summary>
	public bool InFront => Radius >= FrontRadius;

	public short Inertia { get; private set; }

	/// <summary>True on the pipe surface: only then can the player collect rings and hit bombs.</summary>
	public bool OnGround => _mode == Mode.Ground;

	/// <summary>True during a jump. A fall from the top of the pipe is not a jump: the player does not curl up.</summary>
	public bool IsJumping => _mode == Mode.Jump;

	public bool InAir => _mode != Mode.Ground;

	/// <summary>True during the hurt animation after a bomb. The player cannot move and cannot collide.</summary>
	public bool IsHurt => _hurt;

	/// <summary>The frames of flashing that are left. Bombs do no damage while the player flashes.</summary>
	public int FlashFramesLeft { get; private set; }

	/// <summary>The progress of the hurt animation, from 0 to 1: the sprite turns once.</summary>
	public double HurtProgress => _hurtTimer / 256.0;

	/// <summary>The position in the pipe, in pixels from the centre, with Y down.</summary>
	public (double X, double Y) Position => (_x / 65536.0, _y / 65536.0);

	/// <param name="outerLane">True to start in the outer lane, as Tails does.</param>
	public HalfPipePlayer(bool outerLane = false)
	{
		OuterLane = outerLane;
		Radius = outerLane ? OuterRadius : InnerRadius;
		PlaceOnPipe();
	}

	/// <param name="left">True while LEFT is held. LEFT wins when both are held, as in the original.</param>
	/// <param name="right">True while RIGHT is held.</param>
	/// <param name="jumpPressed">True on the frame that A, B or C was pressed.</param>
	/// <returns>True when the player started a jump on this frame.</returns>
	public bool Update(bool left, bool right, bool jumpPressed)
	{
		if (FlashFramesLeft > 0 && !IsHurt)
		{
			FlashFramesLeft--;
		}
		if (_mode != Mode.Ground)
		{
			UpdateAir(left, right);
			return false;
		}
		if (IsHurt)
		{
			UpdateHurt();
			MoveToLane();
			MoveAroundPipe();
			return false;
		}
		Move(left, right);
		bool falls = ApplyTraction();
		MoveToLane();
		if (falls)
		{
			// Too slow upside down: fall with the physics of a jump, from the current speed (0)
			_mode = Mode.Fall;
			return false;
		}
		MoveAroundPipe();
		if (jumpPressed)
		{
			int direction = (Angle + 0x80) & 0xFF;
			_xVelocity += Cos(direction) * JumpSpeed >> 8;
			// The original shifts the Y part by 7, so it is twice the X part
			_yVelocity += Sin(direction) * JumpSpeed >> 7;
			_mode = Mode.Jump;
			return true;
		}
		return false;
	}

	/// <summary>A bomb hit. Returns false when the player flashes and takes no damage.</summary>
	public bool Hurt()
	{
		if (FlashFramesLeft > 0 || IsHurt || !OnGround)
		{
			return false;
		}
		// The original clears only the high byte of the inertia, so the player stops but keeps the fraction
		Inertia = (short)(Inertia & 0x00FF);
		_hurt = true;
		_hurtTimer = 0;
		return true;
	}

	private void Move(bool left, bool right)
	{
		int inertia = Inertia;
		if (left)
		{
			inertia = Math.Min(inertia + Acceleration, MaxInertia);
			_slowing = false;
			_slideTimer = 0;
		}
		else if (right)
		{
			inertia = Math.Max(inertia - Acceleration, -MaxInertia);
			_slowing = false;
			_slideTimer = 0;
		}
		else
		{
			if (!_slowing)
			{
				_slowing = true;
				_slideTimer = SlideFrames;
			}
			inertia -= inertia >> 3;
			if (_slideTimer > 0)
			{
				_slideTimer--;
			}
		}
		Inertia = (short)inertia;
	}

	/// <summary>Pulls the player toward the bottom. Returns true when the player falls off the top of the pipe.</summary>
	private bool ApplyTraction()
	{
		if (_slideTimer == 0)
		{
			Inertia = (short)(Inertia + (Cos(Angle) * Traction >> 8));
		}
		// Upside down (angle $84 to $FB) and slower than one angle unit for each frame
		return Angle >= 0x80 && ((Angle + 4) & 0xFF) >= 0x88 && Math.Abs((int)Inertia) < FallInertia;
	}

	private void MoveAroundPipe()
	{
		// The fraction is dropped each frame, so a speed below one unit does not move the player
		int inertia = Inertia;
		Angle = inertia >= 0 ? (Angle + (inertia >> 8)) & 0xFF : (Angle - (-inertia >> 8)) & 0xFF;
		PlaceOnPipe();
	}

	/// <summary>Moves the player one pixel toward the radius of its lane (SSPlayerSwapPositions).</summary>
	private void MoveToLane()
	{
		int target = OuterLane ? OuterRadius : InnerRadius;
		Radius += Math.Sign(target - Radius);
	}

	private void PlaceOnPipe()
	{
		_x = (Cos(Angle) * Radius >> 8) << 16;
		_y = (Sin(Angle) * Radius >> 8) << 16;
	}

	private void UpdateHurt()
	{
		_hurtTimer = (_hurtTimer + HurtTimerStep) & 0xFF;
		if (_hurtTimer == 0)
		{
			_hurt = false;
			FlashFramesLeft = FlashFrames;
		}
	}

	private void UpdateAir(bool left, bool right)
	{
		if (left)
		{
			_xVelocity -= AirControl;
		}
		else if (right)
		{
			_xVelocity += AirControl;
		}
		_x += _xVelocity << 8;
		_y += _yVelocity << 8;
		_yVelocity += Gravity;
		Angle = GetAngle(_x >> 16, _y >> 16);
		// The player lands only on the lower half of the pipe
		int x = _x >> 16;
		int y = _y >> 16;
		if (y > 0 && x * x + y * y >= Radius * Radius)
		{
			_mode = Mode.Ground;
			_xVelocity = 0;
			_yVelocity = 0;
			Inertia = 0;
			_slideTimer = 0;
			_slowing = true;
			PlaceOnPipe();
		}
		MoveToLane();
	}

	/// <summary>The angle of a position, with the octant approximation of SSPlayer_JumpAngle.</summary>
	public static int GetAngle(int x, int y)
	{
		if (x == 0 && y == 0)
		{
			return BottomAngle;
		}
		int ax = Math.Abs(x);
		int ay = Math.Abs(y);
		// The angle in the first quadrant, from 0 (on the X axis) to $40 (on the Y axis)
		int angle = ax >= ay ? ay * 32 / ax : 0x40 - ax * 32 / ay;
		if (x >= 0)
		{
			return (y >= 0 ? angle : 0x100 - angle) & 0xFF;
		}
		return (y >= 0 ? 0x80 - angle : 0x80 + angle) & 0xFF;
	}

	/// <summary>The original converts angles to radians from the bottom, positive to the right.</summary>
	public static double ToRadiansFromBottom(int angle)
	{
		return (sbyte)(byte)(BottomAngle - angle) * Math.PI / 128.0;
	}

	// Sine and cosine scaled so that 1.0 is $100, as CalcSine returns them
	private static readonly int[] SineTable = CreateSineTable();

	private static int[] CreateSineTable()
	{
		var table = new int[256];
		for (int i = 0; i < 256; i++)
		{
			table[i] = (int)Math.Round(Math.Sin(i * Math.PI / 128.0) * 256.0);
		}
		return table;
	}

	public static int Sin(int angle) => SineTable[angle & 0xFF];

	public static int Cos(int angle) => SineTable[(angle + 0x40) & 0xFF];
}
