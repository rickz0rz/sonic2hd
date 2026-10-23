using System;
using System.IO;
using OpenTK.Graphics.OpenGL;

namespace SonicOrca.Drawing.Renderers;

public class ScreenshotRenderer
{
	public static void GrabScreenshot(string directory, string filename = null)
	{
		// Desktop only: reads pixels through OpenTK, and a browser has nowhere useful to save the file
		if (OperatingSystem.IsBrowser())
		{
			return;
		}
		int width = 1920;
		int height = 1080;
		byte[] pixels = new byte[width * height * 3];
		GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
		GL.ReadPixels(0, 0, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
		DateTime now = DateTime.Now;
		filename ??= "Sonic 2 HD " + now.Year + "-" + now.Month + "-" + now.Day + " " + now.Hour + "-" + now.Minute + "-" + now.Second + "-" + now.Millisecond + ".png";
		PngExport.Save(Path.Combine(directory, filename), pixels, width, height, alpha: false, bottomUp: true);
	}
}
