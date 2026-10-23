using System.IO;
using System.IO.Compression;

namespace SonicOrca.Resources;

/// <summary>Where the bytes of a resource come from. <see cref="Compressed"/> sources hold gzip data.</summary>
public abstract class ResourceSource
{
	private readonly bool _compressed;

	public bool Compressed => _compressed;

	/// <summary>The size of the stored bytes (compressed when <see cref="Compressed"/>).</summary>
	public abstract long Size { get; }

	protected ResourceSource(bool compressed = false)
	{
		_compressed = compressed;
	}

	/// <summary>Opens the stored bytes as they are.</summary>
	public abstract Stream Read();

	/// <summary>Opens the bytes gzip-compressed, compressing them first when the source is not compressed.</summary>
	public Stream ReadCompressed()
	{
		if (_compressed)
		{
			return Read();
		}
		MemoryStream compressed = new MemoryStream();
		using (Stream source = Read())
		using (GZipStream gzip = new GZipStream(compressed, CompressionMode.Compress, leaveOpen: true))
		{
			source.CopyTo(gzip);
		}
		compressed.Position = 0;
		return compressed;
	}

	/// <summary>Opens the bytes uncompressed.</summary>
	public Stream ReadUncompressed()
	{
		return _compressed ? new GZipStream(Read(), CompressionMode.Decompress) : Read();
	}
}
