using System;
using System.Collections.Generic;
using System.IO;
using SonicOrca.Audio;

namespace SonicOrca.Original.Music;

/// <summary>
/// A song that plays live on the emulated sound chips, in place of a recorded sample. The web build uses it, because
/// the browser cannot encode Ogg Vorbis: a song file is about 30 KB, and an uncompressed sample of a song is 20 MB.
/// </summary>
public sealed class SongSample : Sample
{
	/// <summary>The sample rate of the mixer. At this rate, the sound does not go through a resampler.</summary>
	public const int OutputSampleRate = 44100;

	private readonly SmpsSong _song;
	private readonly IReadOnlyDictionary<DacSample, (byte[] Samples, int Rate)> _drums;

	public SongSample(SmpsSong song, IReadOnlyDictionary<DacSample, (byte[] Samples, int Rate)> drums)
		: base(Array.Empty<byte>(), 16, OutputSampleRate, 2)
	{
		_song = song;
		_drums = drums;
	}

	/// <summary>The main tempo of the song.</summary>
	internal int Tempo => _song.Data[SmpsSong.MainTempoOffset];

	/// <summary>Starts the song. The loop point is not used, because the driver follows the loop of the song.</summary>
	public override Stream OpenStream(int? loopSampleIndex)
	{
		return new SongStream(StartPlayer);
	}

	internal SongPlayer StartPlayer()
	{
		return new SongPlayer(_song, _drums, OutputSampleRate);
	}
}
