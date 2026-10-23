using System;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

#nullable disable
namespace S2HD.Menu;

/// <summary>
/// Shows one menu page, reads the input for it, and tells the host when the player goes back or forward.
/// </summary>
internal interface IMenuViewPresenter
{
	Rectanglei Bounds { get; set; }

	event EventHandler NavigateBack;

	event EventHandler<NavigateNextEventArgs> NavigateNext;

	void Update();

	void HandleInput();

	void Draw(Renderer renderer);
}
