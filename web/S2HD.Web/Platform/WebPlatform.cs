using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using OpenTK.Graphics.OpenGL;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.SDL2;
using GL = SonicOrca.SDL2.GL;

namespace SonicOrca.Web;

/// <summary>
/// Browser platform: a WebGL2 canvas, keyboard input from DOM events, and (for now) silent audio.
/// The page calls into the game once per animation frame, so nothing here blocks.
/// </summary>
public sealed class WebPlatform : IPlatform
{
	private readonly WebWindowContext _window = new WebWindowContext();

	private readonly WebInputContext _input = new WebInputContext();

	private readonly WebAudioContext _audio = new WebAudioContext();

	public WindowContext Window => _window;

	public InputContext Input => _input;

	public AudioContext Audio => _audio;

	public string GraphicsAPI => GL.GetString(StringName.Version);

	public string GraphicsVendor => GL.GetString(StringName.Vendor);

	public int TotalMemory => 0;

	public void Initialise()
	{
	}

	/// <summary>WebGL2 provides what the game needs from OpenGL 3.3.</summary>
	public Version GetOpenGLVersion() => new Version(3, 3);

	public void Dispose()
	{
	}
}

public sealed partial class WebWindowContext : WindowContext, IGLWindow
{
	private readonly ConcurrentQueue<Action> _dispatchedActions = new ConcurrentQueue<Action>();

	private readonly GLGraphicsContext _graphicsContext;

	private Vector2i _clientSize;

	public WebWindowContext()
	{
		ContextThread = Thread.CurrentThread;
		UpdateClientSize();
		_graphicsContext = new GLGraphicsContext(this);
	}

	public Thread ContextThread { get; }

	public override IGraphicsContext GraphicsContext => _graphicsContext;

	/// <summary>The canvas drawing buffer size, which the page keeps in step with its layout size.</summary>
	public override Vector2i ClientSize
	{
		get => _clientSize;
		set { }
	}

	public override string WindowTitle
	{
		get => Js.GetTitle();
		set => Js.SetTitle(value);
	}

	public override bool FullScreen
	{
		get => Js.IsFullScreen();
		set => Js.SetFullScreen(value);
	}

	public void Dispatch(Action action)
	{
		_dispatchedActions.Enqueue(action);
	}

	public override void Update()
	{
		UpdateClientSize();
		while (_dispatchedActions.TryDequeue(out Action action))
			action();
	}

	public override void BeginRender()
	{
		GL.ClearColor(0f, 0f, 0f, 0f);
		_graphicsContext.BlendMode = BlendMode.Alpha;
		_graphicsContext.RenderToBackBuffer();
	}

	/// <summary>The browser presents the canvas when the animation frame callback returns.</summary>
	public override void EndRender()
	{
	}

	public override void Dispose()
	{
	}

	private void UpdateClientSize()
	{
		_clientSize = new Vector2i(Js.GetCanvasWidth(), Js.GetCanvasHeight());
	}

	private static partial class Js
	{
		[JSImport("getCanvasWidth", "host")] public static partial int GetCanvasWidth();
		[JSImport("getCanvasHeight", "host")] public static partial int GetCanvasHeight();
		[JSImport("getTitle", "host")] public static partial string GetTitle();
		[JSImport("setTitle", "host")] public static partial void SetTitle(string title);
		[JSImport("isFullScreen", "host")] public static partial bool IsFullScreen();
		[JSImport("setFullScreen", "host")] public static partial void SetFullScreen(bool fullScreen);
	}
}

public sealed partial class WebInputContext : InputContext
{
	// Indexed by SDL scancode, as the engine's KeyboardState expects; the page maps DOM key codes to them
	private const int KeyCount = 512;

	private readonly byte[] _keys = new byte[KeyCount];

	public override void UpdateCurrentState()
	{
		Js.GetKeyState(_keys);
		bool[] keys = new bool[KeyCount];
		for (int i = 0; i < KeyCount; i++)
			keys[i] = _keys[i] != 0;
		CurrentState = new InputState(default, new KeyboardState(keys), new GamePadInputState[4]);
	}

	public override char GetKeyCode(int scancode) => '\0';

	private static partial class Js
	{
		[JSImport("getKeyState", "host")] public static partial void GetKeyState([JSMarshalAs<JSType.MemoryView>] Span<byte> keys);
	}
}

/// <summary>Silent for the prototype; the mixer still runs so game logic that tracks playback behaves normally.</summary>
public sealed class WebAudioContext : AudioContext
{
}
