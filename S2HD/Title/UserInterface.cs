using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

#nullable disable
namespace S2HD.Title;

/// <summary>
/// The menus of the title screen: "PRESS START", the main menu (new game, options, quit), the character
/// selection, and the hidden level select. It also starts the demo when the player waits too long.
/// </summary>
internal class UserInterface
{
	/// <summary>SDL scancode of the Escape key.</summary>
	private const int EscapeScancode = 41;

	// The inputs of the level select code. The code is up, down, left, right, and then Start while Action1 is held.
	private const int LevelSelectInputNone = -1;
	private const int LevelSelectInputUp = 0;
	private const int LevelSelectInputDown = 1;
	private const int LevelSelectInputLeft = 2;
	private const int LevelSelectInputRight = 3;
	private const int LevelSelectInputAction1 = 4;
	private const int LevelSelectInputAction2 = 5;
	private const int LevelSelectInputAction3 = 6;
	private const int LevelSelectInputStart = 7;

	// The values of _characterSelectionIndex.
	private const int SonicAndTailsSelection = 0;
	private const int SonicSelection = 1;
	private const int TailsSelection = 2;
	private const int CharacterSelectionCount = 3;

	/// <summary>The Y position of the menu items and of "PRESS START".</summary>
	private const int MenuY = 900;
	/// <summary>The horizontal distance between two menu items.</summary>
	private const int MenuItemSpacing = 400;

	private readonly EffectEventManager _effectEventManager = new EffectEventManager();
	private readonly S2HDSonicOrcaGameContext _gameContext;
	private readonly TitleGameState _titleGameState;
	private readonly IMaskRenderer _maskRenderer;
	[ResourcePath("SONICORCA/TITLE/SELECTIONMARKER")]
	private ITexture _textureSelectionMarker;
	[ResourcePath("SONICORCA/TITLE/ZIGZAG")]
	private ITexture _textureZigZag;
	[ResourcePath("SONICORCA/MENU/LEFT")]
	private ITexture _textureLeftArrow;
	[ResourcePath("SONICORCA/MENU/RIGHT")]
	private ITexture _textureRightArrow;
	[ResourcePath("SONICORCA/FONTS/IMPACT/REGULAR")]
	private Font _fontImpactRegular;
	[ResourcePath("SONICORCA/FONTS/IMPACT/ITALIC")]
	private Font _fontImpactItalic;
	[ResourcePath("SONICORCA/SOUND/NAVIGATE/CURSOR")]
	private Sample _sampleNavigateCursor;
	[ResourcePath("SONICORCA/SOUND/NAVIGATE/BACK")]
	private Sample _sampleNavigateBack;
	[ResourcePath("SONICORCA/SOUND/NAVIGATE/YES")]
	private Sample _sampleNavigateYes;
	private int _ticks;
	private bool _pressStartActive;
	private int _selectionIndex;
	private bool _busy;
	private Vector2 _pressStartScale;
	private double _pressStartOpacity;
	private double _pressStartWhiteAdditive;
	private double _textOpacity;
	private double _textWhiteAdditive;
	private readonly Vector2[] _selectedMenuItemMarkerPositions = new Vector2[2];
	private int _levelSelectInputState;
	private bool _levelSelectEnabled;
	private int _levelSelectSelectionIndex;
	private LevelSelectItem[] _levelSelectItems;
	private MenuItem[] _menuItems;
	private MenuItemWidget[] _menuItemWidgets;
	private AnimationInstance _miniSonicAniInstance;
	private AnimationInstance _miniTailsAniInstance;
	private int _characterSelectionIndex;
	private bool _characterSelectActive;
	private double _characterSelectOpacity;
	/// <summary>The time, in ticks, that the player can wait before the demo starts.</summary>
	private const int DemoInitialTimeout = 720;
	private int? _demoTimeout;
	/// <summary>The time, in ticks, between the character selection and the start of the game.</summary>
	private const int CharacterSelectTime = 60;
	private int? _characterSelectTimer;
	private bool _characterSelected;
	private static readonly EaseTimeline PressStartScaleXTimeline = new EaseTimeline(new EaseTimeline.Entry[3]
	{
		new EaseTimeline.Entry(5, 1.0),
		new EaseTimeline.Entry(10, 1.2),
		new EaseTimeline.Entry(20, 0.0)
	});
	private static readonly EaseTimeline PressStartScaleYTimeline = new EaseTimeline(new EaseTimeline.Entry[4]
	{
		new EaseTimeline.Entry(0, 1.0),
		new EaseTimeline.Entry(5, 1.45),
		new EaseTimeline.Entry(10, 1.0),
		new EaseTimeline.Entry(20, 0.0)
	});
	private static readonly EaseTimeline PressStartWhiteAdditiveTimeline = new EaseTimeline(new EaseTimeline.Entry[2]
	{
		new EaseTimeline.Entry(0, 0.0),
		new EaseTimeline.Entry(10, 1.0)
	});
	private static readonly EaseTimeline PressStartOpacityTimeline = new EaseTimeline(new EaseTimeline.Entry[2]
	{
		new EaseTimeline.Entry(10, 1.0),
		new EaseTimeline.Entry(20, 0.0)
	});
	private static readonly EaseTimeline TextOpacityTimeline = new EaseTimeline(new EaseTimeline.Entry[2]
	{
		new EaseTimeline.Entry(20, 0.0),
		new EaseTimeline.Entry(30, 1.0)
	});
	private static readonly EaseTimeline TextWhiteAdditiveTimeline = new EaseTimeline(new EaseTimeline.Entry[2]
	{
		new EaseTimeline.Entry(20, 1.0),
		new EaseTimeline.Entry(30, 0.0)
	});
	private static readonly EaseTimeline MenuItemOpacityEaseTimeline = new EaseTimeline(new EaseTimeline.Entry[5]
	{
		new EaseTimeline.Entry(260, 0.0),
		new EaseTimeline.Entry(560, 0.5),
		new EaseTimeline.Entry(960, 1.0),
		new EaseTimeline.Entry(1360, 0.5),
		new EaseTimeline.Entry(1660, 0.0)
	});
	private static readonly EaseTimeline ActivatedTextScaleTimeline = new EaseTimeline(new EaseTimeline.Entry[3]
	{
		new EaseTimeline.Entry(0, 1.0),
		new EaseTimeline.Entry(15, 0.8),
		new EaseTimeline.Entry(30, 1.4)
	});
	private static readonly EaseTimeline ActivatedTextOpacityTimeline = new EaseTimeline(new EaseTimeline.Entry[2]
	{
		new EaseTimeline.Entry(15, 1.0),
		new EaseTimeline.Entry(30, 0.0)
	});

