using System.Collections.Generic;
using System.Collections.Immutable;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// The data of a setting list menu: its settings and its tag.
/// </summary>
internal class SettingListMenuViewModel : ISettingListMenuViewModel, IMenuViewModel
{
	public ImmutableArray<ISetting> Items { get; }

	public object Tag { get; }

	public SettingListMenuViewModel(IEnumerable<ISetting> items, object tag = null)
	  : this(items.ToImmutableArray(), tag)
	{
	}

	public SettingListMenuViewModel(ImmutableArray<ISetting> items, object tag = null)
	{
		Items = items;
		Tag = tag;
	}
}
