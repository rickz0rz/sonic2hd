using System;
using System.Text.RegularExpressions;

namespace SonicOrca.Graphics;

/// <summary>
/// A 32-bit ARGB colour, with 8 bits for each channel. It can also get and set the colour as hue, saturation
/// and luminosity.
/// </summary>
public struct Colour : IEquatable<Colour>
{
	public uint Argb { get; set; }

	public byte Alpha
	{
		get
		{
			return (byte)(Argb >> 24);
		}
		set
		{
			Argb = (Argb & 0xFFFFFF) | (uint)(value << 24);
		}
	}

	public byte Red
	{
		get
		{
			return (byte)((Argb >> 16) & 0xFF);
		}
		set
		{
			Argb = (Argb & 0xFF00FFFFu) | (uint)(value << 16);
		}
	}

	public byte Green
	{
		get
		{
			return (byte)((Argb >> 8) & 0xFF);
		}
		set
		{
			Argb = (Argb & 0xFFFF00FFu) | (uint)(value << 8);
		}
	}

	public byte Blue
	{
		get
		{
			return (byte)(Argb & 0xFF);
		}
		set
		{
			Argb = (Argb & 0xFFFFFF00u) | value;
		}
	}

	public double Hue
	{
		get
		{
			double red = Red / 255.0;
			double green = Green / 255.0;
			double blue = Blue / 255.0;
			double max = Math.Max(Math.Max(red, green), blue);
			double min = Math.Min(Math.Min(red, green), blue);
			if ((min + max) / 2.0 <= 0.0)
			{
				return 0.0;
			}
			double delta = max - min;
			if (delta <= 0.0)
			{
				return 0.0;
			}
			double redDistance = (max - red) / delta;
			double greenDistance = (max - green) / delta;
			double blueDistance = (max - blue) / delta;
			// The hue sector, from 0 to 6, before it is scaled to the range 0 to 1.
			double sector;
			if (red == max)
			{
				sector = (green == min) ? (5.0 + blueDistance) : (1.0 - greenDistance);
			}
			else if (green != max)
			{
				sector = (red == min) ? (3.0 + greenDistance) : (5.0 - redDistance);
			}
			else
			{
				sector = (blue == min) ? (1.0 + redDistance) : (3.0 - blueDistance);
			}
			return sector / 6.0;
		}
		set
		{
			SetFromHSL(value, Saturation, Luminosity);
		}
	}

	public double Saturation
	{
		get
		{
			double red = Red / 255.0;
			double green = Green / 255.0;
			double blue = Blue / 255.0;
			double max = Math.Max(Math.Max(red, green), blue);
			double min = Math.Min(Math.Min(red, green), blue);
			double luminosity = (min + max) / 2.0;
			if (luminosity <= 0.0)
			{
				return 0.0;
			}
			double delta = max - min;
			if (!(delta > 0.0))
			{
				return delta;
			}
			return delta / ((luminosity <= 0.5) ? (max + min) : (2.0 - max - min));
		}
		set
		{
			SetFromHSL(Hue, value, Luminosity);
		}
	}

	public double Luminosity
	{
		get
		{
			double red = Red / 255.0;
			double green = Green / 255.0;
			double blue = Blue / 255.0;
			double max = Math.Max(Math.Max(red, green), blue);
			return (Math.Min(Math.Min(red, green), blue) + max) / 2.0;
		}
		set
		{
			SetFromHSL(Hue, Saturation, value);
		}
	}

	public Colour(double rgb)
		: this(rgb, rgb, rgb)
	{
	}

	public Colour(double r, double g, double b)
		: this(1.0, r, g, b)
	{
	}

	public Colour(double a, double r, double g, double b)
		: this((byte)(a * 255.0), (byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0))
	{
	}

	public Colour(byte r, byte g, byte b)
		: this(byte.MaxValue, r, g, b)
	{
	}

	public Colour(byte a, byte r, byte g, byte b)
	{
		this = default;
		Alpha = a;
		Red = r;
		Green = g;
		Blue = b;
	}

	public Colour(uint argb)
	{
		this = default;
		Argb = argb;
	}

	public override int GetHashCode()
	{
		return Argb.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return Equals((Colour)obj);
	}

	public bool Equals(Colour other)
	{
		return Argb == other.Argb;
	}

	public override string ToString()
	{
		return $"({Red}, {Green}, {Blue}, {Alpha})";
	}

