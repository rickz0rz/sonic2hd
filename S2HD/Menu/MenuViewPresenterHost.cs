// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.MenuViewPresenterHost
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

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
		_viewPresenter.NavigateBack += new EventHandler(NavigateBackHandler);
		_viewPresenter.NavigateNext += (s, e) => NavigateNextHandler(s, e);
		_viewPresenter.Update();
	}

	private void NavigateBackHandler(object sender, EventArgs e)
	{
		if (_viewModelStack.Count == 0)
		{
			EventHandler navigateBack = NavigateBack;
			if (navigateBack == null)
				return;
			navigateBack(this, EventArgs.Empty);
		}
		else
		{
			SetViewModel(_viewModelStack.Pop());
			if (!(NavigateBack.Target.GetType() != typeof(OptionsGameState)))
				return;
			EventHandler navigateBack = NavigateBack;
			if (navigateBack == null)
				return;
			navigateBack(sender, EventArgs.Empty);
		}
	}

	private void NavigateNextHandler(object s, NavigateNextEventArgs e)
	{
		if (e.ViewModel != null)
		{
			_viewModelStack.Push(_viewModel);
			SetViewModel(e.ViewModel);
		}
		if (e.Tag != null && e.Tag.Equals(0))
			NavigateBackHandler(s, e);
		else if (e.ViewModel != null && e.Tag == null)
		{
			EventHandler<NavigateNextEventArgs> navigateNext = NavigateNext;
			if (navigateNext == null)
				return;
			navigateNext(s, new NavigateNextEventArgs(e.ViewModel, e.ViewModel.Tag));
		}
		else
		{
			EventHandler<NavigateNextEventArgs> navigateNext = NavigateNext;
			if (navigateNext == null)
				return;
			navigateNext(s, e);
		}
	}

	private void PlayNavigationSound()
	{
		if (_settingUIResources.NavigateSample == null)
			return;
		SonicOrcaGameContext.Singleton.Audio.PlaySound(_settingUIResources.NavigateSample);
	}

	private void PlayConfirmSound()
	{
		if (_settingUIResources.NavigateSample == null)
			return;
		SonicOrcaGameContext.Singleton.Audio.PlaySound(_settingUIResources.ConfirmSample);
	}

	private void PlayBackSound()
	{
		if (_settingUIResources.NavigateSample == null)
			return;
		SonicOrcaGameContext.Singleton.Audio.PlaySound(_settingUIResources.CancelSample);
	}

	public void Draw(Renderer renderer) => _viewPresenter?.Draw(renderer);
}
