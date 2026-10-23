namespace SonicOrca.Drawing.LevelRendering;

/// <summary>
/// Gives the options that turn optional rendering features on or off.
/// </summary>
public interface IRenderingOptions
{
	bool EnableShadows { get; }

	bool EnableEffects { get; }

	bool EnableParticles { get; }
}
