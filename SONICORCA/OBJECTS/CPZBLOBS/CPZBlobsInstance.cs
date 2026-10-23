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

/// <summary>
/// A group of six chemical blobs in Chemical Plant Zone. The blobs jump out of one pipe and fall into another,
/// in an arc or straight up and down. The blobs hurt characters.
/// This object also draws the splash animations at the ends of the pipes.
/// </summary>
public class CPZBlobsInstance : ActiveObject
{
	private const int AnimationBlob = 0;
	private const int AnimationTopSplashStart = 1;
	private const int AnimationTopSplashEnd = 2;
	private const int AnimationBottomSplashStart = 3;
	private const int AnimationBottomSplashEnd = 4;
	private const int AnimationBottomOverflow = 5;
	private const int BlobCount = 6;
	private AnimationGroup _animationGroup;
	private ArrangementType _arrangementType;
	private Blob[] _children;
	private List<AnimationInstance> _bottomStartOverflowTriggeredAnimations = new List<AnimationInstance>();

	/// <summary>The splash animations that play now, with the draw offset of each.</summary>
	private Dictionary<AnimationInstance, Vector2> _triggeredAnimations = new Dictionary<AnimationInstance, Vector2>();

	/// <summary>The position of the blob that started each splash animation.</summary>
	private Dictionary<AnimationInstance, Vector2> _triggeredAnimationsPositions = new Dictionary<AnimationInstance, Vector2>();

	[StateVariable]
	private ArrangementType Arrangement
	{
		get => _arrangementType;
		set => _arrangementType = value;
	}

	[StateVariable]
	private bool FlipX { get; set; }

	protected override void OnStart()
	{
		_animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type.GetAbsolutePath("/ANIGROUP"));
		DesignBounds = new Rectanglei(-128, -128, 256, 256);
		_children = new Blob[BlobCount];
		for (int index = BlobCount - 1; index >= 0; --index)
		{
			Blob blob = Level.ObjectManager.AddSubObject<Blob>(this);
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

		// The splash animations advance here, not in OnAnimate.
		List<AnimationInstance> finishedAnimations = new List<AnimationInstance>();
		foreach (KeyValuePair<AnimationInstance, Vector2> triggeredAnimation in TriggeredAnimations)
		{
			triggeredAnimation.Key.Animate();
			if (triggeredAnimation.Key.Cycles > 0)
			{
				finishedAnimations.Add(triggeredAnimation.Key);
			}
		}

		// Remove the splashes that finished. Some of them start an overflow particle.
		foreach (AnimationInstance animation in finishedAnimations)
		{
			animation.ResetFrame();
			animation.Cycles = 0;
			if (animation.Index == AnimationBottomSplashEnd || BottomStartOverflowTriggeredAnimations.Contains(animation))
			{
				OverflowInstance overflowInstance = Level.ObjectManager.AddSubObject<OverflowInstance>(this);
				overflowInstance.InitOverflow();
				overflowInstance.Layer = Level.Map.Layers.Last();
				Vector2 blobPosition = TriggeredAnimationsPositions[animation];
				if (BottomStartOverflowTriggeredAnimations.Contains(animation))
				{
					overflowInstance.PositionPrecise = new Vector2(blobPosition.X, blobPosition.Y - 40.0);
				}
				else if (_arrangementType == ArrangementType.Arc)
				{
					overflowInstance.PositionPrecise = new Vector2(blobPosition.X, blobPosition.Y - 15.0);
				}
				else
				{
					overflowInstance.PositionPrecise = new Vector2(blobPosition.X, blobPosition.Y - 23.0);
				}
			}
			if (BottomStartOverflowTriggeredAnimations.Contains(animation))
			{
				BottomStartOverflowTriggeredAnimations.Remove(animation);
			}
			TriggeredAnimations.Remove(animation);
			TriggeredAnimationsPositions.Remove(animation);
		}

		foreach (KeyValuePair<AnimationInstance, Vector2> triggeredAnimation in TriggeredAnimations)
		{
			Vector2 blobPosition = TriggeredAnimationsPositions[triggeredAnimation.Key];
			objectRenderer.Render(triggeredAnimation.Key, blobPosition - PositionPrecise + triggeredAnimation.Value);
		}
	}

	private enum ArrangementType
	{
		Arc,
		Straight,
	}