	public bool Visible { get; set; }

	private bool IsSonicActive
	{
		get => _characterSelectionIndex == SonicAndTailsSelection || _characterSelectionIndex == SonicSelection;
	}

	private bool IsTailsActive
	{
		get => _characterSelectionIndex == SonicAndTailsSelection || _characterSelectionIndex == TailsSelection;
	}

	public UserInterface(
	  S2HDSonicOrcaGameContext gameContext,
	  TitleGameState titleGameState,
	  IMaskRenderer maskRenderer)
	{
		_gameContext = gameContext;
		_titleGameState = titleGameState;
		_maskRenderer = maskRenderer;
		ResourceTree resourceTree = _gameContext.ResourceTree;
		resourceTree.FullfillLoadedResourcesByAttribute(this);
		_miniSonicAniInstance = new AnimationInstance(resourceTree, TitleResources.AnimationGroupResourceKey, TitleResources.MiniSonicIdleAnimationIndex);
		_miniTailsAniInstance = new AnimationInstance(resourceTree, TitleResources.AnimationGroupResourceKey, TitleResources.MiniTailsIdleAnimationIndex);
		InitialiseLevelSelect();
	}

	public void Reset()
	{
		_ticks = 0;
		_pressStartActive = true;
		_pressStartScale = new Vector2(1.0);
		_pressStartOpacity = 1.0;
		_pressStartWhiteAdditive = 0.0;
		_textWhiteAdditive = 0.0;
		_selectionIndex = 0;
		_levelSelectInputState = 0;
		_levelSelectEnabled = false;
		_levelSelectSelectionIndex = 0;
		_effectEventManager.Clear();
		_demoTimeout = DemoInitialTimeout;
		_characterSelectTimer = CharacterSelectTime;
		InitialiseMenuItemWidgets();
	}

