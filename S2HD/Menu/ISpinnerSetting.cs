using System.Collections.Generic;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// A setting with a fixed list of values. The player goes through the values with left and right.
/// </summary>
internal interface ISpinnerSetting : ISetting
{
	IReadOnlyList<string> Values { get; }

	int SelectedIndex { get; set; }
}
