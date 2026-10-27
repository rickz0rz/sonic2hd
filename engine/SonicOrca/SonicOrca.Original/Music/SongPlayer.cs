using System;
using System.Collections.Generic;

namespace SonicOrca.Original.Music;

/// <summary>
/// Plays a <see cref="SmpsSong"/> one frame at a time with the <see cref="SmpsDriver"/> and the chip emulators.
/// <see cref="SongRenderer"/> uses it to make a sample, and <see cref="SongStream"/> uses it to play a song live.
/// </summary>
public sealed class SongPlayer
{
	/// <summary>The time that a song without a loop plays after its last track stops, in seconds.</summary>
	public const double TailSeconds = 2.0;

	/// <summary>The volume of the mix. It keeps the loudest songs below full scale.</summary>
	private const float Gain = 1.6f;

	private readonly IReadOnlyDictionary<DacSample, (byte[] Samples, int Rate)> _drums;
	private readonly int _sampleRate;
	private readonly double _samplesPerFrame;
	private readonly Ym2612 _fm;
	private readonly Sn76489 _psg;
	private readonly float[] _left;
	private readonly float[] _right;
	private byte[] _dac;
	private double _dacPosition;
	private double _dacStep;
	private int _count;
	private int _endFrame = -1;

	/// <summary>Starts a song.</summary>
	/// <param name="song">The song.</param>
	/// <param name="drums">The DAC drums: the 8-bit samples and the sample rate of each drum.</param>
	/// <param name="sampleRate">The output sample rate.</param>
	public SongPlayer(SmpsSong song, IReadOnlyDictionary<DacSample, (byte[] Samples, int Rate)> drums, int sampleRate)
	{
		_drums = drums;
		_sampleRate = sampleRate;
		_samplesPerFrame = sampleRate / SmpsDriver.FrameRate;
		_fm = new Ym2612(sampleRate);
		_psg = new Sn76489(sampleRate);
		_left = new float[(int)_samplesPerFrame + 2];
		_right = new float[(int)_samplesPerFrame + 2];
		Driver = new SmpsDriver(song, _fm, _psg);
	}

	/// <summary>The driver that plays the song.</summary>
	public SmpsDriver Driver { get; }

	/// <summary>The number of frames that the player played.</summary>
	public int Frame { get; private set; }

	/// <summary>True when all the tracks stopped and the tail after them played. A song with a loop does not end.</summary>
	public bool Finished => _endFrame >= 0 && Frame >= _endFrame;

	/// <summary>The left channel of the most recent frame.</summary>
	public ReadOnlySpan<float> Left => _left.AsSpan(0, _count);

	/// <summary>The right channel of the most recent frame.</summary>
	public ReadOnlySpan<float> Right => _right.AsSpan(0, _count);

	/// <summary>Plays one frame. <see cref="Left"/> and <see cref="Right"/> then give its samples.</summary>
	/// <returns>The number of samples of the frame.</returns>
	public int PlayFrame()
	{
		int frame = Frame;
		Driver.UpdateFrame();
		if (Driver.DacRequest is (DacSample sample, double scale) && _drums.TryGetValue(sample, out (byte[] Samples, int Rate) drum))
		{
			(byte[] samples, int rate) = drum;
			_dac = samples;
			_dacPosition = 0;
			_dacStep = SmpsDriver.GetDacRate(rate, scale) / _sampleRate;
		}
		_count = (int)((frame + 1) * _samplesPerFrame) - (int)(frame * _samplesPerFrame);
		Span<float> l = _left.AsSpan(0, _count);
		Span<float> r = _right.AsSpan(0, _count);
		l.Clear();
		r.Clear();
		if (_dac != null)
		{
			// The Z80 writes the drum to the DAC between the frames: one write before each output sample.
			for (int i = 0; i < _count; i++)
			{
				int index = (int)_dacPosition;
				if (_dac != null && index < _dac.Length)
				{
					_fm.Write(0, 0x2A, _dac[index]);
					_dacPosition += _dacStep;
				}
				else
				{
					_dac = null;
				}
				_fm.Render(l.Slice(i, 1), r.Slice(i, 1));
			}
		}
		else
		{
			_fm.Render(l, r);
		}
		_psg.Render(l, r);
		for (int i = 0; i < _count; i++)
		{
			l[i] *= Gain;
			r[i] *= Gain;
		}
		Frame = frame + 1;
		if (_endFrame < 0 && !Driver.IsPlaying)
		{
			_endFrame = Frame + (int)(TailSeconds * SmpsDriver.FrameRate);
		}
		return _count;
	}
}
