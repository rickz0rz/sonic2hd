// Decompiled with JetBrains decompiler
// Type: S2HD.S2HDSettings
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD;

internal class S2HDSettings : IAudioSettings, IVideoSettings
{
	private readonly AudioContext _audioContext;
	private readonly WindowContext _windowContext;
	private readonly IniConfiguration _config;
	private VideoMode _mode;
	private bool _enableShadows;
	private bool _enableWaterEffects;
	private bool _enableHeatEffects;

	public double MusicVolume
	{
		get => _audioContext.MusicVolume;
		set
		{
			_audioContext.MusicVolume = value;
			_config.SetProperty("audio", "music_volume", value.ToString().ToLower());
			_config.Save();
		}
	}

	public double SoundVolume
	{
		get => _audioContext.SoundVolume;
		set
		{
			_audioContext.SoundVolume = value;
			_config.SetProperty("audio", "sound_volume", value.ToString().ToLower());
			_config.Save();
		}
	}

	public VideoMode Mode
	{
		get => _mode;
		set
		{
			_mode = value;
			_config.SetProperty("video", "fullscreen", ((int)Mode).ToString().ToLower());
			_config.Save();
		}
	}

	public Resolution Resolution { get; set; }

	public bool EnableShadows
	{
		get => _enableShadows;
		set
		{
			_enableShadows = value;
			_config.SetProperty("graphics", "shadows", value.ToString().ToLower());
			_config.Save();
		}
	}

	public bool EnableWaterEffects
	{
		get => _enableWaterEffects;
		set
		{
			_enableWaterEffects = value;
			_config.SetProperty("graphics", "water_effects", value.ToString().ToLower());
			_config.Save();
		}
	}

	public bool EnableHeatEffects
	{
		get => _enableHeatEffects;
		set
		{
			_enableHeatEffects = value;
			_config.SetProperty("graphics", "heat_effects", value.ToString().ToLower());
			_config.Save();
		}
	}

	public S2HDSettings(
	  IniConfiguration config,
	  AudioContext audioContext,
	  WindowContext windowContext)
	{
		_config = config;
		_audioContext = audioContext;
		_windowContext = windowContext;
		_audioContext.MusicVolume = config.GetPropertyDouble("audio", "music_volume", 1.0);
		_audioContext.SoundVolume = config.GetPropertyDouble("audio", "sound_volume", 1.0);
		_mode = VideoMode.Windowed;
		int result;
		if (int.TryParse(config.GetProperty("video", "fullscreen", "0"), out result))
			_mode = (VideoMode)result;
		Resolution = new Resolution(1920, 1080);
		_enableShadows = config.GetPropertyBoolean("graphics", "shadows", true);
		_enableWaterEffects = config.GetPropertyBoolean("graphics", "water_effects", true);
		_enableHeatEffects = config.GetPropertyBoolean("graphics", "heat_effects");
	}

	public void Apply() => _windowContext.Mode = Mode;
}
