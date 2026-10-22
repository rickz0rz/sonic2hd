using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Accord.Video.FFMPEG;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

public class FilmBuffer : IFilmBuffer, IDisposable, ILoadedResource
{
	private VideoFileReader _reader = new VideoFileReader();

	private int _width;

	private int _height;

	private double _currentFrame;

	private double _numFrames;

	private ImageConverter converter = new ImageConverter();

	private byte[] _bytes;

	private string _path;

	public int Width => _width;

	public int Height => _height;

	public double CurrentTime => _currentFrame / _reader.FrameRate.Value;

	public double Duration => _numFrames / _reader.FrameRate.Value;

	public Resource Resource { get; set; }

	public void Dispose()
	{
		_reader.Close();
	}

	public void OnLoaded()
	{
		try
		{
			_reader.Open(_path);
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		_width = _reader.Width;
		_height = _reader.Height;
		_numFrames = _reader.FrameCount;
		_currentFrame = 0.0;
	}

	public FilmBuffer(string path)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		_path = path;
	}

	public void Decode()
	{
		Bitmap bitmap = _reader.ReadVideoFrame();
		if (bitmap != null)
		{
			_currentFrame++;
			Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
			BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			IntPtr scan = bitmapData.Scan0;
			int num = Math.Abs(bitmapData.Stride) * bitmap.Height;
			if (_bytes == null)
			{
				_bytes = new byte[num];
			}
			else if (num != _bytes.Length)
			{
				_bytes = new byte[num];
			}
			Marshal.Copy(scan, _bytes, 0, num);
			bitmap.UnlockBits(bitmapData);
		}
		bitmap.Dispose();
	}

	public byte[] GetArgbData()
	{
		return _bytes;
	}
}
