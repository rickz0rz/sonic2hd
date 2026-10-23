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

	[DllImport("SonicOrcaFastUtil.dll")]
	private static extern int BeginReadVorbis(IntPtr input, int inputSize, out IntPtr output, out int outputLength);

	[DllImport("SonicOrcaFastUtil.dll")]
	private static extern void EndReadVorbis(IntPtr output);

	/// <summary>
	/// Decodes the data with the native SonicOrcaFastUtil library. Nothing calls this method at this time.
	/// </summary>
	private async Task<Stream> GetFastStream(Stream inputStream)
	{
		byte[] inputData;
		using (MemoryStream ms = new MemoryStream())
		{
			await inputStream.CopyToAsync(ms);
			inputData = ms.ToArray();
		}
		if (BeginReadVorbis(Marshal.UnsafeAddrOfPinnedArrayElement((Array)inputData, 0), inputData.Length, out IntPtr output, out int outputLength) == 0)
		{
			throw new ResourceException("Unable to read OGG Vorbis data");
		}
		byte[] decodedData = new byte[outputLength];
		Marshal.Copy(output, decodedData, 0, outputLength);
		EndReadVorbis(output);
		return new MemoryStream(decodedData);
	}
}
