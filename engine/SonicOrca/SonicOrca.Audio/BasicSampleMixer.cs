using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Audio;

/// <summary>
/// Mixes 16-bit PCM channels into one buffer. Music channels are mixed after all other channels.
/// </summary>
public class BasicSampleMixer : ISampleMixer
{
	private const int BytesPerSample = 2;

	public void Mix(byte[] buffer, int offset, int length, IEnumerable<ISampleProvider> channels)
	{
		byte[] channelBuffer = new byte[length];
		IEnumerable<ISampleProvider> musicChannels = from x in channels.OfType<SampleInstance>()
													 where x.Classification == SampleInstanceClassification.Music
													 select x;
		ISampleProvider[] playingChannels = (from x in channels.Except(musicChannels).Concat(musicChannels)
											 where x.Playing
											 select x).ToArray();
		foreach (ISampleProvider channel in playingChannels)
		{
			double calculatedVolume = channel.CalculatedVolume;
			if (!(calculatedVolume <= 0.0))
			{
				// The read uses the offset, but the mix loop starts at index 0 in both buffers.
				int bytesRead = channel.Read(channelBuffer, offset, length);
				for (int i = 0; i < bytesRead; i += BytesPerSample)
				{
					short existingSample = BitConverter.ToInt16(buffer, i);
					short channelSample = (short)(BitConverter.ToInt16(channelBuffer, i) * calculatedVolume);
					short mixedSample = MixSample(existingSample, channelSample);
					buffer[i] = (byte)(mixedSample & 0xFF);
					buffer[i + 1] = (byte)((mixedSample >> 8) & 0xFF);
				}
			}
		}
	}

	/// <summary>
	/// Adds two samples. When the two samples have the same sign, a product term keeps the result in the 16-bit range.
	/// </summary>
	private static short MixSample(short a, short b)
	{
		int result;
		if (a < 0 && b < 0)
		{
			result = a + b - a * b / short.MinValue;
		}
		else if (a > 0 && b > 0)
		{
			result = a + b - a * b / short.MaxValue;
		}
		else
		{
			result = a + b;
		}
		return (short)result;
	}
}
