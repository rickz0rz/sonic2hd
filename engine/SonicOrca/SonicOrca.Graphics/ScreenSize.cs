namespace SonicOrca.Graphics;

/// <summary>
/// The size of the picture that the game draws: 1920 x 1080. The game stretches the picture to the window, so the
/// HUD, the menus and the screens use these coordinates at any window size. Code that draws a level must use the size
/// of its viewport or framebuffer, not this size, because a half of versus mode or a zoomed-out view has another size.
/// </summary>
public static class ScreenSize
{
	public const int Width = 1920;

	public const int Height = 1080;
}
