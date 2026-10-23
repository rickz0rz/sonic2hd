// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZDOOR.HTZDoorInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.HTZDOOR;

/// <summary>
/// The one-way door from Hill Top Zone. It is a solid barrier that slides up while a character is on the approach side,
/// and slides down again when no character is there.
/// </summary>
public class HTZDoorInstance : ActiveObject
{
	private const int BarrierMoveSpeed = 32;

	/// <summary>The barrier offset when the door is fully open. It is the height of the barrier.</summary>
	private const int OpenBarrierOffset = -320;

	/// <summary>The horizontal distance from the door on the approach side in which a character opens the door.</summary>
	private const int DetectionWidth = 2048;

	/// <summary>The vertical distance above and below the door in which a character opens the door.</summary>
	private const int DetectionHalfHeight = 1080;

	private AnimationInstance _animation;
	private Vector2i _initialPosition;
	private int _barrierOffset;
	private bool _closed;
	private DoorDirection _direction = DoorDirection.Right;

	[StateVariable]
	private DoorDirection Direction
	{
		get => _direction;
		set => _direction = value;
	}

	public HTZDoorInstance()
	{
		DesignBounds = new Rectanglei(-32, -128, 64, 320);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_initialPosition = Position;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-32, -128, 64, 320));
		foreach (CollisionVector collisionVector in CollisionVectors)
			collisionVector.Flags |= CollisionFlags.Solid;
	}

	protected override void OnUpdate()
	{
		_closed = _barrierOffset == 0;
		_barrierOffset = Level.ObjectManager.Characters.Any(CheckCharacter)
			? Math.Max(_barrierOffset - BarrierMoveSpeed, OpenBarrierOffset)
			: Math.Min(_barrierOffset + BarrierMoveSpeed, 0);
		Position = _initialPosition + new Vector2i(0, _barrierOffset);
	}

	/// <summary>Returns true when the character is on the approach side of the door, near enough to open it.</summary>
	private bool CheckCharacter(ICharacter character)
	{
		if (!character.ShouldReactToLevel)
			return false;
		// While the door is open, the area also includes the door itself, so the door stays open for a character in it.
		int doorMargin = _closed ? 0 : character.CollisionRadius.X + 4;
		Rectanglei area = _direction != DoorDirection.Left
			? Rectanglei.FromLTRB(_initialPosition.X - DetectionWidth, _initialPosition.Y - DetectionHalfHeight, _initialPosition.X + doorMargin, _initialPosition.Y + DetectionHalfHeight)
			: Rectanglei.FromLTRB(_initialPosition.X - doorMargin, _initialPosition.Y - DetectionHalfHeight, _initialPosition.X + DetectionWidth, _initialPosition.Y + DetectionHalfHeight);
		return area.Contains(character.Position);
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		using (objectRenderer.BeginMatixState())
		{
			if (_direction == DoorDirection.Left)
				objectRenderer.ModelMatrix *= Matrix4.CreateScale(-1.0, 1.0);
			objectRenderer.Render(_animation);
		}
	}
}
