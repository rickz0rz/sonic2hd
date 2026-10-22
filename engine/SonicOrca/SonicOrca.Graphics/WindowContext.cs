using System;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public abstract class WindowContext : IDisposable
{
	public virtual bool FullScreen { get; set; }

	public virtual VideoMode Mode { get; set; }

	public virtual string WindowTitle { get; set; }

	public virtual Vector2i ClientSize { get; set; }

	public virtual Vector2i AspectRatio { get; set; }

	public virtual bool HideCursorIfIdle { get; set; }

	public bool Finished { get; protected set; }

	/// <summary>
	/// Synchronise buffer swaps to the display. Off by default: windowed games are composited without tearing on
	/// Windows and macOS, and on Intel HD 4000 vsync measured worse frame pacing than the timer.
	/// </summary>
	public virtual bool VSync { get; set; }

	/// <summary>
	/// True when buffer swaps are synchronised to a 60 Hz display, so presenting a frame already paces the game.
	/// </summary>
	public virtual bool IsPacedByVSync => false;

	public virtual IGraphicsContext GraphicsContext => null;

	public Rectanglei ClientBounds
	{
		get
		{
			Vector2i clientSize = ClientSize;
			return new Rectanglei(0, 0, clientSize.X, clientSize.Y);
		}
	}

	public abstract void Dispose();

	public virtual void Update()
	{
	}

	public virtual void BeginRender()
	{
	}

	public virtual void EndRender()
	{
	}
}
