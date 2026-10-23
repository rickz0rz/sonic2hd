using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.POINTS;

/// <summary>
/// A score number that shows the points the player got. It moves up, slows down, and goes away when it stops.
/// </summary>
public class PointsInstance : ActiveObject
{
	private const double InitialVelocityY = -12.0;
	private const double Deceleration = 0.375;

	private Font _font;
	private int _value;
	private double _velocityY;

	[StateVariable]
	public int Value
	{
		get => _value;
		set => _value = value;
	}

	protected override void OnStart()
	{
		_font = Level.GameContext.ResourceTree.GetLoadedResource<Font>(Type, PointsType.FontResourceKey);
		_velocityY = InitialVelocityY;
		Priority = 2304;
	}

	protected override void OnUpdate()
	{
		if (_velocityY >= 0.0)
		{
			FinishForever();
		}
		else
		{
			PositionPrecise = PositionPrecise + new Vector2(0.0, _velocityY);
			_velocityY += Deceleration;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetFontRenderer().RenderStringWithShadow(_value.ToString(), new Rectangle(), FontAlignment.Centre, _font, Colours.White);
	}
}
