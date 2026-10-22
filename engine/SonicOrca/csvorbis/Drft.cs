using System;

namespace csvorbis;

internal class Drft
{
	private int n;

	private float[] trigcache;

	private int[] splitcache;

	private static int[] ntryh = new int[4] { 4, 2, 3, 5 };

	private static float tpi = (float)Math.PI * 2f;

	private static float hsqt2 = 0.70710677f;

	private static float taui = 0.8660254f;

	private static float taur = -0.5f;

	private static float sqrt2 = 1.4142135f;

	internal void backward(float[] data)
	{
		if (n != 1)
		{
			drftb1(n, data, trigcache, trigcache, n, splitcache);
		}
	}

	internal void init(int n)
	{
		this.n = n;
		trigcache = new float[3 * n];
		splitcache = new int[32];
		fdrffti(n, trigcache, splitcache);
	}

	internal void clear()
	{
		if (trigcache != null)
		{
			trigcache = null;
		}
		if (splitcache != null)
		{
			splitcache = null;
		}
	}

	private static void drfti1(int n, float[] wa, int index, int[] ifac)
	{
		int num = 0;
		int num2 = -1;
		int num3 = n;
		int num4 = 0;
		while (true)
		{
			num2++;
			num = ((num2 >= 4) ? (num + 2) : ntryh[num2]);
			while (true)
			{
				int num5 = num3 / num;
				if (num3 - num * num5 != 0)
				{
					break;
				}
				num4++;
				ifac[num4 + 1] = num;
				num3 = num5;
				if (num == 2 && num4 != 1)
				{
					for (int i = 1; i < num4; i++)
					{
						int num6 = num4 - i + 1;
						ifac[num6 + 1] = ifac[num6];
					}
					ifac[2] = 2;
				}
				if (num3 != 1)
				{
					continue;
				}
				ifac[0] = n;
				ifac[1] = num4;
				float num7 = tpi / (float)n;
				int num8 = 0;
				int num9 = num4 - 1;
				int num10 = 1;
				if (num9 == 0)
				{
					return;
				}
				for (int j = 0; j < num9; j++)
				{
					int num11 = ifac[j + 2];
					int num12 = 0;
					int num13 = num10 * num11;
					int num14 = n / num13;
					int num15 = num11 - 1;
					for (num2 = 0; num2 < num15; num2++)
					{
						num12 += num10;
						int i = num8;
						float num16 = (float)num12 * num7;
						float num17 = 0f;
						for (int k = 2; k < num14; k += 2)
						{
							num17++;
							float num18 = num17 * num16;
							wa[index + i++] = (float)Math.Cos(num18);
							wa[index + i++] = (float)Math.Sin(num18);
						}
						num8 += num14;
					}
					num10 = num13;
				}
				return;
			}
		}
	}

	private static void fdrffti(int n, float[] wsave, int[] ifac)
	{
		if (n != 1)
		{
			drfti1(n, wsave, n, ifac);
		}
	}

	private static void dradf2(int ido, int l1, float[] cc, float[] ch, float[] wa1, int index)
	{
		int num = 0;
		int num3;
		int num2 = (num3 = l1 * ido);
		int num4 = ido << 1;
		for (int i = 0; i < l1; i++)
		{
			ch[num << 1] = cc[num] + cc[num3];
			ch[(num << 1) + num4 - 1] = cc[num] - cc[num3];
			num += ido;
			num3 += ido;
		}
		if (ido < 2)
		{
			return;
		}
		if (ido != 2)
		{
			num = 0;
			num3 = num2;
			for (int i = 0; i < l1; i++)
			{
				num4 = num3;
				int num5 = (num << 1) + (ido << 1);
				int num6 = num;
				int num7 = num + num;
				for (int j = 2; j < ido; j += 2)
				{
					num4 += 2;
					num5 -= 2;
					num6 += 2;
					num7 += 2;
					float num8 = wa1[index + j - 2] * cc[num4 - 1] + wa1[index + j - 1] * cc[num4];
					float num9 = wa1[index + j - 2] * cc[num4] - wa1[index + j - 1] * cc[num4 - 1];
					ch[num7] = cc[num6] + num9;
					ch[num5] = num9 - cc[num6];
					ch[num7 - 1] = cc[num6 - 1] + num8;
					ch[num5 - 1] = cc[num6 - 1] - num8;
				}
				num += ido;
				num3 += ido;
			}
			if (ido % 2 == 1)
			{
				return;
			}
		}
		num4 = (num3 = (num = ido) - 1);
		num3 += num2;
		for (int i = 0; i < l1; i++)
		{
			ch[num] = 0f - cc[num3];
			ch[num - 1] = cc[num4];
			num += ido << 1;
			num3 += ido;
			num4 += ido;
		}
	}

