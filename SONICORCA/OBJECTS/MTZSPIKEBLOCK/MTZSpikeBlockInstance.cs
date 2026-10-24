using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZSPIKEBLOCK;

/// <summary>
/// A block with a spike that comes out of each side in turn, as in Sonic 2 (Obj68 in Metropolis Zone). The block is
/// solid. The spike waits for the next multiple of $40 frames of the level time, comes out $20 pixels at 8 pixels
/// each frame, waits again, and goes back in. Then it turns to the next side: up, right, down and left. The level
/// time and the subtype give the first side. The spike hurts a character that touches it.
/// </summary>
public class MTZSpikeBlockInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The solid box: $1B pixels to each side, $10 up and down.</summary>
	private const int HalfWidth = 0x1B;

	private const int HalfHeight = 0x10;

	/// <summary>The spike moves $800 (8 pixels) each frame, up to $2000.</summary>
	private const int Step = 0x800;

	private const int FullOffset = 0x2000;

	private const int BlockFrame = 4;

	private const int SpikeId = 1;

	/// <summary>The touch boxes of the spike (Touch_Sizes 4 and $26): half width and half height, by side.</summary>
	private static readonly (int HalfWidth, int HalfHeight)[] TouchSizes = { (4, 0x10), (0x10, 4), (4, 0x10), (0x10, 4) };

	/// <summary>The directions of the sides: up, right, down and left.</summary>
	private static readonly Vector2i[] Directions = { new Vector2i(0, -1), new Vector2i(1, 0), new Vector2i(0, 1), new Vector2i(-1, 0) };

	private AnimationInstance _animation;

	private AnimationInstance _spikeAnimation;

	private int _subtype;

	private int _side;

	/// <summary>The distance of the spike from the centre, in 1/256 pixels.</summary>
	private int _offset;

	private bool _out;

	private bool _waiting;

	/// <summary>The subtype of the original: it turns the first side.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	public MTZSpikeBlockInstance() => DesignBounds = new Rectanglei(-0x40 * Scale, -0x40 * Scale, 0x80 * Scale, 0x80 * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZSpikeBlockType.AnimationGroupResourceKey));
		_spikeAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZSpikeBlockType.SpikeAnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		int ticks = Level.Ticks;
		_out = ((ticks >> 6) & 1) != 0;
		_side = ((ticks >> 7) + _subtype) & 3;
		UpdateTouchBox();
	}

	protected override void UpdateMovement()
	{
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		MoveSpike();
		UpdateTouchBox();
	}

	/// <summary>Moves the spike out or in (Obj68_Spike_Action).</summary>
	private void MoveSpike()
	{
		if (_waiting)
		{
			if ((Level.Ticks & 0x3F) != 0)
			{
				return;
			}
			_waiting = false;
			if (Level.Camera.Bounds.Contains((Vector2)Position))
			{
				Level.SoundManager.PlaySound(this, Type.GetAbsolutePath(MTZSpikeBlockType.SpikeSoundResourceKey));
			}
		}
		if (_out)
		{
			_offset -= Step;
			if (_offset < 0)
			{
				_offset = 0;
				_out = false;
				_waiting = true;
				_side = (_side + 1) & 3;
			}
			return;
		}
		_offset += Step;
		if (_offset >= FullOffset)
		{
			_offset = FullOffset;
			_out = true;
			_waiting = true;
		}
	}

	/// <summary>The position of the spike from the centre, in HD pixels.</summary>
	private Vector2i SpikeOffset => Directions[_side] * ((_offset >> 8) * Scale);

	private void UpdateTouchBox()
	{
		(int halfWidth, int halfHeight) = TouchSizes[_side];
		Vector2i offset = SpikeOffset;
		CollisionRectangles = new[]
		{
			new CollisionRectangle(this, SpikeId, offset.X - halfWidth * Scale, offset.Y - halfHeight * Scale, halfWidth * 2 * Scale, halfHeight * 2 * Scale)
		};
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != SpikeId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Hurt(Math.Sign(character.Position.X - (Position.X + SpikeOffset.X)));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		// The block is in front of the spike
		_spikeAnimation.Index = _side;
		objectRenderer.RenderOriginal(_spikeAnimation, (Vector2)SpikeOffset);
		_animation.Index = BlockFrame;
		objectRenderer.RenderOriginal(_animation);
	}
}
