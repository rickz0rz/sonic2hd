using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.FORCESPINBALL;

/// <summary>
/// An invisible marker line. When a character crosses it, the marker turns the forced spinball state of the
/// character on or off, as set by the direction. The line is vertical, or horizontal as Obj84 of Sonic 2 can be.
/// </summary>
public class ForceSpinballInstance : ActiveObject
{
	/// <summary>The half length of the line. The HD levels use the default.</summary>
	[StateVariable]
	private int _radius = 64;

	/// <summary>True for a horizontal line, which a character crosses down or up.</summary>
	[StateVariable]
	private bool _horizontal;

	/// <summary>The last side (-1 is left, 1 is right) of the marker that each character was on.</summary>
	private readonly Dictionary<ICharacter, int> _characterTrace = new Dictionary<ICharacter, int>();

	/// <summary>
	/// 1: a character that crosses to the right (or down) is forced into a spinball, and a character that crosses to
	/// the left (or up) is released. 3: the opposite. 0 and 2: the marker has no effect.
	/// </summary>
	[StateVariable]
	private int _direction = 1;

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
			UpdateCharacter(character);
	}

	private void UpdateCharacter(ICharacter character)
	{
		if (character.IsDebug || _direction == 0 || _direction == 2)
			return;
		int across = _horizontal ? character.Position.Y - Position.Y : character.Position.X - Position.X;
		int along = _horizontal ? character.Position.X - Position.X : character.Position.Y - Position.Y;
		int side = across < 0 ? -1 : 1;
		if (_characterTrace.ContainsKey(character) && _characterTrace[character] != side)
		{
			if (along >= -_radius && along < _radius)
				character.ForceSpinball = side != -1 ? _direction == 1 : _direction == 3;
		}
		_characterTrace[character] = side;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
			return;
		Rectangle line = _horizontal ? new Rectangle(-_radius, -2.0, _radius * 2, 4.0) : new Rectangle(-2.0, -_radius, 4.0, _radius * 2);
		renderer.Get2dRenderer().RenderRectangle(Colours.White, line, 1.0);
	}
}
