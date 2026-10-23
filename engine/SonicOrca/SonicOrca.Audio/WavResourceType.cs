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
			if (binaryReader.ReadInt32() != FormatChunkId)
			{
				throw new ResourceException("Invalid wav signature.");
			}
			binaryReader.ReadInt32(); // Format chunk size
			if (binaryReader.ReadInt16() != PcmAudioFormat)
			{
				throw new ResourceException("Non PCM wav signature.");
			}
			short channels = binaryReader.ReadInt16();
			int sampleRate = binaryReader.ReadInt32();
			binaryReader.ReadInt32(); // Byte rate
			binaryReader.ReadInt16(); // Block align
			short bitsPerSample = binaryReader.ReadInt16();
			if (binaryReader.ReadInt32() != DataChunkId)
			{
				throw new ResourceException("Invalid wav format.");
			}
			int dataSize = binaryReader.ReadInt32();
			return new Sample(binaryReader.ReadBytes(dataSize), bitsPerSample, sampleRate, channels)
			{
				Resource = e.Resource
			};
		});
	}
}
