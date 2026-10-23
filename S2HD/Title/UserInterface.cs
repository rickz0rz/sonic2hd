// Decompiled with JetBrains decompiler
// Type: S2HD.Title.UserInterface
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

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

internal class UserInterface
{
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
	private UserInterface.LevelSelectItem[] _levelSelectItems;
	private UserInterface.MenuItem[] _menuItems;
	private UserInterface.MenuItemWidget[] _menuItemWidgets;
	private AnimationInstance _miniSonicAniInstance;
	private AnimationInstance _miniTailsAniInstance;
	private int _characterSelectionIndex;
	private bool _characterSelectActive;
	private double _characterSelectOpacity;
	private const int DemoInitialTimeout = 720;
	private int? _demoTimeout;
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
		get => _characterSelectionIndex == 0 || _characterSelectionIndex == 1;
	}

	private bool IsTailsActive
	{
		get => _characterSelectionIndex == 0 || _characterSelectionIndex == 2;
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
		_miniSonicAniInstance = new AnimationInstance(resourceTree, "SONICORCA/TITLE/ANIGROUP", 11);
		_miniTailsAniInstance = new AnimationInstance(resourceTree, "SONICORCA/TITLE/ANIGROUP", 13);
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
		_demoTimeout = new int?(720);
		_characterSelectTimer = new int?(60);
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
		int? nullable;
		if (_demoTimeout.HasValue)
		{
			int? demoTimeout = _demoTimeout;
			_demoTimeout = demoTimeout.HasValue ? new int?(demoTimeout.GetValueOrDefault() - 1) : new int?();
			nullable = _demoTimeout;
			int num = 0;
			if ((nullable.GetValueOrDefault() <= num ? (nullable.HasValue ? 1 : 0) : 0) != 0)
			{
				_demoTimeout = new int?();
				StartDemo();
			}
		}
		if (!_characterSelected)
			return;
		nullable = _characterSelectTimer;
		_characterSelectTimer = nullable.HasValue ? new int?(nullable.GetValueOrDefault() - 1) : new int?();
		nullable = _characterSelectTimer;
		int num1 = 0;
		if ((nullable.GetValueOrDefault() == num1 ? (nullable.HasValue ? 1 : 0) : 0) == 0)
			return;
		OnSelectCharacter();
	}

	private void HandleInput()
	{
		Controller pressed1 = _gameContext.Pressed[0];
		InputState pressed2 = _gameContext.Input.Pressed;
		if (_busy)
			return;
		if (!_levelSelectEnabled)
		{
			int selectControllerInput = GetLevelSelectControllerInput(pressed1);
			if (selectControllerInput != -1)
			{
				if (_levelSelectInputState <= 3)
				{
					if (_levelSelectInputState != selectControllerInput)
						_levelSelectInputState = 0;
					else
						++_levelSelectInputState;
				}
				else if (selectControllerInput == 4 || selectControllerInput == 7)
				{
					if (_gameContext.Current[0].Action1 && selectControllerInput == 7)
					{
						_levelSelectInputState = 0;
						_levelSelectEnabled = true;
						_gameContext.Audio.PlaySound(_sampleNavigateYes);
						return;
					}
				}
				else
					_levelSelectInputState = 0;
			}
		}
		if (_levelSelectEnabled)
		{
			if (Math.Abs(pressed1.DirectionLeft.Y) >= 0.5)
			{
				if (Math.Sign(pressed1.DirectionLeft.Y) < 0)
				{
					_levelSelectSelectionIndex = UserInterface.NegMod(_levelSelectSelectionIndex - 1, _levelSelectItems.Length);
					_gameContext.Audio.PlaySound(_sampleNavigateCursor);
				}
				else
				{
					_levelSelectSelectionIndex = UserInterface.NegMod(_levelSelectSelectionIndex + 1, _levelSelectItems.Length);
					_gameContext.Audio.PlaySound(_sampleNavigateCursor);
				}
			}
			if (pressed1.Start)
			{
				_gameContext.Audio.PlaySound(_sampleNavigateYes);
				OnLevelSelectStart();
			}
			if (!UserInterface.BackPressed(pressed2))
				return;
			_gameContext.Audio.PlaySound(_sampleNavigateBack);
			_levelSelectEnabled = false;
		}
		else if (_pressStartActive)
		{
			if (!pressed1.Start)
				return;
			_effectEventManager.BeginEvent(EffectPressStart());
			_pressStartActive = false;
			_demoTimeout = new int?();
			_gameContext.Audio.PlaySound(_sampleNavigateYes);
		}
		else if (_characterSelectActive)
		{
			if (Math.Abs(pressed1.DirectionLeft.X) >= 0.5)
			{
				_characterSelectionIndex = Math.Sign(pressed1.DirectionLeft.X) >= 0 ? (_characterSelectionIndex + 1) % 3 : (_characterSelectionIndex - 1 + 3) % 3;
				_miniSonicAniInstance.Index = 11;
				_miniTailsAniInstance.Index = 13;
				_gameContext.Audio.PlaySound(_sampleNavigateCursor);
			}
			if (UserInterface.BackPressed(pressed2))
			{
				_characterSelectActive = false;
				_gameContext.Audio.PlaySound(_sampleNavigateBack);
			}
			else
			{
				if (!pressed1.Start)
					return;
				_gameContext.Audio.PlaySound(_sampleNavigateYes);
				_miniSonicAniInstance.Index = IsSonicActive ? 12 : 11;
				_miniTailsAniInstance.Index = IsTailsActive ? 14 : 13;
				_characterSelected = true;
			}
		}
		else
		{
			if (UserInterface.BackPressed(pressed2))
			{
				_demoTimeout = new int?(720);
				_pressStartActive = true;
				_pressStartOpacity = 1.0;
				_pressStartScale = new Vector2(1.0);
				_pressStartWhiteAdditive = 0.0;
				_selectionIndex = 0;
				InitialiseMenuItemWidgets();
				_gameContext.Audio.PlaySound(_sampleNavigateBack);
			}
			if (Math.Abs(pressed1.DirectionLeft.X) >= 0.5)
			{
				int direction = Math.Sign(pressed1.DirectionLeft.X);
				_selectionIndex = direction >= 0 ? (_selectionIndex + 1) % _menuItems.Length : (_selectionIndex - 1 + _menuItems.Length) % _menuItems.Length;
				_effectEventManager.BeginEvent(EffectNavigateMenu(direction));
				_gameContext.Audio.PlaySound(_sampleNavigateCursor);
			}
			if (!pressed1.Start)
				return;
			Action action = _menuItems[_selectionIndex].Action;
			if (action != null)
				action();
			_gameContext.Audio.PlaySound(_sampleNavigateYes);
		}
	}

	private static bool BackPressed(InputState inputState)
	{
		GamePadInputState gamePadInputState = inputState.GamePad[0];
		return inputState.Keyboard[41] || gamePadInputState.Select || gamePadInputState.East;
	}

	private int GetLevelSelectControllerInput(Controller pressed)
	{
		if (pressed.DirectionLeft.Y <= -0.5)
			return 0;
		if (pressed.DirectionLeft.Y >= 0.5)
			return 1;
		if (pressed.DirectionLeft.X <= -0.5)
			return 2;
		if (pressed.DirectionLeft.X >= 0.5)
			return 3;
		if (pressed.Action1)
			return 4;
		if (pressed.Action2)
			return 5;
		if (pressed.Action3)
			return 6;
		return pressed.Start ? 7 : -1;
	}

	private void InitialiseMenuItemWidgets()
	{
		_menuItems = new UserInterface.MenuItem[3]
		{
	  new UserInterface.MenuItem()
	  {
		Text = "NEW GAME",
		Action = new Action(OnSelectNewGame)
	  },
	  new UserInterface.MenuItem()
	  {
		Text = "OPTIONS",
		Action = new Action(OnSelectOptions)
	  },
	  new UserInterface.MenuItem()
	  {
		Text = "QUIT",
		Action = new Action(OnSelectQuit)
	  }
		};
		_menuItemWidgets = new UserInterface.MenuItemWidget[5];
		for (int index1 = -2; index1 <= 2; ++index1)
		{
			int index2 = (_selectionIndex + index1 + _menuItems.Length) % _menuItems.Length;
			UserInterface.MenuItem menuItem = _menuItems[index2];
			UserInterface.MenuItemWidget menuItemWidget = new UserInterface.MenuItemWidget()
			{
				MenuItemIndex = index2,
				OriginOffset = index1,
				X = 960 + 400 * index1,
				Scale = new Vector2(1.0)
			};
			menuItemWidget.Opacity = (float)UserInterface.MenuItemOpacityEaseTimeline.GetValueAt((int)menuItemWidget.X);
			_menuItemWidgets[index1 + 2] = menuItemWidget;
		}
		SetSelectionMarkerPositions();
	}

	private void SetSelectionMarkerPositions()
	{
		UserInterface.MenuItemWidget menuItemWidget = _menuItemWidgets.First<UserInterface.MenuItemWidget>(x => x.OriginOffset == 0);
		int markerOffset = GetMarkerOffset(_selectionIndex);
		_selectedMenuItemMarkerPositions[0] = new Vector2((double)menuItemWidget.X - markerOffset, 900.0);
		_selectedMenuItemMarkerPositions[1] = new Vector2((double)menuItemWidget.X + markerOffset, 900.0);
	}

	private int GetMenuItemWidth(int index)
	{
		return (int)_fontImpactRegular.MeasureString(_menuItems[index].Text).Width;
	}

	private int GetMarkerOffset(int index) => GetMenuItemWidth(index) / 2 + 48 /*0x30*/;

	private void InitialiseLevelSelect()
	{
		List<UserInterface.LevelSelectItem> levelSelectItemList = new List<UserInterface.LevelSelectItem>();
		int num = 1;
		foreach (LevelInfo level in (IEnumerable<LevelInfo>)Levels.LevelList)
		{
			if (!level.Unreleased)
			{
				for (int index = 1; index <= level.Acts; ++index)
				{
					string upper = $"{level.Name} ACT {index}".ToUpper();
					levelSelectItemList.Add(new UserInterface.LevelSelectItem()
					{
						Text = upper,
						Mnemonic = level.Mnemonic,
						Act = index,
						Number = num
					});
				}
				++num;
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
		UserInterface.LevelSelectItem levelSelectItem = _levelSelectItems[_levelSelectSelectionIndex];
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
			case 0:
				lps.ProtagonistCharacter = CharacterType.Sonic;
				lps.SidekickCharacter = CharacterType.Tails;
				break;
			case 1:
				lps.ProtagonistCharacter = CharacterType.Sonic;
				lps.SidekickCharacter = CharacterType.Null;
				break;
			case 2:
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
			_pressStartScale = new Vector2(UserInterface.PressStartScaleXTimeline.GetValueAt(t), UserInterface.PressStartScaleYTimeline.GetValueAt(t));
			_pressStartWhiteAdditive = UserInterface.PressStartWhiteAdditiveTimeline.GetValueAt(t);
			_pressStartOpacity = UserInterface.PressStartOpacityTimeline.GetValueAt(t);
			_textOpacity = UserInterface.TextOpacityTimeline.GetValueAt(t);
			_textWhiteAdditive = UserInterface.TextWhiteAdditiveTimeline.GetValueAt(t);
			yield return UpdateResult.Next();
		}
		_busy = false;
	}

	private IEnumerable<UpdateResult> EffectNavigateMenu(int direction)
	{
		_busy = true;
		int markerOffset = GetMarkerOffset(UserInterface.NegMod(_selectionIndex - direction, _menuItems.Length));
		double markerVelocityX = (GetMarkerOffset(_selectionIndex) - markerOffset) / 7;
		double markerVelocityY = 7.0;
		float velocity = direction * -1 * 26.666666f;
		for (int t = 0; t < 15; ++t)
		{
			foreach (UserInterface.MenuItemWidget menuItemWidget in _menuItemWidgets)
			{
				menuItemWidget.X += velocity;
				menuItemWidget.Opacity = (float)UserInterface.MenuItemOpacityEaseTimeline.GetValueAt((int)menuItemWidget.X);
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
		foreach (UserInterface.MenuItemWidget menuItemWidget in _menuItemWidgets)
		{
			int num = menuItemWidget.OriginOffset - direction;
			switch (num)
			{
				case -3:
					num = 2;
					menuItemWidget.MenuItemIndex = UserInterface.NegMod(menuItemWidget.MenuItemIndex - 1, _menuItems.Length);
					break;
				case 3:
					num = -2;
					menuItemWidget.MenuItemIndex = UserInterface.NegMod(menuItemWidget.MenuItemIndex + 1, _menuItems.Length);
					break;
			}
			menuItemWidget.X = 960 + 400 * num;
			menuItemWidget.Opacity = (float)UserInterface.MenuItemOpacityEaseTimeline.GetValueAt((int)menuItemWidget.X);
			menuItemWidget.OriginOffset = num;
		}
		SetSelectionMarkerPositions();
		_busy = false;
	}

	private IEnumerable<UpdateResult> EffectFadeOut()
	{
		UserInterface.MenuItemWidget widget = _menuItemWidgets.First<UserInterface.MenuItemWidget>(x => x.MenuItemIndex == _selectionIndex);
		_titleGameState.Background.WipeOut();
		for (int t = 0; t <= 30; ++t)
		{
			if (t == 15)
				_titleGameState.FadeOut();
			widget.Scale = new Vector2(UserInterface.ActivatedTextScaleTimeline.GetValueAt(t));
			_textOpacity = UserInterface.ActivatedTextOpacityTimeline.GetValueAt(t);
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
		Vector2i vector2i1 = new Vector2i(960, 900);
		if (_pressStartOpacity == 0.0)
			return;
		g.BlendMode = BlendMode.Alpha;
		using (g.BeginMatixState())
		{
			g.ModelMatrix = g.ModelMatrix.Scale(_pressStartScale.X, _pressStartScale.Y);
			g.ModelMatrix = g.ModelMatrix.Translate(vector2i1.X, vector2i1.Y);
			g.AdditiveColour = new Colour(_pressStartWhiteAdditive);
			fontRenderer.RenderStringWithShadow("PRESS START", new Rectangle(), FontAlignment.Centre, _fontImpactItalic, new Colour(_pressStartOpacity, 1.0, 1.0, 1.0), new int?(0));
			g.AdditiveColour = Colours.Transparent;
		}
		int width1 = (int)_fontImpactItalic.MeasureString("PRESS START").Width;
		int width2 = 79;
		int num1 = 16 /*0x10*/;
		Vector2i vector2i2 = new Vector2i(width1 / 2 + num1 + width2 / 2, 0);
		int width3 = _textureZigZag.Width;
		int num2 = _ticks / 2;
		int wrapOffsetX1 = width3 - num2 % width3;
		int wrapOffsetX2 = num2 % width3;
		DrawZigZag(g, new Rectanglei(vector2i1.X - vector2i2.X - width2 / 2, vector2i1.Y, width2, 0), wrapOffsetX1);
		DrawZigZag(g, new Rectanglei(vector2i1.X + vector2i2.X - width2 / 2, vector2i1.Y, width2, 0), wrapOffsetX2);
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
		I2dRenderer obj = renderer.Get2dRenderer();
		renderer.GetFontRenderer();
		int y = 900;
		foreach (UserInterface.MenuItemWidget menuItemWidget in _menuItemWidgets)
		{
			UserInterface.MenuItem menuItem = _menuItems[menuItemWidget.MenuItemIndex];
			bool selected = menuItemWidget.MenuItemIndex == _selectionIndex;
			DrawMenuItem(renderer, menuItem.Text, new Vector2i((int)menuItemWidget.X, y), (double)menuItemWidget.Opacity, menuItemWidget.Scale, selected);
		}
		obj.Colour = new Colour(_textOpacity, 1.0, 1.0, 1.0);
		obj.AdditiveColour = new Colour(_textWhiteAdditive);
		foreach (Vector2 itemMarkerPosition in _selectedMenuItemMarkerPositions)
			obj.RenderTexture(_textureSelectionMarker, (Vector2)(Vector2i)itemMarkerPosition);
		obj.AdditiveColour = Colours.Transparent;
	}

	private void DrawMenuItem(
	  Renderer renderer,
	  string text,
	  Vector2i position,
	  double opacity,
	  Vector2 scale,
	  bool selected = false)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		int num = 1;
		if (!selected)
			num = 0;
		if (_textOpacity < 1.0)
			opacity *= _textOpacity;
		double a = opacity * opacity * opacity;
		Colour colour = new Colour(opacity, 1.0, 1.0, 1.0);
		Colour shadowColour = new Colour(a, 0.0, 0.0, 0.0);
		obj.AdditiveColour = new Colour(_textWhiteAdditive);
		using (obj.BeginMatixState())
		{
			obj.ModelMatrix = obj.ModelMatrix.Scale(scale);
			obj.ModelMatrix = obj.ModelMatrix.Translate((Vector2)position);
			FontAlignment fontAlignment = FontAlignment.Centre;
			Font fontImpactRegular = _fontImpactRegular;
			fontRenderer.RenderStringWithShadow(text, new Rectangle(), fontAlignment, fontImpactRegular, colour, new int?(num), fontImpactRegular.DefaultShadow, shadowColour);
		}
		obj.AdditiveColour = Colours.Transparent;
	}

	private void DrawCharacterSelect(Renderer renderer)
	{
		double characterSelectOpacity = _characterSelectOpacity;
		Colour colour1 = new Colour(characterSelectOpacity, 0.25, 0.25, 0.25);
		Colour colour2 = new Colour(characterSelectOpacity, 1.0, 1.0, 1.0);
		string text1 = new string[3]
		{
	  "SONIC & TAILS",
	  "SONIC",
	  "TAILS"
		}[_characterSelectionIndex];
		I2dRenderer renderer1 = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		Rectangle rectangle1 = new Rectangle(0.0, 950.0, 1920.0, 60.0);
		renderer1.BlendMode = BlendMode.Alpha;
		renderer1.RenderQuad(new Colour(0.3 * characterSelectOpacity, 0.0, 0.0, 0.0), rectangle1);
		string text2 = text1;
		Rectangle boundary = rectangle1;
		Font fontImpactRegular = _fontImpactRegular;
		Colour colour3 = Colour.FromOpacity(characterSelectOpacity);
		int? overlay = new int?(1);
		fontRenderer.RenderStringWithShadow(text2, boundary, FontAlignment.Centre, fontImpactRegular, colour3, overlay);
		renderer1.Colour = Colour.FromOpacity(characterSelectOpacity);
		Rectangle rectangle2 = _fontImpactRegular.MeasureString(text1, rectangle1, FontAlignment.Centre);
		renderer1.RenderTexture(_textureLeftArrow, new Vector2(rectangle2.Left - 50.0, rectangle2.CentreY));
		renderer1.RenderTexture(_textureRightArrow, new Vector2(rectangle2.Right + 50.0, rectangle2.CentreY));
		Colour colour4 = IsSonicActive ? colour2 : colour1;
		_miniSonicAniInstance.Draw(renderer1, colour4, (Vector2)new Vector2i(910, 880));
		Colour colour5 = IsTailsActive ? colour2 : colour1;
		_miniTailsAniInstance.Draw(renderer1, colour5, (Vector2)new Vector2i(1010, 880));
	}

	private void DrawLevelSelect(Renderer renderer)
	{
		I2dRenderer obj = renderer.Get2dRenderer();
		IFontRenderer fontRenderer = renderer.GetFontRenderer();
		obj.BlendMode = BlendMode.Alpha;
		obj.RenderQuad(new Colour(0.8, 0.0, 0.0, 0.0), new Rectangle(0.0, 0.0, 1920.0, 1080.0));
		int x = 660;
		int y = 400;
		int num1 = 0;
		fontRenderer.RenderStringWithShadow("LEVEL SELECT", new Rectangle(x, 330.0, 0.0, 0.0), FontAlignment.MiddleY, _fontImpactRegular, 0);
		foreach (UserInterface.LevelSelectItem levelSelectItem in _levelSelectItems)
		{
			int num2 = _levelSelectSelectionIndex == num1 ? 1 : 0;
			int num3 = num2 != 0 ? 1 : 0;
			Colour colour = num2 != 0 ? Colours.White : new Colour(0.5, 1.0, 1.0, 1.0);
			fontRenderer.RenderStringWithShadow(levelSelectItem.Text, new Rectangle(x, y, 0.0, 0.0), FontAlignment.MiddleY, _fontImpactRegular, colour, new int?(num3));
			y += 50;
			++num1;
		}
	}

	private static int NegMod(int x, int divisor)
	{
		while (x < 0)
			x += divisor;
		return x % divisor;
	}

	private class MenuItem
	{
		public string Text { get; set; }

		public Action Action { get; set; }
	}

	private class MenuItemWidget
	{
		public int OriginOffset { get; set; }

		public int MenuItemIndex { get; set; }

		public float Opacity { get; set; }

		public float X { get; set; }

		public Vector2 Scale { get; set; }
	}

	private class LevelSelectItem
	{
		public string Text { get; set; }

		public string Mnemonic { get; set; }

		public int Act { get; set; }

		public int Number { get; set; }
	}
}
