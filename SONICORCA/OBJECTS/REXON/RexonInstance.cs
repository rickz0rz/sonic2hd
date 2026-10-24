using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SONICORCA.OBJECTS.REXON;

/// <summary>
/// Rexon, a badnik that swims in lava. When a character comes near, it lifts a neck of five segments with a head on
/// top. The head fires projectiles at the character. When the head is destroyed, the neck segments fall away.
/// The body itself is a solid platform.
/// </summary>
public class RexonInstance : ActiveObject
{
	private const int AnimationBody = 0;
	private const int AnimationNeck = 1;
	private const int AnimationMouth = 2;
	private const int AnimationHead = 3;
	private const int AnimationMouthFire = 4;
	private const int AnimationHeadFire = 5;
	private const int AnimationProjectile = 6;

	/// <summary>The number of neck segments. The last segment is the head.</summary>
	private const int SegmentCount = 5;
	private const int HeadIndex = SegmentCount - 1;

	private const int TurnTicks = 128;
	private const int DetectDistanceX = 640;

	/// <summary>
	/// Original: the Rexon sees a character up to 160 pixels to its left, but only up to 96 pixels to its right
	/// (Obj94_WaitForPlayer).
	/// </summary>
	private const int DetectDistanceLeftOriginal = 640;
	private const int DetectDistanceRightOriginal = 384;
	private const double SwimSpeed = 0.5;

	private AnimationGroup _animationGroup;
	private AnimationInstance _animationBody;
	private AnimationInstance _animationNeck;
	private AnimationInstance _animationMouth;
	private AnimationInstance _animationHead;
	private Vector2 _velocity;
	private bool _headLifted;
	private bool _facingRight;
	private int _turnTicksRemaining;
	private readonly Neck[] _necks = new Neck[SegmentCount];
	private bool _headIsAlive;

	private HeadBack Head => (HeadBack)_necks[HeadIndex];

	public RexonInstance() => DesignBounds = new Rectanglei(-64, -32, 128, 64);

