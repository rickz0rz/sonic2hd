// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.FORCESPINBALL.ForceSpinballInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.FORCESPINBALL;

/// <summary>
/// An invisible vertical marker. When a character crosses it, the marker turns the forced spinball state of the
/// character on or off, as set by the direction.
/// </summary>
public class ForceSpinballInstance : ActiveObject
{
	/// <summary>The half size of the square area in which the marker has an effect.</summary>
	private const int Radius = 64;

	/// <summary>The last side (-1 is left, 1 is right) of the marker that each character was on.</summary>
	private readonly Dictionary<ICharacter, int> _characterTrace = new Dictionary<ICharacter, int>();

	/// <summary>
	/// 1: a character that crosses to the right is forced into a spinball, and a character that crosses to the left
	/// is released. 3: the opposite. 0 and 2: the marker has no effect.
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
		int side = character.Position.X < Position.X ? -1 : 1;
		if (_characterTrace.ContainsKey(character) && _characterTrace[character] != side)
		{
			if (character.Position.Y >= Position.Y - Radius && character.Position.Y < Position.Y + Radius)
				character.ForceSpinball = side != -1 ? _direction == 1 : _direction == 3;
		}
		_characterTrace[character] = side;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
			return;
		renderer.Get2dRenderer().RenderRectangle(Colours.White, new Rectangle(-64.0, -64.0, 128.0, 128.0), 1.0);
	}
}
