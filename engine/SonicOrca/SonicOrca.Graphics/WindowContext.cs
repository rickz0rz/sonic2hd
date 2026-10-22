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
