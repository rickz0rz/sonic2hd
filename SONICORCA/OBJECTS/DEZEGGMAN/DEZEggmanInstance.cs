using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SONICORCA.OBJECTS.DEZEGGMAN.DOOR;

namespace SONICORCA.OBJECTS.DEZEGGMAN;

/// <summary>
/// Eggman in Death Egg Zone, as in Sonic 2 (ObjC6, subtype $A6). He stands behind a barrier (ObjC6 subtype $A8) $48
/// pixels to his left. When a character is less than $5C pixels away, he looks at it for $18 frames, the barrier
/// opens, and he runs to the right at $200. He stays at most $50 pixels in front of the character, and drops a drop of
/// sweat every $20 frames. At x $810 he stops, and when he is on the screen he jumps ($80 to the right, $200 up) into
/// the Death Egg Robot. <see cref="Boarded"/> tells the robot that he jumped. He goes $50 frames later.
/// </summary>
public class DEZEggmanInstance : ActiveObject
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	/// <summary>The position from the object layout, and the positions that the code gives, in original level pixels.</summary>
	private const int StartX = 0x440;

	private const int StartY = 0x168;

	private const int StopX = 0x810;

	private const int DoorX = 0x3F8;

	private const int DoorY = 0x160;

	/// <summary>He starts when a character is less than this x distance away.</summary>
	private const int WakeDistance = 0x5C;

	/// <summary>He stays this far in front of a character that is less than this distance away.</summary>
	private const int LeadDistance = 0x50;

	private const int Gravity = 0x10;

	/// <summary>width_pixels: the object is on the screen when it is this near to the screen.</summary>
	private const int HalfWidth = 0x20;

	private const int StandFrame = 0;

	private const int LookFrame = 1;

	private const int JumpFrame = 2;

	private const int DropFrame = 5;

	private readonly AnimateSpriteScript _animator = new AnimateSpriteScript(
		// Ani_objC5_objC6: 0 - running, 1 - not used here
		new byte[] { 5, 2, 3, 4, 0xFF, 0 },
		new byte[] { 5, 6, 7, 0xFF });

	private readonly List<Drop> _drops = new List<Drop>();

	private AnimationInstance _animation;

	private State _state;

	private int _timer;

	/// <summary>The position from the start position, in original pixels, and the velocity in 1/256 pixel per frame.</summary>
	private Vector2 _offset;

	private Vector2 _velocity;

	private enum State
	{
		Wait,
		Look,
		Run,
		Jump
	}

	private sealed class Drop
	{
		public Vector2 Offset;

		public Vector2 Velocity;

		public int Timer;
	}

	/// <summary>Eggman jumped into the Death Egg Robot (status.npc.p1_standing, which the robot head reads).</summary>
	[HideInEditor]
	public bool Boarded { get; private set; }

	/// <summary>Eggman runs: the barrier opens (status.npc.misc, which the barrier reads).</summary>
	[HideInEditor]
	public bool Running { get; private set; }

	public DEZEggmanInstance() => DesignBounds = new Rectanglei(-256, -256, 512, 512);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DEZEggmanType.AnimationGroupResourceKey));
		_animator.Frame = StandFrame;
		LockLifetime = true;
		// ObjC6_State2_State1: the barrier
		Vector2i doorPosition = Position + new Vector2i((DoorX - StartX) * Scale, (DoorY - StartY) * Scale);
		if (Level.ObjectManager.AddObject(new ObjectPlacement(Type.GetAbsolutePath(DEZEggmanType.DoorResourceKey),
			Level.Map.Layers.IndexOf(Layer), doorPosition, new { })) is DEZEggmanDoorInstance door)
		{
			door.Eggman = this;
		}
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		UpdateDrops();
		switch (_state)
		{
			case State.Wait:
				if (Math.Abs(GetDistanceToCharacter(out _)) < WakeDistance)
				{
					_state = State.Look;
					_timer = 0x18;
					_animator.Frame = LookFrame;
				}
				break;
			case State.Look:
				if (--_timer < 0)
				{
					_state = State.Run;
					Running = true;
					_velocity = new Vector2(0x200, 0);
					_timer = 0x10;
				}
				break;
			case State.Run:
				UpdateRun();
				break;
			case State.Jump:
				if (--_timer < 0)
				{
					FinishForever();
					return;
				}
				_velocity.Y += Gravity;
				Move();
				break;
		}
		PositionPrecise = ToLevel(_offset);
	}

	/// <summary>ObjC6_State2_State4: run to x $810, then jump when on the screen.</summary>
	private void UpdateRun()
	{
		if (StartX + _offset.X >= StopX)
		{
			_animator.Frame = JumpFrame;
			_velocity = default;
			if (IsOnScreen())
			{
				_state = State.Jump;
				_velocity = new Vector2(0x80, -0x200);
				_timer = 0x50;
				Boarded = true;
				// Behind the robot
				Priority--;
			}
			return;
		}
		double distance = GetDistanceToCharacter(out ICharacter character);
		if (character != null && distance >= -LeadDistance && distance < LeadDistance)
		{
			_offset.X = (double)(character.Position.X - Position.X) / Scale + _offset.X + LeadDistance;
		}
		if (--_timer < 0)
		{
			_timer = 0x20;
			// loc_3D00C: a drop of sweat
			_drops.Add(new Drop { Offset = _offset - new Vector2(0, 0x18), Velocity = new Vector2(-0x100, 0), Timer = 8 });
		}
		Move();
		_animator.Step();
	}

	/// <summary>ObjC6_State4: each drop falls with gravity $10 and goes after 8 frames.</summary>
	private void UpdateDrops()
	{
		foreach (Drop drop in _drops)
		{
			drop.Timer--;
			drop.Velocity.Y += Gravity;
			drop.Offset += drop.Velocity / 256.0;
		}
		_drops.RemoveAll(drop => drop.Timer < 0);
	}

	private void Move() => _offset += _velocity / 256.0;

	/// <summary>
	/// Obj_GetOrientationToPlayer: the x distance from the nearer character to Eggman, in original pixels. It is
	/// positive when the character is on the left.
	/// </summary>
	private double GetDistanceToCharacter(out ICharacter nearest)
	{
		nearest = null;
		double best = double.MaxValue;
		foreach (ICharacter character in new[] { Level.Player.Protagonist, Level.Player.Sidekick })
		{
			if (character == null)
			{
				continue;
			}
			double distance = (double)(Position.X - character.Position.X) / Scale;
			if (nearest == null || Math.Abs(distance) < Math.Abs(best))
			{
				nearest = character;
				best = distance;
			}
		}
		return nearest == null ? double.MaxValue : best;
	}

	private bool IsOnScreen() => Level.Camera.Bounds.IntersectsWith(new Rectangle(Position.X - HalfWidth * Scale, Position.Y - HalfWidth * Scale,
		HalfWidth * 2 * Scale, HalfWidth * 2 * Scale));

	private Vector2 ToLevel(Vector2 offset) => new Vector2(Entry.Position.X + offset.X * Scale, Entry.Position.Y + offset.Y * Scale);

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		foreach (Drop drop in _drops)
		{
			_animation.Index = DropFrame;
			objectRenderer.RenderOriginal(_animation, (drop.Offset - _offset) * Scale);
		}
		_animation.Index = _animator.Frame;
		objectRenderer.RenderOriginal(_animation);
	}
}
