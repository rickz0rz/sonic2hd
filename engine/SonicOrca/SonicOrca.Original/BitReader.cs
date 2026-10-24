using System;
using System.IO;

namespace SonicOrca.Original;

/// <summary>
/// Reads single bits from a stream. It reads little-endian blocks of 1 to 4 bytes. By default it gives the bits of
/// a block from the lowest bit up (Kosinski). With highestBitFirst, it gives them from the highest bit down (Nemesis).
/// </summary>
internal class BitReader
{
	private readonly Stream _stream;

	private readonly int _blockSize;

	private int _data;

	private readonly bool _highestBitFirst;

	private int _bitIndex;

	public BitReader(Stream stream, int blockSize, bool highestBitFirst = false)
	{
		_stream = stream;
		_blockSize = blockSize;
		_highestBitFirst = highestBitFirst;
		ReadBlock();
	}

	/// <summary>Reads a number of <paramref name="count"/> bits, the first bit as the highest bit of the result.</summary>
	public int ReadBits(int count)
	{
		// The decompiled code shifted only for 1 bits, so it lost the 0 bits. Nemesis was the only user.
		int value = 0;
		while (count-- > 0)
		{
			value = (value << 1) | (ReadBit() ? 1 : 0);
		}
		return value;
	}

	public bool ReadBit()
	{
		int bit = _highestBitFirst ? _blockSize * 8 - 1 - _bitIndex : _bitIndex;
		_bitIndex++;
		bool result = (_data & (1 << bit)) != 0;
		if (_bitIndex >= _blockSize * 8)
		{
			ReadBlock();
		}
		return result;
	}

	private void ReadBlock()
	{
		_bitIndex = 0;
		switch (_blockSize)
		{
			case 1:
				_data = _stream.ReadByte();
				break;
			case 2:
				_data = _stream.ReadByte() | (_stream.ReadByte() << 8);
				break;
			case 3:
				_data = _stream.ReadByte() | (_stream.ReadByte() << 8) | (_stream.ReadByte() << 16);
				break;
			case 4:
				_data = _stream.ReadByte() | (_stream.ReadByte() << 8) | (_stream.ReadByte() << 16) | (_stream.ReadByte() << 24);
				break;
			default:
				throw new InvalidOperationException();
		}
	}
}
