using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// A tile that a level layer can draw.
/// </summary>
public interface ITile
{
	int Id { get; }

	bool Animated { get; }

	void Animate();

	void Draw(Renderer renderer, int flags, int x, int y);

	void Draw(Renderer renderer, int flags, Rectanglei source, Rectanglei destination);

	void Draw(I2dRenderer g, int flags, Rectanglei source, Rectanglei destination);

	void Draw(ITileRenderer tileRenderer, int flags, Rectanglei source, Rectanglei destination);
}
