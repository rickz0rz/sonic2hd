using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SonicOrca.HelperLibraries.OggVorbis;
using SonicOrca.Original.Music;
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
