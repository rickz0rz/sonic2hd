using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.GRABBER;

/// <summary>
/// The Grabber spider badnik from Chemical Plant Zone. It patrols left and right. When a character is below it, it
/// goes down on a thread to grab the character. A grabbed character that does not escape gets hurt when the Grabber
/// explodes.
/// </summary>
public class GrabberInstance : Badnik
{
	private const int AnimationBodyStationary = 0;
	private const int AnimationBodyStationaryWiggle = 1;
	private const int AnimationBodyTurnBegin = 2;
	private const int AnimationBodyTurnEnd = 3;
	private const int AnimationBodyOpen = 4;
	private const int AnimationBodyClose = 5;
	private const int AnimationBodyHold = 6;
	private const int AnimationLegsStationary = 7;
	private const int AnimationLegsOpen = 8;
	private const int AnimationLegsClose = 9;
	private const int AnimationLegsHold = 10;
	private const int AnimationLegsHoldWiggle = 11;
	private const int AnimationWheels = 12;
	private const int AnimationThread = 13;
	private const int AnimationBodyExplode = 14;

	private const int CollisionIdBody = 0;
	private const int CollisionIdClaws = 1;

	private const int MoveSpeed = 1;
	private const int LowerSpeed = 8;

	/// <summary>The maximum distance that the Grabber goes down on its thread.</summary>
	private const int MaximumLowerDistance = 264;

	/// <summary>The number of ticks that the Grabber moves in one direction before it turns.</summary>
	private const int PatrolTicks = 255;

	/// <summary>The number of ticks between the detection of a character and the start of the descent.</summary>
	private const int DetectDelayTicks = 16;

	/// <summary>The number of ticks that the Grabber waits after a grab before it goes up.</summary>
	private const int GrabHoldTicks = 20;

	/// <summary>The number of ticks that the Grabber holds a character at the top before it explodes.</summary>
	private const int ExplodeDelayTicks = 130;

	/// <summary>The number of ticks after an escape before the Grabber can grab again.</summary>
	private const int RegrabDelayTicks = 30;

	/// <summary>The number of changes of horizontal input that a character needs to escape.</summary>
	private const int EscapeInputChanges = 8;

	/// <summary>The number of units in one original pixel.</summary>
	private const int OriginalPixel = 4;

	/// <summary>
	/// Original: the maximum horizontal distance and the maximum distance below the Grabber at which it sees a
	/// character (loc_38E52).
	/// </summary>
	private const int OriginalDetectRangeX = 64 * OriginalPixel;
	private const int OriginalDetectRangeY = 128 * OriginalPixel;

	/// <summary>
	/// Original: the number of ticks from the grab to the explosion. ObjA7_CheckExplode waits 1 tick, then 16, 15
	/// and so on down to 2 ticks.
	/// </summary>
	private const int OriginalExplodeTicks = 136;

	/// <summary>Original: the length of one escape window, and the presses that a character needs in it (loc_390BC).</summary>
	private const int OriginalEscapeWindowTicks = 32;
	private const int OriginalEscapePresses = 4;

	/// <summary>The width and height of the body and claw collision boxes, in HD and in the original.</summary>
	private const int CollisionSizeHD = 32;
	private const int CollisionSizeOriginal = 64;

	private AnimationGroup _animationGroup;
	private AnimationInstance _animationBody;
	private AnimationInstance _animationLegs;
	private AnimationInstance _animationWheels;
	private AnimationInstance _animationThread;
	private AnimationInstance _animationBodyExplode;
	private bool _showLegs;
	private float _explodeOpacity;
	private float _explodeOpacityAngle;
	private float _explodeOpacityAngleVelocity;
	private readonly Vector2i CharacterGrabOffset = new Vector2i(-8, 64);
	private StatusType _status;
	private int _direction;
	private int _patrolTicks;
	private int _ticksRemaining;
	private int _lowerY;
	private int _regrabTicksCounter;
	private bool _canGrabAgain;
	private bool _playerEscaped;
	private int _grabEscapeProtagonistCounter;
	private int _lastEscapeProtagonistInput;
	private int _grabEscapeSidekickCounter;
	private int _lastEscapeSidekickInput;
	private int _previousCharacterLayer;
	private ICharacter _grabbedCharacter;

