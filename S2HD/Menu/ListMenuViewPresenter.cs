using System;
using System.Collections.Immutable;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace S2HD.Menu;

/// <summary>
/// Shows a list menu as a column of text items, and moves the highlight with up and down.
/// The player selects an item with Enter, Start or South, and goes back with Escape, Select or East.
/// </summary>
internal class ListMenuViewPresenter : IMenuViewPresenter
{
	// SDL scancodes of the keys that the menu uses.
	private const int EnterScancode = 40;
	private const int EscapeScancode = 41;
	private const int DownScancode = 81;
	private const int UpScancode = 82;
	/// <summary>The height of one item, in pixels.</summary>
	private const int ItemHeight = 64;

	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly AudioContext _audioContext;
	private readonly ISettingUIResources _resources;
	private readonly ImmutableArray<IMenuItem> _items;
	private readonly IListMenuViewModel _viewModel;
	// True while the gamepad is held down (or up). The highlight moves only on the first frame of a hold,
	// so it moves again only after the player releases the gamepad.
	private bool _downHeld;
	private bool _upHeld;

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
		GamePadInputState gamePad = pressed.GamePad[0];
		Vector2i pov = gamePad.POV;
		Vector2 leftAxis = gamePad.LeftAxis;
		if (input.Pressed.Keyboard[EscapeScancode] || gamePad.Select || gamePad.East)
			OnNavigateBack();
		else if (input.Pressed.Keyboard[EnterScancode] || gamePad.Start || gamePad.South)
			NavigateSelect();
		else if (input.Pressed.Keyboard[UpScancode] || ((pov.Y == -1 || leftAxis.Y <= -0.5) && !_upHeld))
			NavigateUp();
		else if (input.Pressed.Keyboard[DownScancode] || ((pov.Y == 1 || leftAxis.Y >= 0.5) && !_downHeld))
			NavigateDown();

		if (pov.Y == 1 || leftAxis.Y >= 0.5)
			_downHeld = true;
		if (pov.Y == -1 || leftAxis.Y < -0.5)
			_upHeld = true;
		else if (pov.Y == 0 && Math.Abs(leftAxis.Y) < 0.01)
		{
			_downHeld = false;
			_upHeld = false;
		}
	}

	private void OnNavigateBack()
	{
		NavigateBack?.Invoke(this, EventArgs.Empty);
	}

	private void NavigateSelect()
	{
		IMenuItem menuItem = _items[HighlightedIndex];
		NavigateNextEventArgs e = new NavigateNextEventArgs(menuItem.Next, menuItem.Tag);
		NavigateNext?.Invoke(this, e);
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

	public void Draw(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		// Centre the list vertically in the bounds.
		int top = Bounds.Centre.Y - _items.Length * ItemHeight / 2;
		Rectanglei itemBounds = new Rectanglei(Bounds.X, top, Bounds.Width, ItemHeight);
		for (int index = 0; index < _items.Length; ++index)
		{
			IMenuItem menuItem = _items[index];
			int overlay = 0;
			if (index == HighlightedIndex)
			{
				g.BlendMode = BlendMode.Additive;
				g.Colour = new Colour(0.3, 1.0, 1.0, 1.0);
				g.RenderTexture(_resources.SelectionBar, (Rectangle)itemBounds);
				overlay = 1;
			}
			fontRenderer.RenderStringWithShadow(menuItem.Text, (Rectangle)itemBounds, FontAlignment.Centre, _resources.Font, overlay);
			itemBounds.Y += itemBounds.Height;
		}
	}
}
