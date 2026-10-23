using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CPZDOOR;

/// <summary>
/// A one-way door in Chemical Plant Zone. The barrier moves up when a character comes near from the open side.
/// It stays shut for a character on the other side.
/// </summary>
public class CPZDoorInstance : ActiveObject
{
	private const int BarrierMoveSpeed = 32;
	private const int BarrierHeight = 256;

	/// <summary>The horizontal distance on the open side in which a character opens the door.</summary>
	private const int DetectionRangeX = 2048;

	/// <summary>The vertical distance above and below the door in which a character opens the door.</summary>
	private const int DetectionRangeY = 1080;

	private AnimationInstance _animation;
	private Vector2i _initialPosition;

	/// <summary>The distance the barrier moved up from its shut position. It is 0 or negative.</summary>
	private int _barrierOffset;

	private bool _closed;

	/// <summary>1 if the door opens for a character on the left. -1 if it opens for a character on the right.</summary>
	private int _direction = 1;

	[StateVariable]
	private int Direction
	{
		get => _direction;
		set => _direction = value;
	}

	public CPZDoorInstance()
	{
		DesignBounds = new Rectanglei(-32, -128, 64, BarrierHeight);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_initialPosition = Position;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-32, -128, 64, BarrierHeight));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
		Priority = -128;
	}

	protected override void OnUpdate()
	{
		_closed = _barrierOffset == 0;
		_barrierOffset = Level.ObjectManager.Characters.Any(x => CheckCharacter(x))
			? Math.Max(_barrierOffset - BarrierMoveSpeed, -BarrierHeight)
			: Math.Min(_barrierOffset + BarrierMoveSpeed, 0);
		Position = _initialPosition + new Vector2i(0, _barrierOffset);
	}

	/// <summary>Returns true when the character is in the area on the open side of the door.</summary>
	private bool CheckCharacter(ICharacter character)
	{
		if (!character.ShouldReactToLevel)
		{
			return false;
		}
		// When the door is open, the area also covers the door, so that a character in the doorway keeps it open.
		int doorwayWidth = _closed ? 0 : character.CollisionRadius.X + 32 + 4;
		Rectanglei area = _direction != -1
			? Rectanglei.FromLTRB(_initialPosition.X - DetectionRangeX, _initialPosition.Y - DetectionRangeY, _initialPosition.X + doorwayWidth, _initialPosition.Y + DetectionRangeY)
			: Rectanglei.FromLTRB(_initialPosition.X - doorwayWidth, _initialPosition.Y - DetectionRangeY, _initialPosition.X + DetectionRangeX, _initialPosition.Y + DetectionRangeY);
		return area.Contains(character.Position);
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
