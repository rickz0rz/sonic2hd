using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.BUBBLEGENERATOR;

/// <summary>
/// A vent on the floor under water that releases air bubbles.
/// It releases a large bubble at a fixed interval and small or medium bubbles at random intervals.
/// </summary>
public class BubbleGeneratorInstance : ActiveObject
{
	private const int LargeBubbleInterval = 960;
	private const int LargeBubbleSize = 2;

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
			_nextLargeBubbleTime = LargeBubbleInterval;
			Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), Position, LargeBubbleSize);
		}
		if (_nextSmallBubbleTime-- > 0)
		{
			return;
		}
		_nextSmallBubbleTime = Level.Random.Next(64, 128);
		// The size is 0 (small) or 1 (medium).
		Level.WaterManager.CreateBubble(Level.Map.Layers.IndexOf(Layer), Position, Level.Random.Next(0, 2));
	}

	protected override void OnAnimate() => _animation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetObjectRenderer().Render(_animation);
	}
}
