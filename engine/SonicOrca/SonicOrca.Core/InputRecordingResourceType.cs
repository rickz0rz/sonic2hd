using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Resources;

namespace SonicOrca.Core;

/// <summary>
/// Loads an input recording resource.
/// </summary>
public class InputRecordingResourceType : ResourceType
{
	public override string Name => "recording, rec";

	public override string DefaultExtension => ".rec";

	public override bool CompressByDefault => true;

	public InputRecordingResourceType()
		: base(ResourceTypeIdentifier.InputRecording)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		byte[] data;
		using (MemoryStream ms = new MemoryStream())
		{
			await e.InputStream.CopyToAsync(ms);
			data = ms.ToArray();
		}
		return new InputRecordingResource(data);
	}
}
