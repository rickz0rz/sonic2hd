namespace S2HD.Menu;

/// <summary>
/// A setting with a value from 0 to 1 that the menu shows as an audio slider.
/// </summary>
internal interface IAudioSliderSetting : ISetting
{
	double Value { get; set; }
}
