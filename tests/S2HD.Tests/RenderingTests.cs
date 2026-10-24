using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using SonicOrca.Drawing.LevelRendering;
using SonicOrca.Drawing.Renderers;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using Xunit;

namespace S2HD.Tests;

/// <summary>Renderer logic that does not need a GPU.</summary>
public sealed class RenderingTests
{
	[Fact]
	public void RenderingTileList_KeepsAllEntriesAfterBufferExpands()
	{
		var list = new RenderingTileList();
		const int count = 1000;
		for (int i = 0; i < count; i++)
		{
			list.AddTileRenderInfo(new TileRenderInfo { TileIndex = i + 1 });
		}

		Assert.Equal(count, list.Count);
		for (int i = 0; i < count; i++)
		{
			Assert.Equal(i + 1, list[i].TileIndex);
		}
	}

	[Fact]
	public void MaskRenderer_GetClippedSourceRect_ScalesClippedFractionBySourceSpan()
	{
		// The source covers x 0.25 to 0.75 and y 0.2 to 0.5 of a 200x100 texture.
		ITexture texture = FakeTexture.Create(200, 100);
		var source = new Rectanglei(50, 20, 100, 30);
		// The clip removes 1/4 of the destination on each side.
		var destination = new Rectanglei(0, 0, 400, 200);
		var clippedDestination = new Rectanglei(100, 50, 200, 100);

		object renderer = RuntimeHelpers.GetUninitializedObject(typeof(MaskRenderer));
		MethodInfo method = typeof(MaskRenderer).GetMethod("GetClippedSourceRect", BindingFlags.Instance | BindingFlags.NonPublic);
		object[] arguments = { texture, source, destination, clippedDestination, null, null, null, null };
		method.Invoke(renderer, arguments);

		Assert.Equal(0.375f, (float)arguments[4], 5);
		Assert.Equal(0.275f, (float)arguments[5], 5);
		Assert.Equal(0.625f, (float)arguments[6], 5);
		Assert.Equal(0.425f, (float)arguments[7], 5);
	}

	/// <summary>A texture that has only a size.</summary>
	public class FakeTexture : DispatchProxy
	{
		private int _width;
		private int _height;

		public static ITexture Create(int width, int height)
		{
			ITexture texture = Create<ITexture, FakeTexture>();
			var proxy = (FakeTexture)(object)texture;
			proxy._width = width;
			proxy._height = height;
			return texture;
		}

		protected override object Invoke(MethodInfo targetMethod, object[] args) => targetMethod.Name switch
		{
			"get_Width" => _width,
			"get_Height" => _height,
			_ => throw new NotSupportedException(targetMethod.Name),
		};
	}
}
