using System;
using System.Collections.Generic;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core.SpecialStage;

/// <summary>A point on the track. Yaw and pitch are in radians.</summary>
public readonly record struct HalfPipeTrackNode(Vector3 Position, double Yaw, double Pitch);

/// <summary>A stage that is ready to play: the track, the objects on it, and the checkpoints.</summary>
public sealed class HalfPipeTrack
{
	public List<HalfPipeTrackNode> Nodes { get; } = new List<HalfPipeTrackNode>();

	public List<HalfPipeObject> Objects { get; } = new List<HalfPipeObject>();

	/// <summary>The three ring tests: two checkpoints, then the emerald.</summary>
	public List<HalfPipeCheckpoint> Checkpoints { get; } = new List<HalfPipeCheckpoint>();

	/// <summary>The track nodes where the "rings to go" counter starts to show.</summary>
	public List<int> RingsToGoNodes { get; } = new List<int>();

	/// <summary>The track node where the emerald phase starts. From here, only START has an effect.</summary>
	public int EmeraldNode { get; set; } = -1;

	/// <summary>The rings that come before the emerald, for the perfect bonus.</summary>
	public int RingCount => Objects.Count(o => o.Kind == HalfPipeObjectKind.Ring);
}

/// <summary>
/// Makes the track of a special stage from its segments, with the timing of Sonic 2: one track frame every 5 ticks,
/// and 5 track nodes for each track frame, so the player moves one node each tick.
/// </summary>
public static class HalfPipeTrackBuilder
{
	public const int TicksPerTrackFrame = 5;

	public const int NodesPerTrackFrame = TicksPerTrackFrame;

	/// <summary>The distance between two track nodes, in track units. The pipe radius is 4.</summary>
	public const double NodeSpacing = 1.0;

	/// <summary>The turn rate in a full turn, in degrees for each track frame.</summary>
	private const double TurnDegreesPerFrame = 7.0;

	/// <summary>The height that a rise climbs or a drop falls, in track units.</summary>
	private const double HillHeight = 12.0;

	// The track frames of each part of a segment, from Ani_SSTrack_Len and the animation lists in s2.asm
	private const int TurningFrames = 7;

	private const int RiseOrDropFrames = 17;

	private const int ExitTurnFrames = 5;

	private const int StraightFrames = 16;

	private const int StraightBeforeTurnFrames = 4;

	private const int EnterTurnFrames = 7;

	/// <summary>The track frames of the rainbow before a checkpoint tests the rings (s2.asm Obj5A).</summary>
	private const int RainbowFrames = 9;

	/// <summary>The ticks after the emerald marker until the emerald appears, and then until its ring test.</summary>
	private const int EmeraldAppearTicks = 60;

	private const int EmeraldTestTicks = 57;

	/// <summary>Extra track after the last ring test, so the track does not end in view.</summary>
	private const int TrackFramesAfterEnd = 48;

	private enum FrameKind
	{
		Straight,
		EnterTurn,
		Turning,
		ExitTurn,
		Rise,
		Drop
	}

	private readonly record struct TrackFrame(FrameKind Kind, int Index, int Segment, bool LatchesFlip);

	public static int GetLength(HalfPipeSegmentType type) => type switch
	{
		HalfPipeSegmentType.TurnThenRise or HalfPipeSegmentType.TurnThenDrop => TurningFrames + RiseOrDropFrames,
		HalfPipeSegmentType.TurnThenStraight => TurningFrames + ExitTurnFrames,
		HalfPipeSegmentType.Straight => StraightFrames,
		HalfPipeSegmentType.StraightThenTurn => StraightBeforeTurnFrames + EnterTurnFrames,
		_ => throw new ArgumentOutOfRangeException(nameof(type)),
	};

	/// <param name="ringQuotas">The total rings needed at the two checkpoints and at the emerald.</param>
	public static HalfPipeTrack Build(HalfPipeStageData stage, IReadOnlyList<int> ringQuotas)
	{
		var frames = new List<TrackFrame>();
		var segmentStartFrame = new List<int>();
		for (int segment = 0; segment < stage.Segments.Count; segment++)
		{
			segmentStartFrame.Add(frames.Count);
			AddFrames(frames, stage.Segments[segment].Type, segment);
		}
		segmentStartFrame.Add(frames.Count);

		var track = new HalfPipeTrack();
		int lastTestFrame = PlaceObjectsAndMarkers(stage, frames, segmentStartFrame, ringQuotas, track);
		int frameCount = Math.Min(frames.Count, lastTestFrame + TrackFramesAfterEnd);
		while (frames.Count < frameCount)
		{
			frames.Add(new TrackFrame(FrameKind.Straight, frames.Count % 4, stage.Segments.Count - 1, false));
		}
		AddNodes(track, stage, frames.Take(frameCount).ToList());
		return track;
	}

	private static void AddFrames(List<TrackFrame> frames, HalfPipeSegmentType type, int segment)
	{
		void Add(FrameKind kind, int count, Func<int, bool> latches = null)
		{
			for (int i = 0; i < count; i++)
			{
				frames.Add(new TrackFrame(kind, i, segment, latches?.Invoke(i) ?? false));
			}
		}
		// The flip of the current segment takes effect only on some frames: the second frame of each four
		// straight frames (map frame $12), rise frame 13 ($0D) and drop frame 5 ($1A)
		switch (type)
		{
			case HalfPipeSegmentType.TurnThenRise:
				Add(FrameKind.Turning, TurningFrames);
				Add(FrameKind.Rise, RiseOrDropFrames, i => i == 13);
				break;
			case HalfPipeSegmentType.TurnThenDrop:
				Add(FrameKind.Turning, TurningFrames);
				Add(FrameKind.Drop, RiseOrDropFrames, i => i == 5);
				break;
			case HalfPipeSegmentType.TurnThenStraight:
				Add(FrameKind.Turning, TurningFrames);
				Add(FrameKind.ExitTurn, ExitTurnFrames);
				break;
			case HalfPipeSegmentType.Straight:
				Add(FrameKind.Straight, StraightFrames, i => i % 4 == 1);
				break;
			case HalfPipeSegmentType.StraightThenTurn:
				Add(FrameKind.Straight, StraightBeforeTurnFrames, i => i == 1);
				Add(FrameKind.EnterTurn, EnterTurnFrames);
				break;
		}
	}

