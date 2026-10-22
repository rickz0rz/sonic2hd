namespace SonicOrca.Graphics;

public interface IRendererFactory
{
	I2dRenderer Create2dRenderer();

	IFontRenderer CreateFontRenderer();

	IFadeTransitionRenderer CreateFadeTransitionRenderer();

	ITileRenderer CreateTileRenderer();

	IObjectRenderer CreateObjectRenderer();

	ICharacterRenderer CreateCharacterRenderer();

	IWaterRenderer CreateWaterRenderer();

	IHeatRenderer CreateHeatRenderer();

	INonLayerRenderer CreateNonLayerRenderer();

	IMaskRenderer CreateMaskRenderer();
}
