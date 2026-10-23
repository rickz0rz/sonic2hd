namespace S2HD.Menu;

/// <summary>
/// One item of a list menu. Selecting it opens <see cref="Next"/> (if set) and sends <see cref="Tag"/> to the menu host.
/// </summary>
internal interface IMenuItem
{
	string Text { get; }

	IMenuViewModel Next { get; }

	object Tag { get; }
}
