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

public class HTZRisingGroupInstance : ActiveObject
{
	private Vector2i _size = new Vector2i(2048 /*0x0800*/, 512 /*0x0200*/);
	private List<ActiveObject> _groupMembers = new List<ActiveObject>();
	private Level _level;
	private int _state;
	private int _earthquakeY;
	private int _earthquakeTimer;
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

	protected override void OnStop()
	{
		if (SlotA != null)
			SlotA.LockLifetime = false;
		if (SlotB != null)
			SlotB.LockLifetime = false;
		if (SlotC == null)
			return;
		SlotC.LockLifetime = false;
	}

	protected override void OnUpdate()
	{
		if (SlotA != null && !_groupMembers.Contains(SlotA))
		{
			SlotA.LockLifetime = true;
			_groupMembers.Add(SlotA);
			SlotA.Position = new Vector2i(SlotA.Position.X, SlotA.Position.Y + _earthquakeUpperBound - _earthquakeY);
		}
		if (SlotB != null && !_groupMembers.Contains(SlotB))
		{
			SlotB.LockLifetime = true;
			_groupMembers.Add(SlotB);
			SlotB.Position = new Vector2i(SlotB.Position.X, SlotB.Position.Y + _earthquakeUpperBound - _earthquakeY);
		}
		if (SlotC != null && !_groupMembers.Contains(SlotC))
		{
			SlotC.LockLifetime = true;
			_groupMembers.Add(SlotC);
			SlotC.Position = new Vector2i(SlotC.Position.X, SlotC.Position.Y + _earthquakeUpperBound - _earthquakeY);
		}
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
		LevelMarker marker1 = _level.GetMarker("eq_1_left_reset");
		LevelMarker marker2 = _level.GetMarker("eq_1_left_begin");
		LevelMarker marker3 = _level.GetMarker("eq_1_right_begin");
		LevelMarker marker4 = _level.GetMarker("eq_1_right_reset");
		switch (_state)
		{
			case 0:
				_level.EarthquakeActive = false;
				if (bounds.X >= marker1.Position.X)
				{
					_earthquakeUpperBound = 1280 /*0x0500*/;
					_earthquakeLowerBound = 896;
					_earthquakeY = _earthquakeUpperBound;
					_earthquakeTimer = 0;
					_state = 1;
					break;
				}
				break;
			case 1:
				int x1 = bounds.X;
				Vector2i position = marker2.Position;
				int x2 = position.X;
				if (x1 >= x2)
				{
					int x3 = bounds.X;
					position = marker3.Position;
					int x4 = position.X;
					if (x3 < x4)
					{
						if (_earthquakeDirection)
						{
							if (_earthquakeY == _earthquakeLowerBound)
							{
								--_earthquakeTimer;
								if (_earthquakeTimer < 0)
								{
									_earthquakeTimer = 120;
									_earthquakeDirection = !_earthquakeDirection;
								}
								_level.EarthquakeActive = false;
							}
							else if (_level.Ticks % 4 == 0)
							{
								_earthquakeY -= 4;
								_level.EarthquakeActive = true;
							}
						}
						else if (_earthquakeY == _earthquakeUpperBound)
						{
							--_earthquakeTimer;
							if (_earthquakeTimer < 0)
							{
								_earthquakeTimer = 120;
								_earthquakeDirection = !_earthquakeDirection;
							}
							_level.EarthquakeActive = false;
						}
						else if (_level.Ticks % 4 == 0)
						{
							_earthquakeY += 4;
							_level.EarthquakeActive = true;
						}
					}
				}
				int x5 = bounds.X;
				position = marker1.Position;
				int x6 = position.X;
				if (x5 < x6)
				{
					_earthquakeDirection = false;
					_state = 0;
					break;
				}
				int x7 = bounds.X;
				position = marker4.Position;
				int x8 = position.X;
				if (x7 >= x8)
				{
					_earthquakeDirection = false;
					_state = 2;
					break;
				}
				break;
			case 2:
				_level.EarthquakeActive = false;
				if (bounds.X < marker4.Position.X)
				{
					_earthquakeY = _earthquakeUpperBound;
					_earthquakeTimer = 0;
					_state = 1;
					break;
				}
				break;
		}
		UpdateGroupPositions();
	}

