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
	public void GeneratedStage_ShowsNoObjectsBeforeTheFirstMessage()
	{
		HalfPipeTrack track = HalfPipeTrackBuilder.Build(HalfPipeLayout.CreateGenerated(), HalfPipeLayout.GetRingQuotas(0, duo: false));

		// The player moves one node each tick, and the screen shows the objects up to ObjectDrawDistance nodes ahead
		int firstNode = track.Objects.Min(o => o.TrackNode);
		Assert.True(firstNode > HalfPipeStageFlow.FirstMessageFrame + HalfPipeSpecialStageScreen.ObjectDrawDistance,
			$"The first object is at node {firstNode}, in view before the first message.");
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

	// The player tests use the values of the original from the Sonic 2 disassembly (bin/special-stage-spec.md)

	[Fact]
	public void Player_Accelerates_By60PerFrame_UpToAbout600()
	{
		var player = new HalfPipePlayer();

		// At the bottom the slope does not pull, so the first frame adds exactly $60
		player.Update(left: true, right: false, jumpPressed: false);
		Assert.Equal(0x60, player.Inertia);

		// The slope pulls back while the player climbs, but holding LEFT still runs round the whole pipe
		int laps = 0;
		int previous = player.Angle;
		for (int frame = 0; frame < 600; frame++)
		{
			player.Update(left: true, right: false, jumpPressed: false);
			// The clamp to $600 comes before the slope pull, which can add up to $50 downhill, as in the original
			Assert.InRange(player.Inertia, 0, 0x600 + 0x50);
			if (player.Angle < previous)
			{
				laps++;
			}
			previous = player.Angle;
		}
		Assert.True(laps > 0, "Holding LEFT did not run round the pipe.");
		Assert.True(player.OnGround);
	}

	[Fact]
	public void Player_StaysOnTheWall_For30Frames_AfterThePadIsReleased()
	{
		var player = new HalfPipePlayer();
		// Climb the left wall, then let go
		while (player.Angle < 0x6C)
		{
			player.Update(left: true, right: false, jumpPressed: false);
		}
		for (int frame = 0; frame < 20; frame++)
		{
			player.Update(left: false, right: false, jumpPressed: false);
		}
		int stopped = player.Angle;
		for (int frame = 20; frame < 29; frame++)
		{
			player.Update(left: false, right: false, jumpPressed: false);
		}
		Assert.Equal(stopped, player.Angle);

		// Then the slope pulls the player back down
		for (int frame = 0; frame < 60; frame++)
		{
			player.Update(left: false, right: false, jumpPressed: false);
		}
		Assert.True(player.Angle < stopped, $"The player did not slide down from angle {stopped}: now {player.Angle}.");
	}

	[Fact]
	public void Player_JumpFromTheBottom_LandsAfterAbout46Frames()
	{
		var player = new HalfPipePlayer();

		Assert.True(player.Update(left: false, right: false, jumpPressed: true));
		int frames = 0;
		while (player.InAir && frames < 200)
		{
			player.Update(left: false, right: false, jumpPressed: false);
			frames++;
		}

		Assert.InRange(frames, 40, 52);
		Assert.True(player.OnGround);
		Assert.InRange(player.Angle, HalfPipePlayer.BottomAngle - 2, HalfPipePlayer.BottomAngle + 2);
	}

	[Fact]
	public void Player_FallsFromTheTop_WhenTooSlow_AndLandsOnTheLowerHalf()
	{
		var player = new HalfPipePlayer();
		// Run up the left wall at full speed to the upper half, then let go
		while (player.Angle < 0x98)
		{
			player.Update(left: true, right: false, jumpPressed: false);
		}
		bool fell = false;
		for (int frame = 0; frame < 60 && !fell; frame++)
		{
			player.Update(left: false, right: false, jumpPressed: false);
			fell = player.InAir;
		}
		Assert.True(fell, "The player did not fall from the top of the pipe.");
		Assert.False(player.IsJumping);

		for (int frame = 0; frame < 120 && player.InAir; frame++)
		{
			player.Update(left: false, right: false, jumpPressed: false);
		}
		Assert.True(player.OnGround);
		Assert.InRange(player.Angle, 0x00, 0x80);
	}

	[Fact]
	public void Player_AfterABomb_SpinsFor32Frames_ThenFlashesFor30Frames()
	{
		var player = new HalfPipePlayer();

		Assert.True(player.Hurt());
		for (int frame = 1; frame < 32; frame++)
		{
			player.Update(left: true, right: false, jumpPressed: true);
			Assert.True(player.IsHurt, $"Frame {frame}: the hurt animation ended early.");
		}
		Assert.Equal(HalfPipePlayer.BottomAngle, player.Angle);
		player.Update(left: false, right: false, jumpPressed: false);

		Assert.False(player.IsHurt);
		Assert.Equal(HalfPipePlayer.FlashFrames, player.FlashFramesLeft);
		Assert.False(player.Hurt());
	}

	[Theory]
	[InlineData(110, 0, 0x00)]
	[InlineData(0, 110, 0x40)]
	[InlineData(-110, 0, 0x80)]
	[InlineData(0, -110, 0xC0)]
	[InlineData(50, 50, 0x20)]
	public void JumpAngle_UsesTheOctantApproximation(int x, int y, int expected)
	{
		Assert.Equal(expected, HalfPipePlayer.GetAngle(x, y));
	}
}
