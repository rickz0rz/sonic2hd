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
/// Tests the music import: the SMPS assembler, the driver and the renderer with a small song, and the import of a
/// song from the Sonic 2 disassembly. The tests with the disassembly are skipped when it is not found.
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
