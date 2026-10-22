using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface IFontRenderer
{
	FontAlignment Alignment { get; set; }

	Rectangle Boundary { get; set; }

	Colour Colour { get; set; }

	Font Font { get; set; }

	int Overlay { get; set; }

	Vector2 Shadow { get; set; }

	string Text { get; set; }

	Rectangle Measure();

	void Render();

	void RenderString(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, Colour colour, int? overlay = null);

	void RenderString(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, int overlay);

	void RenderStringWithShadow(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, Colour colour, int? overlay = null);

	void RenderStringWithShadow(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, Colour colour, int? overlay, Vector2i? shadow, Colour shadowColour, int? shadowOverlay = null);

	void RenderStringWithShadow(string text, Rectangle boundary, FontAlignment fontAlignment, Font font, int overlay);
}
