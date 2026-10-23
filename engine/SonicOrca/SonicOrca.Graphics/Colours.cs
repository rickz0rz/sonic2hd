namespace SonicOrca.Graphics;

/// <summary>
/// Named colours. Each value is in ARGB format.
/// </summary>
public static class Colours
{
	public static Colour Transparent => new Colour(0u);

	public static Colour Black => new Colour(0xFF000000u);

	public static Colour White => new Colour(uint.MaxValue);

	public static Colour Red => new Colour(0xFFFF0000u);

	public static Colour Green => new Colour(0xFF008000u);

	public static Colour Blue => new Colour(0xFF0000FFu);

	public static Colour Yellow => new Colour(0xFFFFFF00u);

	public static Colour Lime => new Colour(0xFF00FF00u);

	public static Colour Aqua => new Colour(0xFF00FFFFu);

	public static Colour Fuchsia => new Colour(0xFFFF00FFu);
}
