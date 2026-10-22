using System;
using SonicOrca.Graphics;

namespace SonicOrca.Geometry;

public class Viewport
{
	private Rectanglei _bounds;

	private Rectanglei _destination;

	private Vector2 _scale;

	public Rectanglei Bounds
	{
		get
		{
			return _bounds;
		}
		set
		{
			_bounds = value;
			CalculateScale();
		}
	}

	public Rectanglei Destination
	{
		get
		{
			return _destination;
		}
		set
		{
			_destination = value;
			CalculateScale();
		}
	}

	public Vector2 Scale => _scale;

	public Viewport(Rectanglei destination)
	{
		Bounds = new Rectangle(0.0, 0.0, destination.Width, destination.Height);
		Destination = destination;
	}

	public Viewport(Rectanglei destination, Rectanglei bounds)
	{
		Destination = destination;
		Bounds = bounds;
	}

	private void CalculateScale()
	{
		_scale = new Vector2((double)Destination.Width / (double)Bounds.Width, (double)Destination.Height / (double)Bounds.Height);
	}

	public Vector2i GetAbsolutePosition(Vector2i position)
	{
		return GetAbsolutePosition(position.X, position.Y);
	}

	public Vector2i GetAbsolutePosition(int x, int y)
	{
		Vector2 scale = Scale;
		return new Vector2i(Bounds.X + (int)((double)x * scale.X), Bounds.Y + (int)((double)y * scale.Y));
	}

	public Vector2i GetRelativePosition(Vector2i position)
	{
		return GetRelativePosition(position.X, position.Y);
	}

	public Vector2i GetRelativePosition(int x, int y)
	{
		Vector2 scale = Scale;
		return new Vector2i((int)((double)(x - Bounds.X) / scale.X), (int)((double)(y - Bounds.Y) / scale.Y));
	}

	public IDisposable ApplyRendererState(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		IDisposable result = i2dRenderer.BeginMatixState();
		i2dRenderer.ClipRectangle = Destination;
		Vector2 scale = Scale;
		i2dRenderer.ModelMatrix = Matrix4.CreateTranslation(Destination.X, Destination.Y) * Matrix4.CreateScale(scale.X, scale.Y);
		return result;
	}
}
