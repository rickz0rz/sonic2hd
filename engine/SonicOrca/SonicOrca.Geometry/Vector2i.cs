using System;

namespace SonicOrca.Geometry;

public struct Vector2i : IEquatable<Vector2i>
{
	public int X { get; set; }

	public int Y { get; set; }

	public int Length => (int)Math.Sqrt((long)X * (long)X + (long)Y * (long)Y);

	public double Angle => Math.Atan2(Y, X);

	public Vector2i(int xy)
		: this(xy, xy)
	{
	}

	public Vector2i(int x, int y)
	{
		this = default;
		X = x;
		Y = y;
	}

	public override bool Equals(object obj)
	{
		return Equals((Vector2i)obj);
	}

	public bool Equals(Vector2i p)
	{
		if (p.X == X)
		{
			return p.Y == Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (13 * 7 + X.GetHashCode()) * 7 + Y.GetHashCode();
	}

	public override string ToString()
	{
		return $"X = {X}, Y = {Y}";
	}

	public static Vector2i operator +(Vector2i a, Vector2i b)
	{
		return new Vector2i(a.X + b.X, a.Y + b.Y);
	}

	public static Vector2i operator -(Vector2i a, Vector2i b)
	{
		return new Vector2i(a.X - b.X, a.Y - b.Y);
	}

	public static Vector2i operator *(Vector2i a, Vector2i b)
	{
		return new Vector2i(a.X * b.X, a.Y * b.Y);
	}

	public static Vector2i operator /(Vector2i a, Vector2i b)
	{
		return new Vector2i(a.X / b.X, a.Y / b.Y);
	}

	public static Vector2i operator *(Vector2i v, int x)
	{
		return new Vector2i(v.X * x, v.Y * x);
	}

	public static Vector2i operator /(Vector2i v, int x)
	{
		return new Vector2i(v.X / x, v.Y / x);
	}

	public static bool operator ==(Vector2i a, Vector2i b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Vector2i a, Vector2i b)
	{
		return !a.Equals(b);
	}

	public static implicit operator Vector2(Vector2i v)
	{
		return new Vector2(v.X, v.Y);
	}

	public static explicit operator Vector2i(Vector2 v)
	{
		return new Vector2i((int)v.X, (int)v.Y);
	}
}
