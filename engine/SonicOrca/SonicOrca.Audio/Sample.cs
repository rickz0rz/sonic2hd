using System;
using SonicOrca.Resources;

namespace SonicOrca.Audio;

public class Sample : ILoadedResource, IDisposable
{
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
		return (long)(_bitsPerSample / 8 * _channels) * (long)sampleIndex;
	}

	public int GetSampleIndex(long pcmDataOffset)
	{
		return (int)(pcmDataOffset / (_bitsPerSample / 8 * _channels));
	}

	public static int PCMToSamples(byte[] source, out float[] leftSamples, out float[] rightSamples)
	{
		leftSamples = new float[source.Length / 4];
		rightSamples = new float[source.Length / 4];
		int num = 0;
		for (int i = 0; i < source.Length; i += 4)
		{
			leftSamples[num] = (float)BitConverter.ToInt16(source, i) / 32767f;
			rightSamples[num] = (float)BitConverter.ToInt16(source, i + 2) / 32767f;
			num++;
		}
		return num;
	}

	public static byte[] SamplesToPCM(float[] leftSamples, float[] rightSamples)
	{
		byte[] array = new byte[leftSamples.Length * 4];
		int num = 0;
		for (int i = 0; i < leftSamples.Length; i++)
		{
			short num2 = (short)(leftSamples[i] * 32767f);
			short num3 = (short)(rightSamples[i] * 32767f);
			array[num++] = (byte)(num2 & 0xFF);
			array[num++] = (byte)(num2 >> 8);
			array[num++] = (byte)(num3 & 0xFF);
			array[num++] = (byte)(num3 >> 8);
		}
		return array;
	}
}
