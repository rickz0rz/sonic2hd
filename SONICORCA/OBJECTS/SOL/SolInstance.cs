using System;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SOL;

/// <summary>
/// Sol, a badnik that moves slowly to the left with four fireballs that turn around it. When the protagonist comes
/// near, it fires each fireball to the left when that fireball gets to the bottom of the circle. A flipped Sol moves
/// to the right, turns its fireballs the other way and fires them to the right.
/// </summary>
public class SolInstance : Badnik
{
	private const int AnimationSol = 0;
	private const int AnimationFireball = 2;
	private const int FireballCount = 4;
	private const int DetectDistanceX = 640;
	private const int DetectDistanceY = 320;

	/// <summary>
	/// Original: the ticks between the detection and the first fire. The Sol shows the first frame of its fire
	/// animation for 16 ticks, and the fireballs fire only on the second frame (Obj95_FireballUpdate).
	/// </summary>
	private const int WindUpTicksOriginal = 16;

	private AnimationInstance _animation;
	private bool _waitingForCharacter;
	private int _windUpTicks;

	/// <summary>The Sol moves to the right and fires to the right (status.npc.x_flip).</summary>
	[StateVariable]
	private bool FlipX { get; set; }

	private int Direction => FlipX ? 1 : -1;

	/// <summary>True when the fireballs can fire: the Sol saw the protagonist and finished its wind-up.</summary>
	private bool CanFire => !_waitingForCharacter && _windUpTicks == 0;

	public SolInstance() => DesignBounds = new Rectanglei(-36, -36, 72, 72);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(SolType.AnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -36, -36, 72, 72)
		};
		_waitingForCharacter = true;

		// Space the fireballs evenly on the circle.
		int angle = 0;
		for (int index = 0; index < FireballCount; ++index)
		{
			Fireball fireball = Level.ObjectManager.AddSubObject<Fireball>(this);
			fireball.Angle = angle;
			fireball.UpdateOrbitPosition();
			angle += Fireball.FullCircle / FireballCount;
		}
	}

	protected override void OnStop()
	{
		base.OnStop();
		if (_waitingForCharacter)
			return;
		FinishForever();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		Position = Position + new Vector2i(Direction, 0);
		if (_windUpTicks > 0)
			--_windUpTicks;
		if (!_waitingForCharacter || !IsCharacterCloseEnough(Level.Player.Protagonist))
			return;
		_waitingForCharacter = false;
		if (Level.OriginalPhysics)
			_windUpTicks = WindUpTicksOriginal;
	}

	private bool IsCharacterCloseEnough(ICharacter character)
	{
		if (character.IsDebug)
			return false;
		Vector2i distance = character.Position - Position;
		distance.X = Math.Abs(distance.X);
		distance.Y = Math.Abs(distance.Y);
		return distance.X < DetectDistanceX && distance.Y < DetectDistanceY;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}

	/// <summary>A fireball that turns around a Sol until the Sol fires it to the left.</summary>
	private class Fireball : Enemy
	{
		/// <summary>The number of angle units in a full circle.</summary>
		public const int FullCircle = 256;

		/// <summary>The angle at the bottom of the circle, where the fireball is fired.</summary>
		private const int FireAngle = 64;

		private const double FireSpeed = 8.0;
		private AnimationInstance _animation;
		private bool _fired;
		private int _fireDirection;

		public int Angle { get; set; }

		public SolInstance Sol => (SolInstance)ParentObject;

		protected override void OnStart()
		{
			base.OnStart();
			_animation = new AnimationInstance(Sol._animation.AnimationGroup, AnimationFireball);
			// The original box (size $18) is 8 by 8 pixels. The HD box is half that size.
			int size = Level.OriginalPhysics ? 32 : 16;
			CollisionRectangles = new CollisionRectangle[]
			{
				new CollisionRectangle(this, 0, -size / 2, -size / 2, size, size)
			};
			LockLifetime = true;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (!_fired)
			{
				if (Sol.Finished)
					Finish();
				else if (!Sol.CanFire || Angle != FireAngle)
				{
					Angle = MathX.Wrap(Angle - Sol.Direction, FullCircle);
					UpdateOrbitPosition();
				}
				else
				{
					_fired = true;
					_fireDirection = Sol.Direction;
					LockLifetime = false;
				}
			}
			else
				PositionPrecise = PositionPrecise + new Vector2(FireSpeed * _fireDirection, 0.0);
		}

		protected override void OnUpdateEditor()
		{
			base.OnUpdateEditor();
			UpdateOrbitPosition();
		}

		public void UpdateOrbitPosition()
		{
			double radians = Angle * (Math.PI / 128.0);
			PositionPrecise = Sol.PositionPrecise + new Vector2(64.0 * Math.Cos(radians), 64.0 * Math.Sin(radians));
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
