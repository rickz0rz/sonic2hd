using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.KNUCKLES;

/// <summary>
/// Knuckles, a playable character. The Character base class has the movement. Knuckles jumps lower than Sonic: $600,
/// and $300 under water (KiS2). The sprites are the original sprites of KiS2, at four times their size.
/// <para>
/// A second press of jump in the air starts a glide (Knuckles_BeginGlide). The glide follows the routines of KiS2:
/// Knuckles_GlideSpeedControl gives the speed and the turn, Knuckles_GlideControl ends the glide. When the player lets
/// go of jump, Knuckles falls. On a flat floor he slides, and he gets up when he stops or the player lets go of jump.
/// When he falls from a glide onto a flat floor, he lands hard. A glide and a slide break monitors and enemies. The
/// values of the original are in subpixels: 256 make one pixel, and one pixel is 4 level units, so a value of the
/// original is 64 times its value here.
/// </para>
/// <para>
/// A glide into a wall starts a climb (Knuckles_BeginClimb). Up and down move Knuckles one pixel each frame, two for
/// Super Knuckles (Knuckles_Climbing_Wall). At the top of the wall he climbs onto the ledge
/// (Knuckles_DoLedgeClimbingAnimation). At the bottom of the wall he lets go, and on a floor he stands. Jump makes
/// him jump away from the wall.
/// </para>
/// </summary>
public class KnucklesInstance : Character
{
	/// <summary>The jump speed of Knuckles, $600, in level units.</summary>
	private const double KnucklesJumpForce = 0x600 / 64.0;

	/// <summary>Super Knuckles moves slower than Super Sonic (Knuckles_TurnSuper): top speed $800, acceleration $18 and deceleration $C0.</summary>
	private const double SuperKnucklesTopSpeed = 0x800 / 64.0;

	private const double SuperKnucklesAcceleration = 0x18 / 64.0;

	private const double SuperKnucklesDeceleration = 0xC0 / 64.0;

	/// <summary>The transformation into Super Knuckles (KnucklesAni_Transform).</summary>
	private const int AnimationTransform = 30;

	/// <summary>
	/// The palette cycle of Super Knuckles (PalCycle_SuperSonic in KiS2). Knuckles_TurnSuper sets Palette_timer to $F.
	/// The fade-in only marks the cycle as started, so it shows the normal colours (row 0) once. Each frame of the
	/// cycle shows for 3 ticks, and the last frame for 15 ticks.
	/// </summary>
	private static readonly SONICORCA.OBJECTS.SONIC.SuperSonicPalette.Schedule SuperKnucklesSchedule =
		new SONICORCA.OBJECTS.SONIC.SuperSonicPalette.Schedule(1, 10, 15, 2, 2, LastFrameTimer: 14);

	/// <summary>The palette cycle of Super Knuckles, or null before he first becomes Super Knuckles or without the palette.</summary>
	private SONICORCA.OBJECTS.SONIC.SuperSonicPalette _superPalette;

	private bool _wasSuper;

	/// <summary>The jump speed of Knuckles under water, $300, in level units.</summary>
	private const double KnucklesUnderwaterJumpForce = 0x300 / 64.0;

	/// <summary>
	/// The height radius of the original Knuckles, at four times the size: y_radius $13, and $E in a roll. The
	/// original sprites line up with these.
	/// </summary>
	private const int OriginalCollisionRadiusY = 76;

	private const int OriginalSpinballCollisionRadiusY = 56;

	/// <summary>The speed at the start of a glide, $400. Below it, the speed grows by 8 each frame.</summary>
	private const double GlideStartSpeed = 0x400 / 64.0;

	private const double GlideSlowAcceleration = 8 / 64.0;

	/// <summary>Above the start speed, the speed grows by 4 each frame up to $1800, but not in a turn.</summary>
	private const double GlideAcceleration = 4 / 64.0;

	private const double GlideTopSpeed = 0x1800 / 64.0;

	/// <summary>Super Knuckles glides faster: 8 more each frame.</summary>
	private const double SuperGlideAcceleration = 8 / 64.0;

	/// <summary>A glide starts with $200 more vertical speed, at least 0.</summary>
	private const double GlideStartFall = 0x200 / 64.0;

