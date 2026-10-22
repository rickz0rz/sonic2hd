using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SonicOrca.Graphics.V2.Video;
using SonicOrca.Resources;

namespace SonicOrca.HelperLibraries.H264;

public class H264ResourceType : ResourceType
{
	private struct PixelData
	{
		public int Width { get; private set; }

		public int Height { get; private set; }

		public byte[] Data { get; private set; }

		public int Channels { get; private set; }

		public PixelData(int width, int height, int channels, byte[] data)
		{
			this = default;
			Width = width;
			Height = height;
			Channels = channels;
			Data = data;
		}
	}

	public override string Name => "h265";

	public override string DefaultExtension => ".mp4";

	public override bool CompressByDefault => false;

	public H264ResourceType()
		: base(ResourceTypeIdentifier.VideoH264)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		return await Task.Run((Func<ILoadedResource>)(() => new FilmBuffer(ContentDirectory.Combine("credits.film"))
		{
			Resource = e.Resource
		}));
	}
}
