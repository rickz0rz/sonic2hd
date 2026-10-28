using System;
using System.Collections.Generic;
using SonicOrca.Core.SpecialStage;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// The stage flow and the results of the special stage, with the frame counts of Sonic 2 (s2disasm), on a small
/// track made by hand: "rings to go" at node 100, checkpoint 1 at node 200, checkpoint 2 at node 400, the emerald
/// marker at node 600 and the emerald test at node 717.
/// </summary>
public sealed class HalfPipeFlowTests
{
	/// <param name="offset">Moves every node further along the track.</param>
	private static HalfPipeTrack CreateTrack(int offset = 0)
	{
		var track = new HalfPipeTrack { EmeraldNode = 600 + offset };
		track.Checkpoints.AddRange(new[]
		{
			new HalfPipeCheckpoint(200 + offset, 30),
			new HalfPipeCheckpoint(400 + offset, 70),
			new HalfPipeCheckpoint(717 + offset, 130),
		});
		track.RingsToGoNodes.AddRange(new[] { 100 + offset, 300 + offset, 500 + offset });
		return track;
	}

	/// <summary>Runs the flow with the player one node further each frame, until the node or the end.</summary>
	private static List<HalfPipeFlowEvent> RunTo(HalfPipeStageFlow flow, ref int node, int targetNode, int rings)
	{
		var events = new List<HalfPipeFlowEvent>();
		while (node < targetNode && !flow.Finished)
		{
			node++;
			events.AddRange(flow.Update(node, rings));
		}
		return events;
	}

	[Fact]
	public void FirstGetRingsMessage_ComesAtFrame182_For70Frames()
	{
		// The checkpoints are far away, so that no other message comes in this time
		var flow = new HalfPipeStageFlow(CreateTrack(offset: 2000));
		int node = 0;

		RunTo(flow, ref node, 181, 0);
		Assert.Null(flow.Message);
		RunTo(flow, ref node, 182, 0);
		Assert.Equal("GET 30 RINGS!", flow.Message);
		RunTo(flow, ref node, 182 + 69, 0);
		Assert.NotNull(flow.Message);
		RunTo(flow, ref node, 182 + 70, 0);
		Assert.Null(flow.Message);
	}

	[Fact]
	public void PassedCheckpoint_ShowsCool_ThenTheNextGoal()
	{
		var flow = new HalfPipeStageFlow(CreateTrack());
		int node = 0;

		var events = RunTo(flow, ref node, 200, 35);

		Assert.Contains(HalfPipeFlowEvent.CheckpointPassed, events);
		Assert.Equal("COOL!", flow.Message);
		RunTo(flow, ref node, 270, 35);
		Assert.Equal("GET 70 RINGS!", flow.Message);
		Assert.Equal(70, flow.CurrentGoal);
	}

	[Fact]
	public void FailedCheckpoint_EndsTheStage_118FramesLater()
	{
		var flow = new HalfPipeStageFlow(CreateTrack());
		int node = 0;

		var events = RunTo(flow, ref node, 200, 10);
		Assert.Contains(HalfPipeFlowEvent.CheckpointFailed, events);
		Assert.Equal("NOT ENOUGH RINGS...", flow.Message);
		RunTo(flow, ref node, 208, 10);
		Assert.Null(flow.Counter);

		RunTo(flow, ref node, 200 + 117, 10);
		Assert.False(flow.Finished);
		RunTo(flow, ref node, 200 + 118, 10);
		Assert.True(flow.Finished);
		Assert.False(flow.EmeraldWon);
	}

	[Fact]
	public void RingsToGoCounter_Flashes_AndHidesAfterTheGoal()
	{
		var flow = new HalfPipeStageFlow(CreateTrack());
		int node = 0;
		RunTo(flow, ref node, 99, 29);
		Assert.Null(flow.Counter);

		// Visible 6 of each 8 frames after the marker
		int visible = 0;
		for (int i = 0; i < 8; i++)
		{
			RunTo(flow, ref node, node + 1, 29);
			if (flow.Counter != null)
			{
				Assert.Equal("1 RING TO GO!", flow.Counter);
				visible++;
			}
		}
		Assert.Equal(6, visible);

		// With the goal reached, "0 RINGS TO GO!" shows for 12 frames, then the counter hides
		RunTo(flow, ref node, node + 12, 30);
		RunTo(flow, ref node, node + 8, 30);
		Assert.Null(flow.Counter);
	}

	[Fact]
	public void Emerald_MasksTheInput_Appears60FramesLater_AndIsWon_WithTheJingle7FramesLater()
	{
		var flow = new HalfPipeStageFlow(CreateTrack());
		int node = 0;
		RunTo(flow, ref node, 599, 200);
		Assert.False(flow.InputMasked);

		RunTo(flow, ref node, 600, 200);
		Assert.True(flow.InputMasked);
		var appear = RunTo(flow, ref node, 660, 200);
		Assert.Contains(HalfPipeFlowEvent.EmeraldAppeared, appear);
		Assert.True(flow.EmeraldVisible);
		Assert.Equal(0x36, flow.EmeraldDepth);

		var won = RunTo(flow, ref node, 717, 200);
		Assert.Contains(HalfPipeFlowEvent.EmeraldNear, won);
		Assert.Contains(HalfPipeFlowEvent.EmeraldWon, won);
		Assert.DoesNotContain(HalfPipeFlowEvent.EmeraldJingle, won);
		Assert.DoesNotContain(HalfPipeFlowEvent.EmeraldJingle, RunTo(flow, ref node, 717 + 6, 200));
		Assert.Contains(HalfPipeFlowEvent.EmeraldJingle, RunTo(flow, ref node, 717 + 7, 200));
		// The emerald stops at the jingle, 64 frames after it appeared
		Assert.Equal(0x36 - 0.8 * 64, flow.EmeraldDepth, 6);
		Assert.DoesNotContain(HalfPipeFlowEvent.EmeraldJingle, RunTo(flow, ref node, 717 + 106, 200));
		Assert.False(flow.Finished);
		RunTo(flow, ref node, 717 + 107, 200);
		Assert.True(flow.Finished);
		Assert.True(flow.EmeraldWon);
	}

