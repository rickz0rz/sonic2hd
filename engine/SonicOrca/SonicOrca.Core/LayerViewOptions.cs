namespace SonicOrca.Core;

/// <summary>
/// The settings that control what a level layer draws.
/// </summary>
public class LayerViewOptions
{
	public bool ShowLandscape { get; set; }

	public bool ShowObjects { get; set; }

	public bool ShowMarkers { get; set; }

	public bool ShowWater { get; set; }

	public bool ShowLandscapeCollision { get; set; }

	public bool ShowObjectCollision { get; set; }

	public bool Shadows { get; set; }

	public int Filter { get; set; }

	public double FilterAmount { get; set; }

	public LayerViewOptions()
	{
		ShowLandscape = true;
		ShowObjects = true;
		ShowWater = true;
	}
}
