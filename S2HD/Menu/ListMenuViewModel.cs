using System.Collections.Generic;
using System.Collections.Immutable;

namespace S2HD.Menu;

/// <summary>
/// The data of a list menu: its items, its tag and the item that is highlighted.
/// </summary>
internal class ListMenuViewModel : IListMenuViewModel, IMenuViewModel
{
	public ImmutableArray<IMenuItem> Items { get; }

	public object Tag { get; }

	public int HighlightedIndex { get; set; }

	public ListMenuViewModel(IEnumerable<IMenuItem> items, object tag = null)
	  : this(items.ToImmutableArray(), tag)
	{
	}

	public ListMenuViewModel(ImmutableArray<IMenuItem> items, object tag = null)
	{
		Items = items;
		Tag = tag;
	}

	public override string ToString()
	{
		return string.Join<IMenuItem>(", ", Items);
	}
}
