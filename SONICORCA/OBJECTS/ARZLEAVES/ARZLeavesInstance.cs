using System;
using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.ARZLEAVES;

/// <summary>
/// An invisible area of leaves, as in Sonic 2 (Obj2C). When a player that moves at $200 or faster passes through it,
/// 4 leaves fly off the player, at most each 16 frames. A leaf drifts with gravity, swings from side to side and
/// turns over each 12 frames.
/// </summary>
public class ARZLeavesInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The half sizes of the three areas, in original pixels (Touch_Sizes $16, $14, $15).</summary>
	private static readonly int[] HalfWidths = { 0x20, 0x40, 0x80 };

	private const int HalfHeight = 0x20;

	private static readonly int[,] LeafSpeeds = { { -0x80, -0x80 }, { 0xC0, -0x40 }, { -0xC0, 0x40 }, { 0x80, 0x80 } };

	private const int MinimumSpeed = 0x200;

	private readonly List<Leaf> _leaves = new List<Leaf>();

	private AnimationInstance _animation;

	private int _size;

	private bool _mainTouched;

	private bool _sidekickTouched;

	private int _lastTime;

	private sealed class Leaf
	{
		/// <summary>The position and the velocity in 1/65536 and 1/256 original pixels.</summary>
		public long X;

		public long Y;

		public int VelocityX;

		public int VelocityY;

		public int Angle;

		public int AngleSpeed = 4;

		public int Frame;

		public int FrameTimer;

		public bool Even;
	}

	/// <summary>The size of the area: 0, 1 or 2 (the subtype).</summary>
	[StateVariable]
	private int Size
	{
		get => _size;
		set => _size = value;
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(ARZLeavesType.AnimationGroupResourceKey));
		int halfWidth = HalfWidths[Math.Clamp(_size, 0, 2)] * Scale;
		DesignBounds = new Rectanglei(-halfWidth, -HalfHeight * Scale, halfWidth * 2, HalfHeight * 2 * Scale);
		CollisionRectangles = new CollisionRectangle[]
		{
			new CollisionRectangle(this, 0, -halfWidth, -HalfHeight * Scale, halfWidth * 2, HalfHeight * 2 * Scale)
		};
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.ActiveObject == Level.Player.Protagonist)
		{
			_mainTouched = true;
		}
		else if (e.ActiveObject == Level.Player.Sidekick)
		{
			_sidekickTouched = true;
		}
	}

	protected override void OnUpdate()
	{
		UpdateTouches();
		UpdateLeaves();
	}

	/// <summary>The timing of the original: the main player first, the sidekick 8 frames out of step.</summary>
	private void UpdateTouches()
	{
		bool main = _mainTouched;
		bool sidekick = _sidekickTouched;
		_mainTouched = false;
		_sidekickTouched = false;
		if (!main && !sidekick)
		{
			_lastTime = 0;
			return;
		}
		int step = _lastTime != 0 ? (_lastTime + (Level.Ticks & 0xFF)) & 0xF : 0;
		if (_lastTime == 0 || step == 0)
		{
			if (main)
			{
				CreateLeaves(Level.Player.Protagonist);
			}
			if (_lastTime == 0)
			{
				_lastTime = Level.Ticks;
			}
			return;
		}
		if (((step + 8) & 0xF) == 0 && sidekick)
		{
			CreateLeaves(Level.Player.Sidekick);
			if (_lastTime == 0)
			{
				_lastTime = Level.Ticks;
			}
		}
	}

	private void CreateLeaves(ICharacter player)
	{
		int velocityX = (int)(player.Velocity.X * 256.0 / Scale);
		int velocityY = (int)(player.Velocity.Y * 256.0 / Scale);
		if (Math.Abs(velocityX) < MinimumSpeed && Math.Abs(velocityY) < MinimumSpeed)
		{
			_lastTime = 0;
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			int random = Level.Random.Next(0x10000);
			int offsetY = ((random >> 8) & 0xF) - 8;
			var leaf = new Leaf
			{
				X = (long)(player.Position.X / Scale + (random & 0xF) - 8) << 16,
				Y = (long)(player.Position.Y / Scale + offsetY) << 16,
				VelocityX = player.Facing < 0 ? -LeafSpeeds[i, 0] : LeafSpeeds[i, 0],
				VelocityY = LeafSpeeds[i, 1],
				Frame = offsetY & 1,
				Even = (_leaves.Count + i) % 2 == 0
			};
			_leaves.Add(leaf);
		}
	}

	private void UpdateLeaves()
	{
		Rectangle camera = Level.Camera.Bounds;
		for (int i = _leaves.Count - 1; i >= 0; i--)
		{
			Leaf leaf = _leaves[i];
			leaf.Angle = (leaf.Angle + leaf.AngleSpeed) & 0xFF;
			if (((leaf.AngleSpeed + (Level.Ticks & 0xFF)) & 0x1F) == 0 && leaf.Even)
			{
				leaf.AngleSpeed = -leaf.AngleSpeed;
			}
			leaf.X += (long)leaf.VelocityX << 8;
			leaf.Y += (long)leaf.VelocityY << 8;
			leaf.VelocityY += (int)(leaf.Y & 3) + 4;
			if (--leaf.FrameTimer < 0)
			{
				leaf.FrameTimer = 0xB;
				leaf.Frame ^= 2;
			}
			Vector2 position = GetPosition(leaf);
			if (!camera.Contains(position))
			{
				_leaves.RemoveAt(i);
			}
		}
	}

	private static Vector2 GetPosition(Leaf leaf)
	{
		double x = (leaf.X >> 16) + (OriginalMath.Sin(leaf.Angle) >> 6);
		double y = (leaf.Y >> 16) + (OriginalMath.Cos(leaf.Angle) >> 6);
		return new Vector2(x * Scale, y * Scale);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		foreach (Leaf leaf in _leaves)
		{
			_animation.Index = leaf.Frame;
			objectRenderer.Render(_animation, GetPosition(leaf) - PositionPrecise);
		}
	}
}
