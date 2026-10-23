// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.ButtonBarUI
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Immutable;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

internal class ButtonBarUI
{
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
		int num1 = 0;
		_itemX = new int[length];
		for (int index = 0; index < length; ++index)
		{
			_itemX[index] = num1;
			ButtonBarItem buttonBarItem = _items[index];
			num1 = num1 + GetItemWidth(g, buttonBarItem) + 16 /*0x10*/;
		}
		int num2 = Bounds.Width - (num1 - 16 /*0x10*/);
		for (int index = 0; index < length; ++index)
			_itemX[index] += num2;
	}

	private int GetItemWidth(I2dRenderer g, ButtonBarItem item)
	{
		int num = 0;
		if (GetButtonTexture(item) != null)
			num += 64 /*0x40*/;
		TextRenderInfo textRenderInfo = GetTextRenderInfo(new Rectanglei(), item.Text);
		return num + (int)g.MeasureText(textRenderInfo).Width;
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
		x += 32 /*0x20*/;
		ITexture buttonTexture = GetButtonTexture(item);
		if (buttonTexture != null)
		{
			g.BlendMode = BlendMode.Alpha;
			g.Colour = Colours.White;
			g.RenderTexture(buttonTexture, (Vector2)new Vector2i(x, y));
			x += 32 /*0x20*/;
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
			Overlay = new int?(0),
			Alignment = FontAlignment.MiddleY,
			Font = _controlResources.Font,
			SizeMultiplier = 0.75f,
			Text = text
		};
	}
}
