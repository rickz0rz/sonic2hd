using System.Collections.Immutable;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// A menu that shows a list of items. The player selects one item to go to the next menu or to do an action.
/// </summary>
internal interface IListMenuViewModel : IMenuViewModel
{
	ImmutableArray<IMenuItem> Items { get; }

	int HighlightedIndex { get; set; }
}
