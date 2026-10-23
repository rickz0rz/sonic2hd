namespace SonicOrca.Drawing.LevelRendering;

/// <summary>
/// Holds the options that turn optional rendering features on or off.
/// </summary>
public class RenderingOptions : IRenderingOptions
{
	public bool EnableShadows { get; set; }

	public bool EnableEffects { get; set; }

	public bool EnableParticles { get; set; }
}
