// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.SettingListMenuViewPresenter
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

internal class SettingListMenuViewPresenter : IMenuViewPresenter
{
	private const int Margin = 16 /*0x10*/;
	private const int ItemHeight = 64 /*0x40*/;
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly AudioContext _audioContext;
	private readonly ISettingUIResources _resources;
	private readonly ImmutableArray<ISetting> _settings;
	private readonly ISettingListMenuViewModel _viewModel;
	private readonly List<SettingUI> _optionSettingUIs = new List<SettingUI>();
	private Rectanglei _bounds;
	private bool _layoutDirty = true;
	private int _selectedIndex;
	private bool _tapUp;
	private bool _tapDown;
	private bool _tapLeft;
	private bool _tapRight;

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
		int x = Bounds.Left + 16 /*0x10*/;
		int width = Bounds.Width - 32 /*0x20*/;
		int y = Bounds.Top + 16 /*0x10*/;
		_optionSettingUIs.Clear();
		foreach (ISetting setting in _settings)
		{
			_optionSettingUIs.Add(new SettingUI(setting, _resources)
			{
				Bounds = new Rectanglei(x, y, width, 64 /*0x40*/),
				Highlighted = false
			});
			y += 64 /*0x40*/;
		}
		if (_optionSettingUIs.Count > 0)
		{
			_selectedIndex = _selectedIndex != -1 ? Math.Min(_selectedIndex, _optionSettingUIs.Count - 1) : 0;
			_optionSettingUIs[_selectedIndex].Highlighted = true;
		}
		else
			_selectedIndex = -1;
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
		GamePadInputState gamePadInputState;
		Vector2i pov;
		Vector2 leftAxis;
		if (!input.Pressed.Keyboard[40])
		{
			gamePadInputState = pressed.GamePad[0];
			if (!gamePadInputState.Start)
			{
				gamePadInputState = pressed.GamePad[0];
				if (!gamePadInputState.South)
				{
					if (!input.Pressed.Keyboard[41])
					{
						gamePadInputState = pressed.GamePad[0];
						if (!gamePadInputState.Select)
						{
							gamePadInputState = pressed.GamePad[0];
							if (!gamePadInputState.East)
							{
								if (!input.Pressed.Keyboard[82])
								{
									gamePadInputState = pressed.GamePad[0];
									pov = gamePadInputState.POV;
									if (pov.Y != -1)
									{
										gamePadInputState = pressed.GamePad[0];
										leftAxis = gamePadInputState.LeftAxis;
										if (leftAxis.Y > -0.5)
											goto label_15;
									}
									if (!_tapDown)
										goto label_14;
								label_15:
									if (!input.Pressed.Keyboard[81])
									{
										gamePadInputState = pressed.GamePad[0];
										pov = gamePadInputState.POV;
										if (pov.Y != 1)
										{
											gamePadInputState = pressed.GamePad[0];
											leftAxis = gamePadInputState.LeftAxis;
											if (leftAxis.Y < 0.5)
												goto label_20;
										}
										if (!_tapUp)
											goto label_19;
									label_20:
										if (!input.Pressed.Keyboard[80 /*0x50*/])
										{
											gamePadInputState = pressed.GamePad[0];
											pov = gamePadInputState.POV;
											if (pov.X != -1)
											{
												gamePadInputState = pressed.GamePad[0];
												leftAxis = gamePadInputState.LeftAxis;
												if (leftAxis.X > -0.5)
													goto label_25;
											}
											if (!_tapLeft)
												goto label_24;
										label_25:
											if (!input.Pressed.Keyboard[79])
											{
												gamePadInputState = pressed.GamePad[0];
												pov = gamePadInputState.POV;
												if (pov.X != 1)
												{
													gamePadInputState = pressed.GamePad[0];
													leftAxis = gamePadInputState.LeftAxis;
													if (leftAxis.X < 0.5)
														goto label_30;
												}
												if (_tapRight)
													goto label_30;
											}
											NavigateRight();
											goto label_30;
										}
									label_24:
										NavigateLeft();
										goto label_30;
									}
								label_19:
									NavigateDown();
									goto label_30;
								}
							label_14:
								NavigateUp();
								goto label_30;
							}
						}
					}
					EventHandler navigateBack = NavigateBack;
					if (navigateBack != null)
					{
						navigateBack(this, EventArgs.Empty);
						goto label_30;
					}
					goto label_30;
				}
			}
		}
		EventHandler<NavigateNextEventArgs> navigateNext = NavigateNext;
		if (navigateNext != null)
			navigateNext(this, new NavigateNextEventArgs(null, 4));
	label_30:
		gamePadInputState = pressed.GamePad[0];
		pov = gamePadInputState.POV;
		if (pov.Y != 1)
		{
			gamePadInputState = pressed.GamePad[0];
			leftAxis = gamePadInputState.LeftAxis;
			if (leftAxis.Y < 0.5)
			{
				gamePadInputState = pressed.GamePad[0];
				pov = gamePadInputState.POV;
				if (pov.Y != -1)
				{
					gamePadInputState = pressed.GamePad[0];
					leftAxis = gamePadInputState.LeftAxis;
					if (leftAxis.Y >= -0.5)
						goto label_36;
				}
				_tapDown = true;
				goto label_36;
			}
		}
		_tapUp = true;
	label_36:
		gamePadInputState = pressed.GamePad[0];
		pov = gamePadInputState.POV;
		if (pov.X != 1)
		{
			gamePadInputState = pressed.GamePad[0];
			leftAxis = gamePadInputState.LeftAxis;
			if (leftAxis.X < 0.5)
			{
				gamePadInputState = pressed.GamePad[0];
				pov = gamePadInputState.POV;
				if (pov.X != -1)
				{
					gamePadInputState = pressed.GamePad[0];
					leftAxis = gamePadInputState.LeftAxis;
					if (leftAxis.X >= -0.5)
						goto label_42;
				}
				_tapLeft = true;
				goto label_42;
			}
		}
		_tapRight = true;
	label_42:
		gamePadInputState = pressed.GamePad[0];
		pov = gamePadInputState.POV;
		if (pov.Y == 0)
		{
			gamePadInputState = pressed.GamePad[0];
			leftAxis = gamePadInputState.LeftAxis;
			if (Math.Abs(leftAxis.Y) < 0.01)
			{
				_tapUp = false;
				_tapDown = false;
			}
		}
		gamePadInputState = pressed.GamePad[0];
		pov = gamePadInputState.POV;
		if (pov.X != 0)
			return;
		gamePadInputState = pressed.GamePad[0];
		leftAxis = gamePadInputState.LeftAxis;
		if (Math.Abs(leftAxis.X) >= 0.01)
			return;
		_tapLeft = false;
		_tapRight = false;
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
			case IAudioSliderSetting _:
				IAudioSliderSetting audioSliderSetting = setting as IAudioSliderSetting;
				if (audioSliderSetting.Value <= 0.0)
					break;
				audioSliderSetting.Value = MathX.Snap(audioSliderSetting.Value - 0.1, 0.1);
				audioSliderSetting.Value = Math.Max(audioSliderSetting.Value, 0.0);
				PlayNavigationSound();
				break;
			case ISpinnerSetting _:
				ISpinnerSetting spinnerSetting = setting as ISpinnerSetting;
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
			case IAudioSliderSetting _:
				IAudioSliderSetting audioSliderSetting = setting as IAudioSliderSetting;
				if (audioSliderSetting.Value >= 1.0)
					break;
				audioSliderSetting.Value = MathX.Snap(audioSliderSetting.Value + 0.1, 0.1);
				audioSliderSetting.Value = Math.Min(audioSliderSetting.Value, 1.0);
				PlayNavigationSound();
				break;
			case ISpinnerSetting _:
				ISpinnerSetting spinnerSetting = setting as ISpinnerSetting;
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
		I2dRenderer obj = renderer.Get2dRenderer();
		if (_selectedIndex != -1)
		{
			Rectanglei destination = _optionSettingUIs[_selectedIndex].Bounds;
			destination = destination.Inflate(new Vector2i(16 /*0x10*/, 0));
			Colour colour = new Colour(0.1, 1.0, 1.0, 1.0);
			obj.BlendMode = BlendMode.Additive;
			obj.RenderQuad(colour, (Rectangle)destination);
		}
		foreach (SettingUI optionSettingUi in _optionSettingUIs)
			optionSettingUi.Draw(renderer);
	}
}
