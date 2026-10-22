using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Hjg.Pngcs;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.HelperLibraries.Png;

public class PngResourceType : ResourceType
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

	private async Task<PixelData> GetSlowStream(Stream inputStream)
	{
		PngReader reader = null;
		try
		{
			reader = new PngReader(inputStream);
			int width = reader.ImgInfo.Cols;
			int height = reader.ImgInfo.Rows;
			int channels = reader.ImgInfo.Channels;
			byte[] argb = new byte[width * height * 4];
			await Task.Run(() =>
			{
				for (int i = 0; i < height; i++)
				{
					ImageLine imageLine = reader.ReadRowByte(i);
					Buffer.BlockCopy(imageLine.ScanlineB, 0, argb, i * width * imageLine.channels, width * imageLine.channels);
				}
			});
			reader.End();
			return new PixelData(width, height, channels, argb);
		}
		finally
		{
			if (reader != null)
			{
				reader.End();
			}
		}
	}

	[DllImport("SonicOrcaFastUtil.dll")]
	private static extern int BeginReadPNG(IntPtr input, int inputSize, out int width, out int height, out IntPtr output);

	[DllImport("SonicOrcaFastUtil.dll")]
	private static extern void EndReadPNG(IntPtr output);

	private async Task<PixelData> GetFastStream(Stream inputStream)
	{
		byte[] array;
		using (MemoryStream ms = new MemoryStream())
		{
			await inputStream.CopyToAsync(ms);
			array = ms.ToArray();
		}
		if (BeginReadPNG(Marshal.UnsafeAddrOfPinnedArrayElement((Array)array, 0), array.Length, out var width, out var height, out var output) == 0)
		{
			throw new ResourceException("Unable to load PNG.");
		}
		byte[] array2 = new byte[width * height * 4];
		Marshal.Copy(output, array2, 0, array2.Length);
		EndReadPNG(output);
		return new PixelData(width, height, 4, array2);
	}
}
