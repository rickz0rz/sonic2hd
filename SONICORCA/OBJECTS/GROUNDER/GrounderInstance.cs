using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.GROUNDER;

/// <summary>
/// Grounder, as in Sonic 2 (Obj8D and Obj8E). A Grounder in a wall waits behind 4 bricks. When the nearer player is
/// within 96 pixels on the x axis, the bricks and 5 rocks fly out, and the Grounder comes out in 16 frames. Then it
/// walks toward that player at $100 along the floor. At a wall or a ledge it waits 59 frames and turns. A free
/// Grounder starts to walk at once.
/// </summary>
public class GrounderInstance : Badnik
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	/// <summary>Gravity for the bricks and the rocks: $38.</summary>
	private const int Gravity = 0x38;

	private const int Radius = 0x14;

	private const int TriggerDistance = 0x60;

	private const int PauseTicks = 0x3B;

	private const int TouchWidth = 12 * Scale;

	private const int TouchHeight = 20 * Scale;

	private const int DebrisTicks = 180;

	/// <summary>The bricks: the offset from the Grounder in original pixels, and the speed when they fly out.</summary>
	private static readonly int[,] Bricks = { { 0, -0x14, 0x100, -0x200 }, { 0x10, -4, 0x100, -0x100 }, { 0, 0xC, -0x100, -0x200 }, { -0x10, -4, -0x100, -0x100 } };

	/// <summary>The rocks: the speed and the mapping frame.</summary>
	private static readonly int[,] Rocks = { { -0x100, -0x400, 0 }, { 0x400, -0x300, 2 }, { 0x200, 0, 0 }, { -0x300, -0x100, 1 }, { -0x300, -0x300, 0 } };

	private readonly List<Debris> _debris = new List<Debris>();

	private AnimationInstance _animation;

	private AnimationInstance _brickAnimation;

	private AnimationInstance _rockAnimation;

	private bool _inWall;

	private State _state;

	private int _timer;

	private int _velocityX;

	private bool _facingRight;

	private int _frame;

	/// <summary>The next walk frame. A Grounder that comes out of a wall starts at frame 4, because the original keeps the animation index.</summary>
	private int _walkFrame = 2;

	private int _walkTicks;

	private sealed class Debris
	{
		public bool Rock;

		public int Frame;

		/// <summary>The position in the level.</summary>
		public Vector2 Position;

		public double VelocityX;

		public double VelocityY;

		public bool Moving;

		public int Ticks;
	}

	private enum State
	{
		Hidden,
		ComeOut,
		Aim,
		Walk,
		Pause
	}

	/// <summary>The Grounder waits in a wall (Obj8D) instead of walking from the start (Obj8E).</summary>
	[StateVariable]
	private bool InWall
	{
		get => _inWall;
		set => _inWall = value;
	}

	public GrounderInstance() => DesignBounds = new Rectanglei(-96, -112, 192, 192);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(GrounderType.AnimationGroupResourceKey));
		_brickAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(GrounderType.BrickAnimationGroupResourceKey));
		_rockAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(GrounderType.RockAnimationGroupResourceKey));
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -TouchWidth, -TouchHeight, TouchWidth * 2, TouchHeight * 2)
		};
		// Move up out of the floor, never down
		int? floor = FloorDistance();
		if (floor < 0)
		{
			PositionPrecise += new Vector2(0.0, floor.Value * Scale);
		}
		if (_inWall)
		{
			for (int i = 0; i < Bricks.GetLength(0); i++)
			{
				_debris.Add(new Debris { Position = PositionPrecise + new Vector2(Bricks[i, 0] * Scale, Bricks[i, 1] * Scale) });
			}
			_state = State.Hidden;
		}
		else
		{
			_state = State.Aim;
		}
	}

	private int? FloorDistance() => LandscapeFloor.GetDistance(Level, Position + new Vector2i(0, Radius * Scale));

	protected override void OnUpdate()
	{
		UpdateDebris();
		switch (_state)
		{
			case State.Hidden:
				if (Math.Abs(PlayerOrientation.Get(Level, Position).DeltaX) <= TriggerDistance)
				{
					BreakWall();
				}
				break;
			case State.ComeOut:
				// Frame 0 for 8 frames, frame 1 for 8 frames, then aim
				_timer++;
				_frame = _timer <= 8 ? 0 : 1;
				if (_timer > 16)
				{
					_state = State.Aim;
					_walkFrame = 4;
				}
				break;
			case State.Aim:
			{
				PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
				_velocityX = orientation.PlayerRight ? 0x100 : -0x100;
				_facingRight = orientation.PlayerRight;
				_state = State.Walk;
				_walkTicks = 0;
				break;
			}
			case State.Walk:
				Walk();
				break;
			case State.Pause:
				if (--_timer < 0)
				{
					_velocityX = -_velocityX;
					_facingRight = !_facingRight;
					_state = State.Walk;
				}
				break;
		}
	}

	private void BreakWall()
	{
		_state = State.ComeOut;
		_timer = 0;
		for (int i = 0; i < _debris.Count; i++)
		{
			_debris[i].Moving = true;
			_debris[i].VelocityX = Bricks[i, 2] * Unit;
			_debris[i].VelocityY = Bricks[i, 3] * Unit;
		}
		for (int i = 0; i < Rocks.GetLength(0); i++)
		{
			_debris.Add(new Debris { Rock = true, Moving = true, Frame = Rocks[i, 2], Position = PositionPrecise, VelocityX = Rocks[i, 0] * Unit, VelocityY = Rocks[i, 1] * Unit });
		}
	}

	private void Walk()
	{
		PositionPrecise += new Vector2(_velocityX * Unit, 0.0);
		int? floor = FloorDistance();
		if (floor == null || floor < -1 || floor >= 0xC)
		{
			_state = State.Pause;
			_timer = PauseTicks;
			return;
		}
		PositionPrecise += new Vector2(0.0, floor.Value * Scale);
		// The tracks turn: frames 2, 3 and 4, 4 frames each
		if (_walkTicks++ % 4 == 0)
		{
			_frame = _walkFrame;
			_walkFrame = _walkFrame == 4 ? 2 : _walkFrame + 1;
		}
	}

	/// <summary>The bricks and the rocks fall with gravity after the wall breaks, and go after 3 seconds.</summary>
	private void UpdateDebris()
	{
		for (int i = _debris.Count - 1; i >= 0; i--)
		{
			Debris debris = _debris[i];
			if (!debris.Moving)
			{
				continue;
			}
			debris.Position += new Vector2(debris.VelocityX, debris.VelocityY);
			debris.VelocityY += Gravity * Unit;
			if (++debris.Ticks > DebrisTicks)
			{
				_debris.RemoveAt(i);
			}
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The art faces left
		_animation.Index = _frame;
		objectRenderer.Render(_animation, _facingRight);
		// The bricks draw in front of the Grounder
		foreach (Debris debris in _debris)
		{
			AnimationInstance animation = debris.Rock ? _rockAnimation : _brickAnimation;
			animation.Index = debris.Frame;
			objectRenderer.Render(animation, debris.Position - PositionPrecise);
		}
	}
}
