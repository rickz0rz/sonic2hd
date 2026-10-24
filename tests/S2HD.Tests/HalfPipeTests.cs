using System;
using System.Linq;
using SonicOrca.Core.SpecialStage;
using Xunit;

namespace S2HD.Tests;

/// <summary>The rules of the half-pipe special stage, without the game window.</summary>
public sealed class HalfPipeTests
{
	// The track of the prototype: 92 segments of 28 track nodes
	private const int SegmentCount = 92;

	private const int NodesPerSegment = 28;

	[Theory]
	[InlineData(0, 30, 70, 130)]
	[InlineData(4, 50, 90, 160)]
	[InlineData(6, 100, 150, 190)]
	public void Checkpoints_UseTheSonic2SoloQuotas(int stage, int first, int second, int third)
	{
		var (_, checkpoints) = HalfPipeLayout.Create(SegmentCount, NodesPerSegment, stage, duo: false);

		Assert.Equal(new[] { first, second, third }, checkpoints.Select(c => c.RingGoal));
	}

	[Fact]
	public void Checkpoints_UseTheDuoQuotas()
	{
		var (_, checkpoints) = HalfPipeLayout.Create(SegmentCount, NodesPerSegment, 0, duo: true);

		Assert.Equal(new[] { 40, 80, 140 }, checkpoints.Select(c => c.RingGoal));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void EveryStage_HasEnoughRingsBeforeEachCheckpoint(bool duo)
	{
		for (int stage = 0; stage < HalfPipeLayout.StageCount; stage++)
		{
			var (objects, checkpoints) = HalfPipeLayout.Create(SegmentCount, NodesPerSegment, stage, duo);
			foreach (HalfPipeCheckpoint checkpoint in checkpoints)
			{
				int rings = objects.Count(o => o.Kind == HalfPipeObjectKind.Ring && o.TrackNode < checkpoint.TrackNode);
				Assert.True(rings >= checkpoint.RingGoal,
					$"Stage {stage + 1}: {rings} rings before the checkpoint at node {checkpoint.TrackNode}, but the goal is {checkpoint.RingGoal}.");
			}
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