	protected override void OnStart()
	{
		base.OnStart();
		_animationGroup = ResourceTree.GetLoadedResource<AnimationGroup>(Type, RexonType.AnimationGroupResourceKey);
		_animationBody = new AnimationInstance(_animationGroup);
		_animationNeck = new AnimationInstance(_animationGroup, AnimationNeck);
		_animationMouth = new AnimationInstance(_animationGroup, AnimationMouth);
		_animationHead = new AnimationInstance(_animationGroup, AnimationHead);
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-64, -32, 128, 64));
		_velocity = new Vector2(-SwimSpeed, 0.0);
		_turnTicksRemaining = TurnTicks;
	}

	protected override void OnUpdate()
	{
		if (!_headLifted)
			WaitForCharacter();
		if (Head == null)
			return;
		_headIsAlive = !Head.Finished;
	}

	/// <summary>Swims left and right until a character comes near, then lifts the neck and head.</summary>
	private void WaitForCharacter()
	{
		ICharacter closestCharacter = Level.ObjectManager.GetClosestCharacterTo((Vector2)Position);
		if (closestCharacter != null && IsInDetectRange(closestCharacter.Position.X - Position.X))
		{
			_facingRight = closestCharacter.Position.X > Position.X;
			_velocity = new Vector2();
			CreateNeckAndHead();
		}
		else
		{
			if (_turnTicksRemaining-- <= 0)
			{
				_velocity.X *= -1.0;
				_facingRight = !_facingRight;
				_turnTicksRemaining = TurnTicks;
			}
			PositionPrecise = PositionPrecise + _velocity;
		}
	}

	private bool IsInDetectRange(int distanceX)
	{
		if (Level.OriginalPhysics)
			return distanceX > -DetectDistanceLeftOriginal && distanceX <= DetectDistanceRightOriginal;
		return Math.Abs(distanceX) < DetectDistanceX;
	}

	/// <summary>Creates the head first, then the neck segments down to the body, and links each one to the next.</summary>
	private void CreateNeckAndHead()
	{
		_headIsAlive = true;
		_headLifted = true;
		Neck upperNeck = null;
		for (int index = HeadIndex; index >= 0; --index)
		{
			Neck neck = index == HeadIndex ? Level.ObjectManager.AddSubObject<HeadBack>(this) : Level.ObjectManager.AddSubObject<Neck>(this);
			_necks[index] = neck;
			neck.Prepare(index);
			if (upperNeck != null)
			{
				neck.NextNeck = upperNeck;
				upperNeck.PreviousNeck = neck;
			}
			upperNeck = neck;
		}
	}

	protected override void OnAnimate()
	{
		if (_headIsAlive)
		{
			// Open the mouth a short time before the head fires.
			if (Head.NextProjectileTicksRemaining < _animationGroup[AnimationMouthFire].Duration - 4)
				_animationMouth.Index = AnimationMouthFire;
			if (Head.NextProjectileTicksRemaining < _animationGroup[AnimationHeadFire].Duration - 4)
				_animationHead.Index = AnimationHeadFire;
		}
		_animationBody.Animate();
		_animationNeck.Animate();
		_animationMouth.Animate();
		_animationHead.Animate();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animationBody, _facingRight);
	}

	/// <summary>
	/// One segment of the Rexon neck. Each segment rises out of the body in turn, then swings on an arc above
	/// the segment below it. Segment 0 is next to the body.
	/// </summary>
	private class Neck : Badnik
	{
		/// <summary>The oscillation cycle length in ticks.</summary>
		private const int OscillationPeriod = 128;
		private const double DropGravity = 0.875;

		private static readonly IReadOnlyList<int> NeckRaiseTicks = new int[]
		{
			30,
			24,
			18,
			12,
			6,
			0
		};

		/// <summary>The start phase of each segment in the oscillation cycle, in units of 4 ticks.</summary>
		private static readonly IReadOnlyList<int> OscillationStartPhases = new int[]
		{
			0,
			36,
			32,
			28,
			26
		};

		private static readonly IReadOnlyList<double> DeathXVelocities = new double[]
		{
			2.0,
			-4.0,
			4.0,
			-2.0,
			2.0
		};

		private int _index;
		private Vector2 _velocity;
		private int _oscillation;

		public RexonInstance Rexon => (RexonInstance)ParentObject;

		/// <summary>The segment below this one, nearer to the body.</summary>
		public Neck PreviousNeck { get; set; }

		/// <summary>The segment above this one, nearer to the head.</summary>
		public Neck NextNeck { get; set; }

		protected NeckState State { get; set; }

		protected int StateTicksRemaining { get; set; }

		public virtual void Prepare(int index)
		{
			_index = index;
			StateTicksRemaining = NeckRaiseTicks[_index];
			_oscillation += _index * 16;
			// The original (Obj97_Init) starts the segments 24 pixels to the left, or 40 pixels to the right when the
			// Rexon faces right. The rise speed does not change with the direction, so these offsets centre the neck.
			Vector2i offset = Level.OriginalPhysics ? new Vector2i(Rexon._facingRight ? 160 : -96, 64) : new Vector2i(Rexon._facingRight ? 80 : -80, 64);
			Position = Position + offset;
			Priority = ParentObject.Priority + 1 + _index;
			// The original box (size $B) is 16 by 16 pixels. The HD box is half that size.
			int size = Level.OriginalPhysics ? 64 : 32;
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -size / 2, -size / 2, size, size)
			};
			CanBeDestoryed = false;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			switch (State)
			{
				case NeckState.PreRise:
					CheckHeadIsAlive();
					if (StateTicksRemaining-- >= 0)
						break;
					StartRaise();
					break;
				case NeckState.Rise:
					CheckHeadIsAlive();
					_velocity.X += 0.25;
					if (StateTicksRemaining-- < 0)
						StartNormalState();
					PositionPrecise = PositionPrecise + _velocity;
					break;
				case NeckState.Oscillate:
					CheckHeadIsAlive();
					Oscillate();
					break;
				case NeckState.Drop:
					_velocity.Y += DropGravity;
					PositionPrecise = PositionPrecise + _velocity;
					break;
			}
		}

		private void StartRaise()
		{
			State = NeckState.Rise;
			// The original rise speed is -$120, which is -4.5 units for each tick. HD uses -0.45.
			_velocity = new Vector2(Level.OriginalPhysics ? -4.5 : -0.45, -8.0);
			StateTicksRemaining = NeckRaiseTicks[HeadIndex - _index];
		}

		private void StartNormalState()
		{
			State = NeckState.Oscillate;
			_velocity = new Vector2();
			StateTicksRemaining = 32;
			_oscillation = OscillationStartPhases[_index] * 4;
		}

		/// <summary>Starts to drop this segment when the head is destroyed.</summary>
		private void CheckHeadIsAlive()
		{
			if (Rexon._headIsAlive)
				return;
			State = NeckState.Drop;
			_velocity.X = DeathXVelocities[_index];
		}

		/// <summary>Moves this segment on an arc above the segment below it.</summary>
		private void Oscillate()
		{
			_oscillation = (_oscillation + 1) % OscillationPeriod;
			double angle = _oscillation / 128.0 * (2.0 * Math.PI);
			int x = (int)(Math.Cos(angle) * 24.0);
			int y = (int)(-Math.Abs(Math.Sin(angle)) * 8.0) - 52;
			if (this is HeadBack)
				y += 16;
			if (PreviousNeck != null)
				Position = PreviousNeck.Position + new Vector2i(x, y);
			if (_index != HeadIndex)
				return;
			Position = Position + new Vector2i(Rexon._facingRight ? 18 : -18, 0);
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			// The neck animation has one frame for each step of the oscillation. Show the frame that points to the
			// next segment.
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			Animation animation = Rexon._animationNeck.Animation;
			int frameCount = animation.Frames.Count;
			Animation.Frame frame = animation.Frames[MathX.Clamp(0, (NextNeck._oscillation + (Rexon._facingRight ? 44 : 108)) % OscillationPeriod, frameCount - 1)];
			Rectanglei source = frame.Source;
			objectRenderer.Texture = Rexon._animationNeck.AnimationGroup.Textures[frame.TextureIndex];
			objectRenderer.Render((Rectangle)source, (Vector2)new Vector2i(), Rexon._facingRight);
		}

		protected enum NeckState
		{
			PreRise,
			Rise,
			Oscillate,
			Drop,
		}
	}

	/// <summary>
	/// The top segment of the neck: the back of the head, with the mouth. Characters can destroy it, and it fires
	/// projectiles.
	/// </summary>
	private class HeadBack : Neck
	{
		private HeadFront _headFront;

		public int NextProjectileTicksRemaining
		{
			get
			{
				return State != NeckState.Oscillate ? sbyte.MaxValue : StateTicksRemaining;
			}
		}

		public override void Prepare(int index)
		{
			base.Prepare(index);
			_headFront = Level.ObjectManager.AddSubObject<HeadFront>(this);
			CanBeDestoryed = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (State == NeckState.Oscillate)
			{
				if (StateTicksRemaining-- < 0)
					FireProjectile();
			}
			if (_headFront == null)
				return;
			_headFront.Position = Position;
		}

		private void FireProjectile()
		{
			StateTicksRemaining = sbyte.MaxValue;
			Projectile projectile = Level.ObjectManager.AddSubObject<Projectile>(ParentObject);
			projectile.Position = Position + new Vector2i(Rexon._facingRight ? 16 : -16, 4);
			projectile.Velocity = new Vector2(4 * (Rexon._facingRight ? 1 : -1), 2.0);
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			renderer.GetObjectRenderer().Render(Rexon._animationMouth, Rexon._facingRight);
		}
	}

	/// <summary>The front of the Rexon head. It draws above the other objects and follows the head back.</summary>
	private class HeadFront : ActiveObject
	{
		public Neck Parent => ParentObject as Neck;

		protected override void OnStart()
		{
			LockLifetime = true;
			Priority = 512;
		}

		protected override void OnUpdate()
		{
			if (!Parent.Finished)
				return;
			Finish();
		}

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			RexonInstance rexon = ParentObject.ParentObject as RexonInstance;
			renderer.GetObjectRenderer().Render(rexon._animationHead, rexon._facingRight);
		}
	}

	/// <summary>A projectile that the Rexon head fires. It moves in a straight line and hurts characters.</summary>
	private class Projectile : Enemy
	{
		private AnimationInstance _animation;

		public Vector2 Velocity { get; set; }

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(RexonType.AnimationGroupResourceKey), AnimationProjectile);
			// The original box (size $18) is 8 by 8 pixels.
			int size = Level.OriginalPhysics ? 32 : 50;
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -size / 2, -size / 2, size, size)
			};
			Priority += 32;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			PositionPrecise = PositionPrecise + Velocity;
		}

		protected override void OnAnimate() => _animation.Animate();

		protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
		{
			IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
			objectRenderer.EmitsLight = true;
			objectRenderer.Render(_animation);
		}
	}
}
