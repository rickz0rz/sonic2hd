using System.Collections.Generic;

namespace SonicOrca.Audio;

public interface ISampleMixer
{
	void Mix(byte[] buffer, int offset, int length, IEnumerable<ISampleProvider> channels);
}
