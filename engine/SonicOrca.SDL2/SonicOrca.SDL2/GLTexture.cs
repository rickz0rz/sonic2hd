using System;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.SDL2;

internal class GLTexture : ITexture, IDisposable, ILoadedResource
{
	private readonly GLGraphicsContext _context;

	private int _glId;

	private TextureFiltering _filtering;

	private TextureWrapping _wrapping;

	public Resource Resource { get; set; }

	public int Width { get; private set; }

	public int Height { get; private set; }

	public TextureFiltering Filtering
	{
		get
		{
			return _filtering;
		}
		set
		{
			if (value != _filtering)
			{
				TextureMinFilter param = TextureMinFilter.Linear;
				TextureMagFilter param2 = TextureMagFilter.Linear;
				if ((_filtering = value) == TextureFiltering.NearestNeighbour)
				{
					param = TextureMinFilter.Nearest;
					param2 = TextureMagFilter.Nearest;
				}
				GL.BindTexture(TextureTarget.Texture2D, _glId);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)param);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)param2);
			}
		}
	}

	public TextureWrapping Wrapping
	{
		get
		{
			return _wrapping;
		}
		set
		{
			if (value != _wrapping)
			{
				_wrapping = value;
				TextureWrapMode param = value switch
				{
					TextureWrapping.Clamp => TextureWrapMode.Clamp, 
					TextureWrapping.RepeatMirrored => TextureWrapMode.MirroredRepeat, 
					_ => TextureWrapMode.Repeat, 
				};
				GL.BindTexture(TextureTarget.Texture2D, _glId);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)param);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)param);
			}
		}
	}

	public int Id => _glId;

	public GLTexture(GLGraphicsContext context, int width, int height, int channels, byte[] argb, bool toCompress = false)
	{
		_context = context;
		Width = width;
		Height = height;
		_glId = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, _glId);
		if (toCompress)
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.CompressedRgbaS3tcDxt1Ext, width, height, 0, (channels == 3) ? PixelFormat.Rgb : PixelFormat.Rgba, PixelType.UnsignedByte, argb);
		}
		else
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, (channels == 3) ? PixelFormat.Rgb : PixelFormat.Rgba, PixelType.UnsignedByte, argb);
		}
		_filtering = TextureFiltering.Linear;
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		_wrapping = TextureWrapping.Clamp;
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10496);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10496);
		_context.Textures.Add(this);
	}

	public void Dispose()
	{
		GL.DeleteTexture(_glId);
		_context.Textures.Remove(this);
	}

	public void OnLoaded()
	{
	}

	public byte[] GetArgbData()
	{
		byte[] array = new byte[Width * Height * 4];
		GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
		int num = Width * 4;
		for (int i = 0; i < Height / 2; i++)
		{
			for (int j = 0; j < Width; j++)
			{
				int num2 = i * num + j * 4;
				int num3 = (Height - i - 1) * num + j * 4;
				byte b = array[num2];
				byte b2 = array[num2 + 1];
				byte b3 = array[num2 + 2];
				byte b4 = array[num2 + 3];
				array[num2] = array[num3];
				array[num2 + 1] = array[num3 + 1];
				array[num2 + 2] = array[num3 + 2];
				array[num2 + 3] = array[num3 + 3];
				array[num3] = b;
				array[num3 + 1] = b2;
				array[num3 + 2] = b3;
				array[num3 + 3] = b4;
			}
		}
		for (int k = 0; k < array.Length; k += 4)
		{
			byte b5 = array[k];
			byte b6 = array[k + 1];
			byte b7 = array[k + 2];
			byte b8 = array[k + 3];
			array[k] = b7;
			array[k + 1] = b6;
			array[k + 2] = b5;
			array[k + 3] = b8;
		}
		return array;
	}

	public void SetArgbData(int width, int height, byte[] data)
	{
		PixelFormat format = PixelFormat.Bgra;
		if (data.Length < width * height * 4)
		{
			format = PixelFormat.Bgr;
		}
		GL.BindTexture(TextureTarget.Texture2D, _glId);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, format, PixelType.UnsignedByte, data);
	}
}
