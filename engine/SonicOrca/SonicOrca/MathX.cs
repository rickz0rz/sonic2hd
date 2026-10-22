using System;

namespace SonicOrca;

public static class MathX
{
	public const double TWOPI = Math.PI * 2.0;

	public const double PI_2 = Math.PI / 2.0;

	public const double PI_4 = Math.PI / 4.0;

	public static double Lerp(double a, double b, double t)
	{
		return (1.0 - t) * a + t * b;
	}

	public static double Lerp(double from, double to, double amount, double minChange)
	{
		double num = (to - from) * amount;
		if (to > from)
		{
			from += ((num < minChange) ? minChange : num);
			if (!(from > to))
			{
				return from;
			}
			return to;
		}
		from += ((num > 0.0 - minChange) ? (0.0 - minChange) : num);
		if (!(from < to))
		{
			return from;
		}
		return to;
	}

	public static double LerpWrap(double from, double to, double amount, double minValue, double maxValue, double minChange = 0.0)
	{
		double num = maxValue - minValue;
		double num2 = num / 2.0;
		from %= num;
		for (to %= num; to < from - num2; to += num)
		{
		}
		while (to > from + num2)
		{
			to -= num;
		}
		from = Lerp(from, to, amount, minChange);
		if (from < minValue)
		{
			return from + num;
		}
		if (from > maxValue)
		{
			return from - num;
		}
		return from;
	}

	public static double Snap(double value, double precision)
	{
		return Math.Round(value / precision) * precision;
	}

	public static bool IsBetween(double a, double b, double c)
	{
		if (a <= b)
		{
			return b <= c;
		}
		return false;
	}

	public static int Clamp(int low, int value, int high)
	{
		if (value < low)
		{
			return low;
		}
		if (value > high)
		{
			return high;
		}
		return value;
	}

	public static int Clamp(int value, int high)
	{
		return Clamp(-high, value, high);
	}

	public static double Clamp(double low, double value, double high)
	{
		if (value < low)
		{
			return low;
		}
		if (value > high)
		{
			return high;
		}
		return value;
	}

	public static double Clamp(double value, double high)
	{
		return Clamp(0.0 - high, value, high);
	}

	public static double ChangeSpeed(double x, double amount)
	{
		if (x < 0.0)
		{
			x -= amount;
			if (x > 0.0)
			{
				x = 0.0;
			}
		}
		else
		{
			x += amount;
			if (x < 0.0)
			{
				x = 0.0;
			}
		}
		return x;
	}

	public static double ChangeSpeedNonZero(double x, double amount)
	{
		x = ((!(x < 0.0)) ? (x + amount) : (x - amount));
		return x;
	}

	public static int Wrap(int x, int max, int min = 0)
	{
		int num = max - min;
		while (x < min)
		{
			x += num;
		}
		while (x > max)
		{
			x -= num;
		}
		return x;
	}

	public static double Wrap(double x, double max, double min = 0.0)
	{
		double num = max - min;
		while (x < min)
		{
			x += num;
		}
		while (x > max)
		{
			x -= num;
		}
		return x;
	}

	public static double WrapRadians(double radians)
	{
		if (radians < Math.PI)
		{
			radians += Math.PI * 2.0;
		}
		if (radians > Math.PI)
		{
			radians -= Math.PI * 2.0;
		}
		return radians;
	}

	public static int GoTowards(int x, int dest, int maxChange)
	{
		if (x < dest)
		{
			return Math.Min(x + maxChange, dest);
		}
		if (x > dest)
		{
			return Math.Max(x - maxChange, dest);
		}
		return x;
	}

	public static double GoTowards(double x, double dest, double maxChange)
	{
		if (x < dest)
		{
			return Math.Min(x + maxChange, dest);
		}
		if (x > dest)
		{
			return Math.Max(x - maxChange, dest);
		}
		return x;
	}

	public static double GoTowardsWrap(double x, double dest, double maxChange, double minValue, double maxValue)
	{
		x = Wrap(x, maxValue, minValue);
		dest = Wrap(dest, maxValue, minValue);
		if (x == dest)
		{
			return x;
		}
		if (x < dest)
		{
			double num = x - minValue + (maxValue - dest);
			double num2 = dest - x;
			if (num < num2)
			{
				return Wrap(x - Math.Min(num, maxChange), maxValue, minValue);
			}
			return Math.Min(x + maxChange, dest);
		}
		double num3 = x - dest;
		double num4 = maxValue - x + (dest - minValue);
		if (num3 < num4)
		{
			return Math.Max(x - maxChange, dest);
		}
		return Wrap(x + Math.Min(num4, maxChange), maxValue, minValue);
	}

	public static double WrapDifference(double a, double b, double minValue, double maxValue)
	{
		if (a == b)
		{
			return 0.0;
		}
		if (a < b)
		{
			double num = a - minValue + (maxValue - b);
			double num2 = b - a;
			if (num < num2)
			{
				return 0.0 - num;
			}
			return num2;
		}
		double num3 = a - b;
		double num4 = maxValue - a + (b - minValue);
		if (num3 < num4)
		{
			return 0.0 - num3;
		}
		return num4;
	}

	public static double ToRadians(double degrees)
	{
		return degrees * (Math.PI / 180.0);
	}

	public static double ToDegrees(double radians)
	{
		return radians * (180.0 / Math.PI);
	}

	public static double DifferenceRadians(double a, double b)
	{
		return WrapDifference(a, b, -Math.PI, Math.PI);
	}
}
