namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The side and distance of the ledge when a character stands near the edge of the ground.
/// </summary>
public enum CharacterBalanceDirection
{
	LeftFar = -2,
	LeftClose,
	None,
	RightClose,
	RightFar
}
