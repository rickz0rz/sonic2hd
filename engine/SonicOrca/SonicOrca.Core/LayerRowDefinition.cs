namespace SonicOrca.Core;

/// <summary>
/// The scroll settings of one row in a level layer, for example for parallax.
/// </summary>
public class LayerRowDefinition
{
	public int Width { get; set; }

	public int Height { get; set; }

	public int InitialOffset { get; set; }

	public double CurrentOffset { get; set; }

	public double Parallax { get; set; }

	public double Velocity { get; set; }

	public LayerRowDefinition()
	{
		Parallax = 1.0;
	}

	public void Animate()
	{
		CurrentOffset += Velocity;
	}
}