	public void Update()
	{
		if (!Visible)
			return;
		if (_characterSelectActive)
		{
			_miniSonicAniInstance.Animate();
			_miniTailsAniInstance.Animate();
			_characterSelectOpacity = MathX.GoTowards(_characterSelectOpacity, 1.0, 1.0 / 15.0);
			_textOpacity = 1.0 - _characterSelectOpacity;
		}
		else
		{
			_characterSelectOpacity = MathX.GoTowards(_characterSelectOpacity, 0.0, 1.0 / 15.0);
			_textOpacity = 1.0 - _characterSelectOpacity;
		}
		_effectEventManager.Update();
		HandleInput();
		++_ticks;
		if (_demoTimeout.HasValue)
		{
			_demoTimeout--;
			if (_demoTimeout <= 0)
			{
				_demoTimeout = null;
				StartDemo();
			}
		}
		if (!_characterSelected)
			return;
		_characterSelectTimer--;
		if (_characterSelectTimer != 0)
			return;
		OnSelectCharacter();
	}

	private void HandleInput()
	{
		Controller pressed = _gameContext.Pressed[0];
		InputState pressedInputState = _gameContext.Input.Pressed;
		if (_busy)
			return;
		if (!_levelSelectEnabled)
		{
			// Check for the level select code. _levelSelectInputState counts the correct inputs.
			int levelSelectInput = GetLevelSelectControllerInput(pressed);
			if (levelSelectInput != LevelSelectInputNone)
			{
				if (_levelSelectInputState <= LevelSelectInputRight)
				{
					// The first four inputs are up, down, left and right, which have the values 0 to 3.
					if (_levelSelectInputState != levelSelectInput)
						_levelSelectInputState = 0;
					else
						++_levelSelectInputState;
				}
				else if (levelSelectInput == LevelSelectInputAction1 || levelSelectInput == LevelSelectInputStart)
				{
					if (_gameContext.Current[0].Action1 && levelSelectInput == LevelSelectInputStart)
					{
						_levelSelectInputState = 0;
						_levelSelectEnabled = true;
						_gameContext.Audio.PlaySound(_sampleNavigateYes);
						return;
					}
				}
				else
				{
					_levelSelectInputState = 0;
				}
			}
		}
		if (_levelSelectEnabled)
		{
			if (Math.Abs(pressed.DirectionLeft.Y) >= 0.5)
			{
				if (Math.Sign(pressed.DirectionLeft.Y) < 0)
				{
					_levelSelectSelectionIndex = NegMod(_levelSelectSelectionIndex - 1, _levelSelectItems.Length);
					_gameContext.Audio.PlaySound(_sampleNavigateCursor);
				}
				else
				{
					_levelSelectSelectionIndex = NegMod(_levelSelectSelectionIndex + 1, _levelSelectItems.Length);
					_gameContext.Audio.PlaySound(_sampleNavigateCursor);
				}
			}
			if (pressed.Start)
			{
				_gameContext.Audio.PlaySound(_sampleNavigateYes);
				OnLevelSelectStart();
			}
			if (!BackPressed(pressedInputState))
				return;
			_gameContext.Audio.PlaySound(_sampleNavigateBack);
			_levelSelectEnabled = false;
		}
		else if (_pressStartActive)
		{
			if (!pressed.Start)
				return;
			_effectEventManager.BeginEvent(EffectPressStart());
			_pressStartActive = false;
			_demoTimeout = null;
			_gameContext.Audio.PlaySound(_sampleNavigateYes);
		}
		else if (_characterSelectActive)
		{
			if (Math.Abs(pressed.DirectionLeft.X) >= 0.5)
			{
				_characterSelectionIndex = Math.Sign(pressed.DirectionLeft.X) >= 0 ? (_characterSelectionIndex + 1) % CharacterSelectionCount : (_characterSelectionIndex - 1 + CharacterSelectionCount) % CharacterSelectionCount;
				_miniSonicAniInstance.Index = TitleResources.MiniSonicIdleAnimationIndex;
				_miniTailsAniInstance.Index = TitleResources.MiniTailsIdleAnimationIndex;
				_gameContext.Audio.PlaySound(_sampleNavigateCursor);
			}
			if (BackPressed(pressedInputState))
			{
				_characterSelectActive = false;
				_gameContext.Audio.PlaySound(_sampleNavigateBack);
			}
			else
			{
				if (!pressed.Start)
					return;
				_gameContext.Audio.PlaySound(_sampleNavigateYes);
				_miniSonicAniInstance.Index = IsSonicActive ? TitleResources.MiniSonicReadyAnimationIndex : TitleResources.MiniSonicIdleAnimationIndex;
				_miniTailsAniInstance.Index = IsTailsActive ? TitleResources.MiniTailsReadyAnimationIndex : TitleResources.MiniTailsIdleAnimationIndex;
				_characterSelected = true;
			}
		}
		else
		{
			if (BackPressed(pressedInputState))
			{
				_demoTimeout = DemoInitialTimeout;
				_pressStartActive = true;
				_pressStartOpacity = 1.0;
				_pressStartScale = new Vector2(1.0);
				_pressStartWhiteAdditive = 0.0;
				_selectionIndex = 0;
				InitialiseMenuItemWidgets();
				_gameContext.Audio.PlaySound(_sampleNavigateBack);
			}
			if (Math.Abs(pressed.DirectionLeft.X) >= 0.5)
			{
				int direction = Math.Sign(pressed.DirectionLeft.X);
				_selectionIndex = direction >= 0 ? (_selectionIndex + 1) % _menuItems.Length : (_selectionIndex - 1 + _menuItems.Length) % _menuItems.Length;
				_effectEventManager.BeginEvent(EffectNavigateMenu(direction));
				_gameContext.Audio.PlaySound(_sampleNavigateCursor);
			}
			if (!pressed.Start)
				return;
			_menuItems[_selectionIndex].Action?.Invoke();
			_gameContext.Audio.PlaySound(_sampleNavigateYes);
		}
	}

