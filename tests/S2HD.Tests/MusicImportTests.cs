using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SonicOrca.Audio;
using SonicOrca.HelperLibraries.OggVorbis;
using SonicOrca.Original.Music;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>
/// Tests the music import: the SMPS assembler, the driver and the renderer with a small song and a small sound effect,
/// and the import of a song and a sound effect from the Sonic 2 disassembly. The tests with the disassembly are
/// skipped when it is not found.
/// </summary>
public sealed class MusicImportTests
{
	/// <summary>A song with one FM channel that plays A4 (440 Hz) again and again, with a sine voice.</summary>
	private const string SineSong = """
		Test_Header:
			smpsHeaderStartSong 2
			smpsHeaderVoice     Test_Voices
			smpsHeaderChan      $02, $00
			smpsHeaderTempo     $01, $80

			smpsHeaderDAC       Test_DAC
			smpsHeaderFM        Test_FM1, $00, $00

		; DAC data
		Test_DAC:
			smpsStop

		; FM1 data
		Test_FM1:
			smpsSetvoice        $00
		Test_Loop:
			dc.b	nA4, $18, nRst, $06
			smpsJump            Test_Loop

		Test_Voices:
		;	Voice $00
			smpsVcAlgorithm     $07
			smpsVcFeedback      $00
			smpsVcUnusedBits    $00
			smpsVcDetune        $00, $00, $00, $00
			smpsVcCoarseFreq    $01, $01, $01, $01
			smpsVcRateScale     $00, $00, $00, $00
			smpsVcAttackRate    $1F, $1F, $1F, $1F
			smpsVcAmpMod        $00, $00, $00, $00
			smpsVcDecayRate1    $00, $00, $00, $00
			smpsVcDecayRate2    $00, $00, $00, $00
			smpsVcDecayLevel    $00, $00, $00, $00
			smpsVcReleaseRate   $0F, $0F, $0F, $0F
			smpsVcTotalLevel    $7F, $7F, $7F, $00
		""";

	/// <summary>A sound effect on FM5 that plays A4 for 48 frames, with the sine voice of <see cref="SineSong"/>.</summary>
	private static readonly string SineSound = """
		Test_Header:
			smpsHeaderStartSong 2
			smpsHeaderVoice     Test_Voices
			smpsHeaderTempoSFX  $01
			smpsHeaderChanSFX   $01

			smpsHeaderSFXChannel cFM5, Test_FM5, $00, $00

		; FM5 data
		Test_FM5:
			smpsSetvoice        $00
			dc.b	nA4, $30
			smpsStop

		""" + SineSong.Substring(SineSong.IndexOf("Test_Voices:", StringComparison.Ordinal));

	[Fact]
	public void Assemble_WritesTheHeaderAndTheTrackData()
	{
		SmpsSong song = SmpsSong.FromText(SineSong);

		byte[] data = song.Data;
		Assert.Equal(new byte[] { 2, 0, 1, 0x80 }, data.Skip(2).Take(4).ToArray());
		Assert.Equal(song.Labels["Test_Voices"], data[0] | (data[1] << 8));
		Assert.Equal(song.Labels["Test_DAC"], data[6] | (data[7] << 8));
		Assert.Equal(song.Labels["Test_FM1"], data[10] | (data[11] << 8));
		int loop = song.Labels["Test_Loop"];
		// nA4 is $BA. smpsJump is $F6 and a pointer to Test_Loop.
		Assert.Equal(new byte[] { 0xBA, 0x18, 0x80, 0x06, 0xF6, (byte)loop, (byte)(loop >> 8) }, data.Skip(loop).Take(7).ToArray());
		Assert.Equal(25, data.Length - song.Labels["Test_Voices"]);
	}

	[Fact]
	public void Render_PlaysA4AndFindsTheLoop()
	{
		RenderedSong rendered = SongRenderer.Render(SmpsSong.FromText(SineSong), null);

		Assert.NotNull(rendered.LoopSample);
		Assert.InRange(rendered.Length / (double)rendered.SampleRate, 0.5, 10.0);
		// Count the zero crossings in the first note: tempo $80 plays the 24 ticks of the note in 48 frames.
		int start = rendered.SampleRate / 20;
		int count = rendered.SampleRate / 2;
		int crossings = 0;
		for (int i = start + 1; i < start + count; i++)
		{
			if ((rendered.Samples[(i - 1) * 2] < 0) != (rendered.Samples[i * 2] < 0))
			{
				crossings++;
			}
		}
		double frequency = crossings / 2.0 / (count / (double)rendered.SampleRate);
		Assert.InRange(frequency, 435.0, 445.0);
	}

