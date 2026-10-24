using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZTURRET;

/// <summary>
/// A wall turret, as in Sonic 2 (ObjB8). It waits until it is on screen and the nearer player is below it and within
/// $60 pixels on the x axis. From then on, it aims at the nearer player: down when the player is within $20 pixels on
/// the x axis, else down to the left or down to the right. It fires a shot 2 frames after it wakes up, and then every
/// $60 frames. A shot (Obj98 with Obj98_WallTurretShotMove) flies at $100 on each axis that the aim gives, hurts a
/// character that it touches (touch size $18: 4 by 4 pixels) and goes when it leaves the screen. Nothing can destroy
/// the turret.
/// </summary>
public class WFZTurretInstance : ActiveObject
{
	private const int Scale = 4;

	private const double Unit = Scale / 256.0;

	private const int WakeDistance = 0x60;

	private const int AimDistance = 0x20;

	private const int FirstShotDelay = 2;

	private const int FireDelay = 0x60;

	private const int ShotRadius = 4 * Scale;

	private const int ShotFrame = 3;

	/// <summary>Ani_WallTurretShot: frames 3 and 4, 3 frames each.</summary>
	private const int ShotFrameTicks = 3;

	private const int HalfWidth = 0x10 * Scale;

	/// <summary>For each aim (mapping frame 0 to 2): the offset of the new shot and its speed (byte_3BA2A).</summary>
	private static readonly int[,] Shots = { { 0, 0x18, 0, 0x100 }, { -0x11, 0x10, -0x100, 0x100 }, { 0x11, 0x10, 0x100, 0x100 } };

	private readonly List<Shot> _shots = new List<Shot>();

	private AnimationInstance _animation;

	private bool _awake;

	private int _frame;

	private int _timer;

	private sealed class Shot
	{
		public Vector2 Position;

		public Vector2 Velocity;

		public int Ticks;
	}

	public WFZTurretInstance() => DesignBounds = new Rectanglei(-0x18 * Scale, -0x10 * Scale, 0x30 * Scale, 0x28 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZTurretType.AnimationGroupResourceKey));
	}

	protected override void OnUpdate()
	{
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		if (!_awake)
		{
			if (IsOnScreen(Position, HalfWidth) && orientation.PlayerBelow && orientation.DeltaX >= -WakeDistance && orientation.DeltaX < WakeDistance)
			{
				_awake = true;
				_timer = FirstShotDelay;
			}
		}
		else
		{
			bool straightDown = orientation.DeltaX >= -AimDistance && orientation.DeltaX < AimDistance;
			_frame = straightDown ? 0 : orientation.PlayerRight ? 2 : 1;
			if (--_timer == 0)
			{
				_timer = FireDelay;
				Fire();
			}
		}
		UpdateShots();
	}

	private void Fire()
	{
		_shots.Add(new Shot
		{
			Position = PositionPrecise + new Vector2(Shots[_frame, 0] * Scale, Shots[_frame, 1] * Scale),
			Velocity = new Vector2(Shots[_frame, 2] * Unit, Shots[_frame, 3] * Unit)
		});
	}

	private void UpdateShots()
	{
		for (int i = _shots.Count - 1; i >= 0; i--)
		{
			Shot shot = _shots[i];
			if (!IsOnScreen((Vector2i)shot.Position, ShotRadius))
			{
				_shots.RemoveAt(i);
				continue;
			}
			shot.Position += shot.Velocity;
			shot.Ticks++;
		}
		var rectangles = new CollisionRectangle[_shots.Count];
		for (int i = 0; i < _shots.Count; i++)
		{
			Vector2 offset = _shots[i].Position - PositionPrecise;
			rectangles[i] = new CollisionRectangle(this, i + 1, (int)offset.X - ShotRadius, (int)offset.Y - ShotRadius, ShotRadius * 2, ShotRadius * 2);
		}
		CollisionRectangles = rectangles;
	}

	private bool IsOnScreen(Vector2i position, int halfWidth)
	{
		return Level.Camera.Bounds.IntersectsWith(new Rectangle(position.X - halfWidth, position.Y - halfWidth, halfWidth * 2, halfWidth * 2));
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id > 0 && e.ActiveObject.Type.Classification == ObjectClassification.Character)
		{
			var character = (ICharacter)e.ActiveObject;
			int index = e.Id - 1;
			double shotX = index < _shots.Count ? _shots[index].Position.X : Position.X;
			character.Hurt(Math.Sign(character.Position.X - shotX));
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = _frame;
		objectRenderer.RenderOriginal(_animation);
		foreach (Shot shot in _shots)
		{
			_animation.Index = ShotFrame + (shot.Ticks / ShotFrameTicks) % 2;
			objectRenderer.RenderOriginal(_animation, shot.Position - PositionPrecise);
		}
	}
}
