namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// A special state of a character that changes how it animates.
/// </summary>
public enum CharacterSpecialState
{
	Normal,
	Falling,
	Sliding,
	Grabbed,
	/// <summary>The character hangs from an object, for example a vine in Mystic Cave.</summary>
	Hanging
}
