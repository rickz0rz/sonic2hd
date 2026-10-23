// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.GRABBER.GrabberInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.GRABBER;

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
	private AnimationGroup _animationGroup;
	private AnimationInstance _animationBody;
	private AnimationInstance _animationLegs;
	private AnimationInstance _animationWheels;
	private AnimationInstance _animationThread;
	private AnimationInstance _animationBodyExplode;
	private bool _showLegs;
	private GrabberInstance.Legs _legsSubObject;
	private float _explodeOpacity;
	private float _explodeOpacityAngle;
	private float _explodeOpacityAngleVelocity;
	private const int MoveSpeed = 1;
	private const int LowerSpeed = 8;
	private readonly Vector2i CharacterGrabOffset = new Vector2i(-8, 64 /*0x40*/);
	private GrabberInstance.StatusType _status;
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

	public GrabberInstance()
	{
		DesignBounds = new Rectanglei(sbyte.MinValue, sbyte.MinValue, 256 /*0x0100*/, 256 /*0x0100*/);
		CollisionRectangles = new CollisionRectangle[2]
		{
	  new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/),
	  new CollisionRectangle( this, 1, CharacterGrabOffset.X - 16 /*0x10*/, CharacterGrabOffset.Y - 8, 32 /*0x20*/, 32 /*0x20*/)
		};
	}

	protected override void OnStart()
	{
		base.OnStart();
		_regrabTicksCounter = 0;
		_canGrabAgain = true;
		_playerEscaped = false;
		_grabEscapeProtagonistCounter = 0;
		_lastEscapeProtagonistInput = 0;
		_grabEscapeSidekickCounter = 0;
		_lastEscapeSidekickInput = 0;
		_patrolTicks = byte.MaxValue;
		_direction = -1;
		_animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		_animationBody = new AnimationInstance(_animationGroup);
		_animationLegs = new AnimationInstance(_animationGroup, 7);
		_animationWheels = new AnimationInstance(_animationGroup, 12);
		_animationThread = new AnimationInstance(_animationGroup, 13);
		_animationBodyExplode = new AnimationInstance(_animationGroup, 14);
		_legsSubObject = Level.ObjectManager.AddSubObject<GrabberInstance.Legs>(this);
	}

	protected override void OnStop()
	{
		base.OnStop();
		ReleaseCharacter();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (_grabbedCharacter != null)
		{
			if (_grabbedCharacter == Level.Player.Protagonist)
			{
				if (Level.Player.Protagonist.Input.HorizontalDirection != _lastEscapeProtagonistInput)
				{
					_lastEscapeProtagonistInput = Level.Player.Protagonist.Input.HorizontalDirection;
					++_grabEscapeProtagonistCounter;
				}
				if (_grabEscapeProtagonistCounter >= 8)
					ReleaseCharacter();
			}
			if (Level.Player.Sidekick != null && _grabbedCharacter == Level.Player.Sidekick)
			{
				if (Level.Player.Sidekick.Input.HorizontalDirection != _lastEscapeSidekickInput)
				{
					_lastEscapeSidekickInput = Level.Player.Sidekick.Input.HorizontalDirection;
					++_grabEscapeSidekickCounter;
				}
				if (_lastEscapeSidekickInput >= 8)
					ReleaseCharacter();
			}
			if (_grabbedCharacter == null)
			{
				_lastEscapeSidekickInput = 0;
				_lastEscapeProtagonistInput = 0;
				_grabEscapeProtagonistCounter = 0;
				_lastEscapeSidekickInput = 0;
				_status = GrabberInstance.StatusType.Raising;
				_regrabTicksCounter = 0;
				_playerEscaped = true;
				_canGrabAgain = false;
			}
		}
		if (_playerEscaped)
		{
			if (_regrabTicksCounter <= 30)
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
				_status = GrabberInstance.StatusType.Raising;
			}
			else
			{
				_grabbedCharacter.Position = Position + CharacterGrabOffset * new Vector2i(_direction, 1);
				_grabbedCharacter.Facing = _direction * -1;
			}
		}
		switch (_status)
		{
			case GrabberInstance.StatusType.Patroling:
				if (DetectPlayer())
				{
					_status = GrabberInstance.StatusType.Detected;
					_ticksRemaining = 16 /*0x10*/;
					break;
				}
				if (_patrolTicks == 0)
				{
					_direction *= -1;
					_patrolTicks = byte.MaxValue;
				}
				else
					--_patrolTicks;
				Move(_direction, 0);
				break;
			case GrabberInstance.StatusType.Detected:
				--_ticksRemaining;
				if (_ticksRemaining > 0)
					break;
				_status = GrabberInstance.StatusType.Lowering;
				break;
			case GrabberInstance.StatusType.Lowering:
				if (_lowerY < 264 && _grabbedCharacter == null)
				{
					Move(0, Math.Min(8, 264 - _lowerY));
					_lowerY += 8;
					break;
				}
				_status = GrabberInstance.StatusType.Raising;
				if (_grabbedCharacter == null)
					break;
				_ticksRemaining = 20;
				break;
			case GrabberInstance.StatusType.Raising:
				if (_ticksRemaining > 0)
				{
					--_ticksRemaining;
					break;
				}
				if (_lowerY > 0)
				{
					Move(0, -Math.Min(8, _lowerY));
					_lowerY -= 8;
					break;
				}
				if (_grabbedCharacter != null)
				{
					_status = GrabberInstance.StatusType.Exploding;
					_ticksRemaining = 130;
					break;
				}
				_status = GrabberInstance.StatusType.Patroling;
				_ticksRemaining = 0;
				break;
			case GrabberInstance.StatusType.Exploding:
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
				_status = GrabberInstance.StatusType.Patroling;
				break;
		}
	}

	private bool DetectPlayer()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ShouldReactToLevel && character.Position.Y - Position.Y >= 0.0 && Math.Abs(character.Position.X - Position.X) < (double)(CollisionRectangles[1].Bounds.Width / 4))
				return true;
		}
		return false;
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
		_grabbedCharacter.IsSpinball = true;
		_grabbedCharacter.IsAirborne = true;
		_grabbedCharacter.CheckCollision = true;
		_grabbedCharacter.ObjectLink = null;
		_grabbedCharacter = null;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
			return;
		ICharacter activeObject = (ICharacter)e.ActiveObject;
		if (e.Id == 0)
		{
			if (!activeObject.IsDeadly)
				return;
			ReleaseCharacter();
			Destroy(activeObject);
		}
		else
		{
			if (_grabbedCharacter != null || _status == GrabberInstance.StatusType.Patroling || !_canGrabAgain || activeObject.IsAirborne)
				return;
			GrabCharacter(activeObject);
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
				case GrabberInstance.StatusType.Patroling:
					if (_patrolTicks < _animationGroup[2].Duration)
					{
						_animationBody.Index = 2;
						break;
					}
					if (_patrolTicks == byte.MaxValue)
					{
						_animationBody.Index = 3;
						break;
					}
					if ((_patrolTicks == 84 || _patrolTicks == 168) && Level.Random.Next(0, 2) == 0)
					{
						_animationBody.Index = 1;
						break;
					}
					break;
				case GrabberInstance.StatusType.Lowering:
					_animationBody.Index = 4;
					break;
				case GrabberInstance.StatusType.Raising:
					if (_animationBody.Index == 4)
					{
						_animationBody.Index = 5;
						break;
					}
					break;
				case GrabberInstance.StatusType.Exploding:
					_explodeOpacityAngleVelocity = (float)MathX.Clamp(Math.PI / 30.0, _explodeOpacityAngleVelocity + Math.PI / 600.0, Math.PI / 4.0);
					_explodeOpacityAngle += _explodeOpacityAngleVelocity;
					_explodeOpacity = (float)MathX.Clamp(0.0, (1.0 - Math.Cos(_explodeOpacityAngle)) / 2.0, 1.0);
					break;
			}
			if (_grabbedCharacter != null)
				_animationBody.Index = 6;
			_animationBody.Animate();
			_animationLegs.Animate();
			if (Math.Abs(PositionPrecise.Y - LastPositionPrecise.Y) > 0.0)
				_animationWheels.Animate();
			_animationThread.Animate();
			switch (_animationBody.Index)
			{
				case 1:
				case 2:
				case 3:
					_showLegs = false;
					break;
				case 4:
					_animationLegs.Index = 8;
					_showLegs = true;
					break;
				case 6:
					_animationLegs.Index = _grabbedCharacter == null || !_grabbedCharacter.Entry.Name.Contains("Tails") ? 11 : 10;
					_showLegs = true;
					break;
				case 9:
					_animationLegs.Index = 9;
					_showLegs = true;
					break;
				default:
					_animationLegs.Index = 7;
					_showLegs = true;
					break;
			}
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		bool flag = _direction < 0;
		using (objectRenderer.BeginMatixState())
		{
			if (flag)
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
				Rectanglei source = _animationThread.CurrentFrame.Source;
				objectRenderer.Render((Rectangle)source, new Rectangle(-source.Width / 2 - 6, -_lowerY - 64 /*0x40*/, source.Width, _lowerY));
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
			bool flag = parent._direction < 0;
			using (objectRenderer.BeginMatixState())
			{
				if (flag)
					objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
				objectRenderer.Render(parent._animationLegs);
			}
		}
	}
}
