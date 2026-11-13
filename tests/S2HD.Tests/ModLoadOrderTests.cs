using System;
using System.IO;
using System.Linq;
using SonicOrca;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the order in which the game loads the .dat files and the mod folders.</summary>
public sealed class ModLoadOrderTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "s2hd-mod-order-" + Guid.NewGuid().ToString("N"));

	public ModLoadOrderTests()
	{
		Directory.CreateDirectory(_folder);
		foreach (string file in new[] { "music.dat", "ending.dat", "arz.dat", "readme.txt" })
		{
			File.WriteAllText(Path.Combine(_folder, file), "");
		}
		foreach (string folder in new[] { "ending", "a-mod", ".hidden", "editor", "unsaved" })
		{
			Directory.CreateDirectory(Path.Combine(_folder, folder));
		}
	}

	public void Dispose() => Directory.Delete(_folder, true);

	[Fact]
	public void GetPaths_LoadsTheFolders_AfterAllTheDatFiles()
	{
		string[] names = ModLoadOrder.GetPaths(_folder, folders: true).Select(Path.GetFileName).ToArray();

		Assert.Equal(new[] { "arz.dat", "ending.dat", "music.dat", "a-mod", "ending" }, names);
	}

	[Fact]
	public void GetPaths_GivesOnlyTheDatFiles_WithoutFolders()
	{
		string[] names = ModLoadOrder.GetPaths(_folder, folders: false).Select(Path.GetFileName).ToArray();

		Assert.Equal(new[] { "arz.dat", "ending.dat", "music.dat" }, names);
	}

	[Fact]
	public void GetPaths_GivesNothing_ForAFolderThatDoesNotExist()
	{
		Assert.Empty(ModLoadOrder.GetPaths(Path.Combine(_folder, "missing"), folders: true));
	}
}
