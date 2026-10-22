using System;

namespace csvorbis;

internal class Lpc
{
	private Drft fft = new Drft();

	private int ln;

	private int m;

	private static float lpc_from_data(float[] data, float[] lpc, int n, int m)
	{
		float[] array = new float[m + 1];
		int num = m + 1;
		while (num-- != 0)
		{
			float num2 = 0f;
			for (int i = num; i < n; i++)
			{
				num2 += data[i] * data[i - num];
			}
			array[num] = num2;
		}
		float num3 = array[0];
		for (int i = 0; i < m; i++)
		{
			float num4 = 0f - array[i + 1];
			if (num3 == 0f)
			{
				for (int j = 0; j < m; j++)
				{
					lpc[j] = 0f;
				}
				return 0f;
			}
			for (num = 0; num < i; num++)
			{
				num4 -= lpc[num] * array[i - num];
			}
			num4 = (lpc[i] = num4 / num3);
			for (num = 0; num < i / 2; num++)
			{
				float num5 = lpc[num];
				lpc[num] += num4 * lpc[i - 1 - num];
				lpc[i - 1 - num] += num4 * num5;
			}
			if (i % 2 != 0)
			{
				lpc[num] += lpc[num] * num4;
			}
			num3 *= (float)(1.0 - (double)(num4 * num4));
		}
		return num3;
	}

	private float lpc_from_curve(float[] curve, float[] lpc)
	{
		int num = ln;
		float[] array = new float[num + num];
		float num2 = (float)(0.5 / (double)num);
		int i;
		for (i = 0; i < num; i++)
		{
			array[i * 2] = curve[i] * num2;
			array[i * 2 + 1] = 0f;
		}
		array[num * 2 - 1] = curve[num - 1] * num2;
		num *= 2;
		fft.backward(array);
		i = 0;
		int num3 = num / 2;
		while (i < num / 2)
		{
			float num4 = array[i];
			array[i++] = array[num3];
			array[num3++] = num4;
		}
		return lpc_from_data(array, lpc, num, m);
	}

	internal void init(int mapped, int m)
	{
		ln = mapped;
		this.m = m;
		fft.init(mapped * 2);
	}

	private void clear()
	{
		fft.clear();
	}

	private static float FAST_HYPOT(float a, float b)
	{
		return (float)Math.Sqrt(a * a + b * b);
	}

	internal void lpc_to_curve(float[] curve, float[] lpc, float amp)
	{
		for (int i = 0; i < ln * 2; i++)
		{
			curve[i] = 0f;
		}
		if (amp != 0f)
		{
			for (int j = 0; j < m; j++)
			{
				curve[j * 2 + 1] = lpc[j] / (4f * amp);
				curve[j * 2 + 2] = (0f - lpc[j]) / (4f * amp);
			}
			fft.backward(curve);
			int num = ln * 2;
			float num2 = (float)(1.0 / (double)amp);
			curve[0] = (float)(1.0 / (double)(curve[0] * 2f + num2));
			for (int k = 1; k < ln; k++)
			{
				float num3 = curve[k] + curve[num - k];
				float b = curve[k] - curve[num - k];
				float a = num3 + num2;
				curve[k] = (float)(1.0 / (double)FAST_HYPOT(a, b));
			}
		}
	}
}
