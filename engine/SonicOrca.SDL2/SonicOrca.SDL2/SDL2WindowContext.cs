#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SDL2;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

public class SDL2WindowContext : WindowContext
{
	private static class User32
	{
		public const int WM_SETICON = 128;

		public const int ICON_SMALL = 0;

		[DllImport("user32.dll")]
		public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
	}

	public const int DEFAULT_HICON_RESOURCE_ID = 32512;

	private const int HideMouseIdleTimeMs = 3000;

	private readonly SDL2Platform _platform;

	private readonly ConcurrentQueue<Action> _dispatchedActions = new ConcurrentQueue<Action>();

	private readonly GLGraphicsContext _glGraphicsContext;

	private IntPtr _windowHandle;

	private IntPtr _glContext;

	private Vector2i _clientSize;

	private Vector2i _aspectRatio;

	private VideoMode _mode;

	private string _windowTitle;

	private uint _lastMouseMovementTickCount;

	private bool _showingCursor = true;

	private SDL.SDL_Event[] events = new SDL.SDL_Event[512];

	public IntPtr WindowHandle => _windowHandle;

	public Thread ContextThread { get; private set; }

	public override SonicOrca.Graphics.IGraphicsContext GraphicsContext => _glGraphicsContext;

	public bool IsMaxPerformance { get; set; }

	public override bool FullScreen
	{
		get
		{
			return Mode != VideoMode.Windowed;
		}
		set
		{
			if (value)
			{
				if (Mode == VideoMode.Windowed)
				{
					Mode = VideoMode.WindowedBorderless;
				}
			}
			else
			{
				Mode = VideoMode.Windowed;
			}
		}
	}

