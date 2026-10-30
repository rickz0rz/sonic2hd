namespace S2HD;

/// <summary>
/// The palette fades of the original game, for one Mega Drive colour (0BGR, 3 bits for each channel). Each call is
/// one frame of the fade. A full fade takes 22 frames.
/// </summary>
internal static class MegaDriveFade
{
	/// <summary>One step of Pal_FadeFromBlack: blue goes up first, then green, then red.</summary>
	public static ushort FadeIn(ushort colour, ushort target)
	{
		if (colour == target)
		{
			return colour;
		}
		if (colour + 0x200 <= target)
		{
			return (ushort)(colour + 0x200);
		}
		if (colour + 0x20 <= target)
		{
			return (ushort)(colour + 0x20);
		}
		return (ushort)(colour + 2);
	}

	/// <summary>One step of Pal_FadeToBlack: red goes down first, then green, then blue.</summary>
	public static ushort FadeOut(ushort colour)
	{
		if ((colour & 0xE) != 0)
		{
			return (ushort)(colour - 2);
		}
		if ((colour & 0xE0) != 0)
		{
			return (ushort)(colour - 0x20);
		}
		if ((colour & 0xE00) != 0)
		{
			return (ushort)(colour - 0x200);
		}
		return colour;
	}
}
