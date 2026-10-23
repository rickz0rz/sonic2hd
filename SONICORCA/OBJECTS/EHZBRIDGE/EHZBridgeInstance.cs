// Decompiled with JetBrains decompiler
// Type: SONICORCA.OBJECTS.EHZBRIDGE.EHZBridgeInstance
// Assembly: S2HD, Version=2.0.1012.10521, Culture=neutral, PublicKeyToken=null
// MVID: 18631A0F-16CF-4E18-8563-1EC5E54750D6
// Assembly location: C:\Users\ricky\Downloads\S2HD_2.0.1012-rc2\S2HD_2.0.1012-rc2\S2HD.exe

using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace SONICORCA.OBJECTS.EHZBRIDGE;

public class EHZBridgeInstance : ActiveObject
{
	private AnimationInstance _logAnimation;
	private int _logs = 2;
	private Vector2i[] _logPositions;
	private bool[] _depressedLogs;

	private int TotalWidth => _logs * 64 /*0x40*/;

	public int Logs
	{
		get => _logs;
		set => _logs = value;
	}

	protected override void OnStart()
	{
		_logAnimation = new AnimationInstance(ResourceTree, Type.GetAbsolutePath("/ANIGROUP"));
		_logAnimation.Index = 1;
		_logPositions = new Vector2i[_logs];
		int x = -(TotalWidth / 2);
		for (int index = 0; index < _logs; ++index)
		{
			_logPositions[index] = new Vector2i(x, 0);
			x += 64 /*0x40*/;
		}
		_depressedLogs = new bool[_logs];
		DesignBounds = new Rectanglei(-TotalWidth / 2 - 32 /*0x20*/, -32, TotalWidth, 64 /*0x40*/);
		CollisionVectors = _logPositions.Select<Vector2i, CollisionVector>(log => new CollisionVector(this, new Vector2i(log.X - 32 /*0x20*/, log.Y - 32 /*0x20*/), new Vector2i(log.X + 32 /*0x20*/, log.Y - 32 /*0x20*/), flags: CollisionFlags.Conveyor | CollisionFlags.NoBalance)).ToArray<CollisionVector>();
	}

	protected override void OnUpdate()
	{
		for (int index = 0; index < _logs; ++index)
			_depressedLogs[index] = false;
		double num1 = Position.X + _logPositions[1].X - 32 /*0x20*/;
		Vector2i position = Position;
		double num2 = position.X + _logPositions[_logs - 2].X + 32 /*0x20*/;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this)
			{
				position = character.Position;
				if (position.X >= num1)
				{
					position = character.Position;
					if (position.X < num2)
					{
						position = character.Position;
						int x = position.X;
						position = Position;
						int num3 = position.X + (_logPositions[0].X - 32 /*0x20*/);
						int index = (int)((x - num3) / 64.0);
						if (index >= 0 && index < _logs)
							_depressedLogs[index] = true;
					}
				}
			}
		}
		int logIndex = -1;
		for (int index = 0; index < _logs; ++index)
		{
			if (_depressedLogs[index] && Math.Abs(_logs / 2 - index) < Math.Abs(_logs / 2 - logIndex))
				logIndex = index;
		}
		if (logIndex != -1)
		{
			DepressLogs(logIndex);
		}
		else
		{
			for (int index = 0; index < _logs; ++index)
				_logPositions[index].Y = Math.Max(0, _logPositions[index].Y - 4);
		}
		for (int index = 0; index < _logs; ++index)
		{
			Vector2i logPosition = _logPositions[index];
			CollisionVector collisionVector = CollisionVectors[index];
			collisionVector.RelativeA = new Vector2i(logPosition.X - 32 /*0x20*/, logPosition.Y - 32 /*0x20*/);
			collisionVector.RelativeB = new Vector2i(logPosition.X + 32 /*0x20*/, logPosition.Y - 32 /*0x20*/);
		}
		RegisterCollisionUpdate();
		int count = Level.Map.CollisionPathLayers.Count;
		for (int index = 0; index < _logs; ++index)
		{
			CollisionVector collisionVector = CollisionVectors[index];
			for (int path = 0; path < count; ++path)
			{
				if (index > 0)
					collisionVector.SetConnectionA(path, CollisionVectors[index - 1]);
				if (index < _logs - 1)
					collisionVector.SetConnectionB(path, CollisionVectors[index + 1]);
			}
		}
	}

	private void DepressLogs(int logIndex)
	{
		int num = (int)(Math.Sin(logIndex / (double)_logs * Math.PI) * 40.0);
		for (int i = 0; i < logIndex; ++i)
		{
			int val1 = (int)(Math.Sin(i / (double)logIndex * (Math.PI / 2.0)) * 40.0);
			UpdateLogY(i, Math.Min(val1, num));
		}
		UpdateLogY(logIndex, num);
		for (int i = logIndex + 1; i < _logs; ++i)
		{
			int val1 = (int)(Math.Sin(Math.PI / 2.0 + (i - logIndex) / (double)(_logs - logIndex - 1) * (Math.PI / 2.0)) * 40.0);
			UpdateLogY(i, Math.Min(val1, num));
		}
	}

	private void UpdateLogY(int i, int depress)
	{
		int y = _logPositions[i].Y;
		int num;
		if (y > depress)
		{
			num = y - 4;
			if (num < depress)
				num = depress;
		}
		else
		{
			num = y + 4;
			if (num > depress)
				num = depress;
		}
		_logPositions[i].Y = num;
	}

	protected override void OnAnimate() => _logAnimation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		for (int index = 0; index < _logs; ++index)
			objectRenderer.Render(_logAnimation, (Vector2)_logPositions[index]);
	}
}
