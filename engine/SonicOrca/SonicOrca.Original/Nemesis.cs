using System;
using System.IO;

namespace SonicOrca.Original;

public static class Nemesis
{
	private struct NibbleRun
	{
		public byte Nibble { get; set; }

		public byte Count { get; set; }

		public NibbleRun(byte nibble, byte count)
		{
			this = default;
			Nibble = nibble;
			Count = count;
		}

		public override string ToString()
		{
			return $"{Nibble} x{Count}";
		}
	}

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
		byte[] array = new byte[4];
		MemoryStream memoryStream = new MemoryStream();
		CodeTreeNode codeTree = new CodeTreeNode();
		int num = (byte)input.ReadByte() << 8;
		num |= (byte)input.ReadByte();
		int num2 = (((num & 0x8000) != 0) ? 1 : 0);
		num &= -32769;
		ReadHeader(input, memoryStream, codeTree);
		ReadInternal(input, memoryStream, codeTree, (ushort)num, num2);
		int num3 = (int)memoryStream.Length;
		memoryStream.Position = 0L;
		if (num2 != 0)
		{
			Array.Clear(array, 0, array.Length);
			for (int i = 0; i < num3; i++)
			{
				array[i % 4] ^= (byte)memoryStream.ReadByte();
				output.WriteByte(array[i % 4]);
			}
		}
		else
		{
			memoryStream.CopyTo(output);
		}
		return num3;
	}

	private static void ReadHeader(Stream input, Stream output, CodeTreeNode codeTree)
	{
		byte nibble = 0;
		byte b;
		while ((b = (byte)input.ReadByte()) != byte.MaxValue)
		{
			if ((b & 0x80) != 0)
			{
				nibble = (byte)(b & 0xF);
				b = (byte)input.ReadByte();
			}
			SetCode(codeTree, (byte)input.ReadByte(), b & 0xF, new NibbleRun(nibble, (byte)(((b & 0x70) >> 4) + 1)));
		}
		SetCode(codeTree, 63, 6, new NibbleRun(0, byte.MaxValue));
	}

	private static void ReadInternal(Stream input, Stream output, CodeTreeNode codeTree, ushort numTiles, int xorOutput)
	{
		BitReader bitReader = new BitReader(input, 1);
		BitWriter output2 = new BitWriter(output, 1);
		int num = numTiles << 8;
		int bitsWritten = 0;
		CodeTreeNode codeTreeNode = codeTree;
		while (bitsWritten < num)
		{
			NibbleRun nibbleRun = codeTreeNode.NibbleRun;
			if (nibbleRun.Count == byte.MaxValue)
			{
				byte count = (byte)(bitReader.ReadBits(3) + 1);
				byte nibble = (byte)bitReader.ReadBits(4);
				WriteNibbleRun(output2, count, nibble, ref bitsWritten);
				codeTreeNode = codeTree;
			}
			else if (nibbleRun.Count != 0)
			{
				WriteNibbleRun(output2, nibbleRun.Count, nibbleRun.Nibble, ref bitsWritten);
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
