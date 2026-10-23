using System;

namespace SonicOrca.Extensions;

/// <summary>
/// Gets random values of more types and ranges from a <see cref="Random"/>.
/// </summary>
public static class RandomExtensions
{
	/// <summary>
	/// Gets a random value from -1.0 to 1.0.
	/// </summary>
	public static double NextSignedDouble(this Random random)
	{
		return random.NextDouble() * 2.0 - 1.0;
	}

	/// <summary>
	/// Gets a random angle from -PI to PI radians.
	/// </summary>
	public static double NextRadians(this Random random)
	{
		return random.NextSignedDouble() * Math.PI;
	}

	public static double NextDouble(this Random random, double maxValue)
	{
		return random.NextDouble() * maxValue;
	}

	public static double NextDouble(this Random random, double minValue, double maxValue)
	{
		return minValue + random.NextDouble() * (maxValue - minValue);
	}

	public static bool NextBoolean(this Random random)
	{
		return (random.Next() & 1) == 0;
	}

	public static int NextSign(this Random random)
	{
		if (!random.NextBoolean())
		{
			return 1;
		}
		return -1;
	}

	public static int NextSign(this Random random, int value)
	{
		if (!random.NextBoolean())
		{
			return value;
		}
		return -value;
	}
}
