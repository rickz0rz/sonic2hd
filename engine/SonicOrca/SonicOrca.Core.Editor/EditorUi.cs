using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The look of the editors: the sizes, the colours, and the drawing of the text, the buttons, the headings and the
/// rows of the panels. The level editor and the animation editor use it, so that they look the same.
/// </summary>
internal sealed class EditorUi
{
	public const string FontResourceKey = "SONICORCA/FONTS/SEGOEUI";

	public const double TopBarHeight = 48.0;

	public const double StatusBarHeight = 36.0;

	public const double PanelWidth = 460.0;

	public const double RowHeight = 30.0;

	public const double Padding = 10.0;

	/// <summary>The font is drawn at this scale.</summary>
	private const double TextScale = 0.5;

	public static readonly Colour PanelColour = new Colour(0.96, 0.07, 0.08, 0.11);

	public static readonly Colour ButtonColour = new Colour(1.0, 0.18, 0.2, 0.26);

	public static readonly Colour ButtonHoverColour = new Colour(1.0, 0.27, 0.3, 0.38);

	public static readonly Colour AccentColour = new Colour(1.0, 0.16, 0.44, 0.82);

	public static readonly Colour TextColour = Colours.White;

	public static readonly Colour DimTextColour = new Colour(1.0, 0.62, 0.65, 0.7);

	public static readonly Colour OutlineColour = new Colour(0.45, 0.4, 0.85, 1.0);

	public static readonly Colour HoverColour = Colours.White;

	public static readonly Colour SelectionColour = new Colour(1.0, 1.0, 0.8, 0.1);

	public static readonly Colour SelectionFillColour = new Colour(0.18, 1.0, 0.8, 0.1);

	public static readonly Colour ShadowColour = new Colour(0.8, 0.0, 0.0, 0.0);

	/// <summary>The font of the texts, or null before it loads. Without it, the editor draws no text.</summary>
	public Font Font { get; set; }

	/// <summary>True when the font and its texture are ready. The texture can come a little after the load of the font.</summary>
	public bool HasFont => Font?.ShapeTexture != null;

	public double TextHeight => Font.Height * TextScale;

	public double MeasureText(string text) => HasFont ? Font.MeasureString(text).Width * TextScale : text.Length * 10.0;

	public void DrawText(Renderer renderer, string text, double x, double y, Colour colour)
	{
		if (!HasFont || string.IsNullOrEmpty(text))
		{
			return;
		}
		I2dRenderer g = renderer.Get2dRenderer();
		using (g.BeginMatixState())
		{
			g.ModelMatrix = (g.ModelMatrix * Matrix4.CreateScale(TextScale, TextScale)).Translate(x, y);
			renderer.GetFontRenderer().RenderString(text, default, FontAlignment.Left, Font, colour);
		}
	}

	/// <summary>Draws a text on a dark box, for labels on the view.</summary>
	public void DrawLabel(Renderer renderer, I2dRenderer g, string text, double x, double y, Colour colour)
	{
		if (!HasFont)
		{
			return;
		}
		Rectangle box = new Rectangle(x, y, MeasureText(text) + Padding, RowHeight - 4.0);
		g.RenderQuad(PanelColour, box);
		DrawText(renderer, text, box.X + Padding / 2.0, box.Y + 2.0, colour);
	}

	public static void DrawCross(I2dRenderer g, Vector2 centre, double size, Colour colour, double thickness)
	{
		g.RenderLine(colour, new Vector2(centre.X - size, centre.Y), new Vector2(centre.X + size, centre.Y), thickness);
		g.RenderLine(colour, new Vector2(centre.X, centre.Y - size), new Vector2(centre.X, centre.Y + size), thickness);
	}

	/// <summary>The width of a button of the top bar for a text.</summary>
	public double GetButtonWidth(string text) => MeasureText(text) + Padding * 2.0;

