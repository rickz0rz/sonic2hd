using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZBOSS;

/// <summary>
/// The Aquatic Ruin Zone boss, as in Sonic 2 (Obj89). When the characters are in the arena, two pillars rise from
/// the floor and Eggman flies down. He flies from one pillar to the other and hits it with his hammer. The pillar
/// that he hits shoots an arrow from its eyes at a random height. The arrow sticks in the other pillar, and a
/// character can stand on it until it falls. After 8 hits Eggman explodes, drops the hammer, the pillars go down,
/// and he flies away to the right.
/// </summary>
/// <remarks>
/// The code keeps all positions in original pixels, in the absolute coordinates of act 2 of the original. The
/// <see cref="OriginX"/> and <see cref="OriginY"/> state variables give the HD position of the original (0, 0).
/// </remarks>
public class ARZBossInstance : BossObject
{
	private const int Scale = 4;

	/// <summary>The position that Obj89_Init gives to Eggman.</summary>
	private const int StartX = 0x2AE0;

	private const int StartY = 0x388;

	/// <summary>With Sonic and Tails, the fight starts when both are between these x positions.</summary>
	private const int ArenaLeft = 0x2A60;

	private const int ArenaRight = 0x2B60;

	private const int HoverY = 0x430;

	/// <summary>Eggman stops at these x positions, in front of the left and the right pillar.</summary>
	private const int LeftTargetX = 0x2AB0;

	private const int RightTargetX = 0x2B10;

	private const int DescendSpeed = 0x100;

	private const int MoveSpeed = 0xC8;

	/// <summary>The hammer hits when the sine counter of the hover has this value.</summary>
	private const int HammerSineCount = 0xC0;

	private const int HammerTicks = 0x1E;

	/// <summary>The hammer hits the pillar and starts to hurt when the countdown has this value.</summary>
	private const int HammerHitTick = 0x14;

	private const int InvincibleTicks = 64;

	private const int DefeatScore = 1000;

	private const int DefeatTicks = 0xB3;

	/// <summary>The hammer falls when the defeat countdown is at this value or less.</summary>
	private const int DropHammerTick = 0x78;

	private const int HammerStartVelocity = -0x380;

	private const int HammerGravity = 0x38;

	/// <summary>The hammer stops below the arena at this y position.</summary>
	private const int HammerBottom = 0x540;

	private const int RecoverStartCount = -0x12;

	private const int FleeSpeedX = 0x400;

	private const int FleeSpeedY = -0x40;

	private const int ExplosionInterval = 8;

	/// <summary>The explosions appear up to $20 pixels from Eggman.</summary>
	private const int ExplosionRange = 0x20;

	private const int BodyCollisionId = 0;

	private const int HammerCollisionId = 1;

	// Mapping frames of Obj89_MapUnc_30E04
	private const int FrameFaceDefeated = 5;

	// Animation scripts of Ani_obj89_b: the speed, then the mapping frames and the commands ($FF loop, $FD change)
	private const int AnimationFace = 0;

	private const int AnimationFlames = 1;

	private const int AnimationHammer = 2;

	private const int AnimationHammerSwing = 3;

	private const int AnimationBody = 4;

	private const int AnimationFaceDefeated = 5;

	private static readonly byte[][] Scripts =
	{
		new byte[] { 7, 0, 1, 0xFF, 2, 3, 2, 3, 2, 3, 2, 3, 0xFF, 4, 4, 4, 4, 4, 4, 4, 4, 0xFF },
		new byte[] { 1, 6, 7, 0xFF },
		new byte[] { 0xF, 9, 0xFF },
		new byte[] { 2, 0xA, 0xA, 0xB, 0xB, 0xB, 0xB, 0xB, 0xA, 0xA, 0xFD, 2 },
		new byte[] { 0xF, 8, 0xFF },
		new byte[] { 7, 5, 0xFF }
	};

