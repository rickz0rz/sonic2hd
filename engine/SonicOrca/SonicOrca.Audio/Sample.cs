using System;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

/// <summary>
/// A loaded block of PCM audio data and its format. It also has helpers to convert 16-bit stereo PCM to float samples.
/// </summary>
public class Sample : ILoadedResource, IDisposable
{
	/// <summary>
	/// The number of bytes in one stereo frame of 16-bit samples.
	/// </summary>
	private const int BytesPerStereoFrame = 4;

	/// <summary>
	/// The scale between a 16-bit sample and a float sample in the range -1 to 1.
	/// </summary>
	private const float SampleScale = 32767f;

	private readonly byte[] _pcmData;

	private readonly int _bitsPerSample;

	private readonly int _sampleRate;

	private readonly int _channels;

	public Resource Resource { get; set; }

	public byte[] PcmData => _pcmData;

	public int BitsPerSample => _bitsPerSample;

	public int SampleRate => _sampleRate;

	public int Channels => _channels;

	public int SampleCount => _pcmData.Length / (_bitsPerSample / 8 * _channels);

	public Sample(byte[] pcmData, int bitsPerSample, int sampleRate, int channels)
	{
		_pcmData = pcmData;
		_bitsPerSample = bitsPerSample;
		_sampleRate = sampleRate;
		_channels = channels;
	}

	public void OnLoaded()
	{
	}

	public void Dispose()
	{
	}

	public long GetPcmDataOffset(int sampleIndex)
	{
		return _bitsPerSample / 8 * _channels * (long)sampleIndex;
	}

	public int GetSampleIndex(long pcmDataOffset)
	{
		return (int)(pcmDataOffset / (_bitsPerSample / 8 * _channels));
	}

	public static int PCMToSamples(byte[] source, out float[] leftSamples, out float[] rightSamples)
	{
		leftSamples = new float[source.Length / BytesPerStereoFrame];
		rightSamples = new float[source.Length / BytesPerStereoFrame];
		int frameCount = 0;
		for (int i = 0; i < source.Length; i += BytesPerStereoFrame)
		{
			leftSamples[frameCount] = BitConverter.ToInt16(source, i) / SampleScale;
			rightSamples[frameCount] = BitConverter.ToInt16(source, i + 2) / SampleScale;
			frameCount++;
		}
		return frameCount;
	}

	public static byte[] SamplesToPCM(float[] leftSamples, float[] rightSamples)
	{
		byte[] pcmData = new byte[leftSamples.Length * BytesPerStereoFrame];
		int position = 0;
		for (int i = 0; i < leftSamples.Length; i++)
		{
			// A sample outside -1 to 1 is cut at the limit of 16 bits, so that it does not wrap to the other sign
			short left = (short)Math.Clamp(leftSamples[i] * SampleScale, short.MinValue, short.MaxValue);
			short right = (short)Math.Clamp(rightSamples[i] * SampleScale, short.MinValue, short.MaxValue);
			pcmData[position++] = (byte)(left & 0xFF);
			pcmData[position++] = (byte)(left >> 8);
			pcmData[position++] = (byte)(right & 0xFF);
			pcmData[position++] = (byte)(right >> 8);
		}
		return pcmData;
	}
}