	private static bool BackPressed(InputState inputState)
	{
		GamePadInputState gamePadInputState = inputState.GamePad[0];
		return inputState.Keyboard[EscapeScancode] || gamePadInputState.Select || gamePadInputState.East;
	}

	private int GetLevelSelectControllerInput(Controller pressed)
	{
		if (pressed.DirectionLeft.Y <= -0.5)
			return LevelSelectInputUp;
		if (pressed.DirectionLeft.Y >= 0.5)
			return LevelSelectInputDown;
		if (pressed.DirectionLeft.X <= -0.5)
			return LevelSelectInputLeft;
		if (pressed.DirectionLeft.X >= 0.5)
			return LevelSelectInputRight;
		if (pressed.Action1)
			return LevelSelectInputAction1;
		if (pressed.Action2)
			return LevelSelectInputAction2;
		if (pressed.Action3)
			return LevelSelectInputAction3;
		return pressed.Start ? LevelSelectInputStart : LevelSelectInputNone;
	}

	private void InitialiseMenuItemWidgets()
	{
		_menuItems = new MenuItem[3]
		{
			new MenuItem()
			{
				Text = "NEW GAME",
				Action = OnSelectNewGame
			},
			new MenuItem()
			{
				Text = "OPTIONS",
				Action = OnSelectOptions
			},
			new MenuItem()
			{
				Text = "QUIT",
				Action = OnSelectQuit
			}
		};
		// The menu is a carousel of 5 widgets, from 2 places left of the selection to 2 places right of it.
		// The menu items repeat, so one item can have more than one widget.
		_menuItemWidgets = new MenuItemWidget[5];
		for (int offset = -2; offset <= 2; ++offset)
		{
			int menuItemIndex = (_selectionIndex + offset + _menuItems.Length) % _menuItems.Length;
			MenuItemWidget menuItemWidget = new MenuItemWidget()
			{
				MenuItemIndex = menuItemIndex,
				OriginOffset = offset,
				X = 960 + MenuItemSpacing * offset,
				Scale = new Vector2(1.0)
			};
			menuItemWidget.Opacity = (float)MenuItemOpacityEaseTimeline.GetValueAt((int)menuItemWidget.X);
			_menuItemWidgets[offset + 2] = menuItemWidget;
		}
		SetSelectionMarkerPositions();
	}

