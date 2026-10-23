using System;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// The data of a "navigate next" event: the menu page to open (or null) and the tag of the selected item.
/// </summary>
internal class NavigateNextEventArgs : EventArgs
{
	public IMenuViewModel ViewModel { get; }

	public object Tag { get; }

	public NavigateNextEventArgs(IMenuViewModel viewModel, object tag)
	{
		ViewModel = viewModel;
		Tag = tag;
	}
}
