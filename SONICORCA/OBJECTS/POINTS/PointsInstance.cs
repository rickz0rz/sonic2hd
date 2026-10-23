// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.POINTS.PointsInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca.Core;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.POINTS;

public class PointsInstance : ActiveObject
{
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
		_font = Level.GameContext.ResourceTree.GetLoadedResource<Font>(Type, "SONICORCA/FONTS/POINTS");
		_velocityY = -12.0;
		Priority = 2304 /*0x0900*/;
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
			_velocityY += 0.375;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		renderer.GetFontRenderer().RenderStringWithShadow(_value.ToString(), new Rectangle(), FontAlignment.Centre, _font, Colours.White);
	}
}
