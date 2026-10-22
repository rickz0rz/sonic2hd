using System;
using System.Runtime.InteropServices;

namespace csvorbis;

internal class Lsp
{
	[StructLayout(LayoutKind.Explicit, Size = 32)]
	private struct FloatHack
	{
		[FieldOffset(0)]
		public float fh_float;

		[FieldOffset(0)]
		public int fh_int;
	}

	private static float M_PI = (float)Math.PI;

	internal static void lsp_to_curve(float[] curve, int[] map, int n, int ln, float[] lsp, int m, float amp, float ampoffset)
	{
		float num = M_PI / (float)ln;
		int i;
		for (i = 0; i < m; i++)
		{
			lsp[i] = Lookup.coslook(lsp[i]);
		}
		int num2 = m / 2 * 2;
		i = 0;
		while (i < n)
		{
			FloatHack floatHack = default;
			int num3 = map[i];
			float num4 = 0.70710677f;
			float num5 = 0.70710677f;
			float num6 = Lookup.coslook(num * (float)num3);
			for (int j = 0; j < num2; j += 2)
			{
				num5 *= lsp[j] - num6;
				num4 *= lsp[j + 1] - num6;
			}
			if ((m & 1) != 0)
			{
				num5 *= lsp[m - 1] - num6;
				num5 *= num5;
				num4 *= num4 * (1f - num6 * num6);
			}
			else
			{
				num5 *= num5 * (1f + num6);
				num4 *= num4 * (1f - num6);
			}
			num5 = (floatHack.fh_float = num4 + num5);
			int fh_int = floatHack.fh_int;
			int num7 = 0x7FFFFFFF & fh_int;
			int num8 = 0;
			if (num7 < 2139095040 && num7 != 0)
			{
				if (num7 < 8388608)
				{
					num5 *= 33554432f;
					floatHack.fh_float = num5;
					fh_int = floatHack.fh_int;
					num7 = 0x7FFFFFFF & fh_int;
					num8 = -25;
				}
				num8 += (num7 >>> 23) - 126;
				fh_int = (int)((fh_int & 0x807FFFFFu) | 0x3F000000);
				floatHack.fh_int = fh_int;
				num5 = floatHack.fh_float;
			}
			num5 = Lookup.fromdBlook(amp * Lookup.invsqlook(num5) * Lookup.invsq2explook(num8 + m) - ampoffset);
			do
			{
				curve[i] *= num5;
				i++;
			}
			while (i < n && map[i] == num3);
		}
	}
}
