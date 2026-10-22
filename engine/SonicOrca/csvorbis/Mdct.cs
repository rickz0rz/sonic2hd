using System;
using System.Runtime.CompilerServices;

namespace csvorbis;

internal class Mdct
{
	private int n;

	private int log2n;

	private float[] trig;

	private int[] bitrev;

	private float scale;

	private float[] _x = new float[1024];

	private float[] _w = new float[1024];

	internal void init(int n)
	{
		bitrev = new int[n / 4];
		trig = new float[n + n / 4];
		log2n = (int)Math.Round(Math.Log(n) / Math.Log(2.0));
		this.n = n;
		int num = 0;
		int num2 = 1;
		int num3 = num + n / 2;
		int num4 = num3 + 1;
		int num5 = num3 + n / 2;
		int num6 = num5 + 1;
		for (int i = 0; i < n / 4; i++)
		{
			trig[num + i * 2] = (float)Math.Cos(Math.PI / (double)n * (double)(4 * i));
			trig[num2 + i * 2] = (float)(0.0 - Math.Sin(Math.PI / (double)n * (double)(4 * i)));
			trig[num3 + i * 2] = (float)Math.Cos(Math.PI / (double)(2 * n) * (double)(2 * i + 1));
			trig[num4 + i * 2] = (float)Math.Sin(Math.PI / (double)(2 * n) * (double)(2 * i + 1));
		}
		for (int j = 0; j < n / 8; j++)
		{
			trig[num5 + j * 2] = (float)Math.Cos(Math.PI / (double)n * (double)(4 * j + 2));
			trig[num6 + j * 2] = (float)(0.0 - Math.Sin(Math.PI / (double)n * (double)(4 * j + 2)));
		}
		int num7 = (1 << log2n - 1) - 1;
		int num8 = 1 << log2n - 2;
		for (int k = 0; k < n / 8; k++)
		{
			int num9 = 0;
			for (int l = 0; num8 >>> l != 0; l++)
			{
				if (((uint)(num8 >>> l) & k) != 0L)
				{
					num9 |= 1 << l;
				}
			}
			bitrev[k * 2] = ~num9 & num7;
			bitrev[k * 2 + 1] = num9;
		}
		scale = 4f / (float)n;
	}

	internal void clear()
	{
	}

	internal void forward(float[] fin, float[] fout)
	{
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	internal void backward(float[] fin, float[] fout)
	{
		if (_x.Length < n / 2)
		{
			_x = new float[n / 2];
		}
		if (_w.Length < n / 2)
		{
			_w = new float[n / 2];
		}
		float[] x = _x;
		float[] w = _w;
		int num = n >>> 1;
		int num2 = n >>> 2;
		int num3 = n >>> 3;
		int num4 = 1;
		int num5 = 0;
		int num6 = num;
		for (int i = 0; i < num3; i++)
		{
			num6 -= 2;
			x[num5++] = (0f - fin[num4 + 2]) * trig[num6 + 1] - fin[num4] * trig[num6];
			x[num5++] = fin[num4] * trig[num6 + 1] - fin[num4 + 2] * trig[num6];
			num4 += 4;
		}
		num4 = num - 4;
		for (int i = 0; i < num3; i++)
		{
			num6 -= 2;
			x[num5++] = fin[num4] * trig[num6 + 1] + fin[num4 + 2] * trig[num6];
			x[num5++] = fin[num4] * trig[num6] - fin[num4 + 2] * trig[num6 + 1];
			num4 -= 4;
		}
		float[] array = mdct_kernel(x, w, n, num, num2, num3);
		int num7 = 0;
		int num8 = num;
		int num9 = num2;
		int num10 = num9 - 1;
		int num11 = num2 + num;
		int num12 = num11 - 1;
		for (int j = 0; j < num2; j++)
		{
			float num13 = array[num7] * trig[num8 + 1] - array[num7 + 1] * trig[num8];
			float num14 = 0f - (array[num7] * trig[num8] + array[num7 + 1] * trig[num8 + 1]);
			fout[num9] = 0f - num13;
			fout[num10] = num13;
			fout[num11] = num14;
			fout[num12] = num14;
			num9++;
			num10--;
			num11++;
			num12--;
			num7 += 2;
			num8 += 2;
		}
	}

	internal float[] mdct_kernel(float[] x, float[] w, int n, int n2, int n4, int n8)
	{
		int num = n4;
		int num2 = 0;
		int num3 = n4;
		int num4 = n2;
		for (int i = 0; i < n4; i++)
		{
			float num5 = x[num] - x[num2];
			w[num3 + i] = x[num++] + x[num2++];
			float num6 = x[num] - x[num2];
			num4 -= 4;
			w[i++] = num5 * trig[num4] + num6 * trig[num4 + 1];
			w[i] = num6 * trig[num4] - num5 * trig[num4 + 1];
			w[num3 + i] = x[num++] + x[num2++];
		}
		for (int j = 0; j < log2n - 3; j++)
		{
			int num7 = n >>> j + 2;
			int num8 = 1 << j + 3;
			int num9 = n2 - 2;
			num4 = 0;
			for (int k = 0; k < (uint)(num7 >>> 2); k++)
			{
				int num10 = num9;
				num3 = num10 - (num7 >> 1);
				float num11 = trig[num4];
				float num12 = trig[num4 + 1];
				num9 -= 2;
				num7++;
				for (int l = 0; l < 2 << j; l++)
				{
					float num13 = w[num10] - w[num3];
					x[num10] = w[num10] + w[num3];
					float num14 = w[++num10] - w[++num3];
					x[num10] = w[num10] + w[num3];
					x[num3] = num14 * num11 - num13 * num12;
					x[num3 - 1] = num13 * num11 + num14 * num12;
					num10 -= num7;
					num3 -= num7;
				}
				num7--;
				num4 += num8;
			}
			float[] array = w;
			w = x;
			x = array;
		}
		int num15 = n;
		int num16 = 0;
		int num17 = 0;
		int num18 = n2 - 1;
		for (int m = 0; m < n8; m++)
		{
			int num19 = bitrev[num16++];
			int num20 = bitrev[num16++];
			float num21 = w[num19] - w[num20 + 1];
			float num22 = w[num19 - 1] + w[num20];
			float num23 = w[num19] + w[num20 + 1];
			float num24 = w[num19 - 1] - w[num20];
			float num25 = num21 * trig[num15];
			float num26 = num22 * trig[num15++];
			float num27 = num21 * trig[num15];
			float num28 = num22 * trig[num15++];
			x[num17++] = (num23 + num27 + num26) * 0.5f;
			x[num18--] = (0f - num24 + num28 - num25) * 0.5f;
			x[num17++] = (num24 + num28 - num25) * 0.5f;
			x[num18--] = (num23 - num27 - num26) * 0.5f;
		}
		return x;
	}
}
