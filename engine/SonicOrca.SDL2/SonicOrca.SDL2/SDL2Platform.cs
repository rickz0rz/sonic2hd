#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using SDL2;
using SonicOrca.Audio;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace SonicOrca.SDL2;

public class SDL2Platform : IPlatform, IDisposable
{
	private static readonly SDL2Platform _singleton = new SDL2Platform();

	private bool _initialised;

	private SDL2WindowContext _window;

	private SDL2InputContext _input;

	private SDL2AudioContext _audio;

	public WindowContext Window => _window;

	public InputContext Input => _input;

	public AudioContext Audio => _audio;

	public static SDL2Platform Instance => _singleton;

	public string GraphicsAPI
	{
		get
		{
			Version openGLVersion = GetOpenGLVersion();
			return $"OpenGL {openGLVersion.Major}.{openGLVersion.Minor}";
		}
	}

	public string GraphicsVendor => GL.GetString(StringName.Vendor);

	public int TotalMemory => SDL.SDL_GetSystemRAM();

	[DllImport("kernel32.dll")]
	private static extern IntPtr LoadLibrary(string path);

	private SDL2Platform()
	{
	}

	public void Initialise()
	{
		if (_initialised)
		{
			throw new InvalidOperationException("Platform already initialised.");
		}
		_initialised = true;
		Trace.WriteLine("Initialising SDL2 platform");
		Trace.WriteLine("-- SDL2 " + GetVersion());
		Trace.Indent();
		_window = new SDL2WindowContext(this);
		_input = new SDL2InputContext(this);
		_audio = new SDL2AudioContext(this);
		Trace.Unindent();
	}

	public void Dispose()
	{
		if (!_initialised)
		{
			throw new InvalidOperationException("Platform not initialised.");
		}
		_initialised = false;
		Trace.WriteLine("Disposing SDL2 platform");
		Trace.Indent();
		if (_audio != null)
		{
			_audio.Dispose();
		}
		if (_input != null)
		{
			_input.Dispose();
		}
		if (_window != null)
		{
			_window.Dispose();
		}
		Trace.Unindent();
	}

	public Version GetVersion()
	{
		SDL.SDL_VERSION(out var x);
		return new Version(x.major, x.minor, x.patch);
	}

	public Version GetOpenGLVersion()
	{
		if (!_initialised)
		{
			throw new InvalidOperationException("Platform not initialised.");
		}
		GL.GetInteger(GetPName.MajorVersion, out var data);
		GL.GetInteger(GetPName.MinorVersion, out var data2);
		return new Version(data, data2);
	}
}
