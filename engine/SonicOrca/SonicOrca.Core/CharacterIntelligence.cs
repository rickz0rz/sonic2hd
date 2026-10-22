using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Base;

namespace SonicOrca.Core;

public static class CharacterIntelligence
{
	public static void SimpleMoveRightJumpObsticals(ICharacter character)
	{
		CharacterInputState characterInputState = new CharacterInputState();
		characterInputState.HorizontalDirection = 1;
		if (character.IsAirborne)
		{
			characterInputState.A = CharacterInputButtonState.Down;
		}
		else if (!character.IsAirborne && character.IsPushing)
		{
			characterInputState.A = CharacterInputButtonState.Pressed;
		}
		character.Input = characterInputState;
	}
}
