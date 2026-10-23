#nullable disable
namespace S2HD.Menu;

/// <summary>
/// An audio slider setting that keeps its value in memory only. The game does not use it.
/// </summary>
internal class AudioSliderOptionSetting : IAudioSliderSetting, ISetting
{
	public string Name { get; }

	public double Value { get; set; }

	public AudioSliderOptionSetting(string name, double value)
	{
		Name = name;
		Value = value;
	}
}
