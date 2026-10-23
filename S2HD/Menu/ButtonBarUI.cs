using System.Collections.Immutable;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace S2HD.Menu;

/// <summary>
/// Draws a bar of gamepad button hints, for example "A APPLY", aligned to the right of its bounds.
/// </summary>
internal class ButtonBarUI
{
	/// <summary>The horizontal space between two items.</summary>
	private const int ItemSpacing = 16;
	/// <summary>The width of a button icon. The text starts after it.</summary>
	private const int ButtonIconWidth = 64;

	private readonly IControlResources _controlResources;
	private Rectanglei _bounds;
	private ImmutableArray<ButtonBarItem> _items;
	private bool _layoutDirty = true;
	private int[] _itemX;

	public Rectanglei Bounds
	{
		get => _bounds;
		set
		{
			_bounds = value;
			_layoutDirty = true;
		}
	}

	public ImmutableArray<ButtonBarItem> Items
	{
		get => _items;
		set
		{
			_items = value;
			_layoutDirty = true;
		}
	}

	public ButtonBarUI(IControlResources resources) => _controlResources = resources;

	private void CalculateItemPositions(I2dRenderer g)
	{
		int length = _items.Length;
		int x = 0;
		_itemX = new int[length];
		for (int index = 0; index < length; ++index)
		{
			_itemX[index] = x;
			ButtonBarItem buttonBarItem = _items[index];
			x = x + GetItemWidth(g, buttonBarItem) + ItemSpacing;
		}
		// Move all the items to the right, so that the last item ends at the right edge.
		int offsetX = Bounds.Width - (x - ItemSpacing);
		for (int index = 0; index < length; ++index)
			_itemX[index] += offsetX;
	}

	private int GetItemWidth(I2dRenderer g, ButtonBarItem item)
	{
		int width = 0;
		if (GetButtonTexture(item) != null)
			width += ButtonIconWidth;
		TextRenderInfo textRenderInfo = GetTextRenderInfo(new Rectanglei(), item.Text);
		return width + (int)g.MeasureText(textRenderInfo).Width;
	}

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		if (_layoutDirty)
		{
			_layoutDirty = false;
			CalculateItemPositions(g);
		}
		DrawStrip(g);
		int y = Bounds.Centre.Y;
		for (int index = 0; index < _items.Length; ++index)
		{
			ButtonBarItem buttonBarItem = _items[index];
			int x = _itemX[index];
			DrawButton(g, buttonBarItem, x, y);
		}
	}

	private void DrawStrip(I2dRenderer g)
	{
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour(0.2, 0.0, 0.0, 0.0), (Rectangle)Bounds);
	}

	private void DrawButton(I2dRenderer g, ButtonBarItem item, int x, int y)
	{
		// The icon is drawn at its centre, so move to the middle of the icon, then to its right edge.
		x += ButtonIconWidth / 2;
		ITexture buttonTexture = GetButtonTexture(item);
		if (buttonTexture != null)
		{
			g.BlendMode = BlendMode.Alpha;
			g.Colour = Colours.White;
			g.RenderTexture(buttonTexture, (Vector2)new Vector2i(x, y));
			x += ButtonIconWidth / 2;
		}
		if (string.IsNullOrEmpty(item.Text))
			return;
		TextRenderInfo textRenderInfo = GetTextRenderInfo(new Rectanglei(x, y - Bounds.Height / 2, 0, Bounds.Height), item.Text);
		g.RenderText(textRenderInfo);
	}

	private ITexture GetButtonTexture(ButtonBarItem item)
	{
		switch (item.Button)
		{
			case GamePadButton.A:
				return _controlResources.ButtonA;
			case GamePadButton.B:
				return _controlResources.ButtonB;
			default:
				return null;
		}
	}

	private TextRenderInfo GetTextRenderInfo(Rectanglei bounds, string text)
	{
		return new TextRenderInfo()
		{
			Bounds = (Rectangle)bounds,
			Colour = Colours.White,
			Overlay = 0,
			Alignment = FontAlignment.MiddleY,
			Font = _controlResources.Font,
			SizeMultiplier = 0.75f,
			Text = text
		};
	}
}
