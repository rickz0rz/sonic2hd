using System;
using System.Linq;

namespace SonicOrca.Audio;

/// <summary>
/// In-place radix-2 fast Fourier transform. The data length must be 2 to the power of m.
/// </summary>
public static class FastFourierTransform
{
	public static double HammingWindow(int n, int frameSize)
	{
		return 0.54 - 0.46 * Math.Cos(Math.PI * 2.0 * n / (frameSize - 1));
	}

	public static ComplexNumber[] TimeToFrequency(int m, double[] samples)
	{
		ComplexNumber[] data = samples.Select(x => new ComplexNumber(x, 0.0)).ToArray();
		Apply(forward: true, m, data);
		return data;
	}

	public static void TimeToFrequency(int m, ComplexNumber[] data)
	{
		Apply(forward: true, m, data);
	}

	public static void FrequencyToTime(int m, ComplexNumber[] data)
	{
		Apply(forward: false, m, data);
	}

	/// <summary>
	/// Applies the transform in place. The forward transform divides the result by the number of points.
	/// </summary>
	private static void Apply(bool forward, int m, ComplexNumber[] data)
	{
		int pointCount = 1;
		int j = 0;
		int step = 1;
		double cosine = -1.0;
		double sine = 0.0;
		for (int i = 0; i < m; i++)
		{
			pointCount *= 2;
		}

		// Put the data in bit-reversed order.
		int halfPointCount = pointCount >> 1;
		for (int i = 0; i < pointCount - 1; i++)
		{
			if (i < j)
			{
				double real = data[i].Real;
				double imaginary = data[i].Imaginary;
				data[i].Real = data[j].Real;
				data[i].Imaginary = data[j].Imaginary;
				data[j].Real = real;
				data[j].Imaginary = imaginary;
			}
			int bit;
			for (bit = halfPointCount; bit <= j; bit >>= 1)
			{
				j -= bit;
			}
			j += bit;
		}

		// Butterfly passes.
		for (int level = 0; level < m; level++)
		{
			int halfStep = step;
			step <<= 1;
			double twiddleReal = 1.0;
			double twiddleImaginary = 0.0;
			for (j = 0; j < halfStep; j++)
			{
				for (int i = j; i < pointCount; i += step)
				{
					int pair = i + halfStep;
					double productReal = twiddleReal * data[pair].Real - twiddleImaginary * data[pair].Imaginary;
					double productImaginary = twiddleReal * data[pair].Imaginary + twiddleImaginary * data[pair].Real;
					data[pair].Real = data[i].Real - productReal;
					data[pair].Imaginary = data[i].Imaginary - productImaginary;
					data[i].Real += productReal;
					data[i].Imaginary += productImaginary;
				}
				double nextTwiddleReal = twiddleReal * cosine - twiddleImaginary * sine;
				twiddleImaginary = twiddleReal * sine + twiddleImaginary * cosine;
				twiddleReal = nextTwiddleReal;
			}
			sine = Math.Sqrt((1.0 - cosine) / 2.0);
			if (forward)
			{
				sine = 0.0 - sine;
			}
			cosine = Math.Sqrt((1.0 + cosine) / 2.0);
		}

		if (forward)
		{
			for (int i = 0; i < pointCount; i++)
			{
				data[i].Real /= pointCount;
				data[i].Imaginary /= pointCount;
			}
		}
	}
}
