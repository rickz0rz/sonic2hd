using System;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface IWaterRenderer : IRenderer, IDisposable
{
	double HueTarget { get; set; }

	double HueAmount { get; set; }

	double SaturationChange { get; set; }

	double LuminosityChange { get; set; }

	double WavePhase { get; set; }

	double NumWaves { get; set; }

	double WaveSize { get; set; }

	float Time { get; set; }

	/// <summary>The colours of the water gradient: for dark, middle and light parts of the picture under the water.</summary>
	Colour DarkColour { get; set; }

	Colour MiddleColour { get; set; }

	Colour LightColour { get; set; }

	void Render(Rectanglei bounds);
}
