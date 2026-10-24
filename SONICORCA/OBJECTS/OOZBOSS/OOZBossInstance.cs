using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZBOSS;

/// <summary>
/// The Oil Ocean Zone boss, as in Sonic 2 (Obj55). Eggman rises out of the oil in a submarine, hovers and laughs, and
/// dives again. Only then can a character hit him. After the dive, a laser shooter on a twisting stalk rises out of
/// the oil and fires three lasers at different heights. A laser at the lowest height makes an energy wave on the
/// floor when it reaches the edge of the floor. If Eggman was hit in the submarine, a spiked ball on a chain swings
/// out of the oil in a circle before the laser shooter comes up. After eight hits, the submarine explodes and sinks.
/// </summary>
/// <remarks>
/// The original uses absolute coordinates in the arena of act 2. This object keeps them in original pixels and draws
/// them at (<see cref="OriginX"/> + x * 4, <see cref="OriginY"/> + y * 4). The object position is the position of
/// the main sprite of the original: the submarine, the laser shooter or the spiked ball.
/// </remarks>
public class OOZBossInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The centre of the arena. The submarine and the laser shooter come up here.</summary>
	private const int CentreX = 0x2940;

	/// <summary>A character to the left of this x is on the left side of the arena.</summary>
	private const int SideX = 0x293A;

	private const int SubmergedY = 0x2D0;

	private const int SurfacedY = 0x290;

	/// <summary>Before the dive, the submarine bobs up to this height.</summary>
	private const int BobY = 0x28C;

	private const int ShooterLowY = 0x2B0;

	private const int ShooterHighY = 0x240;

	/// <summary>The heights at which the laser shooter fires. A laser at $25F hits the floor and makes a wave.</summary>
	private static readonly int[] LaserTargets = { 0x238, 0x230, 0x240, 0x25F };

	private const int ChainLeftX = 0x28C0;

	private const int ChainRightX = 0x29C0;

	private const int ChainY = 0x2A0;

	/// <summary>The radius of the circle of the spiked ball, in pixels.</summary>
	private const int ChainRadius = 0x68;

	private const int LinkCount = 8;

	private const int SurfaceSpeed = -0x80;

	private const int ShooterSpeed = 0x80;

	private const int WaitTicks = 0xA8;

	private const int FirstShotTicks = 0x80;

	private const int NextShotTicks = 0x28;

	private const int ShotCount = 3;

	private const int FireFrameTicks = 8;

	private const int DefeatTicks = 0xB3;

	/// <summary>In the last ticks of the defeat countdown, the explosions stop and Eggman looks defeated.</summary>
	private const int DefeatedFaceTicks = 0x1E;

	/// <summary>The original moves the right camera limit from $28C0 to $2A20, 2 pixels each frame, while the boss sinks.</summary>
	private const int ReleaseCameraTicks = (0x2A20 - 0x28C0) / 2;

	private const int DefeatScore = 1000;

	/// <summary>Explosions appear up to 32 pixels from the boss.</summary>
	private const int ExplosionRadius = 32 * Scale;

	private const int LaserSpeed = 0x400;

	private const int LaserMuzzle = 0x20;

	private const int LaserMinX = 0x2870;

	private const int LaserMaxX = 0x2A10;

	private const int FloorY = 0x250;

	private const int WaveCount = 7;

	private const int WaveDelay = 5;

	private const int WaveSpacing = 0x10;

	// The mapping frames of Obj55
	private const int FrameSubmarine = 1;
	private const int FrameSpikeLeft = 2;
	private const int FrameSpikeUpLeft = 3;
	private const int FrameSpikeDownLeft = 4;
	private const int FrameShooter = 5;
	private const int FrameShooterFiring = 6;
	private const int FrameLink = 7;
	private const int FrameEggman = 8;
	private const int FrameEggmanLaughing = 9;
	private const int FrameEggmanHurt = 0xA;
	private const int FrameEggmanDefeated = 0xB;
	private const int FrameLaser = 0xC;
	private const int FrameSpikeUp = 0x15;

	/// <summary>The energy wave (Ani_obj55 animation 2): two ticks for each frame, then the wave goes.</summary>
	private static readonly int[] WaveFrames = { 0xD, 0x11, 0xE, 0x12, 0xF, 0x13, 0x10, 0x14, 0x14, 0x10, 0x13, 0xF, 0x12, 0xE, 0x11, 0xD };

	private const int WaveFrameTicks = 2;

	// Collision rectangle ids. The lasers and the waves use the ids after these.
	private const int MainCollisionId = 0;
	private const int FirstShotCollisionId = 1;

	private readonly List<Laser> _lasers = new List<Laser>();

	private readonly List<Wave> _waves = new List<Wave>();

	private readonly Vector2i[] _links = new Vector2i[LinkCount];

	/// <summary>The x of each collision rectangle, in HD pixels, by id. A hurt character moves away from it.</summary>
	private readonly Dictionary<int, int> _collisionX = new Dictionary<int, int>();

	private AnimationInstance _animation;

	private Phase _phase;

	private int _routine;

	/// <summary>Boss_X_pos and Boss_Y_pos: the base position, in 1/256 pixels.</summary>
	private int _bossX;

	private int _bossY;

	/// <summary>Boss_Y_vel, in 1/256 pixels for each frame.</summary>
	private int _velocityY;

	/// <summary>The position of the main sprite, in pixels.</summary>
	private int _x;

	private int _y;

	private bool _flipX;

	private int _mainFrame;

	private int _countdown;

	/// <summary>boss_sine_count: a byte angle for the hover, the twist of the stalk and the swing of the chain.</summary>
	private int _sineCount;

	/// <summary>Eggman was hit in this submarine phase. Then the spiked ball comes before the laser shooter.</summary>
	private bool _hitThisPhase;

	/// <summary>The submarine has bobbed up and now dives.</summary>
	private bool _diving;

	private int _shotsLeft;

	/// <summary>One bit for each laser height that the laser shooter used in this phase.</summary>
	private int _usedTargets;

	private int _fireFrameTicks;

	private EggmanAnimation _eggmanAnimation;

	private int _eggmanTicks;

	/// <summary>The left of the arena area in which the original coordinates are, in HD pixels.</summary>
	public int OriginX { get; set; }

	/// <summary>The top of the arena area in which the original coordinates are, in HD pixels.</summary>
	public int OriginY { get; set; }

	private int BossX => _bossX >> 8;

	private int BossY => _bossY >> 8;

	private enum Phase
	{
		Submarine,
		LaserShooter,
		SpikeChain,
		Defeated,
	}

	private enum EggmanAnimation
	{
		Normal,
		Laughing,
		Hurt,
	}

	private sealed class Laser
	{
		public int X;

		public int Y;

		public int VelocityX;

		public bool Started;

		public bool Gone;
	}

	private sealed class Wave
	{
		public int X;

		public int Direction;

		public int Delay = WaveDelay;

		public int Count;

		public int Ticks;

		public bool Gone;
	}

	public OOZBossInstance() => DesignBounds = new Rectanglei(-1024, -1024, 2048, 2048);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(OOZBossType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(OOZBossType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(OOZBossType.BossHitSoundResourceKey);
		LockLifetime = true;
		Health = 8;
		_phase = Phase.Submarine;
		_routine = 0;
		_x = CentreX;
		_y = SubmergedY;
		UpdatePosition();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_phase)
		{
			case Phase.Submarine:
				UpdateSubmarine();
				break;
			case Phase.LaserShooter:
				UpdateLaserShooter();
				break;
			case Phase.SpikeChain:
				UpdateSpikeChain();
				break;
			case Phase.Defeated:
				UpdateDefeated();
				break;
		}
		UpdatePosition();
		UpdateLasers();
		UpdateWaves();
		UpdateTouchRectangles();
	}

	/// <summary>
	/// Obj55_Main: the submarine rises to $290 at 1/2 pixel each frame and hovers for $A8 frames while Eggman laughs.
	/// Then it bobs up to $28C and dives at 1/2 pixel each frame. A hit ends the hover at once.
	/// </summary>
	private void UpdateSubmarine()
	{
		switch (_routine)
		{
			case 0:
				// Obj55_Main_Init
				SetBossX(CentreX);
				// The art faces right. Eggman turns to a character on the left side.
				_flipX = PlayerX() < SideX;
				SetBossY(SubmergedY);
				_x = CentreX;
				_y = SubmergedY;
				_mainFrame = FrameEggman;
				_velocityY = SurfaceSpeed;
				_sineCount = 0;
				_hitThisPhase = false;
				_diving = false;
				SetEggmanAnimation(EggmanAnimation.Normal);
				_routine = 1;
				return;
			case 1:
				// Obj55_Main_Surface
				MoveBoss();
				_x = BossX;
				Hover();
				if (BossY < SurfacedY)
				{
					SetBossY(SurfacedY);
					_routine = 2;
					_countdown = WaitTicks;
					if (!_hitThisPhase)
					{
						SetEggmanAnimation(EggmanAnimation.Laughing);
					}
				}
				break;
			case 2:
				// Obj55_Main_Wait
				if (!_hitThisPhase)
				{
					Hover();
					if (--_countdown >= 0)
					{
						break;
					}
					SetEggmanAnimation(EggmanAnimation.Normal);
				}
				_routine = 3;
				_velocityY = -0x40;
				break;
			case 3:
				// Obj55_Main_Dive
				MoveBoss();
				_y = BossY;
				_x = BossX;
				if (!_diving)
				{
					if (BossY < BobY)
					{
						SetBossY(BobY);
						_velocityY = 0x80;
						_diving = true;
					}
				}
				else if (BossY >= SubmergedY)
				{
					SetBossY(SubmergedY);
					_routine = 0;
					_phase = _hitThisPhase ? Phase.SpikeChain : Phase.LaserShooter;
				}
				break;
		}
		AnimateEggman();
	}

	/// <summary>Obj55_HoverPos: the submarine moves up and down by 2 pixels around the base position.</summary>
	private void Hover()
	{
		_y = BossY + (OriginalMath.Cos(_sineCount) >> 7);
		_sineCount = (_sineCount + 4) & 0xFF;
	}

	/// <summary>
	/// Obj55_LaserShooter: the laser shooter rises from $2B0 to $240. It moves to three of the four laser heights in a
	/// random order and fires at each one. Then it goes back into the oil. It always faces the main character.
	/// </summary>
	private void UpdateLaserShooter()
	{
		switch (_routine)
		{
			case 0:
				// Obj55_LaserShooter_Init
				SetBossX(CentreX);
				_flipX = PlayerX() >= SideX;
				SetBossY(ShooterLowY);
				_mainFrame = FrameShooter;
				_velocityY = -ShooterSpeed;
				_usedTargets = 0;
				_routine = 1;
				PlaceShooter();
				return;
			case 1:
				// Obj55_LaserShooter_Rise
				MoveBoss();
				if (BossY < ShooterHighY)
				{
					SetBossY(ShooterHighY);
					_velocityY = 0;
					_routine = 2;
					_countdown = FirstShotTicks;
					_shotsLeft = ShotCount;
				}
				break;
			case 2:
				// Obj55_LaserShooter_ChooseTarget
				UpdateFireFrame();
				if (--_countdown != 0)
				{
					break;
				}
				if (--_shotsLeft < 0)
				{
					_velocityY = ShooterSpeed;
					_routine = 4;
					break;
				}
				// Take the next height after a random one that the laser shooter has not used yet
				int target = Level.Random.Next(4);
				do
				{
					target = (target + 1) & 3;
				}
				while ((_usedTargets & (1 << target)) != 0);
				_usedTargets |= 1 << target;
				_countdown = LaserTargets[target];
				_routine = 3;
				_velocityY = _countdown - BossY >= 0 ? ShooterSpeed : -ShooterSpeed;
				break;
			case 3:
				// Obj55_LaserShooter_Aim: the countdown holds the target height
				MoveBoss();
				bool reached = _velocityY < 0 ? _countdown >= BossY : _countdown < BossY;
				if (reached)
				{
					Fire();
				}
				break;
			case 4:
				// Obj55_LaserShooter_Lower
				UpdateFireFrame();
				MoveBoss();
				if (BossY >= ShooterLowY)
				{
					SetBossY(ShooterLowY);
					_velocityY = 0;
					_routine = 0;
					_phase = Phase.Submarine;
					return;
				}
				break;
		}
		// Obj55_LaserShooter_FacePlayer: turn when the character is more than 8 pixels to the other side
		int distance = PlayerX() - _x;
		if (distance < 0)
		{
			if (distance + 8 <= 0)
			{
				_flipX = false;
			}
		}
		else if (distance - 8 >= 0)
		{
			_flipX = true;
		}
		PlaceShooter();
		_sineCount = (_sineCount + 2) & 0xFF;
	}

	/// <summary>The firing frame shows for 8 frames after a shot.</summary>
	private void UpdateFireFrame()
	{
		_fireFrameTicks = (_fireFrameTicks - 1) & 0xFF;
		if (_fireFrameTicks == 0)
		{
			_mainFrame = FrameShooter;
		}
	}

	/// <summary>Obj55_LaserShooter_Fire: fires a laser at $400 to the side that the laser shooter faces.</summary>
	private void Fire()
	{
		_velocityY = 0;
		_fireFrameTicks = FireFrameTicks;
		_mainFrame = FrameShooterFiring;
		int direction = _flipX ? 1 : -1;
		_lasers.Add(new Laser { X = _x + LaserMuzzle * direction, Y = _y, VelocityX = LaserSpeed * direction });
		PlaySound(OOZBossType.LaserSoundResourceKey);
		_routine = 2;
		_countdown = NextShotTicks;
		_velocityY = -ShooterSpeed;
	}

	/// <summary>
	/// Obj55_LaserShooter_Wind: the stalk twists. The head and each link go round an ellipse of 16 by 4 pixels, each
	/// link $10 later in the turn and $F pixels lower than the one above it.
	/// </summary>
	private void PlaceShooter()
	{
		int angle = _sineCount;
		int y = BossY;
		_x = BossX + (OriginalMath.Cos(angle) >> 4);
		_y = y + (OriginalMath.Sin(angle) >> 6);
		for (int i = 0; i < LinkCount; i++)
		{
			y += 0xF;
			angle = (angle - 0x10) & 0xFF;
			_links[i] = new Vector2i(BossX + (OriginalMath.Cos(angle) >> 4), y + (OriginalMath.Sin(angle) >> 6));
		}
	}

	/// <summary>
	/// Obj55_SpikeChain: the spiked ball comes up out of the oil on the side of the arena that the main character is
	/// on. It goes round a circle of radius $68, one step of 256 each frame, with the links behind it on the circle.
	/// Then the laser shooter comes up.
	/// </summary>
	private void UpdateSpikeChain()
	{
		if (_routine == 0)
		{
			// Obj55_SpikeChain_Init
			SetBossX(ChainLeftX);
			_flipX = false;
			if (PlayerX() >= SideX)
			{
				SetBossX(ChainRightX);
				_flipX = true;
			}
			SetBossY(ChainY);
			_mainFrame = FrameSpikeLeft;
			_sineCount = 0;
			_routine = 1;
			PlaceChain(_sineCount + 0x40);
			return;
		}
		// Obj55_SpikeChain_Move
		PlaceChain(_sineCount + 0x40);
		_sineCount = (_sineCount + 1) & 0xFF;
		if (_sineCount >= 0xFE)
		{
			_routine = 0;
			_phase = Phase.LaserShooter;
			return;
		}
		// Obj55_SpikeChain_SetAnimFrame: the spikes point along the way that the ball moves
		_mainFrame = _sineCount < 0x52 ? FrameSpikeUp : _sineCount < 0x6B ? FrameSpikeUpLeft : _sineCount < 0x92 ? FrameSpikeLeft : FrameSpikeDownLeft;
	}

	private void PlaceChain(int angle)
	{
		Vector2i head = ChainPoint(angle);
		_x = head.X;
		_y = head.Y;
		for (int i = 0; i < LinkCount; i++)
		{
			angle -= 6;
			_links[i] = ChainPoint(angle);
		}
	}

	/// <summary>Obj55_SpikeChain_Rotate: a point on the circle. Without x flip, the ball goes the other way round.</summary>
	private Vector2i ChainPoint(int angle)
	{
		int x = (OriginalMath.Cos(angle) * ChainRadius) >> 8;
		if (!_flipX)
		{
			x = -x;
		}
		int y = (OriginalMath.Sin(angle) * ChainRadius) >> 8;
		return new Vector2i(BossX + x, BossY + y);
	}

	/// <summary>
	/// Obj55_Main_Defeated: explosions for $B3 frames, then Eggman looks defeated for the last $1E of them. Then the
	/// level music starts again, the camera can move to the right, and the submarine sinks 1 pixel each frame.
	/// </summary>
	private void UpdateDefeated()
	{
		if (--_countdown >= 0)
		{
			if (_countdown >= DefeatedFaceTicks)
			{
				UpdateExplosions(ExplosionRadius);
			}
			else
			{
				_mainFrame = FrameEggmanDefeated;
			}
			return;
		}
		if (!Defeated)
		{
			Defeated = true;
			Fleeing = true;
			_routine = ReleaseCameraTicks;
		}
		if (_routine > 0)
		{
			_routine--;
		}
		else if (_y >= SubmergedY)
		{
			LockLifetime = false;
			Finish();
			return;
		}
		_y++;
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (Health > 0)
		{
			// Obj55_HandleHits
			_hitThisPhase = true;
			SetEggmanAnimation(EggmanAnimation.Hurt);
		}
	}

	protected override void Defeat()
	{
		// Boss_Defeat
		base.Defeat();
		Level.Player.GainScore(DefeatScore);
		_phase = Phase.Defeated;
		_countdown = DefeatTicks;
		_routine = 0;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		if (e.Id == MainCollisionId && _phase == Phase.Submarine)
		{
			// The original turns off the collision of the submarine while Eggman flashes after a hit
			if (InvincibilityTimer <= 0)
			{
				base.OnCollision(e);
			}
			return;
		}
		// The laser shooter, the spiked ball, the lasers and the waves always hurt
		var character = (ICharacter)e.ActiveObject;
		if (character.CanBeHurt)
		{
			int x = _collisionX.TryGetValue(e.Id, out int value) ? value : Position.X;
			character.Hurt(System.Math.Sign(character.Position.X - x));
		}
	}

	/// <summary>
	/// Obj55_Laser: a laser moves at $400 until it leaves the arena. A laser at the height of the floor makes an energy
	/// wave when it passes the edge of the floor.
	/// </summary>
	private void UpdateLasers()
	{
		foreach (Laser laser in _lasers)
		{
			// Obj55_Laser_Init takes the first frame
			if (!laser.Started)
			{
				laser.Started = true;
				continue;
			}
			// Obj55_Laser_ChkGround
			if (laser.Y >= FloorY)
			{
				if (laser.VelocityX >= 0 && laser.X >= 0x297C && laser.X < 0x2980)
				{
					AddWave(0x2988, 1);
				}
				else if (laser.VelocityX < 0 && laser.X >= 0x2900 && laser.X < 0x2904)
				{
					AddWave(0x28F8, -1);
				}
			}
			laser.X += laser.VelocityX >> 8;
			laser.Gone = laser.X < LaserMinX || laser.X >= LaserMaxX;
		}
		_lasers.RemoveAll(l => l.Gone);
	}

	private void AddWave(int x, int direction)
	{
		_waves.Add(new Wave { X = x, Direction = direction, Count = WaveCount });
		PlaySound(OOZBossType.WaveSoundResourceKey);
	}

	/// <summary>
	/// Obj55_Wave: each part of the wave makes the next part $10 pixels further on after 6 frames, up to eight parts.
	/// Each part rises and falls in 32 frames and then goes.
	/// </summary>
	private void UpdateWaves()
	{
		// A new part takes its first frame at once, as the original makes it after the current object
		for (int i = 0; i < _waves.Count; i++)
		{
			Wave wave = _waves[i];
			if (--wave.Delay < 0)
			{
				wave.Delay = 0xC7;
				if (--wave.Count >= 0)
				{
					_waves.Add(new Wave { X = wave.X + WaveSpacing * wave.Direction, Direction = wave.Direction, Count = wave.Count });
					PlaySound(OOZBossType.WaveSoundResourceKey);
				}
			}
			wave.Gone = ++wave.Ticks > WaveFrames.Length * WaveFrameTicks + 1;
		}
		_waves.RemoveAll(w => w.Gone);
	}

	/// <summary>
	/// The touch sizes of the original: the submarine $18 x $18, the laser shooter and the spiked ball $10 x 8 at the
	/// centre of the ball, a laser $10 x 2 and a wave 8 x 8. The sizes are half widths and half heights.
	/// </summary>
	private void UpdateTouchRectangles()
	{
		var rectangles = new List<CollisionRectangle>();
		_collisionX.Clear();
		switch (_phase)
		{
			case Phase.Submarine:
				if (_routine != 0)
				{
					rectangles.Add(Box(MainCollisionId, _x, _y, 0x18, 0x18));
				}
				break;
			case Phase.LaserShooter:
			case Phase.SpikeChain:
				if (_routine != 0)
				{
					rectangles.Add(Box(MainCollisionId, _x, _y, 0x10, 8));
				}
				break;
		}
		int id = FirstShotCollisionId;
		foreach (Laser laser in _lasers)
		{
			rectangles.Add(Box(id++, laser.X, laser.Y, 0x10, 2));
		}
		foreach (Wave wave in _waves)
		{
			rectangles.Add(Box(id++, wave.X, FloorY, 8, 8));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	private CollisionRectangle Box(int id, int x, int y, int halfWidth, int halfHeight)
	{
		Vector2i centre = ToHd(x, y) - Position;
		_collisionX[id] = OriginX + x * Scale;
		return new CollisionRectangle(this, id, centre.X - halfWidth * Scale, centre.Y - halfHeight * Scale, halfWidth * 2 * Scale, halfHeight * 2 * Scale);
	}

	/// <summary>
	/// The animations of Eggman in the submarine (Ani_obj55 animations 0, 3 and 5). He laughs with frames 8 and 9,
	/// 40 frames each.
	/// </summary>
	private void SetEggmanAnimation(EggmanAnimation animation)
	{
		_eggmanAnimation = animation;
		_eggmanTicks = 0;
	}

	private void AnimateEggman()
	{
		switch (_eggmanAnimation)
		{
			case EggmanAnimation.Normal:
				_mainFrame = FrameEggman;
				break;
			case EggmanAnimation.Laughing:
				_mainFrame = (_eggmanTicks / 40) % 2 == 0 ? FrameEggman : FrameEggmanLaughing;
				break;
			case EggmanAnimation.Hurt:
				_mainFrame = FrameEggmanHurt;
				break;
		}
		_eggmanTicks++;
	}

	/// <summary>Boss_MoveObject: moves the base position by the velocity.</summary>
	private void MoveBoss() => _bossY += _velocityY;

	/// <summary>The original sets the whole pixels and keeps the fraction.</summary>
	private void SetBossX(int x) => _bossX = (x << 8) | (_bossX & 0xFF);

	private void SetBossY(int y) => _bossY = (y << 8) | (_bossY & 0xFF);

	/// <summary>The x of the main character, in original pixels.</summary>
	private int PlayerX()
	{
		ICharacter protagonist = Level.Player.Protagonist;
		return protagonist == null ? CentreX : (protagonist.Position.X - OriginX) / Scale;
	}

	private Vector2i ToHd(int x, int y) => new Vector2i(OriginX + x * Scale, OriginY + y * Scale);

	private void UpdatePosition() => Position = ToHd(_x, _y);

	private void PlaySound(string key) => Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(key));

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The lasers are behind the boss and the waves in front of it, as their sprite priorities in the original
		foreach (Laser laser in _lasers)
		{
			if (laser.Started)
			{
				DrawFrame(objectRenderer, FrameLaser, laser.X, laser.Y, laser.VelocityX > 0);
			}
		}
		switch (_phase)
		{
			case Phase.Submarine:
			case Phase.Defeated:
				// The first sprite of the original is in front: Eggman, then the submarine
				Colour additiveColour = objectRenderer.AdditiveColour;
				if (IsInvincibleFlashing)
				{
					objectRenderer.AdditiveColour = FlashAdditiveColour;
				}
				DrawFrame(objectRenderer, FrameSubmarine, _x, _y, _flipX);
				objectRenderer.AdditiveColour = additiveColour;
				DrawFrame(objectRenderer, _mainFrame, _x, _y, _flipX);
				break;
			case Phase.LaserShooter:
			case Phase.SpikeChain:
				for (int i = LinkCount - 1; i >= 0; i--)
				{
					DrawFrame(objectRenderer, FrameLink, _links[i].X, _links[i].Y, _flipX);
				}
				DrawFrame(objectRenderer, _mainFrame, _x, _y, _flipX);
				break;
		}
		foreach (Wave wave in _waves)
		{
			int frame = WaveFrames[System.Math.Min((wave.Ticks - 1) / WaveFrameTicks, WaveFrames.Length - 1)];
			DrawFrame(objectRenderer, frame, wave.X, FloorY, false);
		}
	}

	/// <summary>
	/// Draws a mapping frame at a position in original pixels. A flipped frame is also mirrored about the position,
	/// as on the Mega Drive.
	/// </summary>
	private void DrawFrame(IObjectRenderer objectRenderer, int frame, int x, int y, bool flipX)
	{
		_animation.Index = frame;
		Vector2i destination = ToHd(x, y) - Position;
		if (flipX)
		{
			destination.X -= _animation.CurrentFrame.Offset.X * 2;
		}
		objectRenderer.Render(_animation, (Vector2)destination, flipX);
	}
}
