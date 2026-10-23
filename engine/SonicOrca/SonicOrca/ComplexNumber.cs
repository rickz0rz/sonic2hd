using System;

namespace SonicOrca;

/// <summary>
/// A complex number with double precision.
/// </summary>
public struct ComplexNumber : IEquatable<ComplexNumber>
{
	public double Real { get; set; }

	public double Imaginary { get; set; }

	public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

	public ComplexNumber(double real, double imaginary)
	{
		this = default;
		Real = real;
		Imaginary = imaginary;
	}

	public override bool Equals(object obj)
	{
		return Equals((ComplexNumber)obj);
	}

	public bool Equals(ComplexNumber other)
	{
		if (Real == other.Real)
		{
			return Imaginary == other.Imaginary;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (13 * 7 + Real.GetHashCode()) * 7 + Imaginary.GetHashCode();
	}

	public override string ToString()
	{
		return $"{Real} + {Imaginary}i";
	}
}
