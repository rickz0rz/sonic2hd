using System.Collections.Generic;
using SonicOrca;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// Makes the menu pages of the options screen and of the pause menu.
/// </summary>
internal class MenuViewFactory
{
	private readonly S2HDSonicOrcaGameContext _gameContext;

	public MenuViewFactory(S2HDSonicOrcaGameContext gameContext) => _gameContext = gameContext;

	public IListMenuViewModel GetInGameOptionsView(bool canRestart)
	{
		List<IMenuItem> items = new List<IMenuItem>(4);
		items.Add(new MenuItem("RESUME", tag: MenuTags.RESUME));
		if (canRestart)
			items.Add(new MenuItem("RESTART", GetConfirmMenu(MenuTags.RESTART)));
		items.Add(new MenuItem("OPTIONS", GetOptionsView()));
		items.Add(new MenuItem("QUIT", GetConfirmMenu(MenuTags.QUIT)));
		return new ListMenuViewModel(items);
	}

	/// <summary>Makes a NO / YES menu. YES sends <paramref name="confirmTag"/>, and NO goes back.</summary>
	public IListMenuViewModel GetConfirmMenu(object confirmTag)
	{
		return new ListMenuViewModel(new IMenuItem[2]
		{
			new MenuItem("NO", tag: MenuTags.BACK),
			new MenuItem("YES", tag: confirmTag)
		});
	}

	public IMenuViewModel GetOptionsView()
	{
		return new ListMenuViewModel(new IMenuItem[2]
		{
			new MenuItem("AUDIO", GetAudioOptions(), MenuTags.AUDIO_MENU),
			new MenuItem("VIDEO", GetVideoOptions(), MenuTags.VIDEO_MENU)
		}, MenuTags.OPTIONS_MENU);
	}

	public ISettingListMenuViewModel GetAudioOptions()
	{
		return new SettingListMenuViewModel(new ISetting[2]
		{
			new MusicSetting(_gameContext.Settings),
			new SoundSetting(_gameContext.Settings)
		}, MenuTags.OPTIONS_MENU);
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
			}, () => (int)settings.Mode, value => settings.Mode = (VideoMode)value),
			// The game has only one resolution, so this setting does not change anything.
			new StandardOptionSetting("RESOLUTION", new string[1]
			{
				"1920 X 1080"
			}, () => 0, value => { }),
			new OnOffOptionSetting("SHADOWS", () => settings.EnableShadows, value => settings.EnableShadows = value),
			new OnOffOptionSetting("WATER EFFECTS", () => settings.EnableWaterEffects, value => settings.EnableWaterEffects = value),
			new OnOffOptionSetting("HEAT EFFECTS", () => settings.EnableHeatEffects, value => settings.EnableHeatEffects = value)
		}, MenuTags.OPTIONS_MENU);
	}

	/// <summary>The music volume as an audio slider setting.</summary>
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

	/// <summary>The sound effect volume as an audio slider setting.</summary>
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
