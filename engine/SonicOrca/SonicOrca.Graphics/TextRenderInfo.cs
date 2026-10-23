using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

/// <summary>
/// The text, font, bounds, colours and shadow for one text draw call.
/// </summary>
public class TextRenderInfo
{
	public Font Font { get; set; }

	public Rectangle Bounds { get; set; }

	public FontAlignment Alignment { get; set; }

	public Colour Colour { get; set; }

	public int? Overlay { get; set; }

	public string Text { get; set; }

	public Vector2 Shadow { get; set; }

	public Colour ShadowColour { get; set; }

	public int? ShadowOverlay { get; set; }

	public float SizeMultiplier { get; set; }

	public TextRenderInfo()
	{
		Colour = Colours.White;
	}
}
