using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.HelperLibraries.OggVorbis;

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
		return await ResourceType.FromIdentifier(ResourceTypeIdentifier.SampleWAV).LoadAsync(new ResourceLoadArgs(e.ResourceTree, e.Resource, inputStream), ct);
	}

	private Task<Stream> GetSlowStream(Stream inputStream)
	{
		return Task.Run((Func<Stream>)(() => new OggDecodeStream(inputStream, skipWavHeader: false)));
	}

	[DllImport("SonicOrcaFastUtil.dll")]
	private static extern int BeginReadVorbis(IntPtr input, int inputSize, out IntPtr output, out int outputLength);

	[DllImport("SonicOrcaFastUtil.dll")]
	private static extern void EndReadVorbis(IntPtr output);

	private async Task<Stream> GetFastStream(Stream inputStream)
	{
		byte[] array;
		using (MemoryStream ms = new MemoryStream())
		{
			await inputStream.CopyToAsync(ms);
			array = ms.ToArray();
		}
		if (BeginReadVorbis(Marshal.UnsafeAddrOfPinnedArrayElement((Array)array, 0), array.Length, out var output, out var outputLength) == 0)
		{
			throw new ResourceException("Unable to read OGG Vorbis data");
		}
		byte[] array2 = new byte[outputLength];
		Marshal.Copy(output, array2, 0, outputLength);
		EndReadVorbis(output);
		return new MemoryStream(array2);
	}
}
