using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.OOZSCENERY;

/// <summary>
/// Falling oil in Oil Ocean Zone, as in Sonic 2 (Obj1C, subtypes $A to $14). The object only shows a frame of the
/// cascading oil art. Subtypes $A to $F are the narrow streams (frames 0 to 5), subtypes $10 to $14 the wide streams
/// (frames 0 to 4).
/// </summary>
public class OOZSceneryInstance : ActiveObject
{
	private const int Scale = 4;

	private const int FirstNarrowSubtype = 0x0A;

	private const int FirstWideSubtype = 0x10;

	private AnimationInstance _animation;

	private int _subtype = FirstNarrowSubtype;

	private bool _flipX;

	/// <summary>The subtype of the original, $A to $14.</summary>
	[StateVariable]
	private int Subtype
	{
		get => _subtype;
		set => _subtype = value;
	}

	/// <summary>The placement is flipped.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	public OOZSceneryInstance() => DesignBounds = new Rectanglei(-0x18 * Scale, -0x70 * Scale, 0x30 * Scale, 0xE0 * Scale);

	protected override void OnStart()
	{
		bool wide = _subtype >= FirstWideSubtype;
		string key = wide ? OOZSceneryType.WideAnimationGroupResourceKey : OOZSceneryType.AnimationGroupResourceKey;
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(key));
		_animation.Index = wide ? _subtype - FirstWideSubtype : System.Math.Max(0, _subtype - FirstNarrowSubtype);
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().RenderOriginal(_animation, _flipX);
	}
}
