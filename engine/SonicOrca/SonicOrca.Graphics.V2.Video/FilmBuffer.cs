using System;
using System.IO;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Graphics.V2.Video;

/// <summary>
/// Decodes a video file one frame at a time into a BGR byte buffer.
/// </summary>
// Originally backed by Accord.Video.FFMPEG (C++/CLI, Windows/.NET Framework only); now uses FFMediaToolkit
public class FilmBuffer : IFilmBuffer, IDisposable, ILoadedResource
{
	private const string FFmpegDirectoryName = "ffmpeg";

	private static readonly object FFmpegLoadLock = new object();

	private static bool _ffmpegLoaded;

	private MediaFile _file;

	private int _width;

	private int _height;

	private double _frameRate = 1.0;

	private double _currentFrame;

	private double _numFrames;

	private byte[] _bytes;

	private int _stride;

	private string _path;

	public int Width => _width;

	public int Height => _height;

	public double CurrentTime => _currentFrame / _frameRate;

	public double Duration => _numFrames / _frameRate;

	public Resource Resource { get; set; }

	public void Dispose()
	{
		_file?.Dispose();
		_file = null;
	}

	public void OnLoaded()
	{
		try
		{
			EnsureFFmpegLoaded();
			_file = MediaFile.Open(_path, new MediaOptions
			{
				StreamsToLoad = MediaMode.Video,
				VideoPixelFormat = ImagePixelFormat.Bgr24,
				// 0 = auto; libavcodec otherwise decodes single-threaded, too slow for 1080p60 on the game thread
				DecoderThreads = 0
			});
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return;
		}
		VideoStreamInfo info = _file.Video.Info;
		_width = info.FrameSize.Width;
		_height = info.FrameSize.Height;
		_frameRate = info.RealFrameRate.den != 0 ? (double)info.RealFrameRate.num / info.RealFrameRate.den : info.AvgFrameRate;
		_numFrames = info.NumberOfFrames ?? Math.Round(info.Duration.TotalSeconds * _frameRate);
		_currentFrame = 0.0;
		// BGR rows padded to 4 bytes, matching GL's default unpack alignment used by GLTexture.SetArgbData
		_stride = (_width * 3 + 3) & ~3;
		_bytes = new byte[_stride * _height];
	}

	public FilmBuffer(string path)
	{
		_path = path;
	}

	public unsafe void Decode()
	{
		if (_file == null)
		{
			return;
		}
		// At end of stream the previous frame is kept, and time still advances so the film finishes
		fixed (byte* buffer = _bytes)
		{
			_file.Video.TryGetNextFrame((IntPtr)buffer, _stride);
		}
		_currentFrame++;
	}

	public byte[] GetArgbData()
	{
		return _bytes;
	}

	private static void EnsureFFmpegLoaded()
	{
		lock (FFmpegLoadLock)
		{
			if (_ffmpegLoaded)
			{
				return;
			}
			string path = Path.Combine(AppContext.BaseDirectory, FFmpegDirectoryName);
			FFmpegLoader.FFmpegPath = Directory.Exists(path) ? path : AppContext.BaseDirectory;
			FFmpegLoader.LoadFFmpeg();
			_ffmpegLoaded = true;
		}
	}
}
