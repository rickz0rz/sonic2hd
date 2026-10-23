using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SonicOrca.Audio;
using SonicOrca.Core;
using SonicOrca.Core.Debugging;
using SonicOrca.Core.Network;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;

namespace SonicOrca;

/// <summary>
/// The base class of a game. It owns the platform services and the controllers, and runs the main loop of
/// update, draw and frame timing.
/// </summary>
public abstract class SonicOrcaGameContext : IDisposable
{
	private const int MaxControllers = 4;

	private const int DefaultTargetFrameRate = 60;

	/// <summary>
	/// The frame rate that the frame timer waits for when vsync does not pace the frames.
	/// </summary>
	private const int TimerFrameRate = 60;

	/// <summary>
	/// The frame timer sleeps until the frame is 1/70 second long. After that, it spins for accuracy.
	/// </summary>
	private const int TimerSleepFrameRate = 70;

	/// <summary>
	/// A frame that is shorter than 1/75 second was not paced by vsync, even when vsync is on.
	/// </summary>
	private const int MaxVSyncFrameRate = 75;

	private const int FrameRateMeasureIntervalMs = 1000;

	private readonly IPlatform _platform;

	private readonly ResourceTree _resourceTree;

	private bool _catchup;

	private bool _dirty;

	private int _catchupTick;

	private int _lastUpdateTick;

	private int _lastFrameTick;

	private int _fpsMonitorLastTick;

	private int _fpsMonitorLastUps;

	private int _fpsMonitorLastFps;

	private int _fpsMonitorUpdateCount;

	private int _fpsMonitorDrawCount;

	private Renderer _renderer;

	private Stopwatch _stopwatch;

	private readonly NetworkManager _networkManager = new NetworkManager();

	protected IFramebuffer _canvas;

	private Controller[] _controllersCurrent = new Controller[MaxControllers];

	private Controller[] _controllersPressed = new Controller[MaxControllers];

	private Controller[] _controllersReleased = new Controller[MaxControllers];

	private GamePadOutputState[] _controllersOutput = new GamePadOutputState[MaxControllers];

	public static SonicOrcaGameContext Singleton { get; private set; }

	public static bool IsMaxPerformance { get; set; }

	public Renderer Renderer => _renderer;

	public IPlatform Platform => _platform;

	public AudioContext Audio => _platform.Audio;

	public InputContext Input => _platform.Input;

	public WindowContext Window => _platform.Window;

	public ResourceTree ResourceTree => _resourceTree;

	public int UpdateCount { get; private set; }

	public int DrawCount { get; private set; }

	public bool Finish { get; set; }

	public int TargetFrameRate { get; set; }

	public int LastUps => _fpsMonitorLastUps;

	public int LastFps => _fpsMonitorLastFps;

	public bool Catchup
	{
		get
		{
			return _catchup;
		}
		set
		{
			_catchup = value;
			ResetCatchup();
		}
	}

	public SonicOrcaConsole Console { get; private set; }

	public NetworkManager NetworkManager => _networkManager;

	public bool ForceHD { get; set; }

	public IniConfiguration Configuration { get; protected set; }

	public IReadOnlyList<Controller> Current => _controllersCurrent;

	public IReadOnlyList<Controller> Pressed => _controllersPressed;

	public IReadOnlyList<Controller> Released => _controllersReleased;

	public IList<GamePadOutputState> Output => _controllersOutput;

	public string UserDataDirectory { get; protected set; }

	public IEnumerable<Controller> Controllers => _controllersCurrent.Concat(_controllersPressed).Concat(_controllersReleased);

	public abstract IAudioSettings AudioSettings { get; }

	public abstract IVideoSettings VideoSettings { get; }

	public SonicOrcaGameContext(IPlatform platform)
	{
		Singleton = this;
		_platform = platform;
		_resourceTree = new ResourceTree();
		TargetFrameRate = DefaultTargetFrameRate;
		ForceHD = true;
		for (int i = 0; i < MaxControllers; i++)
		{
			_controllersCurrent[i] = new Controller(this, i, InputStateEventType.Current);
			_controllersPressed[i] = new Controller(this, i, InputStateEventType.Pressed);
			_controllersReleased[i] = new Controller(this, i, InputStateEventType.Released);
		}
		Console = new SonicOrcaConsole(this);
	}

