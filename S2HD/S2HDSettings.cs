using System;
using System.Globalization;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Graphics;

namespace S2HD;

/// <summary>
/// The audio, video and game settings of the game. Each change goes to the audio or window context and is saved
/// to the configuration file at once.
/// </summary>
internal class S2HDSettings : IAudioSettings, IVideoSettings
{
	private readonly AudioContext _audioContext;
	private readonly WindowContext _windowContext;
	private readonly IniConfiguration _config;
	private VideoMode _mode;
	private bool _enableShadows;
	private bool _enableWaterEffects;
	private bool _enableHeatEffects;
	private PhysicsMode _physics;
	private double _hudScale;

	public double MusicVolume
	{
		get => _audioContext.MusicVolume;
		set
		{
			_audioContext.MusicVolume = value;
			_config.SetProperty("audio", "music_volume", value.ToString(CultureInfo.InvariantCulture).ToLower());
			_config.Save();
		}
	}

	public double SoundVolume
	{
		get => _audioContext.SoundVolume;
		set
		{
			_audioContext.SoundVolume = value;
			_config.SetProperty("audio", "sound_volume", value.ToString(CultureInfo.InvariantCulture).ToLower());
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

	/// <summary>The size of the HUD, from 75% to 150% in steps of 5%. It is saved as [graphics] hud_scale.</summary>
	public double HudScale
	{
		get => _hudScale;
		set
		{
			_hudScale = SonicOrca.HudScale.Clamp(value);
			_config.SetProperty("graphics", "hud_scale", _hudScale.ToString("0.##", CultureInfo.InvariantCulture));
			_config.Save();
		}
	}

	/// <summary>How the game plays: as the original Sonic 2, or as Sonic 2 HD. A level uses it when it starts.</summary>
	public PhysicsMode Physics
	{
		get => _physics;
		set
		{
			_physics = value;
			_config.SetProperty("game", "physics", value.ToString().ToLower());
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
		if (int.TryParse(config.GetProperty("video", "fullscreen", "0"), out int videoMode))
			_mode = (VideoMode)videoMode;
		_enableShadows = config.GetPropertyBoolean("graphics", "shadows", true);
		_enableWaterEffects = config.GetPropertyBoolean("graphics", "water_effects", true);
		_enableHeatEffects = config.GetPropertyBoolean("graphics", "heat_effects");
		_hudScale = SonicOrca.HudScale.Clamp(config.GetPropertyDouble("graphics", "hud_scale", 1.0));
		_physics = Enum.TryParse(config.GetProperty("game", "physics", "original"), ignoreCase: true, out PhysicsMode physics)
			? physics
			: PhysicsMode.Original;
	}

	/// <summary>Applies the video mode to the window.</summary>
	public void Apply() => _windowContext.Mode = Mode;
}
