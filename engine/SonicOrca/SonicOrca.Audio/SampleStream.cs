using System;
using System.IO;

namespace SonicOrca.Audio;

/// <summary>
/// Read-only stream over the PCM data of a <see cref="Sample"/>. When the sample has a loop point, reads past the end
/// continue from the loop point.
/// </summary>
internal class SampleStream : Stream
{
	private readonly Sample _sample;

	private readonly byte[] _pcmData;

	private readonly long? _loopPoint;

	private readonly long _length;

	private readonly long _lengthFromLoopPoint;

	public override bool CanRead => true;

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
		long bytesPastEnd = offset - _sample.PcmData.LongLength;
		if (bytesPastEnd > 0)
		{
			Position = _loopPoint.HasValue ? (_loopPoint.Value + bytesPastEnd % _lengthFromLoopPoint) : _length;
		}
		else
		{
			Position = offset;
		}
		return Position;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		long bytesRemaining = _length - Position;
		if (count > bytesRemaining && _loopPoint.HasValue)
		{
			return ReadWrapped(buffer, offset, count);
		}
		return ReadChunk(buffer, offset, (int)Math.Min(count, bytesRemaining));
	}

	/// <summary>
	/// Reads <paramref name="count"/> bytes and goes back to the loop point each time the end of the data is reached.
	/// </summary>
	private int ReadWrapped(byte[] buffer, int offset, int count)
	{
		int totalRead = 0;
		while (totalRead < count)
		{
			long bytesRemaining = _length - Position;
			long bytesWanted = count - totalRead;
			int chunkSize = (int)Math.Min(bytesRemaining, bytesWanted);
			ReadChunk(buffer, offset, chunkSize);
			offset += chunkSize;
			totalRead += chunkSize;
			if (bytesRemaining < bytesWanted)
			{
				Position = _loopPoint.HasValue ? _loopPoint.Value : _length;
			}
		}
		return totalRead;
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
