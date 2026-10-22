using System;
using SonicOrca.Geometry;
using SonicOrca.Graphics.V2.Animation;

namespace SonicOrca.Graphics;

public interface IObjectRenderer : IRenderer, IDisposable
{
	Colour AdditiveColour { get; set; }

	Colour MultiplyColour { get; set; }

	BlendMode BlendMode { get; set; }

	Rectangle ClipRectangle { get; set; }

	Matrix4 ModelMatrix { get; set; }

	bool EmitsLight { get; set; }

	bool Shadow { get; set; }

	int Filter { get; set; }

	double FilterAmount { get; set; }

	ITexture Texture { get; set; }

	Vector2 Scale { get; set; }

	IDisposable BeginMatixState();

	void SetDefault();

	void Render(AnimationInstance animationInstance, bool flipX = false, bool flipY = false);

	void Render(AnimationInstance animationInstance, Vector2 destination, bool flipX = false, bool flipY = false);

	void Render(CompositionInstance compositionInstance, Vector2 destination, bool flipX = false, bool flipY = false);

	void Render(Vector2 destination = default(Vector2), bool flipX = false, bool flipY = false);

	void Render(Rectangle destination, bool flipX = false, bool flipY = false);

	void Render(Rectangle source, Vector2 destination, bool flipX = false, bool flipY = false);

	void Render(Rectangle source, Rectangle destination, bool flipX = false, bool flipY = false);
}
