using System;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace SonicOrca.Drawing.Renderers;

/// <summary>
/// Saves the contents of the screen to a PNG file.
/// </summary>
public class ScreenshotRenderer
{
	private const int ScreenWidth = 1920;

	private const int ScreenHeight = 1080;

	public static void GrabScreenshot(string directory, string filename = null)
	{
		// Desktop only: reads pixels through OpenTK, and a browser has nowhere useful to save the file
		if (OperatingSystem.IsBrowser())
		{
			return;
		}
		int width = ScreenWidth;
		int height = ScreenHeight;
		byte[] pixels = new byte[width * height * 3];
		GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
		GL.ReadPixels(0, 0, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
		DateTime now = DateTime.Now;
		filename ??= "Sonic 2 HD " + now.Year + "-" + now.Month + "-" + now.Day + " " + now.Hour + "-" + now.Minute + "-" + now.Second + "-" + now.Millisecond + ".png";
		PngExport.Save(Path.Combine(directory, filename), pixels, width, height, alpha: false, bottomUp: true);
	}
}
