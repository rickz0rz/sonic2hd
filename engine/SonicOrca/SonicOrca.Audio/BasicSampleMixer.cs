using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicOrca.Audio;

public class BasicSampleMixer : ISampleMixer
{
	public void Mix(byte[] buffer, int offset, int length, IEnumerable<ISampleProvider> channels)
	{
		byte[] array = new byte[length];
		IEnumerable<ISampleProvider> second = from x in channels.OfType<SampleInstance>()
											  where x.Classification == SampleInstanceClassification.Music
											  select x;
		foreach (ISampleProvider item in (IEnumerable<ISampleProvider>)(from x in channels.Except(second).Concat(second)
																		where x.Playing
																		select x).ToArray())
		{
			double calculatedVolume = item.CalculatedVolume;
			if (!(calculatedVolume <= 0.0))
			{
				int num = item.Read(array, offset, length);
				for (int num2 = 0; num2 < num; num2 += 2)
				{
					short a = BitConverter.ToInt16(buffer, num2);
					short b = (short)(BitConverter.ToInt16(array, num2) * calculatedVolume);
					short num3 = MixSample(a, b);
					buffer[num2] = (byte)(num3 & 0xFF);
					buffer[num2 + 1] = (byte)((num3 >> 8) & 0xFF);
				}
			}
		}
	}

	private short MixSample(short a, short b)
	{
		int num;
		if (a < 0 && b < 0)
		{
			num = a + b - a * b / -32768;
		}
		else
		{
			num = ((a <= 0 || b <= 0) ? (a + b) : (a + b - a * b / 32767));
		}
		return (short)num;
	}
}