	private static void dradf4(int ido, int l1, float[] cc, float[] ch, float[] wa1, int index1, float[] wa2, int index2, float[] wa3, int index3)
	{
		int num = l1 * ido;
		int num2 = num;
		int num3 = num2 << 1;
		int num4 = num2 + (num2 << 1);
		int num5 = 0;
		int num8;
		for (int i = 0; i < l1; i++)
		{
			float num6 = cc[num2] + cc[num4];
			float num7 = cc[num5] + cc[num3];
			ch[num8 = num5 << 2] = num6 + num7;
			ch[(ido << 2) + num8 - 1] = num7 - num6;
			ch[(num8 += ido << 1) - 1] = cc[num5] - cc[num3];
			ch[num8] = cc[num4] - cc[num2];
			num2 += ido;
			num4 += ido;
			num5 += ido;
			num3 += ido;
		}
		if (ido < 2)
		{
			return;
		}
		int num9;
		if (ido != 2)
		{
			num2 = 0;
			for (int i = 0; i < l1; i++)
			{
				num4 = num2;
				num3 = num2 << 2;
				num8 = (num9 = ido << 1) + num3;
				for (int j = 2; j < ido; j += 2)
				{
					num5 = (num4 += 2);
					num3 += 2;
					num8 -= 2;
					num5 += num;
					float num10 = wa1[index1 + j - 2] * cc[num5 - 1] + wa1[index1 + j - 1] * cc[num5];
					float num11 = wa1[index1 + j - 2] * cc[num5] - wa1[index1 + j - 1] * cc[num5 - 1];
					num5 += num;
					float num12 = wa2[index2 + j - 2] * cc[num5 - 1] + wa2[index2 + j - 1] * cc[num5];
					float num13 = wa2[index2 + j - 2] * cc[num5] - wa2[index2 + j - 1] * cc[num5 - 1];
					num5 += num;
					float num14 = wa3[index3 + j - 2] * cc[num5 - 1] + wa3[index3 + j - 1] * cc[num5];
					float num15 = wa3[index3 + j - 2] * cc[num5] - wa3[index3 + j - 1] * cc[num5 - 1];
					float num6 = num10 + num14;
					float num16 = num14 - num10;
					float num17 = num11 + num15;
					float num18 = num11 - num15;
					float num19 = cc[num4] + num13;
					float num20 = cc[num4] - num13;
					float num7 = cc[num4 - 1] + num12;
					float num21 = cc[num4 - 1] - num12;
					ch[num3 - 1] = num6 + num7;
					ch[num3] = num17 + num19;
					ch[num8 - 1] = num21 - num18;
					ch[num8] = num16 - num20;
					ch[num3 + num9 - 1] = num18 + num21;
					ch[num3 + num9] = num16 + num20;
					ch[num8 + num9 - 1] = num7 - num6;
					ch[num8 + num9] = num17 - num19;
				}
				num2 += ido;
			}
			if ((ido & 1) != 0)
			{
				return;
			}
		}
		num4 = (num2 = num + ido - 1) + (num << 1);
		num5 = ido << 2;
		num3 = ido;
		num8 = ido << 1;
		num9 = ido;
		for (int i = 0; i < l1; i++)
		{
			float num17 = (0f - hsqt2) * (cc[num2] + cc[num4]);
			float num6 = hsqt2 * (cc[num2] - cc[num4]);
			ch[num3 - 1] = num6 + cc[num9 - 1];
			ch[num3 + num8 - 1] = cc[num9 - 1] - num6;
			ch[num3] = num17 - cc[num2 + num];
			ch[num3 + num8] = num17 + cc[num2 + num];
			num2 += ido;
			num4 += ido;
			num3 += num5;
			num9 += ido;
		}
	}

