using System;

namespace SonicOrca.Geometry;

/// <summary>
/// A four-dimensional vector with double precision.
/// </summary>
public struct Vector4 : IEquatable<Vector4>
{
	public double X { get; set; }

	public double Y { get; set; }

	public double Z { get; set; }

	public double W { get; set; }

	public Vector2 XY => new Vector2(X, Y);

	public Vector3 XYZ => new Vector3(X, Y, Z);

	public Vector4 Normalised => this / Length;

	public double Length => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

	public static Vector4 UnitX => new Vector4(1.0, 0.0, 0.0, 0.0);

	public static Vector4 UnitY => new Vector4(0.0, 1.0, 0.0, 0.0);

	public static Vector4 UnitZ => new Vector4(0.0, 0.0, 1.0, 0.0);

	public static Vector4 UnitW => new Vector4(0.0, 0.0, 0.0, 1.0);

	public Vector4(double xyzw)
		: this(xyzw, xyzw, xyzw, xyzw)
	{
	}

	public Vector4(double x, double y, double z, double w)
	{
		this = default;
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public override bool Equals(object obj)
	{
		return Equals((Vector4)obj);
	}

	public bool Equals(Vector4 p)
	{
		if (p.X == X && p.Y == Y && p.Z == Z)
		{
			return p.W == W;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((13 * 7 + X.GetHashCode()) * 7 + Y.GetHashCode()) * 7 + Z.GetHashCode()) * 7 + W.GetHashCode();
	}

	public override string ToString()
	{
		return $"X = {X} Y = {Y} Z = {Z} W = {W}";
	}

	public static Vector4 operator +(Vector4 a, Vector4 b)
	{
		return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
	}

	public static Vector4 operator -(Vector4 a, Vector4 b)
	{
		return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
	}

	public static Vector4 operator -(Vector4 v)
	{
		return new Vector4(0.0 - v.X, 0.0 - v.Y, 0.0 - v.Z, 0.0 - v.W);
	}

	public static Vector4 operator *(Vector4 a, Vector4 b)
	{
		return new Vector4(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
	}

	public static Vector4 operator *(Vector4 v, double s)
	{
		return new Vector4(v.X * s, v.Y * s, v.Z * s, v.W * s);
	}

	public static Vector4 operator *(double s, Vector4 v)
	{
		return new Vector4(v.X * s, v.Y * s, v.Z * s, v.W * s);
	}

	public static Vector4 operator /(Vector4 a, Vector4 b)
	{
		return new Vector4(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
	}

	public static Vector4 operator /(Vector4 v, double s)
	{
		return new Vector4(v.X / s, v.Y / s, v.Z / s, v.W / s);
	}

	public static Vector4 operator /(double s, Vector4 v)
	{
		return new Vector4(s / v.X, s / v.Y, s / v.Z, s / v.W);
	}

	public static bool operator ==(Vector4 a, Vector4 b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Vector4 a, Vector4 b)
	{
		return !a.Equals(b);
	}
}
