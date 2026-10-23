using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Extensions;

namespace SonicOrca.Audio;

internal class BassFilter
{
	public static void Apply(byte[] buffer)
	{
		Sample.PCMToSamples(buffer, out var leftSamples, out var rightSamples);
		int num = 512;
		for (int i = 0; i < leftSamples.Length; i += num)
		{
			float[] range = leftSamples.GetRange(i, Math.Min(leftSamples.Length - i, num));
			float[] range2 = rightSamples.GetRange(i, Math.Min(rightSamples.Length - i, num));
			Apply(range);
			Apply(range2);
			Array.Copy(range, 0, leftSamples, i, range.Length);
			Array.Copy(range2, 0, rightSamples, i, range2.Length);
		}
		Array.Copy(Sample.SamplesToPCM(leftSamples, leftSamples), buffer, buffer.Length);
	}

	private static void Apply(float[] samples)
	{
		int m = (int)Math.Log(samples.Length, 2.0);
		ComplexNumber[] array = FastFourierTransform.TimeToFrequency(m, samples.Select((Func<float, double>)((float x) => x)).ToArray());
		for (int num = 32; num < samples.Length; num++)
		{
			array[num].Real = 0.0;
			array[num].Imaginary = 0.0;
		}
		FastFourierTransform.FrequencyToTime(m, array);
		for (int num2 = 0; num2 < samples.Length; num2++)
		{
			samples[num2] = (float)array[num2].Real;
		}
	}
}
