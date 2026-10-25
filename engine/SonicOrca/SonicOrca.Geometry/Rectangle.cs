using System;

namespace SonicOrca.Geometry;

/// <summary>
/// An axis-aligned rectangle with double precision, given by its top-left corner and its size.
/// </summary>
public struct Rectangle : IEquatable<Rectangle>
{
	public static Rectangle Empty => default;

	public double X { get; set; }

	public double Y { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public double Area => Width * Height;

	public Vector2 TopLeft => new Vector2(Left, Top);

	public Vector2 TopRight => new Vector2(Right, Top);

	public Vector2 BottomLeft => new Vector2(Left, Bottom);

	public Vector2 BottomRight => new Vector2(Right, Bottom);

	public Vector2 Centre => new Vector2(Left + Width / 2.0, Top + Height / 2.0);

	public Size Size => new Size(Width, Height);

	public Vector2 Location
	{
		get
		{
			return new Vector2(X, Y);
		}
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public double Left
	{
		get
		{
			return X;
		}
		set
		{
			double right = Right;
			X = value;
			Right = right;
		}
	}

	public double Top
	{
		get
		{
			return Y;
		}
		set
		{
			double bottom = Bottom;
			Y = value;
			Bottom = bottom;
		}
	}

	public double Right
	{
		get
		{
			return X + Width;
		}
		set
		{
			Width = value - X;
		}
	}

	public double Bottom
	{
		get
		{
			return Y + Height;
		}
		set
		{
			Height = value - Y;
		}
	}

	public double CentreX => X + Width / 2.0;

	public double CentreY => Y + Height / 2.0;

	public static Rectangle FromLTRB(double left, double top, double right, double bottom)
	{
		return new Rectangle(left, top, right - left, bottom - top);
	}

	public Rectangle(double x, double y, double width, double height)
	{
		this = default;
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public bool Contains(Vector2 p)
	{
		if (p.X > X && p.Y > Y && p.X < Right)
		{
			return p.Y < Bottom;
		}
		return false;
	}

	/// <summary>True when the point is in the rectangle or on its edge.</summary>
	public bool ContainsOrOverlaps(Vector2 p)
	{
		if (p.X >= Left && p.Y >= Top && p.X <= Right)
		{
			return p.Y <= Bottom;
		}
		return false;
	}

	public bool Contains(Rectangle rect)
	{
		if (rect.X >= X && rect.Y >= Y && rect.Right <= Right)
		{
			return rect.Bottom <= Bottom;
		}
		return false;
	}

	public bool IntersectsWith(Rectangle rect)
	{
		if (!(rect.X > Right) && !(rect.Right < X) && !(rect.Y > Bottom))
		{
			return !(rect.Bottom < Y);
		}
		return false;
	}

	public Rectangle Inflate(Vector2 amount)
	{
		return new Rectangle(X - amount.X, Y - amount.Y, Width + amount.X * 2.0, Height + amount.Y * 2.0);
	}

	public Rectangle OffsetBy(Vector2 amount)
	{
		return new Rectangle(X + amount.X, Y + amount.Y, Width, Height);
	}

	public override bool Equals(object obj)
	{
		return Equals((Rectangle)obj);
	}

	public bool Equals(Rectangle other)
	{
		if (other.X == X && other.Y == Y && other.Width == Width)
		{
			return other.Height == Height;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((13 * 7 + X.GetHashCode()) * 7 + Y.GetHashCode()) * 7 + Width.GetHashCode()) * 7 + Height.GetHashCode();
	}

	public override string ToString()
	{
		return $"X = {X} Y = {Y} Width = {Width} Height = {Height}";
	}

	public static Rectangle operator +(Rectangle rectangle, Vector2 offset)
	{
		rectangle.X += offset.X;
		rectangle.Y += offset.Y;
		return rectangle;
	}

	public static Rectangle operator -(Rectangle rectangle, Vector2 offset)
	{
		rectangle.X -= offset.X;
		rectangle.Y -= offset.Y;
		return rectangle;
	}

	public static bool operator ==(Rectangle a, Rectangle b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Rectangle a, Rectangle b)
	{
		return !a.Equals(b);
	}

	public static Rectangle Intersect(Rectangle a, Rectangle b)
	{
		double left = Math.Max(a.X, b.X);
		double right = Math.Min(a.Right, b.Right);
		double top = Math.Max(a.Y, b.Y);
		double bottom = Math.Min(a.Bottom, b.Bottom);
		if (right >= left && bottom >= top)
		{
			return new Rectangle(left, top, right - left, bottom - top);
		}
		return Empty;
	}
}