	private readonly PartAnimation _body = new PartAnimation(AnimationBody, 8);

	private readonly PartAnimation _face = new PartAnimation(AnimationFace, 0);

	private readonly PartAnimation _hammer = new PartAnimation(AnimationHammer, 9);

	private readonly PartAnimation _flames = new PartAnimation(AnimationFlames, 6);

	private AnimationInstance _animation;

	private readonly List<Pillar> _pillars = new List<Pillar>();

	private State _state;

	/// <summary>Boss_X_pos and Boss_Y_pos: the position without the hover.</summary>
	private double _x = StartX;

	private double _y = StartY;

	/// <summary>Boss_X_vel and Boss_Y_vel, in 1/256 pixels for each frame.</summary>
	private int _velocityX;

	private int _velocityY = DescendSpeed;

	/// <summary>y_pos: the position with the hover.</summary>
	private int _drawY = StartY;

	private int _sineCount;

	private bool _facingRight;

	/// <summary>obj89_target: Eggman flies to the left pillar next.</summary>
	private bool _targetLeft;

	private int _countdown;

	private bool _hammerCollision;

	private bool _hammerDropped;

	private bool _hammerHidden;

	private double _hammerX;

	private double _hammerY;

	private int _hammerVelocityY = HammerStartVelocity;

	private int _fleeTicks;

	private bool _earthquake;

	private enum State
	{
		Wait,
		Descend,
		Move,
		AtPillar,
		Hammer,
		Exploding,
		Recover,
		Flee
	}

	/// <summary>The HD x position of the original (0, 0).</summary>
	[StateVariable]
	public int OriginX { get; set; }

	/// <summary>The HD y position of the original (0, 0).</summary>
	[StateVariable]
	public int OriginY { get; set; }

	/// <summary>
	/// obj89_hammer_flags bit 0: the hammer hit a pillar this cycle. The pillar on the side of the new target clears
	/// it and shoots an arrow.
	/// </summary>
	private bool HammerHitPillar { get; set; }

	private bool IsBeaten => _state >= State.Exploding;

