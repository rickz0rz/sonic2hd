using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Hjg.Pngcs;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.HelperLibraries.Png;

/// <summary>
/// Loads a PNG file into a texture.
/// </summary>
public class PngResourceType : ResourceType
{
	/// <summary>
	/// Decoded image data before it becomes a texture.
	/// </summary>
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

	private const int BytesPerPixel = 4;

	public override string Name => "png";

	public override string DefaultExtension => ".png";

	public override bool CompressByDefault => false;

	public PngResourceType()
		: base(ResourceTypeIdentifier.TexturePNG)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		PixelData pixelData = await GetSlowStream(e.InputStream);
		ITexture texture = SonicOrcaGameContext.Singleton.Window.GraphicsContext.CreateTexture(pixelData.Width, pixelData.Height, pixelData.Channels, pixelData.Data);
		texture.Resource = e.Resource;
		return texture;
	}

	/// <summary>
	/// Decodes the image with the managed PNG reader.
	/// </summary>
	private async Task<PixelData> GetSlowStream(Stream inputStream)
	{
		PngReader reader = null;
		try
		{
			reader = new PngReader(inputStream);
			int width = reader.ImgInfo.Cols;
			int height = reader.ImgInfo.Rows;
			int channels = reader.ImgInfo.Channels;
			// The buffer has space for 4 channels. The rows are packed with the real channel count of the image.
			byte[] pixels = new byte[width * height * BytesPerPixel];
			await Task.Run(() =>
			{
				for (int row = 0; row < height; row++)
				{
					ImageLine imageLine = reader.ReadRowByte(row);
					Buffer.BlockCopy(imageLine.ScanlineB, 0, pixels, row * width * imageLine.channels, width * imageLine.channels);
				}
			});
			reader.End();
			return new PixelData(width, height, channels, pixels);
		}
		finally
		{
			// On success, End is called a second time here.
			if (reader != null)
			{
				reader.End();
			}
		}
	}
}