	/// <summary>Original: the ticks until the Grabber explodes. Zero when the countdown has not started.</summary>
	private int _explodeTicks;

	/// <summary>Original: the state of the escape window. The window opens at the first left or right press.</summary>
	private bool _escapeWindowOpen;
	private int _escapeWindowTicks;
	private int _escapePresses;
	private int _escapeLastPress;
	private int _escapePreviousDirection;

	public GrabberInstance()
	{
		DesignBounds = new Rectanglei(-128, -128, 256, 256);
		SetCollisionRectangles(CollisionSizeHD);
	}

	/// <summary>Sets the body box and the claw box. Both boxes are squares of the given size.</summary>
	private void SetCollisionRectangles(int size)
	{
		Vector2i clawCentre = CharacterGrabOffset + new Vector2i(0, 8);
		CollisionRectangles = new CollisionRectangle[2]
		{
			new CollisionRectangle(this, CollisionIdBody, -size / 2, -size / 2, size, size),
			new CollisionRectangle(this, CollisionIdClaws, clawCentre.X - size / 2, clawCentre.Y - size / 2, size, size)
		};
	}

	protected override void OnStart()
	{
		base.OnStart();
		SetCollisionRectangles(Level.OriginalPhysics ? CollisionSizeOriginal : CollisionSizeHD);
		_regrabTicksCounter = 0;
		_canGrabAgain = true;
		_playerEscaped = false;
		_grabEscapeProtagonistCounter = 0;
		_lastEscapeProtagonistInput = 0;
		_grabEscapeSidekickCounter = 0;
		_lastEscapeSidekickInput = 0;
		_patrolTicks = PatrolTicks;
		_direction = -1;
		_animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationBody = new AnimationInstance(_animationGroup);
		_animationLegs = new AnimationInstance(_animationGroup, AnimationLegsStationary);
		_animationWheels = new AnimationInstance(_animationGroup, AnimationWheels);
		_animationThread = new AnimationInstance(_animationGroup, AnimationThread);
		_animationBodyExplode = new AnimationInstance(_animationGroup, AnimationBodyExplode);
		Level.ObjectManager.AddSubObject<Legs>(this);
	}

	protected override void OnStop()
	{
		base.OnStop();
		ReleaseCharacter();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (Level.OriginalPhysics && _explodeTicks > 0)
		{
			--_explodeTicks;
			if (_explodeTicks == 0)
			{
				Explode();
				return;
			}
		}
		if (_grabbedCharacter != null)
		{
			if (Level.OriginalPhysics)
				UpdateEscapeOriginal();
			else
				UpdateEscape();
		}
		if (_playerEscaped)
		{
			if (_regrabTicksCounter <= RegrabDelayTicks)
			{
				++_regrabTicksCounter;
			}
			else
			{
				_playerEscaped = false;
				_canGrabAgain = true;
			}
		}
		if (_grabbedCharacter != null)
		{
			if (_grabbedCharacter.ObjectLink != this || _grabbedCharacter.IsDebug)
			{
				ReleaseCharacter();
				_status = StatusType.Raising;
			}
			else
			{
				_grabbedCharacter.Position = Position + CharacterGrabOffset * new Vector2i(_direction, 1);
				_grabbedCharacter.Facing = _direction * -1;
			}
		}
		UpdateStatus();
	}

	/// <summary>
	/// Counts the changes of horizontal input of the grabbed character, and releases the character when it has
	/// enough of them.
	/// </summary>
	private void UpdateEscape()
	{
		if (_grabbedCharacter == Level.Player.Protagonist)
		{
			if (Level.Player.Protagonist.Input.HorizontalDirection != _lastEscapeProtagonistInput)
			{
				_lastEscapeProtagonistInput = Level.Player.Protagonist.Input.HorizontalDirection;
				++_grabEscapeProtagonistCounter;
			}
			if (_grabEscapeProtagonistCounter >= EscapeInputChanges)
				ReleaseCharacter();
		}
		if (Level.Player.Sidekick != null && _grabbedCharacter == Level.Player.Sidekick)
		{
			if (Level.Player.Sidekick.Input.HorizontalDirection != _lastEscapeSidekickInput)
			{
				_lastEscapeSidekickInput = Level.Player.Sidekick.Input.HorizontalDirection;
				++_grabEscapeSidekickCounter;
			}
			if (_grabEscapeSidekickCounter >= EscapeInputChanges)
				ReleaseCharacter();
		}
		if (_grabbedCharacter == null)
		{
			_lastEscapeSidekickInput = 0;
			_lastEscapeProtagonistInput = 0;
			_grabEscapeProtagonistCounter = 0;
			_grabEscapeSidekickCounter = 0;
			_status = StatusType.Raising;
			_regrabTicksCounter = 0;
			_playerEscaped = true;
			_canGrabAgain = false;
		}
	}

