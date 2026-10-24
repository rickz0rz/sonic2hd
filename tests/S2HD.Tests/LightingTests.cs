using System;
using SonicOrca.Core.Lighting;
using SonicOrca.Geometry;
using Xunit;

namespace S2HD.Tests;

/// <summary>The shadow offset that the lighting manager combines from the light sources.</summary>
public sealed class LightingTests
{
	[Fact]
	public void GetShadowOffset_OneLightSource_KeepsTheOffset()
	{
		ILightingManager manager = CreateLightingManager();
		manager.RegisterLightSource(new FixedLightSource(new Vector2i(10, -10)));

		Vector2i offset = manager.GetShadowOffset(default, new ShadowInfo(new Vector2i(32, 32)));

		Assert.Equal(new Vector2i(10, -10), offset);
	}

	[Fact]
	public void GetShadowOffset_LightsAboveAndBelow_AveragesTheYOffset()
	{
		ILightingManager manager = CreateLightingManager();
		manager.RegisterLightSource(new FixedLightSource(new Vector2i(-4, -10)));
		manager.RegisterLightSource(new FixedLightSource(new Vector2i(-4, 20)));

		Vector2i offset = manager.GetShadowOffset(default, new ShadowInfo(new Vector2i(32, 32)));

		Assert.Equal(new Vector2i(-4, 5), offset);
	}

	private static ILightingManager CreateLightingManager()
	{
		Type type = typeof(ILightingManager).Assembly.GetType("SonicOrca.Core.Lighting.LightingManager", throwOnError: true);
		return (ILightingManager)Activator.CreateInstance(type, nonPublic: true);
	}

	private sealed class FixedLightSource(Vector2i offset) : ILightSource
	{
		public Vector2i GetShadowOffset(Vector2i position, IShadowInfo shadowInfo) => offset;
	}

	private sealed class ShadowInfo(Vector2i maxShadowOffset) : IShadowInfo
	{
		public Vector2i MaxShadowOffset => maxShadowOffset;

		public Vector2i OcclusionSize => default;
	}
}
