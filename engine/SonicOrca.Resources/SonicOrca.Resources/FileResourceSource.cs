using System;
using System.IO;

namespace SonicOrca.Resources;

public class FileResourceSource : ResourceSource
{
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
			get
			{
				return _fileStream.Position - _offset;
			}
			set
			{
				if (value > _size)
				{
					throw new ArgumentException("Position greater than resource size.");
				}
				_fileStream.Position = value - _offset;
			}
		}

		public Stream(FileResourceSource source)
		{
			_offset = source._offset;
			_size = source._size;
			_fileStream = new FileStream(source._path, FileMode.Open, FileAccess.Read);
			if (_fileStream.Length < _offset + _size)
			{
				_fileStream.Dispose();
				throw new FileLoadException("File not large enough to contain resource.");
			}
			_fileStream.Position = source._offset;
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			count = (int)Math.Min(count, _size - Position);
			if (count > 0)
			{
				return _fileStream.Read(buffer, offset, count);
			}
			return 0;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long num = origin switch
			{
				SeekOrigin.Begin => _offset + offset, 
				SeekOrigin.Current => Position + offset, 
				SeekOrigin.End => _offset + _size - offset, 
				_ => throw new ArgumentException("Invalid seek origin", "origin"), 
			};
			if (num < _offset || num > _offset + _size)
			{
				throw new IOException("Invalid position to seek to");
			}
			Position = num;
			return num;
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
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

	public override System.IO.Stream Read()
	{
		return new Stream(this);
	}
}
