using System;
using System.IO;

namespace SonicOrca.Original;

internal class BitWriter
{
	private readonly Stream _stream;

	private readonly int _blockSize;

	private int _data;

	private int _bitIndex;

	public BitWriter(Stream stream, int blockSize)
	{
		_stream = stream;
		_blockSize = blockSize;
	}

	public void WriteBits(int value, int count)
	{
		while (count-- > 0)
		{
			WriteBit((value >> count) & 1);
		}
	}

	public void WriteBit(int bit)
	{
		int num = 1 << _blockSize * 8 - 1 - _bitIndex++;
		if (bit != 0)
		{
			_data |= num;
		}
		else
		{
			_data &= ~num;
		}
		if (_bitIndex >= _blockSize * 8)
		{
			WriteBlock();
		}
	}

	private void WriteBlock()
	{
		switch (_blockSize)
		{
		case 1:
			_stream.WriteByte((byte)(_data & 0xFF));
			break;
		case 2:
			_stream.WriteByte((byte)(_data & 0xFF));
			_stream.WriteByte((byte)((_data >> 8) & 0xFF));
			break;
		case 3:
			_stream.WriteByte((byte)(_data & 0xFF));
			_stream.WriteByte((byte)((_data >> 8) & 0xFF));
			_stream.WriteByte((byte)((_data >> 16) & 0xFF));
			break;
		case 4:
			_stream.WriteByte((byte)(_data & 0xFF));
			_stream.WriteByte((byte)((_data >> 8) & 0xFF));
			_stream.WriteByte((byte)((_data >> 16) & 0xFF));
			_stream.WriteByte((byte)((_data >> 24) & 0xFF));
			break;
		default:
			throw new InvalidOperationException();
		}
		_bitIndex = 0;
		_data = 0;
	}
}
