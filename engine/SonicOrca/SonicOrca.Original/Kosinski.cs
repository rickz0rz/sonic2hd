using System.Collections.Generic;
using System.IO;

namespace SonicOrca.Original;

public static class Kosinski
{
	public static byte[] Decompress(byte[] input)
	{
		using MemoryStream input2 = new MemoryStream(input);
		return Decompress(input2);
	}

	public static byte[] Decompress(Stream input)
	{
		using MemoryStream memoryStream = new MemoryStream();
		Decompress(input, memoryStream);
		return memoryStream.ToArray();
	}

	public static int Decompress(Stream input, Stream output)
	{
		BitReader bitReader = new BitReader(input, 2);
		BinaryReader binaryReader = new BinaryReader(input);
		List<byte> list = new List<byte>();
		while (true)
		{
			if (bitReader.ReadBit())
			{
				byte b = binaryReader.ReadByte();
				output.WriteByte(b);
				list.Add(b);
				continue;
			}
			int num4;
			int num3;
			if (bitReader.ReadBit())
			{
				int num = binaryReader.ReadByte();
				int num2 = binaryReader.ReadByte();
				num3 = num2 & 7;
				if (num3 == 0)
				{
					num3 = binaryReader.ReadByte();
					switch (num3)
					{
					case 1:
						continue;
					case 0:
						return list.Count;
					}
				}
				else
				{
					num3++;
				}
				num4 = (short)(0xE000 | ((num2 & 0xF8) << 5) | num);
			}
			else
			{
				int num5 = (bitReader.ReadBit() ? 1 : 0);
				int num6 = (bitReader.ReadBit() ? 1 : 0);
				num3 = ((num5 << 1) | num6) + 1;
				num4 = (short)(0xFF00 | binaryReader.ReadByte());
			}
			num3++;
			int num7 = list.Count + num4;
			for (int i = 0; i < num3; i++)
			{
				output.WriteByte(list[num7]);
				list.Add(list[num7]);
				num7++;
			}
		}
	}
}
