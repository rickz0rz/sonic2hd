using System;
using System.Linq;
using SonicOrca;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// Draws one setting of a setting list menu: the name on the left, and the value (slider or spinner) on the right.
/// </summary>
internal class SettingUI
{
	/// <summary>The width of the filled part of the audio slider for each step from 0 to 10.</summary>
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
	private readonly string _name;
	private readonly string[] _values;

	public Rectanglei Bounds { get; set; }

	public bool Highlighted { get; set; }

	public SettingUI(ISetting setting, ISettingUIResources resources)
	{
		_setting = setting;
		_resources = resources;
		_name = setting.Name;
		if (setting is ISpinnerSetting spinnerSetting)
			_values = spinnerSetting.Values.ToArray();
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
		if (_setting is IAudioSliderSetting audioSliderSetting)
			DrawRightAudioSlider(renderer, audioSliderSetting);
		else if (_setting is ISpinnerSetting spinnerSetting)
			DrawRightSpinner(renderer, spinnerSetting);
	}

	private void DrawRightAudioSlider(Renderer renderer, IAudioSliderSetting setting)
	{
		ITexture sliderEmptyTexture = _resources.AudioSliderEmptyTexture;
		ITexture sliderFillTexture = Highlighted ? _resources.AudioSliderGoldTexture : _resources.AudioSliderSilverTexture;
		Rectanglei sliderBounds = new Rectanglei(0, 0, sliderEmptyTexture.Width, sliderEmptyTexture.Height)
		{
			X = Bounds.Right - sliderEmptyTexture.Width - 24
		};
		sliderBounds.Y = Bounds.Centre.Y - sliderBounds.Height / 2;
		I2dRenderer g = renderer.Get2dRenderer();
		g.RenderTexture(sliderEmptyTexture, (Rectangle)sliderBounds);
		int sliderWidth = SliderWidths[MathX.Clamp(0, (int)Math.Floor(setting.Value * SliderWidths.Length), SliderWidths.Length - 1)];
		Rectanglei fillBounds = sliderBounds with
		{
			Width = sliderWidth
		};
		g.RenderTexture(sliderFillTexture, new Rectangle(0.0, 0.0, fillBounds.Width, fillBounds.Height), (Rectangle)fillBounds);
		// Draw the right arrow at the right edge, and the left arrow just left of the slider.
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Rectanglei bounds = Bounds;
		fontRenderer.RenderStringWithShadow(ArrowRightChar.ToString(), (Rectangle)bounds, FontAlignment.Right | FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
		bounds.Right = sliderBounds.X + 12;
		fontRenderer.RenderStringWithShadow(ArrowLeftChar.ToString(), (Rectangle)bounds, FontAlignment.Right | FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
	}

	private void DrawRightSpinner(Renderer renderer, ISpinnerSetting setting)
	{
		string text = $"{ArrowLeftChar} {_values[setting.SelectedIndex]} {ArrowRightChar}";
		renderer.GetFontRenderer().RenderStringWithShadow(text, (Rectangle)Bounds, FontAlignment.Right | FontAlignment.MiddleY, _resources.Font, FontColour, new int?(FontOverlay));
	}

	private Colour FontColour => Highlighted ? Colours.White : Colour.FromOpacity(0.6);

	private int FontOverlay => Highlighted ? 1 : 0;
}
