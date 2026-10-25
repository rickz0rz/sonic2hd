using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.HelperLibraries.OggVorbis;

/// <summary>
/// Loads an Ogg Vorbis file. It decodes the file to WAVE data and gives that data to the WAVE resource type.
/// </summary>
public class OggResourceType : ResourceType
{
	public override string Name => "ogg";

	public override string DefaultExtension => ".ogg";

	public override bool CompressByDefault => false;

	public OggResourceType()
		: base(ResourceTypeIdentifier.SampleOGG)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		Stream inputStream = await GetSlowStream(e.InputStream);
		return await FromIdentifier(ResourceTypeIdentifier.SampleWAV).LoadAsync(new ResourceLoadArgs(e.ResourceTree, e.Resource, inputStream), ct);
	}

	/// <summary>
	/// Decodes the data with the managed Vorbis decoder.
	/// </summary>
	private Task<Stream> GetSlowStream(Stream inputStream)
	{
		return Task.Run<Stream>(() => new OggDecodeStream(inputStream, skipWavHeader: false));
	}
}
