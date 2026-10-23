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

public class HTZDoorInstance : ActiveObject
{
	private const int BarrierMoveSpeed = 32 /*0x20*/;
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
		DesignBounds = new Rectanglei(-32, sbyte.MinValue, 64 /*0x40*/, 320);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_initialPosition = Position;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-32, sbyte.MinValue, 64 /*0x40*/, 320));
		foreach (CollisionVector collisionVector in CollisionVectors)
			collisionVector.Flags |= CollisionFlags.Solid;
	}

	protected override void OnUpdate()
	{
		_closed = _barrierOffset == 0;
		_barrierOffset = Level.ObjectManager.Characters.Any<ICharacter>(x => CheckCharacter(x)) ? Math.Max(_barrierOffset - 32 /*0x20*/, -320) : Math.Min(_barrierOffset + 32 /*0x20*/, 0);
		Position = _initialPosition + new Vector2i(0, _barrierOffset);
	}

	private bool CheckCharacter(ICharacter character)
	{
		if (!character.ShouldReactToLevel)
			return false;
		int num1 = _closed ? 0 : character.CollisionRadius.X + 4;
		int num2 = 1080;
		return (_direction != DoorDirection.Left ? Rectanglei.FromLTRB(_initialPosition.X - 2048 /*0x0800*/, _initialPosition.Y - num2, _initialPosition.X + num1, _initialPosition.Y + num2) : Rectanglei.FromLTRB(_initialPosition.X - num1, _initialPosition.Y - num2, _initialPosition.X + 2048 /*0x0800*/, _initialPosition.Y + num2)).Contains(character.Position);
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
