using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZTHRUST;

/// <summary>
/// The fire from an engine of Eggman's getaway ship at the end of Wing Fortress Zone, as in Sonic 2 (ObjBC). It moves
/// with the ship in the background: its x is its start x plus the background offset (<see cref="WFZShipOffset"/>). It
/// shows only on every second frame, so it flickers. It goes for ever when the offset reaches $380.
/// </summary>
public class WFZThrustInstance : ActiveObject
{
	private const int Scale = 4;

	private const int EndOffset = 0x380;

	private AnimationInstance _animation;

	private double _startX;

	private bool _visible;

	public WFZThrustInstance() => DesignBounds = new Rectanglei(-0x10 * Scale, -0x10 * Scale, 0x20 * Scale, 0x20 * Scale);

	protected override void OnStart()
	{
		_animation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath(WFZThrustType.AnimationGroupResourceKey));
		_startX = PositionPrecise.X;
	}

	protected override void OnUpdate()
	{
		int offset = WFZShipOffset.Get(Level);
		if (offset >= EndOffset)
		{
			FinishForever();
			return;
		}
		PositionPrecise = new Vector2(_startX + offset * Scale, PositionPrecise.Y);
		_visible = !_visible;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!_visible)
		{
			return;
		}
		_animation.Index = 0;
		renderer.GetObjectRenderer().RenderOriginal(_animation);
	}
}
