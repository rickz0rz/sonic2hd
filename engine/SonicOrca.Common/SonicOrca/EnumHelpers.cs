using System;

namespace SonicOrca;

/// <summary>
/// Contains helper methods for enumeration types.
/// </summary>
public static class EnumHelpers
{
	public static int GetEnumCount(Type enumeration)
	{
		return Enum.GetNames(enumeration).Length;
	}
}
