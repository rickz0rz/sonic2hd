using SonicOrca.Graphics;

namespace SonicOrca;

/// <summary>
/// The video settings that the user selected.
/// </summary>
public interface IVideoSettings
{
	VideoMode Mode { get; }

	Resolution Resolution { get; }

	bool EnableShadows { get; }

	bool EnableHeatEffects { get; }

	bool EnableWaterEffects { get; }
}
