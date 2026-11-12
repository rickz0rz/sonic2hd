using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.WFZFIRESWITCH;

/// <summary>
/// An invisible vertical line in Wing Fortress (Obj8B). The zone has one palette cycle for the fire and one for the
/// conveyor belts, on the same colours. The level starts with the fire. A character that crosses the line to the
/// right shows the belts, and a character that crosses it to the left shows the fire. A flipped switch does the
/// opposite. The imported tiles have the belts as their alternate animation (<see cref="TileSet.UseAlternateAnimations"/>).
/// </summary>
public class WfzFireSwitchInstance : ActiveObject
{
	/// <summary>True for each character that was right of the line (objoff_34 and objoff_35).</summary>
	private readonly Dictionary<ICharacter, bool> _right = new Dictionary<ICharacter, bool>();

	private int _height = 256;

	private bool _flipX;

	/// <summary>The length of the line, centred on the object. A character outside it changes nothing.</summary>
	[StateVariable]
	public int Height
	{
		get => _height;
		set => _height = value;
	}

	/// <summary>The switch shows the fire to the right of the line and the belts to the left of it.</summary>
	[StateVariable]
	private bool FlipX
	{
		get => _flipX;
		set => _flipX = value;
	}

	protected override void OnStart()
	{
		DesignBounds = new Rectanglei(-4, -_height / 2, 8, _height);
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			UpdateCharacter(character);
		}
	}

	private void UpdateCharacter(ICharacter character)
	{
		if (character.IsDebug)
		{
			return;
		}
		bool right = character.Position.X >= Position.X;
		// The first update only records the side (Obj8B_Init)
		if (_right.TryGetValue(character, out bool wasRight) && wasRight != right && IsAlongSwitch(character) && Level.TileSet != null)
		{
			// To the right: the belts, or the fire with a flipped switch
			bool belts = right != _flipX;
			if (Level.TileSet.UseAlternateAnimations != belts)
			{
				System.Diagnostics.Trace.WriteLine($"WFZ fire switch at {Position.X}: {(belts ? "conveyor belts" : "fire")}, tick {Level.Ticks}");
			}
			Level.TileSet.UseAlternateAnimations = belts;
		}
		_right[character] = right;
	}

	/// <summary>True when the character is within the length of the line.</summary>
	private bool IsAlongSwitch(ICharacter character)
	{
		int radius = _height / 2;
		return character.Position.Y >= Position.Y - radius && character.Position.Y < Position.Y + radius;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
		{
			return;
		}
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		int radius = _height / 2;
		// An orange line, the colour of the fire
		renderer2d.RenderLine(new Colour(byte.MaxValue, 128, 0), new Vector2(0.0, -radius * scale.Y), new Vector2(0.0, radius * scale.Y), 1.0);
	}
}
