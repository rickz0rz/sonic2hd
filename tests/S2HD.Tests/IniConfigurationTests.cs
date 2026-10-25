using System;
using System.IO;
using SonicOrca;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the settings file: new properties and the first save.</summary>
public sealed class IniConfigurationTests : IDisposable
{
	private readonly string _directory = Directory.CreateTempSubdirectory("s2hd-ini-").FullName;

	public void Dispose() => Directory.Delete(_directory, recursive: true);

	[Fact]
	public void ANewConfiguration_SavesToTheFileOfItsFirstSave()
	{
		string path = Path.Combine(_directory, "sonicorca.cfg");
		IniConfiguration configuration = new IniConfiguration();
		configuration.SetProperty("audio", "volume", "1.0");
		configuration.Save(path);

		// The settings call Save() without a path after each change
		configuration.SetProperty("audio", "volume", "0.5");
		configuration.Save();

		Assert.Equal("0.5", new IniConfiguration(path).GetProperty("audio", "volume"));
	}

	[Fact]
	public void NewPropertiesInTheSameSection_KeepTheirValues()
	{
		string path = Path.Combine(_directory, "sonicorca.cfg");
		File.WriteAllText(path, "[graphics]\nfullscreen=0\n\n[audio]\nvolume=1.0\n");
		IniConfiguration configuration = new IniConfiguration(path);

		// Each new property is inserted after the section header, so the lines after it move
		configuration.SetProperty("graphics", "shadows", "1");
		configuration.SetProperty("graphics", "water_effects", "0");
		configuration.SetProperty("graphics", "shadows", "0");
		configuration.SetProperty("audio", "volume", "0.25");
		configuration.Save();

		IniConfiguration loaded = new IniConfiguration(path);
		Assert.Equal("0", loaded.GetProperty("graphics", "shadows"));
		Assert.Equal("0", loaded.GetProperty("graphics", "water_effects"));
		Assert.Equal("0", loaded.GetProperty("graphics", "fullscreen"));
		Assert.Equal("0.25", loaded.GetProperty("audio", "volume"));
	}
}
