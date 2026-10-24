using System;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// The movement of the player in the half-pipe. The player runs forward with the track. <see cref="Angle"/> is the
/// position around the pipe, in radians from the bottom (negative to the left), and <see cref="Height"/> is the
/// distance above the surface during a jump. As in Sonic 2, a player who runs off one edge of the pipe leaps across
/// to the other edge, and a bomb stops the player for a moment. All values change once per tick, so the movement
/// is deterministic.
/// </summary>
public sealed class HalfPipePlayer
{
	/// <summary>The angle of the top edge on each side. A player who runs past it leaps to the other edge.</summary>
	public const double MaxAngle = 88.0 * Math.PI / 180.0;

	/// <summary>The duration of a leap from one edge of the pipe to the other.</summary>
	private const int EdgeLeapTicks = 40;

	/// <summary>The highest point of an edge leap above the surface, in track units. The pipe radius is 4.</summary>
	private const double EdgeLeapHeight = 3.0;

	/// <summary>The ticks at the start of <see cref="HurtTicks"/> during which the player cannot move.</summary>
	private const int StunTicks = 30;

	private const double Acceleration = 0.006;

	private const double MaxAngularSpeed = 0.05;

	/// <summary>The fraction of the angular speed that stays each tick when there is no input.</summary>
	private const double Friction = 0.88;

	/// <summary>Pulls the player back to the bottom of the pipe, more strongly higher up the side.</summary>
	private const double SlopeGravity = 0.003;

	private const double JumpSpeed = 0.2;

	private const double JumpGravity = 0.011;

	/// <summary>The ticks after a bomb hit during which bombs do not hurt the player.</summary>
	public const int HurtTicks = 90;

	public double Angle { get; private set; }

	public double AngularSpeed { get; private set; }

	public double Height { get; private set; }

	public bool IsJumping => Height > 0.0 || _verticalSpeed > 0.0 || IsLeaping;

	/// <summary>True while the player leaps from one edge of the pipe to the other.</summary>
	public bool IsLeaping => _leapTick > 0;

	public int HurtTicksLeft { get; private set; }

	public bool IsHurt => HurtTicksLeft > 0;

	private double _verticalSpeed;

	private int _leapTick;

	private double _leapStartAngle;

	/// <param name="direction">The horizontal input, from -1 (left) to 1 (right).</param>
	/// <param name="jumpPressed">True on the tick that a jump button was pressed.</param>
	/// <returns>True when the player started a jump on this tick.</returns>
	public bool Update(double direction, bool jumpPressed)
	{
		if (HurtTicksLeft > 0)
		{
			HurtTicksLeft--;
		}
		if (IsLeaping)
		{
			UpdateEdgeLeap();
			return false;
		}
		if (HurtTicksLeft > HurtTicks - StunTicks)
		{
			// Stopped by a bomb: no input, and no sideways movement
			direction = 0.0;
			jumpPressed = false;
			AngularSpeed = 0.0;
		}

		if (Math.Abs(direction) > 0.1)
		{
			AngularSpeed = Math.Clamp(AngularSpeed + direction * Acceleration, -MaxAngularSpeed, MaxAngularSpeed);
		}
		else
		{
			AngularSpeed *= Friction;
		}
		if (!IsJumping)
		{
			AngularSpeed -= Math.Sin(Angle) * SlopeGravity;
		}
		Angle += AngularSpeed;
		if (Math.Abs(Angle) > MaxAngle)
		{
			if (!IsJumping && Math.Sign(AngularSpeed) == Math.Sign(Angle))
			{
				StartEdgeLeap();
				return false;
			}
			Angle = Math.Sign(Angle) * MaxAngle;
			AngularSpeed = 0.0;
		}

		bool jumped = false;
		if (jumpPressed && !IsJumping)
		{
			_verticalSpeed = JumpSpeed;
			jumped = true;
		}
		if (IsJumping)
		{
			Height += _verticalSpeed;
			_verticalSpeed -= JumpGravity;
			if (Height <= 0.0)
			{
				Height = 0.0;
				_verticalSpeed = 0.0;
			}
		}
		return jumped;
	}

	public void Hurt()
	{
		HurtTicksLeft = HurtTicks;
	}

	private void StartEdgeLeap()
	{
		_leapStartAngle = Math.Sign(Angle) * MaxAngle;
		_leapTick = 1;
		AngularSpeed = 0.0;
		UpdateEdgeLeap();
	}

	/// <summary>Moves the player from one edge to the other on an arc over the pipe.</summary>
	private void UpdateEdgeLeap()
	{
		double progress = _leapTick / (double)EdgeLeapTicks;
		Angle = _leapStartAngle * (1.0 - 2.0 * progress);
		Height = EdgeLeapHeight * 4.0 * progress * (1.0 - progress);
		if (++_leapTick > EdgeLeapTicks)
		{
			_leapTick = 0;
			Angle = -_leapStartAngle;
			Height = 0.0;
			// Move a little away from the edge, so that the player does not leap back at once
			AngularSpeed = Math.Sign(_leapStartAngle) * 0.01;
		}
	}
}
