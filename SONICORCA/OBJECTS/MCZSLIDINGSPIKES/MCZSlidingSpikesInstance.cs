using System;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.MCZSLIDINGSPIKES;

/// <summary>
/// A spike block that slides out of a wall, as in Sonic 2 (Obj76). The block waits until a player on the ground is
/// $40 to $C0 pixels in front of it (to the left, or to the right when mirrored) and less than $10 pixels above or
/// below its centre. Then it slides $80 pixels in that direction at 1 pixel each frame. The block is solid, and a
/// player that touches its left or right side gets hurt.
/// </summary>
public class MCZSlidingSpikesInstance : Platform
{
	private const int Scale = 4;

	private const int SideCollisionId = 1;

	/// <summary>The solid box: width_pixels ($40) + $B to each side, $10 up and down.</summary>
	private const int HalfWidth = (0x40 + 0xB) * Scale;

	private const int HalfHeight = 0x10 * Scale;

	private const int SlideDistance = 0x80;

	/// <summary>The centre of the spikes from the object position, in original pixels.</summary>
	private const int SpikeOffset = -0x38;

	/// <summary>The animation index of the horizontal spikes (Obj36 frame 4).</summary>
	private const int SpikeFrame = 4;

	private AnimationInstance _animation;

	private AnimationInstance _spikeAnimation;

	private bool _flipX;

	private bool _sliding;

	private int _remaining;

	/// <summary>The block slides to the right instead of to the left.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public MCZSlidingSpikesInstance() => DesignBounds = new Rectanglei(-(0x40 + SlideDistance) * Scale, -HalfHeight, (0x80 + SlideDistance * 2) * Scale, HalfHeight * 2);

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZSlidingSpikesType.AnimationGroupResourceKey));
		_spikeAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(MCZSlidingSpikesType.SpikeAnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2));
		// The left and the right sides hurt
		foreach (int side in new[] { 0, 2 })
		{
			CollisionVectors[side].Id = SideCollisionId;
			CollisionVectors[side].Flags |= CollisionFlags.Solid;
		}
	}

	protected override void UpdateMovement()
	{
		if (!_sliding)
		{
			foreach (ICharacter character in Level.ObjectManager.Characters)
			{
				if (IsInFront(character))
				{
					_sliding = true;
					_remaining = SlideDistance;
					break;
				}
			}
			return;
		}
		if (_remaining > 0)
		{
			_remaining--;
			PositionPrecise += new Vector2((_flipX ? 1 : -1) * Scale, 0.0);
		}
	}

	private bool IsInFront(ICharacter character)
	{
		if (character.IsAirborne)
		{
			return false;
		}
		int dx = (int)Math.Floor((character.Position.X - PositionPrecise.X) / Scale) + 0xC0 - (_flipX ? 0x100 : 0);
		int dy = (int)Math.Floor((character.Position.Y - PositionPrecise.Y) / Scale) + 0x10;
		return (uint)dx < 0x80 && (uint)dy < 0x20;
	}

	protected override void OnCollision(CollisionEvent e)
	{
		if (e.Id != SideCollisionId || e.ActiveObject.Type.Classification != ObjectClassification.Character)
		{
			return;
		}
		ICharacter character = (ICharacter)e.ActiveObject;
		character.Hurt(Math.Sign(character.Position.X - Position.X), PlayerDeathCause.Spikes);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		_animation.Index = 0;
		objectRenderer.Render(_animation, new Vector2(_flipX ? -2.0 * _animation.CurrentFrame.Offset.X : 0.0, 0.0), _flipX);
		// The spikes are the left half of the horizontal spikes
		_spikeAnimation.Index = SpikeFrame;
		Rectanglei source = _spikeAnimation.CurrentFrame.Source;
		objectRenderer.Texture = _spikeAnimation.CurrentTexture;
		objectRenderer.Render(new Rectangle(source.X, source.Y, source.Width / 2, source.Height), new Vector2((_flipX ? -SpikeOffset : SpikeOffset) * Scale, 0.0), _flipX);
	}
}
