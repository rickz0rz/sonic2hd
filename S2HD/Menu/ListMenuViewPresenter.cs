// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.ListMenuViewPresenter
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Immutable;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;

#nullable disable
namespace S2HD.Menu;

internal class ListMenuViewPresenter : IMenuViewPresenter
{
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly AudioContext _audioContext;
	private readonly ISettingUIResources _resources;
	private readonly ImmutableArray<IMenuItem> _items;
	private readonly IListMenuViewModel _viewModel;
	private bool _tapUp;
	private bool _tapDown;

	public int HighlightedIndex
	{
		get => _viewModel.HighlightedIndex;
		set => _viewModel.HighlightedIndex = value;
	}

	public Rectanglei Bounds { get; set; }

	public event EventHandler NavigateBack;

	public event EventHandler<NavigateNextEventArgs> NavigateNext;

	public ListMenuViewPresenter(
	  S2HDSonicOrcaGameContext gameContext,
	  ISettingUIResources resources,
	  IListMenuViewModel viewModel)
	{
		_gameContext = gameContext;
		_audioContext = gameContext.Audio;
		_resources = resources;
		_items = viewModel.Items;
		_viewModel = viewModel;
	}

	public void Update()
	{
	}

	public void HandleInput()
	{
		InputContext input = _gameContext.Input;
		InputState pressed = _gameContext.Input.Pressed;
		GamePadInputState gamePadInputState;
		Vector2i pov;
		Vector2 leftAxis;
		if (!input.Pressed.Keyboard[41])
		{
			gamePadInputState = pressed.GamePad[0];
			if (!gamePadInputState.Select)
			{
				gamePadInputState = pressed.GamePad[0];
				if (!gamePadInputState.East)
				{
					if (!input.Pressed.Keyboard[40])
					{
						gamePadInputState = pressed.GamePad[0];
						if (!gamePadInputState.Start)
						{
							gamePadInputState = pressed.GamePad[0];
							if (!gamePadInputState.South)
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
											goto label_13;
									}
									if (!_tapDown)
										goto label_12;
								label_13:
									if (!input.Pressed.Keyboard[81])
									{
										gamePadInputState = pressed.GamePad[0];
										pov = gamePadInputState.POV;
										if (pov.Y != 1)
										{
											gamePadInputState = pressed.GamePad[0];
											leftAxis = gamePadInputState.LeftAxis;
											if (leftAxis.Y < 0.5)
												goto label_18;
										}
										if (_tapUp)
											goto label_18;
									}
									NavigateDown();
									goto label_18;
								}
							label_12:
								NavigateUp();
								goto label_18;
							}
						}
					}
					NavigateSelect();
					goto label_18;
				}
			}
		}
		NavigateBack2();
	label_18:
		gamePadInputState = pressed.GamePad[0];
		pov = gamePadInputState.POV;
		if (pov.Y != 1)
		{
			gamePadInputState = pressed.GamePad[0];
			leftAxis = gamePadInputState.LeftAxis;
			if (leftAxis.Y < 0.5)
				goto label_21;
		}
		_tapUp = true;
	label_21:
		gamePadInputState = pressed.GamePad[0];
		pov = gamePadInputState.POV;
		if (pov.Y != -1)
		{
			gamePadInputState = pressed.GamePad[0];
			leftAxis = gamePadInputState.LeftAxis;
			if (leftAxis.Y >= -0.5)
			{
				gamePadInputState = pressed.GamePad[0];
				pov = gamePadInputState.POV;
				if (pov.Y != 0)
					return;
				gamePadInputState = pressed.GamePad[0];
				leftAxis = gamePadInputState.LeftAxis;
				if (Math.Abs(leftAxis.Y) >= 0.01)
					return;
				_tapUp = false;
				_tapDown = false;
				return;
			}
		}
		_tapDown = true;
	}

	private void NavigateBack2()
	{
		EventHandler navigateBack = NavigateBack;
		if (navigateBack == null)
			return;
		navigateBack(this, EventArgs.Empty);
	}

	private void NavigateSelect()
	{
		IMenuItem menuItem = _items[HighlightedIndex];
		NavigateNextEventArgs e = new NavigateNextEventArgs(menuItem.Next, menuItem.Tag);
		EventHandler<NavigateNextEventArgs> navigateNext = NavigateNext;
		if (navigateNext == null)
			return;
		navigateNext(this, e);
	}

	private void NavigateUp()
	{
		if (HighlightedIndex <= 0)
			return;
		--HighlightedIndex;
		PlayNavigationSound();
	}

	private void NavigateDown()
	{
		if (HighlightedIndex >= _items.Length - 1)
			return;
		++HighlightedIndex;
		PlayNavigationSound();
	}

	private void PlayNavigationSound()
	{
		if (_resources.NavigateSample == null)
			return;
		_audioContext.PlaySound(_resources.NavigateSample);
	}

	private void PlayConfirmSound()
	{
		if (_resources.NavigateSample == null)
			return;
		_audioContext.PlaySound(_resources.NavigateSample);
	}

	public void Draw(Renderer renderer)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		int num = Bounds.Centre.Y - _items.Length * 64 /*0x40*/ / 2;
		Rectanglei bounds1 = Bounds;
		int x1 = bounds1.X;
		int y1 = num;
		bounds1 = Bounds;
		int width1 = bounds1.Width;
		Rectanglei rectanglei1 = new Rectanglei(x1, y1, width1, 64 /*0x40*/);
		Rectanglei rectanglei2;
		Rectanglei bounds2 = Bounds;
		int x2 = bounds2.X;
		int y2 = num;
		bounds2 = Bounds;
		int width2 = bounds2.Width;
		rectanglei2 = new Rectanglei(x2, y2, width2, 64 /*0x40*/);
		for (int index = 0; index < _items.Length; ++index)
		{
			IMenuItem menuItem = _items[index];
			int overlay = 0;
			if (index == HighlightedIndex)
			{
				obj.BlendMode = BlendMode.Additive;
				obj.Colour = new Colour(0.3, 1.0, 1.0, 1.0);
				obj.RenderTexture(_resources.SelectionBar, (Rectangle)rectanglei2);
				overlay = 1;
			}
			fontRenderer.RenderStringWithShadow(menuItem.Text, (Rectangle)rectanglei2, FontAlignment.Centre, _resources.Font, overlay);
			rectanglei2.Y += rectanglei2.Height;
		}
	}
}
