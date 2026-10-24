using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.NEBULA;

/// <summary>
/// Nebula, as in Sonic 2 (Obj99). It flies to the left at $C0 with the Tornado velocity added. When the player is
/// less than $80 pixels to its left, it climbs: it starts at $A0 up and slows down by 1 each frame. When it is within
/// 8 pixels of the player on the x axis, it drops one bomb. The bomb falls with gravity $38, blinks, and hurts the
/// player. Nebula goes when it leaves the screen on the left, and the bomb goes when it leaves the screen.
/// </summary>
public class NebulaInstance : Badnik
{
	private const int Scale = 4;

	/// <summary>Speeds in 1/256 original pixels for each frame, at four times the resolution.</summary>
	private const double Unit = Scale / 256.0;

	private const int FlySpeed = -0xC0;

	private const int ClimbSpeed = -0xA0;

	private const int ClimbDistance = 0x80;

	private const int DropDistance = 8;

	private const int BombOffsetY = 0x18;

	private const int Gravity = 0x38;

	/// <summary>The touch sizes: $10 x $10 for Nebula, 8 x 8 for the bomb.</summary>
	private const int TouchRadius = 0x10 * Scale;

	private const int BombTouchRadius = 8 * Scale;

	private const int Frames = 4;

	private const int FrameTicks = 4;

	/// <summary>The animation group has frames 0 to 4 in palette line 1, then the same frames in palette line 0.</summary>
	private const int FramesPerPaletteLine = 5;

	private const int BombFrame = 4;

	private AnimationInstance _animation;

	private SkyChaseScroll _scroll;

	private bool _climbing;

	private bool _dropped;

	private int _velocityY;

	private int _ticks;

	private bool _bombActive;

	private Vector2 _bomb;

	private double _bombVelocityY;

	public NebulaInstance() => DesignBounds = new Rectanglei(-0x18 * Scale, -0x18 * Scale, 0x30 * Scale, 0x30 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(NebulaType.AnimationGroupResourceKey));
		_scroll = SkyChaseScroll.For(Level);
		UpdateRectangles();
	}

	protected override void OnUpdate()
	{
		_ticks++;
		PlayerOrientation orientation = PlayerOrientation.Get(Level, Position);
		if (!_climbing)
		{
			if (orientation.Player != null && orientation.DeltaX >= 0 && orientation.DeltaX < ClimbDistance)
			{
				_climbing = true;
				_velocityY = ClimbSpeed;
			}
		}
		else
		{
			if (!_dropped && orientation.Player != null && (uint)(orientation.DeltaX + DropDistance) < DropDistance * 2)
			{
				_dropped = true;
				_bombActive = true;
				_bomb = PositionPrecise + new Vector2(0.0, BombOffsetY * Scale);
				_bombVelocityY = 0.0;
			}
			_velocityY++;
		}
		PositionPrecise += new Vector2(FlySpeed * Unit, _velocityY * Unit) + _scroll.Velocity;
		UpdateBomb();
		UpdateRectangles();
		if (Position.X + DesignBounds.Right < Level.Camera.Bounds.Left)
		{
			Finish();
		}
	}

	/// <summary>Obj98_NebulaBombFall: the bomb falls without the Tornado velocity, and goes when it leaves the screen.</summary>
	private void UpdateBomb()
	{
		if (!_bombActive)
		{
			return;
		}
		_bomb += new Vector2(0.0, _bombVelocityY);
		_bombVelocityY += Gravity * Unit;
		if (!Level.Camera.Bounds.Contains(_bomb))
		{
			_bombActive = false;
		}
	}

	private void UpdateRectangles()
	{
		var body = new CollisionRectangle(this, 0, -TouchRadius, -TouchRadius, TouchRadius * 2, TouchRadius * 2);
		if (!_bombActive)
		{
			CollisionRectangles = new[] { body };
			return;
		}
		Vector2 offset = _bomb - PositionPrecise;
		CollisionRectangles = new[]
		{
			body,
			new CollisionRectangle(this, 1, (int)offset.X - BombTouchRadius, (int)offset.Y - BombTouchRadius, BombTouchRadius * 2, BombTouchRadius * 2)
		};
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id == 1)
		{
			// The bomb only hurts
			if (e.ActiveObject.Type.Classification == ObjectClassification.Character)
			{
				var character = (ICharacter)e.ActiveObject;
				character.Hurt(Math.Sign(character.Position.X - (int)_bomb.X));
			}
			return;
		}
		base.OnCollision(e);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = (_ticks / FrameTicks) % Frames;
		objectRenderer.RenderOriginal(_animation);
		if (_bombActive)
		{
			// The bomb blinks: it changes between palette lines 1 and 0 each frame
			_animation.Index = BombFrame + (_ticks % 2 == 0 ? FramesPerPaletteLine : 0);
			objectRenderer.RenderOriginal(_animation, _bomb - PositionPrecise);
		}
	}
}
