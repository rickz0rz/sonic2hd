using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Graphics.V2.Video;
using SonicOrca.Resources;

namespace SonicOrca.HelperLibraries.H264;

/// <summary>
/// Video resource type. It ignores the resource data and always loads the "credits.film" file from the content directory.
/// </summary>
public class H264ResourceType : ResourceType
{
	public override string Name => "h265";

	public override string DefaultExtension => ".mp4";

	public override bool CompressByDefault => false;

	public H264ResourceType()
		: base(ResourceTypeIdentifier.VideoH264)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run<ILoadedResource>(() => new FilmBuffer(ContentDirectory.Combine("credits.film"))
		{
			Resource = e.Resource
		});
	}
}
