using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

/// <summary>
/// Loads a PCM RIFF WAVE file into a <see cref="Sample"/>.
/// </summary>
public class WavResourceType : ResourceType
{
	/// <summary>"RIFF" as a little-endian 32-bit value.</summary>
	private const int RiffChunkId = 0x46464952;

	/// <summary>"WAVE" as a little-endian 32-bit value.</summary>
	private const int WaveFormatId = 0x45564157;

	/// <summary>"fmt " as a little-endian 32-bit value.</summary>
	private const int FormatChunkId = 0x20746D66;

	/// <summary>"data" as a little-endian 32-bit value.</summary>
	private const int DataChunkId = 0x61746164;

	private const short PcmAudioFormat = 1;

	public override string Name => "wav";

	public override string DefaultExtension => ".wav";

	public override bool CompressByDefault => false;

	public WavResourceType()
		: base(ResourceTypeIdentifier.SampleWAV)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run<ILoadedResource>(() =>
		{
			// The reader is not disposed because that would close the input stream.
			BinaryReader binaryReader = new BinaryReader(e.InputStream);
			if (binaryReader.ReadInt32() != RiffChunkId)
			{
				throw new ResourceException("Invalid wav signature.");
			}
			binaryReader.ReadInt32(); // RIFF chunk size
			if (binaryReader.ReadInt32() != WaveFormatId)
			{
				throw new ResourceException("Invalid wav signature.");
			}
			// The file is a list of chunks. Skip the chunks that are not "fmt " or "data", for example "LIST". A
			// chunk with an odd size has one byte of padding.
			short channels = 0;
			int sampleRate = 0;
			short bitsPerSample = 0;
			bool hasFormat = false;
			int dataSize;
			while (true)
			{
				int chunkId = binaryReader.ReadInt32();
				int chunkSize = binaryReader.ReadInt32();
				if (chunkId == FormatChunkId)
				{
					if (chunkSize < 16 || binaryReader.ReadInt16() != PcmAudioFormat)
					{
						throw new ResourceException("Non PCM wav signature.");
					}
					channels = binaryReader.ReadInt16();
					sampleRate = binaryReader.ReadInt32();
					binaryReader.ReadInt32(); // Byte rate
					binaryReader.ReadInt16(); // Block align
					bitsPerSample = binaryReader.ReadInt16();
					// A longer format chunk has extra fields after the first 16 bytes
					SkipBytes(binaryReader, chunkSize - 16 + (chunkSize & 1));
					hasFormat = true;
				}
				else if (chunkId == DataChunkId)
				{
					if (!hasFormat)
					{
						throw new ResourceException("Invalid wav format.");
					}
					dataSize = chunkSize;
					break;
				}
				else
				{
					SkipBytes(binaryReader, chunkSize + (chunkSize & 1));
				}
			}
			return new Sample(binaryReader.ReadBytes(dataSize), bitsPerSample, sampleRate, channels)
			{
				Resource = e.Resource
			};
		});
	}

	private static void SkipBytes(BinaryReader reader, int count)
	{
		if (count > 0 && reader.ReadBytes(count).Length < count)
		{
			throw new ResourceException("Invalid wav format.");
		}
	}
}
