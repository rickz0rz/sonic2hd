using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Core.Objects;
using SonicOrca.Core.Objects.Metadata;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.LAYERSWITCH;

/// <summary>
/// An invisible line that changes the path and the layer of a character that crosses it. A switch with a width is
/// horizontal and reacts to characters that cross it up or down. Otherwise it is vertical and reacts to characters that
/// cross it left or right.
/// </summary>
public class LayerSwitchInstance : ActiveObject
{
	/// <summary>The last side (-1 or 1) of the switch that each character was on.</summary>
	private readonly Dictionary<ICharacter, int> _characterTrace = new Dictionary<ICharacter, int>();
	private bool _allowAirborne = true;
	private bool _pathConstrained;
	private bool _layerOnly;
	private int _path;
	private int _width;
	private int _height;
	private int _above;
	private int _below;
	private int _left;
	private int _right;
	private int _aboveLayer;
	private int _belowLayer;
	private int _leftLayer;
	private int _rightLayer;
	private bool _horizontal;
	private int _radius;
	private int _pathA;
	private int _pathB;
	private int _layerA;
	private int _layerB;

	[StateVariable]
	private bool AllowAirborne
	{
		get => _allowAirborne;
		set => _allowAirborne = value;
	}

	/// <summary>The switch changes only the layer of a character, not its path (a flipped switch of Sonic 2).</summary>
	[StateVariable]
	private bool LayerOnly
	{
		get => _layerOnly;
		set => _layerOnly = value;
	}

	[StateVariable]
	private bool PathConstrained
	{
		get => _pathConstrained;
		set => _pathConstrained = value;
	}

	[StateVariable]
	private int Path
	{
		get => _path;
		set => _path = value;
	}

	[StateVariable]
	public int Width
	{
		get => _width;
		set => _width = value;
	}

	[StateVariable]
	public int Height
	{
		get => _height;
		set => _height = value;
	}

	[StateVariable]
	private int Above
	{
		get => _above;
		set => _above = value;
	}

	[StateVariable]
	private int Below
	{
		get => _below;
		set => _below = value;
	}

	[StateVariable]
	private int Left
	{
		get => _left;
		set => _left = value;
	}

	[StateVariable]
	private int Right
	{
		get => _right;
		set => _right = value;
	}

	[StateVariable]
	private int AboveLayer
	{
		get => _aboveLayer;
		set => _aboveLayer = value;
	}

	[StateVariable]
	private int BelowLayer
	{
		get => _belowLayer;
		set => _belowLayer = value;
	}

	[StateVariable]
	private int LeftLayer
	{
		get => _leftLayer;
		set => _leftLayer = value;
	}

	[StateVariable]
	private int RightLayer
	{
		get => _rightLayer;
		set => _rightLayer = value;
	}

	protected override void OnStart()
	{
		if (_width != 0)
		{
			_horizontal = true;
			_pathA = _above;
			_pathB = _below;
			_layerA = _aboveLayer;
			_layerB = _belowLayer;
			_radius = _width / 2;
			DesignBounds = new Rectanglei(-_radius, -4, _radius * 2, 8);
		}
		else
		{
			_pathA = _left;
			_pathB = _right;
			_layerA = _leftLayer;
			_layerB = _rightLayer;
			_radius = _height / 2;
			DesignBounds = new Rectanglei(-4, -_radius, 8, _radius * 2);
		}
		// The layer state variables are relative to the layer of the switch.
		int layerIndex = Layer.Index;
		_layerA += layerIndex;
		_layerB += layerIndex;
	}

	protected override void OnUpdate()
	{
		foreach (ICharacter character in Level.ObjectManager.Characters)
			UpdateCharacter(character);
	}

	private void UpdateCharacter(ICharacter character)
	{
		if (character.IsDebug || !character.ShouldReactToLevel || character.IsObjectControlled || _pathConstrained && character.Path != _path)
			return;
		// Side A is above (horizontal switch) or to the left (vertical switch). Side B is below or to the right.
		int side = _horizontal
			? (character.Position.Y < Position.Y ? -1 : 1)
			: (character.Position.X < Position.X ? -1 : 1);
		if (_characterTrace.TryGetValue(character, out int previousSide))
		{
			if ((_allowAirborne || !character.IsAirborne) && previousSide != side && IsAlongSwitch(character))
			{
				if (side == -1)
				{
					if (!_layerOnly)
						character.Path = _pathA;
					character.Layer = Level.Map.Layers[_layerA];
				}
				else
				{
					if (!_layerOnly)
						character.Path = _pathB;
					character.Layer = Level.Map.Layers[_layerB];
				}
			}
		}
		_characterTrace[character] = side;
	}

	/// <summary>Returns true when the character is within the length of the switch line.</summary>
	private bool IsAlongSwitch(ICharacter character)
	{
		if (_horizontal)
			return character.Position.X >= Position.X - _radius && character.Position.X < Position.X + _radius;
		return character.Position.Y >= Position.Y - _radius && character.Position.Y < Position.Y + _radius;
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
			return;
		I2dRenderer renderer2d = renderer.Get2dRenderer();
		Vector2 scale = renderer.GetObjectRenderer().Scale;
		Colour yellow = new Colour(byte.MaxValue, byte.MaxValue, 0);
		// A white line on the switch, with a yellow line on each side of it.
		if (_horizontal)
		{
			renderer2d.RenderLine(yellow, new Vector2(-_radius * scale.X, -2.0 * scale.Y), new Vector2(_radius * scale.X, -2.0 * scale.Y), 1.0);
			renderer2d.RenderLine(Colours.White, new Vector2(-_radius * scale.X, 0.0 * scale.Y), new Vector2(_radius * scale.X, 0.0 * scale.Y), 1.0);
			renderer2d.RenderLine(yellow, new Vector2(-_radius * scale.X, 2.0 * scale.Y), new Vector2(_radius * scale.X, 2.0 * scale.Y), 1.0);
		}
		else
		{
			renderer2d.RenderLine(yellow, new Vector2(-2.0 * scale.X, -_radius * scale.Y), new Vector2(-2.0 * scale.X, _radius * scale.Y), 1.0);
			renderer2d.RenderLine(Colours.White, new Vector2(0.0, -_radius * scale.Y), new Vector2(0.0, _radius * scale.Y), 1.0);
			renderer2d.RenderLine(yellow, new Vector2(2.0 * scale.X, -_radius * scale.Y), new Vector2(2.0 * scale.X, _radius * scale.Y), 1.0);
		}
	}
}
