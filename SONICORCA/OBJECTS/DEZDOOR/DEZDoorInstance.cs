using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.DEZDOOR;

/// <summary>
/// The one-way barrier of Death Egg Zone, as in Sonic 2 (Obj2D). It is a solid block of construction stripes, 16 by
/// 64 pixels. While a character is on the open side, from $200 pixels away up to the centre of the barrier and less
/// than $20 pixels above or below it, the barrier moves up 8 pixels each frame, to a maximum of $40. Then the open
/// area includes $18 pixels on the other side, so that a character in the doorway keeps it open. When no character is
/// there, the barrier moves down 8 pixels each frame. A flipped barrier opens for characters on the right.
/// </summary>
public class DEZDoorInstance : ActiveObject
{
	private const int Scale = 4;

	private const int MoveSpeed = 8 * Scale;

	private const int OpenHeight = 0x40 * Scale;

	/// <summary>The open area goes $200 pixels to the open side, and $18 pixels to the other side while the barrier moves up.</summary>
	private const int OpenSideWidth = 0x200 * Scale;

	private const int DoorwayWidth = 0x18 * Scale;

	private const int AreaHalfHeight = 0x20 * Scale;

	/// <summary>The barrier is 8 pixels to each side of its centre (width_pixels) and $20 pixels up and down.</summary>
	private const int HalfWidth = 8 * Scale;

	private const int HalfHeight = 0x20 * Scale;

	private AnimationInstance _animation;

	private Vector2i _initialPosition;

	/// <summary>objoff_30: how far the barrier is up.</summary>
	private int _offset;

	/// <summary>routine_secondary: a character opened the barrier on the last frame.</summary>
	private bool _opening;

	/// <summary>The barrier opens for characters on the right.</summary>
	[StateVariable]
	public bool FlipX { get; set; }

	/// <summary>The mapping frame: the subtype of the original. Death Egg Zone uses frame 0.</summary>
	[StateVariable]
	public int Frame { get; set; }

	public DEZDoorInstance() => DesignBounds = new Rectanglei(-HalfWidth, -HalfHeight - OpenHeight, HalfWidth * 2, HalfHeight * 2 + OpenHeight);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DEZDoorType.AnimationGroupResourceKey));
		_animation.Index = Frame;
		_initialPosition = Position;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
	}

	protected override void OnUpdate()
	{
		// The open area of Obj2D_Main
		int left;
		int right;
		if (!FlipX)
		{
			left = _initialPosition.X - OpenSideWidth;
			right = _opening ? _initialPosition.X + DoorwayWidth : _initialPosition.X;
		}
		else
		{
			left = _opening ? _initialPosition.X - DoorwayWidth : _initialPosition.X;
			right = _initialPosition.X + OpenSideWidth;
		}
		int top = _initialPosition.Y - AreaHalfHeight;
		int bottom = _initialPosition.Y + AreaHalfHeight;
		_opening = false;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			Vector2i position = character.Position;
			// A character that an object holds does not open the barrier
			if (character.ShouldReactToLevel && !character.IsObjectControlled && position.X >= left && position.X < right &&
				position.Y >= top && position.Y < bottom)
			{
				_opening = true;
			}
		}
		_offset = _opening ? Math.Min(_offset + MoveSpeed, OpenHeight) : Math.Max(_offset - MoveSpeed, 0);
		Position = _initialPosition - new Vector2i(0, _offset);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation, FlipX);
	}
}
