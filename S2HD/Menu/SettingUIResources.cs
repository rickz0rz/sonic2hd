using SonicOrca.Audio;
using SonicOrca.Extensions;
using SonicOrca.Graphics;
using SonicOrca.Resources;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// Loads and keeps the fonts, textures and sounds of the option menus.
/// </summary>
internal class SettingUIResources : IControlResources, ISettingUIResources
{
	[ResourcePath("SONICORCA/FONTS/IMPACT/REGULAR")]
	public Font Font { get; set; }

	[ResourcePath("SONICORCA/MENU/OPTIONS/AUDIOSLIDER/EMPTY")]
	public ITexture AudioSliderEmptyTexture { get; set; }

	[ResourcePath("SONICORCA/MENU/OPTIONS/AUDIOSLIDER/SILVER")]
	public ITexture AudioSliderSilverTexture { get; set; }

	[ResourcePath("SONICORCA/MENU/OPTIONS/AUDIOSLIDER/GOLD")]
	public ITexture AudioSliderGoldTexture { get; set; }

	[ResourcePath("SONICORCA/SOUND/NAVIGATE/CURSOR")]
	public Sample NavigateSample { get; set; }

	[ResourcePath("SONICORCA/SOUND/NAVIGATE/YES")]
	public Sample ConfirmSample { get; set; }

	[ResourcePath("SONICORCA/SOUND/NAVIGATE/NO")]
	public Sample CancelSample { get; set; }

	[ResourcePath("SONICORCA/MENU/GAMEPAD/A")]
	public ITexture ButtonA { get; set; }

	[ResourcePath("SONICORCA/MENU/GAMEPAD/B")]
	public ITexture ButtonB { get; set; }

	[ResourcePath("SONICORCA/MENU/OPTIONS/V2/UI/SELECTION/BAR")]
	public ITexture SelectionBar { get; set; }

	public void PushDependencies(ResourceSession resourceSession)
	{
		resourceSession.PushDependenciesByAttribute(this);
	}

	public void FetchResources(ResourceTree resourceTree)
	{
		resourceTree.FullfillLoadedResourcesByAttribute(this);
	}
}
