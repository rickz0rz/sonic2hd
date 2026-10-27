using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SonicOrca.Original.Music;

/// <summary>
/// Makes the music and the sounds that the HD demo data does not have. They come from the songs and the sound
/// effects of the Sonic 2 disassembly. Each song plays on the emulated sound chips (see <see cref="SongRenderer"/>). ffmpeg encodes
/// the sound as Ogg Vorbis, and a sample info file gives the loop point of a song. The music comes from the original
/// game, so it stays local.
/// </summary>
public static class MusicImport
{
	/// <summary>The folder of the songs in the disassembly.</summary>
	public const string MusicFolder = "sound/music";

	/// <summary>The folder of the sound effects in the disassembly.</summary>
	public const string SoundFolder = "sound/sfx";

	/// <summary>The folder of the DAC drums in the disassembly.</summary>
	public const string DacFolder = "sound/DAC";

	/// <summary>The songs to import: the file in <see cref="MusicFolder"/> and the resource key of the music.</summary>
	public static readonly IReadOnlyList<(string File, string Key)> Songs = new[]
	{
		("87 - ARZ.asm", "SONICORCA/MUSIC/LEVELS/ARZ"),
		("89 - CNZ.asm", "SONICORCA/MUSIC/LEVELS/CNZ"),
		("8B - MCZ.asm", "SONICORCA/MUSIC/LEVELS/MCZ/COOPERATIVE"),
		("84 - OOZ.asm", "SONICORCA/MUSIC/LEVELS/OOZ"),
		("8D - SCZ.asm", "SONICORCA/MUSIC/LEVELS/SCZ"),
		("8F - WFZ.asm", "SONICORCA/MUSIC/LEVELS/WFZ"),
		("8A - DEZ.asm", "SONICORCA/MUSIC/LEVELS/DEZ"),
		// The jingle of the results screen of the special stage (MusID_EndLevel)
		("9A - End of Act.asm", "SONICORCA/MUSIC/JINGLE/STAGECLEAR/S2"),
		// The music of Super Sonic (MusID_SuperSonic)
		("96 - Super Sonic.asm", "SONICORCA/MUSIC/JINGLE/SUPER")
	};

	/// <summary>The sound effects to import: the file in <see cref="SoundFolder"/> and the resource key of the sound.</summary>
	public static readonly IReadOnlyList<(string File, string Key)> Sounds = new[]
	{
		// The sound of the transformation into Super Sonic (SndID_SuperTransform)
		("DF - Super Transform.asm", "SONICORCA/SOUND/SUPERTRANSFORM")
	};

	/// <summary>The quality of the Vorbis encoder.</summary>
	private const string EncoderArguments = "-c:a vorbis -strict experimental -b:a 160k";

	/// <summary>
	/// Imports the songs and the sounds into a folder in the layout of "s2hd-data unpack": for each song, the sample
	/// KEY/SAMPLE.ogg and the sample info KEY/_.sampleinfo.xml, and for each sound, the sample KEY.ogg.
	/// </summary>
	/// <param name="disassembly">The folder of the disassembly.</param>
	/// <param name="folder">The output folder.</param>
	/// <param name="ffmpeg">The ffmpeg program.</param>
	/// <param name="log">Gets a line for each song.</param>
	public static void Import(string disassembly, string folder, string ffmpeg, Action<string> log)
	{
		IEnumerable<Func<string>> imports = Songs.Select(song => (Func<string>)(() => ImportSong(song, disassembly, folder, ffmpeg)))
			.Concat(Sounds.Select(sound => (Func<string>)(() => ImportSound(sound, disassembly, folder, ffmpeg))));
		Parallel.ForEach(imports, import =>
		{
			string line = import();
			lock (log)
			{
				log(line);
			}
		});
	}

