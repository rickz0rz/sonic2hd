// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.SettingUI
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Linq;
using SonicOrca;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

internal class SettingUI
{
	private readonly int[] SliderWidths = new int[11]
	{
	0,
	45,
	59,
	73,
	89,
	105,
	121,
	139,
	159,
	181,
	223
	};
	private const char ArrowLeftChar = '◀';
	private const char ArrowRightChar = '▶';
	private readonly ISetting _setting;
	private readonly ISettingUIResources _resources;
	private string _name;
	private string[] _values;

	public Rectanglei Bounds { get; set; }

	public bool Highlighted { get; set; }

	public SettingUI(ISetting setting, ISettingUIResources resources)
	{
		_setting = setting;
		_resources = resources;
		_name = setting.Name;
		if (!(setting is ISpinnerSetting))
			return;
		_values = (setting as ISpinnerSetting).Values.ToArray<string>();
	}

	public void Draw(Renderer renderer)
	{
		DrawLeft(renderer);
		DrawRight(renderer);
	}

	private void DrawLeft(Renderer renderer)
	{
		renderer.GetFontRenderer().RenderStringWithShadow(_name, (Rectangle)Bounds, FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
	}

	private void DrawRight(Renderer renderer)
	{
		if (_setting is IAudioSliderSetting)
		{
			DrawRightAudioSlider(renderer, _setting as IAudioSliderSetting);
		}
		else
		{
			if (!(_setting is ISpinnerSetting))
				return;
			DrawRightSpinner(renderer, _setting as ISpinnerSetting);
		}
	}

	private void DrawRightAudioSlider(Renderer renderer, IAudioSliderSetting setting)
	{
		ITexture sliderEmptyTexture = _resources.AudioSliderEmptyTexture;
		ITexture texture = Highlighted ? _resources.AudioSliderGoldTexture : _resources.AudioSliderSilverTexture;
		Rectanglei destination1 = new Rectanglei(0, 0, sliderEmptyTexture.Width, sliderEmptyTexture.Height)
		{
			X = Bounds.Right - sliderEmptyTexture.Width - 24
		};
		destination1.Y = Bounds.Centre.Y - destination1.Height / 2;
		I2dRenderer obj = renderer.Get2dRenderer();
		obj.RenderTexture(sliderEmptyTexture, (Rectangle)destination1);
		int sliderWidth = SliderWidths[MathX.Clamp(0, (int)Math.Floor(setting.Value * SliderWidths.Length), SliderWidths.Length - 1)];
		Rectanglei destination2 = destination1 with
		{
			Width = sliderWidth
		};
		obj.RenderTexture(texture, new Rectangle(0.0, 0.0, destination2.Width, destination2.Height), (Rectangle)destination2);
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Rectanglei bounds = Bounds;
		fontRenderer.RenderStringWithShadow('▶'.ToString(), (Rectangle)bounds, FontAlignment.Right | FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
		bounds.Right = destination1.X + 12;
		fontRenderer.RenderStringWithShadow('◀'.ToString(), (Rectangle)bounds, FontAlignment.Right | FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
	}

	private void DrawRightSpinner(Renderer renderer, ISpinnerSetting setting)
	{
		string text = $"◀ {_values[setting.SelectedIndex]} ▶";
		renderer.GetFontRenderer().RenderStringWithShadow(text, (Rectangle)Bounds, FontAlignment.Right | FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
	}

	private Colour FontColour => !Highlighted ? Colour.FromOpacity(0.6) : Colours.White;

	private int FontOverlay => !Highlighted ? 0 : 1;
}
