using System;

namespace SonicOrca.Audio;

/// <summary>
/// Base class for a sample provider that makes its samples with a function. It writes the same value to the two channels.
/// </summary>
public abstract class SampleGenerator : IDisposable, ISampleProvider
{
	/// <summary>
	/// The number of bytes in one stereo frame of 16-bit samples.
	/// </summary>
	private const int BytesPerFrame = 4;

	private const int BytesPerSample = 2;

	private readonly AudioContext _audioAdapter;

	public double Volume { get; set; }

	public double Pan { get; set; }

	public SampleInstanceClassification Classification { get; set; }

	public bool Playing { get; private set; }

	public double CalculatedVolume
	{
		get
		{
			double volume = Volume;
			if (Classification == SampleInstanceClassification.Sound)
			{
				volume *= _audioAdapter.SoundVolume;
			}
			else if (Classification == SampleInstanceClassification.Music)
			{
				volume *= _audioAdapter.MusicVolume;
			}
			return volume * _audioAdapter.Volume;
		}
	}

	public SampleGenerator(AudioContext audioAdapter)
	{
		_audioAdapter = audioAdapter;
		Volume = 1.0;
		_audioAdapter.RegisterSampleProvider(this);
	}

	public void Dispose()
	{
		_audioAdapter.UnregisterSampleProvider(this);
	}

	public void Play()
	{
		Playing = true;
	}

	public void Stop()
	{
		Playing = false;
	}

	public int Read(byte[] buffer, int offset, int count)
	{
		for (int i = 0; i < count; i += BytesPerFrame)
		{
			byte[] bytes = BitConverter.GetBytes((short)(GetNextSample() * 32767.0));
			Array.Copy(bytes, 0, buffer, offset + i, BytesPerSample);
			Array.Copy(bytes, 0, buffer, offset + i + BytesPerSample, BytesPerSample);
		}
		return count;
	}

	protected abstract double GetNextSample();
}
