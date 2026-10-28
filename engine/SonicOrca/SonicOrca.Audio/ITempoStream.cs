namespace SonicOrca.Audio;

/// <summary>
/// A stream of a song that plays live and that can change its tempo while it plays. The original changes the tempo of
/// the music when the player gets the speed shoes.
/// </summary>
public interface ITempoStream
{
	/// <summary>
	/// Plays the rest of the song at the tempo of a copy of the song. Returns false if the copy is not a song that plays
	/// live.
	/// </summary>
	bool ChangeTempo(Sample copy);
}
