using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SonicOrca.Extensions;

/// <summary>
/// Contains extension methods that read characters and tokens from a text reader.
/// </summary>
public static class TextReaderExtensions
{
	/// <summary>
	/// The value that <see cref="TextReader.Peek"/> and <see cref="TextReader.Read()"/> return at the end of the text.
	/// </summary>
	private const int EndOfText = -1;

	public static bool TryPeek(this TextReader reader, out char c)
	{
		int next = reader.Peek();
		if (next == EndOfText)
		{
			c = '\0';
			return false;
		}
		c = (char)next;
		return true;
	}

	public static bool TryRead(this TextReader reader, out char c)
	{
		int next = reader.Read();
		if (next == EndOfText)
		{
			c = '\0';
			return false;
		}
		c = (char)next;
		return true;
	}

	public static string ReadToWhitespace(this TextReader reader)
	{
		StringBuilder stringBuilder = new StringBuilder();
		char c;
		while (reader.TryPeek(out c) && !char.IsWhiteSpace(c))
		{
			reader.Read();
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	public static void SkipWhitespace(this TextReader reader)
	{
		char c;
		while (reader.TryPeek(out c) && char.IsWhiteSpace(c))
		{
			reader.Read();
		}
	}

	public static string ReadRegex(this TextReader reader, string regex)
	{
		return reader.ReadRegex(new Regex(regex));
	}

	/// <summary>
	/// Reads characters while the text that was read still matches <paramref name="regex"/>.
	/// </summary>
	public static string ReadRegex(this TextReader reader, Regex regex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		char c;
		while (reader.TryPeek(out c))
		{
			stringBuilder.Append(c);
			string text = stringBuilder.ToString();
			if (!regex.IsMatch(text))
			{
				// The peeked character breaks the match, so leave it in the reader.
				return text.Remove(text.Length - 1);
			}
			reader.Read();
		}
		return stringBuilder.ToString();
	}

	public static bool CanRead(this TextReader reader)
	{
		return reader.Peek() != EndOfText;
	}
}
