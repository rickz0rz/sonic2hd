namespace S2HD.Menu;

/// <summary>
/// One item of the button bar: a gamepad button icon and a text.
/// </summary>
public struct ButtonBarItem
{
	public GamePadButton Button { get; set; }

	public string Text { get; set; }
}
