namespace SonicOrca.Core.Objects;

/// <summary>
/// An ability that a character got, for example from an item: a double jump. The character calls it at fixed points of
/// its movement. The character loses its abilities when it gets hurt, when it dies, and at the end of the act.
/// </summary>
public interface ICharacterAbility
{
	/// <summary>The key of the type of the ability. A character has one ability of each type.</summary>
	string AbilityKey { get; }

	/// <summary>The character got the ability.</summary>
	void OnGiven(ICharacter character);

	/// <summary>The player pressed jump in the air, after a jump.</summary>
	/// <returns>True when the ability used the press. Then the character does not do its own action in the air.</returns>
	bool OnAirJump(ICharacter character);

	/// <summary>The character jumped from the ground.</summary>
	void OnJump(ICharacter character);

	/// <summary>The character landed.</summary>
	void OnLand(ICharacter character);

	/// <summary>Each tick, after the character set its speed values. The ability can change them.</summary>
	void OnTick(ICharacter character);

	/// <summary>The character lost the ability.</summary>
	void OnLost(ICharacter character);
}
