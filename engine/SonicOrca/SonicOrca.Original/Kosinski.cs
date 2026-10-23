using System.Collections.Generic;
using System.IO;

namespace SonicOrca.Original;

/// <summary>
/// Decompresses data in the Kosinski format of the original game.
/// </summary>
public static class Kosinski
{
	public static byte[] Decompress(byte[] input)
	{
		using MemoryStream inputStream = new MemoryStream(input);
		return Decompress(inputStream);
	}

	public static byte[] Decompress(Stream input)
	{
		using MemoryStream memoryStream = new MemoryStream();
		Decompress(input, memoryStream);
		return memoryStream.ToArray();
	}

	/// <summary>
	/// Decompresses <paramref name="input"/> into <paramref name="output"/> and returns the number of bytes written.
	/// </summary>
	public static int Decompress(Stream input, Stream output)
	{
		// The description field bits come from 16-bit blocks. The data bytes come from the same stream.
		BitReader bitReader = new BitReader(input, 2);
		BinaryReader binaryReader = new BinaryReader(input);
		List<byte> history = new List<byte>();
		while (true)
		{
			// Bit 1: copy one byte without change.
			if (bitReader.ReadBit())
			{
				byte value = binaryReader.ReadByte();
				output.WriteByte(value);
				history.Add(value);
				continue;
			}
			int offset;
			int count;
			if (bitReader.ReadBit())
			{
				// Bits 01: a full match with a 13-bit offset.
				int low = binaryReader.ReadByte();
				int high = binaryReader.ReadByte();
				count = high & 7;
				if (count == 0)
				{
					// A count of zero means that the next byte holds the count, or an end or skip marker.
					count = binaryReader.ReadByte();
					switch (count)
					{
						case 1:
							continue;
						case 0:
							return history.Count;
					}
				}
				else
				{
					count++;
				}
				offset = (short)(0xE000 | ((high & 0xF8) << 5) | low);
			}
			else
			{
				// Bits 00: an inline match with a 2-bit count and an 8-bit offset.
				int countHigh = (bitReader.ReadBit() ? 1 : 0);
				int countLow = (bitReader.ReadBit() ? 1 : 0);
				count = ((countHigh << 1) | countLow) + 1;
				offset = (short)(0xFF00 | binaryReader.ReadByte());
			}
			count++;
			int source = history.Count + offset;
			for (int i = 0; i < count; i++)
			{
				output.WriteByte(history[source]);
				history.Add(history[source]);
				source++;
			}
		}
	}
}