	/// <summary>
	/// Imports one song: the sample KEY/SAMPLE.ogg and the sample info KEY/_.sampleinfo.xml. Without ffmpeg (the web
	/// build), the sample is KEY/SAMPLE.smps: the song and its drums, which the game plays live on the emulated chips.
	/// Returns a line that tells the length and the loop point of the song.
	/// </summary>
	/// <param name="song">The song, from <see cref="Songs"/>.</param>
	/// <param name="disassembly">The folder of the disassembly.</param>
	/// <param name="folder">The output folder.</param>
	/// <param name="ffmpeg">The ffmpeg program, or null for a song that plays live.</param>
	public static string ImportSong((string File, string Key) song, string disassembly, string folder, string ffmpeg)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		SmpsSong smps = SmpsSong.FromFile(Path.Combine(disassembly, MusicFolder, song.File));
		string keyFolder = Path.Combine(new[] { folder }.Concat(song.Key.Split('/')).ToArray());
		Directory.CreateDirectory(keyFolder);
		if (ffmpeg == null)
		{
			SmpsResourceType.Write(Path.Combine(keyFolder, "SAMPLE.smps"), smps, SongRenderer.LoadDrums(Path.Combine(disassembly, DacFolder)));
			WriteSampleInfo(Path.Combine(keyFolder, "_.sampleinfo.xml"), null);
			return string.Format(CultureInfo.InvariantCulture, "{0}: plays live ({1:0.0} s to make)", song.Key, stopwatch.Elapsed.TotalSeconds);
		}
		RenderedSong rendered = SongRenderer.Render(smps, Path.Combine(disassembly, DacFolder));
		string wave = Path.Combine(keyFolder, "SAMPLE.wav");
		rendered.WriteWave(wave);
		try
		{
			Encode(ffmpeg, wave, Path.Combine(keyFolder, "SAMPLE.ogg"));
		}
		finally
		{
			File.Delete(wave);
		}
		WriteSampleInfo(Path.Combine(keyFolder, "_.sampleinfo.xml"), rendered.LoopSample);
		string loop = rendered.LoopSample is int sample
			? (sample / (double)rendered.SampleRate).ToString("0.00", CultureInfo.InvariantCulture) + " s"
			: "none";
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.0} s, loop at {2} ({3:0.0} s to make)",
			song.Key, rendered.Length / (double)rendered.SampleRate, loop, stopwatch.Elapsed.TotalSeconds);
	}

	/// <summary>
	/// Imports one sound effect: the sample KEY.ogg, or without ffmpeg (the web build) KEY.smps, which the game plays
	/// live. A sound effect has no DAC drums. Returns a line that tells the length of the sound.
	/// </summary>
	/// <param name="sound">The sound, from <see cref="Sounds"/>.</param>
	/// <param name="disassembly">The folder of the disassembly.</param>
	/// <param name="folder">The output folder.</param>
	/// <param name="ffmpeg">The ffmpeg program, or null for a sound that plays live.</param>
	public static string ImportSound((string File, string Key) sound, string disassembly, string folder, string ffmpeg)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		SmpsSong smps = SmpsSong.FromFile(Path.Combine(disassembly, SoundFolder, sound.File));
		if (!smps.IsSoundEffect)
		{
			throw new InvalidDataException($"{sound.File} is not a sound effect.");
		}
		string file = Path.Combine(new[] { folder }.Concat(sound.Key.Split('/')).ToArray());
		Directory.CreateDirectory(Path.GetDirectoryName(file));
		if (ffmpeg == null)
		{
			SmpsResourceType.Write(file + ".smps", smps, new Dictionary<DacSample, (byte[], int)>());
			return string.Format(CultureInfo.InvariantCulture, "{0}: plays live ({1:0.0} s to make)", sound.Key, stopwatch.Elapsed.TotalSeconds);
		}
		RenderedSong rendered = SongRenderer.Render(smps, null);
		string wave = file + ".wav";
		rendered.WriteWave(wave);
		try
		{
			Encode(ffmpeg, wave, file + ".ogg");
		}
		finally
		{
			File.Delete(wave);
		}
		return string.Format(CultureInfo.InvariantCulture, "{0}: {1:0.0} s ({2:0.0} s to make)",
			sound.Key, rendered.Length / (double)rendered.SampleRate, stopwatch.Elapsed.TotalSeconds);
	}

	/// <summary>Writes a sample info file for the sample SAMPLE next to it.</summary>
	public static void WriteSampleInfo(string path, int? loopSample)
	{
		string loop = loopSample is int sample ? $"\n  <loop>{sample.ToString(CultureInfo.InvariantCulture)}</loop>" : "";
		File.WriteAllText(path, $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<sampleinfo>\n  <sample>/SAMPLE</sample>{loop}\n</sampleinfo>\n");
	}

	private static void Encode(string ffmpeg, string input, string output)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo(ffmpeg)
		{
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true
		};
		foreach (string argument in $"-hide_banner -loglevel error -y -i".Split(' '))
		{
			startInfo.ArgumentList.Add(argument);
		}
		startInfo.ArgumentList.Add(input);
		foreach (string argument in EncoderArguments.Split(' '))
		{
			startInfo.ArgumentList.Add(argument);
		}
		startInfo.ArgumentList.Add(output);
		using Process process = Process.Start(startInfo) ?? throw new IOException($"Cannot start {ffmpeg}.");
		Task<string> errors = process.StandardError.ReadToEndAsync();
		process.StandardOutput.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new IOException($"ffmpeg cannot encode {input}: {errors.Result.Trim()}");
		}
	}
}
