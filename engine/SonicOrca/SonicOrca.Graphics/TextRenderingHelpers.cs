using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public static class TextRenderingHelpers
{
	public static Rectangle RenderWith2d(I2dRenderer g, TextRenderInfo textRenderInfo)
	{
		if (string.IsNullOrEmpty(textRenderInfo.Text) || textRenderInfo.Colour.Alpha == 0)
		{
			return default;
		}
		if (textRenderInfo.Shadow != default(Vector2))
		{
			new TextRenderInfo
			{
				Font = textRenderInfo.Font,
				Bounds = textRenderInfo.Bounds.OffsetBy(textRenderInfo.Shadow),
				Alignment = textRenderInfo.Alignment,
				Colour = textRenderInfo.ShadowColour,
				Overlay = textRenderInfo.ShadowOverlay,
				Text = textRenderInfo.Text
			};
		}
		return RenderWith2dInternal(g, textRenderInfo);
	}

	private static Rectangle RenderWith2dInternal(I2dRenderer g, TextRenderInfo textRenderInfo)
	{
		Font font = textRenderInfo.Font;
		string text = textRenderInfo.Text;
		Colour colour = textRenderInfo.Colour;
		int? overlay = textRenderInfo.Overlay;
		Rectangle result = font.MeasureString(text, textRenderInfo.Bounds, textRenderInfo.Alignment);
		switch (textRenderInfo.Alignment & FontAlignment.HorizontalMask)
		{
		case FontAlignment.MiddleX:
			result.X += result.Width * (double)textRenderInfo.SizeMultiplier / 2.0;
			break;
		case FontAlignment.Right:
			result.X += result.Width * (double)textRenderInfo.SizeMultiplier;
			break;
		}
		switch (textRenderInfo.Alignment & FontAlignment.VerticalMask)
		{
		case FontAlignment.MiddleY:
		{
			double num = (float)font.Height * textRenderInfo.SizeMultiplier;
			result.Y += (result.Height - num) / 2.0;
			break;
		}
		case FontAlignment.Bottom:
			result.X += result.Height * (double)textRenderInfo.SizeMultiplier;
			break;
		}
		Vector2 destination = new Vector2(result.X, result.Y);
		string text2 = text;
		foreach (char key in text2)
		{
			Font.CharacterDefinition characterDefinition = font[key];
			if (characterDefinition == null)
			{
				destination.X += (float)font.DefaultWidth * textRenderInfo.SizeMultiplier;
			}
			else
			{
				RenderCharacter(g, font, characterDefinition, destination, colour, overlay, textRenderInfo.SizeMultiplier);
				destination.X += (float)characterDefinition.Width * textRenderInfo.SizeMultiplier;
			}
			destination.X += (float)font.Tracking * textRenderInfo.SizeMultiplier;
		}
		return result;
	}

	private static void RenderCharacter(I2dRenderer g, Font font, Font.CharacterDefinition characterDefinition, Vector2 destination, Colour colour, int? overlay, float sizeMultiplier)
	{
		ITexture[] array = new ITexture[2] { font.ShapeTexture, null };
		if (overlay.HasValue)
		{
			array[1] = font.OverlayTextures[overlay.Value];
		}
		Rectangle source = characterDefinition.SourceRectangle;
		Rectangle destination2 = new Rectangle(destination.X + (double)((float)characterDefinition.Offset.X * sizeMultiplier), destination.Y + (double)((float)characterDefinition.Offset.Y * sizeMultiplier), source.Width * (double)sizeMultiplier, source.Height * (double)sizeMultiplier);
		g.BlendMode = BlendMode.Alpha;
		g.Colour = colour;
		if (overlay.HasValue)
		{
			g.RenderTexture(array, source, destination2);
		}
		else
		{
			g.RenderTexture(array[0], source, destination2);
		}
	}

	public static Rectangle MeasureWith2d(I2dRenderer g, TextRenderInfo textRenderInfo)
	{
		Font font = textRenderInfo.Font;
		string text = textRenderInfo.Text;
		_ = textRenderInfo.Colour;
		_ = textRenderInfo.Overlay;
		Rectangle result = font.MeasureString(text, textRenderInfo.Bounds, textRenderInfo.Alignment);
		switch (textRenderInfo.Alignment & FontAlignment.HorizontalMask)
		{
		case FontAlignment.MiddleX:
			result.X += result.Width * (double)textRenderInfo.SizeMultiplier / 2.0;
			break;
		case FontAlignment.Right:
			result.X += result.Width * (double)textRenderInfo.SizeMultiplier;
			break;
		}
		switch (textRenderInfo.Alignment & FontAlignment.VerticalMask)
		{
		case FontAlignment.MiddleY:
			result.Y += result.Height * (double)textRenderInfo.SizeMultiplier / 2.0;
			break;
		case FontAlignment.Right:
			result.X += result.Height * (double)textRenderInfo.SizeMultiplier;
			break;
		}
		Vector2 vector = new Vector2(result.X, result.Y);
		string text2 = text;
		foreach (char key in text2)
		{
			Font.CharacterDefinition characterDefinition = font[key];
			if (characterDefinition == null)
			{
				vector.X += (float)font.DefaultWidth * textRenderInfo.SizeMultiplier;
			}
			else
			{
				vector.X += (float)characterDefinition.Width * textRenderInfo.SizeMultiplier;
			}
			vector.X += (float)font.Tracking * textRenderInfo.SizeMultiplier;
		}
		return result;
	}
}
