// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZRISINGBLOCK.HTZRisingBlockInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.HTZRISINGBLOCK;

public class HTZRisingBlockInstance : ActiveObject
{
	private AnimationInstance _animation;
	private Vector2i _size = new Vector2i(2048 /*0x0800*/, 512 /*0x0200*/);
	private HTZRisingBlockInstance.BlockType _blockKind;

	public HTZRisingBlockInstance.BlockType BlockKind
	{
		get => _blockKind;
		set => _blockKind = value;
	}

	public Vector2i Size
	{
		get => _size;
		set
		{
			_size = value;
			DesignBounds = new Rectanglei(-(_size.X / 2), -(_size.Y / 2), _size.X, _size.Y);
			CollisionVectors = CalculateCollisionVectors();
		}
	}

	public HTZRisingBlockInstance()
	{
		DesignBounds = new Rectanglei(-(_size.X / 2), -(_size.Y / 2), _size.X, _size.Y);
	}

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_animation.Index = (int)BlockKind;
		CollisionVectors = CalculateCollisionVectors();
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (BlockKind == HTZRisingBlockInstance.BlockType.FloorB)
		{
			ICharacter character = Level.ObjectManager.Characters.FirstOrDefault<ICharacter>();
			if (character != null && character.Position.Y > 3400)
				return;
		}
		renderer.GetObjectRenderer().Render(_animation);
	}

	private CollisionVector[] CalculateCollisionVectors()
	{
		CollisionVector[] collisionVectors;
		if (BlockKind == HTZRisingBlockInstance.BlockType.FloorB)
		{
			Vector2i vector2i1 = new Vector2i(_size.X / 2, _size.Y / 2);
			Vector2i vector2i2 = new Vector2i(0, 58) - vector2i1;
			Vector2i vector2i3 = new Vector2i(3072 /*0x0C00*/, 838) - vector2i1;
			Vector2i size = Size;
			Vector2i vector2i4 = new Vector2i(0, size.Y / 2) - vector2i1;
			size = Size;
			int x = size.X;
			size = Size;
			int y = size.Y / 2;
			Vector2i vector2i5 = new Vector2i(x, y) - vector2i1;
			collisionVectors = new CollisionVector[4]
			{
		new CollisionVector( this, vector2i4, vector2i2, flags: CollisionFlags.Solid),
		new CollisionVector( this, vector2i2, vector2i3, flags: CollisionFlags.Solid),
		new CollisionVector( this, vector2i3, vector2i5, flags: CollisionFlags.Solid),
		new CollisionVector( this, vector2i5, vector2i4, flags: CollisionFlags.Solid)
			};
		}
		else
			collisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-(_size.X / 2), -(_size.Y / 2), _size.X, _size.Y), flags: CollisionFlags.Solid);
		collisionVectors[1].Flags |= CollisionFlags.Conveyor;
		collisionVectors[3].Flags |= CollisionFlags.Conveyor;
		return collisionVectors;
	}

	public enum BlockType
	{
		FloorA,
		FloorB,
		CeilingA,
	}
}
