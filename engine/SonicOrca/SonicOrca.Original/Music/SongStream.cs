using System;
using System.IO;

namespace SonicOrca.Original.Music;

/// <summary>
/// A read-only stream of 16-bit stereo PCM data that a <see cref="SongPlayer"/> makes while the stream is read. A song
/// with a loop plays without end, because the driver follows the loop of the song. A song without a loop ends after
/// its tail. A seek back starts the song again and plays it to the new position.
/// </summary>
internal sealed class SongStream : Stream
{
	private const int BytesPerFrame = 4;

	private readonly Func<SongPlayer> _start;
	private SongPlayer _player;
	private int _frameSample;
	private int _frameLength;
	private long _position;

	/// <param name="start">Starts the song from the beginning.</param>
	public SongStream(Func<SongPlayer> start)
	{
		_start = start;
		_player = start();
	}

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => false;

	public override long Length => throw new NotSupportedException();

	public override long Position
	{
		get => _position;
		set
		{
			if (value < _position)
			{
				_player = _start();
				_frameSample = 0;
				_frameLength = 0;
				_position = 0;
			}
			byte[] skipped = new byte[4096];
			while (_position < value && Read(skipped, 0, (int)Math.Min(skipped.Length, value - _position)) > 0)
			{
			}
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int written = 0;
		while (count - written >= BytesPerFrame)
		{
			if (_frameSample == _frameLength)
			{
				if (_player.Finished)
				{
					break;
				}
				_frameLength = _player.PlayFrame();
				_frameSample = 0;
				continue;
			}
			WriteSample(buffer, offset + written, _player.Left[_frameSample]);
			WriteSample(buffer, offset + written + 2, _player.Right[_frameSample]);
			_frameSample++;
			written += BytesPerFrame;
		}
		_position += written;
		return written;
	}

	/// <summary>Writes a sample as a 16-bit value, rounded and limited in the same way as <see cref="RenderedSong.WriteWave"/>.</summary>
	private static void WriteSample(byte[] buffer, int offset, float sample)
	{
		short value = (short)Math.Clamp((int)Math.Round(sample * 32767f), short.MinValue, short.MaxValue);
		buffer[offset] = (byte)value;
		buffer[offset + 1] = (byte)(value >> 8);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		Position = origin switch
		{
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => _position + offset,
			_ => throw new NotSupportedException()
		};
		return _position;
	}

	public override void Flush()
	{
	}

	public override void SetLength(long value) => throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
