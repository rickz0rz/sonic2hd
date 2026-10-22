using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

public class WavResourceType : ResourceType
{
	public override string Name => "wav";

	public override string DefaultExtension => ".wav";

	public override bool CompressByDefault => false;

	public WavResourceType()
		: base(ResourceTypeIdentifier.SampleWAV)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run((Func<ILoadedResource>)(() =>
		{
			BinaryReader binaryReader = new BinaryReader(e.InputStream);
			if (binaryReader.ReadInt32() != 1179011410)
			{
				throw new ResourceException("Invalid wav signature.");
			}
			binaryReader.ReadInt32();
			if (binaryReader.ReadInt32() != 1163280727)
			{
				throw new ResourceException("Invalid wav signature.");
			}
			if (binaryReader.ReadInt32() != 544501094)
			{
				throw new ResourceException("Invalid wav signature.");
			}
			binaryReader.ReadInt32();
			if (binaryReader.ReadInt16() != 1)
			{
				throw new ResourceException("Non PCM wav signature.");
			}
			short channels = binaryReader.ReadInt16();
			int sampleRate = binaryReader.ReadInt32();
			binaryReader.ReadInt32();
			binaryReader.ReadInt16();
			short bitsPerSample = binaryReader.ReadInt16();
			if (binaryReader.ReadInt32() != 1635017060)
			{
				throw new ResourceException("Invalid wav format.");
			}
			int count = binaryReader.ReadInt32();
			return new Sample(binaryReader.ReadBytes(count), bitsPerSample, sampleRate, channels)
			{
				Resource = e.Resource
			};
		}));
	}
}
