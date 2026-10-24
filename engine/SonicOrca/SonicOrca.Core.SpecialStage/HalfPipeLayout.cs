using System;
using System.Collections.Generic;

namespace SonicOrca.Core.SpecialStage;

/// <summary>The kind of an object in the half-pipe.</summary>
public enum HalfPipeObjectKind
{
	Ring,
	Bomb
}

/// <summary>
/// An object on the surface of the half-pipe. <see cref="Angle"/> is in radians from the bottom of the pipe:
/// negative to the left, positive to the right.
/// </summary>
public sealed class HalfPipeObject
{
	public HalfPipeObjectKind Kind { get; }

	public int TrackNode { get; }

	public double Angle { get; }

	/// <summary>True after the player collected the ring or hit the bomb.</summary>
	public bool Used { get; set; }

	/// <summary>The ticks since the object was used, for the sparkle or the explosion.</summary>
	public int UsedTicks { get; set; }

	public HalfPipeObject(HalfPipeObjectKind kind, int trackNode, double angle)
	{
		Kind = kind;
		TrackNode = trackNode;
		Angle = angle;
	}
}

/// <summary>A point on the track where the player must have collected a number of rings.</summary>
public readonly record struct HalfPipeCheckpoint(int TrackNode, int RingGoal);

/// <summary>
/// Makes the objects and the checkpoints of a half-pipe stage. As in Sonic 2, a stage has three sections, and the
/// checkpoint at the end of each section needs a total number of rings. The objects repeat a fixed list of
/// patterns, one for each segment, so the stage is the same each time. The original layouts are not in the game
/// data. A data file or the level editor can replace these patterns later.
/// </summary>
public static class HalfPipeLayout
{
	public const int StageCount = 7;

	public const int CheckpointCount = 3;

	private const double Degree = Math.PI / 180.0;

	/// <summary>The number of segments at the start without objects, so the player can get ready.</summary>
	private const int EmptySegmentsAtStart = 2;

	/// <summary>
	/// The total rings needed at checkpoints 1, 2 and 3 of each Sonic 2 special stage, for one character. The
	/// ring counts continue from one section to the next.
	/// </summary>
	private static readonly int[,] SoloRingQuotas =
	{
		{ 30, 70, 130 },
		{ 50, 100, 140 },
		{ 50, 110, 160 },
		{ 40, 110, 150 },
		{ 50, 90, 160 },
		{ 80, 140, 210 },
		{ 100, 150, 190 },
	};

	/// <summary>The same as <see cref="SoloRingQuotas"/>, for Sonic and Tails together.</summary>
	private static readonly int[,] DuoRingQuotas =
	{
		{ 40, 80, 140 },
		{ 50, 100, 140 },
		{ 60, 110, 160 },
		{ 40, 110, 150 },
		{ 55, 110, 200 },
		{ 80, 140, 220 },
		{ 100, 190, 210 },
	};

	private delegate void Pattern(List<HalfPipeObject> objects, int firstNode, int nodeCount);

	private static readonly Pattern[] Patterns =
	{
		CentreLine,
		RowAcross,
		SweepRight,
		CentreLineWithBombs,
		SweepLeft,
		BombsWithGaps,
	};

	/// <param name="stage">The special stage number, from 0 to 6. It selects the ring quotas.</param>
	/// <param name="duo">True when Sonic and Tails play together, which needs more rings in some stages.</param>
	public static (List<HalfPipeObject> Objects, List<HalfPipeCheckpoint> Checkpoints) Create(
		int segmentCount, int nodesPerSegment, int stage, bool duo)
	{
		if (stage < 0 || stage >= StageCount)
		{
			throw new ArgumentOutOfRangeException(nameof(stage));
		}
		var objects = new List<HalfPipeObject>();
		for (int segment = EmptySegmentsAtStart; segment < segmentCount; segment++)
		{
			Patterns[segment % Patterns.Length](objects, segment * nodesPerSegment, nodesPerSegment);
		}

		var checkpoints = new List<HalfPipeCheckpoint>();
		int totalNodes = segmentCount * nodesPerSegment;
		int[,] quotas = duo ? DuoRingQuotas : SoloRingQuotas;
		for (int i = 0; i < CheckpointCount; i++)
		{
			int node = totalNodes * (i + 1) / CheckpointCount - 1;
			checkpoints.Add(new HalfPipeCheckpoint(node, quotas[stage, i]));
		}
		return (objects, checkpoints);
	}

	private static void CentreLine(List<HalfPipeObject> objects, int firstNode, int nodeCount)
	{
		for (int node = 4; node < nodeCount - 4; node += 3)
		{
			objects.Add(new HalfPipeObject(HalfPipeObjectKind.Ring, firstNode + node, 0.0));
		}
	}

	private static void RowAcross(List<HalfPipeObject> objects, int firstNode, int nodeCount)
	{
		foreach (int node in new[] { nodeCount / 3, 2 * nodeCount / 3 })
		{
			for (int i = -2; i <= 2; i++)
			{
				objects.Add(new HalfPipeObject(HalfPipeObjectKind.Ring, firstNode + node, i * 20 * Degree));
			}
		}
	}

	private static void SweepRight(List<HalfPipeObject> objects, int firstNode, int nodeCount)
	{
		for (int i = 0; i < 8; i++)
		{
			objects.Add(new HalfPipeObject(HalfPipeObjectKind.Ring, firstNode + 4 + i * 3, (-50 + i * 14) * Degree));
		}
	}

	private static void SweepLeft(List<HalfPipeObject> objects, int firstNode, int nodeCount)
	{
		for (int i = 0; i < 8; i++)
		{
			objects.Add(new HalfPipeObject(HalfPipeObjectKind.Ring, firstNode + 4 + i * 3, (50 - i * 14) * Degree));
		}
	}

	private static void CentreLineWithBombs(List<HalfPipeObject> objects, int firstNode, int nodeCount)
	{
		for (int node = 4; node < nodeCount - 4; node += 3)
		{
			objects.Add(new HalfPipeObject(HalfPipeObjectKind.Ring, firstNode + node, 0.0));
		}
		objects.Add(new HalfPipeObject(HalfPipeObjectKind.Bomb, firstNode + nodeCount / 2, -35 * Degree));
		objects.Add(new HalfPipeObject(HalfPipeObjectKind.Bomb, firstNode + nodeCount / 2, 35 * Degree));
	}

	private static void BombsWithGaps(List<HalfPipeObject> objects, int firstNode, int nodeCount)
	{
		// A row of bombs with a gap on the left, then a row with a gap on the right. Rings show the way.
		int first = nodeCount / 3;
		int second = 2 * nodeCount / 3;
		for (int i = -2; i <= 2; i++)
		{
			double angle = i * 20 * Degree;
			objects.Add(new HalfPipeObject(i == -2 ? HalfPipeObjectKind.Ring : HalfPipeObjectKind.Bomb, firstNode + first, angle));
			objects.Add(new HalfPipeObject(i == 2 ? HalfPipeObjectKind.Ring : HalfPipeObjectKind.Bomb, firstNode + second, angle));
		}
	}
}
