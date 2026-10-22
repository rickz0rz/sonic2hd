namespace SonicOrca.Audio;

public interface ISampleProvider
{
	double CalculatedVolume { get; }

	double Pan { get; }

	bool Playing { get; }

	int Read(byte[] buffer, int offset, int count);
}
