using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;

namespace SonicOrca.Drawing.Renderers;

public class ScreenshotRenderer
{
	public static void GrabScreenshot()
	{
		int width = 1920;
		int height = 1080;
		Bitmap bitmap = new Bitmap(width, height);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
		GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, bitmapData.Scan0);
		bitmap.UnlockBits(bitmapData);
		bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
		string filename = "Sonic 2 HD " + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + " " + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond + ".png";
		bitmap.Save(filename, ImageFormat.Png);
		bitmap.Dispose();
	}
}
