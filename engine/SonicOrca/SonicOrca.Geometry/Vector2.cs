using System;
using System.Globalization;

namespace SonicOrca.Geometry;

public struct Vector2 : IEquatable<Vector2>
{
	public double X { get; set; }

	public double Y { get; set; }

	public Vector2 Normalised => this / Length;

	public double Length => Math.Sqrt(X * X + Y * Y);

	public double LengthSquared => Math.Pow(X * X + Y * Y, 2.0);

	public double Angle => Math.Atan2(Y, X);

	public Vector2(double xy)
		: this(xy, xy)
	{
	}

	public Vector2(double x, double y)
	{
		this = default;
		X = x;
		Y = y;
	}

	public override bool Equals(object obj)
	{
		return Equals((Vector2)obj);
	}

	public bool Equals(Vector2 p)
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
		return string.Format(CultureInfo.InvariantCulture, "X = {0}, Y = {1}", new object[2] { X, Y });
	}

	public Vector2 Rotate(double radians)
	{
		double num = Math.Cos(radians);
		double num2 = Math.Sin(radians);
		return new Vector2(X * num - Y * num2, X * num2 + Y * num);
	}

	public Vector2 Rotate(double radians, Vector2 origin)
	{
		return (this + origin).Rotate(radians) - origin;
	}

	public Vector2 Reflect(Vector2 normal)
	{
		return this - 2.0 * Dot(normal) * normal;
	}

	public double Dot(Vector2 v)
	{
		return X * v.X + Y * v.Y;
	}

	public double Cross(Vector2 v)
	{
		return X * v.Y - Y * v.X;
	}

	public static Vector2 GetPointRotatedFromRelative(Vector2 relative, Vector2 point, double theta)
	{
		double x = point.X;
		double y = point.Y;
		double x2 = relative.X;
		double y2 = relative.Y;
		double x3 = Math.Cos(theta) * (x - x2) - Math.Sin(theta) * (y - y2) + x2;
		double y3 = Math.Sin(theta) * (x - x2) + Math.Cos(theta) * (y - y2) + y2;
		return new Vector2(x3, y3);
	}

	public static Vector2 FromAngle(double radians)
	{
		return new Vector2(Math.Cos(radians), Math.Sin(radians));
	}

	public static double GetDistance(Vector2 pointA, Vector2 pointB)
	{
		return Math.Sqrt(Math.Pow(pointB.X - pointA.X, 2.0) + Math.Pow(pointB.Y - pointA.Y, 2.0));
	}

	public static bool Intersects(Vector2 line0point0, Vector2 line0point1, Vector2 line1point0, Vector2 line1point1)
	{
		Vector2 vector = new Vector2(line0point1.X - line0point0.X, line0point1.Y - line0point0.Y);
		Vector2 vector2 = new Vector2(line1point1.X - line1point0.X, line1point1.Y - line1point0.Y);
		double num = ((0.0 - vector.Y) * (line0point0.X - line1point0.X) + vector.X * (line0point0.Y - line1point0.Y)) / ((0.0 - vector2.X) * vector.Y + vector.X * vector2.Y);
		double num2 = (vector2.X * (line0point0.Y - line1point0.Y) - vector2.Y * (line0point0.X - line1point0.X)) / ((0.0 - vector2.X) * vector.Y + vector.X * vector2.Y);
		if (num >= 0.0 && num <= 1.0 && num2 >= 0.0 && num2 <= 1.0)
		{
			return true;
		}
		return false;
	}

	public static bool GetLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 intersection)
	{
		double num = B.Y - A.Y;
		double num2 = A.X - B.X;
		double num3 = num * A.X + num2 * A.Y;
		double num4 = D.Y - C.Y;
		double num5 = C.X - D.X;
		double num6 = num4 * C.X + num5 * C.Y;
		double num7 = num * num5 - num4 * num2;
		if (num7 == 0.0)
		{
			intersection = new Vector2(double.MaxValue, double.MaxValue);
			return false;
		}
		double x = (num5 * num3 - num2 * num6) / num7;
		double y = (num * num6 - num4 * num3) / num7;
		intersection = new Vector2(x, y);
		return true;
	}

	public static Vector2 operator +(Vector2 a, Vector2 b)
	{
		return new Vector2(a.X + b.X, a.Y + b.Y);
	}

	public static Vector2 operator -(Vector2 a, Vector2 b)
	{
		return new Vector2(a.X - b.X, a.Y - b.Y);
	}

	public static Vector2 operator -(Vector2 a)
	{
		return new Vector2(0.0 - a.X, 0.0 - a.Y);
	}

	public static Vector2 operator *(Vector2 a, Vector2 b)
	{
		return new Vector2(a.X * b.X, a.Y * b.Y);
	}

	public static double Multiply(Vector2 v, Vector2 w)
	{
		return v.X * w.X + v.Y * w.Y;
	}

	public static Vector2 operator /(Vector2 a, Vector2 b)
	{
		return new Vector2(a.X / b.X, a.Y / b.Y);
	}

	public static Vector2 operator *(Vector2 v, double x)
	{
		return new Vector2(v.X * x, v.Y * x);
	}

	public static Vector2 operator *(double x, Vector2 v)
	{
		return new Vector2(v.X * x, v.Y * x);
	}

	public static Vector2 operator /(Vector2 v, double x)
	{
		return new Vector2(v.X / x, v.Y / x);
	}

	public static Vector2 operator /(double x, Vector2 v)
	{
		return new Vector2(v.X / x, v.Y / x);
	}

	public static bool operator ==(Vector2 a, Vector2 b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Vector2 a, Vector2 b)
	{
		return !a.Equals(b);
	}
}
