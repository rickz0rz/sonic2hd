using System;
using System.Collections.Generic;
using System.IO;
using SonicOrca.Extensions;

namespace SonicOrca.Audio;

public class SampleInstance : IDisposable, ISampleProvider
{
	private readonly AudioContext _audioAdapter;

	private readonly Sample _sample;

	private Stream _sampleInputStream;

	private byte[] _lastReadBytes;

	private int? _loopSampleIndex;

	public double Volume { get; set; }

	public double Pan { get; set; }

	public SampleInstanceClassification Classification { get; set; } = SampleInstanceClassification.Sound;

	public Sample Sample => _sample;

	public bool Playing { get; private set; }

	public int SampleIndex { get; private set; }

	public double Position => (double)SampleIndex / (double)_sample.SampleRate;

	public IReadOnlyList<byte> LastReadBytes => _lastReadBytes;

	private int? LoopSampleIndex
	{
		get
		{
			return _loopSampleIndex;
		}
		set
		{
			if (_loopSampleIndex != value)
			{
				_loopSampleIndex = value;
				CreateSampleInputStream();
			}
		}
	}

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

	public SampleInstance(SonicOrcaGameContext context, SampleInfo sampleInfo)
		: this(context.Audio, sampleInfo.Sample, sampleInfo.HasLoopPoint ? new int?(sampleInfo.LoopSampleIndex) : ((int?)null))
	{
	}

	public SampleInstance(AudioContext audioAdapter, SampleInfo sampleInfo)
		: this(audioAdapter, sampleInfo.Sample, sampleInfo.HasLoopPoint ? new int?(sampleInfo.LoopSampleIndex) : ((int?)null))
	{
	}

	public SampleInstance(SonicOrcaGameContext context, Sample sample, int? loopSampleIndex = null)
		: this(context.Audio, sample, loopSampleIndex)
	{
	}

	public SampleInstance(AudioContext audioAdapter, Sample sample, int? loopSampleIndex = null)
	{
		_audioAdapter = audioAdapter;
		_sample = sample;
		_loopSampleIndex = loopSampleIndex;
		CreateSampleInputStream();
		Volume = 1.0;
		_audioAdapter.RegisterSampleProvider(this);
	}

	private void CreateSampleInputStream()
	{
		_sampleInputStream = new SampleStream(_sample, _loopSampleIndex);
		if (_sample.SampleRate != 44100)
		{
			_sampleInputStream = new ResamplerStream(_sampleInputStream, _sample.SampleRate, 44100);
		}
	}

	public void Dispose()
	{
		_audioAdapter.UnregisterSampleProvider(this);
		_sampleInputStream.Dispose();
	}

	public void Play()
	{
		Playing = true;
	}

	public void Stop()
	{
		Playing = false;
	}

	public void SeekToStart()
	{
		SeekTo(0);
	}

	public void SeekToLoopPoint()
	{
		SeekTo(_loopSampleIndex ?? 0);
	}

	public void SeekTo(int sampleIndex)
	{
		_sampleInputStream.Position = _sample.GetPcmDataOffset(sampleIndex);
		SampleIndex = sampleIndex;
	}

	public void SeekTo(double time)
	{
		SeekTo((int)(time * (double)_sample.SampleRate));
	}

	public int Read(byte[] buffer, int offset, int count)
	{
		SampleIndex = _sample.GetSampleIndex(_sampleInputStream.Position);
		int num = _sampleInputStream.Read(buffer, offset, count);
		if (num == 0)
		{
			Playing = false;
		}
		_lastReadBytes = buffer.GetRange(0, num);
		return num;
	}
}
