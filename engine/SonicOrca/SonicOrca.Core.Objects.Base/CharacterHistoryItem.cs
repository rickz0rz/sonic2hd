using SonicOrca.Geometry;

namespace SonicOrca.Core.Objects.Base;

public class CharacterHistoryItem
{
	private readonly Vector2 _positionPrecise;

	private readonly CharacterState _state;

	private readonly CharacterInputState _input;

	public Vector2i Position => (Vector2i)_positionPrecise;

	public Vector2 PositionPrecise => _positionPrecise;

	public CharacterState State => _state;

	public CharacterInputState Input => _input;

	public CharacterHistoryItem(Vector2 positionPrecise, CharacterState state, CharacterInputState input)
	{
		_positionPrecise = positionPrecise;
		_state = state;
		_input = input;
	}
}
