using System;
using System.IO;

namespace SonicOrca.Audio;

internal class SampleStream : Stream
{
	private readonly Sample _sample;

	private readonly byte[] _pcmData;

	private readonly long? _loopPoint;

	private readonly long _length;

	private readonly long _lengthFromLoopPoint;

	public override bool CanRead
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public override bool CanSeek => true;

	public override bool CanWrite => false;

	public override long Length => _length;

	public override long Position { get; set; }

	public SampleStream(Sample sample, int? loopSampleIndex = null)
	{
		_sample = sample;
		_pcmData = sample.PcmData;
		_length = _sample.PcmData.LongLength;
		if (loopSampleIndex.HasValue)
		{
			_loopPoint = sample.GetPcmDataOffset(loopSampleIndex.Value);
			_lengthFromLoopPoint = _length - _loopPoint.Value;
		}
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		switch (origin)
		{
		case SeekOrigin.End:
			throw new NotImplementedException();
		case SeekOrigin.Current:
			offset += Position;
			break;
		}
		long num = offset - _sample.PcmData.LongLength;
		if (num > 0)
		{
			Position = (_loopPoint.HasValue ? (_loopPoint.Value + num % _lengthFromLoopPoint) : _length);
		}
		else
		{
			Position = offset;
		}
		return Position;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		long num = _length - Position;
		if (count > num && _loopPoint.HasValue)
		{
			return ReadWrapped(buffer, offset, count);
		}
		return ReadChunk(buffer, offset, (int)Math.Min(count, num));
	}

	private int ReadWrapped(byte[] buffer, int offset, int count)
	{
		int num = 0;
		while (num < count)
		{
			long num2 = _length - Position;
			long num3 = count - num;
			int num4 = (int)Math.Min(num2, num3);
			ReadChunk(buffer, offset, num4);
			offset += num4;
			num += num4;
			if (num2 < num3)
			{
				Position = (_loopPoint.HasValue ? _loopPoint.Value : _length);
			}
		}
		return num;
	}

	private int ReadChunk(byte[] buffer, int offset, int count)
	{
		Array.Copy(_pcmData, Position, buffer, offset, count);
		Position += count;
		return count;
	}

	public override void Flush()
	{
		throw new InvalidOperationException();
	}

	public override void SetLength(long value)
	{
		throw new InvalidOperationException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new InvalidOperationException();
	}
}
