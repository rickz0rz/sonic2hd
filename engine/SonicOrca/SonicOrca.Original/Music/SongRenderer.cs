using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SonicOrca.Original.Music;

/// <summary>
/// Plays a <see cref="SmpsSong"/> with a <see cref="SongPlayer"/>, and gives the sound as samples. The renderer finds
/// the loop of a song: the first frame where the state of the driver is the same as at an earlier frame. The sound
/// then ends one full loop after the start of the loop, and the loop point is the start of the second loop. So the
/// end of the sound continues into the loop point without a click.
/// </summary>
public static class SongRenderer
{
	/// <summary>The output sample rate. The music of the HD game is 48000 Hz.</summary>
	public const int SampleRate = 48000;

	/// <summary>The longest time that the renderer plays a song while it looks for the loop, in seconds.</summary>
	private const double MaxSeconds = 900.0;

	/// <summary>The length of the fade at the end of a song with a loop, in samples (about 33 ms).</summary>
	private const int CrossfadeSamples = 1600;

	/// <summary>The length of a looping song is a multiple of this number of samples.</summary>
	private const int EncoderBlockSamples = 64;

	/// <summary>Plays a song.</summary>
	/// <param name="song">The song.</param>
	/// <param name="dacFolder">The folder of the DAC drums (sound/DAC of the disassembly), or null for no drums.</param>
	public static RenderedSong Render(SmpsSong song, string dacFolder)
	{
		Dictionary<DacSample, (byte[] Samples, int Rate)> drums = dacFolder != null ? LoadDrums(dacFolder) : new Dictionary<DacSample, (byte[], int)>();
		SongPlayer player = new SongPlayer(song, drums, SampleRate);
		SmpsDriver driver = player.Driver;
		int maxFrames = (int)(MaxSeconds * SmpsDriver.FrameRate);
		Dictionary<string, int> states = new Dictionary<string, int>();
		List<int> tickFrames = new List<int>();
		List<int> frameStarts = new List<int>();
		List<float> left = new List<float>();
		List<float> right = new List<float>();
		int loopPointFrame = -1;
		int endTick = -1;
		int endFrame = -1;
		for (int frame = 0; frame < maxFrames && frame != endFrame; frame++)
		{
			frameStarts.Add(left.Count);
			if (driver.NextFrameTicks)
			{
				int tick = tickFrames.Count;
				tickFrames.Add(frame);
				if (tick == endTick)
				{
					// One full loop of ticks after the loop point: the state is the same as at the loop point.
					break;
				}
				if (loopPointFrame < 0 && endFrame < 0)
				{
					string state = Convert.ToBase64String(driver.GetState());
					if (states.TryGetValue(state, out int earlierTick))
					{
						// The ticks from the earlier tick to this tick repeat. The sound ends after one more loop.
						loopPointFrame = frame;
						endTick = tick + (tick - earlierTick);
					}
					else
					{
						states[state] = tick;
					}
				}
			}
			int count = player.PlayFrame();
			ReadOnlySpan<float> l = player.Left;
			ReadOnlySpan<float> r = player.Right;
			for (int i = 0; i < count; i++)
			{
				left.Add(l[i]);
				right.Add(r[i]);
			}
			if (loopPointFrame < 0 && endFrame < 0 && !driver.IsPlaying)
			{
				endFrame = frame + 1 + (int)(SongPlayer.TailSeconds * SmpsDriver.FrameRate);
			}
		}
		int length = left.Count;
		int? loopSample = null;
		if (loopPointFrame >= 0)
		{
			loopSample = frameStarts[loopPointFrame];
			// The Vorbis encoder of ffmpeg pads the end to a multiple of 64 samples, and the padding would play before
			// each loop. The end moves back by up to 63 samples, which is less than 1.5 ms.
			length -= length % EncoderBlockSamples;
			// The tempo counter can be at a different value at the end and at the loop point, so the timing of the
			// notes can differ by one frame. The end fades into the sound before the loop point, which continues
			// into the loop point exactly.
			int fade = Math.Min(CrossfadeSamples, loopSample.Value);
			for (int i = 0; i < fade; i++)
			{
				float weight = (i + 1) / (float)fade;
				int target = length - fade + i;
				int source = loopSample.Value - fade + i;
				left[target] += (left[source] - left[target]) * weight;
				right[target] += (right[source] - right[target]) * weight;
			}
		}
		float[] interleaved = new float[length * 2];
		for (int i = 0; i < length; i++)
		{
			interleaved[i * 2] = left[i];
			interleaved[i * 2 + 1] = right[i];
		}
		return new RenderedSong(interleaved, SampleRate, loopSample);
	}

	/// <summary>Reads the DAC drums from the folder sound/DAC of the disassembly.</summary>
	public static Dictionary<DacSample, (byte[] Samples, int Rate)> LoadDrums(string folder)
	{
		Dictionary<DacSample, (byte[] Samples, int Rate)> drums = new Dictionary<DacSample, (byte[], int)>();
		foreach (DacSample sample in Enum.GetValues<DacSample>())
		{
			drums[sample] = ReadWave8(Path.Combine(folder, sample + ".wav"));
		}
		return drums;
	}

	/// <summary>Reads a WAVE file of 8-bit unsigned mono samples.</summary>
	private static (byte[] Samples, int Rate) ReadWave8(string path)
	{
		using BinaryReader reader = new BinaryReader(File.OpenRead(path));
		if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
		{
			throw new InvalidDataException($"{path} is not a WAVE file.");
		}
		reader.ReadInt32();
		reader.ReadBytes(4);
		int rate = 0;
		while (reader.BaseStream.Position < reader.BaseStream.Length)
		{
			string id = Encoding.ASCII.GetString(reader.ReadBytes(4));
			int size = reader.ReadInt32();
			if (id == "fmt ")
			{
				byte[] format = reader.ReadBytes(size);
				int channels = BitConverter.ToInt16(format, 2);
				rate = BitConverter.ToInt32(format, 4);
				int bits = BitConverter.ToInt16(format, 14);
				if (channels != 1 || bits != 8)
				{
					throw new InvalidDataException($"{path} is not 8-bit mono.");
				}
			}
			else if (id == "data")
			{
				return (reader.ReadBytes(size), rate);
			}
			else
			{
				reader.ReadBytes(size + (size & 1));
			}
		}
		throw new InvalidDataException($"{path} has no samples.");
	}
}

/// <summary>The sound of a song: interleaved stereo samples, and the loop point in samples (null for no loop).</summary>
public sealed record RenderedSong(float[] Samples, int SampleRate, int? LoopSample)
{
	/// <summary>The number of samples of each channel.</summary>
	public int Length => Samples.Length / 2;

	/// <summary>Writes the sound as a 16-bit stereo WAVE file.</summary>
	public void WriteWave(string path)
	{
		using BinaryWriter writer = new BinaryWriter(File.Create(path));
		int dataSize = Samples.Length * 2;
		writer.Write(Encoding.ASCII.GetBytes("RIFF"));
		writer.Write(36 + dataSize);
		writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
		writer.Write(16);
		writer.Write((short)1);
		writer.Write((short)2);
		writer.Write(SampleRate);
		writer.Write(SampleRate * 4);
		writer.Write((short)4);
		writer.Write((short)16);
		writer.Write(Encoding.ASCII.GetBytes("data"));
		writer.Write(dataSize);
		foreach (float sample in Samples)
		{
			writer.Write((short)Math.Clamp((int)Math.Round(sample * 32767f), -32768, 32767));
		}
	}
}
