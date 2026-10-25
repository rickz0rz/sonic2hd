using System;

namespace SonicOrca.Geometry;

/// <summary>
/// A three-dimensional vector with double precision.
/// </summary>
public struct Vector3 : IEquatable<Vector3>
{
	public double X { get; set; }

	public double Y { get; set; }

	public double Z { get; set; }

	public Vector2 XY => new Vector2(X, Y);

	public Vector3 Normalised => this / Length;

	public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

	public Vector3(double xyz)
		: this(xyz, xyz, xyz)
	{
	}

	public Vector3(double x, double y, double z)
	{
		this = default;
		X = x;
		Y = y;
		Z = z;
	}

	public override bool Equals(object obj)
	{
		return Equals((Vector3)obj);
	}

	public bool Equals(Vector3 p)
	{
		if (p.X == X && p.Y == Y)
		{
			return p.Z == Z;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((13 * 7 + X.GetHashCode()) * 7 + Y.GetHashCode()) * 7 + Z.GetHashCode();
	}

	public override string ToString()
	{
		return $"X = {X} Y = {Y} Z = {Z}";
	}

	public Vector3 Cross(Vector3 other)
	{
		return new Vector3(Y * other.Z - Z * other.Y, Z * other.X - X * other.Z, X * other.Y - Y * other.X);
	}

	public static Vector3 operator +(Vector3 a, Vector3 b)
	{
		return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	public static Vector3 operator -(Vector3 a, Vector3 b)
	{
		return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	public static Vector3 operator -(Vector3 v)
	{
		return new Vector3(0.0 - v.X, 0.0 - v.Y, 0.0 - v.Z);
	}

	public static Vector3 operator *(Vector3 a, Vector3 b)
	{
		return new Vector3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
	}

	public static Vector3 operator *(Vector3 v, double s)
	{
		return new Vector3(v.X * s, v.Y * s, v.Z * s);
	}

	public static Vector3 operator *(double s, Vector3 v)
	{
		return new Vector3(v.X * s, v.Y * s, v.Z * s);
	}

	public static Vector3 operator /(Vector3 a, Vector3 b)
	{
		return new Vector3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
	}

	public static Vector3 operator /(Vector3 v, double s)
	{
		return new Vector3(v.X / s, v.Y / s, v.Z / s);
	}

	public static Vector3 operator /(double s, Vector3 v)
	{
		return new Vector3(s / v.X, s / v.Y, s / v.Z);
	}
}