	[Fact]
	public void Assemble_SoundEffect_WritesTheHeaderOfZPlaySound()
	{
		SmpsSong sound = SmpsSong.FromText(SineSound);

		byte[] data = sound.Data;
		Assert.True(sound.IsSoundEffect);
		Assert.False(SmpsSong.FromText(SineSong).IsSoundEffect);
		Assert.Equal(sound.Labels["Test_Voices"], data[0] | (data[1] << 8));
		// The tempo divider and the number of channels, then the playback control and the channel (cFM5 is 5)
		Assert.Equal(new byte[] { 1, 1, 0x80, 5 }, data.Skip(2).Take(4).ToArray());
		Assert.Equal(sound.Labels["Test_FM5"], data[6] | (data[7] << 8));
	}

	[Fact]
	public void Render_SoundEffect_PlaysA4WithoutTheTempoOfASong()
	{
		RenderedSong rendered = SongRenderer.Render(SmpsSong.FromText(SineSound), null);

		Assert.Null(rendered.LoopSample);
		// The note takes 48 frames (0.8 s), then the tail is 2 s
		Assert.InRange(rendered.Length / (double)rendered.SampleRate, 2.7, 2.9);
		short[] samples = rendered.Samples.Select(sample => (short)(sample * 32767f)).ToArray();
		Assert.InRange(MeasureFrequency(samples, rendered.SampleRate / 20, rendered.SampleRate / 2, rendered.SampleRate), 435.0, 445.0);
		// The note ends after 0.8 s, and only its release is left
		float notePeak = rendered.Samples.Skip(2 * rendered.SampleRate / 2).Take(rendered.SampleRate / 5).Max(Math.Abs);
		float endPeak = rendered.Samples.Skip(2 * rendered.SampleRate * 2).Take(rendered.SampleRate / 5).Max(Math.Abs);
		Assert.True(notePeak > 0.1f, $"The note is silent ({notePeak}).");
		Assert.True(endPeak < 0.01f, $"The note continues ({endPeak}).");
	}

	[Fact]
	public void SongSample_SoundEffect_KeepsItsTypeInTheFile()
	{
		SongSample sample = LoadSongSample(SineSound);
		using Stream stream = sample.OpenStream(null);

		short[] samples = ReadSamples(stream, sample.SampleRate / 2);

		Assert.InRange(MeasureFrequency(samples, sample.SampleRate / 20, sample.SampleRate / 3, sample.SampleRate), 435.0, 445.0);
	}

	[Fact]
	public void Render_SuperTransform_PlaysOnceAndStaysBelowFullScale()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();
		SmpsSong sound = SmpsSong.FromFile(Path.Combine(disassembly, MusicImport.SoundFolder, "DF - Super Transform.asm"));

		RenderedSong rendered = SongRenderer.Render(sound, null);

