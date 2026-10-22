using System;
using csogg;

namespace csvorbis;

internal class StaticCodeBook
{
	internal int dim;

	internal int entries;

	internal int[] lengthlist;

	internal int maptype;

	internal int q_min;

	internal int q_delta;

	internal int q_quant;

	internal int q_sequencep;

	internal int[] quantlist;

	internal EncodeAuxNearestMatch nearest_tree;

	internal EncodeAuxThreshMatch thresh_tree;

	private static int VQ_FMAN = 21;

	private static int VQ_FEXP_BIAS = 768;

	internal StaticCodeBook()
	{
	}

	internal StaticCodeBook(int dim, int entries, int[] lengthlist, int maptype, int q_min, int q_delta, int q_quant, int q_sequencep, int[] quantlist, object nearest_tree, object thresh_tree)
		: this()
	{
		this.dim = dim;
		this.entries = entries;
		this.lengthlist = lengthlist;
		this.maptype = maptype;
		this.q_min = q_min;
		this.q_delta = q_delta;
		this.q_quant = q_quant;
		this.q_sequencep = q_sequencep;
		this.quantlist = quantlist;
	}

	internal int pack(csBuffer opb)
	{
		bool flag = false;
		opb.write(5653314, 24);
		opb.write(dim, 16);
		opb.write(entries, 24);
		int i;
		for (i = 1; i < entries && lengthlist[i] >= lengthlist[i - 1]; i++)
		{
		}
		if (i == entries)
		{
			flag = true;
		}
		if (flag)
		{
			int num = 0;
			opb.write(1, 1);
			opb.write(lengthlist[0] - 1, 5);
			for (i = 1; i < entries; i++)
			{
				int num2 = lengthlist[i];
				int num3 = lengthlist[i - 1];
				if (num2 > num3)
				{
					for (int j = num3; j < num2; j++)
					{
						opb.write(i - num, ilog(entries - num));
						num = i;
					}
				}
			}
			opb.write(i - num, ilog(entries - num));
		}
		else
		{
			opb.write(0, 1);
			for (i = 0; i < entries && lengthlist[i] != 0; i++)
			{
			}
			if (i == entries)
			{
				opb.write(0, 1);
				for (i = 0; i < entries; i++)
				{
					opb.write(lengthlist[i] - 1, 5);
				}
			}
			else
			{
				opb.write(1, 1);
				for (i = 0; i < entries; i++)
				{
					if (lengthlist[i] == 0)
					{
						opb.write(0, 1);
						continue;
					}
					opb.write(1, 1);
					opb.write(lengthlist[i] - 1, 5);
				}
			}
		}
		opb.write(maptype, 4);
		switch (maptype)
		{
		case 1:
		case 2:
		{
			if (quantlist == null)
			{
				return -1;
			}
			opb.write(q_min, 32);
			opb.write(q_delta, 32);
			opb.write(q_quant - 1, 4);
			opb.write(q_sequencep, 1);
			int num4 = 0;
			switch (maptype)
			{
			case 1:
				num4 = maptype1_quantvals();
				break;
			case 2:
				num4 = entries * dim;
				break;
			}
			for (i = 0; i < num4; i++)
			{
				opb.write(Math.Abs(quantlist[i]), q_quant);
			}
			break;
		}
		default:
			return -1;
		case 0:
			break;
		}
		return 0;
	}

