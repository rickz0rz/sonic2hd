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

namespace SONICORCA.OBJECTS.CNZBOSS;

/// <summary>
/// The Casino Night Zone boss, as in Sonic 2 (Obj51). Eggman flies in from the right and patrols between x $28C0 and
/// $29C0 at $180. Two generators below the ship make an electric arc: every 64 frames the arc changes between a short
/// arc under the ship and a wide arc between the generators. When the main player is under the ship on the floor,
/// Eggman stops and drops an electric ball (3 balls at most for each pass). The ball breaks in two sparks on the
/// floor. When the main player is under the ship in the air, Eggman swoops down with the wide arc. After 8 hits the
/// ship explodes for 180 frames, the generators fall off, and Eggman flies away to the right.
/// </summary>
/// <remarks>
/// The boss works in the coordinates of the original act: an original position (x, y) is the HD position
/// (OriginX + x * 4, OriginY + y * 4). The level events spawn the boss 90 frames after the camera locks.
/// </remarks>
public class CNZBossInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The position that the init routine of Obj51 sets, in original pixels of the act.</summary>
	public const int InitX = 0x2A46;

	public const int InitY = 0x654;

	private const int LeftTurnX = 0x28C0;

	private const int RightTurnX = 0x29C0;

	private const int PatrolSpeed = 0x180;

	private const int SwoopSpeed = 0x180;

	/// <summary>The swoop turns back up below this y, and ends above the start height.</summary>
	private const int SwoopBottomY = 0x680;

	private const int SwoopTopY = 0x654;

	/// <summary>The main player is on the floor of the arena below this y, and in the air above it.</summary>
	private const int PlayerFloorY = 0x6B0;

	private const int PlayerAirY = 0x67C;

	private const int MaxBallsPerPass = 3;

	/// <summary>Eggman holds the ball for $50 frames. After the release, he waits $14 frames.</summary>
	private const int BallHoldTicks = 0x50;

	private const int BallWaitTicks = 0x14;

	/// <summary>After a ball, Eggman does not look for the player for $40 frames.</summary>
	private const int BallCooldownTicks = 0x40;

	private const int HitInvulnerableTicks = 48;

	private const int DefeatScore = 1000;

	private const int ExplosionTicks = 0xB3;

	/// <summary>The generators fall off when the explosion countdown is at these values.</summary>
	private const int LeftGeneratorFallTick = 0x78;

	private const int RightGeneratorFallTick = 60;

	private const int GeneratorGravity = 0x38;

	private const int GeneratorFloorY = 0x6F0;

	private const int FleeSpeedX = 0x400;

	private const int FleeSpeedY = -0x40;

	/// <summary>The original deletes the ship when the camera limit is at $2B20 ($240 pixels at 2 pixels a frame).</summary>
	private const int FleeTicks = 0x240 / 2;

	private const int BallGravity = 0x38;

	private const int BallHangMaxOffset = 0x2E;

	private const int BallRadius = 8;

	private const int SparkSpeedX = 0x100;

	private const int SparkSpeedY = -0x300;

	private const int SparkDeleteY = 0x705;

	/// <summary>Touch sizes: the ship is $F ($18 x $18), a ball or a spark $18 (4 x 4).</summary>
	private const int BodyHalfSize = 0x18;

	private const int BallHalfSize = 4;

	private const int BodyCollisionId = 0;

	private const int ArcCollisionId = 1;

	private const int BallCollisionId = 2;

	// The mapping frames of Obj51
	private const int FrameNone = 0;

	private const int FrameShortArc = 0xC;

	private const int FrameWideArc = 0xF;

	private const int FrameBall = 0x12;

	private const int FrameSpark = 0x13;

	// The animations of Ani_obj51
	private const int AnimShip = 0;

	private const int AnimRightGenerator = 1;

	private const int AnimLeftGenerator = 2;

	private const int AnimFace = 3;

	private const int AnimShortArc = 4;

	private const int AnimFaceLaugh = 5;

	private const int AnimFaceHit = 6;

	private const int AnimNoArc = 8;

	private const int AnimWideArc = 9;

	/// <summary>The animation index of a generator that is off (0) or that makes the arc (2).</summary>
	private const int GeneratorOff = 0;

	private const int GeneratorOn = 2;

	/// <summary>
	/// Ani_obj51: the speed (the number of frames for each mapping frame, minus 1), then the mapping frames and the
	/// commands $FF (restart), $FD (change to the animation in the next byte) and $FC (go to the frame in the next byte).
	/// Animation 7 (the sparks) is not here, because the sparks are simple.
	/// </summary>
	private static readonly byte[][] Scripts =
	{
		new byte[] { 0x0F, 0x01, 0xFF },
		new byte[] { 0x0F, 0x04, 0xFF, 0x05, 0xFC, 0x02 },
		new byte[] { 0x0F, 0x02, 0xFF, 0x03, 0xFC, 0x02 },
		new byte[] { 0x07, 0x06, 0x07, 0xFF },
		new byte[] { 0x01, 0x0C, 0x0D, 0x0E, 0xFF },
		new byte[] { 0x07, 0x08, 0x09, 0x08, 0x09, 0x08, 0x09, 0x08, 0x09, 0xFD, 0x03 },
		new byte[] { 0x07, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0xFD, 0x03 },
		new byte[] { 0x03, 0x13, 0x14, 0xFF },
		new byte[] { 0x01, 0x00, 0xFF },
		new byte[] { 0x01, 0x0F, 0x10, 0x11, 0xFF },
	};

	private readonly List<Ball> _balls = new List<Ball>();

	private readonly BossAnimation _arc = new BossAnimation(FrameNone);

	private readonly BossAnimation _rightGenerator = new BossAnimation(5);

	private readonly BossAnimation _ship = new BossAnimation(1);

	private readonly BossAnimation _face = new BossAnimation(6);

	private readonly BossAnimation _leftGenerator = new BossAnimation(2);

	private AnimationInstance _animation;

	private Routine _routine;

	/// <summary>Boss_X_pos and Boss_Y_pos: the position without the hover, in original pixels.</summary>
	private double _bossX;

	private double _bossY;

	/// <summary>Boss_X_vel and Boss_Y_vel, in 1/256 pixels a frame.</summary>
	private int _velocityX;

	private int _velocityY;

	/// <summary>The position with the hover (x_pos and y_pos), in original pixels.</summary>
	private int _x;

	private int _y;

	private int _sineCount;

	private bool _facingRight;

	private bool _patrolRight;

	/// <summary>Boss_Countdown: the arc timer, the ball timer, then the defeat timer.</summary>
	private int _countdown;

	private int _ballCount;

	private int _cooldown;

	private bool _swoopRising;

	/// <summary>Boss_CollisionRoutine: 0 for no arc, 1 for the short arc, 2 for the wide arc.</summary>
	private int _arcMode;

	private bool _generatorsLoose;

	private Generator _leftGeneratorPart;

	private Generator _rightGeneratorPart;

	private int _fleeTicks;

	private enum Routine
	{
		Patrol,
		DropBall,
		Swoop,
		Explode,
		Sink,
		Flee
	}

	private enum BallState
	{
		Hanging,
		Falling,
		Spark
	}

	private sealed class Ball
	{
		public BallState State;

		public int X;

		public int Y;

		public int VelocityX;

		public int VelocityY;

		public int Offset;

		public int Ticks;
	}

	/// <summary>A generator that fell off the ship after the defeat.</summary>
	private struct Generator
	{
		public int X;

		public double Y;

		public int VelocityY;
	}

	/// <summary>The left of the act, in HD units. An original x maps to OriginX + x * 4.</summary>
	[StateVariable]
	public int OriginX { get; set; }

	/// <summary>The top of the act, in HD units. An original y maps to OriginY + y * 4.</summary>
	[StateVariable]
	public int OriginY { get; set; }

	public CNZBossInstance() => DesignBounds = new Rectanglei(-48 * Scale, -48 * Scale, 96 * Scale, 112 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CNZBossType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(CNZBossType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(CNZBossType.BossHitSoundResourceKey);
		LockLifetime = true;
		Health = 8;
		_bossX = InitX;
		_bossY = InitY;
		_x = InitX;
		_y = InitY;
		_velocityX = -PatrolSpeed;
		_countdown = 1;
		// The first AnimateBoss starts all the animations
		_arc.Restart(AnimNoArc);
		_rightGenerator.Restart(AnimRightGenerator);
		_ship.Restart(AnimShip);
		_face.Restart(AnimFace);
		_leftGenerator.Restart(AnimLeftGenerator);
		UpdatePosition();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_routine)
		{
			case Routine.Patrol:
				Patrol();
				UpdateCommon();
				break;
			case Routine.DropBall:
				DropBall();
				UpdateCommon();
				break;
			case Routine.Swoop:
				Swoop();
				UpdateCommon();
				break;
			case Routine.Explode:
				Explode();
				break;
			case Routine.Sink:
				Sink();
				break;
			case Routine.Flee:
				Flee();
				break;
		}
		UpdateBalls();
		UpdatePosition();
		UpdateCollisionRectangles();
	}

	/// <summary>Boss_MoveObject.</summary>
	private void Move()
	{
		_bossX += _velocityX / 256.0;
		_bossY += _velocityY / 256.0;
	}

	/// <summary>The ship flies from side to side. It looks for the main player when it is not resting after a ball.</summary>
	private void Patrol()
	{
		if (!_patrolRight && (int)_bossX <= LeftTurnX)
		{
			Turn(LeftTurnX, PatrolSpeed, true);
		}
		else if (_patrolRight && (int)_bossX >= RightTurnX)
		{
			Turn(RightTurnX, -PatrolSpeed, false);
		}
		Move();
		if (_cooldown > 0)
		{
			_cooldown--;
			UpdateArc();
			return;
		}
		ICharacter player = Level.Player.Protagonist;
		if (player == null || !IsUnderShip(player))
		{
			UpdateArc();
			return;
		}
		int playerY = OriginalY(player.Position.Y);
		if (playerY >= PlayerFloorY)
		{
			if (_ballCount >= MaxBallsPerPass)
			{
				UpdateArc();
				return;
			}
			// Stop and drop a ball
			_ballCount++;
			_routine = Routine.DropBall;
			_arc.Restart(AnimNoArc);
			_rightGenerator.SetIndex(GeneratorOff);
			_leftGenerator.SetIndex(GeneratorOff);
			_arcMode = 0;
			_balls.Add(new Ball { State = BallState.Hanging, X = _x, Y = _y });
			_countdown = BallHoldTicks;
		}
		else if (playerY >= PlayerAirY)
		{
			// Swoop down with the wide arc
			_arc.Frame = FrameWideArc;
			_arcMode = 2;
			_rightGenerator.SetIndex(GeneratorOn);
			_leftGenerator.SetIndex(GeneratorOn);
			_arc.Restart(AnimWideArc);
			_routine = Routine.Swoop;
			_velocityX = 0;
			_velocityY = SwoopSpeed;
			_swoopRising = false;
		}
		else
		{
			UpdateArc();
		}
	}

	private void Turn(int x, int velocityX, bool right)
	{
		_bossX = x;
		_velocityY = 0;
		_velocityX = velocityX;
		_patrolRight = right;
		_facingRight = right;
		_ballCount = 0;
	}

	/// <summary>The main player is within 16 pixels of the ship on the x axis.</summary>
	private bool IsUnderShip(ICharacter player)
	{
		int dx = OriginalX(player.Position.X) - _x;
		return dx >= -0x10 && dx < 0x10;
	}

	/// <summary>Every 64 frames the arc changes: the wide arc with the generators on, or the short arc.</summary>
	private void UpdateArc()
	{
		_countdown++;
		if ((_countdown & 0x3F) != 0)
		{
			return;
		}
		if ((_countdown & 0x40) != 0)
		{
			_arc.Frame = FrameWideArc;
			_arcMode = 2;
			_rightGenerator.SetIndex(GeneratorOn);
			_leftGenerator.SetIndex(GeneratorOn);
			_arc.Restart(AnimWideArc);
		}
		else
		{
			_arc.Frame = FrameShortArc;
			_arcMode = 1;
			_rightGenerator.SetIndex(GeneratorOff);
			_leftGenerator.SetIndex(GeneratorOff);
			_arc.Restart(AnimShortArc);
		}
	}

	/// <summary>The ship holds still. At 0 the ball falls, and $14 frames later the ship moves again.</summary>
	private void DropBall()
	{
		_arcMode = 0;
		_countdown--;
		if (_countdown == 0)
		{
			_rightGenerator.SetIndex(GeneratorOn);
			_leftGenerator.SetIndex(GeneratorOn);
		}
		else if (_countdown <= -BallWaitTicks)
		{
			_rightGenerator.SetIndex(GeneratorOff);
			_leftGenerator.SetIndex(GeneratorOff);
			_routine = Routine.Patrol;
			_countdown = -1;
			_cooldown = BallCooldownTicks;
		}
	}

	/// <summary>The ship moves down to y $680 with the wide arc, then up to y $654 with the short arc.</summary>
	private void Swoop()
	{
		Move();
		if (!_swoopRising)
		{
			if (_y >= SwoopBottomY)
			{
				_velocityX = 0;
				_velocityY = -SwoopSpeed;
				_swoopRising = true;
				_arcMode = 1;
				_rightGenerator.SetIndex(GeneratorOff);
				_leftGenerator.SetIndex(GeneratorOff);
				_arc.Restart(AnimShortArc);
			}
		}
		else if (_y < SwoopTopY)
		{
			_routine = Routine.Patrol;
			_velocityY = 0;
			_velocityX = _facingRight ? PatrolSpeed : -PatrolSpeed;
			if (_facingRight)
			{
				_arc.Frame = FrameShortArc;
			}
		}
	}

	/// <summary>loc_31C08: the hover, the generators, the face and the animations.</summary>
	private void UpdateCommon()
	{
		Hover();
		UpdateGenerators();
		UpdateFace();
		AnimateParts();
	}

	/// <summary>The ship moves up and down by 4 pixels around its position.</summary>
	private void Hover()
	{
		_y = (int)Math.Floor(_bossY) + (OriginalMath.Sin(_sineCount) >> 6);
		_x = (int)Math.Floor(_bossX);
		_sineCount = (_sineCount + 2) & 0xFF;
	}

	/// <summary>Eggman laughs while a player is hurt, unless he shows the face of a hit.</summary>
	private void UpdateFace()
	{
		ICharacter main = Level.Player.Protagonist;
		ICharacter sidekick = Level.Player.Sidekick;
		bool hurt = (main != null && main.IsHurt) || (sidekick != null && sidekick.IsHurt);
		if (hurt && _face.Animation != AnimFaceHit)
		{
			_face.Set(AnimFaceLaugh);
		}
	}

	/// <summary>AnimateBoss: the arc animates only while it shows a frame.</summary>
	private void AnimateParts()
	{
		if (_arc.Frame != FrameNone)
		{
			_arc.Step(Scripts);
		}
		_rightGenerator.Step(Scripts);
		_ship.Step(Scripts);
		_face.Step(Scripts);
		_leftGenerator.Step(Scripts);
	}

	/// <summary>
	/// The generators move with the ship. After the defeat they fall off: the left generator when the countdown is
	/// at $78, the right one at 60. They slide away from the ship and stop falling at y $6F0.
	/// </summary>
	private void UpdateGenerators()
	{
		if (!_generatorsLoose)
		{
			_leftGeneratorPart = new Generator { X = _x, Y = _y };
			_rightGeneratorPart = new Generator { X = _x, Y = _y };
			return;
		}
		if (_countdown > LeftGeneratorFallTick)
		{
			return;
		}
		// The left generator moves left, or right when the ship faces right (the bug fix of s2disasm)
		FallGenerator(ref _leftGeneratorPart, _facingRight ? 1 : -1);
		if (_countdown > RightGeneratorFallTick)
		{
			return;
		}
		FallGenerator(ref _rightGeneratorPart, _facingRight ? -1 : 1);
	}

	private static void FallGenerator(ref Generator generator, int moveX)
	{
		generator.X += moveX;
		generator.Y += generator.VelocityY / 256.0;
		generator.VelocityY += GeneratorGravity;
		if (generator.Y >= GeneratorFloorY)
		{
			generator.VelocityY = 0;
		}
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (Health > 0)
		{
			InvincibilityTimer = HitInvulnerableTicks;
			_face.Set(AnimFaceHit);
		}
	}

	protected override void Defeat()
	{
		Defeated = true;
		Level.Player.GainScore(DefeatScore);
		_countdown = ExplosionTicks;
		_routine = Routine.Explode;
		_balls.Clear();
	}

	/// <summary>The ship explodes for $B4 frames. Then it faces right, and the right wall of the arena opens.</summary>
	private void Explode()
	{
		_generatorsLoose = true;
		_countdown--;
		if (_countdown >= 0)
		{
			_arcMode = 0;
			_arc.Frame = FrameNone;
			UpdateExplosions(0x20 * Scale);
		}
		else
		{
			_facingRight = true;
			_velocityX = 0;
			_velocityY = 0;
			_routine = Routine.Sink;
			_face.Set(AnimFace);
			_arc.Restart(AnimNoArc);
			_countdown = -0x12;
		}
		_y = (int)Math.Floor(_bossY);
		_x = (int)Math.Floor(_bossX);
		UpdateGenerators();
	}

	/// <summary>The ship sinks a little, rises, and then flies away.</summary>
	private void Sink()
	{
		_countdown++;
		if (_countdown == 0)
		{
			_velocityY = 0;
		}
		else if (_countdown < 0)
		{
			_velocityY += 0x18;
		}
		else if (_countdown < 0x18)
		{
			_velocityY -= 8;
		}
		else if (_countdown == 0x18)
		{
			// The original plays the level music here
			_velocityY = 0;
		}
		else if (_countdown >= 0x20)
		{
			_routine = Routine.Flee;
		}
		Move();
		_sineCount = (_sineCount + 2) & 0xFF;
		_y = (int)Math.Floor(_bossY);
		_x = (int)Math.Floor(_bossX);
		UpdateGenerators();
	}

	/// <summary>Eggman flies away to the right and up. The ship goes when the camera limit is at $2B20.</summary>
	private void Flee()
	{
		Fleeing = true;
		_velocityX = FleeSpeedX;
		_velocityY = FleeSpeedY;
		if (++_fleeTicks > FleeTicks)
		{
			LockLifetime = false;
			Finish();
			return;
		}
		Move();
		_sineCount = (_sineCount + 2) & 0xFF;
		_y = (int)Math.Floor(_bossY);
		_x = (int)Math.Floor(_bossX);
		UpdateGenerators();
		AnimateParts();
	}

	/// <summary>
	/// A ball comes out of the ship by 1 pixel a frame, to $2E pixels below it. When the countdown is at 0, it falls
	/// with gravity $38. On the floor it breaks in two sparks that jump to the sides and go below y $705.
	/// </summary>
	private void UpdateBalls()
	{
		for (int i = _balls.Count - 1; i >= 0; i--)
		{
			Ball ball = _balls[i];
			switch (ball.State)
			{
				case BallState.Hanging:
					ball.X = _x;
					ball.Y = _y + ball.Offset;
					ball.Offset = Math.Min(ball.Offset + 1, BallHangMaxOffset);
					if (_countdown == 0)
					{
						ball.State = BallState.Falling;
						ball.VelocityX = 0;
						ball.VelocityY = 0;
					}
					break;
				case BallState.Falling:
				{
					MoveBall(ball);
					int? floor = LandscapeFloor.GetDistance(Level, ToHd(ball.X, ball.Y + BallRadius));
					if (floor < 0)
					{
						ball.Y += floor.Value;
						BreakBall(ball);
					}
					break;
				}
				case BallState.Spark:
					MoveBall(ball);
					ball.Ticks++;
					if (ball.Y >= SparkDeleteY)
					{
						_balls.RemoveAt(i);
					}
					break;
			}
		}
	}

	/// <summary>The ball moves by whole pixels: the original drops the fraction of the position each frame.</summary>
	private static void MoveBall(Ball ball)
	{
		ball.X += ball.VelocityX >> 8;
		ball.Y += ball.VelocityY >> 8;
		ball.VelocityY += BallGravity;
	}

	private void BreakBall(Ball ball)
	{
		Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(CNZBossType.BallBreakSoundResourceKey));
		ball.State = BallState.Spark;
		ball.VelocityX = -SparkSpeedX;
		ball.VelocityY = SparkSpeedY;
		ball.Ticks = 0;
		_balls.Add(new Ball { State = BallState.Spark, X = ball.X, Y = ball.Y, VelocityX = SparkSpeedX, VelocityY = SparkSpeedY });
	}

	private int OriginalX(int hdX) => (int)Math.Floor((hdX - OriginX) / (double)Scale);

	private int OriginalY(int hdY) => (int)Math.Floor((hdY - OriginY) / (double)Scale);

	private Vector2i ToHd(int x, int y) => new Vector2i(OriginX + x * Scale, OriginY + y * Scale);

	private void UpdatePosition() => Position = ToHd(_x, _y);

	/// <summary>
	/// The ship (touch size $F) and the arc. The arc hurts in a box under the ship: 32 x 16 pixels for the short arc
	/// and 64 x 16 pixels for the wide arc (BossCollision_CNZ). While the ship flashes after a hit, the original
	/// clears its collision, so neither the ship nor the arc touches. The balls and the sparks always hurt.
	/// </summary>
	private void UpdateCollisionRectangles()
	{
		var rectangles = new List<CollisionRectangle>();
		if (_routine < Routine.Explode && InvincibilityTimer <= 0)
		{
			rectangles.Add(Box(BodyCollisionId, 0, 0, BodyHalfSize, BodyHalfSize));
			if (_arcMode == 1)
			{
				rectangles.Add(Box(ArcCollisionId, 0, 0x28, 0x10, 8));
			}
			else if (_arcMode == 2)
			{
				rectangles.Add(Box(ArcCollisionId, 4, 0x20, 0x20, 8));
			}
		}
		for (int i = 0; i < _balls.Count; i++)
		{
			rectangles.Add(Box(BallCollisionId + i, _balls[i].X - _x, _balls[i].Y - _y, BallHalfSize, BallHalfSize));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	/// <summary>A box around an offset from the ship, in original pixels.</summary>
	private CollisionRectangle Box(int id, int x, int y, int halfWidth, int halfHeight) =>
		new CollisionRectangle(this, id, (x - halfWidth) * Scale, (y - halfHeight) * Scale, halfWidth * 2 * Scale, halfHeight * 2 * Scale);

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id == BodyCollisionId)
		{
			base.OnCollision(e);
			return;
		}
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (ICharacter)e.ActiveObject;
		if (!character.CanBeHurt)
		{
			return;
		}
		int sourceX = Position.X;
		int ball = e.Id - BallCollisionId;
		if (ball >= 0 && ball < _balls.Count)
		{
			sourceX = ToHd(_balls[ball].X, _balls[ball].Y).X;
		}
		character.Hurt(Math.Sign(character.Position.X - sourceX));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The balls and the sparks have priority 7, behind the ship
		foreach (Ball ball in _balls)
		{
			_animation.Index = ball.State == BallState.Spark ? FrameSpark + ball.Ticks / 4 % 2 : FrameBall;
			objectRenderer.Render(_animation, (Vector2)(ToHd(ball.X, ball.Y) - Position));
		}
		if (IsInvincibleFlashing)
		{
			objectRenderer.AdditiveColour = FlashAdditiveColour;
		}
		// The original draws the arc first (in front), then the right generator, the ship, the face and the left
		// generator. The art faces left. A flip mirrors each part around its own position.
		DrawPart(objectRenderer, _leftGenerator.Frame, _leftGeneratorPart.X, (int)_leftGeneratorPart.Y);
		DrawPart(objectRenderer, _face.Frame, _x, _y);
		DrawPart(objectRenderer, _ship.Frame, _x, _y);
		DrawPart(objectRenderer, _rightGenerator.Frame, _rightGeneratorPart.X, (int)_rightGeneratorPart.Y);
		objectRenderer.AdditiveColour = Colours.Black;
		if (_arc.Frame != FrameNone)
		{
			DrawPart(objectRenderer, _arc.Frame, _x, _y);
		}
	}

	private void DrawPart(IObjectRenderer objectRenderer, int frame, int x, int y)
	{
		if (frame == FrameNone)
		{
			return;
		}
		_animation.Index = frame;
		Vector2 offset = (Vector2)(ToHd(x, y) - Position);
		if (!_facingRight)
		{
			objectRenderer.Render(_animation, offset);
			return;
		}
		using (objectRenderer.BeginMatixState())
		{
			// The scale mirrors the destination too, so mirror the offset first
			objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			objectRenderer.Render(_animation, new Vector2(-offset.X, offset.Y));
		}
	}

	/// <summary>
	/// One entry of Boss_AnimationArray, as AnimateBoss runs it: the animation (two ids: the one that runs and the
	/// one that is asked for), the index in the script and the timer.
	/// </summary>
	private sealed class BossAnimation
	{
		private int _running;

		private int _requested;

		private int _index;

		private int _timer;

		public BossAnimation(int frame) => Frame = frame;

		/// <summary>The mapping frame that the part shows.</summary>
		public int Frame { get; set; }

		/// <summary>The animation that is asked for.</summary>
		public int Animation => _requested;

		/// <summary>Asks for an animation. It starts again only when it is not the animation that runs.</summary>
		public void Set(int animation) => _requested = animation;

		/// <summary>Writes the animation byte: it always starts again (unless the animation is 0).</summary>
		public void Restart(int animation)
		{
			_running = 0;
			_requested = animation;
		}

		/// <summary>Writes the frame byte: the next step shows the frame at this index of the script.</summary>
		public void SetIndex(int index)
		{
			_index = index;
			_timer = 0;
		}

		public void Step(byte[][] scripts)
		{
			int index = _index;
			int timer = _timer;
			if (_running != _requested)
			{
				index = 0;
				timer = 0;
			}
			int animation = _requested;
			int previous = _requested;
			timer--;
			if (timer < 0)
			{
				byte[] script = scripts[animation];
				timer = script[0];
				int frame = script[1 + index];
				switch (frame)
				{
					case < 0x80:
						Frame = frame;
						index++;
						break;
					case 0xFF:
						Frame = script[1];
						index = 1;
						break;
					case 0xFD:
						// Change to the next animation. It starts on the next step.
						_requested = script[2 + index];
						break;
					case 0xFC:
						index = script[2 + index];
						Frame = script[1 + index];
						index++;
						break;
					default:
						return;
				}
			}
			_index = index;
			_timer = timer;
			_running = previous;
		}
	}
}