	[Fact]
	public void Emerald_WithAFasterTrack_FadesTheMusicBeforeTheTestAndTheJingle()
	{
		// The game moves the track 1.25 nodes each frame. The emerald must keep the timing of Obj59.
		var flow = new HalfPipeStageFlow(CreateTrack());
		var frames = new Dictionary<HalfPipeFlowEvent, int>();
		double node = 0.0;
		for (int frame = 1; frame < 2000 && !flow.Finished; frame++)
		{
			node += 1.25;
			foreach (HalfPipeFlowEvent flowEvent in flow.Update((int)node, 200))
			{
				frames.TryAdd(flowEvent, frame);
			}
		}

		int appeared = frames[HalfPipeFlowEvent.EmeraldAppeared];
		Assert.Equal(appeared + 49, frames[HalfPipeFlowEvent.EmeraldNear]);
		Assert.Equal(appeared + 57, frames[HalfPipeFlowEvent.EmeraldWon]);
		Assert.Equal(appeared + 64, frames[HalfPipeFlowEvent.EmeraldJingle]);
	}

	[Fact]
	public void EmeraldMissed_SlowsTheTrack_AndEnds80FramesLater()
	{
		var flow = new HalfPipeStageFlow(CreateTrack());
		int node = 0;
		RunTo(flow, ref node, 716, 100);

		var events = RunTo(flow, ref node, 717, 100);

		Assert.Contains(HalfPipeFlowEvent.EmeraldMissed, events);
		Assert.DoesNotContain(HalfPipeFlowEvent.EmeraldJingle, RunTo(flow, ref node, 717 + 20, 100));
		Assert.True(flow.TrackSlowed);
		Assert.Equal("NOT ENOUGH RINGS...", flow.Message);
		RunTo(flow, ref node, 717 + 80, 100);
		Assert.True(flow.Finished);
		Assert.False(flow.EmeraldWon);
	}

	[Fact]
	public void Results_GiveTheSonic2Bonuses_AndTallyThemIntoTheTotal()
	{
		var results = new HalfPipeResults(rings: 150, emeraldWon: true, ringsCollected: 206, ringsInStage: 206);

		Assert.Equal(15000, results.RingBonus);
		Assert.Equal(10000, results.GemsBonus);
		Assert.Equal(50000, results.PerfectBonus);
		Assert.Equal(75000, results.Score);

		int frames = 0;
		while (!results.Finished && frames < 2000)
		{
			results.Update();
			frames++;
		}
		Assert.True(results.Finished);
		Assert.Equal(75000, results.Total);
		Assert.Equal(0, results.RingBonus + results.GemsBonus + results.PerfectBonus);
		// 180 frames; the tally of the rings (150 steps and the frame that ends it); 90 + 90 frames; the tally of the
		// perfect bonus (250 steps and the end frame); then 120 frames
		Assert.Equal(180 + 151 + 90 + 90 + 251 + 120, frames);
	}

	[Fact]
	public void Results_PerfectBonus_ShowsAfterTheTally_WithItsSound_AndTalliesAt200PointsAFrame()
	{
		var results = new HalfPipeResults(rings: 10, emeraldWon: false, ringsCollected: 50, ringsInStage: 50);
		int Run(Func<bool> until)
		{
			int count = 0;
			while (!until() && count < 2000)
			{
				results.Update();
				count++;
			}
			return count;
		}

		Run(() => results.TallyJustEnded);
		Assert.Equal(1000, results.Total);
		Assert.Equal(HalfPipeResultsPhase.PerfectWait, results.Phase);
		Assert.False(results.PerfectShown);

		Assert.Equal(90, Run(() => results.PerfectJustShown));
		Assert.True(results.PerfectShown);
		Run(() => results.Phase == HalfPipeResultsPhase.PerfectTally);
		results.Update();
		Assert.Equal(HalfPipeResults.PerfectBonusPoints - 200, results.PerfectBonus);
		Assert.Equal(1200, results.Total);
	}

	[Fact]
	public void Results_WithTheSeventhEmerald_ShowTheSuperSonicMessage_AfterTheTally()
	{
		var results = new HalfPipeResults(rings: 10, emeraldWon: true, ringsCollected: 20, ringsInStage: 50, allEmeralds: true);
		bool sawMessage = false;
		int frames = 0;
		while (!results.Finished && frames < 2000)
		{
			results.Update();
			sawMessage |= results.Phase == HalfPipeResultsPhase.SuperMessage;
			Assert.NotEqual(HalfPipeResultsPhase.End, results.Phase);
			frames++;
		}

		Assert.True(sawMessage);
		// 180 frames, the tally of the gems bonus (100 steps and the end frame), then the message
		Assert.Equal(180 + 101 + HalfPipeResults.SuperMessageFrames, frames);
		Assert.False(new HalfPipeResults(rings: 10, emeraldWon: false, ringsCollected: 20, ringsInStage: 50, allEmeralds: true).SuperMessage);
	}

	[Fact]
	public void Results_WithoutAllRings_GiveNoPerfectBonus()
	{
		var results = new HalfPipeResults(rings: 40, emeraldWon: false, ringsCollected: 120, ringsInStage: 206);

		Assert.Equal(4000, results.RingBonus);
		Assert.Equal(0, results.GemsBonus);
		Assert.Equal(0, results.PerfectBonus);
	}
}
