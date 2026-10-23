using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Menu;

/// <summary>
/// A full screen colour overlay that fades in or out over a number of updates.
/// </summary>
public class FadeTransition
{
	private const double ScreenWidth = 1920.0;

	private const double ScreenHeight = 1080.0;

	private bool _decreasingOpacity;

	public double Opacity { get; private set; }

	public Colour Colour { get; set; }

	public int Duration { get; set; }

	public int UpdatesRemaining { get; set; }

	public bool Finished => UpdatesRemaining <= 0;

	public FadeTransition(int duration)
		: this(Colours.Black, duration)
	{
	}

	public FadeTransition(Colour colour, int duration)
	{
		Colour = colour;
		Duration = duration;
	}

	public void Clear()
	{
		Opacity = 0.0;
		UpdatesRemaining = 0;
	}

	public void Set()
	{
		Opacity = 1.0;
		UpdatesRemaining = 0;
	}

	/// <summary>
	/// Starts to fade the overlay out, so that the scene below becomes visible.
	/// </summary>
	public void BeginFadeIn()
	{
		_decreasingOpacity = true;
		UpdatesRemaining = Duration;
		Update();
	}

	/// <summary>
	/// Starts to fade the overlay in, so that the scene below becomes hidden.
	/// </summary>
	public void BeginFadeOut()
	{
		_decreasingOpacity = false;
		UpdatesRemaining = Duration;
		Update();
	}

	public void Update()
	{
		if (UpdatesRemaining > 0)
		{
			Opacity = UpdatesRemaining / (float)Duration;
			if (!_decreasingOpacity)
			{
				Opacity = 1.0 - Opacity;
			}
			UpdatesRemaining--;
		}
	}

	public void Draw(Renderer renderer)
	{
		I2dRenderer i2dRenderer = renderer.Get2dRenderer();
		Colour colour = Colour;
		colour.Alpha = (byte)(Opacity * 255.0);
		i2dRenderer.BlendMode = BlendMode.Alpha;
		i2dRenderer.RenderQuad(colour, new Rectangle(0.0, 0.0, ScreenWidth, ScreenHeight));
	}
}
