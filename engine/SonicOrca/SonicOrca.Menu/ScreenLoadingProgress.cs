namespace SonicOrca.Menu;

public class ScreenLoadingProgress
{
	private readonly Screen _screen;

	public Screen Screen => _screen;

	public int MinimumValue { get; set; }

	public int MaximumValue { get; set; }

	public int CurrentValue { get; set; }

	public string Status { get; set; }

	public ScreenLoadingProgress(Screen screen)
	{
		_screen = screen;
	}
}
