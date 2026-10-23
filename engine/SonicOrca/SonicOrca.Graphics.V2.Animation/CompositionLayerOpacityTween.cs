using System.Collections.Generic;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// Tweens the opacity of a layer, in percent.
/// </summary>
public class CompositionLayerOpacityTween : CompositionLayerTween
{
	public CompositionLayerOpacityTween(uint startFrame, uint endFrame, KeyValuePair<double, double> startEndValues)
		: base(startFrame, endFrame)
	{
		StartValues["Opacity"] = startEndValues.Key;
		EndValues["Opacity"] = startEndValues.Value;
		TweenType = Type.OPACITY;
		ValueKeys.Add("Opacity");
	}
}
