using System;
using System.Collections.Generic;
using System.Linq;
using SONICORCA.OBJECTS.CNZSLOTCAGE;
using Xunit;

namespace S2HD.Tests;

/// <summary>The slot machine of Casino Night Zone, with the rules of Sonic 2 (SlotMachine in s2disasm).</summary>
public sealed class CNZSlotMachineTests
{
	/// <summary>Every reward that the rules can give: three of a kind, two jackpots, a pair and a jackpot, and bars.</summary>
	private static readonly HashSet<int> PossibleRewards = new HashSet<int>
	{
		30, 25, -1, 150, 10, 20,
		120, 100, -4, 40, 80,
		60, 50, -2, 300, 20,
		0, 2, 4,
	};

	private static int RunUntilIdle(CNZSlotMachine machine)
	{
		int frames = 0;
		do
		{
			machine.Update();
			frames++;
		}
		while (!machine.IsIdle && frames < 2000);
		return frames;
	}

	[Fact]
	public void LevelStart_ShowsAFaceInEachWindow_ThenTheMachineIsIdle()
	{
		var machine = new CNZSlotMachine(new Random(1));

		RunUntilIdle(machine);

		Assert.True(machine.IsIdle);
		// A face is 32 rows of the strip, so a reel at rest starts a window at a face
		for (int reel = 0; reel < 3; reel++)
		{
			Assert.Equal(0, machine.GetShownRow(reel) % 32);
		}
	}

	[Fact]
	public void Spins_StopOnTheirFaces_AndGiveTheRewardsOfTheRules()
	{
		var machine = new CNZSlotMachine(new Random(2));
		RunUntilIdle(machine);
		var rewards = new List<int>();
		for (int spin = 0; spin < 500; spin++)
		{
			Assert.True(machine.StartSpin());
			int frames = RunUntilIdle(machine);
			Assert.InRange(frames, 60, 400);
			for (int reel = 0; reel < 3; reel++)
			{
				Assert.Equal(0, machine.GetShownRow(reel) % 32);
			}
			rewards.Add(machine.Reward);
		}
		Assert.All(rewards, reward => Assert.Contains(reward, PossibleRewards));
		// The table makes about 55% of the spins three of a kind, and a three-Robotnik spin gives bombs
		Assert.InRange(rewards.Count(r => r != 0 && r != 2 && r != 4), 200, 400);
		Assert.Contains(-1, rewards);
	}

	[Fact]
	public void StartSpin_FailsWhileASpinRuns()
	{
		var machine = new CNZSlotMachine(new Random(3));
		RunUntilIdle(machine);

		Assert.True(machine.StartSpin());
		machine.Update();

		Assert.False(machine.StartSpin());
	}
}