	public override VideoMode Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			if (_mode != value)
			{
				_mode = value;
				uint flags = 0u;
				switch (value)
				{
				case VideoMode.Fullscreen:
					flags = 1u;
					break;
				case VideoMode.WindowedBorderless:
					flags = 4097u;
					break;
				}
				SDL.SDL_SetWindowFullscreen(_windowHandle, flags);
				if (value == VideoMode.Windowed)
				{
					UpdateWindowSize(new Vector2i(1920, 1080));
				}
			}
		}
	}

	public override string WindowTitle
	{
		get
		{
			return _windowTitle;
		}
		set
		{
			_windowTitle = value;
			if (_windowHandle != IntPtr.Zero)
			{
				SDL.SDL_SetWindowTitle(_windowHandle, value);
			}
		}
	}

	public override Vector2i ClientSize
	{
		get
		{
			return _clientSize;
		}
		set
		{
			UpdateWindowSize(value);
		}
	}

	public override Vector2i AspectRatio
	{
		get
		{
			return _aspectRatio;
		}
		set
		{
			if (_aspectRatio != value)
			{
				_aspectRatio = value;
				UpdateWindowSize();
			}
		}
	}

	public override bool HideCursorIfIdle
	{
		get
		{
			return base.HideCursorIfIdle;
		}
		set
		{
			base.HideCursorIfIdle = value;
			if (value)
			{
				ShowCursor = true;
			}
		}
	}

	public bool ShowCursor
	{
		get
		{
			return _showingCursor;
		}
		set
		{
			if (_showingCursor != value)
			{
				_showingCursor = value;
				SDL.SDL_ShowCursor(value ? 1 : 0);
			}
		}
	}

	public SDL2WindowContext(SDL2Platform platform)
	{
		_platform = platform;
		Trace.WriteLine("Initialising SDL2 video");
		if (SDL.SDL_InitSubSystem(32u) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise a video driver.");
		}
		SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES, 1);
		SDL.SDL_GL_SetSwapInterval(1);
		SDL.SDL_GetDesktopDisplayMode(0, out var mode);
		int num = 1920;
		int num2 = 1080;
		while (num > mode.w || num2 > mode.h)
		{
			num -= 480;
			num2 -= 270;
		}
		Trace.WriteLine("Creating window");
		_windowHandle = SDL.SDL_CreateWindow(_windowTitle, 805240832, 805240832, num, num2, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
		if (_windowHandle == IntPtr.Zero)
		{
			throw SDL2Exception.FromError("Unable to create window.");
		}
		_clientSize = new Vector2i(num, num2);
		SetIconToAssemblyResource();
		Trace.WriteLine("Creating OpenGL context");
		_glContext = SDL.SDL_GL_CreateContext(_windowHandle);
		if (_glContext == IntPtr.Zero)
		{
			throw SDL2Exception.FromError("Unable to create OpenGL context.");
		}
		SetOpenTKOpenGLHandle(_glContext, GetWindowHWND(_windowHandle));
		ContextThread = Thread.CurrentThread;
		_glGraphicsContext = new GLGraphicsContext(this);
		ShowWindowWithBlackBackground();
	}

	private void SetIconToAssemblyResource()
	{
		IntPtr intPtr = User32.LoadIcon(Marshal.GetHINSTANCE(Assembly.GetEntryAssembly().ManifestModule), new IntPtr(32512));
		if (intPtr != IntPtr.Zero)
		{
			User32.SendMessage(GetWindowHWND(_windowHandle), 128u, new IntPtr(0), intPtr);
		}
	}

	// SDL owns the GL context; OpenTK only needs SDL's loader to resolve GL entry points
	private sealed class SDL2BindingsContext : IBindingsContext
	{
		public IntPtr GetProcAddress(string procName) => SDL.SDL_GL_GetProcAddress(procName);
	}

	private void SetOpenTKOpenGLHandle(IntPtr glHandle, IntPtr windowHandle)
	{
		GL.LoadBindings(new SDL2BindingsContext());
	}

	private IntPtr GetWindowHWND(IntPtr window)
	{
		SDL.SDL_SysWMinfo info = default;
		SDL.SDL_GetWindowWMInfo(window, ref info);
		return info.info.win.window;
	}

	public override void Dispose()
	{
		Trace.WriteLine("Deleting OpenGL context");
		SDL.SDL_GL_DeleteContext(_glContext);
		Trace.WriteLine("Closing window");
		SDL.SDL_DestroyWindow(_windowHandle);
		Trace.WriteLine("Quitting SDL2 video");
		SDL.SDL_QuitSubSystem(32u);
	}

	public override void Update()
	{
		uint num = SDL.SDL_GetTicks();
		SDL.SDL_PumpEvents();
		int num2 = SDL.SDL_PeepEvents(events, events.Length, SDL.SDL_eventaction.SDL_PEEKEVENT, SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);
		for (int i = 0; i < num2; i++)
		{
			SDL.SDL_Event sDL_Event = events[i];
			if (sDL_Event.type == SDL.SDL_EventType.SDL_QUIT)
			{
				Finished = true;
			}
			switch (sDL_Event.type)
			{
			case SDL.SDL_EventType.SDL_MOUSEMOTION:
				if (HideCursorIfIdle)
				{
					_lastMouseMovementTickCount = num;
					ShowCursor = true;
				}
				break;
			case SDL.SDL_EventType.SDL_KEYDOWN:
				if ((sDL_Event.key.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0 && sDL_Event.key.keysym.sym == SDL.SDL_Keycode.SDLK_F4)
				{
					Finished = true;
				}
				break;
			case SDL.SDL_EventType.SDL_WINDOWEVENT:
				switch (sDL_Event.window.windowEvent)
				{
				case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
					Finished = true;
					break;
				case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
					UpdateWindowSize(new Vector2i(sDL_Event.window.data1, sDL_Event.window.data2));
					break;
				}
				break;
			}
		}
		Action result;
		while (_dispatchedActions.TryDequeue(out result))
		{
			result();
		}
		_glGraphicsContext.Update();
		if (HideCursorIfIdle && num - _lastMouseMovementTickCount > 3000)
		{
			ShowCursor = false;
		}
	}

	public override void BeginRender()
	{
		GL.ClearColor(0f, 0f, 0f, 0f);
		_glGraphicsContext.BlendMode = BlendMode.Alpha;
		_glGraphicsContext.RenderToBackBuffer();
	}

	public override void EndRender()
	{
		if (!SonicOrcaGameContext.IsMaxPerformance)
		{
			GL.Finish();
		}
		SDL.SDL_GL_SwapWindow(_windowHandle);
	}

	public void Dispatch(Action action)
	{
		_dispatchedActions.Enqueue(action);
	}

	private void ShowWindowWithBlackBackground()
	{
		GL.ClearColor(0f, 0f, 0f, 1f);
		SDL.SDL_GL_SwapWindow(_windowHandle);
		SDL.SDL_ShowWindow(_windowHandle);
	}

	private void UpdateWindowSize()
	{
		UpdateWindowSize(_clientSize);
	}

	private void UpdateWindowSize(Vector2i size)
	{
		if (_aspectRatio.X != 0 || _aspectRatio.Y != 0)
		{
			double num = (double)_aspectRatio.X / (double)_aspectRatio.Y;
			if (_clientSize.X != size.X)
			{
				size.Y = (int)((double)size.X / num);
			}
			else if (_clientSize.Y != size.Y)
			{
				size.X = (int)((double)size.Y * num);
			}
		}
		_clientSize = size;
		SDL.SDL_SetWindowSize(_windowHandle, size.X, size.Y);
		GL.Viewport(0, 0, size.X, size.Y);
	}
}
