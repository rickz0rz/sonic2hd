using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZBOSS;

/// <summary>
/// The Metropolis Zone boss, as in Sonic 2 (Obj54 and Obj53). Eggman comes down at $100 from y $380 in a ship with 7
/// balls that circle it. At y $4A0 the ship moves at $100 toward the player, turns at the arena edges (x $2AD0 and
/// $2BD0), and after the second turn goes up to y $470 and back to the middle. There the circle of balls grows, goes
/// flat and becomes small again, and the ship comes down again. The balls hurt. Each hit makes one ball fall: it
/// becomes a clone that hops toward the player until a rolling player breaks it or it hurts a player. While a clone
/// is there, the ship waits at y $420 and the other balls move into a flat ring. When no balls are left, the ship
/// flies down and up from one side of the arena to the other and fires 3 lasers at $400 on each dive. After 8 hits
/// the ship explodes for 180 frames, waits 60 frames and flies away to the right.
/// </summary>
public class MTZBossInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The position that Obj54_Init gives, in original level pixels.</summary>
	public const int InitX = 0x2B50;

	public const int InitY = 0x380;

	private const int LeftX = 0x2AD0;

	private const int RightX = 0x2BD0;

	private const int MiddleX = 0x2B50;

	/// <summary>The x at which the ship dives in the laser attack.</summary>
	private const int DiveLeftX = 0x2AF0;

	private const int DiveRightX = 0x2BB0;

	/// <summary>A laser goes when it leaves this range.</summary>
	private const int LaserMinX = 0x2AB0;

	private const int LaserMaxX = 0x2BF0;

	private const int BottomY = 0x4A0;

	private const int HoverY = 0x470;

	private const int TopY = 0x420;

	/// <summary>The clones hop on this y.</summary>
	private const int FloorY = 0x4AC;

	private const int SmallRadius = 0x27;

	private const int LargeRadius = 0x68;

	/// <summary>The height of the flat ring of balls while a clone is there.</summary>
	private const int FlatRadius = 0x10;

	private const int OrbCount = 7;

	private const int Lasers = 3;

	private const int LaserInterval = 0x1E;

	private const int LaserPauseTicks = 0x10;

	private const int LaserSpeed = 0x400;

	private const int InvulnerableTicks = 0x40;

	private const int DefeatScore = 1000;

	private const int ExplodeTicks = 0xEF;

	/// <summary>The explosions stop when the countdown is below this.</summary>
	private const int QuietTicks = 60;

	/// <summary>The original moves the right camera limit from $2AB0 to $2BF0 at 2 pixels per frame, then deletes the ship.</summary>
	private const int FleeTicks = (0x2BF0 - 0x2AB0) / 2;

	// The mapping frames of Obj54
	private const int ShipFrame = 2;

	private const int OrbFrontFrame = 3;

	private const int OrbFrame = 4;

	private const int OrbBackFrame = 5;

	private const int CloneReadyFrame = 0xB;

	private const int LaserFrame = 0x12;

	private const int LaserShooterFrame = 0x13;

	private const int BurstFrame = 0x14;

	/// <summary>The start angles of the balls.</summary>
	private static readonly int[] OrbAngles = { 0x24, 0x6C, 0xB4, 0xFC, 0x48, 0x90, 0xD8 };

	/// <summary>Ani_obj53 script 2: a falling ball becomes a clone. The last frame stays.</summary>
	private static readonly int[] CloneFrames = { 5, 5, 5, 5, 5, 5, 5, 5, 6, 7, 6, 7, 6, 7, 8, 9, 0xA, 0xB };

	private const int CloneFrameTicks = 4;

	/// <summary>The scripts of Ani_obj53 that the ship uses: 1 is the jet, 3 to 7 are the faces of Eggman.</summary>
	private static readonly byte[][] Scripts =
	{
		new byte[] { 0xF, ShipFrame, 0xFF },
		new byte[] { 1, 0, 1, 0xFF },
		new byte[] { 3, 5, 0xFF },
		new byte[] { 7, 0xC, 0xD, 0xFF },
		new byte[] { 7, 0xE, 0xF, 0xE, 0xF, 0xE, 0xF, 0xE, 0xF, 0xFD, FaceNormal },
		new byte[] { 7, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0xFD, FaceNormal },
		new byte[] { 1, BurstFrame, 0xFF },
		new byte[] { 7, 0x11, 0xFF }
	};

	private const int FaceNormal = 3;

	private const int FaceLaugh = 4;

	private const int FaceHit = 5;

	private const int FaceDefeated = 7;

	private readonly AnimateSpriteScript _face = new AnimateSpriteScript(Scripts) { Animation = FaceNormal, Frame = 0xC };

	private readonly AnimateSpriteScript _jet = new AnimateSpriteScript(Scripts) { Animation = 1 };

	private readonly Orb[] _orbs = new Orb[OrbCount];

	private readonly List<Vector2> _lasers = new List<Vector2>();

	/// <summary>The speed of each laser, in original pixels each frame.</summary>
	private readonly List<int> _laserVelocities = new List<int>();

	private AnimationInstance _animation;

	private Routine _routine;

	/// <summary>Boss_X_pos and Boss_Y_pos, in original level pixels.</summary>
	private double _bossX;

	private double _bossY;

	/// <summary>Boss_X_vel and Boss_Y_vel, in 1/256 pixel per frame.</summary>
	private int _velocityX;

	private int _velocityY;

	/// <summary>x_pos and y_pos: the boss position with the float, in original level pixels.</summary>
	private double _x;

	private double _y;

	private int _sineCount = 0x40;

	/// <summary>Bit 7 of objoff_2B: the ship moves to the right.</summary>
	private bool _movingRight;

	/// <summary>Bit 6 of objoff_2B: the ship has turned once at an edge.</summary>
	private bool _turnedOnce;

	/// <summary>The ship faces right (the art faces left).</summary>
	private bool _facingRight;

	/// <summary>objoff_33 and objoff_39: the width and the height of the circle of balls.</summary>
	private int _radiusX = SmallRadius;

	private int _radiusY = SmallRadius;

	/// <summary>objoff_3A: 0, or $FF while the balls move into the flat ring, or $80 while they move back.</summary>
	private int _flatten;

	/// <summary>objoff_3E: the balls that still circle the ship.</summary>
	private int _orbsLeft = OrbCount;

	/// <summary>objoff_2C: the clones.</summary>
	private int _clones;

	/// <summary>objoff_2E: the step of the laser attack.</summary>
	private int _laserStep;

	/// <summary>objoff_2D: the lasers left in this dive.</summary>
	private int _lasersLeft;

	/// <summary>objoff_2F: the ship stops after a laser.</summary>
	private int _laserPause;

	/// <summary>Boss_Countdown.</summary>
	private int _countdown;

	private int _fleeTicks;

	private enum Routine
	{
		Descend,
		Sweep,
		Return,
		Expand,
		Contract,
		Recoil,
		Recover,
		LaserAttack,
		Exploding,
		Flee
	}

	private enum OrbState
	{
		Orbit,
		Fall,
		Hop,
		Burst,
		Gone
	}

	/// <summary>A ball (Obj53). Positions are in original level pixels.</summary>
	private sealed class Orb
	{
		public OrbState State;

		public double X;

		public double Y;

		/// <summary>objoff_28: the angle around the ship.</summary>
		public int Angle;

		/// <summary>objoff_3B and objoff_3C: the angles for the height in the circle and in the flat ring.</summary>
		public int AngleY;

		public int FlatAngle;

		/// <summary>objoff_30: the depth. A ball in front of the ship has a positive depth.</summary>
		public int Depth;

		public int Frame;

		public int VelocityX;

		public int VelocityY;

		/// <summary>objoff_32: the frames until the clone can be broken. Until then it only hurts.</summary>
		public int HarmTicks;

		/// <summary>collision_property: -1 when the clone hurt a player, -2 when a rolling player broke it.</summary>
		public int Property;

		/// <summary>objoff_2C: the angle of the hop.</summary>
		public int HopAngle;

		public bool FacingRight;

		public int AnimationTicks;

		public int BurstTicks;

		/// <summary>The ball draws in front of the ship (priority 1 or 2 in the original).</summary>
		public bool InFront => State != OrbState.Orbit || Depth >= 0;

		public bool Breakable => HarmTicks < 0;
	}

	/// <summary>The x of the original level origin, in HD units. The level area gives it.</summary>
	[StateVariable]
	public int OriginX { get; set; }

	/// <summary>The y of the original level origin, in HD units. The level area gives it.</summary>
	[StateVariable]
	public int OriginY { get; set; }

	public MTZBossInstance() => DesignBounds = new Rectanglei(-1024, -1024, 2048, 2048);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZBossType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(MTZBossType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(MTZBossType.BossHitSoundResourceKey);
		LockLifetime = true;
		_bossX = (double)(Position.X - OriginX) / Scale;
		_bossY = (double)(Position.Y - OriginY) / Scale;
		_x = _bossX;
		_y = _bossY;
		_velocityX = 0;
		_velocityY = 0x100;
		_routine = Routine.Descend;
		for (int i = 0; i < OrbCount; i++)
		{
			_orbs[i] = new Orb { State = OrbState.Orbit, Angle = OrbAngles[i], AngleY = OrbAngles[i], X = _x, Y = _y, Frame = OrbFrame };
		}
	}

	protected override void OnStop()
	{
		FinishForever();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_routine)
		{
			case Routine.Descend:
				MoveBoss();
				_y = _bossY;
				if (_bossY >= BottomY)
				{
					_routine = Routine.Sweep;
					_velocityY = 0;
					_movingRight = PlayerX() >= _bossX;
					_facingRight = _movingRight;
					_velocityX = _movingRight ? 0x100 : -0x100;
				}
				CheckLaugh();
				break;
			case Routine.Sweep:
				UpdateSweep();
				break;
			case Routine.Return:
				UpdateReturn();
				break;
			case Routine.Expand:
				// The circle grows to $68, then goes flat
				if (_radiusX < LargeRadius)
				{
					_radiusX++;
					_radiusY++;
				}
				else if (--_radiusY == 0)
				{
					_routine = Routine.Contract;
				}
				MoveAndShow();
				break;
			case Routine.Contract:
				// The circle becomes small, then round again
				if (_radiusX >= SmallRadius)
				{
					_radiusX--;
				}
				else if (++_radiusY >= SmallRadius)
				{
					StartDescend();
				}
				MoveAndShow();
				break;
			case Routine.Recoil:
				UpdateRecoil();
				break;
			case Routine.Recover:
				UpdateRecover();
				break;
			case Routine.LaserAttack:
				UpdateLaserAttack();
				break;
			case Routine.Exploding:
				UpdateExploding();
				break;
			case Routine.Flee:
				_velocityX = 0x400;
				_velocityY = -0x40;
				MoveBoss();
				_x = _bossX;
				_y = _bossY;
				if (++_fleeTicks >= FleeTicks)
				{
					LockLifetime = false;
				}
				break;
		}
		_face.Step();
		_jet.Step();
		PositionPrecise = ToLevel(_x, _y);
		UpdateOrbs();
		UpdateLasers();
		UpdateTouchAreas();
	}

	/// <summary>Routine 2: move to one edge and turn, then to the other edge and turn, then go up.</summary>
	private void UpdateSweep()
	{
		MoveBoss();
		bool atEdge = _movingRight ? _bossX >= RightX : _bossX < LeftX;
		if (atEdge)
		{
			_movingRight = !_movingRight;
			_facingRight = _movingRight;
			_velocityX = _movingRight ? 0x100 : -0x100;
			if (_turnedOnce)
			{
				_routine = Routine.Return;
				_velocityY = -0x100;
			}
			_turnedOnce = true;
		}
		MoveAndShow();
	}

	/// <summary>Routine 4: go up to y $470 and back to the middle.</summary>
	private void UpdateReturn()
	{
		MoveBoss();
		if (_bossY < HoverY)
		{
			_velocityY = 0;
		}
		if (_movingRight ? _bossX >= MiddleX : _bossX < MiddleX)
		{
			_velocityX = 0;
		}
		if (_velocityX == 0 && _velocityY == 0)
		{
			_routine = Routine.Expand;
		}
		MoveAndShow();
	}

	private void StartDescend()
	{
		_velocityY = 0x100;
		_routine = Routine.Descend;
		_turnedOnce = false;
	}

	/// <summary>
	/// Routine $A, after a hit: go up to y $420 while the balls move into a flat ring, and wait until no clone is
	/// left.
	/// </summary>
	private void UpdateRecoil()
	{
		if (_radiusY > 0)
		{
			_radiusY--;
		}
		else
		{
			_flatten = 0xFF;
		}
		if (_radiusX >= SmallRadius)
		{
			_radiusX--;
		}
		MoveBoss();
		if (_bossY < TopY)
		{
			_velocityY = 0;
		}
		if (_clones == 0)
		{
			if (_flatten != 0)
			{
				_flatten = 0x80;
			}
			_routine = Routine.Recover;
		}
		MoveAndShow();
	}

	/// <summary>
	/// Routine $C: the balls go back into the circle and the ship comes down again. When no balls are left, the laser
	/// attack starts.
	/// </summary>
	private void UpdateRecover()
	{
		if (_orbsLeft > 0)
		{
			if (_flatten == 0)
			{
				if (_radiusY < SmallRadius)
				{
					_radiusY++;
				}
				else
				{
					StartDescend();
				}
			}
		}
		else
		{
			_velocityY = -0x180;
			_facingRight = _movingRight;
			_velocityX = _movingRight ? 0x100 : -0x100;
			_routine = Routine.LaserAttack;
			_laserStep = 0;
			_turnedOnce = false;
			_laserPause = 0;
		}
		MoveAndShow();
	}

	/// <summary>
	/// Routine $E: fly up to y $420 toward a side, dive to y $4A0 near the edge and fire 3 lasers 30 frames apart
	/// (the ship stops 16 frames after each), then turn and climb.
	/// </summary>
	private void UpdateLaserAttack()
	{
		if (_laserPause > 0)
		{
			_laserPause--;
			CheckLaugh();
			return;
		}
		switch (_laserStep)
		{
			case 0:
				MoveBoss();
				if (_bossY < TopY)
				{
					_velocityY = 0;
				}
				if (_movingRight ? _bossX >= DiveRightX : _bossX < DiveLeftX)
				{
					// Dive and face the middle of the arena
					_laserStep = 1;
					_velocityY = 0x180;
					_lasersLeft = Lasers;
					_countdown = LaserInterval;
					_facingRight = !_movingRight;
				}
				break;
			case 1:
				MoveBoss();
				if (_bossY >= BottomY)
				{
					_velocityY = -0x180;
					_laserStep = 2;
					_movingRight = !_movingRight;
				}
				else if (_movingRight ? _bossX >= RightX : _bossX < LeftX)
				{
					_velocityX = 0;
				}
				FireLaser();
				break;
			case 2:
				MoveBoss();
				if (_bossY < HoverY)
				{
					_velocityX = _movingRight ? 0x100 : -0x100;
				}
				if (_bossY < TopY)
				{
					_velocityY = 0;
					_laserStep = 0;
				}
				FireLaser();
				break;
		}
		MoveAndShow();
	}

	/// <summary>Obj54_FireLaser. The countdown keeps going down after the last laser, like the original.</summary>
	private void FireLaser()
	{
		if (--_countdown != 0 || _lasersLeft == 0)
		{
			return;
		}
		_lasersLeft--;
		_countdown = LaserInterval;
		_laserPause = LaserPauseTicks;
		// The laser starts 4 pixels behind the centre and 7 pixels down, and moves on once
		int velocity = _facingRight ? LaserSpeed / 256 : -LaserSpeed / 256;
		_lasers.Add(new Vector2(_x + (_facingRight ? 4 : -4) + velocity, _y + 7));
		_laserVelocities.Add(velocity);
		PlaySound(MTZBossType.LaserSoundResourceKey);
	}

	/// <summary>Routine $10: explosions for 180 frames, then 60 quiet frames, then Eggman turns to the right.</summary>
	private void UpdateExploding()
	{
		_countdown--;
		if (_countdown < 0)
		{
			_facingRight = true;
			_velocityX = 0;
			_velocityY = 0;
			_routine = Routine.Flee;
			_countdown = -0x12;
			_face.Animation = FaceNormal;
			Fleeing = true;
		}
		else if (_countdown >= QuietTicks)
		{
			if ((Level.Ticks & 7) == 0)
			{
				ExplodeAt(Position + new Vector2i((Level.Random.Next(0x40) - 0x20) * Scale, (Level.Random.Next(0x40) - 0x20) * Scale));
			}
			_face.Animation = FaceDefeated;
		}
		_x = _bossX;
		_y = _bossY;
	}

	/// <summary>Obj54_MoveAndShow: x follows Boss_X_pos, and y floats 4 pixels up and down around Boss_Y_pos.</summary>
	private void MoveAndShow()
	{
		_x = _bossX;
		_y = _bossY + (OriginalMath.Sin(_sineCount) >> 6);
		_sineCount = (_sineCount + 4) & 0xFF;
		CheckLaugh();
	}

	/// <summary>Obj54_AnimateFace: Eggman laughs while a player is hurt.</summary>
	private void CheckLaugh()
	{
		// The original does not check in the first frame after a hit
		if (AnyPlayerHurt() && _face.Animation != FaceLaugh && InvincibilityTimer < InvulnerableTicks - 1)
		{
			_face.Animation = FaceLaugh;
		}
	}

	/// <summary>
	/// Obj53_Main and the clone routines. The balls circle the centre 4 pixels above the ship: 4 angle steps each
	/// frame around it, 8 steps each frame up and down. The depth decides the frame and if the ball is in front.
	/// </summary>
	private void UpdateOrbs()
	{
		foreach (Orb orb in _orbs)
		{
			switch (orb.State)
			{
				case OrbState.Orbit:
					UpdateOrbit(orb);
					break;
				case OrbState.Fall:
					UpdateHarm(orb);
					orb.X += orb.VelocityX / 256.0;
					orb.Y += orb.VelocityY / 256.0;
					// Gravity $38, less $20, and at most $180
					orb.VelocityY = Math.Min(orb.VelocityY + 0x38 - 0x20, 0x180);
					if (orb.Y >= FloorY)
					{
						orb.Y = FloorY;
						orb.HopAngle = 1;
						orb.State = OrbState.Hop;
						FaceLeader(orb);
					}
					AnimateClone(orb);
					break;
				case OrbState.Hop:
					UpdateHop(orb);
					break;
				case OrbState.Burst:
					if (--orb.BurstTicks < 0)
					{
						orb.State = OrbState.Gone;
						_clones--;
						PlaySound(MTZBossType.BurstSoundResourceKey);
					}
					break;
			}
		}
	}

	private void UpdateOrbit(Orb orb)
	{
		double centreX = _x;
		double centreY = _y - 4;
		int radiusY = _flatten != 0 ? FlatRadius : _radiusY;
		int angleY = _flatten != 0 ? orb.FlatAngle : orb.AngleY;
		orb.X = centreX + ((_radiusX * 0x100 * OriginalMath.Sin(orb.Angle)) >> 16);
		orb.Depth = (_radiusX * 0x100 * OriginalMath.Cos(orb.Angle)) >> 16;
		orb.Y = centreY + ((radiusY * 0x100 * OriginalMath.Sin(angleY)) >> 16);
		orb.Angle = (orb.Angle + 4) & 0xFF;
		if (_flatten == 0)
		{
			orb.AngleY = (orb.AngleY + 8) & 0xFF;
		}
		else if (_flatten == 0xFF)
		{
			if (orb.FlatAngle < 0x40)
			{
				orb.FlatAngle += 2;
			}
		}
		else
		{
			orb.FlatAngle -= 2;
			if (orb.FlatAngle < 0)
			{
				// The first ball that is back ends the flat ring for all of them
				orb.FlatAngle = 0;
				_flatten = 0;
			}
		}
		orb.Frame = orb.Depth >= 0xC ? OrbFrontFrame : orb.Depth >= -0xC ? OrbFrame : OrbBackFrame;
	}

	/// <summary>
	/// Obj53_BounceAround: when the clone is ready, it hops 64 pixels high toward the main player, 1 pixel each 2
	/// frames.
	/// </summary>
	private void UpdateHop(Orb orb)
	{
		UpdateHarm(orb);
		if (AnyPlayerHurt() || orb.Property <= -2)
		{
			StartBurst(orb);
			return;
		}
		if (orb.Frame != CloneReadyFrame)
		{
			AnimateClone(orb);
			return;
		}
		int y = FloorY + ((-OriginalMath.Sin(orb.HopAngle)) >> 2);
		if (y >= FloorY)
		{
			orb.Y = FloorY;
			FaceLeader(orb);
			orb.HopAngle = 1;
			return;
		}
		orb.Y = y;
		orb.HopAngle = (orb.HopAngle + 1) & 0xFF;
		if ((orb.HopAngle & 1) != 0)
		{
			orb.X += orb.FacingRight ? 1 : -1;
		}
	}

	/// <summary>A clone only hurts for 61 frames. Then a rolling player can break it.</summary>
	private static void UpdateHarm(Orb orb)
	{
		if (orb.HarmTicks >= 0)
		{
			orb.HarmTicks--;
		}
	}

	/// <summary>Ani_obj53 script 2, and the burst of a clone that a rolling player broke.</summary>
	private void AnimateClone(Orb orb)
	{
		if (orb.Property <= -2)
		{
			StartBurst(orb);
			return;
		}
		orb.Frame = CloneFrames[Math.Min(orb.AnimationTicks / CloneFrameTicks, CloneFrames.Length - 1)];
		orb.AnimationTicks++;
	}

	private static void StartBurst(Orb orb)
	{
		orb.State = OrbState.Burst;
		orb.Frame = BurstFrame;
		orb.BurstTicks = 2;
	}

	private void FaceLeader(Orb orb)
	{
		ICharacter player = Level.Player.Protagonist;
		orb.FacingRight = player != null && (player.Position.X - OriginX) / (double)Scale - orb.X >= 0;
	}

	private bool AnyPlayerHurt() => Level.Player.Protagonist?.IsHurt == true || Level.Player.Sidekick?.IsHurt == true;

	/// <summary>
	/// The first ball that circles the ship falls off. It jumps at -$400 and moves at $80 away from the main player,
	/// or toward the middle near an edge of the arena.
	/// </summary>
	private void BreakOrb()
	{
		foreach (Orb orb in _orbs)
		{
			if (orb.State != OrbState.Orbit)
			{
				continue;
			}
			_clones++;
			orb.State = OrbState.Fall;
			orb.HarmTicks = 60;
			orb.Property = 0;
			orb.AnimationTicks = 0;
			orb.VelocityY = -0x400;
			ICharacter player = Level.Player.Protagonist;
			double playerX = player != null ? (player.Position.X - OriginX) / (double)Scale : orb.X;
			int velocityX = playerX - orb.X >= 0 ? -0x80 : 0x80;
			if (orb.X < DiveLeftX)
			{
				velocityX = 0x80;
			}
			if (orb.X >= DiveRightX)
			{
				velocityX = -0x80;
			}
			orb.VelocityX = velocityX;
			orb.FacingRight = velocityX >= 0;
			return;
		}
	}

	/// <summary>Obj54_Laser_Main: the lasers fly straight on until they leave the arena.</summary>
	private void UpdateLasers()
	{
		for (int i = _lasers.Count - 1; i >= 0; i--)
		{
			Vector2 laser = _lasers[i] + new Vector2(_laserVelocities[i], 0.0);
			if (laser.X < LaserMinX || laser.X >= LaserMaxX)
			{
				_lasers.RemoveAt(i);
				_laserVelocities.RemoveAt(i);
				continue;
			}
			_lasers[i] = laser;
		}
	}

	/// <summary>
	/// The touch areas. The ship (collision $F, 24 by 24 pixels from the centre) can be hit, but not while it flashes
	/// after a hit or after the defeat. The balls (collision $87, 6 by 6 pixels) hurt. A clone (collision $DA, 12 by
	/// 12 pixels) can be broken after 61 frames. The lasers (collision $99, 32 by 8 pixels) hurt.
	/// </summary>
	private void UpdateTouchAreas()
	{
		var rectangles = new List<CollisionRectangle>();
		if (Health > 0 && InvincibilityTimer <= 0)
		{
			rectangles.Add(Box(0, 0, 0, 0x18, 0x18));
		}
		for (int i = 0; i < OrbCount; i++)
		{
			Orb orb = _orbs[i];
			if (orb.State == OrbState.Burst || orb.State == OrbState.Gone)
			{
				continue;
			}
			int size = orb.State != OrbState.Orbit && orb.Breakable ? 0xC : 6;
			rectangles.Add(Box(1 + i, orb.X - _x, orb.Y - _y, size, size));
		}
		for (int i = 0; i < _lasers.Count; i++)
		{
			rectangles.Add(Box(1 + OrbCount + i, _lasers[i].X - _x, _lasers[i].Y - _y, 0x20, 8));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	/// <summary>A touch area from its centre (relative to the ship) and half size in original pixels.</summary>
	private CollisionRectangle Box(int id, double x, double y, int halfWidth, int halfHeight) =>
		new CollisionRectangle(this, id, (int)((x - halfWidth) * Scale), (int)((y - halfHeight) * Scale), halfWidth * 2 * Scale, halfHeight * 2 * Scale);

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		if (e.Id == 0)
		{
			if (Health > 0 && InvincibilityTimer <= 0)
			{
				base.OnCollision(e);
			}
			return;
		}
		var character = (ICharacter)e.ActiveObject;
		int orbIndex = e.Id - 1;
		if (orbIndex < OrbCount)
		{
			Orb orb = _orbs[orbIndex];
			if (orb.State == OrbState.Burst || orb.State == OrbState.Gone)
			{
				return;
			}
			if (orb.State != OrbState.Orbit && orb.Breakable)
			{
				if (character.IsDeadly)
				{
					// The player bounces back and the clone bursts
					if (character.IsAirborne)
					{
						character.Velocity *= -1.0;
					}
					else
					{
						character.GroundVelocity *= -1.0;
					}
					orb.Property = -2;
					return;
				}
				orb.Property = -1;
			}
		}
		if (character.CanBeHurt)
		{
			character.Hurt(Math.Sign(character.Position.X - Position.X));
		}
	}

	/// <summary>
	/// Obj54_AnimateFace after a hit: one ball falls, Eggman makes the face of a hit, and the ship stops. While balls
	/// are left, the ship goes up at $180 and waits for the clones.
	/// </summary>
	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (Health <= 0)
		{
			return;
		}
		InvincibilityTimer = InvulnerableTicks;
		_face.Animation = FaceHit;
		BreakOrb();
		if (_orbsLeft > 0)
		{
			_routine = Routine.Recoil;
			_velocityY = -0x180;
			_orbsLeft--;
		}
		_velocityX = 0;
	}

	protected override void Defeat()
	{
		Level.Player.GainScore(DefeatScore);
		_countdown = ExplodeTicks;
		_routine = Routine.Exploding;
		Defeated = true;
	}

	private void MoveBoss()
	{
		_bossX += _velocityX / 256.0;
		_bossY += _velocityY / 256.0;
	}

	private double PlayerX()
	{
		ICharacter player = Level.Player.Protagonist;
		return player != null ? (double)(player.Position.X - OriginX) / Scale : _bossX;
	}

	private void PlaySound(string key) => Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(key));

	private Vector2 ToLevel(double x, double y) => new Vector2(OriginX + x * Scale, OriginY + y * Scale);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// Back to front: the balls behind the ship, the laser shooter, the lasers, the ship, then the balls in front
		foreach (Orb orb in _orbs)
		{
			if (!orb.InFront && orb.State != OrbState.Gone)
			{
				DrawPart(objectRenderer, orb.Frame, orb.X, orb.Y, orb.FacingRight);
			}
		}
		if (_routine != Routine.Exploding && _routine != Routine.Flee)
		{
			DrawPart(objectRenderer, LaserShooterFrame, _x, _y, _facingRight);
		}
		foreach (Vector2 laser in _lasers)
		{
			DrawPart(objectRenderer, LaserFrame, laser.X, laser.Y, false);
		}
		objectRenderer.AdditiveColour = IsInvincibleFlashing ? FlashAdditiveColour : Colours.Black;
		// The original draws the main sprite in front of the child sprites: the jet, then the face, then the ship
		DrawPart(objectRenderer, _jet.Frame, _x, _y, _facingRight);
		DrawPart(objectRenderer, _face.Frame, _x, _y, _facingRight);
		DrawPart(objectRenderer, ShipFrame, _x, _y, _facingRight);
		objectRenderer.AdditiveColour = Colours.Black;
		foreach (Orb orb in _orbs)
		{
			if (orb.InFront && orb.State != OrbState.Gone)
			{
				DrawPart(objectRenderer, orb.Frame, orb.X, orb.Y, orb.FacingRight);
			}
		}
	}

	/// <summary>Draws a frame at a position in original level pixels.</summary>
	private void DrawPart(IObjectRenderer objectRenderer, int frame, double x, double y, bool flipX)
	{
		_animation.Index = frame;
		objectRenderer.RenderOriginal(_animation, new Vector2((x - _x) * Scale, (y - _y) * Scale), flipX);
	}
}