	/// <summary>One chemical blob. It moves between the two pipe ends and starts the splash animations.</summary>
	private class Blob : Enemy
	{
		/// <summary>The index of the last blob in the group. The last blob starts the end splash.</summary>
		private const int LastBlobIndex = BlobCount - 1;

		/// <summary>The horizontal distance between the two pipe ends.</summary>
		private const int PipeDistance = 384;

		/// <summary>The angle in radians that an arc blob turns each frame.</summary>
		private const double ArcAngleStep = 0.031939525311496228;

		/// <summary>The height above the bottom pipe of the top splash in the straight arrangement.</summary>
		private const int TopSplashHeight = 224;

		private const int SplashStartOffsetY = 112;
		private const int SplashEndOffsetY = 120;
		private const int BottomWaitDuration = 72;

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

		/// <summary>The X position of the pipe end where the blob goes up.</summary>
		private int _upX;

		/// <summary>The X position of the pipe end where the blob comes down.</summary>
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
			_animationTopSplashStart = new AnimationInstance(Host._animationGroup, AnimationTopSplashStart);
			_animationTopSplashEnd = new AnimationInstance(Host._animationGroup, AnimationTopSplashEnd);
			_animationBottomSplashStart = new AnimationInstance(Host._animationGroup, AnimationBottomSplashStart);
			_animationBottomSplashEnd = new AnimationInstance(Host._animationGroup, AnimationBottomSplashEnd);
			_animationBottomOverflow = new AnimationInstance(Host._animationGroup, AnimationBottomOverflow);
			Priority = -128;
			CollisionRectangles = new CollisionRectangle[1]
			{
				new CollisionRectangle(this, 0, -16, -16, 32, 32)
			};
		}

		public void Initialise(Vector2i position, int index, bool flipX)
		{
			_index = index;
			_upVelocity = Host._arrangementType != ArrangementType.Arc ? -9.0 : -17.55;
			_velocity = new Vector2(0.0, _upVelocity);
			Position = position;
			_originalY = Host._arrangementType != ArrangementType.Arc ? position.Y : position.Y + 8;
			if (flipX)
			{
				_leftToRight = false;
				_upX = position.X;
				_downX = position.X + PipeDistance;
				_currentAngle = Math.PI;
			}
			else
			{
				_leftToRight = true;
				_upX = position.X + PipeDistance;
				_downX = position.X;
				_currentAngle = 0.0;
			}
			Delay = Host._arrangementType != ArrangementType.Arc ? index * 4 : index * 5;
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (Host.Finished)
			{
				Finish();
			}
			else if (Delay > 0)
			{
				--Delay;
			}
			else if (Host._arrangementType == ArrangementType.Arc)
			{
				UpdateArc();
			}
			else
			{
				UpdateStraight();
			}
		}

		/// <summary>
		/// Moves the blob along a half circle between the two pipe ends. The X position follows the circle.
		/// The Y position follows the velocity and gravity.
		/// </summary>
		private void UpdateArc()
		{
			ArcPrepareTriggers();
			if (_leftToRight && _currentAngle <= 0.0)
			{
				PlayGloopSound();
			}
			if (!_leftToRight && _currentAngle >= Math.PI)
			{
				PlayGloopSound();
			}
			Vector2 centre = new Vector2((_downX + _upX) * 0.5, _originalY);
			double radius = Vector2.GetDistance(new Vector2(_downX, 0.0), new Vector2(_upX, 0.0)) * 0.5;
			double startX = centre.X - radius;
			if (_leftToRight)
			{
				_currentAngle += ArcAngleStep;
			}
			else
			{
				_currentAngle -= ArcAngleStep;
			}
			PositionPrecise = PositionPrecise + _velocity;
			Vector2 startPoint = new Vector2(startX, _originalY);
			PositionPrecise = new Vector2(Vector2.GetPointRotatedFromRelative(centre, startPoint, _currentAngle).X, PositionPrecise.Y);
			_velocity.Y += 0.375;
			if (PositionPrecise.Y >= _originalY && !_directionChange)
			{
				_directionChange = true;
				Delay = BottomWaitDuration;
			}
			if (!_directionChange)
			{
				return;
			}
			// The blob is back at the bottom. It jumps again from the other pipe end, in the other direction.
			_velocity.Y = _upVelocity;
			_directionChange = false;
			_leftToRight = !_leftToRight;
			_currentAngle = _leftToRight ? 0.0 : Math.PI;
			PositionPrecise = new Vector2(_leftToRight ? _downX : _upX, _originalY);
		}

