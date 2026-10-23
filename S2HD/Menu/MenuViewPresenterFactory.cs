using System;
using SonicOrca;

namespace S2HD.Menu;

/// <summary>
/// Makes the presenter that shows a given menu page.
/// </summary>
internal class MenuViewPresenterFactory
{
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly ISettingUIResources _resources;

	public MenuViewPresenterFactory(
	  S2HDSonicOrcaGameContext gameContext,
	  ISettingUIResources resources)
	{
		_gameContext = gameContext;
		_resources = resources;
	}

	public IMenuViewPresenter Create(IMenuViewModel viewModel)
	{
		switch (viewModel)
		{
			case IListMenuViewModel listViewModel:
				return new ListMenuViewPresenter(_gameContext, _resources, listViewModel);
			case ISettingListMenuViewModel settingListViewModel:
				return new SettingListMenuViewPresenter(_gameContext, _resources, settingListViewModel);
			default:
				throw new NotSupportedException();
		}
	}
}