	private void SetSelectionMarkerPositions()
	{
		MenuItemWidget menuItemWidget = _menuItemWidgets.First(x => x.OriginOffset == 0);
		int markerOffset = GetMarkerOffset(_selectionIndex);
		_selectedMenuItemMarkerPositions[0] = new Vector2((double)menuItemWidget.X - markerOffset, MenuY);
		_selectedMenuItemMarkerPositions[1] = new Vector2((double)menuItemWidget.X + markerOffset, MenuY);
	}

	private int GetMenuItemWidth(int index)
	{
		return (int)_fontImpactRegular.MeasureString(_menuItems[index].Text).Width;
	}

	/// <summary>The horizontal distance from the centre of a menu item to each of its selection markers.</summary>
	private int GetMarkerOffset(int index) => GetMenuItemWidth(index) / 2 + 48;

	private void InitialiseLevelSelect()
	{
		List<LevelSelectItem> levelSelectItemList = new List<LevelSelectItem>();
		int levelNumber = 1;
		foreach (LevelInfo level in Levels.LevelList)
		{
			if (!level.Unreleased)
			{
				for (int act = 1; act <= level.Acts; ++act)
				{
					string text = $"{level.Name} ACT {act}".ToUpper();
					levelSelectItemList.Add(new LevelSelectItem()
					{
						Text = text,
						Mnemonic = level.Mnemonic,
						Act = act,
						Number = levelNumber
					});
				}
				++levelNumber;
			}
		}
		_levelSelectItems = levelSelectItemList.ToArray();
	}

	private void OnSelectNewGame() => _characterSelectActive = true;

	private void OnSelectCharacter()
	{
		_busy = true;
		_effectEventManager.BeginEvent(EffectFadeOut());
		_titleGameState.Result = TitleGameState.ResultType.NewGame;
		LevelPrepareSettings lps = new LevelPrepareSettings()
		{
			AreaResourceKey = Levels.GetAreaResourceKey("ehz"),
			Act = 1,
			LevelNumber = 1
		};
		ApplyCharacterSelection(lps);
		_titleGameState.LevelPrepareSettings = lps;
	}

	private void OnSelectOptions()
	{
		_busy = true;
		_effectEventManager.BeginEvent(EffectFadeOut());
		_titleGameState.Result = TitleGameState.ResultType.ShowOptions;
	}

	private void OnSelectQuit()
	{
		_busy = true;
		_effectEventManager.BeginEvent(EffectFadeOut());
		_titleGameState.Result = TitleGameState.ResultType.Quit;
	}

	private void StartDemo()
	{
		_busy = true;
		_effectEventManager.BeginEvent(EffectFadeOut());
		_titleGameState.Result = TitleGameState.ResultType.StartDemo;
	}

	private void OnLevelSelectStart()
	{
		_busy = true;
		LevelSelectItem levelSelectItem = _levelSelectItems[_levelSelectSelectionIndex];
		LevelPrepareSettings lps = new LevelPrepareSettings()
		{
			AreaResourceKey = Levels.GetAreaResourceKey(levelSelectItem.Mnemonic),
			Act = levelSelectItem.Act,
			LevelNumber = levelSelectItem.Number
		};
		ApplyCharacterSelection(lps);
		_titleGameState.LevelPrepareSettings = lps;
		_titleGameState.FadeOut();
		_titleGameState.Result = TitleGameState.ResultType.LevelSelect;
	}

	private void ApplyCharacterSelection(LevelPrepareSettings lps)
	{
		switch (_characterSelectionIndex)
		{
			case SonicAndTailsSelection:
				lps.ProtagonistCharacter = CharacterType.Sonic;
				lps.SidekickCharacter = CharacterType.Tails;
				break;
			case SonicSelection:
				lps.ProtagonistCharacter = CharacterType.Sonic;
				lps.SidekickCharacter = CharacterType.Null;
				break;
			case TailsSelection:
				lps.ProtagonistCharacter = CharacterType.Tails;
				lps.SidekickCharacter = CharacterType.Null;
				break;
		}
	}

