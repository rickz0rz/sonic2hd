// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.CPZBLOBS.CPZBlobsInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.CPZBLOBS;

public class CPZBlobsInstance : ActiveObject
{
	private const int AnimationBlob = 0;
	private const int AnimationTopSplashStart = 1;
	private const int AnimationTopSplashEnd = 2;
	private const int AnimationBottomSplashStart = 3;
	private const int AnimationBottomSplashEnd = 4;
	private const int AnimationBottomOverflow = 5;
	private AnimationGroup _animationGroup;
	private CPZBlobsInstance.ArrangementType _arrangementType;
	private CPZBlobsInstance.Blob[] _children;
	private List<AnimationInstance> _bottomStartOverflowTriggeredAnimations = new List<AnimationInstance>();
	private Dictionary<AnimationInstance, Vector2> _triggeredAnimations = new Dictionary<AnimationInstance, Vector2>();
	private Dictionary<AnimationInstance, Vector2> _triggeredAnimationsPositions = new Dictionary<AnimationInstance, Vector2>();

	[StateVariable]
	private CPZBlobsInstance.ArrangementType Arrangement
	{
		get => _arrangementType;
		set => _arrangementType = value;
	}

	[StateVariable]
	private bool FlipX { get; set; }

	protected override void OnStart()
	{
		_animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		DesignBounds = new Rectanglei(sbyte.MinValue, sbyte.MinValue, 256 /*0x0100*/, 256 /*0x0100*/);
		_children = new CPZBlobsInstance.Blob[6];
		for (int index = 5; index >= 0; --index)
		{
			CPZBlobsInstance.Blob blob = Level.ObjectManager.AddSubObject<CPZBlobsInstance.Blob>(this);
			blob.Initialise(Position, index, FlipX);
			_children[index] = blob;
			_children[index].Priority = index + 2;
		}
	}

	protected override void OnUpdate()
	{
	}

	[HideInEditor]
	public List<AnimationInstance> BottomStartOverflowTriggeredAnimations
	{
		get => _bottomStartOverflowTriggeredAnimations;
	}

	[HideInEditor]
	public Dictionary<AnimationInstance, Vector2> TriggeredAnimationsPositions
	{
		get => _triggeredAnimationsPositions;
	}

