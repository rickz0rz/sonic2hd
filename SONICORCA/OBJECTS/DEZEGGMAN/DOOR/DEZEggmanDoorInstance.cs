using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.DEZEGGMAN.DOOR;

/// <summary>
/// The barrier in front of Eggman in Death Egg Zone, as in Sonic 2 (ObjC6, subtype $A8). It is a solid block of
/// construction stripes, 16 by 64 pixels. When Eggman runs, it goes up into the ceiling one block every 2 frames
/// (Ani_objC6), and then it goes.
/// </summary>
public class DEZEggmanDoorInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>SolidObject: 8 pixels to each side ($13 less the $B of the character) and $20 pixels up and down.</summary>
	private const int HalfWidth = 8 * Scale;

	private const int HalfHeight = 0x20 * Scale;

	private readonly AnimateSpriteScript _animator = new AnimateSpriteScript(new byte[] { 1, 0, 1, 2, 3, 0xFA });

	private AnimationInstance _animation;

	private bool _opening;

	/// <summary>The Eggman that added the barrier.</summary>
	[HideInEditor]
	public DEZEggmanInstance Eggman { get; set; }

	public DEZEggmanDoorInstance() => DesignBounds = new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(DEZEggmanDoorType.AnimationGroupResourceKey));
		LockLifetime = true;
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2));
		foreach (CollisionVector collisionVector in CollisionVectors)
		{
			collisionVector.Flags |= CollisionFlags.Solid;
		}
	}

	protected override void OnStop() => FinishForever();

	protected override void OnUpdate()
	{
		if (!_opening)
		{
			_opening = Eggman != null && Eggman.Running;
			return;
		}
		if (_animator.Step() == AnimateSpriteScript.NextSecondaryRoutine)
		{
			FinishForever();
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = _animator.Frame;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
