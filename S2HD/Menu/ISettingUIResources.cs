using SonicOrca.Audio;
using SonicOrca.Graphics;

namespace S2HD.Menu;

/// <summary>
/// The fonts, textures and sounds that the option menus use.
/// </summary>
internal interface ISettingUIResources
{
	Font Font { get; }

	ITexture AudioSliderEmptyTexture { get; }

	ITexture AudioSliderSilverTexture { get; }

	ITexture AudioSliderGoldTexture { get; }

	ITexture SelectionBar { get; set; }

	Sample NavigateSample { get; set; }

	Sample ConfirmSample { get; set; }

	Sample CancelSample { get; set; }
}
