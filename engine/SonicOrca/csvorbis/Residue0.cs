using System;
using System.Runtime.CompilerServices;
using csogg;

namespace csvorbis;

internal class Residue0 : FuncResidue
{
	private static int[][][] partword = new int[2][][];

	public override void pack(object vr, csBuffer opb)
	{
		InfoResidue0 infoResidue = (InfoResidue0)vr;
		int num = 0;
		opb.write(infoResidue.begin, 24);
		opb.write(infoResidue.end, 24);
		opb.write(infoResidue.grouping - 1, 24);
		opb.write(infoResidue.partitions - 1, 6);
		opb.write(infoResidue.groupbook, 8);
		for (int i = 0; i < infoResidue.partitions; i++)
		{
			if (ilog(infoResidue.secondstages[i]) > 3)
			{
				opb.write(infoResidue.secondstages[i], 3);
				opb.write(1, 1);
				opb.write(infoResidue.secondstages[i] >> 3, 5);
			}
			else
			{
				opb.write(infoResidue.secondstages[i], 4);
			}
			num += icount(infoResidue.secondstages[i]);
		}
		for (int j = 0; j < num; j++)
		{
			opb.write(infoResidue.booklist[j], 8);
		}
	}

	public override object unpack(Info vi, csBuffer opb)
	{
		int num = 0;
		InfoResidue0 infoResidue = new InfoResidue0();
		infoResidue.begin = opb.read(24);
		infoResidue.end = opb.read(24);
		infoResidue.grouping = opb.read(24) + 1;
		infoResidue.partitions = opb.read(6) + 1;
		infoResidue.groupbook = opb.read(8);
		for (int i = 0; i < infoResidue.partitions; i++)
		{
			int num2 = opb.read(3);
			if (opb.read(1) != 0)
			{
				num2 |= opb.read(5) << 3;
			}
			infoResidue.secondstages[i] = num2;
			num += icount(num2);
		}
		for (int j = 0; j < num; j++)
		{
			infoResidue.booklist[j] = opb.read(8);
		}
		if (infoResidue.groupbook >= vi.books)
		{
			free_info(infoResidue);
			return null;
		}
		for (int k = 0; k < num; k++)
		{
			if (infoResidue.booklist[k] >= vi.books)
			{
				free_info(infoResidue);
				return null;
			}
		}
		return infoResidue;
	}

	public override object look(DspState vd, InfoMode vm, object vr)
	{
		InfoResidue0 infoResidue = (InfoResidue0)vr;
		LookResidue0 lookResidue = new LookResidue0();
		int num = 0;
		int num2 = 0;
		lookResidue.info = infoResidue;
		lookResidue.map = vm.mapping;
		lookResidue.parts = infoResidue.partitions;
		lookResidue.fullbooks = vd.fullbooks;
		lookResidue.phrasebook = vd.fullbooks[infoResidue.groupbook];
		int dim = lookResidue.phrasebook.dim;
		lookResidue.partbooks = new int[lookResidue.parts][];
		for (int i = 0; i < lookResidue.parts; i++)
		{
			int num3 = ilog(infoResidue.secondstages[i]);
			if (num3 == 0)
			{
				continue;
			}
			if (num3 > num2)
			{
				num2 = num3;
			}
			lookResidue.partbooks[i] = new int[num3];
			for (int j = 0; j < num3; j++)
			{
				if ((infoResidue.secondstages[i] & (1 << j)) != 0)
				{
					lookResidue.partbooks[i][j] = infoResidue.booklist[num++];
				}
			}
		}
		lookResidue.partvals = (int)Math.Round(Math.Pow(lookResidue.parts, dim));
		lookResidue.stages = num2;
		lookResidue.decodemap = new int[lookResidue.partvals][];
		for (int k = 0; k < lookResidue.partvals; k++)
		{
			int num4 = k;
			int num5 = lookResidue.partvals / lookResidue.parts;
			lookResidue.decodemap[k] = new int[dim];
			for (int l = 0; l < dim; l++)
			{
				int num6 = num4 / num5;
				num4 -= num6 * num5;
				num5 /= lookResidue.parts;
				lookResidue.decodemap[k][l] = num6;
			}
		}
		return lookResidue;
	}

	public override void free_info(object i)
	{
	}

	public override void free_look(object i)
	{
	}

