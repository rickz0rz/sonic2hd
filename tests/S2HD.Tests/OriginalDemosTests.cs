using System.Linq;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Converts the demos of the original game from a local Sonic 2 disassembly (s2disasm). Skipped when the disassembly
/// is not found.
/// </summary>
public sealed class OriginalDemosTests
{
	[Fact]
	public void ReadButtons_HoldsEachInputForItsDuration()
	{
		byte[] buttons = OriginalDemos.ReadButtons(SpecialStageImportTests.FindDisassembly(), "Demo_EHZ");

		// Demo_EHZ starts with no input for $4C frames, then right for $43 frames, then right and C for 9 frames
		Assert.Equal(OriginalDemos.DemoFrames, buttons.Length);
		Assert.All(buttons.Take(0x4C), b => Assert.Equal(0, b));
		Assert.All(buttons.Skip(0x4C).Take(0x43), b => Assert.Equal(8, b));
		Assert.All(buttons.Skip(0x4C + 0x43).Take(9), b => Assert.Equal(8 | 0x20, b));
		Assert.Equal(8, buttons[0x4C + 0x43 + 9]);
	}

	[Fact]
	public void ToRecording_StartsAfterTheTitleCard()
	{
		byte[] buttons = OriginalDemos.ReadButtons(SpecialStageImportTests.FindDisassembly(), "Demo_CPZ");
		byte[] recording = OriginalDemos.ToRecording(buttons);

		// 40 bytes for each frame: the title card frames, then the demo
		Assert.Equal((OriginalDemos.LevelStartFrames + OriginalDemos.DemoFrames) * 40, recording.Length);
		// Demo_CPZ holds right from frame $47: the x direction of that frame is 1.0
		Assert.Equal(1.0, System.BitConverter.ToDouble(recording, (OriginalDemos.LevelStartFrames + 0x47) * 40));
		Assert.Equal(0.0, System.BitConverter.ToDouble(recording, (OriginalDemos.LevelStartFrames + 0x46) * 40));
	}
}
