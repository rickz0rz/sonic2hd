using System.IO;
using Hjg.Pngcs;

namespace SonicOrca.Drawing;

/// <summary>
/// Cross-platform PNG encoding using the engine's bundled Pngcs library (replaces System.Drawing).
/// </summary>
internal static class PngExport
{
	/// <param name="pixels">8-bit RGB or RGBA samples, rows top to bottom unless <paramref name="bottomUp"/>.</param>
	/// <param name="bgr">Pixels are in BGR(A) order and need red and blue swapped.</param>
	public static void Save(string path, byte[] pixels, int width, int height, bool alpha, bool bgr = false, bool bottomUp = false)
	{
		int channels = alpha ? 4 : 3;
		int stride = width * channels;
		var writer = new PngWriter(File.Create(path), new ImageInfo(width, height, 8, alpha), path);
		byte[] row = new byte[stride];
		for (int y = 0; y < height; y++)
		{
			int sourceRow = bottomUp ? height - 1 - y : y;
			System.Array.Copy(pixels, sourceRow * stride, row, 0, stride);
			if (bgr)
			{
				for (int x = 0; x < stride; x += channels)
					(row[x], row[x + 2]) = (row[x + 2], row[x]);
			}
			writer.WriteRowByte(row, y);
		}
		writer.End();
	}
}