	/// <summary>
	/// Original: the escape of loc_390BC. The first left or right press opens a window of 32 ticks. Each later press
	/// that differs from the last press counts. At the end of the window, the character escapes with 4 or more counted
	/// presses. Otherwise the counter clears and the next press opens a new window.
	/// </summary>
	private void UpdateEscapeOriginal()
	{
		int direction = _grabbedCharacter.Input.HorizontalDirection;
		int press = direction != _escapePreviousDirection ? direction : 0;
		_escapePreviousDirection = direction;
		if (!_escapeWindowOpen)
		{
			if (press == 0)
				return;
			_escapeWindowOpen = true;
			_escapeWindowTicks = OriginalEscapeWindowTicks;
			_escapeLastPress = press;
			return;
		}
		if (--_escapeWindowTicks > 0)
		{
			if (press != 0 && press != _escapeLastPress)
			{
				_escapeLastPress = press;
				++_escapePresses;
			}
			return;
		}
		if (_escapePresses >= OriginalEscapePresses)
		{
			// The Grabber stops and still explodes, but it can no longer hurt or grab.
			ReleaseCharacter();
			_status = StatusType.Exploding;
		}
		_escapeWindowOpen = false;
		_escapePresses = 0;
	}

	/// <summary>Hurts the character that the Grabber holds, and replaces the Grabber with an explosion.</summary>
	private void Explode()
	{
		if (_grabbedCharacter != null)
		{
			// Release the character first. Otherwise the release clears the knockback of the hurt.
			ICharacter character = _grabbedCharacter;
			ReleaseCharacter();
			character.Hurt(_direction);
		}
		CreateExplosionObject();
		FinishForever();
	}

	private void UpdateStatus()
	{
		switch (_status)
		{
			case StatusType.Patroling:
				if (DetectPlayer())
				{
					_status = StatusType.Detected;
					_ticksRemaining = DetectDelayTicks;
					break;
				}
				if (_patrolTicks == 0)
				{
					_direction *= -1;
					_patrolTicks = PatrolTicks;
				}
				else
					--_patrolTicks;
				Move(_direction * MoveSpeed, 0);
				break;
			case StatusType.Detected:
				--_ticksRemaining;
				if (_ticksRemaining > 0)
					break;
				_status = StatusType.Lowering;
				break;
			case StatusType.Lowering:
				if (_lowerY < MaximumLowerDistance && _grabbedCharacter == null)
				{
					Move(0, Math.Min(LowerSpeed, MaximumLowerDistance - _lowerY));
					_lowerY += LowerSpeed;
					break;
				}
				_status = StatusType.Raising;
				if (_grabbedCharacter == null)
					break;
				// The original goes up at once after the grab (ObjA7_GrabCharacter).
				_ticksRemaining = Level.OriginalPhysics ? 0 : GrabHoldTicks;
				break;
			case StatusType.Raising:
				if (_ticksRemaining > 0)
				{
					--_ticksRemaining;
					break;
				}
				if (_lowerY > 0)
				{
					Move(0, -Math.Min(LowerSpeed, _lowerY));
					_lowerY -= LowerSpeed;
					break;
				}
				if (_grabbedCharacter != null)
				{
					// The original countdown starts at the grab, so it runs on here.
					_status = StatusType.Exploding;
					_ticksRemaining = Level.OriginalPhysics ? 0 : ExplodeDelayTicks;
					break;
				}
				_status = StatusType.Patroling;
				_ticksRemaining = 0;
				break;
			case StatusType.Exploding:
				if (Level.OriginalPhysics)
					break;
				--_ticksRemaining;
				if (_ticksRemaining != 0)
					break;
				if (_grabbedCharacter != null)
				{
					_grabbedCharacter.Hurt(_direction);
					ReleaseCharacter();
					CreateExplosionObject();
					FinishForever();
				}
				_status = StatusType.Patroling;
				break;
		}
	}

