using SonicOrca.Drawing.Renderers;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing;

public class DefaultRendererFactory : IRendererFactory
{
	private readonly IGraphicsContext _graphicsContext;

	private DefaultRendererFactory(IGraphicsContext graphicsContext)
	{
		_graphicsContext = graphicsContext;
	}

	public static IRendererFactory Create(IGraphicsContext graphicsContext)
	{
		return new DefaultRendererFactory(graphicsContext);
	}

	public I2dRenderer Create2dRenderer()
	{
		return null;
	}

	public IFontRenderer CreateFontRenderer()
	{
		return null;
	}

	public IFadeTransitionRenderer CreateFadeTransitionRenderer()
	{
		return new ClassicFadeTransitionRenderer(_graphicsContext);
	}

	public ITileRenderer CreateTileRenderer()
	{
		return null;
	}

	public IObjectRenderer CreateObjectRenderer()
	{
		return null;
	}

	public ICharacterRenderer CreateCharacterRenderer()
	{
		return null;
	}

	public IWaterRenderer CreateWaterRenderer()
	{
		return null;
	}

	public IHeatRenderer CreateHeatRenderer()
	{
		return null;
	}

	public INonLayerRenderer CreateNonLayerRenderer()
	{
		return null;
	}

	public IMaskRenderer CreateMaskRenderer()
	{
		return null;
	}
}
