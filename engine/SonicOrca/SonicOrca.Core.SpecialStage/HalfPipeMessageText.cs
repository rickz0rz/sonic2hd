using System;
using System.Collections.Generic;

namespace SonicOrca.Core.SpecialStage;

/// <summary>
/// One letter or digit of a message of the special stage, at its centre on the 256 x 224 screen of the original.
/// A letter is a frame of the messages art (Obj5A); a digit is a frame of the HUD art (Obj5F_MapUnc_72D2).
/// </summary>
public readonly record struct HalfPipeGlyph(bool Digit, int Frame, int X, int Y);

/// <summary>
/// The letters and digits of the messages of the special stage, at the places where the original prints them
/// (s2disasm: Obj5A_CreateRingReqMessage, Obj5A_PrintCheckpointMessage and Obj5A_CreateRingsToGoText). Each letter or
/// digit is 8 pixels wide.
/// </summary>
public static class HalfPipeMessageText
{
	/// <summary>The centre of the screen of the original. The letters of a message fly away from it.</summary>
	public const int CentreX = 0x80;

	public const int CentreY = 0x70;

	/// <summary>The speed of the letters that fly away, in pixels of the original a frame (Obj5A_TextFlyout).</summary>
	public const int FlyoutSpeed = 8;

	/// <summary>The frames of the letters in the messages art (the charset of Obj5A). Letters that no message uses have no frame.</summary>
	private static readonly Dictionary<char, int> LetterFrames = new Dictionary<char, int>
	{
		['G'] = 0, ['E'] = 1, ['T'] = 2, ['R'] = 3, ['I'] = 4, ['N'] = 5, ['S'] = 6, ['C'] = 7, ['O'] = 8, ['L'] = 9,
		['U'] = 0xA, ['H'] = 0xB, ['P'] = 0xC, ['A'] = 0xD, ['Y'] = 0xE, ['M'] = 0xF, ['W'] = 0x10, ['!'] = 0x11, ['.'] = 0x12
	};

	/// <summary>"GET xx RINGS!" at y $6C: the digits end at x $78, or $80 with a hundreds digit.</summary>
	public static List<HalfPipeGlyph> RingRequirement(int goal)
	{
		const int y = 0x6C;
		List<HalfPipeGlyph> glyphs = Word("GET", 0x54, y);
		string digits = Math.Max(0, goal).ToString("00");
		int last = digits.Length > 2 ? 0x80 : 0x78;
		for (int i = 0; i < digits.Length; i++)
		{
			glyphs.Add(new HalfPipeGlyph(true, digits[i] - '0', last - (digits.Length - 1 - i) * 8, y));
		}
		glyphs.AddRange(Word("RINGS!", digits.Length > 2 ? 0x8C : 0x84, y));
		return glyphs;
	}

	/// <summary>"COOL!" at ($74, $68), or "NOT ENOUGH" at ($5E, $68) and "RINGS..." at ($5E, $7E), under the winged hand.</summary>
	public static List<HalfPipeGlyph> Checkpoint(bool passed)
	{
		if (passed)
		{
			return Word("COOL!", 0x74, 0x68);
		}
		List<HalfPipeGlyph> glyphs = Word("NOT", 0x5E, 0x68);
		glyphs.AddRange(Word("ENOUGH", 0x5E + 4 * 8, 0x68));
		glyphs.AddRange(Word("RINGS", 0x5E, 0x7E));
		// The original adds a space after each word, so the dots come one letter after RINGS
		glyphs.AddRange(Word("...", 0x5E + 6 * 8, 0x7E));
		return glyphs;
	}

	/// <summary>
	/// "xx RINGS TO GO!" at y $38: the digits end at x $5A, "RING" starts at $68, and "S" is at $88. Without the S (1 ring
	/// or 0), "TO GO!" moves 8 pixels to the left.
	/// </summary>
	public static List<HalfPipeGlyph> RingsToGo(int toGo)
	{
		const int y = 0x38;
		List<HalfPipeGlyph> glyphs = new List<HalfPipeGlyph>();
		string digits = Math.Clamp(toGo, 0, 999).ToString();
		for (int i = 0; i < digits.Length; i++)
		{
			glyphs.Add(new HalfPipeGlyph(true, digits[i] - '0', 0x5A - (digits.Length - 1 - i) * 8, y));
		}
		glyphs.AddRange(Word("RING", 0x68, y));
		int shift = toGo >= 2 ? 0 : -8;
		if (toGo >= 2)
		{
			glyphs.AddRange(Word("S", 0x88, y));
		}
		glyphs.AddRange(Word("TO", 0x98 + shift, y));
		glyphs.AddRange(Word("GO!", 0xB0 + shift, y));
		return glyphs;
	}

	private static List<HalfPipeGlyph> Word(string word, int x, int y)
	{
		List<HalfPipeGlyph> glyphs = new List<HalfPipeGlyph>();
		foreach (char letter in word)
		{
			glyphs.Add(new HalfPipeGlyph(false, LetterFrames[letter], x, y));
			x += 8;
		}
		return glyphs;
	}
}