		Assert.True(sound.IsSoundEffect);
		Assert.Null(rendered.LoopSample);
		Assert.InRange(rendered.Length / (double)rendered.SampleRate, 2.5, 6.0);
		float peak = rendered.Samples.Max(Math.Abs);
		Assert.InRange(peak, 0.1f, 1.0f);
	}

	[Fact]
	public void Render_SuperSonic_Loops()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();
		SmpsSong song = SmpsSong.FromFile(Path.Combine(disassembly, MusicImport.MusicFolder, "96 - Super Sonic.asm"));

		RenderedSong rendered = SongRenderer.Render(song, Path.Combine(disassembly, MusicImport.DacFolder));

		Assert.NotNull(rendered.LoopSample);
		Assert.InRange(rendered.Length / (double)rendered.SampleRate, 10.0, 300.0);
	}

	[Fact]
	public void Render_SuperSonicAtTheSpeedShoesTempo_LoopsSooner()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();
		SmpsSong song = SmpsSong.FromFile(Path.Combine(disassembly, MusicImport.MusicFolder, "96 - Super Sonic.asm"));
		var spedUp = MusicImport.SpedUpSongs.Single(s => s.File == "96 - Super Sonic.asm");

		RenderedSong normal = SongRenderer.Render(song, Path.Combine(disassembly, MusicImport.DacFolder));
		RenderedSong fast = SongRenderer.Render(song.WithTempo(spedUp.Tempo), Path.Combine(disassembly, MusicImport.DacFolder));

		// The song moves on in 250 of 256 frames at tempo $FA, and in 255 of 256 frames at tempo $FF
		Assert.Equal(0xFF, spedUp.Tempo);
		Assert.InRange(fast.LoopSample.Value / (double)normal.LoopSample.Value, 250.0 / 255.0 - 0.005, 250.0 / 255.0 + 0.005);
	}

	[Fact]
	public void Render_AquaticRuin_LoopsAndStaysBelowFullScale()
	{
		string disassembly = SpecialStageImportTests.FindDisassembly();
		SmpsSong song = SmpsSong.FromFile(Path.Combine(disassembly, MusicImport.MusicFolder, "87 - ARZ.asm"));

		RenderedSong rendered = SongRenderer.Render(song, Path.Combine(disassembly, MusicImport.DacFolder));

		Assert.NotNull(rendered.LoopSample);
		Assert.InRange(rendered.Length / (double)rendered.SampleRate, 30.0, 300.0);
		float peak = rendered.Samples.Max(Math.Abs);
		Assert.InRange(peak, 0.2f, 1.0f);
	}

	[Fact]
	public void Encode_TheGameDecodesTheSameNumberOfSamples()
	{
		string folder = Directory.CreateTempSubdirectory("s2hd-music-").FullName;
		try
		{
			RenderedSong rendered = SongRenderer.Render(SmpsSong.FromText(SineSong), null);
			string wave = Path.Combine(folder, "song.wav");
			string ogg = Path.Combine(folder, "song.ogg");
			rendered.WriteWave(wave);
			if (!RunFfmpeg($"-v error -y -i {wave} -c:a vorbis -strict experimental -b:a 160k {ogg}"))
			{
				Assert.Skip("ffmpeg is not found.");
			}

			using FileStream input = File.OpenRead(ogg);
			using OggDecodeStream decoded = new OggDecodeStream(input, skipWavHeader: true);

			// 16-bit stereo: 4 bytes for each sample. The loop point is a sample index, so the samples must line up.
			byte[] bytes = new byte[decoded.Length];
			decoded.ReadExactly(bytes);
			short[] samples = new short[bytes.Length / 2];
			Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
			int best = int.MinValue;
			double bestError = double.MaxValue;
			for (int lag = -8; lag <= 8; lag++)
			{
				double error = 0;
				for (int i = 10000; i < 20000; i++)
				{
					double difference = samples[(i + lag) * 2] / 32768.0 - rendered.Samples[i * 2];
					error += difference * difference;
				}
				if (error < bestError)
				{
					bestError = error;
					best = lag;
				}
			}
			Assert.Equal(0, best);
			Assert.Equal(rendered.Length, samples.Length / 2);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public void SongSample_LoadsTheFileAndPlaysA4Live()
	{
		SongSample sample = LoadSongSample(SineSong);
		using Stream stream = sample.OpenStream(null);

		short[] samples = ReadSamples(stream, sample.SampleRate);

		Assert.Equal(SongSample.OutputSampleRate, sample.SampleRate);
		Assert.Equal(sample.SampleRate * 2, samples.Length);
		Assert.InRange(MeasureFrequency(samples, sample.SampleRate / 20, sample.SampleRate / 2, sample.SampleRate), 435.0, 445.0);
	}

	[Fact]
	public void SongSample_SeekBackStartsTheSongAgain()
	{
		using Stream stream = LoadSongSample(SineSong).OpenStream(null);
		short[] first = ReadSamples(stream, 20000);

		stream.Position = 0;
		short[] again = ReadSamples(stream, 20000);
		stream.Position = 4 * 5000;
		short[] skipped = ReadSamples(stream, 15000);

		Assert.Equal(first, again);
		Assert.Equal(first.Skip(2 * 5000), skipped);
	}

	[Fact]
	public void WithTempo_ChangesOnlyTheMainTempo()
	{
		SmpsSong song = SmpsSong.FromText(SineSong);

		SmpsSong fast = song.WithTempo(0xFF);

		Assert.Equal(0x80, song.Data[SmpsSong.MainTempoOffset]);
		Assert.Equal(0xFF, fast.Data[SmpsSong.MainTempoOffset]);
		Assert.Equal(song.Data.Where((_, i) => i != SmpsSong.MainTempoOffset), fast.Data.Where((_, i) => i != SmpsSong.MainTempoOffset));
	}

	[Fact]
	public void SongSample_ChangeTempo_ContinuesAtTheNewTempoAndStartsTheCopyAgain()
	{
		SongSample normal = LoadSongSample(SineSong);
		SongSample fast = LoadSongSample(SineSong.Replace("smpsHeaderTempo     $01, $80", "smpsHeaderTempo     $01, $FF"));
		using Stream stream = normal.OpenStream(null);
		using Stream normalStream = normal.OpenStream(null);
		using Stream fastStream = fast.OpenStream(null);
		short[] start = ReadSamples(stream, 20000);

		bool changed = ((ITempoStream)stream).ChangeTempo(fast);
		short[] changedRest = ReadSamples(stream, 3 * SongSample.OutputSampleRate);
		short[] normalRest = ReadSamples(normalStream, 20000 + 3 * SongSample.OutputSampleRate).Skip(2 * 20000).ToArray();
		stream.Position = 0;
		short[] again = ReadSamples(stream, 20000);

		Assert.True(changed);
		Assert.False(((ITempoStream)stream).ChangeTempo(new Sample(new byte[4], 16, SongSample.OutputSampleRate, 2)));
		Assert.Equal(ReadSamples(normal.OpenStream(null), 20000), start);
		Assert.NotEqual(normalRest, changedRest);
		Assert.Equal(ReadSamples(fastStream, 20000), again);
	}

	[Fact]
	public void SongSample_SongWithoutLoopEndsAfterItsTail()
	{
		string song = SineSong.Replace("smpsJump            Test_Loop", "smpsStop");
		using Stream stream = LoadSongSample(song).OpenStream(null);

		byte[] buffer = new byte[4096];
		long total = 0;
		int read;
		while ((read = stream.Read(buffer, 0, buffer.Length)) > 0 && total < 60L * 4 * SongSample.OutputSampleRate)
		{
			total += read;
		}

		// The note and the rest take 30 ticks at tempo $80 (60 frames), and the tail is 2 s
		Assert.InRange(total / 4.0 / SongSample.OutputSampleRate, 2.5, 4.0);
	}

	/// <summary>Writes a song as a .smps file and loads it with the resource type of the game.</summary>
	private static SongSample LoadSongSample(string text)
	{
		ResourceType.RegisterTypesFrom(typeof(SongSample).Assembly);
		string path = Path.GetTempFileName();
		try
		{
			SmpsResourceType.Write(path, SmpsSong.FromText(text), new Dictionary<DacSample, (byte[], int)>
			{
				[DacSample.Kick] = (new byte[] { 0x80, 0x90, 0x70 }, 8000)
			});
			using FileStream input = File.OpenRead(path);
			ResourceType type = ResourceType.FromIdentifier(ResourceTypeIdentifier.SampleSMPS);
			return (SongSample)type.LoadAsync(new ResourceLoadArgs(null, null, input)).GetAwaiter().GetResult();
		}
		finally
		{
			File.Delete(path);
		}
	}

	/// <summary>Reads 16-bit stereo samples: two values for each sample.</summary>
	private static short[] ReadSamples(Stream stream, int count)
	{
		byte[] bytes = new byte[count * 4];
		stream.ReadExactly(bytes);
		short[] samples = new short[count * 2];
		Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);
		return samples;
	}

	/// <summary>Measures the frequency of the left channel from its zero crossings.</summary>
	private static double MeasureFrequency(short[] samples, int start, int count, int sampleRate)
	{
		int crossings = 0;
		for (int i = start + 1; i < start + count; i++)
		{
			if ((samples[(i - 1) * 2] < 0) != (samples[i * 2] < 0))
			{
				crossings++;
			}
		}
		return crossings / 2.0 / (count / (double)sampleRate);
	}

	private static bool RunFfmpeg(string arguments)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo("ffmpeg") { UseShellExecute = false, RedirectStandardError = true };
			foreach (string argument in arguments.Split(' '))
			{
				startInfo.ArgumentList.Add(argument);
			}
			using Process process = Process.Start(startInfo);
			process.StandardError.ReadToEnd();
			process.WaitForExit();
			Assert.True(process.ExitCode == 0, "ffmpeg failed.");
			return true;
		}
		catch (Win32Exception)
		{
			return false;
		}
	}
}
