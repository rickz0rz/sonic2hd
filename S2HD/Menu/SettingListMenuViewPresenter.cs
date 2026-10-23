using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// Shows a setting list menu. Up and down move the highlight, and left and right change the highlighted setting.
/// Enter, Start or South applies the settings, and Escape, Select or East goes back.
/// </summary>
internal class SettingListMenuViewPresenter : IMenuViewPresenter
{
	// SDL scancodes of the keys that the menu uses.
	private const int EnterScancode = 40;
	private const int EscapeScancode = 41;
	private const int RightScancode = 79;
	private const int LeftScancode = 80;
	private const int DownScancode = 81;
	private const int UpScancode = 82;
	private const int Margin = 16;
	private const int ItemHeight = 64;
	/// <summary>The step of an audio slider when the player moves it left or right.</summary>
	private const double AudioSliderStep = 0.1;
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly AudioContext _audioContext;
	private readonly ISettingUIResources _resources;
	private readonly ImmutableArray<ISetting> _settings;
	private readonly ISettingListMenuViewModel _viewModel;
	private readonly List<SettingUI> _optionSettingUIs = new List<SettingUI>();
	private Rectanglei _bounds;
	private bool _layoutDirty = true;
	private int _selectedIndex;
	// True while the gamepad is held in a direction after it moved the selection.
	// The selection moves again in that direction only after the player releases the gamepad.
	private bool _downHeld;
	private bool _upHeld;
	private bool _leftHeld;
	private bool _rightHeld;

	public event EventHandler NavigateBack;

	public event EventHandler<NavigateNextEventArgs> NavigateNext;

	public Rectanglei Bounds
	{
		get => _bounds;
		set
		{
			_bounds = value;
			_layoutDirty = true;
		}
	}

	public SettingListMenuViewPresenter(
	  S2HDSonicOrcaGameContext gameContext,
	  ISettingUIResources resources,
	  ISettingListMenuViewModel viewModel)
	{
		_gameContext = gameContext;
		_audioContext = _gameContext.Audio;
		_resources = resources;
		_viewModel = viewModel;
		_settings = viewModel.Items;
		Layout();
	}

	private void Layout()
	{
		int x = Bounds.Left + Margin;
		int width = Bounds.Width - Margin * 2;
		int y = Bounds.Top + Margin;
		_optionSettingUIs.Clear();
		foreach (ISetting setting in _settings)
		{
			_optionSettingUIs.Add(new SettingUI(setting, _resources)
			{
				Bounds = new Rectanglei(x, y, width, ItemHeight),
				Highlighted = false
			});
			y += ItemHeight;
		}
		if (_optionSettingUIs.Count > 0)
		{
			_selectedIndex = _selectedIndex != -1 ? Math.Min(_selectedIndex, _optionSettingUIs.Count - 1) : 0;
			_optionSettingUIs[_selectedIndex].Highlighted = true;
		}
		else
		{
			_selectedIndex = -1;
		}
		_layoutDirty = false;
	}

	public void Update()
	{
		if (!_layoutDirty)
			return;
		Layout();
	}