	internal int unpack(csBuffer opb)
	{
		if (opb.read(24) != 5653314)
		{
			clear();
			return -1;
		}
		dim = opb.read(16);
		entries = opb.read(24);
		if (entries == -1)
		{
			clear();
			return -1;
		}
		switch (opb.read(1))
		{
		case 0:
		{
			lengthlist = new int[entries];
			if (opb.read(1) != 0)
			{
				for (int num2 = 0; num2 < entries; num2++)
				{
					if (opb.read(1) != 0)
					{
						int num5 = opb.read(5);
						if (num5 == -1)
						{
							clear();
							return -1;
						}
						lengthlist[num2] = num5 + 1;
					}
					else
					{
						lengthlist[num2] = 0;
					}
				}
				break;
			}
			for (int num2 = 0; num2 < entries; num2++)
			{
				int num6 = opb.read(5);
				if (num6 == -1)
				{
					clear();
					return -1;
				}
				lengthlist[num2] = num6 + 1;
			}
			break;
		}
		case 1:
		{
			int num = opb.read(5) + 1;
			lengthlist = new int[entries];
			int num2 = 0;
			while (num2 < entries)
			{
				int num3 = opb.read(ilog(entries - num2));
				if (num3 == -1)
				{
					clear();
					return -1;
				}
				int num4 = 0;
				while (num4 < num3)
				{
					lengthlist[num2] = num;
					num4++;
					num2++;
				}
				num++;
			}
			break;
		}
		default:
			return -1;
		}
		switch (maptype = opb.read(4))
		{
		case 1:
		case 2:
		{
			q_min = opb.read(32);
			q_delta = opb.read(32);
			q_quant = opb.read(4) + 1;
			q_sequencep = opb.read(1);
			int num7 = 0;
			switch (maptype)
			{
			case 1:
				num7 = maptype1_quantvals();
				break;
			case 2:
				num7 = entries * dim;
				break;
			}
			quantlist = new int[num7];
			for (int num2 = 0; num2 < num7; num2++)
			{
				quantlist[num2] = opb.read(q_quant);
			}
			if (quantlist[num7 - 1] == -1)
			{
				clear();
				return -1;
			}
			break;
		}
		default:
			clear();
			return -1;
		case 0:
			break;
		}
		return 0;
	}

	internal int maptype1_quantvals()
	{
		int num = (int)Math.Floor(Math.Pow(entries, 1.0 / (double)dim));
		while (true)
		{
			int num2 = 1;
			int num3 = 1;
			for (int i = 0; i < dim; i++)
			{
				num2 *= num;
				num3 *= num + 1;
			}
			if (num2 <= entries && num3 > entries)
			{
				break;
			}
			num = ((num2 <= entries) ? (num + 1) : (num - 1));
		}
		return num;
	}

	internal void clear()
	{
	}

	internal float[] unquantize()
	{
		if (maptype == 1 || maptype == 2)
		{
			float num = float32_unpack(q_min);
			float num2 = float32_unpack(q_delta);
			float[] array = new float[entries * dim];
			switch (maptype)
			{
			case 1:
			{
				int num4 = maptype1_quantvals();
				for (int k = 0; k < entries; k++)
				{
					float num5 = 0f;
					int num6 = 1;
					for (int l = 0; l < dim; l++)
					{
						int num7 = k / num6 % num4;
						float value2 = quantlist[num7];
						value2 = Math.Abs(value2) * num2 + num + num5;
						if (q_sequencep != 0)
						{
							num5 = value2;
						}
						array[k * dim + l] = value2;
						num6 *= num4;
					}
				}
				break;
			}
			case 2:
			{
				for (int i = 0; i < entries; i++)
				{
					float num3 = 0f;
					for (int j = 0; j < dim; j++)
					{
						float value = quantlist[i * dim + j];
						value = Math.Abs(value) * num2 + num + num3;
						if (q_sequencep != 0)
						{
							num3 = value;
						}
						array[i * dim + j] = value;
					}
				}
				break;
			}
			}
			return array;
		}
		return null;
	}

	internal static int ilog(int v)
	{
		int num = 0;
		while (v != 0)
		{
			num++;
			v >>>= 1;
		}
		return num;
	}

	internal static long float32_pack(float val)
	{
		uint num = 0u;
		if (val < 0f)
		{
			num = 2147483648u;
			val = 0f - val;
		}
		int num2 = (int)Math.Floor(Math.Log(val) / Math.Log(2.0));
		int num3 = (int)Math.Round(Math.Pow(val, VQ_FMAN - 1 - num2));
		num2 = num2 + VQ_FEXP_BIAS << VQ_FMAN;
		return (int)(num | (uint)num2) | num3;
	}

	internal static float float32_unpack(int val)
	{
		float num = val & 0x1FFFFF;
		float num2 = (uint)(val & 0x7FE00000) >> VQ_FMAN;
		if ((val & 0x80000000u) != 0L)
		{
			num = 0f - num;
		}
		return ldexp(num, (int)num2 - (VQ_FMAN - 1) - VQ_FEXP_BIAS);
	}

	internal static float ldexp(float foo, int e)
	{
		return (float)((double)foo * Math.Pow(2.0, e));
	}
}
