using System;
using System.IO;
using System.Linq;
using Hjg.Pngcs;
using SonicOrca.Core;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.SONIC;

/// <summary>
/// The palette cycle of Super Sonic (PalCycle_SuperSonic). The cycle fades from the blue of Sonic to the colours of
/// Super Sonic, and then shows the frames of the normal cycle again and again. The original sprites have the blue of
/// Sonic, so this class draws the current frame again with the colours of the cycle into a small texture. The
/// pixels come from the PNG files of the resources, because the web build cannot read a texture back.
/// </summary>
internal sealed class SuperSonicPalette : IDisposable
{
	/// <summary>The fade-in shows the frames 0 to 5 of CyclingPal_SSTransformation.</summary>
	private const int FadeFrames = 6;

	/// <summary>The normal cycle shows the frames $30 to $70. A bug of the original skips the last frame, $78.</summary>
	private const int LastCycleFrame = 14;

	/// <summary>Sonic_CheckGoSuper sets Palette_timer to $F.</summary>
	private const int StartTimer = 15;

	/// <summary>The fade-in shows each frame for 4 ticks.</summary>
	private const int FadeTimer = 3;

	/// <summary>The normal cycle shows each frame for 8 ticks.</summary>
	private const int CycleTimer = 7;

	private const int BytesPerPixel = 4;

	private readonly byte[] _pixels;

	private readonly int _width;

	/// <summary>The colours of each frame of the cycle, as RGB. Frame 0 has the colours of the sprites.</summary>
	private readonly int[][] _colours;

	private readonly ITexture _texture;

	private readonly byte[] _buffer;

	private int _frame;

	private int _nextFrame;

	private int _timer;

	private bool _fading;

	private Rectanglei _uploadedSource;

	private int _uploadedFrame = -1;

	private SuperSonicPalette(IGraphicsContext graphicsContext, AnimationGroup animationGroup, byte[] pixels, int width, int[][] colours)
	{
		_pixels = pixels;
		_width = width;
		_colours = colours;
		int maxWidth = animationGroup.SelectMany(animation => animation.Frames).Max(frame => frame.Source.Width);
		int maxHeight = animationGroup.SelectMany(animation => animation.Frames).Max(frame => frame.Source.Height);
		_texture = graphicsContext.CreateTexture(maxWidth, maxHeight);
		_buffer = new byte[maxWidth * maxHeight * BytesPerPixel];
	}

	/// <summary>
	/// Returns the palette cycle for the original sprites of Super Sonic, or null when the group has no palette (an
	/// import before version 5, whose sprites have the Super Sonic colours already).
	/// </summary>
	public static SuperSonicPalette Create(IGraphicsContext graphicsContext, AnimationGroup animationGroup)
	{
		if (animationGroup.Textures.Count < 2 || animationGroup.Textures[0].Resource == null || animationGroup.Textures[1].Resource == null)
			return null;
		(byte[] pixels, int width, _) = ReadPng(animationGroup.Textures[0]);
		(byte[] palette, int colourCount, int frames) = ReadPng(animationGroup.Textures[1]);
		int[][] colours = new int[frames][];
		for (int frame = 0; frame < frames; frame++)
		{
			colours[frame] = new int[colourCount];
			for (int i = 0; i < colourCount; i++)
				colours[frame][i] = GetRgb(palette, (frame * colourCount + i) * BytesPerPixel);
		}
		return new SuperSonicPalette(graphicsContext, animationGroup, pixels, width, colours);
	}

	/// <summary>Starts the fade-in, as Sonic_CheckGoSuper does.</summary>
	public void Start()
	{
		_frame = 0;
		_nextFrame = 0;
		_timer = StartTimer;
		_fading = true;
	}

	/// <summary>Runs the cycle for one tick.</summary>
	public void Update()
	{
		if (--_timer >= 0)
			return;
		_frame = _nextFrame++;
		if (_fading)
		{
			_timer = FadeTimer;
			_fading = _nextFrame < FadeFrames;
		}
		else
		{
			_timer = CycleTimer;
			if (_nextFrame > LastCycleFrame)
				_nextFrame = FadeFrames;
		}
	}

	/// <summary>Draws the current frame of the animation with the colours of the current frame of the cycle.</summary>
	public void Render(IObjectRenderer objectRenderer, AnimationInstance animation, Vector2 destination, bool flipX)
	{
		Animation.Frame frame = animation.CurrentFrame;
		Rectanglei source = frame.Source;
		if (source != _uploadedSource || _frame != _uploadedFrame)
		{
			Upload(source);
			_uploadedSource = source;
			_uploadedFrame = _frame;
		}
		Vector2 offset = new Vector2(flipX ? -frame.Offset.X : frame.Offset.X, frame.Offset.Y);
		objectRenderer.Texture = _texture;
		objectRenderer.Render(new Rectangle(0, 0, source.Width, source.Height), destination + offset, flipX);
	}

	public void Dispose()
	{
		_texture.Dispose();
	}

	/// <summary>Copies the pixels of a frame to the texture and changes the colours of the sprites to the colours of the cycle.</summary>
	private void Upload(Rectanglei source)
	{
		Array.Clear(_buffer);
		int[] keys = _colours[0];
		int[] colours = _colours[Math.Min(_frame, _colours.Length - 1)];
		for (int y = 0; y < source.Height; y++)
		{
			int from = ((source.Y + y) * _width + source.X) * BytesPerPixel;
			int to = y * _texture.Width * BytesPerPixel;
			for (int x = 0; x < source.Width; x++, from += BytesPerPixel, to += BytesPerPixel)
			{
				int rgb = GetRgb(_pixels, from);
				int key = Array.IndexOf(keys, rgb);
				if (key >= 0 && _pixels[from + 3] != 0)
					rgb = colours[key];
				// The texture takes BGRA
				_buffer[to] = (byte)rgb;
				_buffer[to + 1] = (byte)(rgb >> 8);
				_buffer[to + 2] = (byte)(rgb >> 16);
				_buffer[to + 3] = _pixels[from + 3];
			}
		}
		_texture.SetArgbData(_texture.Width, _texture.Height, _buffer);
	}

	private static int GetRgb(byte[] rgba, int offset) => (rgba[offset] << 16) | (rgba[offset + 1] << 8) | rgba[offset + 2];

	/// <summary>Decodes the PNG of a texture resource to RGBA rows from the top.</summary>
	private static (byte[] Pixels, int Width, int Height) ReadPng(ITexture texture)
	{
		using Stream stream = texture.Resource.Source.ReadUncompressed();
		PngReader reader = new PngReader(stream);
		try
		{
			int width = reader.ImgInfo.Cols;
			int height = reader.ImgInfo.Rows;
			int channels = reader.ImgInfo.Channels;
			if (channels < 3 || reader.ImgInfo.BitDepth != 8)
				throw new InvalidDataException($"{texture.Resource.FullKeyPath} is not an 8-bit RGB or RGBA image.");
			byte[] pixels = new byte[width * height * BytesPerPixel];
			for (int row = 0; row < height; row++)
			{
				byte[] line = reader.ReadRowByte(row).ScanlineB;
				for (int x = 0; x < width; x++)
				{
					int to = (row * width + x) * BytesPerPixel;
					pixels[to] = line[x * channels];
					pixels[to + 1] = line[x * channels + 1];
					pixels[to + 2] = line[x * channels + 2];
					pixels[to + 3] = channels == 4 ? line[x * channels + 3] : (byte)255;
				}
			}
			return (pixels, width, height);
		}
		finally
		{
			reader.End();
		}
	}
}
