using System;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.SDL2;

/// <summary>An OpenGL 2D texture. New textures use linear filtering and clamp to the edge.</summary>
internal class GLTexture : ITexture, IDisposable, ILoadedResource
{
	private readonly GLGraphicsContext _context;

	private readonly int _id;

	private TextureFiltering _filtering;

	private TextureWrapping _wrapping;

	public Resource Resource { get; set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public int Id => _id;

	public TextureFiltering Filtering
	{
		get => _filtering;
		set
		{
			if (value == _filtering)
			{
				return;
			}
			_filtering = value;
			bool nearest = value == TextureFiltering.NearestNeighbour;
			GL.BindTexture(TextureTarget.Texture2D, _id);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)(nearest ? TextureMinFilter.Nearest : TextureMinFilter.Linear));
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)(nearest ? TextureMagFilter.Nearest : TextureMagFilter.Linear));
		}
	}

	public TextureWrapping Wrapping
	{
		get => _wrapping;
		set
		{
			if (value == _wrapping)
			{
				return;
			}
			_wrapping = value;
			TextureWrapMode wrapS = value switch
			{
				TextureWrapping.Clamp => TextureWrapMode.ClampToEdge,
				TextureWrapping.RepeatMirrored => TextureWrapMode.MirroredRepeat,
				_ => TextureWrapMode.Repeat,
			};
			TextureWrapMode wrapT = value == TextureWrapping.RepeatHorizontal ? TextureWrapMode.ClampToEdge : wrapS;
			GL.BindTexture(TextureTarget.Texture2D, _id);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);
		}
	}

	/// <param name="pixels">RGBA (or RGB when <paramref name="channels"/> is 3) rows, from the top.</param>
	/// <param name="toCompress">Stores the texture as DXT1, which has no alpha channel.</param>
	public GLTexture(GLGraphicsContext context, int width, int height, int channels, byte[] pixels, bool toCompress = false)
	{
		_context = context;
		Width = width;
		Height = height;
		_id = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, _id);
		PixelInternalFormat internalFormat = toCompress ? PixelInternalFormat.CompressedRgbaS3tcDxt1Ext : PixelInternalFormat.Rgba;
		PixelFormat format = channels == 3 ? PixelFormat.Rgb : PixelFormat.Rgba;
		GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, PixelType.UnsignedByte, pixels);
		_filtering = TextureFiltering.Linear;
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
		_wrapping = TextureWrapping.Clamp;
		// GL_CLAMP (10496) in the original is invalid in core profiles and WebGL, which silently left textures repeating
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
		_context.Textures.Add(this);
	}

	public void Dispose()
	{
		GL.DeleteTexture(_id);
		_context.Textures.Remove(this);
	}

	public void OnLoaded()
	{
	}

	/// <summary>Reads the texture back as BGRA rows from the top (OpenGL returns RGBA rows from the bottom).</summary>
	public byte[] GetArgbData()
	{
		int stride = Width * 4;
		byte[] rgba = new byte[stride * Height];
		GL.BindTexture(TextureTarget.Texture2D, _id);
		GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, rgba);
		byte[] bgra = new byte[rgba.Length];
		for (int y = 0; y < Height; y++)
		{
			int sourceRow = (Height - 1 - y) * stride;
			int targetRow = y * stride;
			for (int x = 0; x < stride; x += 4)
			{
				bgra[targetRow + x] = rgba[sourceRow + x + 2];
				bgra[targetRow + x + 1] = rgba[sourceRow + x + 1];
				bgra[targetRow + x + 2] = rgba[sourceRow + x];
				bgra[targetRow + x + 3] = rgba[sourceRow + x + 3];
			}
		}
		return bgra;
	}

	/// <summary>Replaces the texture with BGRA data, or BGR data when <paramref name="data"/> is too short for BGRA.</summary>
	public void SetArgbData(int width, int height, byte[] data)
	{
		PixelFormat format = data.Length < width * height * 4 ? PixelFormat.Bgr : PixelFormat.Bgra;
		GL.BindTexture(TextureTarget.Texture2D, _id);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, format, PixelType.UnsignedByte, data);
	}
}
