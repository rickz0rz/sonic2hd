using System;
using System.IO;

namespace SonicOrca.Resources;

/// <summary>A resource stored in a file, from <see cref="Offset"/> for <see cref="Size"/> bytes.</summary>
public class FileResourceSource : ResourceSource
{
	/// <summary>A read-only view of the resource's bytes. Positions are relative to the start of the resource.</summary>
	public class Stream : System.IO.Stream
	{
		private readonly FileStream _fileStream;

		private readonly long _offset;

		private readonly long _size;

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => false;

		public override long Length => _size;

		public override long Position
		{
			get => _fileStream.Position - _offset;
			set
			{
				if (value < 0 || value > _size)
				{
					throw new ArgumentOutOfRangeException(nameof(value), "Position outside the resource.");
				}
				_fileStream.Position = _offset + value;
			}
		}

		public Stream(FileResourceSource source)
		{
			_offset = source._offset;
			_size = source._size;
			_fileStream = new FileStream(source._path, FileMode.Open, FileAccess.Read, FileShare.Read);
			if (_fileStream.Length < _offset + _size)
			{
				_fileStream.Dispose();
				throw new FileLoadException("File not large enough to contain resource.");
			}
			_fileStream.Position = _offset;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			count = (int)Math.Min(count, _size - Position);
			return count > 0 ? _fileStream.Read(buffer, offset, count) : 0;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long position = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => Position + offset,
				SeekOrigin.End => _size + offset,
				_ => throw new ArgumentException("Invalid seek origin", nameof(origin)),
			};
			if (position < 0 || position > _size)
			{
				throw new IOException("Invalid position to seek to");
			}
			Position = position;
			return position;
		}

		public override void Flush()
		{
		}

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_fileStream.Dispose();
			}
			base.Dispose(disposing);
		}
	}

	private readonly string _path;

	private readonly long _offset;

	private readonly long _size;

	public string Path => _path;

	public long Offset => _offset;

	public override long Size => _size;

	public FileResourceSource(string path, long offset, long size, bool compressed = false)
		: base(compressed)
	{
		_path = path;
		_offset = offset;
		_size = size;
	}

	public override System.IO.Stream Read() => new Stream(this);
}
