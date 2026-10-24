using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MTZDOOR;

/// <summary>
/// The one-way barrier of Metropolis Zone, as in Sonic 2 (Obj2D). The solid barrier goes up 8 pixels each frame, up to
/// $40 pixels, while a character is in the area in front of it: from $200 pixels to the left of the barrier to the
/// barrier, and less than $20 pixels above or below its start position. While the barrier is up, the area goes on
/// $18 pixels past the barrier. When no character is in the area, the barrier comes down 8 pixels each frame. A
/// flipped barrier opens from the right.
/// </summary>
public class MTZDoorInstance : Platform
{
	private const int Scale = 4;

	/// <summary>The solid box: width_pixels ($C) + $B to each side, $20 up and down.</summary>
	private const int HalfWidth = 0xC + 0xB;

	private const int HalfHeight = 0x20;

	private const int AreaWidth = 0x200;

	private const int AreaOverlap = 0x18;

	private const int AreaHalfHeight = 0x20;

	private const int OpenOffset = 0x40;

	private const int Speed = 8;

	/// <summary>The frame of the Metropolis barrier (the subtype in the original).</summary>
	private const int BarrierFrame = 1;

	private AnimationInstance _animation;

	private bool _flipX;

	private Vector2 _start;

	private int _offset;

	private bool _opening;

	/// <summary>The barrier opens from the right.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MTZDoorInstance() => DesignBounds = new Rectanglei(-HalfWidth * Scale, (-HalfHeight - OpenOffset) * Scale, HalfWidth * 2 * Scale, (HalfHeight * 2 + OpenOffset) * Scale);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MTZDoorType.AnimationGroupResourceKey));
		_animation.Index = BarrierFrame;
		_start = PositionPrecise;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth * Scale, -HalfHeight * Scale, HalfWidth * 2 * Scale, HalfHeight * 2 * Scale));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
	}

	protected override void UpdateMovement()
	{
		_opening = Level.ObjectManager.Characters.Any(IsInArea);
		_offset = _opening ? Math.Min(_offset + Speed, OpenOffset) : Math.Max(_offset - Speed, 0);
		PositionPrecise = _start - new Vector2(0.0, _offset * Scale);
	}

	/// <summary>True when the character is in the area in front of the barrier.</summary>
	private bool IsInArea(ICharacter character)
	{
		if (character.IsDebug || character.IsDying || character.IsDead || character.IsObjectControlled)
		{
			return false;
		}
		int x = (int)Math.Floor(_start.X / Scale);
		int left = _flipX ? (_opening ? x - AreaOverlap : x) : x - AreaWidth;
		int right = _flipX ? x + AreaWidth : (_opening ? x + AreaOverlap : x);
		int characterX = (int)Math.Floor(character.PositionPrecise.X / Scale);
		int dy = (int)Math.Floor((character.PositionPrecise.Y - _start.Y) / Scale);
		return characterX >= left && characterX < right && dy >= -AreaHalfHeight && dy < AreaHalfHeight;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
