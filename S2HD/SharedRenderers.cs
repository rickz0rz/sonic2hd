using System;
using SonicOrca.Graphics;

namespace S2HD;

/// <summary>
/// Renderers that all game states share. The root game state creates them at start and disposes of them at the end.
/// </summary>
internal static class SharedRenderers
{
	private static readonly object InitialiseLock = new object();

	public static bool Initialised { get; private set; }

	public static I2dRenderer Standard2d { get; private set; }

	public static IFadeTransitionRenderer FadeTransition { get; private set; }

	public static void Initialise(IRendererFactory rendererFactory)
	{
		lock (InitialiseLock)
		{
			if (Initialised)
				throw new InvalidOperationException("Shared renderers already initialised.");
			Standard2d = rendererFactory.Create2dRenderer();
			FadeTransition = rendererFactory.CreateFadeTransitionRenderer();
			Initialised = true;
		}
	}

	public static void Dispose()
	{
		lock (InitialiseLock)
		{
			if (Standard2d != null)
			{
				Standard2d.Dispose();
				Standard2d = null;
			}
			if (FadeTransition != null)
			{
				FadeTransition.Dispose();
				FadeTransition = null;
			}
			Initialised = false;
		}
	}
}
