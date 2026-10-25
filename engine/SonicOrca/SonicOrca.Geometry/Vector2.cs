using System;
using System.Globalization;

namespace SonicOrca.Geometry;

/// <summary>
/// A two-dimensional vector or point with double precision.
/// </summary>
public struct Vector2 : IEquatable<Vector2>
{
	public double X { get; set; }

	public double Y { get; set; }

	public Vector2 Normalised => this / Length;

	public double Length => Math.Sqrt(X * X + Y * Y);

	public double LengthSquared => X * X + Y * Y;

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
		return string.Format(CultureInfo.InvariantCulture, "X = {0}, Y = {1}", X, Y);
	}

	public Vector2 Rotate(double radians)
	{
		double cos = Math.Cos(radians);
		double sin = Math.Sin(radians);
		return new Vector2(X * cos - Y * sin, X * sin + Y * cos);
	}

	/// <summary>Rotates the vector about a point.</summary>
	public Vector2 Rotate(double radians, Vector2 origin)
	{
		return (this - origin).Rotate(radians) + origin;
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
		double originX = relative.X;
		double originY = relative.Y;
		double rotatedX = Math.Cos(theta) * (x - originX) - Math.Sin(theta) * (y - originY) + originX;
		double rotatedY = Math.Sin(theta) * (x - originX) + Math.Cos(theta) * (y - originY) + originY;
		return new Vector2(rotatedX, rotatedY);
	}

	public static Vector2 FromAngle(double radians)
	{
		return new Vector2(Math.Cos(radians), Math.Sin(radians));
	}

	public static double GetDistance(Vector2 pointA, Vector2 pointB)
	{
		return Math.Sqrt(Math.Pow(pointB.X - pointA.X, 2.0) + Math.Pow(pointB.Y - pointA.Y, 2.0));
	}

	/// <summary>
	/// Tests if the line segment from <paramref name="line0point0"/> to <paramref name="line0point1"/> crosses
	/// the line segment from <paramref name="line1point0"/> to <paramref name="line1point1"/>.
	/// </summary>
	public static bool Intersects(Vector2 line0point0, Vector2 line0point1, Vector2 line1point0, Vector2 line1point1)
	{
		Vector2 direction0 = new Vector2(line0point1.X - line0point0.X, line0point1.Y - line0point0.Y);
		Vector2 direction1 = new Vector2(line1point1.X - line1point0.X, line1point1.Y - line1point0.Y);
		// s and t are the positions of the crossing point along each segment, from 0 to 1.
		double s = ((0.0 - direction0.Y) * (line0point0.X - line1point0.X) + direction0.X * (line0point0.Y - line1point0.Y)) / ((0.0 - direction1.X) * direction0.Y + direction0.X * direction1.Y);
		double t = (direction1.X * (line0point0.Y - line1point0.Y) - direction1.Y * (line0point0.X - line1point0.X)) / ((0.0 - direction1.X) * direction0.Y + direction0.X * direction1.Y);
		return s >= 0.0 && s <= 1.0 && t >= 0.0 && t <= 1.0;
	}

	/// <summary>
	/// Gets the point where the infinite line through A and B crosses the infinite line through C and D.
	/// </summary>
	/// <returns><c>false</c> when the lines are parallel.</returns>
	public static bool GetLineIntersection(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out Vector2 intersection)
	{
		// Each line in the form a * x + b * y = c.
		double a1 = B.Y - A.Y;
		double b1 = A.X - B.X;
		double c1 = a1 * A.X + b1 * A.Y;
		double a2 = D.Y - C.Y;
		double b2 = C.X - D.X;
		double c2 = a2 * C.X + b2 * C.Y;
		double determinant = a1 * b2 - a2 * b1;
		if (determinant == 0.0)
		{
			intersection = new Vector2(double.MaxValue, double.MaxValue);
			return false;
		}
		double x = (b2 * c1 - b1 * c2) / determinant;
		double y = (a1 * c2 - a2 * c1) / determinant;
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
		return new Vector2(x / v.X, x / v.Y);
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