	/// <summary>The vertical speed of a glide moves by $20 each frame to $80.</summary>
	private const double GlideFallSpeed = 0x80 / 64.0;

	private const double GlideFallChange = 0x20 / 64.0;

	/// <summary>A slide loses $20 of speed each frame.</summary>
	private const double SlideFriction = 0x20 / 64.0;

	/// <summary>move_lock after a hard landing and after a slide: $F frames.</summary>
	private const int LandingLockTicks = 0x0F;

	/// <summary>The turn of a glide changes by 2 each frame. 0 is to the right and $80 is to the left.</summary>
	private const int TurnStep = 2;

	private const int TurnLeft = 0x80;

	/// <summary>The animations of KnucklesArt after the 30 values of CharacterAnimation.</summary>
	private const int AnimationGlide = 31;

	private const int AnimationFallFromGlide = 32;

	private const int AnimationHardFall = 33;

	private const int AnimationSlide = 34;

	private const int AnimationClimb = 35;

	private const int AnimationClimbLedge = 36;

	private const int AnimationGetUp = 37;

	/// <summary>The frames of the turn of a glide (Knuckles_DoGlidingAnimation), as frames of the glide animation. 4 is frame 0, flipped.</summary>
	private static readonly int[] GlideTurnFrames = { 0, 1, 2, 3, 4, 3, 2, 1 };

	/// <summary>One pixel of the original, in level units.</summary>
	private const double Pixel = 4.0;

	/// <summary>The radius of Knuckles on a wall: x_radius and y_radius 10.</summary>
	private const double ClimbRadius = 10 * Pixel;

	/// <summary>Knuckles_Climbing_Wall looks for the wall 11 pixels above and below his centre.</summary>
	private const double ClimbWallCheckY = 11 * Pixel;

	/// <summary>Knuckles_BeginClimb holds the top of a low wall that is less than 12 pixels down.</summary>
	private const double LowWallDistance = 12 * Pixel;

	/// <summary>Above this distance to the wall, the wall ends above Knuckles, and he climbs onto the ledge.</summary>
	private const double LedgeDistance = 4 * Pixel;

	/// <summary>The climb frame changes each 4 frames (double_jump_property counts from 3).</summary>
	private const int ClimbFrameTicks = 4;

	private const int ClimbFrames = 6;

	/// <summary>The jump from a wall: $400 away from the wall and $380 up.</summary>
	private const double WallJumpSpeedX = 0x400 / 64.0;

	private const double WallJumpSpeedY = 0x380 / 64.0;

	/// <summary>
	/// The steps of the climb onto a ledge (Knuckles_DoLedgeClimbingAnimation): the frame of ClimbLedge (-1 for the
	/// stand), the move in pixels towards the wall and up, and the frames that the step lasts.
	/// </summary>
	private static readonly (int Frame, int X, int Y, int Ticks)[] LedgeSteps =
	{
		(0, 3, -3, 6),
		(1, 8, -10, 6),
		(2, -8, -12, 6),
		(-1, 8, -5, 6)
	};

	/// <summary>The states of double_jump_flag in KiS2.</summary>
	private enum GlideState
	{
		None,
		Glide,
		Fall,
		Slide,
		Climb,
		ClimbLedge
	}

	private GlideState _state;

	/// <summary>double_jump_property: the direction of the glide, from 0 (right) to $80 (left), as a byte.</summary>
	private int _turn;

	/// <summary>inertia in a glide: the speed along the direction of the glide.</summary>
	private double _glideSpeed;

	/// <summary>The animation after a glide while the move lock runs: the hard landing or the get up, or -1.</summary>
	private int _landingAnimation = -1;

	private int _landingLockTicks;

	/// <summary>x_sub on a wall: the X where Knuckles took hold of the wall. When something moves him, he lets go.</summary>
	private double _climbX;

	private int _climbFrame;

	private int _climbFrameTimer;

	/// <summary>The step of the climb onto a ledge and the frames left in it.</summary>
	private int _ledgeStep;

	private int _ledgeTimer;

	/// <summary>True in the frame of a jump from a wall, so that the same press does not start a glide.</summary>
	private bool _jumpedFromWall;

	protected override bool BecomesSuperAtJumpTop => false;

	[HideInEditor]
	public override bool IsGliding => _state == GlideState.Glide || _state == GlideState.Slide;

