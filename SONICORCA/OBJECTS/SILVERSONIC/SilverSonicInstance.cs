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
using SONICORCA.OBJECTS.SILVERSONIC.WINDOW;

namespace SONICORCA.OBJECTS.SILVERSONIC;

/// <summary>
/// Silver Sonic, the first boss of Death Egg Zone, as in Sonic 2 (ObjAF, Mecha Sonic). The level events add him at
/// ($348, $A0). He waits until the camera reaches x $224, where the camera locks, and the boss music starts 60 frames
/// later. He comes down at $100 to the floor, and Eggman opens the window in the wall to watch. Then he stands for 100
/// frames and makes one of four attacks, in the order of a table of 16 entries:
/// <list type="bullet">
/// <item>Two dashes: each time he waits $20 frames, then dashes at $800 and slows down by $20 each frame.</item>
/// <item>A rolling dash: he rolls up, spins for $20 frames, dashes at $800 and slows down.</item>
/// <item>A rolling jump: he rolls up, spins, moves at $400 and jumps at $600.</item>
/// <item>The same jump, but at the top of the jump he shoots 8 spikes in all directions.</item>
/// </list>
/// Each dash changes direction. While he rolls (mapping frames 6 to 8) he cannot be hit, and he hurts a character.
/// While he stands, the spikes on his head hurt. After 8 hits he explodes for 256 frames and goes. The level events
/// then open the camera to the right.
/// </summary>
public class SilverSonicInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The position that LevEvents_DEZ gives, in original level pixels.</summary>
	public const int InitX = 0x348;

	public const int InitY = 0xA0;

	/// <summary>He comes when the camera reaches this x. The level area locks the camera here.</summary>
	public const int LockCameraX = 0x224;

	/// <summary>The position of the window, from ObjAF (routine $1A).</summary>
	private const int WindowX = 0x2C0;

	private const int WindowY = 0x139;

	private const int OriginalScreenWidth = 320;

	/// <summary>y_radius: the floor is this far below the centre.</summary>
	private const int YRadius = 0x1B;

	private const int Gravity = 0x38;

	private const int DefeatScore = 1000;

	private const int MusicFadeTicks = 60;

	// Touch areas: the touch sizes ($1A, $18) are half sizes in original pixels
	private const int BodyId = 0;

	private const int HeadSpikesId = 1;

	private const int FirstProjectileId = 2;

	private const int BodyHalfSize = 0xC;

	private const int SpikeHalfSize = 4;

	/// <summary>The rolling frames: the body hurts and cannot be hit.</summary>
	private const int FirstBallFrame = 6;

	private const int LastBallFrame = 8;

	/// <summary>byte_398B0: the first state of each attack.</summary>
	private static readonly int[] AttackTable = { 6, 0, 0x10, 6, 6, 0x1E, 0, 0x10, 6, 6, 0x10, 6, 0, 6, 0x10, 0x1E };

	/// <summary>byte_39D92: the offset, the velocity (pixels per frame) and the mapping frame of each spike.</summary>
	private static readonly (int X, int Y, int VelocityX, int VelocityY, int Frame)[] Projectiles =
	{
		(0, -0x18, 0, -3, 0xF),
		(-0x10, -0x10, -2, -2, 0x10),
		(-0x18, 0, -3, 0, 0x11),
		(-0x10, 0x10, -2, 2, 0x12),
		(0, 0x18, 0, 3, 0x13),
		(0x10, 0x10, 2, 2, 0x14),
		(0x18, 0, 3, 0, 0x15),
		(0x10, -0x10, 2, -2, 0x16),
	};

	/// <summary>off_39DE2, with AnimateSprite_Checked.</summary>
	private readonly AnimateSpriteScript _animator = new AnimateSpriteScript(
		// 0 - standing
		new byte[] { 2, 0, 1, 2, 0xFF, 0 },
		// 1 - ready to dash
		new byte[] { 0x45, 3, 0xFD, 0 },
		// 2 - stopping and turning round
		new byte[] { 3, 4, 5, 4, 3, 0xFC },
		// 3 - rolling up
		new byte[] { 3, 3, 3, 3, 6, 6, 6, 7, 7, 7, 8, 8, 8, 6, 6, 7, 7, 8, 8, 6, 7, 8, 0xFC },
		// 4 - spinning
		new byte[] { 2, 6, 7, 8, 0xFF, 0 },
		// 5 - unrolling
		new byte[] { 3, 8, 7, 6, 8, 8, 7, 7, 6, 6, 8, 8, 8, 7, 7, 7, 6, 6, 6, 3, 3, 0xFC })
	{
		Checked = true
	};

	/// <summary>off_39E30: the jet flame (the child in objoff_3E).</summary>
	private readonly AnimateSpriteScript _flameAnimator = new AnimateSpriteScript(
		new byte[] { 1, 0xB, 0xC, 0xFF },
		new byte[] { 1, 0xD, 0xE, 0xFF },
		new byte[] { 1, 9, 0xA, 0xFF })
	{
		Checked = true
	};

	private readonly List<Projectile> _projectiles = new List<Projectile>();

	private AnimationInstance _animation;

	private Routine _routine;

	/// <summary>routine_secondary in the attacks: the offsets of off_398F2.</summary>
	private int _attackState;

	/// <summary>objoff_2A.</summary>
	private int _timer;

	/// <summary>objoff_32: the explosion time.</summary>
	private int _explosionTimer;

	/// <summary>objoff_2C: the dashes that are left.</summary>
	private int _dashes;

	/// <summary>objoff_2D: the direction of the next dash changes each time.</summary>
	private bool _dashRight;

	/// <summary>objoff_2F: the next entry of the attack table.</summary>
	private int _attackIndex;

	/// <summary>objoff_2E: the spikes of this jump are out.</summary>
	private bool _spikesFired;

	/// <summary>The position in original level pixels, and the velocity in 1/256 pixel per frame.</summary>
	private Vector2 _position;

	private Vector2 _velocity;

	/// <summary>render_flags.x_flip: he faces right. The art faces left.</summary>
	private bool _facingRight;

	/// <summary>The body can touch (collision_flags is not 0). It is 0 until he lands.</summary>
	private bool _landed;

	/// <summary>The jet flame shows (routine $10 of the child), or not (routine $12).</summary>
	private bool _flameVisible = true;

	/// <summary>The spikes on the head hurt (routine $18 of the child in objoff_3C).</summary>
	private bool _headSpikesActive;

	private enum Routine
	{
		Wait,
		Delay,
		Fall,
		Stand,
		Attack,
		Exploding
	}

	private sealed class Projectile
	{
		public Vector2 Position;

		public Vector2 Velocity;

		public int Frame;
	}

	/// <summary>The x of the original level origin, in HD units. The level area gives it.</summary>
	[StateVariable]
	public int OriginX { get; set; }

	/// <summary>The y of the original level origin, in HD units. The level area gives it.</summary>
	[StateVariable]
	public int OriginY { get; set; }

	/// <summary>He is on the floor (status.npc.y_flip, which opens the window).</summary>
	[HideInEditor]
	public bool Landed => _landed;

	/// <summary>He flashes after a hit: the body does not touch.</summary>
	[HideInEditor]
	public bool Flashing => InvincibilityTimer > 0;

	/// <summary>The explosions are over and he is gone. The level events open the camera to the right.</summary>
	[HideInEditor]
	public bool Exploded { get; private set; }

	public SilverSonicInstance() => DesignBounds = new Rectanglei(-2048, -2048, 4096, 4096);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SilverSonicType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(SilverSonicType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(SilverSonicType.BossHitSoundResourceKey);
		LockLifetime = true;
		Health = 8;
		_position = new Vector2((double)(Position.X - OriginX) / Scale, (double)(Position.Y - OriginY) / Scale);
		_routine = Routine.Wait;
		_flameAnimator.Frame = 0xB;
		if (Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(SilverSonicType.WindowResourceKey),
			Level.Map.Layers.IndexOf(Layer), new Vector2i(OriginX + WindowX * Scale, OriginY + WindowY * Scale), new { })) is SilverSonicWindowInstance window)
		{
			window.Boss = this;
		}
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateProjectiles();
		switch (_routine)
		{
			case Routine.Wait:
				if (GetCameraX() >= LockCameraX)
				{
					_routine = Routine.Delay;
					_timer = 60;
					_velocity = new Vector2(0, 0x100);
					Level.SoundManager.FadeOutMusic(MusicFadeTicks);
				}
				break;
			case Routine.Delay:
				if (--_timer < 0)
				{
					_routine = Routine.Fall;
					PlayMusic(Type.GetAbsolutePath(SilverSonicType.BossMusicResourceKey));
				}
				break;
			case Routine.Fall:
				UpdateFall();
				break;
			case Routine.Stand:
				UpdateStand();
				break;
			case Routine.Attack:
				UpdateAttack();
				_position += _velocity / 256.0;
				break;
			case Routine.Exploding:
				UpdateExploding();
				break;
		}
		// loc_39BCC: the flame animates on each frame while it shows
		if (_flameVisible)
		{
			_flameAnimator.Step();
		}
		PositionPrecise = ToLevel(_position);
		UpdateTouchAreas();
	}

	/// <summary>Routine 6: come down at $100 until the floor, then stand.</summary>
	private void UpdateFall()
	{
		int? floor = GetFloorDistance();
		if (floor is int distance && distance < 0)
		{
			_position.Y += distance;
			_velocity.Y = 0;
			_landed = true;
			StartStanding();
			return;
		}
		_position += _velocity / 256.0;
	}

	/// <summary>Routine 8: stand for 100 frames on the floor, then start the next attack of the table.</summary>
	private void UpdateStand()
	{
		if (--_timer == 0)
		{
			_routine = Routine.Attack;
			_attackState = AttackTable[_attackIndex & 0xF];
			_attackIndex++;
			_spikesFired = false;
			_headSpikesActive = false;
			return;
		}
		SnapToFloor();
		_animator.Step();
	}

	/// <summary>loc_399D6: stand still, with no flame, and the spikes on the head hurt.</summary>
	private void StartStanding()
	{
		_routine = Routine.Stand;
		_animator.Animation = 0;
		_timer = 0x64;
		_velocity.X = 0;
		_flameVisible = false;
		_headSpikesActive = true;
	}

	/// <summary>Routine $A: the states of the attacks (off_398F2).</summary>
	private void UpdateAttack()
	{
		switch (_attackState)
		{
			case 0:
				// Two dashes
				_attackState = 2;
				_animator.Frame = 3;
				_dashes = 2;
				PrepareDash();
				break;
			case 2:
				if (--_timer >= 0)
				{
					break;
				}
				_attackState = 4;
				_timer = 0x40;
				_animator.Animation = 1;
				Dash(0x800);
				_flameAnimator.Animation = 2;
				PlaySound(SilverSonicType.SpindashSoundResourceKey);
				break;
			case 4:
				UpdateDash();
				break;
			case 6:
			case 0x10:
			case 0x1E:
				// Roll up
				_attackState += 2;
				_animator.Frame = 3;
				_animator.Animation = 3;
				break;
			case 8:
			case 0x12:
			case 0x20:
				if (_animator.Step() != 0)
				{
					_attackState += 2;
					_timer = 0x20;
					_animator.Animation = 4;
				}
				break;
			case 0xA:
				if (--_timer >= 0)
				{
					_animator.Step();
					break;
				}
				_attackState += 2;
				_timer = 0x40;
				Dash(0x800);
				break;
			case 0xC:
				if (--_timer >= 0)
				{
					SlowDown();
					_animator.Step();
					break;
				}
				StopAndUnroll();
				break;
			case 0xE:
			case 0x1C:
			case 0x2A:
				if (_animator.Step() != 0)
				{
					StartStanding();
				}
				break;
			case 0x14:
			case 0x22:
				if (--_timer >= 0)
				{
					_animator.Step();
					break;
				}
				_attackState += 2;
				_timer = 0x40;
				Dash(0x400);
				break;
			case 0x16:
			case 0x24:
				if (--_timer == 60)
				{
					_attackState += 2;
					_velocity.Y = -0x600;
				}
				_animator.Step();
				break;
			case 0x18:
			case 0x26:
				UpdateJump(_attackState == 0x26);
				break;
			case 0x1A:
			case 0x28:
				if (--_timer < 0)
				{
					StopAndUnroll();
					break;
				}
				SnapToFloor();
				_animator.Step();
				break;
		}
	}

	/// <summary>State 4: the dash slows down. Halfway he stops rolling and turns round, then he dashes again or stands.</summary>
	private void UpdateDash()
	{
		if (--_timer < 0)
		{
			if (--_dashes != 0)
			{
				_attackState = 2;
				_velocity.X = 0;
				PrepareDash();
			}
			else
			{
				StartStanding();
			}
			return;
		}
		if (_timer == 0x20)
		{
			_animator.Animation = 2;
			_flameVisible = false;
		}
		SlowDown();
		_animator.Step();
		// He turns round when the second frame of the stop shows
		if (_animator.Animation == 2 && _animator.Index == 2 && _animator.Timer == 3)
		{
			_facingRight = !_facingRight;
		}
	}

	/// <summary>States $18 and $26: the jump. At the top of the second jump, the spikes go out.</summary>
	private void UpdateJump(bool withSpikes)
	{
		if (--_timer < 0)
		{
			StopAndUnroll();
			return;
		}
		if (withSpikes && !_spikesFired && _velocity.Y >= 0)
		{
			_spikesFired = true;
			FireSpikes();
		}
		if (GetFloorDistance() is int distance && distance < 0)
		{
			_attackState += 2;
			_position.Y += distance;
			_velocity.Y = 0;
		}
		_velocity.Y += Gravity;
		_animator.Step();
	}

	/// <summary>loc_3992E: get ready to dash for $20 frames, with the flame of the jets.</summary>
	private void PrepareDash()
	{
		_timer = 0x20;
		_flameVisible = true;
		_flameAnimator.Animation = 1;
	}

	/// <summary>loc_39D60: each dash goes the other way. The first one goes to the left.</summary>
	private void Dash(int speed)
	{
		_velocity.X = _dashRight ? speed : -speed;
		_dashRight = !_dashRight;
	}

	/// <summary>loc_39D72.</summary>
	private void SlowDown() => _velocity.X += _velocity.X < 0 ? 0x20 : -0x20;

	/// <summary>loc_39A7C: stop, turn round and unroll.</summary>
	private void StopAndUnroll()
	{
		_attackState += 2;
		_animator.Animation = 5;
		_facingRight = !_facingRight;
		_velocity = default;
	}

	/// <summary>loc_39D82: 8 spikes (Obj98) that fly away in straight lines.</summary>
	private void FireSpikes()
	{
		foreach ((int x, int y, int velocityX, int velocityY, int frame) in Projectiles)
		{
			_projectiles.Add(new Projectile
			{
				Position = _position + new Vector2(x, y),
				Velocity = new Vector2(velocityX, velocityY),
				Frame = frame
			});
		}
	}

	/// <summary>Obj98: a spike moves until it is off the screen.</summary>
	private void UpdateProjectiles()
	{
		foreach (Projectile projectile in _projectiles)
		{
			projectile.Position += projectile.Velocity;
		}
		Rectangle screen = Level.Camera.Bounds;
		_projectiles.RemoveAll(projectile => !screen.Contains(ToLevel(projectile.Position)));
	}

	/// <summary>Routine $C: explosions for 256 frames, then the camera opens and the level music plays again.</summary>
	private void UpdateExploding()
	{
		if (--_explosionTimer >= 0)
		{
			if ((Level.Ticks & 7) == 0)
			{
				ExplodeAt(Position + new Vector2i((Level.Random.Next(0x40) - 0x20) * Scale, (Level.Random.Next(0x40) - 0x20) * Scale));
			}
			return;
		}
		// The original plays Level_Music as a byte, which plays nothing. This plays the level music, as fixBugs does.
		if (!string.IsNullOrEmpty(Level.LevelMusic))
		{
			PlayMusic(Level.LevelMusic);
		}
		Exploded = true;
		FinishForever();
	}

	protected override void Defeat()
	{
		// loc_39CF0
		Level.Player.GainScore(DefeatScore);
		_explosionTimer = 0xFF;
		_routine = Routine.Exploding;
		_flameVisible = false;
		_headSpikesActive = false;
		Defeated = true;
	}

	/// <summary>
	/// The touch areas. The body (collision $1A or $9A, 12 by 12 pixels from the centre) touches from the landing,
	/// but not while it flashes after a hit. It can be hit, but it hurts while he rolls. The spikes on the head
	/// (collision $98, 4 by 4 pixels at x $C, y -$C) and the spikes that fly hurt.
	/// </summary>
	private void UpdateTouchAreas()
	{
		var rectangles = new List<CollisionRectangle>();
		if (_landed && Health > 0 && InvincibilityTimer <= 0)
		{
			rectangles.Add(Box(BodyId, 0, 0, BodyHalfSize));
		}
		if (_headSpikesActive)
		{
			rectangles.Add(Box(HeadSpikesId, _facingRight ? -0xC : 0xC, -0xC, SpikeHalfSize));
		}
		for (int i = 0; i < _projectiles.Count; i++)
		{
			Vector2 offset = _projectiles[i].Position - _position;
			rectangles.Add(Box(FirstProjectileId + i, (int)Math.Round(offset.X), (int)Math.Round(offset.Y), SpikeHalfSize));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	/// <summary>A touch area from its centre and half size in original pixels.</summary>
	private CollisionRectangle Box(int id, int x, int y, int halfSize) =>
		new CollisionRectangle(this, id, (x - halfSize) * Scale, (y - halfSize) * Scale, halfSize * 2 * Scale, halfSize * 2 * Scale);

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		bool rolling = _animator.Frame >= FirstBallFrame && _animator.Frame <= LastBallFrame;
		if (e.Id == BodyId && !rolling)
		{
			base.OnCollision(e);
			return;
		}
		var character = (ICharacter)e.ActiveObject;
		if (character.CanBeHurt)
		{
			character.Hurt(Math.Sign(character.Position.X - Position.X));
		}
	}

	/// <summary>ObjCheckFloorDist: the distance from the feet down to the floor, in original pixels.</summary>
	private int? GetFloorDistance() =>
		LandscapeFloor.GetDistance(Level, (Vector2i)ToLevel(_position + new Vector2(0, YRadius)));

	private void SnapToFloor()
	{
		if (GetFloorDistance() is int distance)
		{
			_position.Y += distance;
		}
	}

	/// <summary>The original camera X: the left edge of a 320 pixel screen at the centre of the HD camera.</summary>
	private double GetCameraX() => (Level.Camera.Bounds.X + Level.Camera.Bounds.Width / 2 - OriginX) / Scale - OriginalScreenWidth / 2;

	private void PlaySound(string key) => Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(key));

	private void PlayMusic(string key)
	{
		if (ResourceTree[key]?.Resource != null)
		{
			Level.SoundManager.PlayMusic(key);
		}
	}

	private Vector2 ToLevel(Vector2 position) => new Vector2(OriginX + position.X * Scale, OriginY + position.Y * Scale);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The spikes (priority 5) are behind him, and the flame (a later object) is behind the body
		foreach (Projectile projectile in _projectiles)
		{
			_animation.Index = projectile.Frame;
			objectRenderer.RenderOriginal(_animation, (projectile.Position - _position) * Scale);
		}
		objectRenderer.AdditiveColour = IsInvincibleFlashing ? FlashAdditiveColour : Colours.Black;
		if (_flameVisible)
		{
			_animation.Index = _flameAnimator.Frame;
			objectRenderer.RenderOriginal(_animation, _facingRight);
		}
		_animation.Index = _animator.Frame;
		objectRenderer.RenderOriginal(_animation, _facingRight);
		objectRenderer.AdditiveColour = Colours.Black;
	}
}
