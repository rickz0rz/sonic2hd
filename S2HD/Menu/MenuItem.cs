namespace S2HD.Menu;

/// <summary>
/// One item of a list menu.
/// </summary>
internal class MenuItem : IMenuItem
{
	public string Text { get; }

	public IMenuViewModel Next { get; }

	public object Tag { get; }

	public MenuItem(string text, IMenuViewModel next = null, object tag = null)
	{
		Text = text;
		Next = next;
		Tag = tag;
	}

	public override string ToString() => Text;
}
