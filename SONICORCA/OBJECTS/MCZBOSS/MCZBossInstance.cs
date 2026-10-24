using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZBOSS;

/// <summary>
/// The Mystic Cave Zone boss, as in Sonic 2 (Obj57). Eggman flies a ship with two drills. The ship comes down through
/// the ceiling at $C0 from y $560 while the screen shakes and stones and spikes fall. Below y $620 the shaking stops.
/// At y $660 the drills turn to the side and the ship moves at $200 toward the player. At the arena edge (x $2120 or
/// $2200), or when a drill hurts the player, the drills turn up and the ship goes back up into the ceiling at $C0.
/// At y $560 it moves to the side of the arena away from the player and comes down again at $100. After 8 hits the
/// ship explodes for 180 frames, the drills fall off, and Eggman sinks, rises and flies away to the right.
/// </summary>
public class MCZBossInstance : BossObject
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	/// <summary>The position that Obj57_Init gives, in original level pixels.</summary>
	public const int InitX = 0x21A0;

	public const int InitY = 0x560;

	/// <summary>The ship goes up to this y before it changes side.</summary>
	private const int TopY = 0x560;

	/// <summary>Above this y the screen shakes and rocks fall.</summary>
	private const int ShakeY = 0x620;

	/// <summary>The ship stops at this y and moves to the side.</summary>
	private const int BottomY = 0x660;

	private const int LeftX = 0x2120;

	private const int RightX = 0x2200;

	/// <summary>When the ship goes up, it changes to the right side if the player is left of this x.</summary>
	private const int MiddleX = 0x2190;

	/// <summary>The rocks start at a random x from $20F0 to $2230, at y $5F0.</summary>
	private const int RockLeftX = 0x20F0;

	private const int RockRightX = 0x2230;

	private const int RockY = 0x5F0;

	/// <summary>The drills that fall off stop at this y.</summary>
	private const int FloorY = 0x6F0;

	/// <summary>The x distance from the ship to the second drill.</summary>
	private const int DrillSpacing = 0x28;

	private const int DefeatScore = 1000;

	/// <summary>The original moves the right camera limit to $2240 at 2 pixels per frame, then deletes the ship.</summary>
	private const int FleeTicks = (0x2240 - 0x20F0) / 2;

	// Indexes into the animation array of the original (Boss_AnimationArray), 2 bytes for each sprite
	private const int HoverFire = 0;

	private const int Drill1 = 1;

	private const int Vehicle = 2;

	private const int Face = 3;

	private const int Drill2 = 4;

	private AnimationInstance _animation;

	private readonly BossAnimator _animator = new BossAnimator();

	private Routine _routine;

	private int _countdown;

	/// <summary>Boss_X_pos and Boss_Y_pos, in original level pixels.</summary>
	private double _bossX;

	private double _bossY;

	/// <summary>Boss_X_vel and Boss_Y_vel, in 1/256 pixel per frame.</summary>
	private int _velocityX;

	private int _velocityY;

	/// <summary>The ship faces right (the art faces left).</summary>
	private bool _facingRight;

	/// <summary>Boss_CollisionRoutine: 0 when the drills point up, 1 when they point to the side.</summary>
	private int _collisionRoutine;

	/// <summary>The character that a drill hurt (boss_hurt_sonic), while it is still hurt.</summary>
	private ICharacter _hurtCharacter;

	/// <summary>boss_defeated: the drills fall off.</summary>
	private bool _fallingApart;

	private Vector2 _drill1;

	private Vector2 _drill2;

	private int _drill1VelocityY = -0x380;

	private int _drill2VelocityY = -0x380;

	private int _fleeTicks;

	private enum Routine
	{
		Ascend,
		Descend,
		Settle,
		Dig,
		Exploding,
		Sink,
		Flee
	}

	/// <summary>The x of the original level origin, in HD units. The level area gives it.</summary>
	[StateVariable]
	public int OriginX { get; set; }

	/// <summary>The y of the original level origin, in HD units. The level area gives it.</summary>
	[StateVariable]
	public int OriginY { get; set; }

	public MCZBossInstance() => DesignBounds = new Rectanglei(-1024, -1024, 2048, 2048);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZBossType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(MCZBossType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(MCZBossType.BossHitSoundResourceKey);
		LockLifetime = true;
		_bossX = (double)(Position.X - OriginX) / Scale;
		_bossY = (double)(Position.Y - OriginY) / Scale;
		_routine = Routine.Descend;
		_velocityY = 0xC0;
		_countdown = 0x28;
		_collisionRoutine = 0;
		SetShaking(true);
		_animator.Start();
		TransferPositions();
	}

	protected override void OnStop()
	{
		SetShaking(false);
		FinishForever();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_routine)
		{
			case Routine.Ascend:
				UpdateAscend();
				break;
			case Routine.Descend:
				MoveBoss();
				SpawnRock();
				if (_bossY >= ShakeY)
				{
					_routine = Routine.Settle;
					SetShaking(false);
				}
				break;
			case Routine.Settle:
				UpdateSettle();
				break;
			case Routine.Dig:
				UpdateDig();
				break;
			case Routine.Exploding:
				UpdateExploding();
				break;
			case Routine.Sink:
				UpdateSink();
				break;
			case Routine.Flee:
				_velocityX = 0x400;
				_velocityY = -0x40;
				MoveBoss();
				if (++_fleeTicks >= FleeTicks)
				{
					LockLifetime = false;
				}
				break;
		}
		PositionPrecise = ToLevel(_bossX, _bossY);
		// The original animates the sprites in every routine but the explosions and the sinking
		if (_routine != Routine.Exploding && _routine != Routine.Sink)
		{
			_animator.Animate();
		}
		TransferPositions();
		UpdateTouchAreas();
	}

	/// <summary>Routine 0: wait while the drills turn up, then go up into the ceiling and change side.</summary>
	private void UpdateAscend()
	{
		if (--_countdown < 0)
		{
			// The vehicle light goes off
			_animator.Array[Vehicle * 2 + 1] = 0;
			MoveBoss();
			if (_bossY <= TopY)
			{
				_velocityY = 0x100;
				ICharacter player = Level.Player.Protagonist;
				double playerX = player != null ? (double)(player.Position.X - OriginX) / Scale : 0.0;
				_bossX = playerX < MiddleX ? RightX : LeftX;
				_routine = Routine.Descend;
				_facingRight = playerX - _bossX >= 0;
			}
		}
		if (_countdown == 0x28)
		{
			_collisionRoutine = 0;
		}
		if (_bossY < ShakeY)
		{
			SetShaking(true);
			SpawnRock();
		}
	}

	/// <summary>Routine 4: move down to y $660, then turn the drills to the side and move toward the player.</summary>
	private void UpdateSettle()
	{
		MoveBoss();
		if (_bossY < BottomY)
		{
			return;
		}
		_bossY = BottomY;
		_routine = Routine.Dig;
		_animator.Request(Drill1, 6);
		_animator.Request(Drill2, 6);
		_animator.Request(Face, 0xD);
		// The vehicle light comes on and the fire of the hover jets goes off
		_animator.Array[Vehicle * 2 + 1] = 0x20;
		_animator.Array[HoverFire * 2 + 1] = 0x30;
		_countdown = 0x64;
		_facingRight = PlayerX() - _bossX >= 0;
		_velocityX = _facingRight ? 0x200 : -0x200;
		_velocityY = 0;
	}

	/// <summary>Routine 6: the drills turn to the side, then the ship moves until it reaches an edge or hurts the player.</summary>
	private void UpdateDig()
	{
		_countdown--;
		if (_countdown > 0x28)
		{
			return;
		}
		_collisionRoutine = 1;
		if (_countdown >= 0)
		{
			return;
		}
		if (_hurtCharacter != null)
		{
			bool laugh = _hurtCharacter.IsHurt;
			_hurtCharacter = null;
			if (laugh)
			{
				// Eggman laughs
				_animator.Array[Face * 2 + 1] = 0x30;
				StartAscend();
				return;
			}
		}
		MoveBoss();
		if (_bossX <= LeftX)
		{
			_bossX = LeftX;
			StartAscend();
		}
		else if (_bossX >= RightX)
		{
			_bossX = RightX;
			StartAscend();
		}
	}

	private void StartAscend()
	{
		_velocityX = 0;
		_routine = Routine.Ascend;
		_animator.Request(Drill1, 0xB);
		_animator.Request(Drill2, 0xB);
		_animator.Array[HoverFire * 2 + 1] = 0;
		_animator.Request(Face, 0xD);
		_countdown = 0x64;
		_velocityY = -0xC0;
	}

	/// <summary>Routine 8: explosions for 180 frames, then Eggman turns to the right.</summary>
	private void UpdateExploding()
	{
		_fallingApart = true;
		SetShaking(false);
		if (--_countdown >= 0)
		{
			// The burnt face, and no fire from the hover jets
			_animator.Frames[Face] = 0x13;
			_animator.Frames[HoverFire] = 7;
			if ((Level.Ticks & 7) == 0)
			{
				ExplodeAt(Position + new Vector2i((Level.Random.Next(0x40) - 0x20) * Scale, (Level.Random.Next(0x40) - 0x20) * Scale));
			}
			return;
		}
		_facingRight = true;
		_velocityX = 0;
		_velocityY = 0;
		_routine = Routine.Sink;
		_animator.Frames[Face] = 0x12;
		_countdown = -0x12;
	}

	/// <summary>Routine $A: sink to y $620 or lower, rise a little, then fly away.</summary>
	private void UpdateSink()
	{
		_countdown++;
		if (_countdown == 0)
		{
			_velocityY = 0;
		}
		else if (_countdown < 0)
		{
			if (_bossY < ShakeY)
			{
				_countdown--;
			}
			_velocityY += 0x10;
		}
		else if (_countdown < 0x18)
		{
			_velocityY -= 8;
		}
		else if (_countdown == 0x18)
		{
			// The original plays the level music again here
			_velocityY = 0;
		}
		else if (_countdown >= 0x20)
		{
			// The face starts again after 14 frames, and the hover jets fire
			_animator.Array[Face * 2 + 1] = 0xD;
			_animator.Array[HoverFire * 2 + 1] = 0;
			_routine = Routine.Flee;
			Fleeing = true;
		}
		MoveBoss();
	}

	/// <summary>Obj57_SpawnStoneSpike: a spike every 32 frames, else a stone every 8 frames.</summary>
	private void SpawnRock()
	{
		bool spike;
		if ((Level.Ticks & 0x1F) == 0)
		{
			spike = true;
		}
		else if ((Level.Ticks & 7) == 0)
		{
			spike = false;
		}
		else
		{
			return;
		}
		int x;
		do
		{
			x = RockLeftX + Level.Random.Next(0x200);
		}
		while (x > RockRightX);
		Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(MCZBossType.RockResourceKey), Level.Map.Layers.IndexOf(Layer),
			(Vector2i)ToLevel(x, RockY), new { Spike = spike, OriginY }));
	}

	/// <summary>Obj57_TransferPositions: the drills follow the ship until it is defeated, then they fall off.</summary>
	private void TransferPositions()
	{
		if (!_fallingApart)
		{
			_drill1 = new Vector2(_bossX, _bossY);
			_drill2 = new Vector2(_bossX + (_facingRight ? DrillSpacing : -DrillSpacing), _bossY);
			return;
		}
		if (_countdown > 0x78)
		{
			return;
		}
		// The drills fall with gravity $38 and slide apart by 1 pixel each frame. The original never stops the slide.
		_drill2.X -= 1;
		_drill2.Y += _drill2VelocityY / 256.0;
		_drill2VelocityY += 0x38;
		if (_drill2.Y >= FloorY)
		{
			_drill2VelocityY = 0;
		}
		_drill1.X += 1;
		_drill1.Y += _drill1VelocityY / 256.0;
		_drill1VelocityY += 0x38;
		if (_drill1.Y >= FloorY)
		{
			_drill1VelocityY = 0;
		}
	}

	/// <summary>
	/// The touch areas. The body (collision $F, 24 by 24 pixels from the centre) can be hit. The drills hurt
	/// (BossCollision_MCZ): two areas of 4 by 16 pixels at x +-$14, y -$20 when they point up, or one area of 4 by 4
	/// pixels at x -$30 (or +$30 when the ship faces right), y +4 when they point to the side. Nothing touches while
	/// the ship flashes after a hit, or after the defeat.
	/// </summary>
	private void UpdateTouchAreas()
	{
		if (Health <= 0 || InvincibilityTimer > 0)
		{
			CollisionRectangles = new CollisionRectangle[0];
			return;
		}
		CollisionRectangle body = Box(0, 0, 0, 0x18, 0x18);
		if (_collisionRoutine == 0)
		{
			CollisionRectangles = new[] { body, Box(1, 0x14, -0x20, 4, 0x10), Box(2, -0x14, -0x20, 4, 0x10) };
		}
		else
		{
			CollisionRectangles = new[] { body, Box(1, _facingRight ? 0x30 : -0x30, 4, 4, 4) };
		}
	}

	/// <summary>A touch area from its centre and half size in original pixels.</summary>
	private CollisionRectangle Box(int id, int x, int y, int halfWidth, int halfHeight) =>
		new CollisionRectangle(this, id, (x - halfWidth) * Scale, (y - halfHeight) * Scale, halfWidth * 2 * Scale, halfHeight * 2 * Scale);

	protected override void OnCollision(CollisionEvent e)
	{
		if (Health <= 0 || InvincibilityTimer > 0 || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		if (e.Id == 0)
		{
			base.OnCollision(e);
			return;
		}
		// A drill hurts even a rolling character
		var character = (ICharacter)e.ActiveObject;
		if (character.CanBeHurt)
		{
			character.Hurt(Math.Sign(character.Position.X - Position.X));
			if (character.IsHurt)
			{
				_hurtCharacter = character;
			}
		}
	}

	protected override void Hit(ICharacter character)
	{
		base.Hit(character);
		if (Health > 0)
		{
			// Eggman makes the face of a hit
			_animator.Array[Face * 2 + 1] = 0xC0;
		}
	}

	protected override void Defeat()
	{
		Level.Player.GainScore(DefeatScore);
		_countdown = 0xB3;
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

	/// <summary>The screen shakes (Screen_Shaking_Flag). SonicOrca has the earthquake of the level for this.</summary>
	private void SetShaking(bool shaking)
	{
		if (Level != null)
		{
			Level.EarthquakeActive = shaking;
		}
	}

	private Vector2 ToLevel(double x, double y) => new Vector2(OriginX + x * Scale, OriginY + y * Scale);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		objectRenderer.AdditiveColour = IsInvincibleFlashing ? FlashAdditiveColour : Colours.Black;
		// The original draws the main sprite in front, then the child sprites in order. Here the back is drawn first.
		Vector2 origin = new Vector2(_bossX, _bossY);
		DrawPart(objectRenderer, _animator.Frames[Drill2], (_drill2 - origin) * Scale);
		DrawPart(objectRenderer, _animator.Frames[Face], default);
		DrawPart(objectRenderer, _animator.Frames[Vehicle], default);
		DrawPart(objectRenderer, _animator.Frames[Drill1], (_drill1 - origin) * Scale);
		DrawPart(objectRenderer, _animator.Frames[HoverFire], default);
		objectRenderer.AdditiveColour = Colours.Black;
	}

	private void DrawPart(IObjectRenderer objectRenderer, int frame, Vector2 offset)
	{
		_animation.Index = frame;
		objectRenderer.RenderOriginal(_animation, offset, _facingRight);
	}

	/// <summary>
	/// AnimateBoss with the scripts of Ani_obj57. Each of the 5 sprites has 2 bytes: the old and the new script in the
	/// high and low nibbles, then the frame index and the timer in the high and low nibbles. A new script starts from
	/// its first frame. The bytes wrap like the original, so a frame index of 16 becomes 0.
	/// </summary>
	private sealed class BossAnimator
	{
		private static readonly byte[][] Scripts =
		{
			// 0 - main vehicle: light off, then light on from frame 2
			new byte[] { 0xF, 1, 0xFF, 0, 0xFC, 2 },
			// 1 - drill diagonal
			new byte[] { 5, 8, 0xFF },
			// 2 - hover jets: fire on, then fire off from frame 3
			new byte[] { 1, 5, 6, 0xFF, 7, 0xFC, 3 },
			// 3 to 5 - drill vertical
			new byte[] { 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 0xFD, 4 },
			new byte[] { 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 2, 2, 3, 3, 0xFD, 5 },
			new byte[] { 1, 4, 2, 3, 4, 0xFC, 1 },
			// 6 and 7 - drill vertical, then the turn to horizontal
			new byte[] { 1, 2, 3, 4, 4, 2, 2, 3, 3, 3, 4, 4, 4, 2, 2, 2, 0xFD, 7 },
			new byte[] { 1, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 2, 8, 8, 8, 0xFD, 8 },
			// 8 to $A - drill horizontal
			new byte[] { 1, 9, 9, 9, 9, 9, 0xA, 0xA, 0xA, 0xA, 0xA, 0xB, 0xB, 0xB, 0xB, 0xFD, 9 },
			new byte[] { 1, 9, 9, 9, 9, 0xA, 0xA, 0xA, 0xB, 0xB, 0xB, 9, 9, 0xA, 0xA, 0xFD, 0xA },
			new byte[] { 1, 0xB, 9, 0xA, 0xB, 0xFC, 1 },
			// $B and $C - drill horizontal, then the turn to vertical
			new byte[] { 1, 9, 0xA, 0xB, 0xB, 9, 9, 0xA, 0xA, 0xA, 0xB, 0xB, 0xB, 9, 9, 9, 0xFD, 0xC },
			new byte[] { 1, 9, 0xA, 0xA, 0xA, 0xA, 0xB, 0xB, 0xB, 0xB, 0xB, 9, 8, 8, 8, 8, 0xFD, 3 },
			// $D - face: normal, the laugh from frame 3, the hit face from frame $C
			new byte[] { 7, 0xE, 0xF, 0xFF, 0x10, 0x11, 0x10, 0x11, 0x10, 0x11, 0x10, 0x11, 0xFF, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0x12, 0xFF },
			// $E - face when hit
			new byte[] { 7, 0x12, 0xFF }
		};

		/// <summary>Boss_AnimationArray.</summary>
		public byte[] Array { get; } = new byte[10];

		/// <summary>The mapping frame of each sprite: the hover jets, drill 1, the vehicle, the face and drill 2.</summary>
		public int[] Frames { get; } = new int[5];

		/// <summary>Obj57_Init and Obj57_InitAnimationData.</summary>
		public void Start()
		{
			Frames[HoverFire] = 5;
			Frames[Drill1] = 2;
			Frames[Vehicle] = 1;
			Frames[Face] = 0xE;
			Frames[Drill2] = 2;
			byte[] initial = { 2, 0, 3, 0, 0x10, 0, 0xD, 0, 3, 0 };
			initial.CopyTo(Array, 0);
		}

		/// <summary>Changes the new script of a sprite and keeps the old one, so the new script starts.</summary>
		public void Request(int sprite, int script) => Array[sprite * 2] = (byte)((Array[sprite * 2] & 0xF0) | script);

		public void Animate()
		{
			for (int sprite = 0; sprite < Frames.Length; sprite++)
			{
				int oldScript = Array[sprite * 2] >> 4;
				int script = Array[sprite * 2] & 0xF;
				int frame = Array[sprite * 2 + 1] >> 4;
				int timer = Array[sprite * 2 + 1] & 0xF;
				if (oldScript != script)
				{
					frame = 0;
					timer = 0;
				}
				int scripts = (script << 4) | script;
				timer--;
				if (timer < 0)
				{
					byte[] data = Scripts[script];
					timer = data[0];
					int value = data[1 + frame];
					switch (value)
					{
						case 0xFF:
							// Go back to the first frame
							Frames[sprite] = data[1] & 0x7F;
							frame = 1;
							break;
						case 0xFD:
							// Change to the next script. It starts on the next frame.
							scripts = (scripts & 0xF0) | data[2 + frame];
							break;
						case 0xFC:
							// Go back to an earlier frame
							frame = data[2 + frame];
							Frames[sprite] = data[1 + frame] & 0x7F;
							frame++;
							break;
						default:
							Frames[sprite] = value & 0x7F;
							frame++;
							break;
					}
				}
				Array[sprite * 2] = (byte)scripts;
				Array[sprite * 2 + 1] = (byte)((frame << 4) | timer);
			}
		}
	}
}
