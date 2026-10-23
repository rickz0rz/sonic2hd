using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Collision;

/// <summary>
/// A rectangular hit box. The bounds are relative to the owner object.
/// </summary>
public class CollisionRectangle : IBounds
{
	private readonly ActiveObject _owner;

	public ActiveObject Owner => _owner;

	public int Id { get; set; }

	public Rectanglei Bounds { get; set; }

	public Rectanglei AbsoluteBounds
	{
		get
		{
			if (_owner != null)
			{
				return Bounds.OffsetBy(Owner.Position);
			}
			return Bounds;
		}
	}

	public CollisionRectangle(ActiveObject owner, int id, int x, int y, int width, int height)
	{
		_owner = owner;
		Id = id;
		Bounds = new Rectangle(x, y, width, height);
	}

	public void OffsetBy(Vector2i offset)
	{
		Rectangle rectangle = Bounds;
		rectangle.Location += (Vector2)offset;
		Bounds = rectangle;
	}

	public bool IntersectsWith(CollisionRectangle other)
	{
		return AbsoluteBounds.IntersectsWith(other.AbsoluteBounds);
	}

	public void Draw(Renderer renderer, Viewport viewport)
	{
		if (viewport.Bounds.IntersectsWith(AbsoluteBounds))
		{
			Rectangle destination = AbsoluteBounds.OffsetBy(viewport.Bounds.Location * -1);
			destination.X *= viewport.Scale.X;
			destination.Y *= viewport.Scale.Y;
			destination.Width *= viewport.Scale.X;
			destination.Height *= viewport.Scale.Y;
			I2dRenderer i2dRenderer = renderer.Get2dRenderer();
			Colour colour = new Colour(byte.MaxValue, 0, 0);
			i2dRenderer.RenderRectangle(colour, destination, 1.0);
			i2dRenderer.RenderLine(colour, new Vector2(destination.X + destination.Width / 2.0, destination.Y), new Vector2(destination.X + destination.Width / 2.0, destination.Y + destination.Height), 1.0);
			i2dRenderer.RenderLine(colour, new Vector2(destination.X, destination.Y + destination.Height / 2.0), new Vector2(destination.X + destination.Width, destination.Y + destination.Height / 2.0), 1.0);
		}
	}
}
