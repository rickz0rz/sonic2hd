namespace SonicOrca.Core.Objects.Base;

/// <summary>
/// The state of an input button. The <see cref="Down"/> bit is set while the button is down, also in <see cref="Pressed"/>.
/// </summary>
public enum CharacterInputButtonState : byte
{
	Up,
	Down,
	Released,
	Pressed
}
