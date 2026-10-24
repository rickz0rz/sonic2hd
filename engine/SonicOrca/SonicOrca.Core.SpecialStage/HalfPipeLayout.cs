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
/// An object on the surface of the half-pipe. <see cref="OriginalAngle"/> uses the units of the original game ($40 =
/// bottom, up toward the left). <see cref="Angle"/> is the same angle in radians from the bottom of the pipe:
/// negative to the left, positive to the right.
/// </summary>
public sealed class HalfPipeObject
{
	public HalfPipeObjectKind Kind { get; }

	public int TrackNode { get; }

	public int OriginalAngle { get; }

	public double Angle => HalfPipePlayer.ToRadiansFromBottom(OriginalAngle);

	/// <summary>True after the player collected the ring or hit the bomb.</summary>
	public bool Used { get; set; }

	/// <summary>The ticks since the object was used, for the sparkle or the explosion.</summary>
	public int UsedTicks { get; set; }

	public HalfPipeObject(HalfPipeObjectKind kind, int trackNode, int originalAngle)
	{
		Kind = kind;
		TrackNode = trackNode;
		OriginalAngle = originalAngle & 0xFF;
	}
}

/// <summary>A point on the track where the player must have collected a number of rings.</summary>
public readonly record struct HalfPipeCheckpoint(int TrackNode, int RingGoal);

/// <summary>
/// The ring quotas of the Sonic 2 special stages, and a generated stage for when the original layouts are not
/// installed. As in Sonic 2, a stage has two checkpoints and then the emerald, and each needs a total number of rings.
/// </summary>
public static class HalfPipeLayout
{
	public const int StageCount = 7;

	public const int CheckpointCount = 3;

	/// <summary>
	/// The total rings needed at checkpoints 1, 2 and at the emerald of each Sonic 2 special stage, for one
	/// character. The ring counts continue from one section to the next. From SpecialStage_RingReq_Alone in the
	/// Sonic 2 disassembly (s2disasm), which also has an unused fourth value for each stage.
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

	/// <summary>
	/// The same as <see cref="SoloRingQuotas"/>, for Sonic and Tails together. From SpecialStage_RingReq_Team.
	/// </summary>
	private static readonly int[,] DuoRingQuotas =
	{
		{ 40, 80, 140 },
		{ 50, 100, 140 },
		{ 60, 110, 160 },
		{ 40, 100, 150 },
		{ 55, 110, 200 },
		{ 80, 140, 220 },
		{ 100, 190, 210 },
	};

	// Original angle units: $40 is the bottom of the pipe, and the angle goes up toward the left
	private const int Bottom = 0x40;

	private const int Step = 0x0C;

	/// <param name="stage">The special stage number, from 0 to 6.</param>
	/// <param name="duo">True when Sonic and Tails play together, which needs more rings in some stages.</param>
	public static int[] GetRingQuotas(int stage, bool duo)
	{
		if (stage < 0 || stage >= StageCount)
		{
			throw new ArgumentOutOfRangeException(nameof(stage));
		}
		int[,] quotas = duo ? DuoRingQuotas : SoloRingQuotas;
		return new[] { quotas[stage, 0], quotas[stage, 1], quotas[stage, 2] };
	}

	/// <summary>
	/// Makes a stage in the style of the originals: straights with turns between them, groups of rings and bombs,
	/// and the markers in the original order. It has enough rings for the quotas of every stage.
	/// </summary>
	public static HalfPipeStageData CreateGenerated()
	{
		var stage = new HalfPipeStageData();
		HalfPipeSegmentType[] block =
		{
			HalfPipeSegmentType.Straight, HalfPipeSegmentType.Straight, HalfPipeSegmentType.StraightThenTurn,
			HalfPipeSegmentType.TurnThenStraight, HalfPipeSegmentType.Straight, HalfPipeSegmentType.StraightThenTurn,
			HalfPipeSegmentType.TurnThenRise, HalfPipeSegmentType.TurnThenStraight,
		};
		var markers = new Dictionary<int, HalfPipeMarker>
		{
			[13] = HalfPipeMarker.RingsToGo, [15] = HalfPipeMarker.Checkpoint,
			[29] = HalfPipeMarker.RingsToGo, [31] = HalfPipeMarker.Checkpoint,
			[45] = HalfPipeMarker.RingsToGo, [47] = HalfPipeMarker.Emerald,
		};
		const int SegmentCount = 52;
		for (int i = 0; i < SegmentCount; i++)
		{
			HalfPipeSegmentType type = block[i % block.Length];
			stage.Segments.Add(new HalfPipeSegment
			{
				Type = type,
				// Turn the other way in every second block
				Flip = i / block.Length % 2 == 1,
				Objects = i < 1 ? new List<HalfPipeObjectEntry>() : CreateGroup(i, HalfPipeTrackBuilder.GetLength(block[(i + 1) % block.Length])),
				Marker = markers.TryGetValue(i, out HalfPipeMarker marker) ? marker : HalfPipeMarker.None,
			});
		}
		return stage;
	}

	/// <summary>Makes the objects of one group. They reach the player during the next segment.</summary>
	private static List<HalfPipeObjectEntry> CreateGroup(int segment, int nextLength)
	{
		var group = new List<HalfPipeObjectEntry>();
		void Ring(int distance, int angle) => group.Add(new HalfPipeObjectEntry(HalfPipeObjectKind.Ring, distance, angle & 0xFF));
		void Bomb(int distance, int angle) => group.Add(new HalfPipeObjectEntry(HalfPipeObjectKind.Bomb, distance, angle & 0xFF));
		int length = Math.Min(nextLength, 14);
		switch (segment % 5)
		{
			case 0:
				// A line along the bottom
				for (int d = 1; d < length; d += 2)
				{
					Ring(d, Bottom);
				}
				break;
			case 1:
				// A sweep from the right side to the left side
				for (int d = 1, i = 0; d < length; d += 2, i++)
				{
					Ring(d, Bottom - 3 * Step + i * Step);
				}
				break;
			case 2:
				// Two lines, left and right of a line of bombs
				for (int d = 2; d < length; d += 3)
				{
					Ring(d, Bottom - 2 * Step);
					Ring(d, Bottom + 2 * Step);
					Bomb(d, Bottom);
				}
				break;
			case 3:
				// A sweep from the left side to the right side
				for (int d = 1, i = 0; d < length; d += 2, i++)
				{
					Ring(d, Bottom + 3 * Step - i * Step);
				}
				break;
			default:
				// Rows across the pipe, with a gap of bombs
				foreach (int d in new[] { 2, length / 2 + 2 })
				{
					for (int i = -2; i <= 2; i++)
					{
						if (i == 0)
						{
							Bomb(d, Bottom);
						}
						else
						{
							Ring(d, Bottom + i * Step);
						}
					}
				}
				break;
		}
		return group;
	}
}
