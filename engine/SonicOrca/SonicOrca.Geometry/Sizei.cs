using System;

namespace SonicOrca.Geometry;

/// <summary>
/// A width and a height in integers. Sizes compare by area.
/// </summary>
public struct Sizei : IComparable<Sizei>, IEquatable<Sizei>
{
	public int Width { get; set; }

	public int Height { get; set; }

	public long Area => Width * (long)Height;

	public Sizei(int width, int height)
	{
		this = default;
		Width = width;
		Height = height;
	}

	public override bool Equals(object obj)
	{
		return Equals((Sizei)obj);
	}

	public bool Equals(Sizei other)
	{
		if (Width == other.Width)
		{
			return Height == other.Height;
		}
		return false;
	}

	public int CompareTo(Sizei other)
	{
		return Area.CompareTo(other.Area);
	}

	public override int GetHashCode()
	{
		return (13 * 7 + Width.GetHashCode()) * 7 + Height.GetHashCode();
	}

	public override string ToString()
	{
		return $"Width = {Width} Height = {Height}";
	}

	public static Sizei operator +(Sizei a, Sizei b)
	{
		return new Sizei(a.Width + b.Width, a.Height + b.Height);
	}

	public static Sizei operator -(Sizei a, Sizei b)
	{
		return new Sizei(a.Width - b.Width, a.Height - b.Height);
	}

	public static Sizei operator *(Sizei a, Sizei b)
	{
		return new Sizei(a.Width * b.Width, a.Height * b.Height);
	}

	public static Sizei operator /(Sizei a, Sizei b)
	{
		return new Sizei(a.Width / b.Width, a.Height / b.Height);
	}

	public static Sizei operator *(Sizei v, int x)
	{
		return new Sizei(v.Width * x, v.Height * x);
	}

	public static Sizei operator /(Sizei v, int x)
	{
		return new Sizei(v.Width / x, v.Height / x);
	}

	public static bool operator ==(Sizei a, Sizei b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Sizei a, Sizei b)
	{
		return !a.Equals(b);
	}

	public static implicit operator Size(Sizei size)
	{
		return new Size(size.Width, size.Height);
	}

	public static explicit operator Sizei(Size size)
	{
		return new Sizei((int)size.Width, (int)size.Height);
	}
}
