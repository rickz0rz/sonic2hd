using System.Collections.Generic;
using System.IO;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

public static class RingPositions
{
	private const int Horizontal = 0;

	private const int Vertical = 1;

	public static IReadOnlyCollection<Vector2i> FromFile(string filename)
	{
		using FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
		return FromStream(stream);
	}

	public static IReadOnlyCollection<Vector2i> FromStream(Stream stream)
	{
		List<Vector2i> list = new List<Vector2i>();
		byte[] array = new byte[4];
		while (stream.Read(array, 0, 2) == 2 && (array[0] != byte.MaxValue || array[1] != byte.MaxValue) && stream.Read(array, 2, 2) == 2)
		{
			int num = (array[0] << 8) | array[1];
			int num2 = ((array[2] & 0xF) << 8) | array[3];
			int num3 = ((array[2] >> 4) & 7) + 1;
			int num4 = array[2] >> 7;
			for (int i = 0; i < num3; i++)
			{
				list.Add(new Vector2i(num, num2));
				switch (num4)
				{
				case 0:
					num += 24;
					break;
				case 1:
					num2 += 24;
					break;
				}
			}
		}
		return list.ToArray();
	}
}
