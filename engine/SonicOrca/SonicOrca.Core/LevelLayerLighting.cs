namespace SonicOrca.Core;

public class LevelLayerLighting
{
	public LevelLayerLightingType Type { get; set; }

	public double Light { get; set; }

	public LevelLayerLighting()
	{
		Light = 1.0;
	}
}
