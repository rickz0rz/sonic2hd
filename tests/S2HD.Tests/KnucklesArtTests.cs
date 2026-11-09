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
	/// The palette of Super Knuckles has the normal colours of the entries 2, 3 and 5 in row 0, and then the 10 frames of
	/// CyclingPal_SKTransformation, which have other colours.
	/// </summary>
	[Fact]
	public void ImportPalette_HasTheNormalColours_ThenTheTenFramesOfTheCycle()
	{
		byte[] png = KnucklesArt.ImportPalette(FindKis2());

		var reader = new Hjg.Pngcs.PngReader(new System.IO.MemoryStream(png));
		Assert.Equal(3, reader.ImgInfo.Cols);
		Assert.Equal(11, reader.ImgInfo.Rows);
		byte[][] rows = Enumerable.Range(0, 11).Select(row => reader.ReadRowByte(row).ScanlineB.ToArray()).ToArray();
		reader.End();
		Assert.All(rows.Skip(1), row => Assert.NotEqual(rows[0], row));
	}

	/// <summary>The life icon has the size of the HD life icons, 68 pixels, with the head of 16 pixels at four times the size in the middle.</summary>
	[Fact]
	public void ImportLifeIcon_HasTheSizeOfTheHdIcons()
	{
		byte[] png = KnucklesArt.ImportLifeIcon(FindKis2());

		var reader = new Hjg.Pngcs.PngReader(new System.IO.MemoryStream(png));
		Assert.Equal(68, reader.ImgInfo.Cols);
		Assert.Equal(68, reader.ImgInfo.Rows);
		byte[] edge = reader.ReadRowByte(0).ScanlineB.ToArray();
		byte[] middle = reader.ReadRowByte(34).ScanlineB.ToArray();
		reader.End();
		int channels = edge.Length / 68;
		// The margin of 2 pixels is transparent, and the head is not
		Assert.All(Enumerable.Range(0, 68), x => Assert.Equal(0, edge[x * channels + 3]));
		Assert.Equal(255, middle[34 * channels + 3]);
	}

	/// <summary>
	/// The special stage sprites of Knuckles have the animations of the HD group of Sonic: the run with the 4 upright
	/// frames, and the ball with 2 frames.
	/// </summary>
	[Fact]
	public void ImportSpecialStage_HasTheRunAndTheBall()
	{
		ImportedSprite sprite = KnucklesArt.ImportSpecialStage(FindKis2());

		XElement[] animations = XDocument.Parse(sprite.AnimationGroup).Descendants("animation").ToArray();
		Assert.Equal(2, animations.Length);
		Assert.Equal(4, animations[0].Elements("frame").Count());
		Assert.Equal(2, animations[1].Elements("frame").Count());
	}

	/// <summary>Knuckles in Sonic 2 lowered the ring quotas of a player alone. Sonic and Tails keep the quotas of Sonic 2.</summary>
	[Fact]
	public void KnucklesRingQuotas_AreTheQuotasOfKis2()
	{
		Assert.Equal(new[] { 50, 90, 130 }, SonicOrca.Core.SpecialStage.HalfPipeLayout.GetRingQuotas(1, duo: false, knuckles: true));
		Assert.Equal(new[] { 50, 100, 140 }, SonicOrca.Core.SpecialStage.HalfPipeLayout.GetRingQuotas(1, duo: false));
		Assert.Equal(new[] { 50, 100, 140 }, SonicOrca.Core.SpecialStage.HalfPipeLayout.GetRingQuotas(6, duo: false, knuckles: true));
	}

	/// <summary>
	/// The ending of KiS2 has the art of Knuckles with Flickies, and with eagles for the 7 emeralds. Knuckles waits on
	/// the plane with the first frame of KnucklesAni_Wait. The credits end with the logo of KiS2 for each palette.
	/// </summary>
	[Fact]
	public void ImportKnuckles_WritesTheEndingAndTheLogoOfKis2()
	{
		string kis2 = FindKis2();
		string folder = System.IO.Directory.CreateTempSubdirectory("s2hd-knuckles-ending-").FullName;
		try
		{
			EndingArt.ImportKnuckles(kis2, folder);
			OriginalCredits.ImportKnucklesLogo(kis2, folder);

			foreach (string variant in new[] { "KNUCKLES", "SUPERKNUCKLES" })
			{
				foreach (string part in new[] { "CHARACTER/ANIGROUP.anigroup.xml", "BIRDS/ANIGROUP.anigroup.xml", "PLANE/ANIGROUP.anigroup.xml", "CLOSEUP.png" })
				{
					Assert.True(System.IO.File.Exists(System.IO.Path.Combine(folder, "SONICORCA", "ENDING", variant, part)), $"{variant}/{part}");
				}
			}
			XElement[] animations = XDocument.Load(System.IO.Path.Combine(folder, "SONICORCA", "ENDING", "KNUCKLES", "CHARACTER", "ANIGROUP.anigroup.xml"))
				.Descendants("animation").ToArray();
			Assert.Equal(2, animations.Length);
			Assert.Single(animations[1].Elements("frame"));
			for (int p = 0; p < OriginalCredits.LogoPalettes; p++)
			{
				Assert.True(System.IO.File.Exists(System.IO.Path.Combine(folder, "SONICORCA", "CREDITS", "ORIGINAL", OriginalCredits.KnucklesLogoFolder, $"PALETTE{p}.png")));
			}
		}
		finally
		{
			System.IO.Directory.Delete(folder, recursive: true);
		}
	}

	/// <summary>
	/// The signpost of Knuckles has the face and the 3 frames of the turn. The extra life of a monitor is the head of
	/// the lives counter, 64 pixels. The slot reels of Knuckles differ from the reels of Sonic 2 only by the face.
	/// </summary>
	[Fact]
	public void SignpostMonitorAndReels_HaveTheArtOfKis2()
	{
		string kis2 = FindKis2();

		ImportedSprite signpost = KnucklesArt.ImportSignpost(kis2);
		byte[] icon = KnucklesArt.ImportMonitorIcon(kis2);
		uint[] palette = new uint[64];
		SpriteImport.ReadPaletteFile(kis2, "art/palettes/SonicAndTails.bin", palette, 0);
		byte[] reels = SlotReels.RenderTexture(kis2, palette, 4);
		byte[] knucklesReels = SlotReels.RenderTexture(kis2, palette, 4, kis2);

		Assert.Equal(4, XDocument.Parse(signpost.AnimationGroup).Descendants("animation").Count());
		var reader = new Hjg.Pngcs.PngReader(new System.IO.MemoryStream(icon));
		Assert.Equal(64, reader.ImgInfo.Cols);
		Assert.Equal(64, reader.ImgInfo.Rows);
		reader.End();
		Assert.NotEqual(reels, knucklesReels);
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
