using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// A short-lived visual effect, for example smoke or heat haze, that moves in a straight line.
/// </summary>
public class Particle
{
	public ParticleType Type { get; set; }

	public LevelLayer Layer { get; set; }

	public Vector2 Position { get; set; }

	public Vector2 Velocity { get; set; }

	public int Time { get; set; }

	public double Angle { get; set; }

	public double AngularVelocity { get; set; }

	public double Size { get; set; }

	public ITexture CustomTexture { get; set; }

	public Particle()
	{
		Size = 1.0;
	}

	public void Update()
	{
		Position += Velocity;
		Angle = MathX.WrapRadians(Angle + AngularVelocity);
		Time--;
	}

	public override string ToString()
	{
		return $"{Type} particle, Time = {Time} Position = {Position} Velocity = {Velocity}";
	}
}
