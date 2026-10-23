using System;
using System.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Objects;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SONICORCA.OBJECTS.EHZBRIDGE;

/// <summary>
/// A bridge of logs in Emerald Hill Zone. Each log has its own collision vector.
/// When a character stands on the bridge, the logs bend down in a curve with the lowest point under the character.
/// </summary>
public class EHZBridgeInstance : ActiveObject
{
	private const int LogWidth = 64;
	private const int HalfLogWidth = LogWidth / 2;
	private const int HalfLogHeight = 32;

	/// <summary>The maximum distance in pixels that a log moves down.</summary>
	private const double MaxDepression = 40.0;

	/// <summary>The distance in pixels that a log moves up or down in one frame.</summary>
	private const int LogSpeed = 4;

	private AnimationInstance _logAnimation;
	private int _logs = 2;

	/// <summary>The centre of each log, relative to the bridge.</summary>
	private Vector2i[] _logPositions;

	private bool[] _depressedLogs;

	private int TotalWidth => _logs * LogWidth;

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
			x += LogWidth;
		}
		_depressedLogs = new bool[_logs];
		DesignBounds = new Rectanglei(-TotalWidth / 2 - HalfLogWidth, -32, TotalWidth, 64);
		CollisionVectors = _logPositions.Select(log => new CollisionVector(this, GetLogLeft(log), GetLogRight(log), flags: CollisionFlags.Conveyor | CollisionFlags.NoBalance)).ToArray();
	}

	protected override void OnUpdate()
	{
		for (int index = 0; index < _logs; ++index)
		{
			_depressedLogs[index] = false;
		}

		// Find the logs that characters stand on. The first and last logs are not included.
		double left = Position.X + _logPositions[1].X - HalfLogWidth;
		double right = Position.X + _logPositions[_logs - 2].X + HalfLogWidth;
		foreach (ICharacter character in Level.ObjectManager.Characters)
		{
			if (character.ObjectLink == this && character.Position.X >= left && character.Position.X < right)
			{
				int characterX = character.Position.X;
				int bridgeLeft = Position.X + (_logPositions[0].X - HalfLogWidth);
				int index = (int)((characterX - bridgeLeft) / 64.0);
				if (index >= 0 && index < _logs)
				{
					_depressedLogs[index] = true;
				}
			}
		}

		// Of the logs that characters stand on, use the one nearest to the centre.
		int logIndex = -1;
		for (int index = 0; index < _logs; ++index)
		{
			if (_depressedLogs[index] && Math.Abs(_logs / 2 - index) < Math.Abs(_logs / 2 - logIndex))
			{
				logIndex = index;
			}
		}
		if (logIndex != -1)
		{
			DepressLogs(logIndex);
		}
		else
		{
			for (int index = 0; index < _logs; ++index)
			{
				_logPositions[index].Y = Math.Max(0, _logPositions[index].Y - LogSpeed);
			}
		}

		for (int index = 0; index < _logs; ++index)
		{
			Vector2i logPosition = _logPositions[index];
			CollisionVector collisionVector = CollisionVectors[index];
			collisionVector.RelativeA = GetLogLeft(logPosition);
			collisionVector.RelativeB = GetLogRight(logPosition);
		}
		RegisterCollisionUpdate();

		// Connect each log to its neighbours on all collision paths, so that characters move smoothly across the bridge.
		int pathCount = Level.Map.CollisionPathLayers.Count;
		for (int index = 0; index < _logs; ++index)
		{
			CollisionVector collisionVector = CollisionVectors[index];
			for (int path = 0; path < pathCount; ++path)
			{
				if (index > 0)
				{
					collisionVector.SetConnectionA(path, CollisionVectors[index - 1]);
				}
				if (index < _logs - 1)
				{
					collisionVector.SetConnectionB(path, CollisionVectors[index + 1]);
				}
			}
		}
	}

	/// <summary>Returns the top left corner of a log.</summary>
	private static Vector2i GetLogLeft(Vector2i logPosition)
	{
		return new Vector2i(logPosition.X - HalfLogWidth, logPosition.Y - HalfLogHeight);
	}

	/// <summary>Returns the top right corner of a log.</summary>
	private static Vector2i GetLogRight(Vector2i logPosition)
	{
		return new Vector2i(logPosition.X + HalfLogWidth, logPosition.Y - HalfLogHeight);
	}

	/// <summary>
	/// Moves the logs towards a curve with the lowest point at <paramref name="logIndex"/>.
	/// The depth at that point is larger near the centre of the bridge.
	/// </summary>
	private void DepressLogs(int logIndex)
	{
		int depression = (int)(Math.Sin(logIndex / (double)_logs * Math.PI) * MaxDepression);
		for (int i = 0; i < logIndex; ++i)
		{
			int curveDepression = (int)(Math.Sin(i / (double)logIndex * (Math.PI / 2.0)) * MaxDepression);
			UpdateLogY(i, Math.Min(curveDepression, depression));
		}
		UpdateLogY(logIndex, depression);
		for (int i = logIndex + 1; i < _logs; ++i)
		{
			int curveDepression = (int)(Math.Sin(Math.PI / 2.0 + (i - logIndex) / (double)(_logs - logIndex - 1) * (Math.PI / 2.0)) * MaxDepression);
			UpdateLogY(i, Math.Min(curveDepression, depression));
		}
	}

	/// <summary>Moves log <paramref name="i"/> towards the Y position <paramref name="depress"/>, by not more than the log speed.</summary>
	private void UpdateLogY(int i, int depress)
	{
		int y = _logPositions[i].Y;
		int newY;
		if (y > depress)
		{
			newY = y - LogSpeed;
			if (newY < depress)
			{
				newY = depress;
			}
		}
		else
		{
			newY = y + LogSpeed;
			if (newY > depress)
			{
				newY = depress;
			}
		}
		_logPositions[i].Y = newY;
	}

	protected override void OnAnimate() => _logAnimation.Animate();

	protected override void OnDraw(Renderer renderer, LayerViewOptions viewOptions)
	{
		IObjectRenderer objectRenderer = renderer.GetObjectRenderer();
		for (int index = 0; index < _logs; ++index)
		{
			objectRenderer.Render(_logAnimation, (Vector2)_logPositions[index]);
		}
	}
}
