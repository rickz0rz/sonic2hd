using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Copies a texture or a framebuffer into a framebuffer.
/// </summary>
internal static class RenderingHelpers
{
	public static void RenderToFramebuffer(Renderer renderer, IFramebuffer source, IFramebuffer destination, BlendMode blend = BlendMode.Alpha)
	{
		RenderToFramebuffer(renderer, source.Textures[0], destination, new Rectangle(0.0, 0.0, destination.Width, destination.Height), blend);
	}

	public static void RenderToFramebuffer(Renderer renderer, ITexture source, IFramebuffer destination, BlendMode blend = BlendMode.Alpha)
	{
		RenderToFramebuffer(renderer, source, destination, new Rectangle(0.0, 0.0, destination.Width, destination.Height), blend);
	}

	public static void RenderToFramebuffer(Renderer renderer, ITexture source, IFramebuffer destination, Rectangle destinationRect, BlendMode blend)
	{
		destination.Activate();
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		i2dRenderer.BlendMode = blend;
		i2dRenderer.Colour = Colours.White;
		i2dRenderer.ClipRectangle = destinationRect;
		i2dRenderer.ModelMatrix = Matrix4.Identity;
		i2dRenderer.RenderTexture(source, new Rectangle(0.0, 0.0, source.Width, source.Height), destinationRect, flipx: false, flipy: true);
		i2dRenderer.Deactivate();
	}
}
