using System;
using System.IO;

namespace SonicOrca.Original;

/// <summary>
/// Decompresses data in the Nemesis format of the original game. The format codes runs of 4-bit nibbles with a
/// prefix code tree. The output size is a number of 32-byte tiles.
/// </summary>
public static class Nemesis
{
	/// <summary>The number of bits of output for each tile: 32 bytes of 8 bits.</summary>
	private const int TileBitsShift = 8;

	/// <summary>The bit in the tile count that turns on XOR mode.</summary>
	private const int XorModeFlag = 0x8000;

	/// <summary>
	/// A nibble value and the number of times it repeats.
	/// A count of 255 marks the inline code: the run follows in the next 7 bits.
	/// </summary>
	private struct NibbleRun
	{
		public byte Nibble { get; set; }

		public byte Count { get; set; }

		public NibbleRun(byte nibble, byte count)
		{
			Nibble = nibble;
			Count = count;
		}

		public override string ToString()
		{
			return $"{Nibble} x{Count}";
		}
	}

	/// <summary>
	/// A node of the prefix code tree. A 0 bit selects <see cref="Clear"/> and a 1 bit selects <see cref="Set"/>.
	/// </summary>
	private class CodeTreeNode
	{
		public CodeTreeNode Clear { get; set; }

		public CodeTreeNode Set { get; set; }

		public NibbleRun NibbleRun { get; set; }

		public override string ToString()
		{
			if (Clear == null && Set == null)
			{
				return "Leaf";
			}
			return "Node";
		}
	}

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

	public static int Decompress(Stream input, Stream output)
	{
		byte[] xorRow = new byte[4];
		MemoryStream memoryStream = new MemoryStream();
		CodeTreeNode codeTree = new CodeTreeNode();
		int tileCount = (byte)input.ReadByte() << 8;
		tileCount |= (byte)input.ReadByte();
		int xorMode = (((tileCount & XorModeFlag) != 0) ? 1 : 0);
		tileCount &= ~XorModeFlag;
		ReadHeader(input, memoryStream, codeTree);
		ReadInternal(input, memoryStream, codeTree, (ushort)tileCount, xorMode);
		int length = (int)memoryStream.Length;
		memoryStream.Position = 0L;
		if (xorMode != 0)
		{
			// In XOR mode, each 4-byte row is stored as the XOR with the previous row.
			Array.Clear(xorRow, 0, xorRow.Length);
			for (int i = 0; i < length; i++)
			{
				xorRow[i % 4] ^= (byte)memoryStream.ReadByte();
				output.WriteByte(xorRow[i % 4]);
			}
		}
		else
		{
			memoryStream.CopyTo(output);
		}
		return length;
	}

	/// <summary>
	/// Reads the code table and builds the code tree. The table ends with 0xFF.
	/// A byte with bit 7 set selects the nibble for the next entries. Each entry is a byte with the run count
	/// and the code length, and then a byte with the code.
	/// </summary>
	private static void ReadHeader(Stream input, Stream output, CodeTreeNode codeTree)
	{
		byte nibble = 0;
		byte entry;
		while ((entry = (byte)input.ReadByte()) != byte.MaxValue)
		{
			if ((entry & 0x80) != 0)
			{
				nibble = (byte)(entry & 0xF);
				entry = (byte)input.ReadByte();
			}
			SetCode(codeTree, (byte)input.ReadByte(), entry & 0xF, new NibbleRun(nibble, (byte)(((entry & 0x70) >> 4) + 1)));
		}
		// The 6-bit code 111111 is always the inline code.
		SetCode(codeTree, 63, 6, new NibbleRun(0, byte.MaxValue));
	}

	private static void ReadInternal(Stream input, Stream output, CodeTreeNode codeTree, ushort numTiles, int xorOutput)
	{
		BitReader bitReader = new BitReader(input, 1, highestBitFirst: true);
		BitWriter bitWriter = new BitWriter(output, 1);
		int totalBits = numTiles << TileBitsShift;
		int bitsWritten = 0;
		CodeTreeNode codeTreeNode = codeTree;
		while (bitsWritten < totalBits)
		{
			NibbleRun nibbleRun = codeTreeNode.NibbleRun;
			if (nibbleRun.Count == byte.MaxValue)
			{
				byte count = (byte)(bitReader.ReadBits(3) + 1);
				byte nibble = (byte)bitReader.ReadBits(4);
				WriteNibbleRun(bitWriter, count, nibble, ref bitsWritten);
				codeTreeNode = codeTree;
			}
			else if (nibbleRun.Count != 0)
			{
				WriteNibbleRun(bitWriter, nibbleRun.Count, nibbleRun.Nibble, ref bitsWritten);
				codeTreeNode = codeTree;
			}
			else
			{
				codeTreeNode = ((!bitReader.ReadBit()) ? codeTreeNode.Clear : codeTreeNode.Set);
				if (codeTreeNode == null)
				{
					throw new NemesisException("Invalid code.");
				}
			}
		}
	}

	private static void WriteNibbleRun(BitWriter output, byte count, byte nibble, ref int bitsWritten)
	{
		bitsWritten += count * 4;
		if ((count & 1) != 0)
		{
			output.WriteBits(nibble, 4);
		}
		count >>= 1;
		nibble |= (byte)(nibble << 4);
		while (count-- != 0)
		{
			output.WriteBits(nibble, 8);
		}
	}

	private static void SetCode(CodeTreeNode codeTree, byte code, int length, NibbleRun nibbleRun)
	{
		if (length == 0)
		{
			if (codeTree.Clear != null || codeTree.Set != null)
			{
				throw new NemesisException("Code already used as prefix.");
			}
			codeTree.NibbleRun = nibbleRun;
			return;
		}
		if (codeTree.NibbleRun.Count != 0)
		{
			throw new NemesisException("Prefix already used as code.");
		}
		length--;
		if ((code & (1 << length)) == 0)
		{
			if (codeTree.Clear == null)
			{
				codeTree.Clear = new CodeTreeNode();
			}
			SetCode(codeTree.Clear, code, length, nibbleRun);
		}
		else
		{
			if (codeTree.Set == null)
			{
				codeTree.Set = new CodeTreeNode();
			}
			SetCode(codeTree.Set, (byte)(code & ((1 << length) - 1)), length, nibbleRun);
		}
	}
}
