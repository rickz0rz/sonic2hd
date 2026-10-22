using System;
using System.Collections.Generic;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public abstract class Renderer : IDisposable
{
	private readonly HashSet<IRenderer> _registeredRenderers = new HashSet<IRenderer>();

	private readonly WindowContext _window;

	private IRenderer _currentRenderer;

	public WindowContext Window => _window;

	public IRenderer CurrentRenderer => _currentRenderer;

	public Renderer(WindowContext window)
	{
		_window = window;
	}

	public void Dispose()
	{
		foreach (IRenderer registeredRenderer in _registeredRenderers)
		{
			registeredRenderer.Dispose();
		}
	}

	public void RegisterRenderer(IRenderer renderer)
	{
		_registeredRenderers.Add(renderer);
	}

	public void ActivateRenderer(IRenderer renderer)
	{
		if (_currentRenderer != null && _currentRenderer != renderer)
		{
			_currentRenderer.Deactivate();
		}
		_currentRenderer = renderer;
	}

	public void DeativateRenderer()
	{
		ActivateRenderer(null);
	}

	public static void GetVertices(Rectangle destination, Vector2[] vertices)
	{
		vertices[0].X = destination.Left;
		vertices[0].Y = destination.Top;
		vertices[1].X = destination.Left;
		ref Vector2 reference = ref vertices[2];
		double y = (vertices[1].Y = vertices[0].Y + destination.Height);
		reference.Y = y;
		ref Vector2 reference2 = ref vertices[3];
		y = (vertices[2].X = vertices[0].X + destination.Width);
		reference2.X = y;
		vertices[3].Y = destination.Top;
	}

	public static void GetTextureMappings(ITexture texture, Rectanglei source, Vector2[] textureMappings, bool flipX = false, bool flipY = false)
	{
		double x = (flipX ? ((double)source.Right / (double)texture.Width) : ((double)source.Left / (double)texture.Width));
		double x2 = (flipX ? ((double)source.Left / (double)texture.Width) : ((double)source.Right / (double)texture.Width));
		double y = (flipY ? ((double)source.Bottom / (double)texture.Height) : ((double)source.Top / (double)texture.Height));
		double y2 = (flipY ? ((double)source.Top / (double)texture.Height) : ((double)source.Bottom / (double)texture.Height));
		textureMappings[0].X = x;
		textureMappings[0].Y = y;
		textureMappings[1].X = x;
		textureMappings[1].Y = y2;
		textureMappings[2].X = x2;
		textureMappings[2].Y = y2;
		textureMappings[3].X = x2;
		textureMappings[3].Y = y;
	}

	public static void GetTextureMappingsHalfIn(ITexture texture, Rectanglei source, ref Vector2[] textureMappings, bool flipX = false, bool flipY = false)
	{
		double x = (flipX ? (((double)source.Right - 0.5) / (double)texture.Width) : (((double)source.Left + 0.5) / (double)texture.Width));
		double x2 = (flipX ? (((double)source.Left + 0.5) / (double)texture.Width) : (((double)source.Right - 0.5) / (double)texture.Width));
		double y = (flipY ? (((double)source.Bottom - 0.5) / (double)texture.Height) : (((double)source.Top + 0.5) / (double)texture.Height));
		double y2 = (flipY ? (((double)source.Top + 0.5) / (double)texture.Height) : (((double)source.Bottom - 0.5) / (double)texture.Height));
		textureMappings[0].X = x;
		textureMappings[0].Y = y;
		textureMappings[1].X = x;
		textureMappings[1].Y = y2;
		textureMappings[2].X = x2;
		textureMappings[2].Y = y2;
		textureMappings[3].X = x2;
		textureMappings[3].Y = y;
	}

	public abstract I2dRenderer Get2dRenderer();

	public abstract IFontRenderer GetFontRenderer();

	public abstract ITileRenderer GetTileRenderer();

	public abstract IObjectRenderer GetObjectRenderer();

	public abstract ICharacterRenderer GetCharacterRenderer();

	public abstract IWaterRenderer GetWaterRenderer();

	public abstract IHeatRenderer GetHeatRenderer();

	public abstract INonLayerRenderer GetNonLayerRenderer();

	public abstract IMaskRenderer GetMaskRenderer();

	public abstract IFadeTransitionRenderer CreateFadeTransitionRenderer();
}
