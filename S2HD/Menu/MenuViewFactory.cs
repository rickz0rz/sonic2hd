// Decompiled with JetBrains decompiler
// Type: S2HD.Menu.MenuViewFactory
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

internal class MenuViewFactory
{
	private S2HDSonicOrcaGameContext _gameContext;

	public MenuViewFactory(S2HDSonicOrcaGameContext gameContext) => _gameContext = gameContext;

	public IListMenuViewModel GetInGameOptionsView(bool canRestart)
	{
		List<IMenuItem> items = new List<IMenuItem>(4);
		items.Add(new MenuItem("RESUME", tag: 3));
		if (canRestart)
			items.Add(new MenuItem("RESTART", GetConfirmMenu(2)));
		items.Add(new MenuItem("OPTIONS", GetOptionsView()));
		items.Add(new MenuItem("QUIT", GetConfirmMenu(1)));
		return new ListMenuViewModel(items);
	}

	public IListMenuViewModel GetConfirmMenu(object confirmTag)
	{
		return new ListMenuViewModel(new IMenuItem[2]
		{
	   new MenuItem("NO", tag:  0),
	   new MenuItem("YES", tag: confirmTag)
		});
	}

	public IMenuViewModel GetOptionsView()
	{
		return new ListMenuViewModel(new IMenuItem[2]
		{
	   new MenuItem("AUDIO",  GetAudioOptions(),  6),
	   new MenuItem("VIDEO",  GetVideoOptions(),  7)
		}, 5);
	}

	public ISettingListMenuViewModel GetAudioOptions()
	{
		return new SettingListMenuViewModel(new ISetting[2]
		{
	   new MenuViewFactory.MusicSetting(_gameContext.Settings),
	   new MenuViewFactory.SoundSetting(_gameContext.Settings)
		}, 5);
	}

	public ISettingListMenuViewModel GetVideoOptions()
	{
		S2HDSettings settings = _gameContext.Settings;
		return new SettingListMenuViewModel(new ISetting[5]
		{
	   new StandardOptionSetting("MODE", new string[3]
	  {
		"WINDOW",
		"FULLSCREEN",
		"BORDERLESS WINDOWED"
	  },  () => (int) settings.Mode,  value => settings.Mode = (VideoMode) value),
	   new StandardOptionSetting("RESOLUTION", new string[1]
	  {
		"1920 X 1080"
	  },  () => 0,  value => { }),
	   new OnOffOptionSetting("SHADOWS",  () => settings.EnableShadows,  value => settings.EnableShadows = value),
	   new OnOffOptionSetting("WATER EFFECTS",  () => settings.EnableWaterEffects,  value => settings.EnableWaterEffects = value),
	   new OnOffOptionSetting("HEAT EFFECTS",  () => settings.EnableHeatEffects,  value => settings.EnableHeatEffects = value)
		}, 5);
	}

	private class MusicSetting : IAudioSliderSetting, ISetting
	{
		private readonly S2HDSettings _settings;

		public string Name => "MUSIC";

		public double Value
		{
			get => _settings.MusicVolume;
			set => _settings.MusicVolume = value;
		}

		public MusicSetting(S2HDSettings settings) => _settings = settings;
	}

	private class SoundSetting : IAudioSliderSetting, ISetting
	{
		private readonly S2HDSettings _settings;

		public string Name => "SFX";

		public double Value
		{
			get => _settings.SoundVolume;
			set => _settings.SoundVolume = value;
		}

		public SoundSetting(S2HDSettings settings) => _settings = settings;
	}
}
