// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.BUBBLEGENERATOR.BubbleGeneratorInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.BUBBLEGENERATOR;

public class BubbleGeneratorInstance : ActiveObject
{
	private AnimationInstance _animation;
	private int _nextLargeBubbleTime;
	private int _nextSmallBubbleTime;

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
	}

	protected override void OnUpdate()
	{
		if (_nextLargeBubbleTime-- <= 0)
		{
			_nextLargeBubbleTime = 960;
			Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), Position, 2);
		}
		if (_nextSmallBubbleTime-- > 0)
			return;
		_nextSmallBubbleTime = Level.Random.Next(64 /*0x40*/, 128 /*0x80*/);
		Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), Position, Level.Random.Next(0, 2));
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
