using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.CLUCKER.BASE;

/// <summary>
/// The base of a Clucker, as in Sonic 2 (ObjAD). It is a solid block of 32 by 16 pixels (SolidObject with $1B, 8
/// and 8) that shows mapping frame $C of the Clucker. It stays when the Clucker is destroyed.
/// </summary>
public class CluckerBaseInstance : ActiveObject
{
	private const int Scale = 4;

	private const int HalfWidth = (0x1B - 0xB) * Scale;

	private const int HalfHeight = 8 * Scale;

	private const int BaseFrame = 0xC;

	private AnimationInstance _animation;

	private bool _flipX;

	/// <summary>The base is mirrored, like its Clucker.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public CluckerBaseInstance() => DesignBounds = new Rectanglei(-0x18 * Scale, -8 * Scale, 0x30 * Scale, 0x10 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(CluckerBaseType.AnimationGroupResourceKey));
		CollisionVectors = CollisionVector.FromRectangle(this, new Rectanglei(-HalfWidth, -HalfHeight, HalfWidth * 2, HalfHeight * 2));
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		_animation.Index = BaseFrame;
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