	protected override bool CanJumpFromGround => _state != GlideState.Slide;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath(KnucklesType.AnimationGroupResourceKey);
		NormalJumpForce = KnucklesJumpForce;
		UnderwaterJumpForce = KnucklesUnderwaterJumpForce;
		// Super Knuckles does not jump higher, and he is slower than Super Sonic (KiS2)
		SuperJumpForce = KnucklesJumpForce;
		SuperTopSpeed = SuperKnucklesTopSpeed;
		SuperAcceleration = SuperKnucklesAcceleration;
		SuperDeceleration = SuperKnucklesDeceleration;
		HasSuperAbility = true;
		if (Level.OriginalPhysics)
		{
			// The collision box of the original Knuckles: x_radius 9 and y_radius $13, and 7 and $E in a roll
			NormalCollisionRadius = new Vector2i(36, 76);
			SpinballCollisionRadius = new Vector2i(28, 56);
		}
		base.OnStart();
	}

	/// <summary>Knuckles_BeginGlide: a second press of jump in the air, when Knuckles does not go up fast.</summary>
	protected override bool OnAirJumpPressed()
	{
		if (_jumpedFromWall)
		{
			_jumpedFromWall = false;
			return true;
		}
		if (_state != GlideState.None || IsHurt || IsSidekick || Velocity.Y < -GlideStartSpeed)
			return false;
		// Sonic_CheckGoSuper of KiS2: the press makes Knuckles super when he can be, else it starts a glide
		if (TryBecomeSuper())
			return true;
		IsSpinball = false;
		IsJumping = false;
		_state = GlideState.Glide;
		Velocity = new Vector2(0.0, Math.Max(Velocity.Y + GlideStartFall, 0.0));
		_glideSpeed = GlideStartSpeed;
		_turn = IsFacingLeft ? TurnLeft : 0;
		ApplyGlideTurn();
		return true;
	}

	public override void StopGliding(bool fall = false)
	{
		if (_state != GlideState.Glide && _state != GlideState.Slide)
			return;
		if (fall)
			StartFall(divideSpeed: false);
		else
			_state = GlideState.None;
	}

	protected override bool UpdateOwnMovement()
	{
		_jumpedFromWall = false;
		if (IsHurt || IsDead)
		{
			_state = GlideState.None;
			_landingAnimation = -1;
			return false;
		}
		switch (_state)
		{
			case GlideState.Glide:
				return UpdateGlide();
			case GlideState.Fall:
				if (Mode != CollisionMode.Air)
					_state = GlideState.None;
				// Knuckles_FallingFromGlide: the normal air movement and gravity
				return false;
			case GlideState.Slide:
				return UpdateSlide();
			case GlideState.Climb:
				return UpdateClimb();
			case GlideState.ClimbLedge:
				return UpdateClimbLedge();
		}
		if (_landingLockTicks > 0)
		{
			if (Mode == CollisionMode.Air)
			{
				_landingLockTicks = 0;
				_landingAnimation = -1;
				return false;
			}
			// move_lock: Knuckles stays where he landed
			_landingLockTicks--;
			GroundVelocity = 0.0;
			if (_landingLockTicks == 0)
				_landingAnimation = -1;
			return true;
		}
		return false;
	}

	/// <summary>Knuckles_GlideControl and Knuckles_GlideSpeedControl for one frame.</summary>
	private bool UpdateGlide()
	{
		if (Mode != CollisionMode.Air)
		{
			_state = GlideState.None;
			return false;
		}
		// Knuckles_BeginClimb: a wall in front of Knuckles stops the glide, and he takes hold of it
		// The side of the glide comes from the turn: the collision stops the horizontal velocity at a wall
		int side = Math.Cos(_turn * Math.PI * 2.0 / 256.0) < 0.0 ? -1 : 1;
		if (TouchesWall(side))
		{
			if (!TryStartClimb(side))
				StartFall(divideSpeed: false);
			return false;
		}
		if (!Input.ABC.HasFlag(CharacterInputButtonState.Down))
		{
			StartFall(divideSpeed: true);
			return false;
		}
		// The speed grows to the top speed, but not in a turn
		if (_glideSpeed < GlideStartSpeed)
			_glideSpeed += GlideSlowAcceleration;
		else if (_glideSpeed < GlideTopSpeed && (_turn & 0x7F) == 0)
			_glideSpeed += GlideAcceleration + (IsSuper ? SuperGlideAcceleration : 0.0);
		_turn = GetNextTurn(_turn, Input.HorizontalDirection);
		// The fall of a glide moves to $80: a parachute when Knuckles falls fast
		double velocityY = Velocity.Y >= GlideFallSpeed ? Velocity.Y - GlideFallChange : Velocity.Y + GlideFallChange;
		Velocity = new Vector2(_glideSpeed * Math.Cos(_turn * Math.PI * 2.0 / 256.0), velocityY);
		ApplyGlideTurn();
		return true;
	}

	/// <summary>
	/// The turn of Knuckles_GlideSpeedControl: a held direction turns Knuckles to it by 2 each frame. With no direction
	/// held, a turn that started goes on to the end.
	/// </summary>
	private static int GetNextTurn(int turn, double horizontalDirection)
	{
		sbyte value = (sbyte)(byte)turn;
		if (horizontalDirection < 0.0)
		{
			if ((byte)value != TurnLeft)
			{
				if (value < 0)
					value = (sbyte)-value;
				value += TurnStep;
			}
		}
		else if (horizontalDirection > 0.0)
		{
			if (value != 0)
			{
				if (value > 0)
					value = (sbyte)-value;
				value += TurnStep;
			}
		}
		else if ((value & 0x7F) != 0)
		{
			value += TurnStep;
		}
		return (byte)value;
	}

	/// <summary>Knuckles_DoGlidingAnimation: Knuckles faces the camera in the middle of a turn, and left at the end of it.</summary>
	private void ApplyGlideTurn()
	{
		int frame = GlideTurnFrames[((_turn + 0x10) & 0xFF) >> 5];
		Facing = frame == 4 ? -1 : 1;
	}

	/// <summary>
	/// Knuckles_BeginClimb: Knuckles takes hold of a wall beside him, at his centre. He stops, faces the wall, and
	/// moves to touch it with his radius on a wall.
	/// </summary>
	private bool TryStartClimb(int side)
	{
		double? wall = GetWallDistance(side, PositionPrecise.X, PositionPrecise.Y, ClimbRadius + LedgeDistance);
		if (wall == null)
		{
			// .checkFloor: the top of a low wall within 12 pixels below a point 11 pixels above the centre. Knuckles
			// moves down to hold the top of the wall, and then climbs onto it.
			double edgeX = PositionPrecise.X + side * (ClimbRadius + Pixel);
			double? top = GetFloorDistance(1, edgeX, PositionPrecise.Y - ClimbWallCheckY, LowWallDistance);
			if (top == null || top.Value >= LowWallDistance)
				return false;
			PositionPrecise = new Vector2(PositionPrecise.X, PositionPrecise.Y + top.Value);
			wall = GetWallDistance(side, PositionPrecise.X, PositionPrecise.Y, ClimbRadius + LedgeDistance);
			if (wall == null)
				return false;
		}
		_state = GlideState.Climb;
		Facing = side;
		Velocity = new Vector2(0.0, 0.0);
		GroundVelocity = 0.0;
		PositionPrecise = new Vector2(PositionPrecise.X + side * (wall.Value - ClimbRadius), PositionPrecise.Y);
		_climbX = PositionPrecise.X;
		_climbFrame = 0;
		_climbFrameTimer = ClimbFrameTicks - 1;
		return true;
	}

	/// <summary>Knuckles_Climbing_Wall for one frame.</summary>
	private bool UpdateClimb()
	{
		if (Mode != CollisionMode.Air || Math.Abs(PositionPrecise.X - _climbX) >= 1.0)
		{
			// Something moved Knuckles, or he stands on something
			LetGoOfWall();
			return false;
		}
		Velocity = new Vector2(0.0, 0.0);
		GroundVelocity = 0.0;
		int side = Facing;
		double x = PositionPrecise.X;
		double y = PositionPrecise.Y;
		double step = IsSuper ? 2 * Pixel : Pixel;
		int frameDelta = 0;
		if (Input.VerticalDirection < 0.0)
		{
			// Up: at the top of the wall, Knuckles climbs onto the ledge
			double? wall = GetWallDistance(side, x, y - ClimbWallCheckY, ClimbRadius + LedgeDistance);
			if (wall == null || wall.Value - ClimbRadius >= LedgeDistance)
			{
				StartClimbLedge();
				return true;
			}
			if (wall.Value - ClimbRadius < 1.0)
			{
				double? ceiling = GetFloorDistance(-1, x, y - 8 * Pixel, ClimbRadius + step);
				if (ceiling == null || ceiling.Value >= ClimbRadius + step)
					PositionPrecise = new Vector2(x, y - step);
				frameDelta = 1;
			}
		}
		else if (Input.VerticalDirection > 0.0)
		{
			// Down: at the bottom of the wall, Knuckles lets go. On a floor, he stands.
			double? wall = GetWallDistance(side, x, y + ClimbWallCheckY, ClimbRadius + 1.0);
			if (wall == null)
			{
				LetGoOfWall();
				return false;
			}
			double? floor = GetFloorDistance(1, x, y + 9 * Pixel, ClimbRadius + step);
			if (floor != null && floor.Value < ClimbRadius + step)
			{
				// Sonic_ResetOnFloor_Part2: Knuckles stands, and the collision puts him on the floor
				_state = GlideState.None;
				PositionPrecise = new Vector2(x, y + 9 * Pixel + floor.Value - NormalCollisionRadius.Y);
				return true;
			}
			PositionPrecise = new Vector2(x, y + step);
			frameDelta = -1;
		}
		if (frameDelta != 0 && --_climbFrameTimer < 0)
		{
			_climbFrameTimer = ClimbFrameTicks - 1;
			_climbFrame = (_climbFrame + frameDelta + ClimbFrames) % ClimbFrames;
		}
		if (Input.ABC == CharacterInputButtonState.Pressed)
		{
			// A jump away from the wall, in a ball
			_state = GlideState.None;
			_jumpedFromWall = true;
			Facing = -side;
			Velocity = new Vector2(-side * WallJumpSpeedX, -WallJumpSpeedY);
			IsSpinball = true;
			Jumped = true;
			IsJumping = true;
			return true;
		}
		return true;
	}

	/// <summary>Knuckles_LetGoOfWall: Knuckles falls as after a glide.</summary>
	private void LetGoOfWall()
	{
		_state = Mode == CollisionMode.Air ? GlideState.Fall : GlideState.None;
	}

	private void StartClimbLedge()
	{
		_state = GlideState.ClimbLedge;
		_ledgeStep = -1;
		_ledgeTimer = 0;
		UpdateClimbLedge();
	}

	/// <summary>
	/// Knuckles_Climbing_Onto_Ledge: Knuckles moves up and onto the ledge in steps. At the end he stands, and the
	/// collision puts him on the ledge.
	/// </summary>
	private bool UpdateClimbLedge()
	{
		Velocity = new Vector2(0.0, 0.0);
		GroundVelocity = 0.0;
		if (--_ledgeTimer > 0)
			return true;
		if (++_ledgeStep >= LedgeSteps.Length)
		{
			_state = GlideState.None;
			return true;
		}
		(int _, int moveX, int moveY, int ticks) = LedgeSteps[_ledgeStep];
		PositionPrecise += new Vector2(Facing * moveX * Pixel, moveY * Pixel);
		_ledgeTimer = ticks;
		return true;
	}

	/// <summary>The fall after a glide. When the player lets go of jump, the horizontal speed is divided by 4.</summary>
	private void StartFall(bool divideSpeed)
	{
		_state = Mode == CollisionMode.Air ? GlideState.Fall : GlideState.None;
		Facing = Velocity.X < 0.0 ? -1 : 1;
		if (divideSpeed)
			Velocity = new Vector2(Velocity.X / 4.0, Velocity.Y);
		if (Mode != CollisionMode.Air)
			GroundVelocity = 0.0;
	}

	/// <summary>Knuckles_Sliding: the slide loses speed until Knuckles stops or the player lets go of jump.</summary>
	private bool UpdateSlide()
	{
		if (Mode == CollisionMode.Air)
		{
			// Knuckles slid off a ledge
			_state = GlideState.Fall;
			return false;
		}
		double speed = GroundVelocity;
		double slowed = speed > 0.0 ? speed - SlideFriction : speed + SlideFriction;
		if (!Input.ABC.HasFlag(CharacterInputButtonState.Down) || Math.Sign(slowed) != Math.Sign(speed) || slowed == 0.0)
		{
			_state = GlideState.None;
			StartLandingLock(AnimationGetUp);
			return true;
		}
		GroundVelocity = slowed;
		return true;
	}

	/// <summary>
	/// Knuckles_BeginSlide and Knuckles_FallingFromGlide: on a flat floor (an angle within 45 degrees), a glide becomes
	/// a slide, and a fall becomes a hard landing. On a steeper floor, Knuckles lands as normal.
	/// </summary>
	protected override void OnLanded()
	{
		bool flat = Math.Abs(Math.Sin(GroundAngle)) < Math.Sqrt(0.5) && Math.Cos(GroundAngle) > 0.0;
		switch (_state)
		{
			case GlideState.Glide:
				_state = flat ? GlideState.Slide : GlideState.None;
				if (flat)
					Facing = Velocity.X < 0.0 ? -1 : 1;
				break;
			case GlideState.Fall:
				_state = GlideState.None;
				if (flat)
					StartLandingLock(AnimationHardFall);
				break;
		}
	}

	private void StartLandingLock(int animation)
	{
		GroundVelocity = 0.0;
		Velocity = new Vector2(0.0, 0.0);
		_landingAnimation = animation;
		_landingLockTicks = LandingLockTicks;
	}

	protected override void OnStop()
	{
		base.OnStop();
		_superPalette?.Dispose();
		_superPalette = null;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (IsSuper && !_wasSuper)
		{
			_superPalette ??= SONICORCA.OBJECTS.SONIC.SuperSonicPalette.Create(Level.GameContext.Window.GraphicsContext, Animation.AnimationGroup, SuperKnucklesSchedule);
			_superPalette?.Start();
		}
		_wasSuper = IsSuper;
		if (IsSuper)
			_superPalette?.Update();
	}

	protected override void OnAnimate()
	{
		base.OnAnimate();
		if (IsHurt || IsDead)
			return;
		if (IsTransformingToSuper)
		{
			SetAnimation(AnimationTransform);
			return;
		}
		switch (_state)
		{
			case GlideState.Glide:
				SetAnimation(AnimationGlide);
				int frame = GlideTurnFrames[((_turn + 0x10) & 0xFF) >> 5];
				Animation.CurrentFrameIndex = frame == 4 ? 0 : frame;
				return;
			case GlideState.Fall:
				SetAnimation(AnimationFallFromGlide);
				return;
			case GlideState.Slide:
				SetAnimation(AnimationSlide);
				return;
			case GlideState.Climb:
				SetAnimation(AnimationClimb);
				Animation.CurrentFrameIndex = _climbFrame;
				return;
			case GlideState.ClimbLedge:
				int ledgeFrame = LedgeSteps[Math.Max(_ledgeStep, 0)].Frame;
				if (ledgeFrame < 0)
					return;
				SetAnimation(AnimationClimbLedge);
				Animation.CurrentFrameIndex = ledgeFrame;
				return;
		}
		if (_landingAnimation >= 0)
			SetAnimation(_landingAnimation);
	}

	/// <summary>Shows an animation of Knuckles. A new animation starts at its first frame.</summary>
	private void SetAnimation(int index)
	{
		if (Animation.Index != index)
		{
			Animation.Index = index;
			Animation.CurrentFrameIndex = 0;
		}
		Animation.Animate();
	}

	/// <summary>
	/// Draws the original sprite. The original art faces right. The sprite lines up with the collision box of the
	/// original, so it moves by the difference to the box of the character.
	/// </summary>
	protected override void DrawBody(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (DrawBodyRotated)
				objectRenderer.ModelMatrix *= Matrix4.CreateRotationZ(ShowAngle);
			double offsetY = IsSpinball ? SpinballCollisionRadius.Y - OriginalSpinballCollisionRadiusY : NormalCollisionRadius.Y - OriginalCollisionRadiusY;
			if (IsSuper && _superPalette != null)
				_superPalette.Render(objectRenderer, Animation, new Vector2(0.0, offsetY), IsFacingLeft);
			else
				objectRenderer.RenderOriginal(Animation, new Vector2(0.0, offsetY), IsFacingLeft);
		}
	}
}
