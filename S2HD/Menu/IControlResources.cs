using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// The font and the button textures that the button bar uses.
/// </summary>
internal interface IControlResources
{
	Font Font { get; }

	ITexture ButtonA { get; }

	ITexture ButtonB { get; }
}
