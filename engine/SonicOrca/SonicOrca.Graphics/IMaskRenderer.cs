using System;
using SonicOrca.Geometry;

namespace SonicOrca.Graphics;

public interface IMaskRenderer : IDisposable
{
	ITexture Texture { get; set; }

	Rectanglei Source { get; set; }

	Rectanglei Destination { get; set; }

	ITexture MaskTexture { get; set; }

	Rectanglei MaskSource { get; set; }

	Rectanglei MaskDestination { get; set; }

	BlendMode BlendMode { get; set; }

	Colour Colour { get; set; }

	Matrix4 IntersectionModelMatrix { get; set; }

	Matrix4 TargetModelMatrix { get; set; }

	Matrix4 MaskModelMatrix { get; set; }

	IDisposable BeginMatixState();

	void Render(bool maskColorMultiply = false);
}