	private static void dradfg(int ido, int ip, int l1, int idl1, float[] cc, float[] c1, float[] c2, float[] ch, float[] ch2, float[] wa, int index)
	{
		int num = 0;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = tpi / (float)ip;
		num2 = (float)Math.Cos(num4);
		num3 = (float)Math.Sin(num4);
		int num5 = ip + 1 >> 1;
		int num6 = ido - 1 >> 1;
		int num7 = l1 * ido;
		int num8 = ip * ido;
		int num9;
		int num12;
		int num13;
		int num14;
		if (ido != 1)
		{
			for (int i = 0; i < idl1; i++)
			{
				ch2[i] = c2[i];
			}
			num9 = 0;
			for (int j = 1; j < ip; j++)
			{
				num9 += num7;
				num = num9;
				for (int k = 0; k < l1; k++)
				{
					ch[num] = c1[num];
					num += ido;
				}
			}
			int num10 = -ido;
			num9 = 0;
			if (num6 > l1)
			{
				for (int j = 1; j < ip; j++)
				{
					num9 += num7;
					num10 += ido;
					num = -ido + num9;
					for (int k = 0; k < l1; k++)
					{
						int num11 = num10 - 1;
						num += ido;
						num12 = num;
						for (int m = 2; m < ido; m += 2)
						{
							num11 += 2;
							num12 += 2;
							ch[num12 - 1] = wa[index + num11 - 1] * c1[num12 - 1] + wa[index + num11] * c1[num12];
							ch[num12] = wa[index + num11 - 1] * c1[num12] - wa[index + num11] * c1[num12 - 1];
						}
					}
				}
			}
			else
			{
				for (int j = 1; j < ip; j++)
				{
					num10 += ido;
					int num11 = num10 - 1;
					num9 += num7;
					num = num9;
					for (int m = 2; m < ido; m += 2)
					{
						num11 += 2;
						num += 2;
						num12 = num;
						for (int k = 0; k < l1; k++)
						{
							ch[num12 - 1] = wa[index + num11 - 1] * c1[num12 - 1] + wa[index + num11] * c1[num12];
							ch[num12] = wa[index + num11 - 1] * c1[num12] - wa[index + num11] * c1[num12 - 1];
							num12 += ido;
						}
					}
				}
			}
			num9 = 0;
			num = ip * num7;
			if (num6 < l1)
			{
				for (int j = 1; j < num5; j++)
				{
					num9 += num7;
					num -= num7;
					num12 = num9;
					num13 = num;
					for (int m = 2; m < ido; m += 2)
					{
						num12 += 2;
						num13 += 2;
						num14 = num12 - ido;
						int num15 = num13 - ido;
						for (int k = 0; k < l1; k++)
						{
							num14 += ido;
							num15 += ido;
							c1[num14 - 1] = ch[num14 - 1] + ch[num15 - 1];
							c1[num15 - 1] = ch[num14] - ch[num15];
							c1[num14] = ch[num14] + ch[num15];
							c1[num15] = ch[num15 - 1] - ch[num14 - 1];
						}
					}
				}
			}
			else
			{
				for (int j = 1; j < num5; j++)
				{
					num9 += num7;
					num -= num7;
					num12 = num9;
					num13 = num;
					for (int k = 0; k < l1; k++)
					{
						num14 = num12;
						int num15 = num13;
						for (int m = 2; m < ido; m += 2)
						{
							num14 += 2;
							num15 += 2;
							c1[num14 - 1] = ch[num14 - 1] + ch[num15 - 1];
							c1[num15 - 1] = ch[num14] - ch[num15];
							c1[num14] = ch[num14] + ch[num15];
							c1[num15] = ch[num15 - 1] - ch[num14 - 1];
						}
						num12 += ido;
						num13 += ido;
					}
				}
			}
		}
		for (int i = 0; i < idl1; i++)
		{
			c2[i] = ch2[i];
		}
		num9 = 0;
		num = ip * idl1;
		for (int j = 1; j < num5; j++)
		{
			num9 += num7;
			num -= num7;
			num12 = num9 - ido;
			num13 = num - ido;
			for (int k = 0; k < l1; k++)
			{
				num12 += ido;
				num13 += ido;
				c1[num12] = ch[num12] + ch[num13];
				c1[num13] = ch[num13] - ch[num12];
			}
		}
		float num16 = 1f;
		float num17 = 0f;
		num9 = 0;
		num = ip * idl1;
		num12 = (ip - 1) * idl1;
		for (int n = 1; n < num5; n++)
		{
			num9 += idl1;
			num -= idl1;
			float num18 = num2 * num16 - num3 * num17;
			num17 = num2 * num17 + num3 * num16;
			num16 = num18;
			num13 = num9;
			num14 = num;
			int num15 = num12;
			int num19 = idl1;
			for (int i = 0; i < idl1; i++)
			{
				ch2[num13++] = c2[i] + num16 * c2[num19++];
				ch2[num14++] = num17 * c2[num15++];
			}
			float num20 = num16;
			float num21 = num17;
			float num22 = num16;
			float num23 = num17;
			num13 = idl1;
			num14 = (ip - 1) * idl1;
			for (int j = 2; j < num5; j++)
			{
				num13 += idl1;
				num14 -= idl1;
				float num24 = num20 * num22 - num21 * num23;
				num23 = num20 * num23 + num21 * num22;
				num22 = num24;
				num15 = num9;
				num19 = num;
				int num25 = num13;
				int num26 = num14;
				for (int i = 0; i < idl1; i++)
				{
					ch2[num15] += num22 * c2[num25++];
					num15++;
					ch2[num19] += num23 * c2[num26++];
					num19++;
				}
			}
		}
		num9 = 0;
		for (int j = 1; j < num5; j++)
		{
			num9 += idl1;
			num = num9;
			for (int i = 0; i < idl1; i++)
			{
				ch2[i] += c2[num++];
			}
		}
		if (ido >= l1)
		{
			num9 = 0;
			num = 0;
			for (int k = 0; k < l1; k++)
			{
				num12 = num9;
				num13 = num;
				for (int m = 0; m < ido; m++)
				{
					cc[num13++] = ch[num12++];
				}
				num9 += ido;
				num += num8;
			}
		}
		else
		{
			for (int m = 0; m < ido; m++)
			{
				num9 = m;
				num = m;
				for (int k = 0; k < l1; k++)
				{
					cc[num] = ch[num9];
					num9 += ido;
					num += num8;
				}
			}
		}
		num9 = 0;
		num = ido << 1;
		num12 = 0;
		num13 = ip * num7;
		for (int j = 1; j < num5; j++)
		{
			num9 += num;
			num12 += num7;
			num13 -= num7;
			num14 = num9;
			int num15 = num12;
			int num19 = num13;
			for (int k = 0; k < l1; k++)
			{
				cc[num14 - 1] = ch[num15];
				cc[num14] = ch[num19];
				num14 += num8;
				num15 += ido;
				num19 += ido;
			}
		}
		if (ido == 1)
		{
			return;
		}
		if (num6 >= l1)
		{
			num9 = -ido;
			num12 = 0;
			num13 = 0;
			num14 = ip * num7;
			for (int j = 1; j < num5; j++)
			{
				num9 += num;
				num12 += num;
				num13 += num7;
				num14 -= num7;
				int num15 = num9;
				int num19 = num12;
				int num25 = num13;
				int num26 = num14;
				for (int k = 0; k < l1; k++)
				{
					for (int m = 2; m < ido; m += 2)
					{
						int num27 = ido - m;
						cc[m + num19 - 1] = ch[m + num25 - 1] + ch[m + num26 - 1];
						cc[num27 + num15 - 1] = ch[m + num25 - 1] - ch[m + num26 - 1];
						cc[m + num19] = ch[m + num25] + ch[m + num26];
						cc[num27 + num15] = ch[m + num26] - ch[m + num25];
					}
					num15 += num8;
					num19 += num8;
					num25 += ido;
					num26 += ido;
				}
			}
			return;
		}
		num9 = -ido;
		num12 = 0;
		num13 = 0;
		num14 = ip * num7;
		for (int j = 1; j < num5; j++)
		{
			num9 += num;
			num12 += num;
			num13 += num7;
			num14 -= num7;
			for (int m = 2; m < ido; m += 2)
			{
				int num15 = ido + num9 - m;
				int num19 = m + num12;
				int num25 = m + num13;
				int num26 = m + num14;
				for (int k = 0; k < l1; k++)
				{
					cc[num19 - 1] = ch[num25 - 1] + ch[num26 - 1];
					cc[num15 - 1] = ch[num25 - 1] - ch[num26 - 1];
					cc[num19] = ch[num25] + ch[num26];
					cc[num15] = ch[num26] - ch[num25];
					num15 += num8;
					num19 += num8;
					num25 += ido;
					num26 += ido;
				}
			}
		}
	}

