#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SDL2;
using SonicOrca.Core.Debugging;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.SDL2;

/// <summary>
/// The game window: an SDL2 window with an OpenGL 3.3 core-profile context. It also runs actions that other threads
/// queue with <see cref="Dispatch"/>, because GL objects must be created on the context thread.
/// </summary>
public class SDL2WindowContext : WindowContext, IGLWindow
{
	private static class User32
	{
		public const uint WM_SETICON = 128;

		public const int ICON_SMALL = 0;

		[DllImport("user32.dll")]
		public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);
	}

	public const int DEFAULT_HICON_RESOURCE_ID = 32512;

	private const int HideMouseIdleTimeMs = 3000;

	private readonly SDL2Platform _platform;

	private readonly ConcurrentQueue<Action> _dispatchedActions = new ConcurrentQueue<Action>();

	private readonly GLGraphicsContext _glGraphicsContext;

	private IntPtr _windowHandle;

	private IntPtr _glContext;

	// Signalled when the GPU finishes the previous frame; bounds how far the CPU may run ahead
	private IntPtr _previousFrameFence;

	private Vector2i _clientSize;

	private Vector2i _aspectRatio;

	private VideoMode _mode;

	private string _windowTitle;

	private uint _lastMouseMovementTickCount;

	private bool _showingCursor = true;

	private readonly SDL.SDL_Event[] _events = new SDL.SDL_Event[512];

	private bool _vsync;

	private bool _pacedByVSync;

	private int _refreshRate;

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
				uint flags = value switch
				{
					VideoMode.Fullscreen => (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN,
					VideoMode.WindowedBorderless => (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP,
					_ => 0u,
				};
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

	public override bool VSync
	{
		get => _vsync;
		set
		{
			_vsync = value;
			ApplySwapInterval();
		}
	}

	public override bool IsPacedByVSync => _pacedByVSync;

	public SDL2WindowContext(SDL2Platform platform)
	{
		_platform = platform;
		Trace.WriteLine("Initialising SDL2 video");
		if (SDL.SDL_InitSubSystem(SDL.SDL_INIT_VIDEO) != 0)
		{
			throw SDL2Exception.FromError("Unable to initialise a video driver.");
		}
		// Core profile: the only kind macOS offers (up to 4.1), and it must be forward-compatible
		SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
		SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 3);
		SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
		SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL.SDL_GLcontext.SDL_GL_CONTEXT_FORWARD_COMPATIBLE_FLAG);
		SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES, 1);
		// The largest 16:9 size up to 1920x1080 that fits on the desktop, in steps of a quarter of 1080p
		SDL.SDL_GetDesktopDisplayMode(0, out SDL.SDL_DisplayMode mode);
		int width = 1920;
		int height = 1080;
		while (width > mode.w || height > mode.h)
		{
			width -= 480;
			height -= 270;
		}
		Trace.WriteLine("Creating window");
		_windowHandle = SDL.SDL_CreateWindow(_windowTitle, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, width, height,
			SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
		if (_windowHandle == IntPtr.Zero)
		{
			throw SDL2Exception.FromError("Unable to create window.");
		}
		_clientSize = new Vector2i(width, height);
		SetIconToAssemblyResource();
		Trace.WriteLine("Creating OpenGL context");
		_glContext = SDL.SDL_GL_CreateContext(_windowHandle);
		if (_glContext == IntPtr.Zero)
		{
			throw SDL2Exception.FromError("Unable to create OpenGL context.");
		}
		SetOpenTKOpenGLHandle();
		_refreshRate = mode.refresh_rate;
		ApplySwapInterval();
		ContextThread = Thread.CurrentThread;
		_glGraphicsContext = new GLGraphicsContext(this);
		ShowWindowWithBlackBackground();
	}

	private void SetIconToAssemblyResource()
	{
		// macOS takes the Dock icon from the app bundle
		if (!OperatingSystem.IsWindows())
		{
			return;
		}
		IntPtr icon = User32.LoadIcon(User32.GetModuleHandle(null), new IntPtr(DEFAULT_HICON_RESOURCE_ID));
		if (icon != IntPtr.Zero)
		{
			User32.SendMessage(GetWindowHWND(_windowHandle), User32.WM_SETICON, new IntPtr(User32.ICON_SMALL), icon);
		}
	}

	// SDL owns the GL context; OpenTK only needs SDL's loader to resolve GL entry points
	private sealed class SDL2BindingsContext : IBindingsContext
	{
		public IntPtr GetProcAddress(string procName) => SDL.SDL_GL_GetProcAddress(procName);
	}

	private void SetOpenTKOpenGLHandle()
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
		SDL.SDL_QuitSubSystem(SDL.SDL_INIT_VIDEO);
	}

	public override void Update()
	{
		uint now = SDL.SDL_GetTicks();
		// Peek only: SDL2InputContext reads the same events later in the update, then removes them
		SDL.SDL_PumpEvents();
		int eventCount = SDL.SDL_PeepEvents(_events, _events.Length, SDL.SDL_eventaction.SDL_PEEKEVENT, SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);
		for (int i = 0; i < eventCount; i++)
		{
			SDL.SDL_Event e = _events[i];
			switch (e.type)
			{
				case SDL.SDL_EventType.SDL_QUIT:
					Finished = true;
					break;
				case SDL.SDL_EventType.SDL_MOUSEMOTION:
					if (HideCursorIfIdle)
					{
						_lastMouseMovementTickCount = now;
						ShowCursor = true;
					}
					break;
				case SDL.SDL_EventType.SDL_KEYDOWN:
					if ((e.key.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != 0 && e.key.keysym.sym == SDL.SDL_Keycode.SDLK_F4)
					{
						Finished = true;
					}
					break;
				case SDL.SDL_EventType.SDL_WINDOWEVENT:
					switch (e.window.windowEvent)
					{
						case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
							Finished = true;
							break;
						case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
							UpdateWindowSize(new Vector2i(e.window.data1, e.window.data2));
							break;
					}
					break;
			}
		}
		while (_dispatchedActions.TryDequeue(out Action action))
		{
			action();
		}
		_glGraphicsContext.Update();
		if (HideCursorIfIdle && now - _lastMouseMovementTickCount > HideMouseIdleTimeMs)
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
		FrameProfiler.Mark(FrameProfiler.Segment.Draw);
		// The original called GL.Finish() here, which serialised CPU and GPU work. Waiting for the previous
		// frame instead lets the GPU render one frame while the CPU builds the next, without queueing more.
		if (!SonicOrcaGameContext.IsMaxPerformance)
		{
			WaitForPreviousFrame();
		}
		FrameProfiler.Mark(FrameProfiler.Segment.GpuFinish);
		SDL.SDL_GL_SwapWindow(_windowHandle);
		_previousFrameFence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
		FrameProfiler.Mark(FrameProfiler.Segment.Swap);
	}

	private void ApplySwapInterval()
	{
		// The original requested vsync before creating the context, which has no effect, leaving it to the driver.
		// When enabled, prefer adaptive vsync (a late frame tears instead of waiting a whole extra refresh).
		if (!_vsync)
		{
			SDL.SDL_GL_SetSwapInterval(0);
		}
		else if (SDL.SDL_GL_SetSwapInterval(-1) != 0)
		{
			SDL.SDL_GL_SetSwapInterval(1);
		}
		int interval = SDL.SDL_GL_GetSwapInterval();
		// The game logic runs once per frame, so vsync can only replace the timer when the display runs at 60 Hz
		_pacedByVSync = interval != 0 && _refreshRate >= 59 && _refreshRate <= 61;
		Trace.WriteLine($"Display refresh {_refreshRate} Hz, swap interval {interval}, {(_pacedByVSync ? "paced by vsync" : "paced by timer")}");
	}

	private void WaitForPreviousFrame()
	{
		if (_previousFrameFence == IntPtr.Zero)
		{
			return;
		}
		const long OneSecondNs = 1_000_000_000;
		while (GL.ClientWaitSync(_previousFrameFence, ClientWaitSyncFlags.SyncFlushCommandsBit, OneSecondNs) == WaitSyncStatus.TimeoutExpired)
		{
		}
		GL.DeleteSync(_previousFrameFence);
		_previousFrameFence = IntPtr.Zero;
	}

	public void Dispatch(Action action)
	{
		_dispatchedActions.Enqueue(action);
	}

	private void ShowWindowWithBlackBackground()
	{
		GL.ClearColor(0f, 0f, 0f, 1f);
		GL.Clear(ClearBufferMask.ColorBufferBit);
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
			// Keep the aspect ratio, from the dimension that changed
			double ratio = _aspectRatio.X / (double)_aspectRatio.Y;
			if (_clientSize.X != size.X)
			{
				size.Y = (int)(size.X / ratio);
			}
			else if (_clientSize.Y != size.Y)
			{
				size.X = (int)(size.Y * ratio);
			}
		}
		_clientSize = size;
		SDL.SDL_SetWindowSize(_windowHandle, size.X, size.Y);
		GL.Viewport(0, 0, size.X, size.Y);
	}
}
