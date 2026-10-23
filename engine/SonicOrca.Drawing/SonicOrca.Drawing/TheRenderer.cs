using SonicOrca.Drawing.Renderers;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing;

/// <summary>
/// Gives the renderers of the drawing library for one window.
/// </summary>
public class TheRenderer : Renderer
{
	public TheRenderer(WindowContext windowContext)
		: base(windowContext)
	{
	}

	public override void Prewarm()
	{
		Get2dRenderer();
		GetFontRenderer();
		GetTileRenderer();
		GetObjectRenderer();
		GetCharacterRenderer();
		GetWaterRenderer();
		GetHeatRenderer();
		GetNonLayerRenderer();
		GetMaskRenderer();
		// Not exposed through IRendererFactory: used by level rendering and the options menu
		WaterfallRenderer.FromRenderer(this);
		GaussianBlurRenderer.FromRenderer(this);
	}

	public override I2dRenderer Get2dRenderer()
	{
		return SimpleRenderer.FromRenderer(this);
	}

	public override IFontRenderer GetFontRenderer()
	{
		return FontRenderer.FromRenderer(this);
	}

	public override ITileRenderer GetTileRenderer()
	{
		return TileRenderer.FromRenderer(this);
	}

	public override IObjectRenderer GetObjectRenderer()
	{
		return ObjectRenderer.FromRenderer(this);
	}

	public override ICharacterRenderer GetCharacterRenderer()
	{
		return CharacterRenderer.FromRenderer(this);
	}

	public override IWaterRenderer GetWaterRenderer()
	{
		return WaterRenderer.FromRenderer(this);
	}

	public override IHeatRenderer GetHeatRenderer()
	{
		return HeatRenderer.FromRenderer(this);
	}

	public override INonLayerRenderer GetNonLayerRenderer()
	{
		return NonLayerRenderer.FromRenderer(this);
	}

	public override IMaskRenderer GetMaskRenderer()
	{
		return MaskRenderer.FromRenderer(this);
	}

	public override IFadeTransitionRenderer CreateFadeTransitionRenderer()
	{
		return new ClassicFadeTransitionRenderer(Window.GraphicsContext);
	}
}
