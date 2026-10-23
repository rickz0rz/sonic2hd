using System;
using System.Collections.Generic;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

/// <summary>
/// The base class for the renderer. It gives access to the specialised renderers and makes sure that only one of
/// them is active at a time.
/// </summary>
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

	/// <summary>
	/// Writes the four corners of a rectangle in this order: top left, bottom left, bottom right, top right.
	/// </summary>
	public static void GetVertices(Rectangle destination, Vector2[] vertices)
	{
		vertices[0].X = destination.Left;
		vertices[0].Y = destination.Top;
		vertices[1].X = destination.Left;
		vertices[2].Y = vertices[1].Y = vertices[0].Y + destination.Height;
		vertices[3].X = vertices[2].X = vertices[0].X + destination.Width;
		vertices[3].Y = destination.Top;
	}

	/// <summary>
	/// Writes the texture coordinates of a source rectangle, in the same corner order as <see cref="GetVertices"/>.
	/// </summary>
	public static void GetTextureMappings(ITexture texture, Rectanglei source, Vector2[] textureMappings, bool flipX = false, bool flipY = false)
	{
		double left = flipX ? (source.Right / (double)texture.Width) : (source.Left / (double)texture.Width);
		double right = flipX ? (source.Left / (double)texture.Width) : (source.Right / (double)texture.Width);
		double top = flipY ? (source.Bottom / (double)texture.Height) : (source.Top / (double)texture.Height);
		double bottom = flipY ? (source.Top / (double)texture.Height) : (source.Bottom / (double)texture.Height);
		SetQuadMappings(textureMappings, left, top, right, bottom);
	}

	/// <summary>
	/// Does the same as <see cref="GetTextureMappings"/>, but moves each edge half a texel into the source
	/// rectangle. This stops the texture filter from sampling the texels outside the rectangle.
	/// </summary>
	public static void GetTextureMappingsHalfIn(ITexture texture, Rectanglei source, ref Vector2[] textureMappings, bool flipX = false, bool flipY = false)
	{
		double left = flipX ? ((source.Right - 0.5) / texture.Width) : ((source.Left + 0.5) / texture.Width);
		double right = flipX ? ((source.Left + 0.5) / texture.Width) : ((source.Right - 0.5) / texture.Width);
		double top = flipY ? ((source.Bottom - 0.5) / texture.Height) : ((source.Top + 0.5) / texture.Height);
		double bottom = flipY ? ((source.Top + 0.5) / texture.Height) : ((source.Bottom - 0.5) / texture.Height);
		SetQuadMappings(textureMappings, left, top, right, bottom);
	}

	private static void SetQuadMappings(Vector2[] textureMappings, double left, double top, double right, double bottom)
	{
		textureMappings[0].X = left;
		textureMappings[0].Y = top;
		textureMappings[1].X = left;
		textureMappings[1].Y = bottom;
		textureMappings[2].X = right;
		textureMappings[2].Y = bottom;
		textureMappings[3].X = right;
		textureMappings[3].Y = top;
	}

	/// <summary>
	/// Creates the specialised renderers up front. They are otherwise created on first use, and compiling their
	/// shaders mid-game causes visible hitches on slow GPUs (over 80 ms on Intel HD 4000).
	/// </summary>
	public virtual void Prewarm()
	{
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
