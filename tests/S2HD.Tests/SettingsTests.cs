using System;
using System.Globalization;
using System.IO;
using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Graphics;
using Xunit;

namespace S2HD.Tests;

/// <summary>The audio and video settings of the game, saved to and loaded from a configuration file.</summary>
public sealed class SettingsTests : IDisposable
{
	private readonly string _directory = Directory.CreateTempSubdirectory("s2hd-tests-").FullName;
	private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

	public void Dispose()
	{
		CultureInfo.CurrentCulture = _originalCulture;
		Directory.Delete(_directory, recursive: true);
	}

	[Fact]
	public void Volumes_SaveAndLoadWithCommaDecimalSeparator()
	{
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
		string path = Path.Combine(_directory, "settings.ini");
		File.WriteAllText(path, "");

		var settings = new S2HDSettings(new IniConfiguration(path), new TestAudioContext(), new TestWindowContext());
		settings.MusicVolume = 0.5;
		settings.SoundVolume = 0.25;

		var audio = new TestAudioContext();
		_ = new S2HDSettings(new IniConfiguration(path), audio, new TestWindowContext());
		Assert.Equal(0.5, audio.MusicVolume);
		Assert.Equal(0.25, audio.SoundVolume);
	}

	private sealed class TestAudioContext : AudioContext
	{
	}

	private sealed class TestWindowContext : WindowContext
	{
		public override void Dispose()
		{
		}
	}
}
