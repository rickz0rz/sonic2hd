using System;
using System.Collections.Generic;
using System.IO;
using SonicOrca.Extensions;

namespace SonicOrca.Audio;

/// <summary>
/// A playing instance of a <see cref="Sample"/>. It can loop and it resamples the data to the output sample rate.
/// </summary>
public class SampleInstance : IDisposable, ISampleProvider
{
	private const int OutputSampleRate = 44100;

	private readonly AudioContext _audioAdapter;

	private Sample _sample;

	private Stream _sampleInputStream;

	private byte[] _lastReadBytes;

	private int? _loopSampleIndex;

	public double Volume { get; set; }

	public double Pan { get; set; }

	public SampleInstanceClassification Classification { get; set; } = SampleInstanceClassification.Sound;

	public Sample Sample => _sample;

	public bool Playing { get; private set; }

	public int SampleIndex { get; private set; }

	public double Position => SampleIndex / (double)_sample.SampleRate;

	public IReadOnlyList<byte> LastReadBytes => _lastReadBytes;

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

	public SampleInstance(SonicOrcaGameContext context, SampleInfo sampleInfo)
		: this(context.Audio, sampleInfo.Sample, sampleInfo.HasLoopPoint ? sampleInfo.LoopSampleIndex : null)
	{
	}

	public SampleInstance(AudioContext audioAdapter, SampleInfo sampleInfo)
		: this(audioAdapter, sampleInfo.Sample, sampleInfo.HasLoopPoint ? sampleInfo.LoopSampleIndex : null)
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
		_sampleInputStream = _sample.OpenStream(_loopSampleIndex);
		if (_sample.SampleRate != OutputSampleRate)
		{
			_sampleInputStream = new ResamplerStream(_sampleInputStream, _sample.SampleRate, OutputSampleRate);
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
		SeekTo((int)(time * _sample.SampleRate));
	}

	/// <summary>
	/// Plays the rest of a song that plays live at the tempo of a copy of the song (see <see cref="ITempoStream"/>).
	/// Returns false if the stream cannot change its tempo.
	/// </summary>
	public bool TryChangeTempo(SampleInfo copy)
	{
		if (_sampleInputStream is ITempoStream stream && stream.ChangeTempo(copy.Sample))
		{
			_sample = copy.Sample;
			return true;
		}
		return false;
	}

	public int Read(byte[] buffer, int offset, int count)
	{
		SampleIndex = _sample.GetSampleIndex(_sampleInputStream.Position);
		int bytesRead = _sampleInputStream.Read(buffer, offset, count);
		if (bytesRead == 0)
		{
			Playing = false;
		}
		_lastReadBytes = buffer.GetRange(offset, bytesRead);
		return bytesRead;
	}
}
