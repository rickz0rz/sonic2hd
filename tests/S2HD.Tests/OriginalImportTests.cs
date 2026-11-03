using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SonicOrca.Original;
using SonicOrca.Original.Music;
using SonicOrca.Resources;
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
	public void TileAnimationsStep_ConvertsTheTileSetsOfTheGameData()
	{
		string root = Directory.CreateTempSubdirectory("s2hd-tile-animations-").FullName;
		try
		{
			// A game data folder with one tile set that has an animated tile
			string data = Path.Combine(root, "data", "SONICORCA", "LEVELS", "EHZ", "TILESET");
			Directory.CreateDirectory(data);
			File.WriteAllText(Path.Combine(data, "_.tileset.xml"),
				"<tileset><textures><texture>/TEXTURE</texture></textures><tiles><tile id=\"1\"><frame x=\"0\" y=\"0\" /><frame x=\"64\" y=\"0\" /></tile></tiles></tileset>");
			ResourceTree gameData = ResourceDirectory.Import(Path.Combine(root, "data"));
			OriginalImportStep step = OriginalImport.GetSteps(null, _ => { }, gameData).Single(s => s.Mod == "tile-animations");

			string folder = Path.Combine(root, "out");
			step.Run(null, folder);

			string tileSet = Path.Combine(folder, "SONICORCA", "LEVELS", "EHZ", "TILESET");
			Assert.Contains("anigroup=\"/ANIGROUP\"", File.ReadAllText(Path.Combine(tileSet, "_.tileset.xml")));
			Assert.True(File.Exists(Path.Combine(tileSet, "ANIGROUP.anigroup.xml")));
			// Without the game data, the step writes nothing, but the folder exists for the packing of the web build
			string empty = Path.Combine(root, "empty");
			OriginalImport.GetSteps(null, _ => { }).Single(s => s.Mod == "tile-animations").Run(null, empty);
			Assert.Empty(Directory.GetFileSystemEntries(empty));
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[Fact]
	public void GetSteps_MakesEachModWithConsecutiveSteps()
	{
		var steps = OriginalImport.GetSteps(null, _ => { });
		string[] mods = steps.Select(step => step.Mod).Distinct().ToArray();

		Assert.Equal(OriginalImport.Zones.Concat(new[] { "special-stages", "super-sonic", "demos", "ending", "music", "tile-animations" }), mods);
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
