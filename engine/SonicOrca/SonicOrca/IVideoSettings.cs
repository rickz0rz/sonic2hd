using SonicOrca.Graphics;

namespace SonicOrca;

public interface IVideoSettings
{
	VideoMode Mode { get; }

	Resolution Resolution { get; }

	bool EnableShadows { get; }

	bool EnableHeatEffects { get; }

	bool EnableWaterEffects { get; }
}
