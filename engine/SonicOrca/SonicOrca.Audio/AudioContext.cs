using System.Collections.Generic;

namespace SonicOrca.Audio;

public abstract class AudioContext
{
	private readonly List<SampleInstance> _fireAndForgetSoundInstances = new List<SampleInstance>();

	public ISampleMixer Mixer { get; set; }

	public double Volume { get; set; }

	public double MusicVolume { get; set; }

	public double SoundVolume { get; set; }

	protected AudioContext()
	{
		Volume = 1.0;
		MusicVolume = 0.3;
		SoundVolume = 1.0;
		Mixer = new BasicSampleMixer();
	}

	public virtual void RegisterSampleProvider(ISampleProvider sampleProvider)
	{
	}

	public virtual void UnregisterSampleProvider(ISampleProvider sampleProvider)
	{
	}

	public virtual void Update()
	{
		_fireAndForgetSoundInstances.RemoveAll((SampleInstance x) => !x.Playing);
	}

	public void PlaySound(Sample sample)
	{
		SampleInstance sampleInstance = new SampleInstance(this, sample);
		sampleInstance.Play();
		_fireAndForgetSoundInstances.Add(sampleInstance);
	}

	public void StopAll()
	{
		foreach (SampleInstance fireAndForgetSoundInstance in _fireAndForgetSoundInstances)
		{
			fireAndForgetSoundInstance.Stop();
		}
	}
}
