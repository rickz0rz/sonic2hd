namespace SonicOrca;

/// <summary>
/// The audio settings that the user selected.
/// </summary>
public interface IAudioSettings
{
	double MusicVolume { get; }

	double SoundVolume { get; }
}
