using System;
using System.Linq;
using SonicOrca.Core.SpecialStage;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>The rules of the half-pipe special stage, without the game window.</summary>
public sealed class HalfPipeTests
{
	[Theory]
	[InlineData(0, false, 30, 70, 130)]
	[InlineData(4, false, 50, 90, 160)]
	[InlineData(6, false, 100, 150, 190)]
	// Values from SpecialStage_RingReq_Team in the Sonic 2 disassembly
	[InlineData(0, true, 40, 80, 140)]
	[InlineData(3, true, 40, 100, 150)]
	[InlineData(6, true, 100, 190, 210)]
	public void RingQuotas_MatchSonic2(int stage, bool duo, int first, int second, int third)
	{
		Assert.Equal(new[] { first, second, third }, HalfPipeLayout.GetRingQuotas(stage, duo));
	}

	[Fact]
	public void TrackBuilder_UsesTheOriginalSegmentLengths()
	{
		var stage = new HalfPipeStageData();
		foreach (HalfPipeSegmentType type in Enum.GetValues<HalfPipeSegmentType>())
		{
			stage.Segments.Add(new HalfPipeSegment { Type = type });
		}

		HalfPipeTrack track = HalfPipeTrackBuilder.Build(stage, new[] { 1, 2, 3 });

		// 24 + 24 + 12 + 16 + 11 track frames of 5 nodes each
		Assert.Equal((24 + 24 + 12 + 16 + 11) * 5, track.Nodes.Count);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void GeneratedStage_HasEnoughRingsBeforeEachRingTest(bool duo)
	{
		for (int stage = 0; stage < HalfPipeLayout.StageCount; stage++)
		{
			HalfPipeTrack track = HalfPipeTrackBuilder.Build(HalfPipeLayout.CreateGenerated(), HalfPipeLayout.GetRingQuotas(stage, duo));
			AssertEnoughRings(track, $"Generated stage, quotas of stage {stage + 1}");
		}
	}

	[Fact]
	public void OriginalStages_HaveThreeRingTests_WithEnoughRingsBeforeEach()
	{
		var stages = SpecialStageLayouts.ReadFromDisassembly(SpecialStageImportTests.FindDisassembly());
		for (int stage = 0; stage < stages.Count; stage++)
		{
			HalfPipeTrack track = HalfPipeTrackBuilder.Build(stages[stage], HalfPipeLayout.GetRingQuotas(stage, duo: false));
			Assert.Equal(3, track.Checkpoints.Count);
			Assert.True(track.EmeraldNode > track.Checkpoints[1].TrackNode, $"Stage {stage + 1}: the emerald comes before checkpoint 2.");
			AssertEnoughRings(track, $"Original stage {stage + 1}");
		}
	}

	private static void AssertEnoughRings(HalfPipeTrack track, string name)
	{
		Assert.Equal(3, track.Checkpoints.Count);
		foreach (HalfPipeCheckpoint checkpoint in track.Checkpoints)
		{
			int rings = track.Objects.Count(o => o.Kind == HalfPipeObjectKind.Ring && o.TrackNode < checkpoint.TrackNode);
			Assert.True(rings >= checkpoint.RingGoal,
				$"{name}: {rings} rings before the ring test at node {checkpoint.TrackNode}, but the goal is {checkpoint.RingGoal}.");
		}
	}

	[Fact]
	public void Player_LeapsToTheOtherEdge_WhenRunningOffTheSide()
	{
		var player = new HalfPipePlayer();
		bool leaped = false;
		for (int tick = 0; tick < 600 && !leaped; tick++)
		{
			player.Update(1.0, jumpPressed: false);
			leaped = player.IsLeaping;
		}
		Assert.True(leaped, "Holding right did not reach the edge of the pipe.");

		while (player.IsLeaping)
		{
			player.Update(0.0, jumpPressed: false);
		}

		Assert.True(player.Angle < 0.0, $"The player landed at {player.Angle} rad, not on the left side.");
		Assert.Equal(0.0, player.Height);
	}

	[Fact]
	public void Player_Jumps_AndLandsAgain()
	{
		var player = new HalfPipePlayer();

		Assert.True(player.Update(0.0, jumpPressed: true));
		Assert.True(player.Height > 0.0);
		for (int tick = 0; tick < 200 && player.IsJumping; tick++)
		{
			player.Update(0.0, jumpPressed: false);
		}

		Assert.False(player.IsJumping);
		Assert.Equal(0.0, player.Height);
	}

	[Fact]
	public void Player_CannotMoveOrJump_JustAfterABomb()
	{
		var player = new HalfPipePlayer();
		player.Hurt();

		bool jumped = player.Update(1.0, jumpPressed: true);

		Assert.False(jumped);
		Assert.Equal(0.0, player.Angle);
		Assert.True(player.IsHurt);
	}
}
