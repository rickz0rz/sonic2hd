using System.IO;

namespace SonicOrca.Resources;

/// <summary>A resource held in memory, for example a resource that the game made or a copy of a file that can change.</summary>
public class MemoryResourceSource : ResourceSource
{
	private readonly byte[] _data;

	public override long Size => _data.Length;

	public MemoryResourceSource(byte[] data, bool compressed = false)
		: base(compressed)
	{
		_data = data;
	}

	/// <summary>Reads all the stored bytes of a source into memory, as they are.</summary>
	public static MemoryResourceSource CopyOf(ResourceSource source)
	{
		using MemoryStream copy = new MemoryStream();
		using (Stream stream = source.Read())
		{
			stream.CopyTo(copy);
		}
		return new MemoryResourceSource(copy.ToArray(), source.Compressed);
	}

	public override Stream Read() => new MemoryStream(_data, writable: false);
}
