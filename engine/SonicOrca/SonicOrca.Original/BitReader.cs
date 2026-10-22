using System;
using System.IO;

namespace SonicOrca.Original;

internal class BitReader
{
	private readonly Stream _stream;

	private readonly int _blockSize;

	private int _data;

	private int _bitIndex;

	public BitReader(Stream stream, int blockSize)
	{
		_stream = stream;
		_blockSize = blockSize;
		ReadBlock();
	}

	public int ReadBits(int count)
	{
		int num = 0;
		while (count-- > 0)
		{
			if (ReadBit())
			{
				num = (num << 1) | 1;
			}
		}
		return num;
	}

	public bool ReadBit()
	{
		bool result = (_data & (1 << _bitIndex++)) != 0;
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
