#define TRACE
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using SDL2;
using SonicOrca.Audio;
using SonicOrca.Graphics;
using SonicOrca.Input;

namespace SonicOrca.SDL2;

/// <summary>The desktop platform: window, input and audio through SDL2. Use <see cref="Instance"/>.</summary>
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

	static SDL2Platform()
	{
		SDL2NativeLibrary.Register();
	}

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
		_audio?.Dispose();
		_input?.Dispose();
		_window?.Dispose();
		Trace.Unindent();
	}

	public Version GetVersion()
	{
		SDL.SDL_VERSION(out SDL.SDL_version version);
		return new Version(version.major, version.minor, version.patch);
	}

	public Version GetOpenGLVersion()
	{
		if (!_initialised)
		{
			throw new InvalidOperationException("Platform not initialised.");
		}
		GL.GetInteger(GetPName.MajorVersion, out int major);
		GL.GetInteger(GetPName.MinorVersion, out int minor);
		return new Version(major, minor);
	}
}
