using SonicOrca.Core.SpecialStage;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tails in the special stage, with the rules of Sonic 2 (s2disasm: SSTailsCPU_Control, SSPlayerSwapPositions).</summary>
public sealed class HalfPipeTailsTests
{
	private static readonly HalfPipeInput Left = new HalfPipeInput(true, false, false);

	private static readonly HalfPipeInput Jump = new HalfPipeInput(false, false, true);

	[Fact]
	public void Cpu_RepeatsTheInputOfSonic_16FramesLater()
	{
		var cpu = new HalfPipeTailsCpu();
		cpu.Record(Jump);
		Assert.Equal(default, cpu.GetGroundInput(default, false));
		for (int frame = 1; frame < 16; frame++)
		{
			cpu.Record(default);
			Assert.Equal(default, cpu.GetGroundInput(default, false));
		}
		cpu.Record(default);
		Assert.Equal(Jump, cpu.GetGroundInput(default, false));
		cpu.Record(default);
		Assert.Equal(default, cpu.GetGroundInput(default, false));
	}

	[Fact]
	public void SecondPad_TakesControl_Until180FramesAfterItsLastInput()
	{
		var cpu = new HalfPipeTailsCpu();
		for (int frame = 0; frame < 40; frame++)
		{
			cpu.Record(Left);
		}
		Assert.Equal(Left, cpu.GetGroundInput(default, false));

		Assert.Equal(Jump, cpu.GetGroundInput(Jump, true));
		Assert.True(cpu.HumanControl);
		for (int frame = 0; frame < HalfPipeTailsCpu.HumanControlFrames; frame++)
		{
			cpu.Record(Left);
			Assert.Equal(default, cpu.GetGroundInput(default, false));
		}
		Assert.False(cpu.HumanControl);
		// As in the original, the computer then continues with the recorded input of Sonic
		cpu.Record(Left);
		Assert.Equal(Left, cpu.GetGroundInput(default, false));
	}

	[Fact]
	public void Player_MovesToItsLane_OnePixelEachFrame()
	{
		var tails = new HalfPipePlayer(outerLane: true);
		Assert.Equal(HalfPipePlayer.OuterRadius, tails.Radius);
		Assert.True(tails.InFront);

		tails.OuterLane = false;
		int frames = 0;
		while (tails.Radius != HalfPipePlayer.InnerRadius && frames < 100)
		{
			tails.Update(false, false, false);
			frames++;
		}
		Assert.Equal(HalfPipePlayer.OuterRadius - HalfPipePlayer.InnerRadius, frames);
		Assert.False(tails.InFront);
		// On the ground, the player is on the pipe at the radius of the lane
		Assert.Equal(HalfPipePlayer.InnerRadius, tails.Position.Y, 0);
	}

	[Fact]
	public void DuoQuotas_AreUsedWithTails()
	{
		Assert.NotEqual(HalfPipeLayout.GetRingQuotas(0, duo: false), HalfPipeLayout.GetRingQuotas(0, duo: true));
	}
}
