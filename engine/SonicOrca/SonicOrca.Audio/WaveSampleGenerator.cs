using System;

namespace SonicOrca.Audio;

public class WaveSampleGenerator : SampleGenerator
{
	public enum WaveFunction
	{
		Sine,
		Square,
		Triangle,
		Sawtooth
	}

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
		_angle = MathX.WrapRadians(_angle + 0.00014247585730565955 * Frequency);
		return Function switch
		{
			WaveFunction.Sine => Math.Sin(_angle) * Amplitude, 
			WaveFunction.Square => (double)Math.Sign(_angle / Math.PI) * Amplitude, 
			WaveFunction.Triangle => 2.0 * Amplitude / Math.PI * Math.Asin(Math.Sin(_angle)), 
			WaveFunction.Sawtooth => 2.0 * Amplitude / Math.PI * Math.Atan(Math.Cos(_angle) / Math.Sin(_angle)), 
			_ => 0.0, 
		};
	}
}
