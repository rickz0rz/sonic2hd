using System.IO;
using System.IO.Compression;

namespace SonicOrca.Resources;

public abstract class ResourceSource
{
	private readonly bool _compressed;

	public bool Compressed => _compressed;

	public abstract long Size { get; }

	protected ResourceSource(bool compressed = false)
	{
		_compressed = compressed;
	}

	public abstract Stream Read();

	private byte[] GetData()
	{
		using MemoryStream memoryStream = new MemoryStream();
		Read().CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	public Stream ReadCompressed()
	{
		if (_compressed)
		{
			return Read();
		}
		using MemoryStream memoryStream = new MemoryStream();
		using (GZipStream destination = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			Read().CopyTo(destination);
		}
		return new MemoryStream(memoryStream.ToArray());
	}

	private static byte[] Compress(byte[] uncompressedData)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (MemoryStream memoryStream2 = new MemoryStream(uncompressedData))
		{
			using GZipStream destination = new GZipStream(memoryStream, CompressionMode.Compress);
			memoryStream2.CopyTo(destination);
		}
		return memoryStream.ToArray();
	}

	private static byte[] Uncompress(byte[] compressedData)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (MemoryStream stream = new MemoryStream(compressedData))
		{
			using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
			gZipStream.CopyTo(memoryStream);
		}
		return memoryStream.ToArray();
	}

	public Stream ReadUncompressed()
	{
		if (_compressed)
		{
			return new GZipStream(Read(), CompressionMode.Decompress);
		}
		return Read();
	}
}
