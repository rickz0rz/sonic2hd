using System;
using System.Collections.Generic;

namespace SonicOrca.Graphics.V2.Animation;

/// <summary>
/// Compares strings so that numbers sort by value and come before other strings. Other strings sort without case.
/// </summary>
public class SemiNumericComparer : IComparer<string>
{
	public int Compare(string s1, string s2)
	{
		bool isNumeric1 = IsNumeric(s1);
		bool isNumeric2 = IsNumeric(s2);
		if (isNumeric1 && isNumeric2)
		{
			int number1 = Convert.ToInt32(s1);
			int number2 = Convert.ToInt32(s2);
			if (number1 > number2)
			{
				return 1;
			}
			if (number1 < number2)
			{
				return -1;
			}
			return 0;
		}
		if (isNumeric1)
		{
			return -1;
		}
		if (isNumeric2)
		{
			return 1;
		}
		return string.Compare(s1, s2, ignoreCase: true);
	}

	public static bool IsNumeric(object value)
	{
		try
		{
			Convert.ToInt32(value.ToString());
			return true;
		}
		catch (FormatException)
		{
			return false;
		}
	}
}
