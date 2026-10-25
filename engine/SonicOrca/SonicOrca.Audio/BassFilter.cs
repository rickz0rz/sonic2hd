using System;
using System.Linq;
using SonicOrca.Extensions;

namespace SonicOrca.Audio;

/// <summary>
/// Low-pass filter for 16-bit stereo PCM. It removes high frequency bins with a Fourier transform on blocks of samples.
/// </summary>
internal class BassFilter
{
	private const int BlockSize = 512;

	private const int FrequencyCutoffBin = 32;

	public static void Apply(byte[] buffer)
	{
		Sample.PCMToSamples(buffer, out float[] leftSamples, out float[] rightSamples);
		for (int i = 0; i < leftSamples.Length; i += BlockSize)
		{
			float[] leftBlock = leftSamples.GetRange(i, Math.Min(leftSamples.Length - i, BlockSize));
			float[] rightBlock = rightSamples.GetRange(i, Math.Min(rightSamples.Length - i, BlockSize));
			Apply(leftBlock);
			Apply(rightBlock);
			Array.Copy(leftBlock, 0, leftSamples, i, leftBlock.Length);
			Array.Copy(rightBlock, 0, rightSamples, i, rightBlock.Length);
		}
		Array.Copy(Sample.SamplesToPCM(leftSamples, rightSamples), buffer, buffer.Length);
	}

	private static void Apply(float[] samples)
	{
		int m = (int)Math.Log(samples.Length, 2.0);
		ComplexNumber[] frequencies = FastFourierTransform.TimeToFrequency(m, samples.Select(x => (double)x).ToArray());
		for (int i = FrequencyCutoffBin; i < samples.Length; i++)
		{
			frequencies[i].Real = 0.0;
			frequencies[i].Imaginary = 0.0;
		}
		FastFourierTransform.FrequencyToTime(m, frequencies);
		for (int i = 0; i < samples.Length; i++)
		{
			samples[i] = (float)frequencies[i].Real;
		}
	}
}
