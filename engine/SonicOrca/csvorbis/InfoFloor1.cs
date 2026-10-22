using System;

namespace csvorbis;

internal class InfoFloor1
{
	private const int VIF_POSIT = 63;

	private const int VIF_CLASS = 16;

	private const int VIF_PARTS = 31;

	internal int partitions;

	internal int[] partitionclass = new int[31];

	internal int[] class_dim = new int[16];

	internal int[] class_subs = new int[16];

	internal int[] class_book = new int[16];

	internal int[][] class_subbook = new int[16][];

	internal int mult;

	internal int[] postlist = new int[65];

	internal float maxover;

	internal float maxunder;

	internal float maxerr;

	internal int twofitminsize;

	internal int twofitminused;

	internal int twofitweight;

	internal float twofitatten;

	internal int unusedminsize;

	internal int unusedmin_n;

	internal int n;

	internal InfoFloor1()
	{
		for (int i = 0; i < class_subbook.Length; i++)
		{
			class_subbook[i] = new int[8];
		}
	}

	internal void free()
	{
		partitionclass = null;
		class_dim = null;
		class_subs = null;
		class_book = null;
		class_subbook = null;
		postlist = null;
	}

	internal object copy_info()
	{
		InfoFloor1 infoFloor = new InfoFloor1();
		infoFloor.partitions = partitions;
		Array.Copy(partitionclass, 0, infoFloor.partitionclass, 0, 31);
		Array.Copy(class_dim, 0, infoFloor.class_dim, 0, 16);
		Array.Copy(class_subs, 0, infoFloor.class_subs, 0, 16);
		Array.Copy(class_book, 0, infoFloor.class_book, 0, 16);
		for (int i = 0; i < 16; i++)
		{
			Array.Copy(class_subbook[i], 0, infoFloor.class_subbook[i], 0, 8);
		}
		infoFloor.mult = mult;
		Array.Copy(postlist, 0, infoFloor.postlist, 0, 65);
		infoFloor.maxover = maxover;
		infoFloor.maxunder = maxunder;
		infoFloor.maxerr = maxerr;
		infoFloor.twofitminsize = twofitminsize;
		infoFloor.twofitminused = twofitminused;
		infoFloor.twofitweight = twofitweight;
		infoFloor.twofitatten = twofitatten;
		infoFloor.unusedminsize = unusedminsize;
		infoFloor.unusedmin_n = unusedmin_n;
		infoFloor.n = n;
		return infoFloor;
	}
}