	private void Act2Events()
	{
		Rectanglei bounds = (Rectanglei)_level.Camera.Bounds;
		LevelMarker marker1 = _level.GetMarker("eq_2_split");
		LevelMarker marker2 = _level.GetMarker("eq_2a_left_reset");
		LevelMarker marker3 = _level.GetMarker("eq_2a_left_begin");
		LevelMarker marker4 = _level.GetMarker("eq_2a_right_begin");
		LevelMarker marker5 = _level.GetMarker("eq_2a_right_reset");
		switch (_state)
		{
			case 0:
				_level.EarthquakeActive = false;
				if (bounds.X >= marker2.Position.X)
				{
					if (bounds.Y < marker1.Position.Y)
					{
						_earthquakeUpperBound = 3328 /*0x0D00*/;
						_earthquakeLowerBound = 0;
						_earthquakeY = _earthquakeUpperBound;
						_state = 1;
						break;
					}
					_earthquakeUpperBound = 3072 /*0x0C00*/;
					_earthquakeLowerBound = 0;
					_earthquakeY = _earthquakeUpperBound;
					_state = 4;
					break;
				}
				break;
			case 1:
				int x1 = bounds.X;
				Vector2i position1 = marker3.Position;
				int x2 = position1.X;
				if (x1 >= x2)
				{
					int x3 = bounds.X;
					position1 = marker4.Position;
					int x4 = position1.X;
					if (x3 < x4)
					{
						if (_earthquakeDirection)
						{
							if (_earthquakeY == _earthquakeLowerBound)
							{
								--_earthquakeTimer;
								if (_earthquakeTimer < 0)
								{
									_earthquakeTimer = 120;
									_earthquakeDirection = !_earthquakeDirection;
								}
								_level.EarthquakeActive = false;
							}
							else if (_level.Ticks % 4 == 0)
							{
								_earthquakeY -= 4;
								_level.EarthquakeActive = true;
							}
						}
						else if (_earthquakeY == _earthquakeUpperBound)
						{
							--_earthquakeTimer;
							if (_earthquakeTimer < 0)
							{
								_earthquakeTimer = 120;
								_earthquakeDirection = !_earthquakeDirection;
							}
							_level.EarthquakeActive = false;
						}
						else if (_level.Ticks % 4 == 0)
						{
							_earthquakeY += 4;
							_level.EarthquakeActive = true;
						}
					}
				}
				int x5 = bounds.X;
				position1 = marker2.Position;
				int x6 = position1.X;
				if (x5 < x6)
				{
					_earthquakeDirection = false;
					_state = 0;
					break;
				}
				int x7 = bounds.X;
				position1 = marker5.Position;
				int x8 = position1.X;
				if (x7 >= x8)
				{
					_earthquakeDirection = false;
					_state = 2;
					break;
				}
				break;
			case 2:
				_level.EarthquakeActive = false;
				if (bounds.X < marker5.Position.X)
				{
					_earthquakeY = _earthquakeUpperBound;
					_earthquakeTimer = 0;
					_state = 1;
					break;
				}
				break;
			case 4:
				int x9 = bounds.X;
				Vector2i position2 = marker3.Position;
				int x10 = position2.X;
				if (x9 >= x10)
				{
					int x11 = bounds.X;
					position2 = marker4.Position;
					int x12 = position2.X;
					if (x11 < x12)
					{
						if (_earthquakeDirection)
						{
							if (_earthquakeY == _earthquakeLowerBound)
							{
								--_earthquakeTimer;
								if (_earthquakeTimer < 0)
								{
									_earthquakeTimer = 120;
									_earthquakeDirection = !_earthquakeDirection;
								}
								_level.EarthquakeActive = false;
							}
							else if (_level.Ticks % 4 == 0)
							{
								_earthquakeY -= 4;
								_level.EarthquakeActive = true;
							}
						}
						else if (_earthquakeY == _earthquakeUpperBound)
						{
							--_earthquakeTimer;
							if (_earthquakeTimer < 0)
							{
								_earthquakeTimer = 120;
								_earthquakeDirection = !_earthquakeDirection;
							}
							_level.EarthquakeActive = false;
						}
						else if (_level.Ticks % 4 == 0)
						{
							_earthquakeY += 4;
							_level.EarthquakeActive = true;
						}
					}
				}
				int x13 = bounds.X;
				position2 = marker2.Position;
				int x14 = position2.X;
				if (x13 < x14)
				{
					_earthquakeDirection = false;
					_state = 0;
					break;
				}
				int x15 = bounds.X;
				position2 = marker5.Position;
				int x16 = position2.X;
				if (x15 >= x16)
				{
					_earthquakeDirection = false;
					_state = 5;
					break;
				}
				break;
			case 5:
				_level.EarthquakeActive = false;
				if (bounds.X < marker5.Position.X)
				{
					_earthquakeY = _earthquakeUpperBound;
					_earthquakeTimer = 0;
					_state = 4;
					break;
				}
				break;
		}
		UpdateGroupPositions();
	}

	private void UpdateGroupPositions()
	{
		if (!_level.EarthquakeActive)
			return;
		int num = _earthquakeDirection ? -4 : 4;
		if (_level.Ticks % 4 != 0)
			return;
		foreach (ActiveObject groupMember in _groupMembers)
		{
			Vector2i position = groupMember.Position;
			position.Y += num;
			groupMember.Position = position;
		}
	}
}