	public override int forward(Block vb, object vl, float[][] fin, int ch)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.Synchronized)]
	internal static int _01inverse(Block vb, object vl, float[][] fin, int ch, int decodepart)
	{
		LookResidue0 lookResidue = (LookResidue0)vl;
		InfoResidue0 info = lookResidue.info;
		int grouping = info.grouping;
		int dim = lookResidue.phrasebook.dim;
		int num = (info.end - info.begin) / grouping;
		int num2 = (num + dim - 1) / dim;
		if (partword.Length < ch)
		{
			partword = new int[ch][][];
			for (int i = 0; i < ch; i++)
			{
				partword[i] = new int[num2][];
			}
		}
		else
		{
			for (int i = 0; i < ch; i++)
			{
				if (partword[i] == null || partword[i].Length < num2)
				{
					partword[i] = new int[num2][];
				}
			}
		}
		for (int j = 0; j < lookResidue.stages; j++)
		{
			int num3 = 0;
			int num4 = 0;
			while (num3 < num)
			{
				if (j == 0)
				{
					for (int i = 0; i < ch; i++)
					{
						int num5 = lookResidue.phrasebook.decode(vb.opb);
						if (num5 == -1)
						{
							return 0;
						}
						partword[i][num4] = lookResidue.decodemap[num5];
						if (partword[i][num4] == null)
						{
							return 0;
						}
					}
				}
				int num6 = 0;
				while (num6 < dim && num3 < num)
				{
					for (int i = 0; i < ch; i++)
					{
						int offset = info.begin + num3 * grouping;
						if ((info.secondstages[partword[i][num4][num6]] & (1 << j)) == 0)
						{
							continue;
						}
						CodeBook codeBook = lookResidue.fullbooks[lookResidue.partbooks[partword[i][num4][num6]][j]];
						if (codeBook == null)
						{
							continue;
						}
						switch (decodepart)
						{
						case 0:
							if (codeBook.decodevs_add(fin[i], offset, vb.opb, grouping) == -1)
							{
								return 0;
							}
							break;
						case 1:
							if (codeBook.decodev_add(fin[i], offset, vb.opb, grouping) == -1)
							{
								return 0;
							}
							break;
						}
					}
					num6++;
					num3++;
				}
				num4++;
			}
		}
		return 0;
	}

	internal static int _2inverse(Block vb, object vl, float[][] fin, int ch)
	{
		LookResidue0 lookResidue = (LookResidue0)vl;
		InfoResidue0 info = lookResidue.info;
		int grouping = info.grouping;
		int dim = lookResidue.phrasebook.dim;
		int num = (info.end - info.begin) / grouping;
		int[][] array = new int[(num + dim - 1) / dim][];
		for (int i = 0; i < lookResidue.stages; i++)
		{
			int num2 = 0;
			int num3 = 0;
			while (num2 < num)
			{
				if (i == 0)
				{
					int num4 = lookResidue.phrasebook.decode(vb.opb);
					if (num4 == -1)
					{
						return 0;
					}
					array[num3] = lookResidue.decodemap[num4];
					if (array[num3] == null)
					{
						return 0;
					}
				}
				int num5 = 0;
				while (num5 < dim && num2 < num)
				{
					int offset = info.begin + num2 * grouping;
					if ((info.secondstages[array[num3][num5]] & (1 << i)) != 0)
					{
						CodeBook codeBook = lookResidue.fullbooks[lookResidue.partbooks[array[num3][num5]][i]];
						if (codeBook != null && codeBook.decodevv_add(fin, offset, ch, vb.opb, grouping) == -1)
						{
							return 0;
						}
					}
					num5++;
					num2++;
				}
				num3++;
			}
		}
		return 0;
	}

	public override int inverse(Block vb, object vl, float[][] fin, int[] nonzero, int ch)
	{
		int num = 0;
		for (int i = 0; i < ch; i++)
		{
			if (nonzero[i] != 0)
			{
				fin[num++] = fin[i];
			}
		}
		if (num != 0)
		{
			return _01inverse(vb, vl, fin, num, 0);
		}
		return 0;
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

	internal static int icount(int v)
	{
		int num = 0;
		while (v != 0)
		{
			num += v & 1;
			v >>>= 1;
		}
		return num;
	}
}
