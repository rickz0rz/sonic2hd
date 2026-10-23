// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.HTZRISINGGROUP.HTZRisingGroupInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System.Collections.Generic;
using SonicOrca.Core;
using SonicOrca.Geometry;

#nullable disable
namespace SONICORCA.OBJECTS.HTZRISINGGROUP;

/// <summary>
/// The controller of the Hill Top Zone earthquake. While the camera is between the level markers of the earthquake
/// area, it moves the objects in its three slots up and down, and shakes the screen while they move.
/// </summary>
public class HTZRisingGroupInstance : ActiveObject
{
	/// <summary>The camera is to the left of the earthquake area.</summary>
	private const int StateBeforeArea = 0;

	/// <summary>The camera is in the earthquake area (act 1, or the upper route of act 2).</summary>
	private const int StateInArea = 1;

	/// <summary>The camera is to the right of the earthquake area.</summary>
	private const int StateAfterArea = 2;

	/// <summary>The camera is in the earthquake area on the lower route of act 2.</summary>
	private const int StateInLowerArea = 4;

	/// <summary>The camera is to the right of the earthquake area on the lower route of act 2.</summary>
	private const int StateAfterLowerArea = 5;

	/// <summary>The distance that the group moves each time it moves.</summary>
	private const int MoveStep = 4;

	/// <summary>The group moves one time in this number of ticks.</summary>
	private const int MoveInterval = 4;

	/// <summary>The number of ticks that the group waits at the top and at the bottom.</summary>
	private const int PauseTicks = 120;

	private Vector2i _size = new Vector2i(2048, 512);
	private List<ActiveObject> _groupMembers = new List<ActiveObject>();
	private Level _level;
	private int _state;
	private int _earthquakeY;
	private int _earthquakeTimer;

	/// <summary>False when the group moves to the upper bound, true when it moves to the lower bound.</summary>
	private bool _earthquakeDirection;

	private int _earthquakeUpperBound;
	private int _earthquakeLowerBound;

	public ActiveObject SlotA { get; set; }

	public ActiveObject SlotB { get; set; }

	public ActiveObject SlotC { get; set; }

	public Vector2i Size
	{
		get => _size;
		set
		{
			_size = value;
			DesignBounds = new Rectanglei(-(_size.X / 2), -(_size.Y / 2), _size.X, _size.Y);
		}
	}

	public HTZRisingGroupInstance()
	{
		DesignBounds = new Rectanglei(-(_size.X / 2), -(_size.Y / 2), _size.X, _size.Y);
	}

	protected override void OnStart()
	{
		_level = Level;
		UpdateActEvents();
	}

	protected override void OnStop()
	{
		if (SlotA != null)
			SlotA.LockLifetime = false;
		if (SlotB != null)
			SlotB.LockLifetime = false;
		if (SlotC != null)
			SlotC.LockLifetime = false;
	}

	protected override void OnUpdate()
	{
		AddGroupMember(SlotA);
		AddGroupMember(SlotB);
		AddGroupMember(SlotC);
		UpdateActEvents();
	}

	/// <summary>
	/// Adds the object in a slot to the group the first time it is seen, and moves it to the current earthquake
	/// position.
	/// </summary>
	private void AddGroupMember(ActiveObject slot)
	{
		if (slot == null || _groupMembers.Contains(slot))
			return;
		slot.LockLifetime = true;
		_groupMembers.Add(slot);
		slot.Position = new Vector2i(slot.Position.X, slot.Position.Y + _earthquakeUpperBound - _earthquakeY);
	}

	private void UpdateActEvents()
	{
		switch (_level.CurrentAct)
		{
			case 1:
				Act1Events();
				break;
			case 2:
				Act2Events();
				break;
		}
	}

	private void Act1Events()
	{
		Rectanglei bounds = (Rectanglei)_level.Camera.Bounds;
		LevelMarker leftReset = _level.GetMarker("eq_1_left_reset");
		LevelMarker leftBegin = _level.GetMarker("eq_1_left_begin");
		LevelMarker rightBegin = _level.GetMarker("eq_1_right_begin");
		LevelMarker rightReset = _level.GetMarker("eq_1_right_reset");
		switch (_state)
		{
			case StateBeforeArea:
				_level.EarthquakeActive = false;
				if (bounds.X >= leftReset.Position.X)
				{
					_earthquakeUpperBound = 1280;
					_earthquakeLowerBound = 896;
					_earthquakeY = _earthquakeUpperBound;
					_earthquakeTimer = 0;
					_state = StateInArea;
				}
				break;
			case StateInArea:
				UpdateInArea(bounds, leftReset, leftBegin, rightBegin, rightReset, StateAfterArea);
				break;
			case StateAfterArea:
				UpdateAfterArea(bounds, rightReset, StateInArea);
				break;
		}
		UpdateGroupPositions();
	}

