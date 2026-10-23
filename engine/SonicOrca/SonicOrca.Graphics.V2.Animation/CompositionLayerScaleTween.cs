using System.Collections.Generic;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// Tweens the scale of a layer, in percent.
/// </summary>
public class CompositionLayerScaleTween : CompositionLayerTween
{
	public CompositionLayerScaleTween(uint startFrame, uint endFrame, KeyValuePair<double, double> startEndValuesX, KeyValuePair<double, double> startEndValuesY, KeyValuePair<double, double> startEndValuesZ)
		: base(startFrame, endFrame)
	{
		StartValues["X"] = startEndValuesX.Key;
		StartValues["Y"] = startEndValuesY.Key;
		StartValues["Z"] = startEndValuesZ.Key;
		EndValues["X"] = startEndValuesX.Value;
		EndValues["Y"] = startEndValuesY.Value;
		EndValues["Z"] = startEndValuesZ.Value;
		TweenType = Type.SCALE;
		ValueKeys.Add("X");
		ValueKeys.Add("Y");
		ValueKeys.Add("Z");
	}

	public CompositionLayerScaleTween(uint startFrame, uint endFrame, KeyValuePair<double, double> startEndValuesX, KeyValuePair<double, double> startEndValuesY)
		: base(startFrame, endFrame)
	{
		StartValues["X"] = startEndValuesX.Key;
		StartValues["Y"] = startEndValuesY.Key;
		EndValues["X"] = startEndValuesX.Value;
		EndValues["Y"] = startEndValuesY.Value;
		TweenType = Type.SCALE;
		ValueKeys.Add("X");
		ValueKeys.Add("Y");
	}
}
