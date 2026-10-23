using System;

namespace SonicOrca;

/// <summary>
/// Contains math helpers for interpolation, clamps, wrapped ranges and angles.
/// </summary>
public static class MathX
{
	public const double TWOPI = Math.PI * 2.0;

	public const double PI_2 = Math.PI / 2.0;

	public const double PI_4 = Math.PI / 4.0;

	public static double Lerp(double a, double b, double t)
	{
		return (1.0 - t) * a + t * b;
	}

	/// <summary>
	/// Moves <paramref name="from"/> towards <paramref name="to"/> by a fraction of the distance. Each move is at
	/// least <paramref name="minChange"/>, and the result never goes past <paramref name="to"/>.
	/// </summary>
	public static double Lerp(double from, double to, double amount, double minChange)
	{
		double change = (to - from) * amount;
		if (to > from)
		{
			from += ((change < minChange) ? minChange : change);
			if (!(from > to))
			{
				return from;
			}
			return to;
		}
		from += ((change > 0.0 - minChange) ? (0.0 - minChange) : change);
		if (!(from < to))
		{
			return from;
		}
		return to;
	}

	/// <summary>
	/// Does <see cref="Lerp(double, double, double, double)"/> in a range that wraps, such as an angle.
	/// It moves along the shorter path.
	/// </summary>
	public static double LerpWrap(double from, double to, double amount, double minValue, double maxValue, double minChange = 0.0)
	{
		double range = maxValue - minValue;
		double halfRange = range / 2.0;
		from %= range;
		to %= range;
		// Move the target by whole ranges until it is within half a range of the start.
		while (to < from - halfRange)
		{
			to += range;
		}
		while (to > from + halfRange)
		{
			to -= range;
		}
		from = Lerp(from, to, amount, minChange);
		if (from < minValue)
		{
			return from + range;
		}
		if (from > maxValue)
		{
			return from - range;
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
		int range = max - min;
		while (x < min)
		{
			x += range;
		}
		while (x > max)
		{
			x -= range;
		}
		return x;
	}

	public static double Wrap(double x, double max, double min = 0.0)
	{
		double range = max - min;
		while (x < min)
		{
			x += range;
		}
		while (x > max)
		{
			x -= range;
		}
		return x;
	}

	// Note: the first test is against PI, not -PI. Thus a value from -PI to PI moves up by 2 PI and then back down.
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

	/// <summary>
	/// Does <see cref="GoTowards(double, double, double)"/> in a range that wraps, such as an angle.
	/// It moves along the shorter path.
	/// </summary>
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
			double distanceDown = x - minValue + (maxValue - dest);
			double distanceUp = dest - x;
			if (distanceDown < distanceUp)
			{
				return Wrap(x - Math.Min(distanceDown, maxChange), maxValue, minValue);
			}
			return Math.Min(x + maxChange, dest);
		}
		double distanceDownToDest = x - dest;
		double distanceUpToDest = maxValue - x + (dest - minValue);
		if (distanceDownToDest < distanceUpToDest)
		{
			return Math.Max(x - maxChange, dest);
		}
		return Wrap(x + Math.Min(distanceUpToDest, maxChange), maxValue, minValue);
	}

	/// <summary>
	/// Gets the signed change along the shorter path from <paramref name="a"/> to <paramref name="b"/> in a range
	/// that wraps.
	/// </summary>
	public static double WrapDifference(double a, double b, double minValue, double maxValue)
	{
		if (a == b)
		{
			return 0.0;
		}
		if (a < b)
		{
			double distanceDown = a - minValue + (maxValue - b);
			double distanceUp = b - a;
			if (distanceDown < distanceUp)
			{
				return 0.0 - distanceDown;
			}
			return distanceUp;
		}
		double distanceDownToB = a - b;
		double distanceUpToB = maxValue - a + (b - minValue);
		if (distanceDownToB < distanceUpToB)
		{
			return 0.0 - distanceDownToB;
		}
		return distanceUpToB;
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
