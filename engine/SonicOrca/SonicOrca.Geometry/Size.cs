using System;

namespace SonicOrca.Geometry;

/// <summary>
/// A width and a height with double precision. Sizes compare by area.
/// </summary>
public struct Size : IComparable<Size>, IEquatable<Size>
{
	public double Width { get; set; }

	public double Height { get; set; }

	public double Area => Width * Height;

	public Size(double width, double height)
	{
		this = default;
		Width = width;
		Height = height;
	}

	public override bool Equals(object obj)
	{
		return Equals((Size)obj);
	}

	public bool Equals(Size other)
	{
		if (Width == other.Width)
		{
			return Height == other.Height;
		}
		return false;
	}

	public int CompareTo(Size other)
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

	public static Size operator +(Size a, Size b)
	{
		return new Size(a.Width + b.Width, a.Height + b.Height);
	}

	public static Size operator -(Size a, Size b)
	{
		return new Size(a.Width - b.Width, a.Height - b.Height);
	}

	public static Size operator *(Size a, Size b)
	{
		return new Size(a.Width * b.Width, a.Height * b.Height);
	}

	public static Size operator /(Size a, Size b)
	{
		return new Size(a.Width / b.Width, a.Height / b.Height);
	}

	public static Size operator *(Size v, int x)
	{
		return new Size(v.Width * x, v.Height * x);
	}

	public static Size operator /(Size v, int x)
	{
		return new Size(v.Width / x, v.Height / x);
	}

	public static bool operator ==(Size a, Size b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Size a, Size b)
	{
		return !a.Equals(b);
	}
}