	[HideInEditor]
	public Dictionary<AnimationInstance, Vector2> TriggeredAnimations => _triggeredAnimations;

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		base.OnDraw(renderer, viewOptions);
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		List<AnimationInstance> animationInstanceList = new List<AnimationInstance>();
		foreach (KeyValuePair<AnimationInstance, Vector2> triggeredAnimation in TriggeredAnimations)
		{
			triggeredAnimation.Key.Animate();
			if (triggeredAnimation.Key.Cycles > 0)
				animationInstanceList.Add(triggeredAnimation.Key);
		}
		foreach (AnimationInstance key in animationInstanceList)
		{
			key.ResetFrame();
			key.Cycles = 0;
			if (key.Index == 4 || BottomStartOverflowTriggeredAnimations.Contains(key))
			{
				CPZBlobsInstance.OverflowInstance overflowInstance = Level.ObjectManager.AddSubObject<CPZBlobsInstance.OverflowInstance>(this);
				overflowInstance.InitOverflow();
				overflowInstance.Layer = Level.Map.Layers.Last<LevelLayer>();
				Vector2 animationsPosition = TriggeredAnimationsPositions[key];
				if (BottomStartOverflowTriggeredAnimations.Contains(key))
				{
					if (_arrangementType == CPZBlobsInstance.ArrangementType.Arc)
						overflowInstance.PositionPrecise = new Vector2(animationsPosition.X, animationsPosition.Y - 40.0);
					else
						overflowInstance.PositionPrecise = new Vector2(animationsPosition.X, animationsPosition.Y - 40.0);
				}
				else if (_arrangementType == CPZBlobsInstance.ArrangementType.Arc)
					overflowInstance.PositionPrecise = new Vector2(animationsPosition.X, animationsPosition.Y - 15.0);
				else
					overflowInstance.PositionPrecise = new Vector2(animationsPosition.X, animationsPosition.Y - 23.0);
			}
			if (BottomStartOverflowTriggeredAnimations.Contains(key))
				BottomStartOverflowTriggeredAnimations.Remove(key);
			TriggeredAnimations.Remove(key);
			TriggeredAnimationsPositions.Remove(key);
		}
		foreach (KeyValuePair<AnimationInstance, Vector2> triggeredAnimation in TriggeredAnimations)
		{
			Vector2 animationsPosition = TriggeredAnimationsPositions[triggeredAnimation.Key];
			objectRenderer.Render(triggeredAnimation.Key, animationsPosition - PositionPrecise + triggeredAnimation.Value);
		}
	}

	private enum ArrangementType
	{
		Arc,
		Straight,
	}

	private class Blob : Enemy
	{
		private AnimationInstance _animationBlob;
		private AnimationInstance _animationTopSplashStart;
		private AnimationInstance _animationTopSplashEnd;
		private AnimationInstance _animationBottomSplashStart;
		private AnimationInstance _animationBottomSplashEnd;
		private AnimationInstance _animationBottomOverflow;
		private int _index;
		private int _originalY;
		private Vector2 _velocity;
		private double _upVelocity;
		private int _upX;
		private int _downX;
		private bool _leftToRight;
		private AnimationInstance _lastTriggeredAnimation;
		private bool _directionChange;
		private int _directionChangeNormal;
		private bool _startAnimation;
		private double _currentAngle;

		public int Delay { get; set; }

		public CPZBlobsInstance Host => ParentObject as CPZBlobsInstance;

		public Vector2 Velocity => _velocity;

		protected override void OnStart()
		{
			base.OnStart();
			_animationBlob = new AnimationInstance(Host._animationGroup);
			_animationTopSplashStart = new AnimationInstance(Host._animationGroup, 1);
			_animationTopSplashEnd = new AnimationInstance(Host._animationGroup, 2);
			_animationBottomSplashStart = new AnimationInstance(Host._animationGroup, 3);
			_animationBottomSplashEnd = new AnimationInstance(Host._animationGroup, 4);
			_animationBottomOverflow = new AnimationInstance(Host._animationGroup, 5);
			Priority = sbyte.MinValue;
			CollisionRectangles = new CollisionRectangle[1]
			{
		new CollisionRectangle( this, 0, -16, -16, 32 /*0x20*/, 32 /*0x20*/)
			};
		}

		public void Initialise(Vector2i position, int index, bool flipX)
		{
			_index = index;
			_upVelocity = Host._arrangementType != CPZBlobsInstance.ArrangementType.Arc ? -9.0 : -17.55;
			_velocity = new Vector2(0.0, _upVelocity);
			Position = position;
			_originalY = Host._arrangementType != CPZBlobsInstance.ArrangementType.Arc ? position.Y : position.Y + 8;
			if (flipX)
			{
				_leftToRight = false;
				_upX = position.X;
				_downX = position.X + 384;
				_currentAngle = Math.PI;
			}
			else
			{
				_leftToRight = true;
				_upX = position.X + 384;
				_downX = position.X;
				_currentAngle = 0.0;
			}
			Delay = Host._arrangementType != CPZBlobsInstance.ArrangementType.Arc ? index * 4 : index * 5;
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (Host.Finished)
				Finish();
			else if (Delay > 0)
				--Delay;
			else if (Host._arrangementType == CPZBlobsInstance.ArrangementType.Arc)
				UpdateArc();
			else
				UpdateStraight();
		}

		private void UpdateArc()
		{
			if (Host._arrangementType == CPZBlobsInstance.ArrangementType.Arc)
				ArcPrepareTriggers();
			if (_leftToRight && _currentAngle <= 0.0)
				PlayGloopSound();
			if (!_leftToRight && _currentAngle >= Math.PI)
				PlayGloopSound();
			Vector2 relative = new Vector2((_downX + _upX) * 0.5, _originalY);
			double num = Vector2.GetDistance(new Vector2(_downX, 0.0), new Vector2(_upX, 0.0)) * 0.5;
			double x = relative.X - num;
			if (_leftToRight)
				_currentAngle += 0.031939525311496228;
			else
				_currentAngle -= 0.031939525311496228;
			PositionPrecise = PositionPrecise + _velocity;
			if (_leftToRight)
			{
				Vector2 point = new Vector2(x, _originalY);
				PositionPrecise = new Vector2(Vector2.GetPointRotatedFromRelative(relative, point, _currentAngle).X, PositionPrecise.Y);
			}
			else
			{
				Vector2 point = new Vector2(x, _originalY);
				PositionPrecise = new Vector2(Vector2.GetPointRotatedFromRelative(relative, point, _currentAngle).X, PositionPrecise.Y);
			}
			_velocity.Y += 0.375;
			if (PositionPrecise.Y >= _originalY && !_directionChange)
			{
				_directionChange = true;
				Delay = 72;
			}
			if (!_directionChange)
				return;
			_velocity.Y = _upVelocity;
			_directionChange = false;
			_leftToRight = !_leftToRight;
			if (_leftToRight)
				_currentAngle = 0.0;
			if (!_leftToRight)
				_currentAngle = Math.PI;
			PositionPrecise = new Vector2(_leftToRight ? _downX : _upX, _originalY);
		}

		private void UpdateStraight()
		{
			PositionPrecise = PositionPrecise + _velocity;
			if (_directionChange)
			{
				_velocity.Y = _upVelocity * _directionChangeNormal;
				_directionChange = false;
			}
			if (Position.Y > _originalY && _velocity.Y > 0.0)
			{
				PlayGloopSound();
				Delay = 72;
				_directionChange = true;
				_velocity.Y = 0.0;
				_directionChangeNormal = 1;
			}
			else if (Position.Y < _originalY - 324 && _velocity.Y < 0.0)
			{
				PlayGloopSound();
				Delay = 9;
				_directionChange = true;
				_velocity.Y = 0.0;
				_directionChangeNormal = -1;
			}
			PositionPrecise = new Vector2(_velocity.Y >= 0.0 ? _downX : _upX, PositionPrecise.Y);
		}

		private void PlayGloopSound()
		{
			Level.SoundManager.PlaySoundVisibleOnly("SONICORCA/SOUND/BLOB", Position);
		}

		protected override void OnAnimate()
		{
			if (!_startAnimation)
				return;
			_animationBlob.Animate();
		}

		private void TriggerAnimation(AnimationInstance animation, Vector2 position, bool isFinal = false)
		{
			if (!(_lastTriggeredAnimation != animation | isFinal))
				return;
			_lastTriggeredAnimation = animation;
			if (isFinal)
				_lastTriggeredAnimation = null;
			if (!Host.TriggeredAnimations.Any<KeyValuePair<AnimationInstance, Vector2>>(a => a.Key.Index == animation.Index) && !Host.TriggeredAnimationsPositions.Any<KeyValuePair<AnimationInstance, Vector2>>(p => p.Value == PositionPrecise))
			{
				animation.ResetFrame();
				animation.Cycles = 0;
				Host.TriggeredAnimations.Add(animation, position);
				Host.TriggeredAnimationsPositions.Add(animation, PositionPrecise);
			}
			if (_index == 5 && animation.Index == 3 && Host._arrangementType == CPZBlobsInstance.ArrangementType.Straight)
			{
				KeyValuePair<AnimationInstance, Vector2> keyValuePair = Host.TriggeredAnimations.LastOrDefault<KeyValuePair<AnimationInstance, Vector2>>(a => a.Key.Index == 3);
				if (keyValuePair.Key == null)
					return;
				Host.BottomStartOverflowTriggeredAnimations.Add(keyValuePair.Key);
			}
			else
			{
				if (_index != 5 || animation.Index != 3 || Host._arrangementType != CPZBlobsInstance.ArrangementType.Arc || _velocity.Y >= 0.0 || Host.BottomStartOverflowTriggeredAnimations.Count != 0)
					return;
				KeyValuePair<AnimationInstance, Vector2> keyValuePair = Host.TriggeredAnimations.LastOrDefault<KeyValuePair<AnimationInstance, Vector2>>(a => a.Key.Index == 3);
				if (keyValuePair.Key == null)
					return;
				Host.BottomStartOverflowTriggeredAnimations.Add(keyValuePair.Key);
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (Host._arrangementType == CPZBlobsInstance.ArrangementType.Straight)
				StraightPrepareTriggers();
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			if (Host._arrangementType == CPZBlobsInstance.ArrangementType.Arc)
				DrawArc(objectRenderer, viewOptions);
			else
				DrawStraight(objectRenderer, viewOptions);
		}

		private void ArcPrepareTriggers()
		{
			Rectanglei absoluteBounds = CollisionRectangles[0].AbsoluteBounds;
			Rectangle rect1 = Rectangle.FromLTRB(_downX - absoluteBounds.Width * 0.5, _originalY - absoluteBounds.Height, _downX + absoluteBounds.Width * 0.5, _originalY + absoluteBounds.Height * 0.5);
			Rectangle rect2 = Rectangle.FromLTRB(_upX - absoluteBounds.Width * 0.5, _originalY - absoluteBounds.Height, _upX + absoluteBounds.Width * 0.5, _originalY + absoluteBounds.Height * 0.5);
			Vector2i position1;
			if (absoluteBounds.IntersectsWith((Rectanglei)rect2) && !_leftToRight)
			{
				_startAnimation = true;
				AnimationInstance bottomSplashStart = _animationBottomSplashStart;
				double x = _upX - Position.X;
				int num = _originalY - 112 /*0x70*/;
				position1 = Position;
				int y1 = position1.Y;
				double y2 = num - y1;
				Vector2 position2 = new Vector2(x, y2);
				TriggerAnimation(bottomSplashStart, position2, true);
			}
			if (absoluteBounds.IntersectsWith((Rectanglei)rect1) && _leftToRight)
			{
				_startAnimation = true;
				AnimationInstance bottomSplashStart = _animationBottomSplashStart;
				int downX = _downX;
				position1 = Position;
				int x1 = position1.X;
				double x2 = downX - x1;
				int num = _originalY - 112 /*0x70*/;
				position1 = Position;
				int y3 = position1.Y;
				double y4 = num - y3;
				Vector2 position3 = new Vector2(x2, y4);
				TriggerAnimation(bottomSplashStart, position3);
			}
			if (absoluteBounds.IntersectsWith((Rectanglei)rect2) && _leftToRight)
			{
				if (_index == 5)
				{
					AnimationInstance animationBottomSplashEnd = _animationBottomSplashEnd;
					int upX = _upX;
					position1 = Position;
					int x3 = position1.X;
					double x4 = upX - x3;
					int num = _originalY - 120;
					position1 = Position;
					int y5 = position1.Y;
					double y6 = num - y5;
					Vector2 position4 = new Vector2(x4, y6);
					TriggerAnimation(animationBottomSplashEnd, position4, true);
				}
				else
				{
					AnimationInstance bottomSplashStart = _animationBottomSplashStart;
					int upX = _upX;
					position1 = Position;
					int x5 = position1.X;
					double x6 = upX - x5;
					int num = _originalY - 112 /*0x70*/;
					position1 = Position;
					int y7 = position1.Y;
					double y8 = num - y7;
					Vector2 position5 = new Vector2(x6, y8);
					TriggerAnimation(bottomSplashStart, position5, true);
				}
			}
			if (!absoluteBounds.IntersectsWith((Rectanglei)rect1) || _leftToRight)
				return;
			if (_index == 5)
			{
				AnimationInstance animationBottomSplashEnd = _animationBottomSplashEnd;
				int downX = _downX;
				position1 = Position;
				int x7 = position1.X;
				double x8 = downX - x7;
				int num = _originalY - 120;
				position1 = Position;
				int y9 = position1.Y;
				double y10 = num - y9;
				Vector2 position6 = new Vector2(x8, y10);
				TriggerAnimation(animationBottomSplashEnd, position6, true);
			}
			else
			{
				AnimationInstance bottomSplashStart = _animationBottomSplashStart;
				int downX = _downX;
				position1 = Position;
				int x9 = position1.X;
				double x10 = downX - x9;
				int num = _originalY - 112 /*0x70*/;
				position1 = Position;
				int y11 = position1.Y;
				double y12 = num - y11;
				Vector2 position7 = new Vector2(x10, y12);
				TriggerAnimation(bottomSplashStart, position7, true);
			}
		}

		private void StraightPrepareTriggers()
		{
			if (_velocity.Y > 0.0 && Position.Y > _originalY - 224 /*0xE0*/ && Position.Y < _originalY - 14)
			{
				_startAnimation = true;
				TriggerAnimation(_animationTopSplashStart, new Vector2(_downX - Position.X, _originalY - 224 /*0xE0*/ - Position.Y));
			}
			if (_velocity.Y > 0.0 && Position.Y > _originalY - 14)
			{
				if (_index == 5)
					TriggerAnimation(_animationBottomSplashEnd, new Vector2(_downX - Position.X, _originalY - 120 - Position.Y));
				else
					TriggerAnimation(_animationBottomSplashStart, new Vector2(_downX - Position.X, _originalY - 112 /*0x70*/ - Position.Y), true);
			}
			if (_velocity.Y < 0.0 && Position.Y < _originalY - 224 /*0xE0*/ - 60)
			{
				if (_index == 5)
					TriggerAnimation(_animationTopSplashEnd, new Vector2(_upX - Position.X, _originalY - 224 /*0xE0*/ - Position.Y + 24), true);
				else
					TriggerAnimation(_animationTopSplashStart, new Vector2(_upX - Position.X, _originalY - 224 /*0xE0*/ - Position.Y + 24), true);
			}
			if (_velocity.Y >= 0.0 || Position.Y <= _originalY - 14 || Position.Y <= _originalY - 224 /*0xE0*/ - 60)
				return;
			TriggerAnimation(_animationBottomSplashStart, new Vector2(_upX - Position.X, _originalY - 112 /*0x70*/ - Position.Y));
		}

		private void DrawArc(IObjectRenderer or, LayerViewOptions viewOptions)
		{
			Rectanglei clipRectangle = (Rectanglei)or.ClipRectangle;
			IObjectRenderer objectRenderer = or;
			Rectangle rectangle1 = or.ClipRectangle;
			double x = rectangle1.X;
			int num = _originalY - 288 - 256 /*0x0100*/;
			rectangle1 = Level.Camera.Bounds;
			int y1 = (int)rectangle1.Y;
			double y2 = num - y1;
			rectangle1 = or.ClipRectangle;
			double width = rectangle1.Width;
			Rectangle rectangle2 = new Rectangle(x, y2, width, 512.0);
			objectRenderer.ClipRectangle = rectangle2;
			or.Render(_animationBlob);
			or.ClipRectangle = (Rectangle)clipRectangle;
		}

		private void DrawStraight(IObjectRenderer or, LayerViewOptions viewOptions)
		{
			Rectangle clipRectangle = or.ClipRectangle;
			int num = Math.Min(_downX, _upX);
			or.ClipRectangle = new Rectangle(num - 64 /*0x40*/ - (int)Level.Camera.Bounds.X, _originalY - 288 - (int)Level.Camera.Bounds.Y, 512.0, 256.0);
			or.Render(_animationBlob);
			or.ClipRectangle = clipRectangle;
		}
	}

	public class OverflowInstance : ParticleObject
	{
		public OverflowInstance()
		  : base("/ANIGROUP")
		{
		}

		public void InitOverflow() => _animationInstance.Index = 5;
	}
}