	public void HandleInput()
	{
		InputContext input = _gameContext.Input;
		InputState pressed = _gameContext.Input.Pressed;
		GamePadInputState gamePad = pressed.GamePad[0];
		Vector2i pov = gamePad.POV;
		Vector2 leftAxis = gamePad.LeftAxis;
		if (input.Pressed.Keyboard[EnterScancode] || gamePad.Start || gamePad.South)
			NavigateNext?.Invoke(this, new NavigateNextEventArgs(null, MenuTags.APPLY));
		else if (input.Pressed.Keyboard[EscapeScancode] || gamePad.Select || gamePad.East)
			NavigateBack?.Invoke(this, EventArgs.Empty);
		else if (input.Pressed.Keyboard[UpScancode] || ((pov.Y == -1 || leftAxis.Y <= -0.5) && !_upHeld))
			NavigateUp();
		else if (input.Pressed.Keyboard[DownScancode] || ((pov.Y == 1 || leftAxis.Y >= 0.5) && !_downHeld))
			NavigateDown();
		else if (input.Pressed.Keyboard[LeftScancode] || ((pov.X == -1 || leftAxis.X <= -0.5) && !_leftHeld))
			NavigateLeft();
		else if (input.Pressed.Keyboard[RightScancode] || ((pov.X == 1 || leftAxis.X >= 0.5) && !_rightHeld))
			NavigateRight();

		if (pov.Y == 1 || leftAxis.Y >= 0.5)
			_downHeld = true;
		else if (pov.Y == -1 || leftAxis.Y < -0.5)
			_upHeld = true;
		if (pov.X == 1 || leftAxis.X >= 0.5)
			_rightHeld = true;
		else if (pov.X == -1 || leftAxis.X < -0.5)
			_leftHeld = true;
		if (pov.Y == 0 && Math.Abs(leftAxis.Y) < 0.01)
		{
			_downHeld = false;
			_upHeld = false;
		}
		if (pov.X == 0 && Math.Abs(leftAxis.X) < 0.01)
		{
			_leftHeld = false;
			_rightHeld = false;
		}
	}

	public void NavigateUp()
	{
		if (_selectedIndex <= 0)
			return;
		_optionSettingUIs[_selectedIndex].Highlighted = false;
		--_selectedIndex;
		_optionSettingUIs[_selectedIndex].Highlighted = true;
		PlayNavigationSound();
	}

	public void NavigateDown()
	{
		if (_selectedIndex >= _optionSettingUIs.Count - 1)
			return;
		_optionSettingUIs[_selectedIndex].Highlighted = false;
		++_selectedIndex;
		_optionSettingUIs[_selectedIndex].Highlighted = true;
		PlayNavigationSound();
	}

	private void PlayNavigationSound()
	{
		if (_resources.NavigateSample == null)
			return;
		_audioContext.PlaySound(_resources.NavigateSample);
	}

	private void NavigateLeft()
	{
		if (_selectedIndex == -1)
			return;
		ISetting setting = _settings[_selectedIndex];
		switch (setting)
		{
			case IAudioSliderSetting audioSliderSetting:
				if (audioSliderSetting.Value <= 0.0)
					break;
				audioSliderSetting.Value = MathX.Snap(audioSliderSetting.Value - AudioSliderStep, AudioSliderStep);
				audioSliderSetting.Value = Math.Max(audioSliderSetting.Value, 0.0);
				PlayNavigationSound();
				break;
			case ISpinnerSetting spinnerSetting:
				if (spinnerSetting.SelectedIndex <= 0)
					break;
				--spinnerSetting.SelectedIndex;
				PlayNavigationSound();
				break;
		}
	}

	private void NavigateRight()
	{
		if (_selectedIndex == -1)
			return;
		ISetting setting = _settings[_selectedIndex];
		switch (setting)
		{
			case IAudioSliderSetting audioSliderSetting:
				if (audioSliderSetting.Value >= 1.0)
					break;
				audioSliderSetting.Value = MathX.Snap(audioSliderSetting.Value + AudioSliderStep, AudioSliderStep);
				audioSliderSetting.Value = Math.Min(audioSliderSetting.Value, 1.0);
				PlayNavigationSound();
				break;
			case ISpinnerSetting spinnerSetting:
				int count = spinnerSetting.Values.Count;
				if (spinnerSetting.SelectedIndex >= count - 1)
					break;
				++spinnerSetting.SelectedIndex;
				PlayNavigationSound();
				break;
		}
	}

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		if (_selectedIndex != -1)
		{
			// Draw a light highlight behind the selected setting.
			Rectanglei destination = _optionSettingUIs[_selectedIndex].Bounds;
			destination = destination.Inflate(new Vector2i(Margin, 0));
			Colour colour = new Colour(0.1, 1.0, 1.0, 1.0);
			g.BlendMode = BlendMode.Additive;
			g.RenderQuad(colour, (Rectangle)destination);
		}
		foreach (SettingUI settingUI in _optionSettingUIs)
			settingUI.Draw(renderer);
	}
}