	private IEnumerable<UpdateResult> EffectPressStart()
	{
		_busy = true;
		for (int t = 0; t <= 50; ++t)
		{
			_pressStartScale = new Vector2(PressStartScaleXTimeline.GetValueAt(t), PressStartScaleYTimeline.GetValueAt(t));
			_pressStartWhiteAdditive = PressStartWhiteAdditiveTimeline.GetValueAt(t);
			_pressStartOpacity = PressStartOpacityTimeline.GetValueAt(t);
			_textOpacity = TextOpacityTimeline.GetValueAt(t);
			_textWhiteAdditive = TextWhiteAdditiveTimeline.GetValueAt(t);
			yield return UpdateResult.Next();
		}
		_busy = false;
	}

	/// <summary>
	/// Slides the menu items one place in <paramref name="direction"/> over 15 ticks. The selection markers
	/// move up during the first 8 ticks, and then move down to the new item.
	/// </summary>
	private IEnumerable<UpdateResult> EffectNavigateMenu(int direction)
	{
		_busy = true;
		int markerOffset = GetMarkerOffset(NegMod(_selectionIndex - direction, _menuItems.Length));
		// Integer division, as in the original code.
		double markerVelocityX = (GetMarkerOffset(_selectionIndex) - markerOffset) / 7;
		double markerVelocityY = 7.0;
		float velocity = direction * -1 * 26.666666f;
		for (int t = 0; t < 15; ++t)
		{
			foreach (MenuItemWidget menuItemWidget in _menuItemWidgets)
			{
				menuItemWidget.X += velocity;
				menuItemWidget.Opacity = (float)MenuItemOpacityEaseTimeline.GetValueAt((int)menuItemWidget.X);
			}
			if (t <= 7)
			{
				for (int index = 0; index < _selectedMenuItemMarkerPositions.Length; ++index)
					_selectedMenuItemMarkerPositions[index].Y -= markerVelocityY;
			}
			else
			{
				_selectedMenuItemMarkerPositions[0].X -= markerVelocityX;
				_selectedMenuItemMarkerPositions[1].X += markerVelocityX;
				_selectedMenuItemMarkerPositions[0].Y += markerVelocityY;
				_selectedMenuItemMarkerPositions[1].Y += markerVelocityY;
			}
			yield return UpdateResult.Next();
		}
		// Snap the widgets to their new places. A widget that leaves one end of the carousel goes to the other end.
		foreach (MenuItemWidget menuItemWidget in _menuItemWidgets)
		{
			int offset = menuItemWidget.OriginOffset - direction;
			switch (offset)
			{
				case -3:
					offset = 2;
					menuItemWidget.MenuItemIndex = NegMod(menuItemWidget.MenuItemIndex - 1, _menuItems.Length);
					break;
				case 3:
					offset = -2;
					menuItemWidget.MenuItemIndex = NegMod(menuItemWidget.MenuItemIndex + 1, _menuItems.Length);
					break;
			}
			menuItemWidget.X = 960 + MenuItemSpacing * offset;
			menuItemWidget.Opacity = (float)MenuItemOpacityEaseTimeline.GetValueAt((int)menuItemWidget.X);
			menuItemWidget.OriginOffset = offset;
		}
		SetSelectionMarkerPositions();
		_busy = false;
	}

	private IEnumerable<UpdateResult> EffectFadeOut()
	{
		MenuItemWidget widget = _menuItemWidgets.First(x => x.MenuItemIndex == _selectionIndex);
		_titleGameState.Background.WipeOut();
		for (int t = 0; t <= 30; ++t)
		{
			if (t == 15)
				_titleGameState.FadeOut();
			widget.Scale = new Vector2(ActivatedTextScaleTimeline.GetValueAt(t));
			_textOpacity = ActivatedTextOpacityTimeline.GetValueAt(t);
			yield return UpdateResult.Next();
		}
	}

	public void Draw(Renderer renderer)
	{
		if (!Visible)
			return;
		DrawPressStart(renderer);
		if (!_pressStartActive)
		{
			if (_characterSelectOpacity != 0.0)
				DrawCharacterSelect(renderer);
			if (_characterSelectOpacity != 1.0)
				DrawMenuItems(renderer);
		}
		if (!_levelSelectEnabled)
			return;
		DrawLevelSelect(renderer);
	}