	private static void drftf1(int n, float[] c, float[] ch, float[] wa, int[] ifac)
	{
		int num = ifac[1];
		int num2 = 1;
		int num3 = n;
		int num4 = n;
		int num7;
		for (int i = 0; i < num; num3 = num7, i++)
		{
			int num5 = num - i;
			int num6 = ifac[num5 + 1];
			num7 = num3 / num6;
			int num8 = n / num3;
			int idl = num8 * num7;
			num4 -= (num6 - 1) * num8;
			num2 = 1 - num2;
			switch (num6)
			{
			case 4:
			{
				int num9 = num4 + num8;
				int num10 = num9 + num8;
				if (num2 != 0)
				{
					dradf4(num8, num7, ch, c, wa, num4 - 1, wa, num9 - 1, wa, num10 - 1);
				}
				else
				{
					dradf4(num8, num7, c, ch, wa, num4 - 1, wa, num9 - 1, wa, num10 - 1);
				}
				continue;
			}
			case 2:
				if (num2 == 0)
				{
					dradf2(num8, num7, c, ch, wa, num4 - 1);
				}
				else
				{
					dradf2(num8, num7, ch, c, wa, num4 - 1);
				}
				continue;
			default:
				if (num8 == 1)
				{
					num2 = 1 - num2;
				}
				if (num2 == 0)
				{
					dradfg(num8, num6, num7, idl, c, c, c, ch, ch, wa, num4 - 1);
					num2 = 1;
					continue;
				}
				break;
			}
			dradfg(num8, num6, num7, idl, ch, ch, ch, c, c, wa, num4 - 1);
			num2 = 0;
			num3 = num7;
			break;
		}
		if (num2 != 1)
		{
			for (int j = 0; j < n; j++)
			{
				c[j] = ch[j];
			}
		}
	}

