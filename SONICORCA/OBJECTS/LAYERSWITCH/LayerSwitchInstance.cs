// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.LAYERSWITCH.LayerSwitchInstance
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
namespace SONICORCA.OBJECTS.LAYERSWITCH;

public class LayerSwitchInstance : ActiveObject
{
	private readonly Dictionary<ICharacter, int> _characterTrace = new Dictionary<ICharacter, int>();
	private bool _allowAirborne = true;
	private bool _pathConstrained;
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
		int index = Layer.Index;
		_layerA += index;
		_layerB += index;
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
		if (_horizontal)
		{
			int y1 = character.Position.Y;
			Vector2i position = Position;
			int y2 = position.Y;
			int num1 = y1 < y2 ? -1 : 1;
			if (_characterTrace.ContainsKey(character))
			{
				int num2 = _characterTrace[character];
				if ((_allowAirborne || !character.IsAirborne) && num2 != num1)
				{
					position = character.Position;
					int x1 = position.X;
					position = Position;
					int num3 = position.X - _radius;
					if (x1 >= num3)
					{
						position = character.Position;
						int x2 = position.X;
						position = Position;
						int num4 = position.X + _radius;
						if (x2 < num4)
						{
							if (num1 == -1)
							{
								character.Path = _pathA;
								character.Layer = Level.Map.Layers[_layerA];
							}
							else
							{
								character.Path = _pathB;
								character.Layer = Level.Map.Layers[_layerB];
							}
						}
					}
				}
			}
			_characterTrace[character] = num1;
		}
		else
		{
			int x3 = character.Position.X;
			Vector2i position = Position;
			int x4 = position.X;
			int num5 = x3 < x4 ? -1 : 1;
			if (_characterTrace.ContainsKey(character))
			{
				int num6 = _characterTrace[character];
				if ((_allowAirborne || !character.IsAirborne) && num6 != num5)
				{
					position = character.Position;
					int y3 = position.Y;
					position = Position;
					int num7 = position.Y - _radius;
					if (y3 >= num7)
					{
						position = character.Position;
						int y4 = position.Y;
						position = Position;
						int num8 = position.Y + _radius;
						if (y4 < num8)
						{
							if (num5 == -1)
							{
								character.Path = _pathA;
								character.Layer = Level.Map.Layers[_layerA];
							}
							else
							{
								character.Path = _pathB;
								character.Layer = Level.Map.Layers[_layerB];
							}
						}
					}
				}
			}
			_characterTrace[character] = num5;
		}
	}

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		if (!viewOptions.ShowObjectCollision && !Level.StateFlags.HasFlag(LevelStateFlags.Editing))
			return;
		I2dRenderer obj1 = renderer.Get2dRenderer();
		if (_horizontal)
		{
			obj1.RenderLine(new Colour(byte.MaxValue, byte.MaxValue, 0), new Vector2(-_radius * renderer.GetObjectRenderer().Scale.X, -2.0 * renderer.GetObjectRenderer().Scale.Y), new Vector2(_radius * renderer.GetObjectRenderer().Scale.X, -2.0 * renderer.GetObjectRenderer().Scale.Y), 1.0);
			I2dRenderer obj2 = obj1;
			Colour white = Colours.White;
			double x1 = -_radius * renderer.GetObjectRenderer().Scale.X;
			Vector2 scale = renderer.GetObjectRenderer().Scale;
			double y1 = 0.0 * scale.Y;
			Vector2 a1 = new Vector2(x1, y1);
			double radius = _radius;
			scale = renderer.GetObjectRenderer().Scale;
			double x2 = scale.X;
			double x3 = radius * x2;
			scale = renderer.GetObjectRenderer().Scale;
			double y2 = 0.0 * scale.Y;
			Vector2 b1 = new Vector2(x3, y2);
			obj2.RenderLine(white, a1, b1, 1.0);
			I2dRenderer obj3 = obj1;
			Colour colour = new Colour(byte.MaxValue, byte.MaxValue, 0);
			double num = -_radius;
			scale = renderer.GetObjectRenderer().Scale;
			double x4 = scale.X;
			Vector2 a2 = new Vector2(num * x4, 2.0 * renderer.GetObjectRenderer().Scale.Y);
			Vector2 b2 = new Vector2(_radius * renderer.GetObjectRenderer().Scale.X, 2.0 * renderer.GetObjectRenderer().Scale.Y);
			obj3.RenderLine(colour, a2, b2, 1.0);
		}
		else
		{
			I2dRenderer obj4 = obj1;
			Colour colour1 = new Colour(byte.MaxValue, byte.MaxValue, 0);
			Vector2 scale = renderer.GetObjectRenderer().Scale;
			double x5 = -2.0 * scale.X;
			double num1 = -_radius;
			scale = renderer.GetObjectRenderer().Scale;
			double y3 = scale.Y;
			double y4 = num1 * y3;
			Vector2 a3 = new Vector2(x5, y4);
			scale = renderer.GetObjectRenderer().Scale;
			double x6 = -2.0 * scale.X;
			double radius1 = _radius;
			scale = renderer.GetObjectRenderer().Scale;
			double y5 = scale.Y;
			double y6 = radius1 * y5;
			Vector2 b3 = new Vector2(x6, y6);
			obj4.RenderLine(colour1, a3, b3, 1.0);
			I2dRenderer obj5 = obj1;
			Colour white = Colours.White;
			double num2 = -_radius;
			scale = renderer.GetObjectRenderer().Scale;
			double y7 = scale.Y;
			Vector2 a4 = new Vector2(0.0, num2 * y7);
			double radius2 = _radius;
			scale = renderer.GetObjectRenderer().Scale;
			double y8 = scale.Y;
			Vector2 b4 = new Vector2(0.0, radius2 * y8);
			obj5.RenderLine(white, a4, b4, 1.0);
			I2dRenderer obj6 = obj1;
			Colour colour2 = new Colour(byte.MaxValue, byte.MaxValue, 0);
			scale = renderer.GetObjectRenderer().Scale;
			double x7 = 2.0 * scale.X;
			double num3 = -_radius;
			scale = renderer.GetObjectRenderer().Scale;
			double y9 = scale.Y;
			double y10 = num3 * y9;
			Vector2 a5 = new Vector2(x7, y10);
			scale = renderer.GetObjectRenderer().Scale;
			double x8 = 2.0 * scale.X;
			double radius3 = _radius;
			scale = renderer.GetObjectRenderer().Scale;
			double y11 = scale.Y;
			double y12 = radius3 * y11;
			Vector2 b5 = new Vector2(x8, y12);
			obj6.RenderLine(colour2, a5, b5, 1.0);
		}
	}
}