	private void DrawPressStart(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Vector2i centre = new Vector2i(960, MenuY);
		if (_pressStartOpacity == 0.0)
			return;
		g.BlendMode = BlendMode.Alpha;
		using (g.BeginMatixState())
		{
			g.ModelMatrix = g.ModelMatrix.Scale(_pressStartScale.X, _pressStartScale.Y);
			g.ModelMatrix = g.ModelMatrix.Translate(centre.X, centre.Y);
			g.AdditiveColour = new Colour(_pressStartWhiteAdditive);
			fontRenderer.RenderStringWithShadow("PRESS START", new Rectangle(), FontAlignment.Centre, _fontImpactItalic, new Colour(_pressStartOpacity, 1.0, 1.0, 1.0), new int?(0));
			g.AdditiveColour = Colours.Transparent;
		}
		// Draw a scrolling zig-zag strip on each side of the text. The two strips scroll in opposite directions.
		int textWidth = (int)_fontImpactItalic.MeasureString("PRESS START").Width;
		int zigZagWidth = 79;
		int gap = 16;
		Vector2i zigZagOffset = new Vector2i(textWidth / 2 + gap + zigZagWidth / 2, 0);
		int zigZagTextureWidth = _textureZigZag.Width;
		int scroll = _ticks / 2;
		int leftWrapOffsetX = zigZagTextureWidth - scroll % zigZagTextureWidth;
		int rightWrapOffsetX = scroll % zigZagTextureWidth;
		DrawZigZag(g, new Rectanglei(centre.X - zigZagOffset.X - zigZagWidth / 2, centre.Y, zigZagWidth, 0), leftWrapOffsetX);
		DrawZigZag(g, new Rectanglei(centre.X + zigZagOffset.X - zigZagWidth / 2, centre.Y, zigZagWidth, 0), rightWrapOffsetX);
	}

	private void DrawZigZag(I2dRenderer g, Rectanglei rect, int wrapOffsetX)
	{
		rect.Y -= _textureZigZag.Height / 2;
		rect.Height = _textureZigZag.Height;
		Rectangle clipRectangle = g.ClipRectangle;
		g.ClipRectangle = (Rectangle)rect;
		Rectanglei destination = rect with
		{
			X = rect.X - wrapOffsetX,
			Width = _textureZigZag.Width
		};
		for (; destination.X < rect.Right; destination.X += destination.Width)
			g.RenderTexture(_textureZigZag, (Rectangle)destination);
		g.ClipRectangle = clipRectangle;
	}

	private void DrawMenuItems(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		renderer.GetFontRenderer();
		int y = MenuY;
		foreach (MenuItemWidget menuItemWidget in _menuItemWidgets)
		{
			MenuItem menuItem = _menuItems[menuItemWidget.MenuItemIndex];
			bool selected = menuItemWidget.MenuItemIndex == _selectionIndex;
			DrawMenuItem(renderer, menuItem.Text, new Vector2i((int)menuItemWidget.X, y), menuItemWidget.Opacity, menuItemWidget.Scale, selected);
		}
		g.Colour = new Colour(_textOpacity, 1.0, 1.0, 1.0);
		g.AdditiveColour = new Colour(_textWhiteAdditive);
		// Round the marker positions to whole pixels.
		foreach (Vector2 itemMarkerPosition in _selectedMenuItemMarkerPositions)
			g.RenderTexture(_textureSelectionMarker, (Vector2)(Vector2i)itemMarkerPosition);
		g.AdditiveColour = Colours.Transparent;
	}

