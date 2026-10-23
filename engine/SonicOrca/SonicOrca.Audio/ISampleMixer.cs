using System.Collections.Generic;

namespace SonicOrca.Audio;

/// <summary>
/// Mixes the output of many sample providers into one PCM buffer.
/// </summary>
public interface ISampleMixer
{
	void Mix(byte[] buffer, int offset, int length, IEnumerable<ISampleProvider> channels);
}