	/// <summary>Draws a button of the top bar.</summary>
	public void DrawButton(Renderer renderer, I2dRenderer g, Rectangle bounds, string text, bool active, bool hover, bool enabled)
	{
		g.RenderQuad(active ? AccentColour : (hover ? ButtonHoverColour : ButtonColour), bounds);
		DrawText(renderer, text, bounds.X + Padding, bounds.Y + (bounds.Height - TextHeight) / 2.0, enabled ? TextColour : DimTextColour);
	}

	/// <summary>Draws the heading of a part of a panel.</summary>
	public void DrawHeading(Renderer renderer, I2dRenderer g, string text, double left, double top, double width = PanelWidth)
	{
		g.RenderQuad(ButtonColour, new Rectangle(left, top, width, RowHeight));
		DrawText(renderer, text, left + Padding, top + (RowHeight - TextHeight) / 2.0, TextColour);
	}

	/// <summary>Draws a row of a list: the text on the left and the detail on the right.</summary>
	public void DrawListRow(Renderer renderer, I2dRenderer g, EditorListRow listRow, Rectangle bounds, Vector2 mouse)
	{
		if (listRow.Active)
		{
			g.RenderQuad(AccentColour, bounds);
		}
		else if (bounds.Contains(mouse))
		{
			g.RenderQuad(ButtonHoverColour, bounds);
		}
		double textY = bounds.Y + (RowHeight - TextHeight) / 2.0;
		DrawText(renderer, listRow.Text, bounds.X + Padding, textY, listRow.Dim ? DimTextColour : TextColour);
		if (!string.IsNullOrEmpty(listRow.Detail))
		{
			DrawText(renderer, listRow.Detail, bounds.Right - Padding - MeasureText(listRow.Detail), textY, listRow.DetailColour ?? DimTextColour);
		}
	}

	/// <summary>Draws a row of the properties: the label, and the value or the text that the user types.</summary>
	public void DrawPropertyRow(Renderer renderer, I2dRenderer g, EditorPropertyRow row, Rectangle bounds, Vector2 mouse, EditorTextEntry entry, int frame)
	{
		bool editing = entry.IsEditing(row.Label);
		bool changeable = row.Commit != null || row.Step != null;
		if (editing)
		{
			g.RenderQuad(AccentColour, bounds);
		}
		else if (changeable && bounds.Contains(mouse))
		{
			g.RenderQuad(ButtonHoverColour, bounds);
		}
		double textY = bounds.Y + (RowHeight - TextHeight) / 2.0;
		DrawText(renderer, row.Label, bounds.X + Padding, textY, row.IsHeading ? SelectionColour : DimTextColour);
		string value = editing ? entry.Text + ((frame / 30) % 2 == 0 ? "|" : "") : row.Value;
		DrawText(renderer, value, bounds.X + bounds.Width * 0.45, textY, changeable || row.IsHeading ? TextColour : DimTextColour);
	}
}

/// <summary>
/// The value that the user types into a property row. The first key replaces the old value; Backspace deletes a
/// character.
/// </summary>
internal sealed class EditorTextEntry
{
	private bool _replace;

	/// <summary>The label of the row that the user types into, or null.</summary>
	public string Row { get; private set; }

	public string Text { get; private set; } = "";

	public bool IsActive => Row != null;

	public bool IsEditing(string row) => Row != null && Row == row;

	public void Begin(string row, string value)
	{
		Row = row;
		Text = value ?? "";
		_replace = true;
	}

	public void Cancel() => Row = null;

	/// <summary>Takes the typed text and Backspace. Returns true when the user pressed Enter: the value is ready.</summary>
	public bool Update(EditorInput input)
	{
		string text = input.TextInput;
		if (!string.IsNullOrEmpty(text))
		{
			Text = _replace ? text : Text + text;
			_replace = false;
		}
		if (input.IsKeyPressed(KeyboardState.KEY_BACKSPACE) && Text.Length > 0)
		{
			Text = _replace ? "" : Text.Substring(0, Text.Length - 1);
			_replace = false;
		}
		return input.IsKeyPressed(KeyboardState.KEY_RETURN) || input.IsKeyPressed(KeyboardState.KEY_KP_ENTER);
	}
}
