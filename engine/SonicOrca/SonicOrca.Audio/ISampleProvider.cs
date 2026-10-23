namespace SonicOrca.Audio;

/// <summary>
/// A source of 16-bit stereo PCM data for the mixer.
/// </summary>
public interface ISampleProvider
{
	double CalculatedVolume { get; }

	double Pan { get; }

	bool Playing { get; }

	int Read(byte[] buffer, int offset, int count);
}
