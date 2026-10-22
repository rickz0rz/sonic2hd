using System;
using System.Linq;

namespace SonicOrca.Audio;

public static class FastFourierTransform
{
	public static double HammingWindow(int n, int frameSize)
	{
		return 0.54 - 0.46 * Math.Cos(Math.PI * 2.0 * (double)n / (double)(frameSize - 1));
	}

	public static ComplexNumber[] TimeToFrequency(int m, double[] samples)
	{
		ComplexNumber[] array = samples.Select((double x) => new ComplexNumber(x, 0.0)).ToArray();
		Apply(forward: true, m, array);
		return array;
	}

	public static void TimeToFrequency(int m, ComplexNumber[] data)
	{
		Apply(forward: true, m, data);
	}

	public static void FrequencyToTime(int m, ComplexNumber[] data)
	{
		Apply(forward: false, m, data);
	}

	private static void Apply(bool forward, int m, ComplexNumber[] data)
	{
		int num = 1;
		int num2 = 0;
		int num3 = 1;
		double num4 = -1.0;
		double num5 = 0.0;
		for (int i = 0; i < m; i++)
		{
			num *= 2;
		}
		int num6 = num >> 1;
		for (int i = 0; i < num - 1; i++)
		{
			if (i < num2)
			{
				double real = data[i].Real;
				double imaginary = data[i].Imaginary;
				data[i].Real = data[num2].Real;
				data[i].Imaginary = data[num2].Imaginary;
				data[num2].Real = real;
				data[num2].Imaginary = imaginary;
			}
			int num7;
			for (num7 = num6; num7 <= num2; num7 >>= 1)
			{
				num2 -= num7;
			}
			num2 += num7;
		}
		for (int j = 0; j < m; j++)
		{
			int num8 = num3;
			num3 <<= 1;
			double num9 = 1.0;
			double num10 = 0.0;
			for (num2 = 0; num2 < num8; num2++)
			{
				for (int i = num2; i < num; i += num3)
				{
					int num11 = i + num8;
					double num12 = num9 * data[num11].Real - num10 * data[num11].Imaginary;
					double num13 = num9 * data[num11].Imaginary + num10 * data[num11].Real;
					data[num11].Real = data[i].Real - num12;
					data[num11].Imaginary = data[i].Imaginary - num13;
					data[i].Real += num12;
					data[i].Imaginary += num13;
				}
				double num14 = num9 * num4 - num10 * num5;
				num10 = num9 * num5 + num10 * num4;
				num9 = num14;
			}
			num5 = Math.Sqrt((1.0 - num4) / 2.0);
			if (forward)
			{
				num5 = 0.0 - num5;
			}
			num4 = Math.Sqrt((1.0 + num4) / 2.0);
		}
		if (forward)
		{
			for (int i = 0; i < num; i++)
			{
				data[i].Real /= num;
				data[i].Imaginary /= num;
			}
		}
	}
}