	public ARZBossInstance() => DesignBounds = new Rectanglei(-0x70 * Scale, -0x40 * Scale, 0xE0 * Scale, 0x80 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZBossType.AnimationGroupResourceKey));
		ExplosionResourceKey = Type.GetAbsolutePath(ARZBossType.BossExplosionResourceKey);
		HitSoundResourceKey = Type.GetAbsolutePath(ARZBossType.BossHitSoundResourceKey);
		LockLifetime = true;
		if (OriginX == 0 && OriginY == 0)
		{
			// Without an origin, take the spawn position as the start position of the original
			OriginX = Position.X - StartX * Scale;
			OriginY = Position.Y - StartY * Scale;
		}
		UpdatePosition();
	}

	protected override void OnStop()
	{
		if (_earthquake)
		{
			Level.EarthquakeActive = false;
		}
	}

	/// <summary>Converts a position of the original to an HD position.</summary>
	private Vector2 ToHd(double x, double y) => new Vector2(OriginX + x * Scale, OriginY + y * Scale);

	protected override void OnUpdate()
	{
		base.OnUpdate();
		switch (_state)
		{
			case State.Wait:
				if (!ReadyToStart())
				{
					return;
				}
				RaisePillars();
				break;
			case State.Descend:
				MoveBoss();
				UpdateHover();
				if ((int)Math.Floor(_y) >= HoverY)
				{
					_y = HoverY;
					_state = State.Move;
					_velocityY = 0;
					_velocityX = -MoveSpeed;
					_targetLeft = true;
				}
				break;
			case State.Move:
				MoveBoss();
				UpdateHover();
				int x = (int)Math.Floor(_x);
				if (_targetLeft ? x <= LeftTargetX : x >= RightTargetX)
				{
					_state = State.AtPillar;
					_velocityX = 0;
				}
				break;
			case State.AtPillar:
				MoveBoss();
				UpdateHover();
				if (_sineCount == HammerSineCount)
				{
					// Swing the hammer. The new target is the other pillar.
					_hammer.Change(AnimationHammerSwing);
					_state = State.Hammer;
					_targetLeft = _facingRight;
					_countdown = HammerTicks;
				}
				break;
			case State.Hammer:
				if (_countdown == HammerHitTick)
				{
					HammerHitPillar = true;
					_hammerCollision = true;
				}
				if (--_countdown < 0)
				{
					_hammerCollision = false;
					_state = State.Move;
					_facingRight = !_facingRight;
					_velocityX = _facingRight ? MoveSpeed : -MoveSpeed;
				}
				MoveBoss();
				UpdateHover();
				break;
			case State.Exploding:
				if (--_countdown < 0)
				{
					StartRecovery();
				}
				else if (Level.Ticks % ExplosionInterval == 0)
				{
					var offset = new Vector2(Level.Random.Next(-ExplosionRange, ExplosionRange), Level.Random.Next(-ExplosionRange, ExplosionRange));
					ExplodeAt((Vector2i)ToHd(Math.Floor(_x) + offset.X, Math.Floor(_y) + offset.Y));
				}
				_drawY = (int)Math.Floor(_y);
				break;
			case State.Recover:
				UpdateRecovery();
				break;
			case State.Flee:
				_velocityX = FleeSpeedX;
				_velocityY = FleeSpeedY;
				MoveBoss();
				_sineCount = (_sineCount + 2) & 0xFF;
				_drawY = (int)Math.Floor(_y);
				_fleeTicks++;
				if (_fleeTicks > 60 && Position.X - DesignBounds.Width > Level.Camera.Bounds.Right)
				{
					FinishForever();
					return;
				}
				break;
		}
		if (_state < State.Exploding)
		{
			UpdateFace();
		}
		if (_hammerDropped)
		{
			DropHammer();
		}
		UpdatePosition();
		UpdateTouchAreas();
		UpdateEarthquake();
		_body.Step();
		_face.Step();
		_hammer.Step();
		_flames.Step();
	}

	/// <summary>
	/// Obj89_Init: with Sonic alone the fight starts at once. With Sonic and Tails it waits until both are in the
	/// arena, or until Tails flies back in.
	/// </summary>
	private bool ReadyToStart()
	{
		if (!Level.ObjectManager.Characters.Any(c => c.IsSidekick))
		{
			return true;
		}
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.IsSidekick && character.Respawning)
			{
				return true;
			}
			double x = (character.Position.X - OriginX) / (double)Scale;
			if (x < ArenaLeft || x > ArenaRight)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>Obj89_Init_RaisePillars: the pillars rise at each side of the arena and Eggman flies down.</summary>
	private void RaisePillars()
	{
		_state = State.Descend;
		Health = 8;
		_pillars.Add(CreatePillar(false));
		_pillars.Add(CreatePillar(true));
	}

	private Pillar CreatePillar(bool right)
	{
		Pillar pillar = Level.ObjectManager.AddSubObject<Pillar>(this);
		pillar.Setup(right);
		pillar.Priority = Priority - 2;
		return pillar;
	}

	/// <summary>Boss_MoveObject.</summary>
	private void MoveBoss()
	{
		_x += _velocityX / 256.0;
		_y += _velocityY / 256.0;
	}

	/// <summary>The hover of Obj89_Main_HandleHoveringAndHits: up to 4 pixels up and down.</summary>
	private void UpdateHover()
	{
		_drawY = (int)Math.Floor(_y) + (OriginalMath.Sin(_sineCount) >> 6);
		_sineCount = (_sineCount + 2) & 0xFF;
	}

	/// <summary>Obj89_Main_HandleFace: Eggman laughs while a character is hurt.</summary>
	private void UpdateFace()
	{
		if (Level.ObjectManager.Characters.Any(c => c.IsHurt))
		{
			// The original sets the laugh again each frame, so the laugh starts when the character lands
			_face.SetFrame(3, 1);
		}
	}

	private void UpdatePosition()
	{
		PositionPrecise = ToHd(Math.Floor(_x), _drawY);
	}

	private void UpdateTouchAreas()
	{
		var rectangles = new List<CollisionRectangle>();
		if (_state != State.Wait && !IsBeaten)
		{
			if (InvincibilityTimer <= 0)
			{
				// Touch size $F: $18 by $18
				rectangles.Add(new CollisionRectangle(this, BodyCollisionId, -0x18 * Scale, -0x18 * Scale, 0x30 * Scale, 0x30 * Scale));
			}
			if (_hammerCollision)
			{
				// BossCollision_ARZ: $10 by $14 around the hammer head, $50 pixels in front of Eggman
				int centreX = (_facingRight ? 0x50 : -0x50) * Scale;
				rectangles.Add(new CollisionRectangle(this, HammerCollisionId, centreX - 0x10 * Scale, (4 - 0x14) * Scale, 0x20 * Scale, 0x28 * Scale));
			}
		}
		CollisionRectangles = rectangles.ToArray();
	}

	private void UpdateEarthquake()
	{
		bool earthquake = _pillars.Any(p => p.Moving);
		if (earthquake != _earthquake)
		{
			_earthquake = earthquake;
			Level.EarthquakeActive = earthquake;
		}
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		if (e.Id == HammerCollisionId)
		{
			var character = (ICharacter)e.ActiveObject;
			if (character.CanBeHurt)
			{
				int hammerX = Position.X + (_facingRight ? 0x50 : -0x50) * Scale;
				character.Hurt(Math.Sign(character.Position.X - hammerX));
			}
			return;
		}
		base.OnCollision(e);
	}

	protected override void Hit(ICharacter character)
	{
		if (IsBeaten)
		{
			return;
		}
		base.Hit(character);
		if (Health > 0)
		{
			InvincibilityTimer = InvincibleTicks;
			// The hurt face (anim_frame $C) shows for 4 frames, then the anim_frame nybble wraps to the normal face
			_face.SetFrame(0xC, 0);
		}
	}

	/// <summary>Obj89_Main_KillBoss.</summary>
	protected override void Defeat()
	{
		Defeated = true;
		Level.Player.GainScore(DefeatScore);
		_state = State.Exploding;
		_countdown = DefeatTicks;
		_hammerCollision = false;
		_face.Set(AnimationFaceDefeated, FrameFaceDefeated);
		CollisionRectangles = new CollisionRectangle[0];
	}

	/// <summary>Obj89_Main_SetupEscapeAnim: Eggman turns to the right, drops a little and rises again.</summary>
	private void StartRecovery()
	{
		_face.Set(AnimationFace, _face.MappingFrame);
		_facingRight = true;
		_hammerHidden = true;
		_velocityX = 0;
		_velocityY = 0;
		_state = State.Recover;
		_countdown = RecoverStartCount;
	}

	/// <summary>Obj89_Main_SubA.</summary>
	private void UpdateRecovery()
	{
		_countdown++;
		if (_countdown < 0)
		{
			_velocityY += 0x18;
		}
		else if (_countdown == 0 || _countdown == 0x18)
		{
			_velocityY = 0;
		}
		else if (_countdown < 0x18)
		{
			_velocityY -= 8;
		}
		else if (_countdown >= 0x20)
		{
			_state = State.Flee;
			Fleeing = true;
		}
		MoveBoss();
		_sineCount = (_sineCount + 2) & 0xFF;
		_drawY = (int)Math.Floor(_y);
	}

	/// <summary>
	/// Obj89_Main_DropHammer: after the defeat the hammer falls away from Eggman with gravity $38. The original always
	/// moves it to the left. This port moves it away from Eggman, as the fix in s2disasm does.
	/// </summary>
	private void DropHammer()
	{
		if (_hammerHidden || _countdown > DropHammerTick && _state == State.Exploding)
		{
			return;
		}
		_hammerX += _facingRight ? 1 : -1;
		_hammerY += _hammerVelocityY / 256.0;
		_hammerVelocityY += HammerGravity;
		if (_hammerY >= HammerBottom)
		{
			_hammerHidden = true;
		}
	}

	protected override void OnUpdatePrepare()
	{
		base.OnUpdatePrepare();
		if (IsBeaten && !_hammerDropped)
		{
			// The hammer stays where Eggman was when he was defeated
			_hammerDropped = true;
			_hammerX = Math.Floor(_x);
			_hammerY = _drawY;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (_state == State.Wait)
		{
			return;
		}
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			// The art faces left. All parts turn around Eggman.
			if (_facingRight)
			{
				objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			}
			// From back to front: the jet flames, the hammer, Eggman, the Egg Mobile
			bool flash = IsInvincibleFlashing;
			if (flash)
			{
				objectRenderer.AdditiveColour = FlashAdditiveColour;
			}
			RenderFrame(objectRenderer, _flames.MappingFrame, new Vector2());
			if (!_hammerHidden)
			{
				var offset = new Vector2();
				if (_hammerDropped)
				{
					offset = ToHd(_hammerX, _hammerY) - PositionPrecise;
					if (_facingRight)
					{
						offset = new Vector2(-offset.X, offset.Y);
					}
				}
				RenderFrame(objectRenderer, _hammer.MappingFrame, offset);
			}
			objectRenderer.AdditiveColour = Colours.Black;
			RenderFrame(objectRenderer, _face.MappingFrame, new Vector2());
			if (flash)
			{
				objectRenderer.AdditiveColour = FlashAdditiveColour;
			}
			RenderFrame(objectRenderer, _body.MappingFrame, new Vector2());
			objectRenderer.AdditiveColour = Colours.Black;
		}
	}

	private void RenderFrame(IObjectRenderer objectRenderer, int frame, Vector2 offset)
	{
		_animation.Index = frame;
		objectRenderer.Render(_animation, offset);
	}

	/// <summary>
	/// One entry of Boss_AnimationArray and the AnimateBoss logic for it. An entry has two animation ids: when they
	/// differ, the animation starts again. The frame and the timer are 4-bit values, as in the original.
	/// </summary>
	private sealed class PartAnimation
	{
		private int _previousId;

		private int _id;

		private int _frame;

		private int _timer;

		public PartAnimation(int id, int mappingFrame)
		{
			_previousId = id;
			_id = id;
			MappingFrame = mappingFrame;
		}

		public int MappingFrame { get; private set; }

		/// <summary>Changes the second id only, so the animation starts again on the next step.</summary>
		public void Change(int id) => _id = id;

		/// <summary>Writes both ids and clears the frame and the timer.</summary>
		public void Set(int id, int mappingFrame)
		{
			_previousId = 0;
			_id = id;
			_frame = 0;
			_timer = 0;
			MappingFrame = mappingFrame;
		}

		/// <summary>Writes the frame and the timer byte, as the laugh and the hurt face do.</summary>
		public void SetFrame(int frame, int timer)
		{
			_frame = frame;
			_timer = timer;
		}

		public void Step()
		{
			if (_previousId != _id)
			{
				_frame = 0;
				_timer = 0;
			}
			_previousId = _id;
			if (--_timer >= 0)
			{
				return;
			}
			byte[] script = Scripts[_id];
			_timer = script[0] & 0xF;
			int value = script[1 + _frame];
			if (value == 0xFD)
			{
				// Change to the animation in the next byte on the next step
				_id = script[2 + _frame];
				return;
			}
			if (value == 0xFF)
			{
				_frame = 0;
				value = script[1];
			}
			MappingFrame = value & 0x7F;
			_frame = (_frame + 1) & 0xF;
		}
	}

	/// <summary>
	/// One of the two pillars (Obj89_Pillar). It rises from the floor at the start and is solid. When the hammer hits
	/// it, it shakes for $1F frames and shoots an arrow from its eyes. After the defeat it goes back into the floor.
	/// The left pillar is at $2A50 and the right pillar, which is mirrored, at $2B70.
	/// </summary>
	private sealed class Pillar : ActiveObject
	{
		private const int LeftX = 0x2A50;

		private const int RightX = 0x2B70;

		private const int LowY = 0x510;

		private const int StandY = 0x488;

		private const int ShakeTicks = 0x1F;

		private const int EyesTicks = 0x28;

		/// <summary>The eyes appear at these x positions, then at one of the heights.</summary>
		private const int LeftEyesX = 0x2A6A;

		private const int RightEyesX = 0x2B56;

		private static readonly int[] EyesHeights = { 0x458, 0x478, 0x498, 0x4B8 };

		/// <summary>The arrow starts this distance below the eyes.</summary>
		private const int ArrowOffsetY = 9;

		private const int FramePillar = 0;

		private const int FrameEyes = 2;

		private AnimationInstance _animation;

		private bool _right;

		private int _x;

		private int _y = LowY;

		private PillarState _state;

		private bool _shaking;

		private int _shakeTimer;

		private int _eyesTimer;

		private int _eyesY;

		private enum PillarState
		{
			Rise,
			Stand,
			Lower
		}

		private ARZBossInstance Boss => (ARZBossInstance)ParentObject;

		/// <summary>The pillar rises or goes down. The screen shakes.</summary>
		public bool Moving => _state != PillarState.Stand && !Finished;

		protected override void OnStart()
		{
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZBossType.PillarAnimationGroupResourceKey));
			LockLifetime = true;
			DesignBounds = new Rectanglei(-0x30 * Scale, -0x50 * Scale, 0x60 * Scale, 0xA0 * Scale);
		}

		public void Setup(bool right)
		{
			_right = right;
			_x = right ? RightX : LeftX;
			// Obj89_Pillar_SolidObject: $18 pixels at each side and $44 above and below a point 4 pixels lower
			CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-0x18 * Scale, (4 - 0x44) * Scale, 0x30 * Scale, 0x88 * Scale));
			RegisterCollisionUpdate();
			UpdatePosition();
		}

		private void UpdatePosition()
		{
			PositionPrecise = Boss.ToHd(_x, _y);
		}

		protected override void OnUpdate()
		{
			if (Boss.IsBeaten && _state != PillarState.Lower)
			{
				StartLowering();
			}
			switch (_state)
			{
				case PillarState.Rise:
					if (--_y <= StandY)
					{
						_state = PillarState.Stand;
					}
					break;
				case PillarState.Stand:
					CheckHammer();
					UpdateShake();
					break;
				case PillarState.Lower:
					if (++_y >= LowY)
					{
						Finish();
					}
					break;
			}
			if (_eyesTimer > 0)
			{
				_eyesTimer--;
			}
			UpdatePosition();
		}

		/// <summary>The pillar on the side of the new target is the pillar that the hammer hit.</summary>
		private void CheckHammer()
		{
			if (!Boss.HammerHitPillar || Boss._targetLeft != _right)
			{
				return;
			}
			Boss.HammerHitPillar = false;
			Shoot();
			_shaking = true;
		}

		/// <summary>Obj89_Pillar_ChkShake: the pillar moves 1 pixel down and right, then up and left, each frame.</summary>
		private void UpdateShake()
		{
			if (!_shaking)
			{
				return;
			}
			if (_shakeTimer <= 0)
			{
				_shakeTimer = ShakeTicks;
			}
			int baseX = _right ? RightX : LeftX;
			if (--_shakeTimer > 0)
			{
				int offset = Level.Ticks % 2 == 0 ? 1 : -1;
				_x = baseX + offset;
				_y = StandY + offset;
				return;
			}
			_shaking = false;
			_shakeTimer = 0;
			_x = baseX;
			_y = StandY;
		}

		/// <summary>Obj89_Pillar_Shoot: the eyes bulge at a random height and an arrow flies to the other pillar.</summary>
		private void Shoot()
		{
			_eyesTimer = EyesTicks;
			_eyesY = EyesHeights[Level.Random.Next(EyesHeights.Length)];
			int eyesX = _right ? RightEyesX : LeftEyesX;
			Arrow arrow = Level.ObjectManager.AddSubObject<Arrow>(Boss);
			arrow.Setup(eyesX, _eyesY + ArrowOffsetY, _right);
			arrow.Priority = Priority - 1;
		}

		private void StartLowering()
		{
			_state = PillarState.Lower;
			_shaking = false;
			_x = _right ? RightX : LeftX;
			// The original stops the solid test here, so characters on the pillar fall
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (character.ObjectLink == this)
				{
					character.LeaveGround();
					character.IsAirborne = true;
				}
			}
			CollisionVectors = new CollisionVector[0];
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			using (objectRenderer.BeginMatixState())
			{
				if (_right)
				{
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				}
				_animation.Index = FramePillar;
				objectRenderer.Render(_animation, new Vector2());
				if (_eyesTimer > 0)
				{
					int eyesX = _right ? RightEyesX : LeftEyesX;
					Vector2 offset = Boss.ToHd(eyesX, _eyesY) - PositionPrecise;
					if (_right)
					{
						offset = new Vector2(-offset.X, offset.Y);
					}
					_animation.Index = FrameEyes;
					objectRenderer.Render(_animation, offset);
				}
			}
		}
	}

	/// <summary>
	/// An arrow from a pillar (Obj89_Arrow). It flies 3 pixels each frame and hurts characters. It sticks in the
	/// other pillar and shakes, and then a character can stand on it. It falls $1F frames after a character stands
	/// on it, after its animation ends, or when Eggman is defeated.
	/// </summary>
	private sealed class Arrow : ActiveObject
	{
		private const int Speed = 3;

		private const int FallSpeed = 4;

		/// <summary>The arrow sticks at these x positions in the left and the right pillar.</summary>
		private const int LeftStopX = 0x2A77;

		private const int RightStopX = 0x2B49;

		private const int GroundY = 0x4F0;

		private const int StandTicks = 0x1F;

		private const int FrameArrow = 4;

		// Ani_obj89_a: the shake when the arrow sticks, then a long wait. $F9 makes the arrow fall.
		private static readonly byte[][] ArrowScripts =
		{
			new byte[] { 1, 4, 6, 5, 4, 6, 4, 5, 4, 6, 4, 4, 6, 5, 4, 6, 4, 5, 4, 6, 4, 0xFD, 1 },
			new byte[]
			{
				0xF, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 0xF9
			}
		};

		private AnimationInstance _animation;

		private int _x;

		private int _y;

		private bool _left;

		private bool _harmful;

		private ArrowState _state;

		private int _standTimer;

		private int _frame = FrameArrow;

		private int _script;

		private int _previousScript;

		private int _scriptFrame;

		private int _scriptTimer;

		private enum ArrowState
		{
			Fly,
			Stuck,
			Fall
		}

		private ARZBossInstance Boss => (ARZBossInstance)ParentObject;

		protected override void OnStart()
		{
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZBossType.PillarAnimationGroupResourceKey));
			LockLifetime = true;
			DesignBounds = new Rectanglei(-0x18 * Scale, -0x10 * Scale, 0x30 * Scale, 0x20 * Scale);
		}

		/// <param name="left">The arrow comes from the right pillar and flies to the left.</param>
		public void Setup(int x, int y, bool left)
		{
			_x = x;
			_y = y;
			_left = left;
			_harmful = true;
			UpdatePosition();
			UpdateTouchAreas();
		}

		private void UpdatePosition()
		{
			PositionPrecise = Boss.ToHd(_x, _y);
		}

		private void UpdateTouchAreas()
		{
			// Touch size $30: $10 by 1
			CollisionRectangles = _harmful
				? new[] { new CollisionRectangle(this, 0, -0x10 * Scale, -1 * Scale, 0x20 * Scale, 2 * Scale) }
				: new CollisionRectangle[0];
		}

		protected override void OnUpdate()
		{
			if (Boss.IsBeaten && _state != ArrowState.Fall)
			{
				StartFalling();
			}
			switch (_state)
			{
				case ArrowState.Fly:
				{
					int x = _x + (_left ? -Speed : Speed);
					if (_left ? x <= LeftStopX : x >= RightStopX)
					{
						x = _left ? LeftStopX : RightStopX;
						_state = ArrowState.Stuck;
					}
					_x = x;
					break;
				}
				case ArrowState.Stuck:
					if (_harmful)
					{
						// The arrow is harmless from the first frame that it is stuck. Its top is 2 pixels above it.
						_harmful = false;
						CollisionVectors = new[] { new CollisionVector(this, new Vector2i(-0x10 * Scale, -2 * Scale), new Vector2i(0x10 * Scale, -2 * Scale)) };
						RegisterCollisionUpdate();
					}
					UpdateStanding();
					if (_state == ArrowState.Stuck)
					{
						AnimateArrow();
					}
					break;
				case ArrowState.Fall:
					_y += FallSpeed;
					if (_y > GroundY)
					{
						Finish();
						return;
					}
					break;
			}
			UpdatePosition();
			UpdateTouchAreas();
		}

		/// <summary>
		/// Obj89_Arrow_Platform. The original starts the timer only for Sonic and stops the platform test while the
		/// timer runs. This port follows the fix in s2disasm: any character starts it and the platform stays solid.
		/// </summary>
		private void UpdateStanding()
		{
			if (_standTimer == 0 && Level.ObjectManager.Characters.Any(c => c.ObjectLink == this))
			{
				_standTimer = StandTicks;
			}
			if (_standTimer > 0 && --_standTimer == 0)
			{
				StartFalling();
			}
		}

		/// <summary>AnimateSprite with Ani_obj89_a.</summary>
		private void AnimateArrow()
		{
			if (_script != _previousScript)
			{
				_previousScript = _script;
				_scriptFrame = 0;
				_scriptTimer = 0;
			}
			if (--_scriptTimer >= 0)
			{
				return;
			}
			byte[] script = ArrowScripts[_script];
			_scriptTimer = script[0];
			int value = script[1 + _scriptFrame];
			switch (value)
			{
				case 0xFD:
					_script = script[2 + _scriptFrame];
					return;
				case 0xF9:
					StartFalling();
					return;
				case 0xFF:
					_scriptFrame = 0;
					value = script[1];
					break;
			}
			_frame = value & 0x7F;
			_scriptFrame++;
		}

		/// <summary>Obj89_Arrow_ChkDropPlayers: the characters on the arrow fall, and the arrow falls 4 pixels each frame.</summary>
		private void StartFalling()
		{
			_state = ArrowState.Fall;
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (character.ObjectLink == this)
				{
					character.LeaveGround();
					character.IsAirborne = true;
				}
			}
			CollisionVectors = new CollisionVector[0];
		}

		protected override void OnCollision(CollisionEvent e)
		{
			if (!_harmful || e.ActiveObject.Type.Classification != ObjectClassification.Character)
			{
				return;
			}
			var character = (ICharacter)e.ActiveObject;
			if (character.CanBeHurt)
			{
				character.Hurt(Math.Sign(character.Position.X - Position.X));
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			// The art faces right. The frames are centred on the arrow.
			_animation.Index = _frame;
			renderer.GetObjectRenderer().Render(_animation, _left);
		}
	}
}
