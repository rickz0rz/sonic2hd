using System;
using System.Collections.Generic;

namespace SonicOrca.Original;

/// <summary>
/// Decompresses Enigma data: the format of the plane maps of the original game, for example the logo at the end of
/// the credits. The output is a list of 16-bit plane entries: priority (bit 15), palette line (bits 13 and 14),
/// vertical flip (bit 12), horizontal flip (bit 11) and tile (bits 0 to 10).
/// </summary>
public static class Enigma
{
	/// <summary>Decompresses Enigma data.</summary>
	/// <param name="input">The compressed data.</param>
	/// <param name="baseEntry">A value that the original adds to each entry, for example the first tile of the art.</param>
	public static ushort[] Decompress(byte[] input, ushort baseEntry = 0)
	{
		if (input.Length < 6)
		{
			throw new ArgumentException("The Enigma data has no header.", nameof(input));
		}
		int tileBits = input[0];
		int flagMask = input[1];
		ushort incrementing = (ushort)(((input[2] << 8) | input[3]) + baseEntry);
		ushort common = (ushort)(((input[4] << 8) | input[5]) + baseEntry);
		BitStream bits = new BitStream(input, 6);
		List<ushort> output = new List<ushort>();

		// An entry in the stream: the flags that the mask selects, then the tile
		ushort ReadEntry()
		{
			int entry = 0;
			for (int flag = 4; flag >= 0; flag--)
			{
				if ((flagMask & (1 << flag)) != 0 && bits.Read(1) != 0)
				{
					entry |= 1 << (11 + flag);
				}
			}
			entry |= bits.Read(tileBits);
			return (ushort)(entry + baseEntry);
		}

		while (true)
		{
			if (bits.Read(1) == 0)
			{
				// 00: the incrementing entry, which goes up after each copy. 01: the common entry.
				bool useCommon = bits.Read(1) != 0;
				int count = bits.Read(4) + 1;
				for (int i = 0; i < count; i++)
				{
					output.Add(useCommon ? common : incrementing++);
				}
				continue;
			}
			int mode = bits.Read(2);
			int repeat = bits.Read(4);
			if (mode == 3 && repeat == 0xF)
			{
				return output.ToArray();
			}
			if (mode == 3)
			{
				// 111: separate entries
				for (int i = 0; i <= repeat; i++)
				{
					output.Add(ReadEntry());
				}
				continue;
			}
			// 100: the same entry. 101: an entry that goes up. 110: an entry that goes down.
			ushort value = ReadEntry();
			int step = mode == 0 ? 0 : mode == 1 ? 1 : -1;
			for (int i = 0; i <= repeat; i++)
			{
				output.Add(value);
				value = (ushort)(value + step);
			}
		}
	}

	/// <summary>Reads bits from the start of each byte (the most significant bit first).</summary>
	private sealed class BitStream
	{
		private readonly byte[] _data;

		private int _position;

		public BitStream(byte[] data, int start)
		{
			_data = data;
			_position = start * 8;
		}

		public int Read(int count)
		{
			int value = 0;
			for (int i = 0; i < count; i++)
			{
				int index = _position >> 3;
				if (index >= _data.Length)
				{
					throw new ArgumentException("The Enigma data ends before its end code.");
				}
				value = (value << 1) | ((_data[index] >> (7 - (_position & 7))) & 1);
				_position++;
			}
			return value;
		}
	}
}
