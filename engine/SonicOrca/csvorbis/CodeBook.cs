using System.Runtime.CompilerServices;
using csogg;

namespace csvorbis;

internal class CodeBook
{
	internal int dim;

	internal int entries;

	internal StaticCodeBook c = new StaticCodeBook();

	internal float[] valuelist;

	internal int[] codelist;

	internal DecodeAux decode_tree;

	internal int[] t = new int[15];

	internal int encode(int a, csBuffer b)
	{
		b.write(codelist[a], c.lengthlist[a]);
		return c.lengthlist[a];
	}

	internal int errorv(float[] a)
	{
		int num = best(a, 1);
		for (int i = 0; i < dim; i++)
		{
			a[i] = valuelist[num * dim + i];
		}
		return num;
	}

	internal int encodev(int best, float[] a, csBuffer b)
	{
		for (int i = 0; i < dim; i++)
		{
			a[i] = valuelist[best * dim + i];
		}
		return encode(best, b);
	}

	internal int encodevs(float[] a, csBuffer b, int step, int addmul)
	{
		int a2 = besterror(a, step, addmul);
		return encode(a2, b);
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	internal int decodevs_add(float[] a, int offset, csBuffer b, int n)
	{
		int num = n / dim;
		if (t.Length < num)
		{
			t = new int[num];
		}
		int i;
		for (i = 0; i < num; i++)
		{
			int num2 = decode(b);
			if (num2 == -1)
			{
				return -1;
			}
			t[i] = num2 * dim;
		}
		i = 0;
		int num3 = 0;
		while (i < dim)
		{
			for (int j = 0; j < num; j++)
			{
				a[offset + num3 + j] += valuelist[t[j] + i];
			}
			i++;
			num3 += num;
		}
		return 0;
	}

	internal int decodev_add(float[] a, int offset, csBuffer b, int n)
	{
		if (dim > 8)
		{
			int num = 0;
			while (num < n)
			{
				int num2 = decode(b);
				if (num2 == -1)
				{
					return -1;
				}
				int num3 = num2 * dim;
				int num4 = 0;
				while (num4 < dim)
				{
					a[offset + num++] += valuelist[num3 + num4++];
				}
			}
		}
		else
		{
			int num = 0;
			while (num < n)
			{
				int num2 = decode(b);
				if (num2 == -1)
				{
					return -1;
				}
				int num3 = num2 * dim;
				int num4 = 0;
				for (int i = 0; i < dim; i++)
				{
					a[offset + num++] += valuelist[num3 + num4++];
				}
			}
		}
		return 0;
	}

	internal int decodev_set(float[] a, int offset, csBuffer b, int n)
	{
		int num = 0;
		while (num < n)
		{
			int num2 = decode(b);
			if (num2 == -1)
			{
				return -1;
			}
			int num3 = num2 * dim;
			int num4 = 0;
			while (num4 < dim)
			{
				a[offset + num++] = valuelist[num3 + num4++];
			}
		}
		return 0;
	}

	internal int decodevv_add(float[][] a, int offset, int ch, csBuffer b, int n)
	{
		int num = 0;
		int num2 = offset / ch;
		while (num2 < (offset + n) / ch)
		{
			int num3 = decode(b);
			if (num3 == -1)
			{
				return -1;
			}
			int num4 = num3 * dim;
			for (int i = 0; i < dim; i++)
			{
				a[num][num2] += valuelist[num4 + i];
				num++;
				if (num == ch)
				{
					num = 0;
					num2++;
				}
			}
		}
		return 0;
	}

	internal int decode(csBuffer b)
	{
		int num = 0;
		DecodeAux decodeAux = decode_tree;
		int num2 = b.look(decodeAux.tabn);
		if (num2 >= 0)
		{
			num = decodeAux.tab[num2];
			b.adv(decodeAux.tabl[num2]);
			if (num <= 0)
			{
				return -num;
			}
		}
		do
		{
			switch (b.read1())
			{
			case 0:
				num = decodeAux.ptr0[num];
				break;
			case 1:
				num = decodeAux.ptr1[num];
				break;
			default:
				return -1;
			}
		}
		while (num > 0);
		return -num;
	}

	internal int decodevs(float[] a, int index, csBuffer b, int step, int addmul)
	{
		int num = decode(b);
		if (num == -1)
		{
			return -1;
		}
		switch (addmul)
		{
		case -1:
		{
			int num4 = 0;
			int num5 = 0;
			while (num4 < dim)
			{
				a[index + num5] = valuelist[num * dim + num4];
				num4++;
				num5 += step;
			}
			break;
		}
		case 0:
		{
			int num6 = 0;
			int num7 = 0;
			while (num6 < dim)
			{
				a[index + num7] += valuelist[num * dim + num6];
				num6++;
				num7 += step;
			}
			break;
		}
		case 1:
		{
			int num2 = 0;
			int num3 = 0;
			while (num2 < dim)
			{
				a[index + num3] *= valuelist[num * dim + num2];
				num2++;
				num3 += step;
			}
			break;
		}
		}
		return num;
	}

	internal int best(float[] a, int step)
	{
		EncodeAuxNearestMatch nearest_tree = c.nearest_tree;
		EncodeAuxThreshMatch thresh_tree = c.thresh_tree;
		int num = 0;
		if (thresh_tree != null)
		{
			int num2 = 0;
			int num3 = 0;
			int num4 = step * (dim - 1);
			while (num3 < dim)
			{
				int i;
				for (i = 0; i < thresh_tree.threshvals - 1 && !(a[num4] < thresh_tree.quantthresh[i]); i++)
				{
				}
				num2 = num2 * thresh_tree.quantvals + thresh_tree.quantmap[i];
				num3++;
				num4 -= step;
			}
			if (c.lengthlist[num2] > 0)
			{
				return num2;
			}
		}
		if (nearest_tree != null)
		{
			do
			{
				double num5 = 0.0;
				int num6 = nearest_tree.p[num];
				int num7 = nearest_tree.q[num];
				int num8 = 0;
				int num9 = 0;
				while (num8 < dim)
				{
					num5 += (double)(valuelist[num6 + num8] - valuelist[num7 + num8]) * ((double)a[num9] - (double)(valuelist[num6 + num8] + valuelist[num7 + num8]) * 0.5);
					num8++;
					num9 += step;
				}
				num = ((!(num5 > 0.0)) ? (-nearest_tree.ptr1[num]) : (-nearest_tree.ptr0[num]));
			}
			while (num > 0);
			return -num;
		}
		int num10 = -1;
		float num11 = 0f;
		int num12 = 0;
		for (int j = 0; j < entries; j++)
		{
			if (c.lengthlist[j] > 0)
			{
				float num13 = dist(dim, valuelist, num12, a, step);
				if (num10 == -1 || num13 < num11)
				{
					num11 = num13;
					num10 = j;
				}
			}
			num12 += dim;
		}
		return num10;
	}

	internal int besterror(float[] a, int step, int addmul)
	{
		int num = best(a, step);
		switch (addmul)
		{
		case 0:
		{
			int num5 = 0;
			int num6 = 0;
			while (num5 < dim)
			{
				a[num6] -= valuelist[num * dim + num5];
				num5++;
				num6 += step;
			}
			break;
		}
		case 1:
		{
			int num2 = 0;
			int num3 = 0;
			while (num2 < dim)
			{
				float num4 = valuelist[num * dim + num2];
				if (num4 == 0f)
				{
					a[num3] = 0f;
				}
				else
				{
					a[num3] /= num4;
				}
				num2++;
				num3 += step;
			}
			break;
		}
		}
		return num;
	}

	internal void clear()
	{
	}

	internal static float dist(int el, float[] rref, int index, float[] b, int step)
	{
		float num = 0f;
		for (int i = 0; i < el; i++)
		{
			float num2 = rref[index + i] - b[i * step];
			num += num2 * num2;
		}
		return num;
	}

	internal int init_decode(StaticCodeBook s)
	{
		c = s;
		entries = s.entries;
		dim = s.dim;
		valuelist = s.unquantize();
		decode_tree = make_decode_tree();
		if (decode_tree == null)
		{
			clear();
			return -1;
		}
		return 0;
	}

	internal static int[] make_words(int[] l, int n)
	{
		int[] array = new int[33];
		int[] array2 = new int[n];
		for (int i = 0; i < n; i++)
		{
			int num = l[i];
			if (num <= 0)
			{
				continue;
			}
			int num2 = array[num];
			if (num < 32 && num2 >>> num != 0)
			{
				return null;
			}
			array2[i] = num2;
			for (int num3 = num; num3 > 0; num3--)
			{
				if ((array[num3] & 1) != 0)
				{
					if (num3 == 1)
					{
						array[1]++;
					}
					else
					{
						array[num3] = array[num3 - 1] << 1;
					}
					break;
				}
				array[num3]++;
			}
			for (int j = num + 1; j < 33 && (uint)(array[j] >>> 1) == num2; j++)
			{
				num2 = array[j];
				array[j] = array[j - 1] << 1;
			}
		}
		for (int k = 0; k < n; k++)
		{
			int num4 = 0;
			for (int m = 0; m < l[k]; m++)
			{
				num4 <<= 1;
				num4 |= (array2[k] >>> m) & 1;
			}
			array2[k] = num4;
		}
		return array2;
	}

	internal DecodeAux make_decode_tree()
	{
		int num = 0;
		DecodeAux decodeAux = new DecodeAux();
		int[] array = (decodeAux.ptr0 = new int[entries * 2]);
		int[] array2 = (decodeAux.ptr1 = new int[entries * 2]);
		int[] array3 = make_words(c.lengthlist, c.entries);
		if (array3 == null)
		{
			return null;
		}
		decodeAux.aux = entries * 2;
		for (int i = 0; i < entries; i++)
		{
			if (c.lengthlist[i] <= 0)
			{
				continue;
			}
			int num2 = 0;
			int j;
			for (j = 0; j < c.lengthlist[i] - 1; j++)
			{
				if (((array3[i] >>> j) & 1) == 0)
				{
					if (array[num2] == 0)
					{
						num = (array[num2] = num + 1);
					}
					num2 = array[num2];
				}
				else
				{
					if (array2[num2] == 0)
					{
						num = (array2[num2] = num + 1);
					}
					num2 = array2[num2];
				}
			}
			if (((array3[i] >>> j) & 1) == 0)
			{
				array[num2] = -i;
			}
			else
			{
				array2[num2] = -i;
			}
		}
		decodeAux.tabn = ilog(entries) - 4;
		if (decodeAux.tabn < 5)
		{
			decodeAux.tabn = 5;
		}
		int num3 = 1 << decodeAux.tabn;
		decodeAux.tab = new int[num3];
		decodeAux.tabl = new int[num3];
		for (int k = 0; k < num3; k++)
		{
			int num4 = 0;
			int num5 = 0;
			for (num5 = 0; num5 < decodeAux.tabn && (num4 > 0 || num5 == 0); num5++)
			{
				num4 = (((k & (1 << num5)) == 0) ? array[num4] : array2[num4]);
			}
			decodeAux.tab[k] = num4;
			decodeAux.tabl[k] = num5;
		}
		return decodeAux;
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
}
