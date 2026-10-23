using System;
using System.Text.RegularExpressions;

namespace SonicOrca.Graphics;

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
			double num = Red / 255.0;
			double num2 = Green / 255.0;
			double num3 = Blue / 255.0;
			double num4 = Math.Max(Math.Max(num, num2), num3);
			double num5 = Math.Min(Math.Min(num, num2), num3);
			if ((num5 + num4) / 2.0 <= 0.0)
			{
				return 0.0;
			}
			double num6 = num4 - num5;
			if (num6 <= 0.0)
			{
				return 0.0;
			}
			double num7 = (num4 - num) / num6;
			double num8 = (num4 - num2) / num6;
			double num9 = (num4 - num3) / num6;
			double num10;
			if (num == num4)
			{
				num10 = ((num2 == num5) ? (5.0 + num9) : (1.0 - num8));
			}
			else if (num2 != num4)
			{
				num10 = ((num == num5) ? (3.0 + num8) : (5.0 - num7));
			}
			else
			{
				num10 = ((num3 == num5) ? (1.0 + num7) : (3.0 - num9));
			}
			return num10 / 6.0;
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
			double val = Red / 255.0;
			double val2 = Green / 255.0;
			double val3 = Blue / 255.0;
			double num = Math.Max(Math.Max(val, val2), val3);
			double num2 = Math.Min(Math.Min(val, val2), val3);
			double num3 = (num2 + num) / 2.0;
			if (num3 <= 0.0)
			{
				return 0.0;
			}
			double num4 = num - num2;
			if (!(num4 > 0.0))
			{
				return num4;
			}
			return num4 / ((num3 <= 0.5) ? (num + num2) : (2.0 - num - num2));
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
			double val = Red / 255.0;
			double val2 = Green / 255.0;
			double val3 = Blue / 255.0;
			double num = Math.Max(Math.Max(val, val2), val3);
			return (Math.Min(Math.Min(val, val2), val3) + num) / 2.0;
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
		double num = lum;
		double num2 = lum;
		double num3 = lum;
		double num4 = ((lum <= 0.5) ? (lum * (1.0 + sat)) : (lum + sat - lum * sat));
		if (num4 > 0.0)
		{
			double num5 = lum + lum - num4;
			double num6 = (num4 - num5) / num4;
			hue *= 6.0;
			int num7 = (int)hue;
			double num8 = hue - num7;
			double num9 = num4 * num6 * num8;
			double num10 = num5 + num9;
			double num11 = num4 - num9;
			switch (num7)
			{
				case 0:
					num = num4;
					num2 = num10;
					num3 = num5;
					break;
				case 1:
					num = num11;
					num2 = num4;
					num3 = num5;
					break;
				case 2:
					num = num5;
					num2 = num4;
					num3 = num10;
					break;
				case 3:
					num = num5;
					num2 = num11;
					num3 = num4;
					break;
				case 4:
					num = num10;
					num2 = num5;
					num3 = num4;
					break;
				case 5:
					num = num4;
					num2 = num5;
					num3 = num11;
					break;
			}
		}
		Red = (byte)MathX.Clamp(0.0, num * 255.0, 255.0);
		Green = (byte)MathX.Clamp(0.0, num2 * 255.0, 255.0);
		Blue = (byte)MathX.Clamp(0.0, num3 * 255.0, 255.0);
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
		if (!TryParseHex(s, out var result))
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
