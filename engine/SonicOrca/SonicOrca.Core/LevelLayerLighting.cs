namespace SonicOrca.Core;

/// <summary>
/// The light settings of a level layer.
/// </summary>
public class LevelLayerLighting
{
	public LevelLayerLightingType Type { get; set; }

	public double Light { get; set; }

	public LevelLayerLighting()
	{
		Light = 1.0;
	}
}
