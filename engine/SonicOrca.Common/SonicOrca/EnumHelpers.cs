using System;

namespace SonicOrca;

public static class EnumHelpers
{
	public static int GetEnumCount(Type enumeration)
	{
		return Enum.GetNames(enumeration).Length;
	}
}
