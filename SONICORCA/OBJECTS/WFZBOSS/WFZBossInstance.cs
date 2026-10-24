using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SONICORCA.OBJECTS.WFZBOSS.LASERWALL;
using SONICORCA.OBJECTS.WFZBOSS.PLATFORM;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZBOSS;

/// <summary>
/// The Wing Fortress Zone boss, as in Sonic 2 (ObjC5). A laser case hangs from the ceiling of the boss room. When the
/// main player comes within $20 pixels of it, two laser walls close the room, the case comes down $18 pixels, and
/// Robotnik watches from his stand on the right. Then the case repeats this sequence:
/// <list type="bullet">
/// <item>It moves to the side of the player at $100 for $70 frames, and turns at $60 pixels from its start.</item>
/// <item>It opens, and the laser shooter comes down out of it. Only now can a character hit the case.</item>
/// <item>The laser charges with a flicker, waits $40 frames, and shoots down in 5 frames.</item>
/// <item>The case follows the player at $80 for $80 frames with the laser. Then the laser goes, the shooter goes back
/// up and the case closes.</item>
/// </list>
/// From the first move, a releaser under the case lets out a spiked platform every $80 frames, three in all. After
/// eight hits, the case explodes for $EF frames, the platforms and the laser walls go, and Robotnik goes down out of
/// the room on his stand.
/// </summary>
/// <remarks>
/// The original uses positions in the act. This object keeps them in original pixels and draws them at
/// (<see cref="OriginX"/> + x * 4, <see cref="OriginY"/> + y * 4). The object position is the position of the case.
/// </remarks>
public class WFZBossInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The case moves up to $60 pixels to each side of its start.</summary>
	private const int Range = 0x60;

	/// <summary>The player starts the boss within $20 pixels of the case.</summary>
	private const int StartDistance = 0x20;

	private const int WallX = 0x88;

	private const int WallY = 0x60;

	private const int DownSpeed = 0x40;

	private const int StartTicks = 0x5A;

	private const int DownTicks = 0x60;

	private const int MoveSpeed = 0x100;

	private const int MoveTicks = 0x70;

	private const int ShooterTicks = 0xE;

	private const int ChargeTicks = 0x40;

	private const int LaserMoveSpeed = 0x80;

	private const int LaserMoveTicks = 0x80;

	private const int DefeatTicks = 0xEF;

	private const int DefeatScore = 1000;

	private const int ExplosionRadius = 0x20 * Scale;

	/// <summary>Robotnik stands at this position in the act, his stand $26 pixels lower.</summary>
	private static readonly Vector2i RobotnikStart = new Vector2i(0x2C60, 0x4E6);

	private const int StandOffset = 0x26;

	private const int RobotnikFleeTicks = 0xC0;

	private const int ReleaserOffset = 8;

	private const int ReleaserSpeed = 0x40;

	private const int ReleaserDownTicks = 0x40;

	private const int ReleaserFirstTicks = 0x10;

	private const int ReleaserTicks = 0x80;

	private const int PlatformCount = 3;

	// The mapping frames of ObjC5
	private const int FrameShooter = 4;
	private const int FrameReleaser = 5;
	private const int FrameLaserCharge = 0xD;
	private const int FrameLaserFirst = 0xF;

	/// <summary>The opening of the case (Ani_objC5 0) and its closing (Ani_objC5 1), with the ticks of each frame.</summary>
	private static readonly int[] OpenFrames = { 0, 1, 2, 3, 3, 3, 3 };

	private const int OpenFrameTicks = 6;

	private static readonly int[] CloseFrames = { 3, 2, 1, 0, 0 };

	private const int CloseFrameTicks = 4;

	/// <summary>Robotnik (Ani_objC5_objC6 1): frames 6 and 7, 6 frames each.</summary>
	private const int RobotnikFrameTicks = 6;

	private const int CaseCollisionId = 0;

	private const int LaserCollisionId = 1;

	private readonly List<WFZBossPlatformInstance> _platforms = new List<WFZBossPlatformInstance>();

	private readonly List<WFZBossLaserWallInstance> _walls = new List<WFZBossLaserWallInstance>();

	private readonly bool[] _platformSlots = new bool[PlatformCount];

	private AnimationInstance _animation;

	private AnimationInstance _robotnikAnimation;

	private AnimationInstance _standAnimation;

	private State _state;

	private int _timer;

	/// <summary>The position of the case in original pixels, and its speed in 1/256 pixels per frame.</summary>
	private Vector2 _position;

	private int _velocityX;

	private int _velocityY;

	private int _left;

	private int _right;

	private int _caseFrame;

	private int _animationTicks;

	/// <summary>The case can be hit (collision_flags 6) while the laser shooter is down.</summary>
	private bool _vulnerable;

	/// <summary>The first move has started: the releaser comes down (status.npc.misc).</summary>
	private bool _moved;

	private bool _shooterVisible;

	private int _shooterOffset;

	private Releaser _releaser;

	private Laser _laser;

	private bool _robotnikVisible;

	private Vector2 _robotnik;

	private int _robotnikTicks;

	private int _robotnikFleeTicks;

	private enum State
	{
		WaitStart,
		WaitDown,
		Down,
		Move,
		Open,
		ShooterDown,
		WaitLaser,
		WaitShot,
		MoveWithLaser,
		ShooterUp,
		Close,
		Defeated,
		Finished
	}

	private sealed class Releaser
	{
		public Vector2 Position;

		public int State;

		public int Timer;

		public int Slot;
	}

	private sealed class Laser
	{
		public Vector2 Position;

		public int State;

		public int Timer;

		public int Duration;

		public int Countdown = 0xC;

		public int Length;

		public bool Hidden;

		public bool Visible;

		public int Frame = FrameLaserCharge;
	}

	/// <summary>The left of the act in which the original coordinates are, in HD pixels.</summary>
	public int OriginX { get; set; }

	/// <summary>The top of the act in which the original coordinates are, in HD pixels.</summary>
	public int OriginY { get; set; }

	public WFZBossInstance() => DesignBounds = new Rectanglei(-1024, -512, 2048, 1024);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBossType.AnimationGroupResourceKey));
		_robotnikAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBossType.RobotnikAnimationGroupResourceKey));
		_standAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZBossType.StandAnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(WFZBossType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(WFZBossType.BossHitSoundResourceKey);
		LockLifetime = true;
		Health = 8;
		// ObjC5_CaseBoundary
		_position = new Vector2((PositionPrecise.X - OriginX) / Scale, (PositionPrecise.Y - OriginY) / Scale);
		_left = (int)_position.X - Range;
		_right = (int)_position.X + Range;
		_state = State.WaitStart;
		UpdatePosition();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateCase();
		UpdateReleaser();
		UpdateLaser();
		UpdateRobotnik();
		UpdatePosition();
		UpdateTouchRectangles();
	}

	private void UpdateCase()
	{
		switch (_state)
		{
			case State.WaitStart:
				if (System.Math.Abs(PlayerX() - _position.X) < StartDistance)
				{
					StartBoss();
				}
				break;
			case State.WaitDown:
				if (--_timer < 0)
				{
					_state = State.Down;
					_timer = DownTicks;
				}
				break;
			case State.Down:
				if (--_timer == 0)
				{
					_velocityY = 0;
					StartMove();
				}
				else
				{
					MoveCase();
				}
				break;
			case State.Move:
				// ObjC5_CaseBoundaryChk: the case turns at the ends of its range
				if (--_timer < 0)
				{
					_state = State.Open;
					_animationTicks = 0;
					break;
				}
				if (_velocityX >= 0 ? _position.X >= _right : _position.X < _left)
				{
					_velocityX = -_velocityX;
				}
				MoveCase();
				break;
			case State.Open:
				if (Animate(OpenFrames, OpenFrameTicks))
				{
					// ObjC5_CaseLSLoad
					_state = State.ShooterDown;
					_timer = ShooterTicks;
				}
				break;
			case State.ShooterDown:
				if (--_timer == 0)
				{
					// ObjC5_CaseAddCollision
					_state = State.WaitLaser;
					_timer = ChargeTicks;
					_vulnerable = true;
				}
				else
				{
					_shooterOffset++;
				}
				break;
			case State.WaitLaser:
				if (--_timer < 0)
				{
					_state = State.WaitShot;
					_laser = new Laser { Position = _position + new Vector2(0, 0x10 - 3) };
				}
				break;
			case State.WaitShot:
				if (_laser != null && _laser.State == 4)
				{
					_state = State.MoveWithLaser;
					_timer = LaserMoveTicks;
					_velocityX = PlayerX() > _position.X ? LaserMoveSpeed : -LaserMoveSpeed;
				}
				break;
			case State.MoveWithLaser:
				if (--_timer < 0)
				{
					// ObjC5_CaseStopLaserDelete
					_state = State.ShooterUp;
					_timer = ShooterTicks;
					_vulnerable = false;
					_laser = null;
					break;
				}
				if (_velocityX >= 0 ? _position.X >= _right : _position.X < _left)
				{
					_velocityX = 0;
				}
				MoveCase();
				break;
			case State.ShooterUp:
				if (--_timer == 0)
				{
					_state = State.Close;
					_animationTicks = 0;
				}
				else
				{
					_shooterOffset--;
				}
				break;
			case State.Close:
				if (Animate(CloseFrames, CloseFrameTicks))
				{
					// ObjC5_CaseStartOver
					StartMove();
				}
				break;
			case State.Defeated:
				// ObjC5_CaseDefeated
				if (--_timer < 0)
				{
					// ObjC5_End: the area plays the level music and lets the camera go down again
					_state = State.Finished;
					Defeated = true;
					Fleeing = true;
					LockLifetime = false;
					FinishForever();
					return;
				}
				UpdateExplosions(ExplosionRadius);
				break;
		}
	}

	/// <summary>ObjC5_CaseStart: the laser walls close the room, and the case starts to come down.</summary>
	private void StartBoss()
	{
		_state = State.WaitDown;
		_velocityY = DownSpeed;
		_timer = StartTicks;
		foreach (int side in new[] { -1, 1 })
		{
			Vector2 position = _position + new Vector2(side * WallX, WallY);
			if (AddChild(WFZBossType.LaserWallResourceKey, position) is WFZBossLaserWallInstance wall)
			{
				_walls.Add(wall);
			}
		}
		_shooterVisible = true;
		_releaser = new Releaser { Position = _position + new Vector2(0, ReleaserOffset) };
		_robotnikVisible = true;
		_robotnik = new Vector2(RobotnikStart.X, RobotnikStart.Y);
	}

	/// <summary>ObjC5_CaseXSpeed: the case moves to the side of the player.</summary>
	private void StartMove()
	{
		_state = State.Move;
		_velocityX = PlayerX() > _position.X ? MoveSpeed : -MoveSpeed;
		_moved = true;
		_timer = MoveTicks;
	}

	private void MoveCase() => _position += new Vector2(_velocityX / 256.0, _velocityY / 256.0);

	/// <summary>Shows the frames of an animation of the case. Returns true when the animation is over.</summary>
	private bool Animate(int[] frames, int ticks)
	{
		int index = _animationTicks / ticks;
		_animationTicks++;
		if (index >= frames.Length)
		{
			return true;
		}
		_caseFrame = frames[index];
		return false;
	}

	/// <summary>
	/// ObjC5_PlatformReleaser: the releaser comes down $10 pixels when the case first moves. Then it lets out a
	/// platform every $80 frames, in three slots that it never frees. When the boss is defeated, it explodes.
	/// </summary>
	private void UpdateReleaser()
	{
		Releaser releaser = _releaser;
		if (releaser == null)
		{
			return;
		}
		switch (releaser.State)
		{
			case 0:
				if (_moved)
				{
					releaser.State = 1;
					releaser.Timer = ReleaserDownTicks;
				}
				break;
			case 1:
				if (--releaser.Timer == 0)
				{
					releaser.State = 2;
					releaser.Timer = ReleaserFirstTicks;
				}
				else
				{
					releaser.Position.Y += ReleaserSpeed / 256.0;
				}
				break;
			case 2:
				if (--releaser.Timer != 0)
				{
					break;
				}
				releaser.Timer = ReleaserTicks;
				releaser.Slot = (releaser.Slot + 1) % PlatformCount;
				if (!_platformSlots[releaser.Slot])
				{
					_platformSlots[releaser.Slot] = true;
					if (AddChild(WFZBossType.PlatformResourceKey, releaser.Position) is WFZBossPlatformInstance platform)
					{
						_platforms.Add(platform);
					}
				}
				break;
			case 3:
				// ObjC5_PlatformReleaserDelete: explosions while the case explodes
				if ((Level.Ticks & 7) == 0)
				{
					Vector2i offset = new Vector2i(Level.Random.Next(-0x20, 0x20), Level.Random.Next(-0x20, 0x20)) * Scale;
					ExplodeAt(ToHd(releaser.Position) + offset);
				}
				break;
		}
	}

	/// <summary>
	/// ObjC5_Laser: the laser charges with a flicker that slows down (ObjC5_LaserFlash), waits $40 frames, and then
	/// shoots down $10 pixels each frame for 4 frames. It follows the case and shows in every second frame.
	/// </summary>
	private void UpdateLaser()
	{
		Laser laser = _laser;
		if (laser == null)
		{
			return;
		}
		switch (laser.State)
		{
			case 0:
				laser.Hidden = true;
				if (--laser.Duration >= 0)
				{
					break;
				}
				if (laser.Duration + 2 >= 0)
				{
					laser.Hidden = false;
					break;
				}
				if (--laser.Countdown == 0)
				{
					laser.State = 1;
					laser.Timer = ChargeTicks;
					break;
				}
				laser.Duration = laser.Countdown;
				break;
			case 1:
				if (--laser.Timer < 0)
				{
					laser.State = 2;
					laser.Position.Y += 0x10;
				}
				break;
			case 2:
				if (laser.Length + 1 >= 5)
				{
					// ObjC5_LaseShotOut: the case can move now
					laser.State = 4;
					break;
				}
				laser.Length++;
				laser.Position.Y += 0x10;
				laser.Frame = FrameLaserFirst + laser.Length - 1;
				break;
			case 4:
				laser.Position.X = _position.X;
				break;
		}
		// The original shows the laser only when the flag was clear, then turns the flag over
		laser.Visible = !laser.Hidden;
		laser.Hidden = !laser.Hidden;
	}

	/// <summary>ObjC5_Robotnik: Robotnik watches. When the boss is defeated, he goes down on his stand for $C0 frames.</summary>
	private void UpdateRobotnik()
	{
		if (!_robotnikVisible)
		{
			return;
		}
		_robotnikTicks++;
		if (_state != State.Defeated && _state != State.Finished)
		{
			return;
		}
		if (--_robotnikFleeTicks < 0)
		{
			_robotnikVisible = false;
			return;
		}
		_robotnik.Y++;
	}

	protected override void Defeat()
	{
		// ObjC5_NoHitPointsLeft
		base.Defeat();
		Level.Player.GainScore(DefeatScore);
		_state = State.Defeated;
		_timer = DefeatTicks;
		_vulnerable = false;
		_shooterVisible = false;
		_laser = null;
		_robotnikFleeTicks = RobotnikFleeTicks;
		if (_releaser != null)
		{
			_releaser.State = 3;
		}
		foreach (WFZBossPlatformInstance platform in _platforms)
		{
			platform.Explode();
		}
		_platforms.Clear();
		foreach (WFZBossLaserWallInstance wall in _walls)
		{
			wall.Vanish();
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		if (e.Id == CaseCollisionId)
		{
			// The original turns off the collision of the case while it flashes after a hit
			if (_vulnerable && InvincibilityTimer <= 0)
			{
				base.OnCollision(e);
			}
			return;
		}
		var character = (ICharacter)e.ActiveObject;
		if (character.CanBeHurt)
		{
			character.Hurt(System.Math.Sign(character.Position.X - Position.X));
		}
	}

	/// <summary>
	/// The touch sizes of the original: the case $10 by $10 pixels to each side, and the laser $10 pixels to each
	/// side and $20 to $50 pixels up and down (collision sizes $2B to $2E).
	/// </summary>
	private void UpdateTouchRectangles()
	{
		var rectangles = new List<CollisionRectangle>();
		if (_vulnerable)
		{
			rectangles.Add(Box(CaseCollisionId, _position, 0x10, 0x10));
		}
		if (_laser != null && _laser.Length > 0)
		{
			rectangles.Add(Box(LaserCollisionId, _laser.Position, 0x10, 0x10 + _laser.Length * 0x10));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	private CollisionRectangle Box(int id, Vector2 centre, int halfWidth, int halfHeight)
	{
		Vector2i offset = ToHd(centre) - Position;
		return new CollisionRectangle(this, id, offset.X - halfWidth * Scale, offset.Y - halfHeight * Scale, halfWidth * 2 * Scale, halfHeight * 2 * Scale);
	}

	private ActiveObject AddChild(string key, Vector2 position)
	{
		return Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(key), Level.Map.Layers.IndexOf(Layer), ToHd(position)));
	}

	/// <summary>The x of the main character, in original pixels.</summary>
	private double PlayerX()
	{
		ICharacter protagonist = Level.Player.Protagonist;
		return protagonist == null ? _position.X : (protagonist.PositionPrecise.X - OriginX) / Scale;
	}

	private Vector2i ToHd(Vector2 position) => new Vector2i(OriginX + (int)System.Math.Floor(position.X) * Scale, OriginY + (int)System.Math.Floor(position.Y) * Scale);

	private void UpdatePosition() => Position = ToHd(_position);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		if (_robotnikVisible)
		{
			Vector2 robotnik = (Vector2)(ToHd(_robotnik) - Position);
			_standAnimation.Index = 0;
			objectRenderer.Render(_standAnimation, robotnik + new Vector2(0, StandOffset * Scale));
			_robotnikAnimation.Index = 6 + (_robotnikTicks / RobotnikFrameTicks) % 2;
			objectRenderer.Render(_robotnikAnimation, robotnik);
		}
		if (_releaser != null && _state != State.Finished)
		{
			DrawFrame(objectRenderer, FrameReleaser, _releaser.Position);
		}
		if (_laser != null && _laser.Visible)
		{
			DrawFrame(objectRenderer, _laser.Frame, _laser.Position);
		}
		if (_shooterVisible)
		{
			DrawFrame(objectRenderer, FrameShooter, _position + new Vector2(0, _shooterOffset));
		}
		if (_state != State.Finished)
		{
			Colour additiveColour = objectRenderer.AdditiveColour;
			if (IsInvincibleFlashing)
			{
				objectRenderer.AdditiveColour = FlashAdditiveColour;
			}
			DrawFrame(objectRenderer, _caseFrame, _position);
			objectRenderer.AdditiveColour = additiveColour;
		}
	}

	private void DrawFrame(IObjectRenderer objectRenderer, int frame, Vector2 position)
	{
		_animation.Index = frame;
		objectRenderer.Render(_animation, (Vector2)(ToHd(position) - Position));
	}
}
