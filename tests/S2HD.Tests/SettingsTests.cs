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

	[Fact]
	public void HudScale_IsSaved_AndKeepsItsLimitsAndSteps()
	{
		string path = Path.Combine(_directory, "hud.ini");
		File.WriteAllText(path, "[graphics]\nhud_scale=9\n");

		var settings = new S2HDSettings(new IniConfiguration(path), new TestAudioContext(), new TestWindowContext());
		Assert.Equal(1.5, settings.HudScale);
		settings.HudScale = 0.83;

		var loaded = new S2HDSettings(new IniConfiguration(path), new TestAudioContext(), new TestWindowContext());
		Assert.Equal(0.85, loaded.HudScale, 9);
		string empty = Path.Combine(_directory, "empty.ini");
		File.WriteAllText(empty, "");
		Assert.Equal(1.0, new S2HDSettings(new IniConfiguration(empty), new TestAudioContext(), new TestWindowContext()).HudScale);
	}

	[Fact]
	public void HudScaleAbout_KeepsTheAnchor_AndScalesTheDistance()
	{
		SonicOrca.Geometry.Matrix4 model = SonicOrca.HudScale.About(SonicOrca.Geometry.Matrix4.Identity, 1.5, 0.0, 1080.0);

		SonicOrca.Geometry.Vector2 anchor = Transform(model, 0.0, 1080.0);
		SonicOrca.Geometry.Vector2 icon = Transform(model, 264.0, 958.0);

		Assert.Equal(0.0, anchor.X, 6);
		Assert.Equal(1080.0, anchor.Y, 6);
		Assert.Equal(396.0, icon.X, 6);
		Assert.Equal(1080.0 - 122.0 * 1.5, icon.Y, 6);
	}

	private static SonicOrca.Geometry.Vector2 Transform(SonicOrca.Geometry.Matrix4 m, double x, double y)
	{
		SonicOrca.Geometry.Vector4 v = m * new SonicOrca.Geometry.Vector4(x, y, 0.0, 1.0);
		return new SonicOrca.Geometry.Vector2(v.X, v.Y);
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