	private static void dradb2(int ido, int l1, float[] cc, float[] ch, float[] wa1, int index)
	{
		int num = l1 * ido;
		int num2 = 0;
		int num3 = 0;
		int num4 = (ido << 1) - 1;
		for (int i = 0; i < l1; i++)
		{
			ch[num2] = cc[num3] + cc[num4 + num3];
			ch[num2 + num] = cc[num3] - cc[num4 + num3];
			num3 = (num2 += ido) << 1;
		}
		if (ido < 2)
		{
			return;
		}
		if (ido != 2)
		{
			num2 = 0;
			num3 = 0;
			for (int i = 0; i < l1; i++)
			{
				num4 = num2;
				int num6;
				int num5 = (num6 = num3) + (ido << 1);
				int num7 = num + num2;
				for (int j = 2; j < ido; j += 2)
				{
					num4 += 2;
					num6 += 2;
					num5 -= 2;
					num7 += 2;
					ch[num4 - 1] = cc[num6 - 1] + cc[num5 - 1];
					float num8 = cc[num6 - 1] - cc[num5 - 1];
					ch[num4] = cc[num6] - cc[num5];
					float num9 = cc[num6] + cc[num5];
					ch[num7 - 1] = wa1[index + j - 2] * num8 - wa1[index + j - 1] * num9;
					ch[num7] = wa1[index + j - 2] * num9 + wa1[index + j - 1] * num8;
				}
				num3 = (num2 += ido) << 1;
			}
			if (ido % 2 == 1)
			{
				return;
			}
		}
		num2 = ido - 1;
		num3 = ido - 1;
		for (int i = 0; i < l1; i++)
		{
			ch[num2] = cc[num3] + cc[num3];
			ch[num2 + num] = 0f - (cc[num3 + 1] + cc[num3 + 1]);
			num2 += ido;
			num3 += ido << 1;
		}
	}

	private static void dradb3(int ido, int l1, float[] cc, float[] ch, float[] wa1, int index1, float[] wa2, int index2)
	{
		int num = l1 * ido;
		int num2 = 0;
		int num3 = num << 1;
		int num4 = ido << 1;
		int num5 = ido + (ido << 1);
		int num6 = 0;
		for (int i = 0; i < l1; i++)
		{
			float num7 = cc[num4 - 1] + cc[num4 - 1];
			float num8 = cc[num6] + taur * num7;
			ch[num2] = cc[num6] + num7;
			float num9 = taui * (cc[num4] + cc[num4]);
			ch[num2 + num] = num8 - num9;
			ch[num2 + num3] = num8 + num9;
			num2 += ido;
			num4 += num5;
			num6 += num5;
		}
		if (ido == 1)
		{
			return;
		}
		num2 = 0;
		num4 = ido << 1;
		for (int i = 0; i < l1; i++)
		{
			int num10 = num2 + (num2 << 1);
			int num11 = (num6 = num10 + num4);
			int num12 = num2;
			int num14;
			int num13 = (num14 = num2 + num) + num;
			for (int j = 2; j < ido; j += 2)
			{
				num6 += 2;
				num11 -= 2;
				num10 += 2;
				num12 += 2;
				num14 += 2;
				num13 += 2;
				float num7 = cc[num6 - 1] + cc[num11 - 1];
				float num8 = cc[num10 - 1] + taur * num7;
				ch[num12 - 1] = cc[num10 - 1] + num7;
				float num15 = cc[num6] - cc[num11];
				float num16 = cc[num10] + taur * num15;
				ch[num12] = cc[num10] + num15;
				float num17 = taui * (cc[num6 - 1] - cc[num11 - 1]);
				float num9 = taui * (cc[num6] + cc[num11]);
				float num18 = num8 - num9;
				float num19 = num8 + num9;
				float num20 = num16 + num17;
				float num21 = num16 - num17;
				ch[num14 - 1] = wa1[index1 + j - 2] * num18 - wa1[index1 + j - 1] * num20;
				ch[num14] = wa1[index1 + j - 2] * num20 + wa1[index1 + j - 1] * num18;
				ch[num13 - 1] = wa2[index2 + j - 2] * num19 - wa2[index2 + j - 1] * num21;
				ch[num13] = wa2[index2 + j - 2] * num21 + wa2[index2 + j - 1] * num19;
			}
			num2 += ido;
		}
	}

