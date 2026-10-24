using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;

namespace SONICORCA.OBJECTS.OOZOIL;

/// <summary>
/// The oil of Oil Ocean Zone, as in Sonic 2 (Obj07). The oil holds each character up on a thin floor that follows
/// the character. The floor starts $30 pixels above the oil line. While a character stands on it, the floor of that
/// character sinks 1 pixel each frame. When it reaches the oil line, the character dies. While the character is off
/// the floor (for example after a jump), the floor rises 1 pixel each frame back to $30 pixels above the oil line.
/// The oil line is $758 in the original. The level events of act 2 move it to $2D8 for the boss: set <see cref="OilY"/>.
/// </summary>
public class OOZOilInstance : ActiveObject
{
	private const int Scale = 4;

	/// <summary>The floor of each character is $20 pixels to each side of the character.</summary>
	private const int HalfWidth = 0x20 * Scale;

	/// <summary>The height of the floor above the oil line when a character is not in the oil (oil_char1submersion).</summary>
	private const int MaxSubmersion = 0x30;

	private readonly Dictionary<ICharacter, int> _submersion = new Dictionary<ICharacter, int>();

	private ICharacter[] _characters = new ICharacter[0];

	private int _oilY;

	/// <summary>
	/// The oil line, in level units (HD pixels). The default is the position of the object. The area can change it at
	/// any time.
	/// </summary>
	[StateVariable]
	public int OilY
	{
		get => _oilY;
		set => _oilY = value;
	}

	public OOZOilInstance() => DesignBounds = new Rectanglei(-HalfWidth, -MaxSubmersion * Scale, HalfWidth * 2, MaxSubmersion * Scale);

	protected override void OnStart()
	{
		if (_oilY == 0)
		{
			_oilY = Position.Y;
		}
		// The oil is at the bottom of the whole zone, so it must stay when the camera is far from it
		LockLifetime = true;
		CollisionVectors = new CollisionVector[0];
	}

	protected override void OnUpdate()
	{
		ICharacter[] characters = Level.ObjectManager.Characters.ToArray();
		// The object stays at the oil line under the main character, so that the floors stay near the object
		ICharacter protagonist = Level.Player.Protagonist;
		if (protagonist != null)
		{
			Position = new Vector2i(protagonist.Position.X, _oilY);
		}
		for (int i = 0; i < characters.Length; i++)
		{
			ICharacter character = characters[i];
			if (!_submersion.TryGetValue(character, out int submersion))
			{
				submersion = MaxSubmersion;
			}
			if (character.ObjectLink == this && !character.IsDebug && !character.IsDying)
			{
				if (submersion == 0)
				{
					character.Kill(PlayerDeathCause.Hurt);
				}
				else
				{
					submersion--;
				}
			}
			else if (submersion < MaxSubmersion)
			{
				submersion++;
			}
			_submersion[character] = submersion;
		}
		UpdateFloors(characters);
	}

	/// <summary>Puts the floor of each character under the character, at its height above the oil line.</summary>
	private void UpdateFloors(ICharacter[] characters)
	{
		if (characters.Length != _characters.Length || !characters.SequenceEqual(_characters))
		{
			_characters = characters;
			CollisionVectors = characters
				.Select(c => new CollisionVector(this, new Vector2i(), new Vector2i(1, 0), flags: CollisionFlags.Conveyor | CollisionFlags.NoBalance))
				.ToArray();
		}
		for (int i = 0; i < characters.Length; i++)
		{
			int x = characters[i].Position.X - Position.X;
			int y = _oilY - Position.Y - _submersion[characters[i]] * Scale;
			CollisionVectors[i].RelativeA = new Vector2i(x - HalfWidth, y);
			CollisionVectors[i].RelativeB = new Vector2i(x + HalfWidth, y);
		}
		RegisterCollisionUpdate();
	}
}
