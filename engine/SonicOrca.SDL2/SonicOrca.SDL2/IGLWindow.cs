using System;
using System.Threading;
using SonicOrca.Geometry;

namespace SonicOrca.SDL2;

/// <summary>
/// What the GL backend needs from the window that owns the context. Implemented by the SDL2 window and by the
/// web build's canvas, which compiles the GL backend sources against a WebGL implementation of the GL calls.
/// </summary>
public interface IGLWindow
{
	/// <summary>The thread the GL context is current on; GL objects must be created there.</summary>
	Thread ContextThread { get; }

	Vector2i ClientSize { get; }

	/// <summary>Runs an action on the context thread at the next window update.</summary>
	void Dispatch(Action action);
}
