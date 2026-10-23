using System.Collections.Immutable;

namespace S2HD.Menu;

/// <summary>
/// A menu that shows a list of settings that the player changes with left and right.
/// </summary>
internal interface ISettingListMenuViewModel : IMenuViewModel
{
	ImmutableArray<ISetting> Items { get; }
}