	private void DrawMenuItem(
	  Renderer renderer,
	  string text,
	  Vector2i position,
	  double opacity,
	  Vector2 scale,
	  bool selected = false)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		int overlay = selected ? 1 : 0;
		if (_textOpacity < 1.0)
			opacity *= _textOpacity;
		// The shadow fades faster than the text.
		double shadowOpacity = opacity * opacity * opacity;
		Colour colour = new Colour(opacity, 1.0, 1.0, 1.0);
		Colour shadowColour = new Colour(shadowOpacity, 0.0, 0.0, 0.0);
		g.AdditiveColour = new Colour(_textWhiteAdditive);
		using (g.BeginMatixState())
		{
			g.ModelMatrix = g.ModelMatrix.Scale(scale);
			g.ModelMatrix = g.ModelMatrix.Translate((Vector2)position);
			fontRenderer.RenderStringWithShadow(text, new Rectangle(), FontAlignment.Centre, _fontImpactRegular, colour, new int?(overlay), _fontImpactRegular.DefaultShadow, shadowColour);
		}
		g.AdditiveColour = Colours.Transparent;
	}

	private void DrawCharacterSelect(Renderer renderer)
	{
		double characterSelectOpacity = _characterSelectOpacity;
		Colour inactiveColour = new Colour(characterSelectOpacity, 0.25, 0.25, 0.25);
		Colour activeColour = new Colour(characterSelectOpacity, 1.0, 1.0, 1.0);
		string text = new string[3]
		{
			"SONIC & TAILS",
			"SONIC",
			"TAILS"
		}[_characterSelectionIndex];
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Rectangle stripBounds = new Rectangle(0.0, 950.0, 1920.0, 60.0);
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour(0.3 * characterSelectOpacity, 0.0, 0.0, 0.0), stripBounds);
		fontRenderer.RenderStringWithShadow(text, stripBounds, FontAlignment.Centre, _fontImpactRegular, Colour.FromOpacity(characterSelectOpacity), new int?(1));
		// Draw the left and right arrows next to the text.
		g.Colour = Colour.FromOpacity(characterSelectOpacity);
		Rectangle textBounds = _fontImpactRegular.MeasureString(text, stripBounds, FontAlignment.Centre);
		g.RenderTexture(_textureLeftArrow, new Vector2(textBounds.Left - 50.0, textBounds.CentreY));
		g.RenderTexture(_textureRightArrow, new Vector2(textBounds.Right + 50.0, textBounds.CentreY));
		_miniSonicAniInstance.Draw(g, IsSonicActive ? activeColour : inactiveColour, (Vector2)new Vector2i(910, 880));
		_miniTailsAniInstance.Draw(g, IsTailsActive ? activeColour : inactiveColour, (Vector2)new Vector2i(1010, 880));
	}

	private void DrawLevelSelect(Renderer renderer)
	{
		I2dRenderer g = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		g.BlendMode = BlendMode.Alpha;
		g.RenderQuad(new Colour(0.8, 0.0, 0.0, 0.0), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		int x = 660;
		int y = 400;
		int index = 0;
		fontRenderer.RenderStringWithShadow("LEVEL SELECT", new Rectangle(x, 330.0, 0.0, 0.0), FontAlignment.MiddleY, _fontImpactRegular, 0);
		foreach (LevelSelectItem levelSelectItem in _levelSelectItems)
		{
			bool selected = _levelSelectSelectionIndex == index;
			int overlay = selected ? 1 : 0;
			Colour colour = selected ? Colours.White : new Colour(0.5, 1.0, 1.0, 1.0);
			fontRenderer.RenderStringWithShadow(levelSelectItem.Text, new Rectangle(x, y, 0.0, 0.0), FontAlignment.MiddleY, _fontImpactRegular, colour, new int?(overlay));
			y += 50;
			++index;
		}
	}

	/// <summary>Returns x modulo divisor in the range 0 to divisor - 1, also when x is negative.</summary>
	private static int NegMod(int x, int divisor)
	{
		while (x < 0)
			x += divisor;
		return x % divisor;
	}

	/// <summary>One item of the main menu: its text and the action that it does.</summary>
	private class MenuItem
	{
		public string Text { get; set; }

		public Action Action { get; set; }
	}

	/// <summary>
	/// The position, opacity and scale of one menu item on the screen. <see cref="OriginOffset"/> is the place
	/// relative to the selected item, from -2 to 2.
	/// </summary>
	private class MenuItemWidget
	{
		public int OriginOffset { get; set; }

		public int MenuItemIndex { get; set; }

		public float Opacity { get; set; }

		public float X { get; set; }

		public Vector2 Scale { get; set; }
	}

	/// <summary>One line of the level select: a level, an act, and the level number in the story.</summary>
	private class LevelSelectItem
	{
		public string Text { get; set; }

		public string Mnemonic { get; set; }

		public int Act { get; set; }

		public int Number { get; set; }
	}
}
