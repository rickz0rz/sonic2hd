using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.TURTLOID;

/// <summary>
/// Turtloid and its rider, as in Sonic 2 (Obj9A and Obj9B). The turtle flies to the left at $80 with the Tornado
/// velocity added, and the player can stand on its back. It does not hurt. The rider sits on it ($18 pixels above,
/// 4 pixels to the right) and is the badnik: it hurts the player, and a rolling player destroys it. When the player
/// is less than $80 pixels to the left of the turtle, the turtle stops for 5 frames, then fires one shot to the left
/// at $100, and flies on 9 frames later. The shot hurts the player. The turtle goes when it leaves the screen on the
/// left, and a shot goes when it leaves the screen.
/// </summary>
public class TurtloidInstance : Platform
{
	private const int Scale = 4;

	/// <summary>Speeds in 1/256 original pixels for each frame, at four times the resolution.</summary>
	private const double Unit = Scale / 256.0;

	private const int FlySpeed = -0x80;

	private const int ShotSpeed = -0x100;

	private const int TriggerDistance = 0x80;

	private const int StopTicks = 4;

	private const int FireTicks = 8;

	/// <summary>The platform: $18 pixels to each side, $E pixels above the centre (PlatformObject).</summary>
	private const int HalfWidth = 0x18 * Scale;

	private const int Top = -0xE * Scale;

	private static readonly Vector2 RiderOffset = new Vector2(4 * Scale, -0x18 * Scale);

	private static readonly Vector2 ShotOffset = new Vector2(-0x14 * Scale, 0xA * Scale);

	/// <summary>The touch sizes: $C x $C for the rider, 4 x 4 for a shot.</summary>
	private const int RiderTouchRadius = 0xC * Scale;

	private const int ShotTouchRadius = 4 * Scale;

	private const int RiderId = 0;

	private const int FirstShotId = 1;

	/// <summary>The frames of obj9C: the turtle, the turtle that stops, the rider, the rider that fires, the shot, the jet.</summary>
	private const int TurtleFrame = 0;

	private const int StoppedTurtleFrame = 1;

	private const int RiderFrame = 2;

	private const int FiringRiderFrame = 3;

	private const int ShotFrame = 4;

	private const int JetFrame = 6;

	private const int FrameTicks = 2;

	private readonly List<Vector2> _shots = new List<Vector2>();

	private AnimationInstance _animation;

	private SkyChaseScroll _scroll;

	private State _state;

	private int _timer;

	private int _velocityX = FlySpeed;

	private int _turtleFrame = TurtleFrame;

	private int _riderFrame = RiderFrame;

	private bool _riderAlive = true;

	private int _ticks;

	private enum State
	{
		Wait,
		Stop,
		Fire,
		Done
	}

	public TurtloidInstance() => DesignBounds = new Rectanglei(-0x20 * Scale, -0x28 * Scale, 0x48 * Scale, 0x40 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(TurtloidType.AnimationGroupResourceKey));
		_scroll = SkyChaseScroll.For(Level);
		Priority = 128;
		CollisionVectors = new[]
		{
			new CollisionVector(this, new Vector2i(-HalfWidth, Top), new Vector2i(HalfWidth, Top), flags: CollisionFlags.Conveyor)
		};
		UpdateRectangles();
	}

	protected override void UpdateMovement()
	{
		switch (_state)
		{
			case State.Wait:
			{
				PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
				if (orientation.Player != null && (uint)orientation.DeltaX < TriggerDistance)
				{
					_state = State.Stop;
					_velocityX = 0;
					_timer = StopTicks;
					_turtleFrame = StoppedTurtleFrame;
				}
				break;
			}
			case State.Stop:
				if (--_timer < 0)
				{
					_state = State.Fire;
					_timer = FireTicks;
					_riderFrame = FiringRiderFrame;
					_shots.Add(PositionPrecise + ShotOffset);
				}
				break;
			case State.Fire:
				if (--_timer < 0)
				{
					_state = State.Done;
					_velocityX = FlySpeed;
					_turtleFrame = TurtleFrame;
				}
				break;
		}
		PositionPrecise += new Vector2(_velocityX * Unit, 0.0) + _scroll.Velocity;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		_ticks++;
		// Obj98_TurtloidShotMove: the shots fly without the Tornado velocity
		for (int i = _shots.Count - 1; i >= 0; i--)
		{
			_shots[i] += new Vector2(ShotSpeed * Unit, 0.0);
			if (!Level.Camera.Bounds.Contains(_shots[i]))
			{
				_shots.RemoveAt(i);
			}
		}
		UpdateRectangles();
		if (Position.X + DesignBounds.Right < Level.Camera.Bounds.Left && _shots.Count == 0)
		{
			Finish();
		}
	}

	private void UpdateRectangles()
	{
		var rectangles = new List<CollisionRectangle>();
		if (_riderAlive)
		{
			rectangles.Add(new CollisionRectangle(this, RiderId, (int)RiderOffset.X - RiderTouchRadius, (int)RiderOffset.Y - RiderTouchRadius,
				RiderTouchRadius * 2, RiderTouchRadius * 2));
		}
		for (int i = 0; i < _shots.Count; i++)
		{
			Vector2 offset = _shots[i] - PositionPrecise;
			rectangles.Add(new CollisionRectangle(this, FirstShotId + i, (int)offset.X - ShotTouchRadius, (int)offset.Y - ShotTouchRadius,
				ShotTouchRadius * 2, ShotTouchRadius * 2));
		}
		CollisionRectangles = rectangles.ToArray();
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		var character = (ICharacter)e.ActiveObject;
		if (e.Id == RiderId && _riderAlive && character.IsDeadly)
		{
			DestroyRider(character);
			return;
		}
		if (e.Id != RiderId || _riderAlive)
		{
			character.Hurt(Math.Sign(character.Position.X - Position.X));
		}
	}

	/// <summary>The rider is a badnik: it explodes, sets an animal free and gives points. The turtle flies on.</summary>
	private void DestroyRider(ICharacter character)
	{
		_riderAlive = false;
		if (character.IsAirborne)
		{
			Vector2 velocity = character.Velocity;
			Vector2i rider = Position + (Vector2i)RiderOffset;
			if (velocity.Y < 0.0)
			{
				velocity.Y += 4.0;
			}
			else if (character.Position.Y >= rider.Y)
			{
				velocity.Y -= 4.0;
			}
			else
			{
				velocity.Y = -velocity.Y;
			}
			character.Velocity = velocity;
		}
		Vector2i position = Position + (Vector2i)RiderOffset;
		int layer = Level.Map.Layers.IndexOf(Layer);
		int points = character.Player.AwardChainedScore();
		Level.ObjectManager.AddObject(new ObjectPlacement(Level.CommonResources.GetResourcePath("badnikexplosionobject"), layer, position));
		Level.CreateRandomAnimalObject(layer, position);
		Level.CreateScoreObject(points, position);
		UpdateRectangles();
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The jet behind the turtle: frames 6 and 7, 2 frames each. The art faces left.
		_animation.Index = JetFrame + (_ticks / FrameTicks) % 2;
		objectRenderer.RenderOriginal(_animation);
		_animation.Index = _turtleFrame;
		objectRenderer.RenderOriginal(_animation);
		if (_riderAlive)
		{
			_animation.Index = _riderFrame;
			objectRenderer.RenderOriginal(_animation, RiderOffset);
		}
		foreach (Vector2 shot in _shots)
		{
			_animation.Index = ShotFrame + (_ticks / FrameTicks) % 2;
			objectRenderer.RenderOriginal(_animation, shot - PositionPrecise);
		}
	}
}
