using System;
using System.Linq;
using System.Xml.Linq;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests of the import of the sprites of Knuckles from "Knuckles in Sonic 2" (KiS2).</summary>
public sealed class KnucklesArtTests
{
	[Fact]
	public void Import_HasAnAnimationForEachCharacterAnimation_ThenTheAnimationsOfKnuckles()
	{
		ImportedSprite sprite = KnucklesArt.Import(FindKis2());

		XElement[] animations = XDocument.Parse(sprite.AnimationGroup).Descendants("animation").ToArray();
		Assert.Equal(KnucklesArt.TransformAnimation, Enum.GetValues<SonicOrca.Core.Objects.Base.CharacterAnimation>().Length);
		Assert.Equal(KnucklesArt.GetUpAnimation + 1, animations.Length);
		// The walk has the 8 upright frames, and the glide has the 5 frames of the turn
		Assert.Equal(8, animations[(int)SonicOrca.Core.Objects.Base.CharacterAnimation.Walk].Elements("frame").Count());
		Assert.Equal(5, animations[KnucklesArt.GlideAnimation].Elements("frame").Count());
		// "$FE 6" at the end of KnucklesAni_Balance goes back 6 frames
		XElement balance = animations[(int)SonicOrca.Core.Objects.Base.CharacterAnimation.BalanceClose];
		Assert.Equal(balance.Elements("frame").Count() - 6, (int)balance.Attribute("loop"));
	}

	/// <summary>
	/// The folder of the knuckles-in-sonic-2 branch of s2disasm: S2HD_KIS2_DIR, or ../s2disasm-kis2 next to the
	/// repository. The test skips when the folder is not there.
	/// </summary>
	internal static string FindKis2()
	{
		string[] candidates =
		{
			Environment.GetEnvironmentVariable("S2HD_KIS2_DIR"),
			System.IO.Path.GetFullPath(System.IO.Path.Combine(TestPaths.RepoRoot, "..", "s2disasm-kis2")),
		};
		string found = candidates.FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate) && KnucklesArt.IsKis2Folder(candidate));
		if (found == null)
		{
			Assert.Skip("The KiS2 disassembly was not found. Set S2HD_KIS2_DIR to a folder of the knuckles-in-sonic-2 branch of s2disasm.");
		}
		return found;
	}
}