	public virtual void Initialise()
	{
		Window.HideCursorIfIdle = true;
		_renderer = CreateRenderer();
		Stopwatch stopwatch = Stopwatch.StartNew();
		_renderer.Prewarm();
		Trace.WriteLine($"Prewarmed renderers in {stopwatch.ElapsedMilliseconds} ms");
	}

	public virtual void Dispose()
	{
	}

	/// <summary>
	/// Set by hosts that pace frames themselves (e.g. a browser's requestAnimationFrame); skips the frame timer.
	/// </summary>
	public bool ExternalPacing { get; set; }

	/// <summary>
	/// Runs one update and draw, for hosts that cannot block in <see cref="Run"/>.
	/// </summary>
	public void Step()
	{
		MainLoopIteration();
	}

	public void Run()
	{
		ResetCatchup();
		Finish = false;
		while (!Finish)
		{
			MainLoopIteration();
		}
	}

	private void MainLoopIteration()
	{
		_stopwatch ??= new Stopwatch();
		_stopwatch.Start();
		FrameProfiler.BeginFrame();
		if (_catchup)
		{
			UpdateCatchup();
		}
		else
		{
			Update();
		}
		FrameProfiler.Mark(FrameProfiler.Segment.Update);
		if (_dirty)
		{
			Draw(_renderer);
		}
		MeasureFrameRate();
		// With vsync on a 60 Hz display the swap already waited for the refresh. Still fall back to the timer when
		// a frame finished implausibly fast, e.g. drivers that stop blocking while the window is minimised.
		bool pacedByVSync = Window.IsPacedByVSync && _stopwatch.ElapsedTicks >= Stopwatch.Frequency / MaxVSyncFrameRate;
		if (!IsMaxPerformance && !pacedByVSync && !ExternalPacing)
		{
			while (_stopwatch.ElapsedTicks <= Stopwatch.Frequency / TimerFrameRate)
			{
				if (_stopwatch.ElapsedTicks < Stopwatch.Frequency / TimerSleepFrameRate)
				{
					Thread.Sleep(1);
				}
			}
		}
		FrameProfiler.Mark(FrameProfiler.Segment.Wait);
		FrameProfiler.EndFrame();
		_stopwatch.Reset();
	}

	private void MeasureFrameRate()
	{
		if (Environment.TickCount - _fpsMonitorLastTick > FrameRateMeasureIntervalMs)
		{
			_fpsMonitorLastUps = UpdateCount - _fpsMonitorUpdateCount;
			_fpsMonitorLastFps = DrawCount - _fpsMonitorDrawCount;
			_fpsMonitorLastTick = Environment.TickCount;
			_fpsMonitorUpdateCount = UpdateCount;
			_fpsMonitorDrawCount = DrawCount;
		}
	}

	public void ResetCatchup()
	{
		_catchupTick = Environment.TickCount;
	}

	/// <summary>
	/// Runs as many updates as the elapsed time needs at <see cref="TargetFrameRate"/>. When the game is too far
	/// behind, it runs one update and starts to count again.
	/// </summary>
	private void UpdateCatchup()
	{
		int expectedUpdates = (Environment.TickCount - _catchupTick) * TargetFrameRate / 1000;
		// Note: _lastFrameTick is never set, so this test is against two seconds of updates from the start of the count.
		if (expectedUpdates > _lastFrameTick + TargetFrameRate * 2)
		{
			_lastUpdateTick = 0;
			_catchupTick = Environment.TickCount;
			Update();
		}
		else
		{
			while (expectedUpdates > _lastUpdateTick)
			{
				Update();
				_lastUpdateTick++;
			}
		}
	}

	public void Update()
	{
		Window.Update();
		if (Window.Finished)
		{
			Finish = true;
		}
		Audio.Update();
		Input.Update();
		OnUpdate();
		Input.UpdatePressedReleased();
		OnUpdateStep();
		_dirty = true;
		UpdateCount++;
	}

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnUpdateStep()
	{
	}

	public void Draw(Renderer renderer)
	{
		Window.BeginRender();
		OnDraw();
		renderer.DeativateRenderer();
		Window.EndRender();
		DrawCount++;
	}

	protected virtual void OnDraw()
	{
	}

	protected abstract Renderer CreateRenderer();

	protected internal abstract ILevelRenderer CreateLevelRenderer(Level level);
}