	public static bool operator ==(Colour lhs, Colour rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(Colour lhs, Colour rhs)
	{
		return !lhs.Equals(rhs);
	}

	public Colour GetDarker(double amount)
	{
		Colour result = this;
		result.Luminosity -= amount;
		return result;
	}

	public Colour GetLighter(double amount)
	{
		Colour result = this;
		result.Luminosity += amount;
		return result;
	}

	public static Colour FromHSL(double hue, double sat, double lum)
	{
		Colour result = default;
		result.SetFromHSL(hue, sat, lum);
		return result;
	}

	private void SetFromHSL(double hue, double sat, double lum)
	{
		hue = MathX.Clamp(0.0, hue, 1.0);
		sat = MathX.Clamp(0.0, sat, 1.0);
		lum = MathX.Clamp(0.0, lum, 1.0);
		double red = lum;
		double green = lum;
		double blue = lum;
		double max = (lum <= 0.5) ? (lum * (1.0 + sat)) : (lum + sat - lum * sat);
		if (max > 0.0)
		{
			double min = lum + lum - max;
			double saturation = (max - min) / max;
			hue *= 6.0;
			int sector = (int)hue;
			double fraction = hue - sector;
			double step = max * saturation * fraction;
			double rising = min + step;
			double falling = max - step;
			// A hue of exactly 1 gives sector 6. No case matches it, so the colour stays grey.
			switch (sector)
			{
				case 0:
					red = max;
					green = rising;
					blue = min;
					break;
				case 1:
					red = falling;
					green = max;
					blue = min;
					break;
				case 2:
					red = min;
					green = max;
					blue = rising;
					break;
				case 3:
					red = min;
					green = falling;
					blue = max;
					break;
				case 4:
					red = rising;
					green = min;
					blue = max;
					break;
				case 5:
					red = max;
					green = min;
					blue = falling;
					break;
			}
		}
		Red = (byte)MathX.Clamp(0.0, red * 255.0, 255.0);
		Green = (byte)MathX.Clamp(0.0, green * 255.0, 255.0);
		Blue = (byte)MathX.Clamp(0.0, blue * 255.0, 255.0);
	}

	public string ToHexString()
	{
		if (IsFourBitColour(Alpha) && IsFourBitColour(Red) && IsFourBitColour(Green) && IsFourBitColour(Blue))
		{
			if (Alpha == byte.MaxValue)
			{
				return $"#{Red & 0xF:X}{Green & 0xF:X}{Blue & 0xF:X}";
			}
			return $"#{Alpha & 0xF:X}{Red & 0xF:X}{Green & 0xF:X}{Blue & 0xF:X}";
		}
		if (Alpha == byte.MaxValue)
		{
			return $"#{Red:X2}{Green:X2}{Blue:X2}";
		}
		return $"#{Alpha:X2}{Red:X2}{Green:X2}{Blue:X2}";
	}

	public static Colour ParseHex(string s)
	{
		if (!TryParseHex(s, out Colour result))
		{
			throw new FormatException("Invalid colour hex string.");
		}
		return result;
	}

	public static bool TryParseHex(string s, out Colour result)
	{
		s = s.Trim();
		if (s.Length == 0)
		{
			result = Colours.Black;
			return false;
		}
		if (s[0] == '#')
		{
			s = s.Substring(1);
		}
		s = s.ToUpper();
		if (!Regex.IsMatch(s, "^([0-9A-F]{3}|[0-9A-F]{4}|[0-9A-F]{6}|[0-9A-F]{8})$"))
		{
			result = Colours.Black;
			return false;
		}
		switch (s.Length)
		{
			case 3:
				result = new Colour(Convert4to8BitColour(Convert.ToByte(s.Substring(0, 1), 16)), Convert4to8BitColour(Convert.ToByte(s.Substring(1, 1), 16)), Convert4to8BitColour(Convert.ToByte(s.Substring(2, 1), 16)));
				return true;
			case 4:
				result = new Colour(Convert4to8BitColour(Convert.ToByte(s.Substring(0, 1), 16)), Convert4to8BitColour(Convert.ToByte(s.Substring(1, 1), 16)), Convert4to8BitColour(Convert.ToByte(s.Substring(2, 1), 16)), Convert4to8BitColour(Convert.ToByte(s.Substring(3, 1), 16)));
				return true;
			case 6:
				result = new Colour(Convert.ToByte(s.Substring(0, 2), 16), Convert.ToByte(s.Substring(2, 2), 16), Convert.ToByte(s.Substring(4, 2), 16));
				return true;
			case 8:
				result = new Colour(Convert.ToByte(s.Substring(0, 2), 16), Convert.ToByte(s.Substring(2, 2), 16), Convert.ToByte(s.Substring(4, 2), 16), Convert.ToByte(s.Substring(6, 2), 16));
				return true;
			default:
				result = Colours.Black;
				return false;
		}
	}

	private static byte Convert4to8BitColour(byte colour)
	{
		return (byte)((colour << 4) | colour);
	}

	private static bool IsFourBitColour(byte colour)
	{
		return colour >> 4 == (colour & 0xF);
	}

	public static Colour FromOpacity(double opacity)
	{
		return new Colour(opacity, 1.0, 1.0, 1.0);
	}
}
