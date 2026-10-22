using System;

namespace SonicOrca.Geometry;

public struct Ellipse : IEquatable<Ellipse>
{
	public static Ellipse Empty => default;

	public Vector2 Position { get; set; }

	public Vector2 Radius { get; set; }

	public double Area => Math.PI * Radius.X * Radius.Y;

	public Ellipse(double x, double y, double radiusX, double radiusY)
	{
		this = default;
		Position = new Vector2(x, y);
		Radius = new Vector2(radiusX, radiusY);
	}

	public bool Contains(Vector2 p)
	{
		if (Radius.X <= 0.0 || Radius.Y <= 0.0)
		{
			return false;
		}
		Vector2 vector = new Vector2(p.X - Position.X, p.Y - Position.Y);
		return vector.X * vector.X / (Radius.X * Radius.X) + vector.Y * vector.Y / (Radius.Y * Radius.Y) <= 1.0;
	}

	public override bool Equals(object obj)
	{
		return Equals((Ellipse)obj);
	}

	public bool Equals(Ellipse other)
	{
		if (other.Position.X == Position.X && other.Position.Y == Position.Y && other.Radius.X == Radius.X)
		{
			return other.Radius.Y == Radius.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((13 * 7 + Position.X.GetHashCode()) * 7 + Position.Y.GetHashCode()) * 7 + Radius.X.GetHashCode()) * 7 + Radius.Y.GetHashCode();
	}

	public override string ToString()
	{
		return $"X = {Position.X} Y = {Position.Y} RadiusX = {Radius.X} RadiusY = {Radius.Y}";
	}
}
