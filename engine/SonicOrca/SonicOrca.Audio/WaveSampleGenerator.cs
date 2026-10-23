using System;

namespace SonicOrca.Audio;

/// <summary>
/// Sample generator that makes a sine, square, triangle or sawtooth wave.
/// </summary>
public class WaveSampleGenerator : SampleGenerator
{
	public enum WaveFunction
	{
		Sine,
		Square,
		Triangle,
		Sawtooth
	}

	/// <summary>
	/// The angle change for each sample at 1 Hz. The value is 2 pi divided by the 44100 Hz sample rate.
	/// </summary>
	private const double AngleStepPerHertz = 0.00014247585730565955;

	private double _angle;

	public WaveFunction Function { get; set; }

	public double Amplitude { get; set; }

	public double Frequency { get; set; }

	public WaveSampleGenerator(AudioContext audioAdapter)
		: base(audioAdapter)
	{
		Function = WaveFunction.Sine;
		Amplitude = 1.0;
		Frequency = 256.0;
	}

	protected override double GetNextSample()
	{
		_angle = MathX.WrapRadians(_angle + AngleStepPerHertz * Frequency);
		return Function switch
		{
			WaveFunction.Sine => Math.Sin(_angle) * Amplitude,
			WaveFunction.Square => Math.Sign(_angle / Math.PI) * Amplitude,
			WaveFunction.Triangle => 2.0 * Amplitude / Math.PI * Math.Asin(Math.Sin(_angle)),
			WaveFunction.Sawtooth => 2.0 * Amplitude / Math.PI * Math.Atan(Math.Cos(_angle) / Math.Sin(_angle)),
			_ => 0.0,
		};
	}
}
