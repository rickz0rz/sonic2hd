using System;

namespace SonicOrca.Geometry;

public struct Rectanglei : IEquatable<Rectanglei>
{
	public static Rectanglei Empty => default;

	public int X { get; set; }

	public int Y { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	public long Area => Width * (long)Height;

	public Vector2i TopLeft => new Vector2i(Left, Top);

	public Vector2i TopRight => new Vector2i(Right, Top);

	public Vector2i BottomLeft => new Vector2i(Left, Bottom);

	public Vector2i BottomRight => new Vector2i(Right, Bottom);

	public Vector2i Centre => new Vector2i(Left + Width / 2, Top + Height / 2);

	public Sizei Size => new Sizei(Width, Height);

	public Vector2i Location
	{
		get
		{
			return new Vector2i(X, Y);
		}
		set
		{
			X = value.X;
			Y = value.Y;
		}
	}

	public int Left
	{
		get
		{
			return X;
		}
		set
		{
			int right = Right;
			X = value;
			Right = right;
		}
	}

	public int Top
	{
		get
		{
			return Y;
		}
		set
		{
			int bottom = Bottom;
			Y = value;
			Bottom = bottom;
		}
	}

	public int Right
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

	public int Bottom
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

	public static Rectangle FromLTRB(float left, float top, float right, float bottom)
	{
		return new Rectangle(left, top, right - left, bottom - top);
	}

	public static Rectanglei FromLTRB(int left, int top, int right, int bottom)
	{
		return new Rectanglei(left, top, right - left, bottom - top);
	}

	public Rectanglei(int x, int y, int width, int height)
	{
		this = default;
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public bool Contains(Vector2i p)
	{
		if (p.X >= X && p.Y >= Y && p.X < Right)
		{
			return p.Y < Bottom;
		}
		return false;
	}

	public bool Contains(Rectanglei rect)
	{
		if (rect.X >= X && rect.Y >= Y && rect.Right <= Right)
		{
			return rect.Bottom <= Bottom;
		}
		return false;
	}

	public bool IntersectsWith(Rectanglei rect)
	{
		if (rect.X < Right && rect.Right > X && rect.Y < Bottom)
		{
			return rect.Bottom > Y;
		}
		return false;
	}

	public Rectanglei Inflate(Vector2i amount)
	{
		return new Rectanglei(X - amount.X, Y - amount.Y, Width + amount.X * 2, Height + amount.Y * 2);
	}

	public Rectanglei OffsetBy(Vector2i amount)
	{
		return new Rectanglei(X + amount.X, Y + amount.Y, Width, Height);
	}

	public Rectanglei Union(Rectanglei rect)
	{
		return new Rectanglei
		{
			Left = Math.Min(Left, rect.Left),
			Top = Math.Min(Top, rect.Top),
			Right = Math.Max(Right, rect.Right),
			Bottom = Math.Max(Bottom, rect.Bottom)
		};
	}

	public override bool Equals(object obj)
	{
		Rectanglei rectanglei = (Rectanglei)obj;
		if (rectanglei.X == X && rectanglei.Y == Y && rectanglei.Width == Width)
		{
			return rectanglei.Height == Height;
		}
		return false;
	}

	public bool Equals(Rectanglei other)
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

	public static Rectanglei Union(Rectanglei a, Rectanglei b)
	{
		return FromLTRB(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
	}

	public static Rectanglei Intersect(Rectanglei a, Rectanglei b)
	{
		int num = Math.Max(a.X, b.X);
		int num2 = Math.Min(a.Right, b.Right);
		int num3 = Math.Max(a.Y, b.Y);
		int num4 = Math.Min(a.Bottom, b.Bottom);
		if (num2 >= num && num4 >= num3)
		{
			return new Rectanglei(num, num3, num2 - num, num4 - num3);
		}
		return Empty;
	}

	public static implicit operator Rectangle(Rectanglei r)
	{
		return new Rectangle(r.X, r.Y, r.Width, r.Height);
	}

	public static implicit operator Rectanglei(Rectangle r)
	{
		return new Rectanglei((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
	}

	public static bool operator ==(Rectanglei a, Rectanglei b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Rectanglei a, Rectanglei b)
	{
		return !a.Equals(b);
	}
}
