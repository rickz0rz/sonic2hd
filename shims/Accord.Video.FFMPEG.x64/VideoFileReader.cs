using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Accord.Math;
using FFMediaToolkit;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;

namespace Accord.Video.FFMPEG;

/// <summary>
/// Drop-in replacement for Accord.NET's C++/CLI <c>VideoFileReader</c>, which cannot load on modern .NET.
/// Implements only the members SonicOrca.dll binds to, backed by FFMediaToolkit (FFmpeg 7.x).
/// </summary>
public class VideoFileReader : IDisposable
{
  private const string FFmpegDirectoryName = "ffmpeg";

  private static readonly object LoadLock = new object();
  private static bool _ffmpegLoaded;

  private MediaFile _file;
  private Bitmap _frame;
  private bool _hasFrame;

  public int Width { get; private set; }

  public int Height { get; private set; }

  public Rational FrameRate { get; private set; }

  public long FrameCount { get; private set; }

  public bool IsOpen => _file != null;

  public void Open(string fileName)
  {
    Close();
    EnsureFFmpegLoaded();

    _file = MediaFile.Open(fileName, new MediaOptions
    {
      StreamsToLoad = MediaMode.Video,
      VideoPixelFormat = ImagePixelFormat.Bgr24,
      // 0 = auto; libavcodec otherwise decodes single-threaded, too slow for 1080p60 on the game thread
      DecoderThreads = 0,
    });
    if (!_file.HasVideo)
    {
      Close();
      throw new IOException($"No video stream found in '{fileName}'.");
    }

    VideoStreamInfo info = _file.Video.Info;
    Width = info.FrameSize.Width;
    Height = info.FrameSize.Height;
    FrameRate = info.RealFrameRate.den != 0
      ? new Rational(info.RealFrameRate.num, info.RealFrameRate.den)
      : Rational.FromDouble(info.AvgFrameRate);
    FrameCount = info.NumberOfFrames ?? (long)System.Math.Round(info.Duration.TotalSeconds * FrameRate.Value);
    _frame = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
  }

  /// <summary>
  /// Decodes the next frame. The caller owns the returned bitmap.
  /// </summary>
  /// <remarks>
  /// Accord returns null at end of stream, but SonicOrca's FilmBuffer.Decode unconditionally disposes the
  /// result, so a null would crash it. Instead, a copy of the last frame is returned once the stream ends.
  /// </remarks>
  public Bitmap ReadVideoFrame()
  {
    if (_file == null)
      throw new InvalidOperationException("The video file was not opened.");

    // Decode into a reusable bitmap, so the last frame is still available after end of stream
    var bounds = new Rectangle(0, 0, Width, Height);
    BitmapData data = _frame.LockBits(bounds, ImageLockMode.WriteOnly, _frame.PixelFormat);
    try
    {
      if (_file.Video.TryGetNextFrame(data.Scan0, data.Stride))
        _hasFrame = true;
    }
    finally
    {
      _frame.UnlockBits(data);
    }

    return _hasFrame ? _frame.Clone(bounds, _frame.PixelFormat) : null;
  }

  public void Close()
  {
    _file?.Dispose();
    _file = null;
    _frame?.Dispose();
    _frame = null;
    _hasFrame = false;
  }

  public void Dispose()
  {
    Close();
    GC.SuppressFinalize(this);
  }

  private static void EnsureFFmpegLoaded()
  {
    lock (LoadLock)
    {
      if (_ffmpegLoaded)
        return;
      string path = Path.Combine(AppContext.BaseDirectory, FFmpegDirectoryName);
      FFmpegLoader.FFmpegPath = Directory.Exists(path) ? path : AppContext.BaseDirectory;
      FFmpegLoader.LoadFFmpeg();
      _ffmpegLoaded = true;
    }
  }
}
