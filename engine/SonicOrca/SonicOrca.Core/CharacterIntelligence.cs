using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;

namespace SonicOrca.Core;

/// <summary>
/// Simple computer control for a character.
/// </summary>
public static class CharacterIntelligence
{
	public static void SimpleMoveRightJumpObsticals(ICharacter character)
	{
		// Move right. Jump when the character pushes against an obstacle, and hold the jump while airborne.
		CharacterInputState input = new CharacterInputState();
		input.HorizontalDirection = 1;
		if (character.IsAirborne)
		{
			input.A = CharacterInputButtonState.Down;
		}
		else if (!character.IsAirborne && character.IsPushing)
		{
			input.A = CharacterInputButtonState.Pressed;
		}
		character.Input = input;
	}
}