	/// <summary>Returns true when a character is below the Grabber, near its horizontal position.</summary>
	private bool DetectPlayer()
	{
		if (Level.OriginalPhysics)
			return DetectPlayerOriginal();
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ShouldReactToLevel && character.Position.Y - Position.Y >= 0.0 && Math.Abs(character.Position.X - Position.X) < (double)(CollisionRectangles[CollisionIdClaws].Bounds.Width / 4))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Original: the check of loc_38E52. It uses the character that is nearest horizontally. That character must be
	/// less than 64 pixels to the side and 1 to 127 pixels below.
	/// </summary>
	private bool DetectPlayerOriginal()
	{
		ICharacter nearest = null;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ShouldReactToLevel && (nearest == null || Math.Abs(character.Position.X - Position.X) < Math.Abs(nearest.Position.X - Position.X)))
				nearest = character;
		}
		if (nearest == null)
			return false;
		int dy = nearest.Position.Y - Position.Y;
		return Math.Abs(nearest.Position.X - Position.X) < OriginalDetectRangeX && dy >= OriginalPixel && dy < OriginalDetectRangeY;
	}

	private void GrabCharacter(ICharacter character)
	{
		_grabbedCharacter = character;
		_grabbedCharacter.LeaveGround();
		character.IsAirborne = true;
		character.CheckCollision = false;
		character.ObjectLink = this;
		character.Position = Position + CharacterGrabOffset;
		character.Facing = _direction * -1;
		character.SpecialState = CharacterSpecialState.Grabbed;
		_ticksRemaining = 0;
		if (Level.OriginalPhysics)
		{
			_explodeTicks = OriginalExplodeTicks;
			_escapeWindowOpen = false;
			_escapePresses = 0;
			_escapePreviousDirection = character.Input.HorizontalDirection;
		}
		_previousCharacterLayer = Level.Map.Layers.IndexOf(_grabbedCharacter.Layer);
		_grabbedCharacter.Layer = Layer;
	}

	private void ReleaseCharacter()
	{
		if (_grabbedCharacter == null)
			return;
		_explodeOpacity = 0.0f;
		_explodeOpacityAngleVelocity = 0.0f;
		_animationBody.ResetFrame();
		_animationBody.Cycles = 0;
		_grabbedCharacter.Layer = Level.Map.Layers[_previousCharacterLayer];
		_grabbedCharacter.SpecialState = CharacterSpecialState.Normal;
		_grabbedCharacter.Velocity = new Vector2();
		// The original releases the character in the walk animation, not in a roll (loc_390FA).
		_grabbedCharacter.IsSpinball = !Level.OriginalPhysics;
		_grabbedCharacter.IsAirborne = true;
		_grabbedCharacter.CheckCollision = true;
		_grabbedCharacter.ObjectLink = null;
		_grabbedCharacter = null;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter character = (ICharacter)e.ActiveObject;
		// Original: after an escape, the Grabber has no collision until it explodes.
		if (Level.OriginalPhysics && _explodeTicks > 0 && _grabbedCharacter == null)
			return;
		if (e.Id == CollisionIdBody)
		{
			if (!character.IsDeadly)
				return;
			ReleaseCharacter();
			Destroy(character);
		}
		else
		{
			if (_grabbedCharacter != null || !_canGrabAgain)
				return;
			// The original grabs only on the way down and up, and also grabs a character in the air.
			if (Level.OriginalPhysics ? _status != StatusType.Lowering && _status != StatusType.Raising : _status == StatusType.Patroling || character.IsAirborne)
				return;
			GrabCharacter(character);
			e.MaintainVelocity = true;
		}
	}

	protected override void OnAnimate()
	{
		if (Level.State == LevelState.Editing)
		{
			_animationBody.Animate();
			_animationLegs.Animate();
			_showLegs = true;
		}
		else
		{
			switch (_status)
			{
				case StatusType.Patroling:
					if (_patrolTicks < _animationGroup[AnimationBodyTurnBegin].Duration)
					{
						_animationBody.Index = AnimationBodyTurnBegin;
						break;
					}
					if (_patrolTicks == PatrolTicks)
					{
						_animationBody.Index = AnimationBodyTurnEnd;
						break;
					}
					// Sometimes wiggle at one third and two thirds of the patrol.
					if ((_patrolTicks == 84 || _patrolTicks == 168) && Level.Random.Next(0, 2) == 0)
					{
						_animationBody.Index = AnimationBodyStationaryWiggle;
						break;
					}
					break;
				case StatusType.Lowering:
					_animationBody.Index = AnimationBodyOpen;
					break;
				case StatusType.Raising:
					if (_animationBody.Index == AnimationBodyOpen)
					{
						_animationBody.Index = AnimationBodyClose;
						break;
					}
					break;
			}
			// The body flashes faster and faster until it explodes. The original countdown starts at the grab.
			if (_status == StatusType.Exploding || (Level.OriginalPhysics && _explodeTicks > 0))
			{
				_explodeOpacityAngleVelocity = (float)MathX.Clamp(Math.PI / 30.0, _explodeOpacityAngleVelocity + Math.PI / 600.0, Math.PI / 4.0);
				_explodeOpacityAngle += _explodeOpacityAngleVelocity;
				_explodeOpacity = (float)MathX.Clamp(0.0, (1.0 - Math.Cos(_explodeOpacityAngle)) / 2.0, 1.0);
			}
			if (_grabbedCharacter != null)
				_animationBody.Index = AnimationBodyHold;
			_animationBody.Animate();
			_animationLegs.Animate();
			if (Math.Abs(PositionPrecise.Y - LastPositionPrecise.Y) > 0.0)
				_animationWheels.Animate();
			_animationThread.Animate();
			switch (_animationBody.Index)
			{
				case AnimationBodyStationaryWiggle:
				case AnimationBodyTurnBegin:
				case AnimationBodyTurnEnd:
					_showLegs = false;
					break;
				case AnimationBodyOpen:
					_animationLegs.Index = AnimationLegsOpen;
					_showLegs = true;
					break;
				case AnimationBodyHold:
					_animationLegs.Index = _grabbedCharacter == null || !_grabbedCharacter.Entry.Name.Contains("Tails") ? AnimationLegsHoldWiggle : AnimationLegsHold;
					_showLegs = true;
					break;
				case AnimationBodyClose:
					_animationLegs.Index = AnimationLegsClose;
					_showLegs = true;
					break;
				default:
					_animationLegs.Index = AnimationLegsStationary;
					_showLegs = true;
					break;
			}
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		bool flipX = _direction < 0;
		using (objectRenderer.BeginMatixState())
		{
			if (flipX)
				objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			objectRenderer.Render(_animationBody);
			if (_explodeOpacity > 0.0)
			{
				objectRenderer.MultiplyColour = new Colour(_explodeOpacity, 1.0, 1.0, 1.0);
				objectRenderer.Render(_animationBodyExplode);
				objectRenderer.MultiplyColour = Colours.White;
			}
			if (_showLegs)
				objectRenderer.Render(_animationLegs);
			if (_lowerY > 0)
			{
				// Stretch the thread texture from the body down to the current descent.
				Rectanglei source = _animationThread.CurrentFrame.Source;
				objectRenderer.Render((Rectangle)source, new Rectangle(-source.Width / 2 - 6, -_lowerY - 64, source.Width, _lowerY));
			}
			objectRenderer.Render(_animationWheels);
		}
	}

	private enum StatusType
	{
		Patroling,
		Detected,
		Lowering,
		Raising,
		Exploding,
	}

	/// <summary>A sub-object that draws the legs of the Grabber again, with a different draw priority.</summary>
	private class Legs : ActiveObject
	{
		public GrabberInstance Parent => ParentObject as GrabberInstance;

		protected override void OnStart()
		{
			Priority = 1152;
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			if (Parent.Finished)
				Finish();
			else
				Position = Parent.Position;
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			GrabberInstance parent = Parent;
			if (!parent._showLegs)
				return;
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			bool flipX = parent._direction < 0;
			using (objectRenderer.BeginMatixState())
			{
				if (flipX)
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				objectRenderer.Render(parent._animationLegs);
			}
		}
	}
}
