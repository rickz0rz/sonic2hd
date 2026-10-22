using System;

namespace SonicOrca.Audio;

public abstract class SampleGenerator : IDisposable, ISampleProvider
{
	private readonly AudioContext _audioAdapter;

	public double Volume { get; set; }

	public double Pan { get; set; }

	public SampleInstanceClassification Classification { get; set; }

	public bool Playing { get; private set; }

	public double CalculatedVolume
	{
		get
		{
			double num = Volume;
			if (Classification == SampleInstanceClassification.Sound)
			{
				num *= _audioAdapter.SoundVolume;
			}
			else if (Classification == SampleInstanceClassification.Music)
			{
				num *= _audioAdapter.MusicVolume;
			}
			return num * _audioAdapter.Volume;
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
		for (int i = 0; i < count; i += 4)
		{
			byte[] bytes = BitConverter.GetBytes((short)(GetNextSample() * 32767.0));
			Array.Copy(bytes, 0, buffer, offset + i, 2);
			Array.Copy(bytes, 0, buffer, offset + i + 2, 2);
		}
		return count;
	}

	protected abstract double GetNextSample();
}