		/// <summary>Moves the blob straight up out of one pipe end and straight down into the other.</summary>
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
				Delay = BottomWaitDuration;
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
			if (_startAnimation)
			{
				_animationBlob.Animate();
			}
		}

		/// <summary>
		/// Starts a splash animation on the host, at <paramref name="position"/> relative to this blob.
		/// The same animation does not start two times in a row, unless <paramref name="isFinal"/> is true.
		/// </summary>
		private void TriggerAnimation(AnimationInstance animation, Vector2 position, bool isFinal = false)
		{
			if (_lastTriggeredAnimation == animation && !isFinal)
			{
				return;
			}
			_lastTriggeredAnimation = animation;
			if (isFinal)
			{
				_lastTriggeredAnimation = null;
			}
			if (!Host.TriggeredAnimations.Any(a => a.Key.Index == animation.Index) && !Host.TriggeredAnimationsPositions.Any(p => p.Value == PositionPrecise))
			{
				animation.ResetFrame();
				animation.Cycles = 0;
				Host.TriggeredAnimations.Add(animation, position);
				Host.TriggeredAnimationsPositions.Add(animation, PositionPrecise);
			}
			if (_index != LastBlobIndex || animation.Index != AnimationBottomSplashStart)
			{
				return;
			}
			// The bottom splash of the last blob also starts an overflow when it finishes.
			if (Host._arrangementType == ArrangementType.Straight)
			{
				AddLastBottomSplashStartToOverflow();
			}
			else if (Host._arrangementType == ArrangementType.Arc && _velocity.Y < 0.0 && Host.BottomStartOverflowTriggeredAnimations.Count == 0)
			{
				AddLastBottomSplashStartToOverflow();
			}
		}

		private void AddLastBottomSplashStartToOverflow()
		{
			KeyValuePair<AnimationInstance, Vector2> lastBottomSplashStart = Host.TriggeredAnimations.LastOrDefault(a => a.Key.Index == AnimationBottomSplashStart);
			if (lastBottomSplashStart.Key != null)
			{
				Host.BottomStartOverflowTriggeredAnimations.Add(lastBottomSplashStart.Key);
			}
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			if (Host._arrangementType == ArrangementType.Straight)
			{
				StraightPrepareTriggers();
			}
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			if (Host._arrangementType == ArrangementType.Arc)
			{
				DrawArc(objectRenderer, viewOptions);
			}
			else
			{
				DrawStraight(objectRenderer, viewOptions);
			}
		}

		/// <summary>Returns the offset from this blob to the point that is <paramref name="offsetY"/> above the pipe end at <paramref name="pipeX"/>.</summary>
		private Vector2 GetSplashOffset(int pipeX, int offsetY)
		{
			return new Vector2(pipeX - Position.X, _originalY - offsetY - Position.Y);
		}

		/// <summary>Starts the splash animations when an arc blob touches one of the pipe ends.</summary>
		private void ArcPrepareTriggers()
		{
			Rectanglei bounds = CollisionRectangles[0].AbsoluteBounds;
			Rectangle downPipeArea = Rectangle.FromLTRB(_downX - bounds.Width * 0.5, _originalY - bounds.Height, _downX + bounds.Width * 0.5, _originalY + bounds.Height * 0.5);
			Rectangle upPipeArea = Rectangle.FromLTRB(_upX - bounds.Width * 0.5, _originalY - bounds.Height, _upX + bounds.Width * 0.5, _originalY + bounds.Height * 0.5);
			if (bounds.IntersectsWith((Rectanglei)upPipeArea) && !_leftToRight)
			{
				_startAnimation = true;
				TriggerAnimation(_animationBottomSplashStart, GetSplashOffset(_upX, SplashStartOffsetY), true);
			}
			if (bounds.IntersectsWith((Rectanglei)downPipeArea) && _leftToRight)
			{
				_startAnimation = true;
				TriggerAnimation(_animationBottomSplashStart, GetSplashOffset(_downX, SplashStartOffsetY));
			}
			if (bounds.IntersectsWith((Rectanglei)upPipeArea) && _leftToRight)
			{
				TriggerArcEndSplash(_upX);
			}
			if (bounds.IntersectsWith((Rectanglei)downPipeArea) && !_leftToRight)
			{
				TriggerArcEndSplash(_downX);
			}
		}

		private void TriggerArcEndSplash(int pipeX)
		{
			if (_index == LastBlobIndex)
			{
				TriggerAnimation(_animationBottomSplashEnd, GetSplashOffset(pipeX, SplashEndOffsetY), true);
			}
			else
			{
				TriggerAnimation(_animationBottomSplashStart, GetSplashOffset(pipeX, SplashStartOffsetY), true);
			}
		}

		/// <summary>Starts the splash animations when a straight blob passes the pipe ends.</summary>
		private void StraightPrepareTriggers()
		{
			if (_velocity.Y > 0.0 && Position.Y > _originalY - TopSplashHeight && Position.Y < _originalY - 14)
			{
				_startAnimation = true;
				TriggerAnimation(_animationTopSplashStart, new Vector2(_downX - Position.X, _originalY - TopSplashHeight - Position.Y));
			}
			if (_velocity.Y > 0.0 && Position.Y > _originalY - 14)
			{
				if (_index == LastBlobIndex)
				{
					TriggerAnimation(_animationBottomSplashEnd, new Vector2(_downX - Position.X, _originalY - SplashEndOffsetY - Position.Y));
				}
				else
				{
					TriggerAnimation(_animationBottomSplashStart, new Vector2(_downX - Position.X, _originalY - SplashStartOffsetY - Position.Y), true);
				}
			}
			if (_velocity.Y < 0.0 && Position.Y < _originalY - TopSplashHeight - 60)
			{
				if (_index == LastBlobIndex)
				{
					TriggerAnimation(_animationTopSplashEnd, new Vector2(_upX - Position.X, _originalY - TopSplashHeight - Position.Y + 24), true);
				}
				else
				{
					TriggerAnimation(_animationTopSplashStart, new Vector2(_upX - Position.X, _originalY - TopSplashHeight - Position.Y + 24), true);
				}
			}
			if (_velocity.Y < 0.0 && Position.Y > _originalY - 14 && Position.Y > _originalY - TopSplashHeight - 60)
			{
				TriggerAnimation(_animationBottomSplashStart, new Vector2(_upX - Position.X, _originalY - SplashStartOffsetY - Position.Y));
			}
		}

		/// <summary>Draws the blob, clipped so that it does not show outside the pipes.</summary>
		private void DrawArc(IObjectRenderer objectRenderer, LayerViewOptions viewOptions)
		{
			// The clip rectangle is saved as integers, so the restored value is rounded.
			Rectanglei savedClipRectangle = (Rectanglei)objectRenderer.ClipRectangle;
			double clipX = objectRenderer.ClipRectangle.X;
			double clipY = _originalY - 288 - 256 - (int)Level.Camera.Bounds.Y;
			double clipWidth = objectRenderer.ClipRectangle.Width;
			objectRenderer.ClipRectangle = new Rectangle(clipX, clipY, clipWidth, 512.0);
			objectRenderer.Render(_animationBlob);
			objectRenderer.ClipRectangle = (Rectangle)savedClipRectangle;
		}

		/// <summary>Draws the blob, clipped so that it does not show outside the pipes.</summary>
		private void DrawStraight(IObjectRenderer objectRenderer, LayerViewOptions viewOptions)
		{
			Rectangle savedClipRectangle = objectRenderer.ClipRectangle;
			int left = Math.Min(_downX, _upX);
			objectRenderer.ClipRectangle = new Rectangle(left - 64 - (int)Level.Camera.Bounds.X, _originalY - 288 - (int)Level.Camera.Bounds.Y, 512.0, 256.0);
			objectRenderer.Render(_animationBlob);
			objectRenderer.ClipRectangle = savedClipRectangle;
		}
	}

	/// <summary>A particle of chemical that overflows from a pipe end after a splash.</summary>
	public class OverflowInstance : ParticleObject
	{
		public OverflowInstance()
			: base("/ANIGROUP")
		{
		}

		public void InitOverflow() => _animationInstance.Index = AnimationBottomOverflow;
	}
}
