using System.Collections.Generic;

namespace SonicOrca.Graphics.V2.Animation;

public class CompositionLayerRotationTween : CompositionLayerTween
{
	public CompositionLayerRotationTween(uint startFrame, uint endFrame, KeyValuePair<double, double> startEndValues)
		: base(startFrame, endFrame)
	{
		StartValues["Rotation"] = startEndValues.Key;
		EndValues["Rotation"] = startEndValues.Value;
		TweenType = Type.ROTATION;
		ValueKeys.Add("Rotation");
	}
}
