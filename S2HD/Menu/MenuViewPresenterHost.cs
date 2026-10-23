using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace S2HD.Menu;

/// <summary>
/// Shows the current menu page and keeps a stack of the previous pages, so that the player can go back.
/// </summary>
internal class MenuViewPresenterHost
{
	private readonly Stack<IMenuViewModel> _viewModelStack = new Stack<IMenuViewModel>();
	private readonly MenuViewPresenterFactory _viewPresenterFactory;
	private readonly ISettingUIResources _settingUIResources;
	private IMenuViewPresenter _viewPresenter;
	private IMenuViewModel _viewModel;
	private Rectanglei _bounds;

	public event EventHandler NavigateBack;

	public event EventHandler<NavigateNextEventArgs> NavigateNext;

	public Rectanglei Bounds
	{
		get => _bounds;
		set
		{
			_bounds = value;
			if (_viewPresenter == null)
				return;
			_viewPresenter.Bounds = value;
		}
	}

	public object CurrentViewModelTag => _viewModel.Tag;

	public MenuViewPresenterHost(
	  MenuViewPresenterFactory viewPresenterFactory,
	  IMenuViewModel initialViewModel,
	  ISettingUIResources settingUIResources)
	{
		_viewPresenterFactory = viewPresenterFactory;
		_settingUIResources = settingUIResources;
		SetViewModel(initialViewModel);
	}

	public void Update() => _viewPresenter?.Update();

	public void HandleInput() => _viewPresenter?.HandleInput();

	private void SetViewModel(IMenuViewModel viewModel)
	{
		if (_viewModel != null)
			PlayConfirmSound();
		_viewModel = viewModel;
		_viewPresenter = _viewPresenterFactory.Create(_viewModel);
		_viewPresenter.Bounds = _bounds;
		_viewPresenter.NavigateBack += NavigateBackHandler;
		_viewPresenter.NavigateNext += NavigateNextHandler;
		_viewPresenter.Update();
	}

	private void NavigateBackHandler(object sender, EventArgs e)
	{
		if (_viewModelStack.Count == 0)
		{
			NavigateBack?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			SetViewModel(_viewModelStack.Pop());
			// The options screen does not need to know about a step back to a previous page.
			// The pause menu does, because it plays a different animation for each page.
			if (NavigateBack.Target.GetType() == typeof(OptionsGameState))
				return;
			NavigateBack?.Invoke(sender, EventArgs.Empty);
		}
	}

	private void NavigateNextHandler(object s, NavigateNextEventArgs e)
	{
		if (e.ViewModel != null)
		{
			_viewModelStack.Push(_viewModel);
			SetViewModel(e.ViewModel);
		}
		if (e.Tag != null && e.Tag.Equals(MenuTags.BACK))
		{
			NavigateBackHandler(s, e);
		}
		else if (e.ViewModel != null && e.Tag == null)
		{
			// An item without a tag that opens a page sends the tag of that page.
			NavigateNext?.Invoke(s, new NavigateNextEventArgs(e.ViewModel, e.ViewModel.Tag));
		}
		else
		{
			NavigateNext?.Invoke(s, e);
		}
	}

	private void PlayConfirmSound()
	{
		// This checks NavigateSample, but it plays ConfirmSample. The original code does the same.
		if (_settingUIResources.NavigateSample == null)
			return;
		SonicOrcaGameContext.Singleton.Audio.PlaySound(_settingUIResources.ConfirmSample);
	}

	public void Draw(Renderer renderer) => _viewPresenter?.Draw(renderer);
}
