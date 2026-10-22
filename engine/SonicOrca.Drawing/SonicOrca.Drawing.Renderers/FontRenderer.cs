using System;
using System.Collections.Generic;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Drawing.Renderers;

public class FontRenderer : IFontRenderer
{
	private static readonly Dictionary<Renderer, FontRenderer> RendererDictionary = new Dictionary<Renderer, FontRenderer>();

	private readonly Renderer _renderer;

	private ITexture[] textures = new ITexture[2];

	public Font Font { get; set; }

	public Colour Colour { get; set; }

	public Rectangle Boundary { get; set; }

	public FontAlignment Alignment { get; set; }

	public Vector2 Shadow { get; set; }

	public int Overlay { get; set; }

	public string Text { get; set; }

	public static FontRenderer FromRenderer(Renderer renderer)
	{
		if (!RendererDictionary.ContainsKey(renderer))
		{
			RendererDictionary.Add(renderer, new FontRenderer(renderer));
		}
		return RendererDictionary[renderer];
	}

	public FontRenderer(Renderer renderer)
	{
		_renderer = renderer;
	}

	public Rectangle Measure()
	{
		throw new NotImplementedException();
	}

	public void RenderString(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, int overlay)
	{
		RenderString(text, boundary, fontAlignment, font, Colours.White, overlay);
	}

	public void RenderString(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, Colour colour, int? overlay = null)
	{
		RenderStringWithShadow(text, boundary, fontAlignment, font, colour, overlay, null, Colours.Black, null);
	}

	public void RenderStringWithShadow(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, int overlay)
	{
		RenderStringWithShadow(text, boundary, fontAlignment, font, Colours.White, overlay, font.DefaultShadow, Colours.Black, null);
	}

	public void RenderStringWithShadow(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, Colour colour, int? overlay = null)
	{
		RenderStringWithShadow(text, boundary, fontAlignment, font, colour, overlay, font.DefaultShadow, new Colour(colour.Alpha, 0, 0, 0), null);
	}

	public void RenderStringWithShadow(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, Colour colour, int? overlay, Vector2i? shadow, Colour shadowColour, int? shadowOverlay)
	{
		SimpleRenderer g = SimpleRenderer.FromRenderer(_renderer);
		Rectangle rectangle = font.MeasureString(text, boundary, fontAlignment);
		Vector2 destination = new Vector2(rectangle.X, rectangle.Y);
		if (shadow.HasValue)
		{
			RenderStringWithShadow(text, boundary + shadow.Value, fontAlignment, font, shadowColour, shadowOverlay, null, default, null);
		}
		foreach (char key in text)
		{
			Font.CharacterDefinition characterDefinition = font[key];
			if (characterDefinition == null)
			{
				destination.X += font.DefaultWidth;
			}
			else
			{
				RenderCharacter(g, font, characterDefinition, destination, colour, overlay);
				destination.X += characterDefinition.Width;
			}
			destination.X += font.Tracking;
		}
	}

	private void RenderCharacter(SimpleRenderer g, Font font, Font.CharacterDefinition characterDefinition, Vector2 destination, Colour colour, int? overlay)
	{
		textures[0] = font.ShapeTexture;
		if (overlay.HasValue)
		{
			textures[1] = font.OverlayTextures[overlay.Value];
		}
		Rectangle source = characterDefinition.SourceRectangle;
		Rectangle destination2 = new Rectangle(destination.X + (double)characterDefinition.Offset.X, destination.Y + (double)characterDefinition.Offset.Y, source.Width, source.Height);
		destination2.X *= _renderer.GetObjectRenderer().Scale.X;
		destination2.Y *= _renderer.GetObjectRenderer().Scale.Y;
		destination2.Width *= _renderer.GetObjectRenderer().Scale.X;
		destination2.Height *= _renderer.GetObjectRenderer().Scale.Y;
		g.BlendMode = BlendMode.Alpha;
		g.Colour = colour;
		if (overlay.HasValue)
		{
			g.RenderTexture(textures, source, destination2);
		}
		else
		{
			g.RenderTexture(textures[0], source, destination2);
		}
	}

	public void Render()
	{
		SimpleRenderer.FromRenderer(_renderer);
	}
}
