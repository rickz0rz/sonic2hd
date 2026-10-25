using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

/// <summary>
/// Draws and measures text with a 2D renderer, one character at a time.
/// </summary>
public static class TextRenderingHelpers
{
	public static Rectangle RenderWith2d(I2dRenderer g, TextRenderInfo textRenderInfo)
	{
		if (string.IsNullOrEmpty(textRenderInfo.Text) || textRenderInfo.Colour.Alpha == 0)
		{
			return default;
		}
		if (textRenderInfo.Shadow != default)
		{
			// The shadow is a copy of the text, moved and in the shadow colour, under the text
			RenderWith2dInternal(g, new TextRenderInfo
			{
				Font = textRenderInfo.Font,
				Bounds = textRenderInfo.Bounds.OffsetBy(textRenderInfo.Shadow),
				Alignment = textRenderInfo.Alignment,
				Colour = textRenderInfo.ShadowColour,
				Overlay = textRenderInfo.ShadowOverlay,
				Text = textRenderInfo.Text,
				SizeMultiplier = textRenderInfo.SizeMultiplier
			});
		}
		return RenderWith2dInternal(g, textRenderInfo);
	}

	private static Rectangle RenderWith2dInternal(I2dRenderer g, TextRenderInfo textRenderInfo)
	{
		Font font = textRenderInfo.Font;
		string text = textRenderInfo.Text;
		Colour colour = textRenderInfo.Colour;
		int? overlay = textRenderInfo.Overlay;
		Rectangle result = GetTextBounds(textRenderInfo);
		Vector2 destination = new Vector2(result.X, result.Y);
		foreach (char key in text)
		{
			Font.CharacterDefinition characterDefinition = font[key];
			if (characterDefinition == null)
			{
				destination.X += font.DefaultWidth * textRenderInfo.SizeMultiplier;
			}
			else
			{
				RenderCharacter(g, font, characterDefinition, destination, colour, overlay, textRenderInfo.SizeMultiplier);
				destination.X += characterDefinition.Width * textRenderInfo.SizeMultiplier;
			}
			destination.X += font.Tracking * textRenderInfo.SizeMultiplier;
		}
		return result;
	}

	private static void RenderCharacter(I2dRenderer g, Font font, Font.CharacterDefinition characterDefinition, Vector2 destination, Colour colour, int? overlay, float sizeMultiplier)
	{
		ITexture[] textures = new ITexture[2] { font.ShapeTexture, null };
		if (overlay.HasValue)
		{
			textures[1] = font.OverlayTextures[overlay.Value];
		}
		Rectangle source = characterDefinition.SourceRectangle;
		Rectangle characterDestination = new Rectangle(destination.X + (double)(characterDefinition.Offset.X * sizeMultiplier), destination.Y + (double)(characterDefinition.Offset.Y * sizeMultiplier), source.Width * (double)sizeMultiplier, source.Height * (double)sizeMultiplier);
		g.BlendMode = BlendMode.Alpha;
		g.Colour = colour;
		if (overlay.HasValue)
		{
			g.RenderTexture(textures, source, characterDestination);
		}
		else
		{
			g.RenderTexture(textures[0], source, characterDestination);
		}
	}

	/// <summary>Gives the bounds where <see cref="RenderWith2d"/> draws the text.</summary>
	public static Rectangle MeasureWith2d(I2dRenderer g, TextRenderInfo textRenderInfo)
	{
		return GetTextBounds(textRenderInfo);
	}

	/// <summary>The bounds of the text, with the alignment. Drawing and measuring use the same bounds.</summary>
	private static Rectangle GetTextBounds(TextRenderInfo textRenderInfo)
	{
		Font font = textRenderInfo.Font;
		Rectangle result = font.MeasureString(textRenderInfo.Text, textRenderInfo.Bounds, textRenderInfo.Alignment);
		switch (textRenderInfo.Alignment & FontAlignment.HorizontalMask)
		{
			case FontAlignment.MiddleX:
				result.X += result.Width * (double)textRenderInfo.SizeMultiplier / 2.0;
				break;
			case FontAlignment.Right:
				result.X += result.Width * (double)textRenderInfo.SizeMultiplier;
				break;
		}
		double scaledHeight = font.Height * textRenderInfo.SizeMultiplier;
		switch (textRenderInfo.Alignment & FontAlignment.VerticalMask)
		{
			case FontAlignment.MiddleY:
				result.Y += (result.Height - scaledHeight) / 2.0;
				break;
			case FontAlignment.Bottom:
				result.Y += result.Height - scaledHeight;
				break;
		}
		return result;
	}
}
