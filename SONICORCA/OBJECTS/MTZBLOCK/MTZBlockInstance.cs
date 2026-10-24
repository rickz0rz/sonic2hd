using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZBLOCK;

/// <summary>
/// A solid platform, as in Sonic 2 (Obj6B in Metropolis Zone). Subtype bits 4-6 select the size and the art: 0 is
/// the platform $40 pixels wide, 1 the block $20 pixels wide. Subtype bits 0-3 select the movement:
/// 0 stays. 1 and 2 move left and right with the global oscillators 2 and 7 ($40 and $80 pixels). 3 and 4 move up
/// and down in the same way. The flip of the original reverses these movements.
/// 5 bobs up and down by 8 pixels with oscillator 0. When a character stands on it, it sinks (6) faster and faster
/// until it goes below the bottom of the level, and then stays.
/// 7 descends when a character stands on it: it goes down faster and faster to the half way point, then slower,
/// and stops $E1 pixels below its start.
/// The stair movements (8 to $B) of the original are for Chemical Plant Zone: this platform stays with them.
/// </summary>
public class MTZBlockInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The sinking platform goes down $8 (1/256 pixels each frame) faster each frame.</summary>
	private const int SinkGravity = 8;

	/// <summary>The descending platform changes its speed by 8 each frame, and turns at speed $2A8.</summary>
	private const int DescendAcceleration = 8;

	private const int DescendTurnSpeed = 0x2A8;

	/// <summary>Obj6B_Properties: width_pixels, y_radius and the mapping frame, by size.</summary>
	private static readonly (int Width, int Height, int Frame)[] Properties =
	{
		(32, 12, 1),
		(16, 16, 0),
	};

	private AnimationInstance _animation;

	private int _subtype;

	private bool _flipX;

	private int _movement;

	private int _frame;

	private Vector2 _origin;

	/// <summary>The vertical speed in 1/256 pixels each frame.</summary>
	private int _velocityY;

	private int _acceleration;

	/// <summary>The subtype of the original.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The flip of the original: it reverses the movements.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MTZBlockInstance() => DesignBounds = new Rectanglei(-0xC0 * Scale, -0xC0 * Scale, 0x180 * Scale, 0x1C0 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZBlockType.AnimationGroupResourceKey));
		int size = (_subtype >> 4) & 7;
		(int width, int height, int frame) = Properties[size < Properties.Length ? size : 0];
		_frame = frame;
		_movement = _subtype & 0xF;
		_origin = PositionPrecise;
		int halfWidth = (width + 0xB) * Scale;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-halfWidth, -height * Scale, halfWidth * 2, height * 2 * Scale));
	}

	private bool IsStoodOn => Level.ObjectManager.Characters.Any(c => c.ObjectLink == this);

	protected override void UpdateMovement()
	{
		Oscillators oscillators = Oscillators.For(Level);
		switch (_movement)
		{
			case 1:
				MoveX(oscillators.GetHighByte(Level, 2), 0x40);
				break;
			case 2:
				MoveX(oscillators.GetHighByte(Level, 7), 0x80);
				break;
			case 3:
				MoveY(oscillators.GetHighByte(Level, 2), 0x40);
				break;
			case 4:
				MoveY(oscillators.GetHighByte(Level, 7), 0x80);
				break;
			case 5:
				// Bob on top of the lava (Obj6B_Sinking)
				PositionPrecise = new Vector2(PositionPrecise.X, _origin.Y + (oscillators.GetHighByte(Level, 0) >> 1) * Scale);
				if (IsStoodOn)
				{
					_movement = 6;
				}
				break;
			case 6:
				Sink();
				break;
			case 7:
				Descend();
				break;
		}
	}

	private void MoveX(int value, int range)
	{
		int offset = _flipX ? range - value : value;
		PositionPrecise = new Vector2(_origin.X - offset * Scale, PositionPrecise.Y);
	}

	private void MoveY(int value, int range)
	{
		int offset = _flipX ? range - value : value;
		PositionPrecise = new Vector2(PositionPrecise.X, _origin.Y - offset * Scale);
	}

	/// <summary>Sinks faster and faster until the platform is below the level (Obj6B_Sink).</summary>
	private void Sink()
	{
		PositionPrecise += new Vector2(0.0, _velocityY * Scale / 256.0);
		_velocityY += SinkGravity;
		if (PositionPrecise.Y > Level.Bounds.Bottom)
		{
			_movement = 0;
		}
	}

	/// <summary>Descends faster to the half way point, then slower until it stops (Obj6B_Descend).</summary>
	private void Descend()
	{
		if (_acceleration == 0)
		{
			if (!IsStoodOn)
			{
				return;
			}
			_acceleration = DescendAcceleration;
		}
		PositionPrecise += new Vector2(0.0, _velocityY * Scale / 256.0);
		if (_velocityY == DescendTurnSpeed)
		{
			_acceleration = -_acceleration;
		}
		_velocityY += _acceleration;
		if (_velocityY == 0)
		{
			_movement = 0;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
