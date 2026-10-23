namespace SonicOrca.Core.Collision;

/// <summary>
/// The surface that a character touches. Air means that the character touches no surface.
/// </summary>
public enum CollisionMode
{
	Air = -1,
	Top,
	Left,
	Bottom,
	Right
}
