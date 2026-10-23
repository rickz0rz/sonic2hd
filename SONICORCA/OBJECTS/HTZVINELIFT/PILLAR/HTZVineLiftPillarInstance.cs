// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZVINELIFT.PILLAR.HTZVineLiftPillarInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.HTZVINELIFT.PILLAR;

/// <summary>The pillar at the top or the bottom end of a vine lift path in Hill Top Zone. It is scenery only.</summary>
public class HTZVineLiftPillarInstance : ActiveObject
{
	private const int TopAnimationIndex = 2;
	private const int BottomAnimationIndex = 3;
	private AnimationInstance _animation;
	private bool _flipX;
	private bool _bottom;

	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	[StateVariable]
	private bool Bottom
	{
		get => _bottom;
		set => _bottom = value;
	}

	public HTZVineLiftPillarInstance()
	{
		DesignBounds = new Rectanglei(-64, -170, 128, 340);
	}

	protected override void OnStart()
	{
		base.OnStart();
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("//ANIGROUP"), _bottom ? BottomAnimationIndex : TopAnimationIndex);
		Priority = -256;
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation, _flipX);
	}
}
