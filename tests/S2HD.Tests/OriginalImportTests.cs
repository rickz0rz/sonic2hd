using System.Linq;
using System.Security.Cryptography;
using SonicOrca.Original;
using SonicOrca.Original.Music;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the parts of the import from the disassembly that the web build needs.</summary>
public sealed class OriginalImportTests
{
	[Fact]
	public void Md5_GivesTheSameHashAsTheFramework()
	{
		// Each length from 0 to 200 bytes, so that the padding goes into one and into two extra blocks
		for (int length = 0; length <= 200; length++)
		{
			byte[] data = Enumerable.Range(0, length).Select(i => (byte)(i * 31 + length)).ToArray();

			Assert.Equal(MD5.HashData(data), Md5.HashData(data));
		}
	}

	[Fact]
	public void GetSteps_MakesEachModWithConsecutiveSteps()
	{
		var steps = OriginalImport.GetSteps(null, _ => { });
		string[] mods = steps.Select(step => step.Mod).Distinct().ToArray();

		Assert.Equal(OriginalImport.Zones.Concat(new[] { "special-stages", "super-sonic", "demos", "ending", "music" }), mods);
		// A mod is packed after its last step, so the steps of a mod must not be split
		for (int i = 1; i < steps.Count; i++)
		{
			if (steps[i].Mod != steps[i - 1].Mod)
			{
				Assert.DoesNotContain(steps.Take(i), step => step.Mod == steps[i].Mod);
			}
		}
		Assert.Equal(MusicImport.Songs.Count + MusicImport.SpedUpSongs.Count + MusicImport.Sounds.Count, steps.Count(step => step.Mod == "music"));
	}
}