	private static void dradb4(int ido, int l1, float[] cc, float[] ch, float[] wa1, int index1, float[] wa2, int index2, float[] wa3, int index3)
	{
		int num = l1 * ido;
		int num2 = 0;
		int num3 = ido << 2;
		int num4 = 0;
		int num5 = ido << 1;
		int num6;
		for (int i = 0; i < l1; i++)
		{
			num6 = num4 + num5;
			int num7 = num2;
			float num8 = cc[num6 - 1] + cc[num6 - 1];
			float num9 = cc[num6] + cc[num6];
			float num10 = cc[num4] - cc[(num6 += num5) - 1];
			float num11 = cc[num4] + cc[num6 - 1];
			ch[num7] = num11 + num8;
			ch[num7 += num] = num10 - num9;
			ch[num7 += num] = num11 - num8;
			ch[num7 += num] = num10 + num9;
			num2 += ido;
			num4 += num3;
		}
		if (ido < 2)
		{
			return;
		}
		if (ido != 2)
		{
			num2 = 0;
			for (int i = 0; i < l1; i++)
			{
				int num7 = (num6 = (num4 = (num3 = num2 << 2) + num5)) + num5;
				int num12 = num2;
				for (int j = 2; j < ido; j += 2)
				{
					num3 += 2;
					num4 += 2;
					num6 -= 2;
					num7 -= 2;
					num12 += 2;
					float num13 = cc[num3] + cc[num7];
					float num14 = cc[num3] - cc[num7];
					float num15 = cc[num4] - cc[num6];
					float num9 = cc[num4] + cc[num6];
					float num10 = cc[num3 - 1] - cc[num7 - 1];
					float num11 = cc[num3 - 1] + cc[num7 - 1];
					float num16 = cc[num4 - 1] - cc[num6 - 1];
					float num8 = cc[num4 - 1] + cc[num6 - 1];
					ch[num12 - 1] = num11 + num8;
					float num17 = num11 - num8;
					ch[num12] = num14 + num15;
					float num18 = num14 - num15;
					float num19 = num10 - num9;
					float num20 = num10 + num9;
					float num21 = num13 + num16;
					float num22 = num13 - num16;
					int num23;
					ch[(num23 = num12 + num) - 1] = wa1[index1 + j - 2] * num19 - wa1[index1 + j - 1] * num21;
					ch[num23] = wa1[index1 + j - 2] * num21 + wa1[index1 + j - 1] * num19;
					ch[(num23 += num) - 1] = wa2[index2 + j - 2] * num17 - wa2[index2 + j - 1] * num18;
					ch[num23] = wa2[index2 + j - 2] * num18 + wa2[index2 + j - 1] * num17;
					ch[(num23 += num) - 1] = wa3[index3 + j - 2] * num20 - wa3[index3 + j - 1] * num22;
					ch[num23] = wa3[index3 + j - 2] * num22 + wa3[index3 + j - 1] * num20;
				}
				num2 += ido;
			}
			if (ido % 2 == 1)
			{
				return;
			}
		}
		num2 = ido;
		num3 = ido << 2;
		num4 = ido - 1;
		num6 = ido + (ido << 1);
		for (int i = 0; i < l1; i++)
		{
			int num7 = num4;
			float num13 = cc[num2] + cc[num6];
			float num14 = cc[num6] - cc[num2];
			float num10 = cc[num2 - 1] - cc[num6 - 1];
			float num11 = cc[num2 - 1] + cc[num6 - 1];
			ch[num7] = num11 + num11;
			ch[num7 += num] = sqrt2 * (num10 - num13);
			ch[num7 += num] = num14 + num14;
			ch[num7 += num] = (0f - sqrt2) * (num10 + num13);
			num4 += ido;
			num2 += num3;
			num6 += num3;
		}
	}

