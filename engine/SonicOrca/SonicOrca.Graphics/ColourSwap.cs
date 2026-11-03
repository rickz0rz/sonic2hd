namespace SonicOrca.Graphics;

/// <summary>
/// A colour of a texture that the object shader draws as another colour, for the palette cycles of the original on
/// the sprites of the objects.
/// </summary>
public readonly record struct ColourSwap(Colour From, Colour To);
