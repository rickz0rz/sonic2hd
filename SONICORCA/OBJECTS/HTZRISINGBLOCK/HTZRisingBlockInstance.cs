using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.HTZRISINGBLOCK;

/// <summary>
/// A large solid block of ground or ceiling from Hill Top Zone. A rising group moves it up and down during the
/// earthquake.
/// </summary>
public class HTZRisingBlockInstance : ActiveObject
{
	/// <summary>The FloorB block is not drawn when the first character is below this Y position.</summary>
	private const int FloorBHideY = 3400;

	private AnimationInstance _animation;
	private Vector2i _size = new Vector2i(2048, 512);
	private BlockType _blockKind;

	public BlockType BlockKind
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
		if (BlockKind == BlockType.FloorB)
		{
			ICharacter character = Level.ObjectManager.Characters.FirstOrDefault();
			if (character != null && character.Position.Y > FloorBHideY)
				return;
		}
		renderer.GetObjectRenderer().Render(_animation);
	}

	private CollisionVector[] CalculateCollisionVectors()
	{
		CollisionVector[] collisionVectors;
		if (BlockKind == BlockType.FloorB)
		{
			// FloorB is a polygon with a sloped top edge from topLeft to slopeEnd.
			Vector2i halfSize = new Vector2i(_size.X / 2, _size.Y / 2);
			Vector2i topLeft = new Vector2i(0, 58) - halfSize;
			Vector2i slopeEnd = new Vector2i(3072, 838) - halfSize;
			Vector2i middleLeft = new Vector2i(0, Size.Y / 2) - halfSize;
			Vector2i middleRight = new Vector2i(Size.X, Size.Y / 2) - halfSize;
			collisionVectors = new CollisionVector[4]
			{
				new CollisionVector(this, middleLeft, topLeft, flags: CollisionFlags.Solid),
				new CollisionVector(this, topLeft, slopeEnd, flags: CollisionFlags.Solid),
				new CollisionVector(this, slopeEnd, middleRight, flags: CollisionFlags.Solid),
				new CollisionVector(this, middleRight, middleLeft, flags: CollisionFlags.Solid)
			};
		}
		else
		{
			// The HD release gave these blocks no Solid flag, because FromRectangle did not pass the flags on. Original
			// physics makes them solid, as the sloped blocks are. The state of a placement has no level yet: the active
			// object calculates the vectors again when it gets the state.
			collisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-(_size.X / 2), -(_size.Y / 2), _size.X, _size.Y),
				flags: Level != null && Level.OriginalPhysics ? CollisionFlags.Solid : 0);
		}
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
