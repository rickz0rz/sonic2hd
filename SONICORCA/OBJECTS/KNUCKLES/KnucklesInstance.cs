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
/// When he falls from a glide onto a flat floor, he lands hard. A glide and a slide break monitors and enemies. A
/// wall stops the glide, and Knuckles falls. The values of the original are in subpixels: 256 make one pixel, and one
/// pixel is 4 level units, so a value of the original is 64 times its value here.
/// </para>
/// </summary>
public class KnucklesInstance : Character
{
	/// <summary>The jump speed of Knuckles, $600, in level units.</summary>
	private const double KnucklesJumpForce = 0x600 / 64.0;

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

	private const int AnimationGetUp = 37;

	/// <summary>The frames of the turn of a glide (Knuckles_DoGlidingAnimation), as frames of the glide animation. 4 is frame 0, flipped.</summary>
	private static readonly int[] GlideTurnFrames = { 0, 1, 2, 3, 4, 3, 2, 1 };

	/// <summary>The states of double_jump_flag in KiS2. The climb comes in a later step.</summary>
	private enum GlideState
	{
		None,
		Glide,
		Fall,
		Slide
	}

	private GlideState _state;

	/// <summary>double_jump_property: the direction of the glide, from 0 (right) to $80 (left), as a byte.</summary>
	private int _turn;

	/// <summary>inertia in a glide: the speed along the direction of the glide.</summary>
	private double _glideSpeed;

	/// <summary>The animation after a glide while the move lock runs: the hard landing or the get up, or -1.</summary>
	private int _landingAnimation = -1;

	private int _landingLockTicks;

	[HideInEditor]
	public override bool IsGliding => _state == GlideState.Glide || _state == GlideState.Slide;

	protected override bool CanJumpFromGround => _state != GlideState.Slide;

	protected override void OnStart()
	{
		AnimationGroupResourceKey = Type.GetAbsolutePath(KnucklesType.AnimationGroupResourceKey);
		NormalJumpForce = KnucklesJumpForce;
		UnderwaterJumpForce = KnucklesUnderwaterJumpForce;
		// Super Knuckles does not jump higher (KiS2)
		SuperJumpForce = KnucklesJumpForce;
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
		if (_state != GlideState.None || IsHurt || IsSidekick || Velocity.Y < -GlideStartSpeed)
			return false;
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
		// A wall stops the glide. The climb on the wall comes in a later step.
		if ((TouchesWall(-1) && Velocity.X <= 0.0) || (TouchesWall(1) && Velocity.X >= 0.0))
		{
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

	protected override void OnAnimate()
	{
		base.OnAnimate();
		if (IsHurt || IsDead)
			return;
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
			objectRenderer.RenderOriginal(Animation, new Vector2(0.0, offsetY), IsFacingLeft);
		}
	}
}
