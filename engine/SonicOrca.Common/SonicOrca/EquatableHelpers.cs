using System;

namespace SonicOrca;

public static class EquatableHelpers
{
	public static bool ReferenceThenValueEquals<T>(T a, T b) where T : IEquatable<T>
	{
		if ((object)a == (object)b)
		{
			return true;
		}
		if (a == null || b == null)
		{
			return false;
		}
		return a.Equals(b);
	}
}