	/// <returns>The track frame of the last ring test.</returns>
	private static int PlaceObjectsAndMarkers(HalfPipeStageData stage, List<TrackFrame> frames, List<int> segmentStartFrame,
		IReadOnlyList<int> ringQuotas, HalfPipeTrack track)
	{
		int lastTestFrame = frames.Count - 1;
		for (int segment = 0; segment < stage.Segments.Count; segment++)
		{
			HalfPipeSegment data = stage.Segments[segment];
			int start = segmentStartFrame[segment];
			// The game reads the group at the start of a segment. Its objects reach the player during the next
			// segment, about (distance - 1) track frames after it starts.
			int next = segmentStartFrame[segment + 1];
			foreach (HalfPipeObjectEntry entry in data.Objects)
			{
				int frame = next + Math.Max(0, entry.Distance - 1);
				track.Objects.Add(new HalfPipeObject(entry.Kind, frame * NodesPerTrackFrame + NodesPerTrackFrame / 2, ToRadians(entry.Angle)));
			}
			switch (data.Marker)
			{
				case HalfPipeMarker.RingsToGo:
					track.RingsToGoNodes.Add(start * NodesPerTrackFrame);
					break;
				case HalfPipeMarker.Checkpoint when track.Checkpoints.Count < 2:
				{
					// The checkpoint waits for the last of four straight frames, then shows the rainbow
					int waitFrame = frames.FindIndex(start, f => f.Kind == FrameKind.Straight && f.Index % 4 == 3);
					if (waitFrame < 0)
					{
						waitFrame = start;
					}
					int testFrame = waitFrame + RainbowFrames;
					track.Checkpoints.Add(new HalfPipeCheckpoint(testFrame * NodesPerTrackFrame, ringQuotas[track.Checkpoints.Count]));
					break;
				}
				case HalfPipeMarker.Emerald when track.EmeraldNode < 0:
				{
					track.EmeraldNode = start * NodesPerTrackFrame;
					int testNode = track.EmeraldNode + EmeraldAppearTicks + EmeraldTestTicks;
					track.Checkpoints.Add(new HalfPipeCheckpoint(testNode, ringQuotas[2]));
					lastTestFrame = testNode / NodesPerTrackFrame;
					// The groups after the emerald are never reached
					track.Objects.RemoveAll(o => o.TrackNode >= testNode);
					return lastTestFrame;
				}
			}
		}
		return lastTestFrame;
	}

	private static void AddNodes(HalfPipeTrack track, HalfPipeStageData stage, List<TrackFrame> frames)
	{
		double yaw = 0.0;
		bool flip = false;
		Vector3 position = default;
		foreach (TrackFrame frame in frames)
		{
			if (frame.LatchesFlip)
			{
				flip = stage.Segments[frame.Segment].Flip;
			}
			double fullRate = MathX.ToRadians(TurnDegreesPerFrame) * (flip ? -1.0 : 1.0);
			for (int i = 0; i < NodesPerTrackFrame; i++)
			{
				// The fraction of the current part of the segment, for smooth curves
				double t = (frame.Index * NodesPerTrackFrame + i + 0.5) / (PartLength(frame.Kind) * NodesPerTrackFrame);
				double turnRate = frame.Kind switch
				{
					FrameKind.EnterTurn => fullRate * t,
					// Rise and drop segments start with turning frames and keep the curve while they climb or fall
					FrameKind.Turning or FrameKind.Rise or FrameKind.Drop => fullRate,
					FrameKind.ExitTurn => fullRate * (1.0 - t),
					_ => 0.0,
				};
				yaw += turnRate / NodesPerTrackFrame;
				// A rise climbs and a drop falls on a smooth S curve
				double climb = frame.Kind switch
				{
					FrameKind.Rise => HillHeight * (1.0 - Math.Cos(2.0 * Math.PI * t)) / (RiseOrDropFrames * NodesPerTrackFrame),
					FrameKind.Drop => -HillHeight * (1.0 - Math.Cos(2.0 * Math.PI * t)) / (RiseOrDropFrames * NodesPerTrackFrame),
					_ => 0.0,
				};
				position += new Vector3(Math.Sin(yaw) * NodeSpacing, climb, Math.Cos(yaw) * NodeSpacing);
				track.Nodes.Add(new HalfPipeTrackNode(position, yaw, Math.Atan2(climb, NodeSpacing)));
			}
		}
	}

	private static int PartLength(FrameKind kind) => kind switch
	{
		FrameKind.EnterTurn => EnterTurnFrames,
		FrameKind.Turning => TurningFrames,
		FrameKind.ExitTurn => ExitTurnFrames,
		FrameKind.Rise or FrameKind.Drop => RiseOrDropFrames,
		_ => 4,
	};

	/// <summary>Converts an angle in original units ($40 = bottom, up toward the left) to radians from the bottom.</summary>
	public static double ToRadians(int originalAngle)
	{
		int fromBottom = (sbyte)(byte)(0x40 - originalAngle);
		return fromBottom * Math.PI / 128.0;
	}
}