	private void Act2Events()
	{
		Rectanglei bounds = (Rectanglei)_level.Camera.Bounds;
		LevelMarker split = _level.GetMarker("eq_2_split");
		LevelMarker leftReset = _level.GetMarker("eq_2a_left_reset");
		LevelMarker leftBegin = _level.GetMarker("eq_2a_left_begin");
		LevelMarker rightBegin = _level.GetMarker("eq_2a_right_begin");
		LevelMarker rightReset = _level.GetMarker("eq_2a_right_reset");
		switch (_state)
		{
			case StateBeforeArea:
				_level.EarthquakeActive = false;
				if (bounds.X >= leftReset.Position.X)
				{
					// The split marker separates the upper route from the lower route.
					if (bounds.Y < split.Position.Y)
					{
						_earthquakeUpperBound = 3328;
						_earthquakeLowerBound = 0;
						_earthquakeY = _earthquakeUpperBound;
						_state = StateInArea;
						break;
					}
					_earthquakeUpperBound = 3072;
					_earthquakeLowerBound = 0;
					_earthquakeY = _earthquakeUpperBound;
					_state = StateInLowerArea;
				}
				break;
			case StateInArea:
				UpdateInArea(bounds, leftReset, leftBegin, rightBegin, rightReset, StateAfterArea);
				break;
			case StateAfterArea:
				UpdateAfterArea(bounds, rightReset, StateInArea);
				break;
			case StateInLowerArea:
				UpdateInArea(bounds, leftReset, leftBegin, rightBegin, rightReset, StateAfterLowerArea);
				break;
			case StateAfterLowerArea:
				UpdateAfterArea(bounds, rightReset, StateInLowerArea);
				break;
		}
		UpdateGroupPositions();
	}

	/// <summary>
	/// Moves the earthquake while the camera is between the begin markers, and changes the state when the camera
	/// passes a reset marker.
	/// </summary>
	private void UpdateInArea(Rectanglei bounds, LevelMarker leftReset, LevelMarker leftBegin, LevelMarker rightBegin, LevelMarker rightReset, int stateAfterArea)
	{
		if (bounds.X >= leftBegin.Position.X && bounds.X < rightBegin.Position.X)
			UpdateEarthquake();
		if (bounds.X < leftReset.Position.X)
		{
			_earthquakeDirection = false;
			_state = StateBeforeArea;
		}
		else if (bounds.X >= rightReset.Position.X)
		{
			_earthquakeDirection = false;
			_state = stateAfterArea;
		}
	}

	/// <summary>Goes back to the area state, with the group at the top, when the camera moves back past the right reset marker.</summary>
	private void UpdateAfterArea(Rectanglei bounds, LevelMarker rightReset, int stateInArea)
	{
		_level.EarthquakeActive = false;
		if (bounds.X < rightReset.Position.X)
		{
			_earthquakeY = _earthquakeUpperBound;
			_earthquakeTimer = 0;
			_state = stateInArea;
		}
	}

	/// <summary>
	/// Moves the earthquake position one step toward the current bound. At the bound, waits and then turns around.
	/// </summary>
	private void UpdateEarthquake()
	{
		int targetY = _earthquakeDirection ? _earthquakeLowerBound : _earthquakeUpperBound;
		if (_earthquakeY == targetY)
		{
			--_earthquakeTimer;
			if (_earthquakeTimer < 0)
			{
				_earthquakeTimer = PauseTicks;
				_earthquakeDirection = !_earthquakeDirection;
			}
			_level.EarthquakeActive = false;
		}
		else if (_level.Ticks % MoveInterval == 0)
		{
			if (_earthquakeDirection)
				_earthquakeY -= MoveStep;
			else
				_earthquakeY += MoveStep;
			_level.EarthquakeActive = true;
		}
	}

	private void UpdateGroupPositions()
	{
		if (!_level.EarthquakeActive)
			return;
		int deltaY = _earthquakeDirection ? -MoveStep : MoveStep;
		if (_level.Ticks % MoveInterval != 0)
			return;
		foreach (ActiveObject groupMember in _groupMembers)
		{
			Vector2i position = groupMember.Position;
			position.Y += deltaY;
			groupMember.Position = position;
		}
	}
}