	private static void dradbg(int ido, int ip, int l1, int idl1, float[] cc, float[] c1, float[] c2, float[] ch, float[] ch2, float[] wa, int index)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		float num5 = 0f;
		float num6 = 0f;
		int num7 = 0;
		num3 = ip * ido;
		num2 = l1 * ido;
		float num8 = tpi / (float)ip;
		num5 = (float)Math.Cos(num8);
		num6 = (float)Math.Sin(num8);
		num4 = ido - 1 >>> 1;
		num7 = ip;
		num = ip + 1 >>> 1;
		int num9;
		int num10;
		int num11;
		if (ido >= l1)
		{
			num9 = 0;
			num10 = 0;
			for (int i = 0; i < l1; i++)
			{
				num11 = num9;
				int num12 = num10;
				for (int j = 0; j < ido; j++)
				{
					ch[num11] = cc[num12];
					num11++;
					num12++;
				}
				num9 += ido;
				num10 += num3;
			}
		}
		else
		{
			num9 = 0;
			for (int j = 0; j < ido; j++)
			{
				num10 = num9;
				num11 = num9;
				for (int i = 0; i < l1; i++)
				{
					ch[num10] = cc[num11];
					num10 += ido;
					num11 += num3;
				}
				num9++;
			}
		}
		num9 = 0;
		num10 = num7 * num2;
		int num14;
		int num13 = (num14 = ido << 1);
		for (int k = 1; k < num; k++)
		{
			num9 += num2;
			num10 -= num2;
			num11 = num9;
			int num12 = num10;
			int num15 = num14;
			for (int i = 0; i < l1; i++)
			{
				ch[num11] = cc[num15 - 1] + cc[num15 - 1];
				ch[num12] = cc[num15] + cc[num15];
				num11 += ido;
				num12 += ido;
				num15 += num3;
			}
			num14 += num13;
		}
		int num17;
		if (ido != 1)
		{
			if (num4 >= l1)
			{
				num9 = 0;
				num10 = num7 * num2;
				num13 = 0;
				for (int k = 1; k < num; k++)
				{
					num9 += num2;
					num10 -= num2;
					num11 = num9;
					int num12 = num10;
					num13 += ido << 1;
					int num16 = num13;
					for (int i = 0; i < l1; i++)
					{
						num14 = num11;
						int num15 = num12;
						num17 = num16;
						int num18 = num16;
						for (int j = 2; j < ido; j += 2)
						{
							num14 += 2;
							num15 += 2;
							num17 += 2;
							num18 -= 2;
							ch[num14 - 1] = cc[num17 - 1] + cc[num18 - 1];
							ch[num15 - 1] = cc[num17 - 1] - cc[num18 - 1];
							ch[num14] = cc[num17] - cc[num18];
							ch[num15] = cc[num17] + cc[num18];
						}
						num11 += ido;
						num12 += ido;
						num16 += num3;
					}
				}
			}
			else
			{
				num9 = 0;
				num10 = num7 * num2;
				num13 = 0;
				for (int k = 1; k < num; k++)
				{
					num9 += num2;
					num10 -= num2;
					num11 = num9;
					int num12 = num10;
					num13 += ido << 1;
					int num16 = num13;
					num17 = num13;
					for (int j = 2; j < ido; j += 2)
					{
						num11 += 2;
						num12 += 2;
						num16 += 2;
						num17 -= 2;
						num14 = num11;
						int num15 = num12;
						int num18 = num16;
						int num19 = num17;
						for (int i = 0; i < l1; i++)
						{
							ch[num14 - 1] = cc[num18 - 1] + cc[num19 - 1];
							ch[num15 - 1] = cc[num18 - 1] - cc[num19 - 1];
							ch[num14] = cc[num18] - cc[num19];
							ch[num15] = cc[num18] + cc[num19];
							num14 += ido;
							num15 += ido;
							num18 += num3;
							num19 += num3;
						}
					}
				}
			}
		}
		float num20 = 1f;
		float num21 = 0f;
		num9 = 0;
		num17 = (num10 = num7 * idl1);
		num11 = (ip - 1) * idl1;
		for (int m = 1; m < num; m++)
		{
			num9 += idl1;
			num10 -= idl1;
			float num22 = num5 * num20 - num6 * num21;
			num21 = num5 * num21 + num6 * num20;
			num20 = num22;
			int num12 = num9;
			num14 = num10;
			int num15 = 0;
			num13 = idl1;
			int num16 = num11;
			for (int n = 0; n < idl1; n++)
			{
				c2[num12++] = ch2[num15++] + num20 * ch2[num13++];
				c2[num14++] = num21 * ch2[num16++];
			}
			float num23 = num20;
			float num24 = num21;
			float num25 = num20;
			float num26 = num21;
			num15 = idl1;
			num13 = num17 - idl1;
			for (int k = 2; k < num; k++)
			{
				num15 += idl1;
				num13 -= idl1;
				float num27 = num23 * num25 - num24 * num26;
				num26 = num23 * num26 + num24 * num25;
				num25 = num27;
				num12 = num9;
				num14 = num10;
				int num18 = num15;
				int num19 = num13;
				for (int n = 0; n < idl1; n++)
				{
					c2[num12] += num25 * ch2[num18++];
					num12++;
					c2[num14] += num26 * ch2[num19++];
					num14++;
				}
			}
		}
		num9 = 0;
		for (int k = 1; k < num; k++)
		{
			num9 += idl1;
			num10 = num9;
			for (int n = 0; n < idl1; n++)
			{
				ch2[n] += ch2[num10++];
			}
		}
		num9 = 0;
		num10 = num7 * num2;
		for (int k = 1; k < num; k++)
		{
			num9 += num2;
			num10 -= num2;
			num11 = num9;
			int num12 = num10;
			for (int i = 0; i < l1; i++)
			{
				ch[num11] = c1[num11] - c1[num12];
				ch[num12] = c1[num11] + c1[num12];
				num11 += ido;
				num12 += ido;
			}
		}
		if (ido != 1)
		{
			if (num4 >= l1)
			{
				num9 = 0;
				num10 = num7 * num2;
				for (int k = 1; k < num; k++)
				{
					num9 += num2;
					num10 -= num2;
					num11 = num9;
					int num12 = num10;
					for (int i = 0; i < l1; i++)
					{
						num14 = num11;
						int num15 = num12;
						for (int j = 2; j < ido; j += 2)
						{
							num14 += 2;
							num15 += 2;
							ch[num14 - 1] = c1[num14 - 1] - c1[num15];
							ch[num15 - 1] = c1[num14 - 1] + c1[num15];
							ch[num14] = c1[num14] + c1[num15 - 1];
							ch[num15] = c1[num14] - c1[num15 - 1];
						}
						num11 += ido;
						num12 += ido;
					}
				}
			}
			else
			{
				num9 = 0;
				num10 = num7 * num2;
				for (int k = 1; k < num; k++)
				{
					num9 += num2;
					num10 -= num2;
					num11 = num9;
					int num12 = num10;
					for (int j = 2; j < ido; j += 2)
					{
						num11 += 2;
						num12 += 2;
						num14 = num11;
						int num15 = num12;
						for (int i = 0; i < l1; i++)
						{
							ch[num14 - 1] = c1[num14 - 1] - c1[num15];
							ch[num15 - 1] = c1[num14 - 1] + c1[num15];
							ch[num14] = c1[num14] + c1[num15 - 1];
							ch[num15] = c1[num14] - c1[num15 - 1];
							num14 += ido;
							num15 += ido;
						}
					}
				}
			}
		}
		if (ido == 1)
		{
			return;
		}
		for (int n = 0; n < idl1; n++)
		{
			c2[n] = ch2[n];
		}
		num9 = 0;
		for (int k = 1; k < ip; k++)
		{
			num10 = (num9 += num2);
			for (int i = 0; i < l1; i++)
			{
				c1[num10] = ch[num10];
				num10 += ido;
			}
		}
		int num28;
		if (num4 <= l1)
		{
			num28 = -ido - 1;
			num9 = 0;
			for (int k = 1; k < ip; k++)
			{
				num28 += ido;
				num9 += num2;
				int num29 = num28;
				num10 = num9;
				for (int j = 2; j < ido; j += 2)
				{
					num10 += 2;
					num29 += 2;
					num11 = num10;
					for (int i = 0; i < l1; i++)
					{
						c1[num11 - 1] = wa[index + num29 - 1] * ch[num11 - 1] - wa[index + num29] * ch[num11];
						c1[num11] = wa[index + num29 - 1] * ch[num11] + wa[index + num29] * ch[num11 - 1];
						num11 += ido;
					}
				}
			}
			return;
		}
		num28 = -ido - 1;
		num9 = 0;
		for (int k = 1; k < ip; k++)
		{
			num28 += ido;
			num9 += num2;
			num10 = num9;
			for (int i = 0; i < l1; i++)
			{
				int num29 = num28;
				num11 = num10;
				for (int j = 2; j < ido; j += 2)
				{
					num29 += 2;
					num11 += 2;
					c1[num11 - 1] = wa[index + num29 - 1] * ch[num11 - 1] - wa[index + num29] * ch[num11];
					c1[num11] = wa[index + num29 - 1] * ch[num11] + wa[index + num29] * ch[num11 - 1];
				}
				num10 += ido;
			}
		}
	}

	private static void drftb1(int n, float[] c, float[] ch, float[] wa, int index, int[] ifac)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = ifac[1];
		int num6 = 0;
		int num7 = 1;
		int num8 = 1;
		for (int i = 0; i < num5; i++)
		{
			num2 = ifac[i + 2];
			num = num2 * num7;
			num3 = n / num;
			num4 = num3 * num7;
			switch (num2)
			{
			case 4:
			{
				int num9 = num8 + num3;
				int num10 = num9 + num3;
				if (num6 != 0)
				{
					dradb4(num3, num7, ch, c, wa, index + num8 - 1, wa, index + num9 - 1, wa, index + num10 - 1);
				}
				else
				{
					dradb4(num3, num7, c, ch, wa, index + num8 - 1, wa, index + num9 - 1, wa, index + num10 - 1);
				}
				num6 = 1 - num6;
				break;
			}
			case 2:
				if (num6 != 0)
				{
					dradb2(num3, num7, ch, c, wa, index + num8 - 1);
				}
				else
				{
					dradb2(num3, num7, c, ch, wa, index + num8 - 1);
				}
				num6 = 1 - num6;
				break;
			case 3:
			{
				int num9 = num8 + num3;
				if (num6 != 0)
				{
					dradb3(num3, num7, ch, c, wa, index + num8 - 1, wa, index + num9 - 1);
				}
				else
				{
					dradb3(num3, num7, c, ch, wa, index + num8 - 1, wa, index + num9 - 1);
				}
				num6 = 1 - num6;
				break;
			}
			default:
				if (num6 != 0)
				{
					dradbg(num3, num2, num7, num4, ch, ch, ch, c, c, wa, index + num8 - 1);
				}
				else
				{
					dradbg(num3, num2, num7, num4, c, c, c, ch, ch, wa, index + num8 - 1);
				}
				if (num3 == 1)
				{
					num6 = 1 - num6;
				}
				num7 = num;
				num8 += (num2 - 1) * num3;
				break;
			}
			num7 = num;
			num8 += (num2 - 1) * num3;
		}
		if (num6 != 0)
		{
			for (int j = 0; j < n; j++)
			{
				c[j] = ch[j];
			}
		}
	}
}
