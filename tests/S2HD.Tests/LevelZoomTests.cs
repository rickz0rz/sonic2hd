using System;
using SonicOrca.Core;
using SonicOrca.Drawing.LevelRendering;
using SonicOrca.Geometry;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the zoom of a level: the easing, the limits, and the pixels of the tiles at a zoom.</summary>
public sealed class LevelZoomTests
{
	[Fact]
	public void SineEase_StartsAndEndsSlowly_AndIsHalfWayAtTheMiddle()
	{
		Assert.Equal(0.0, LevelZoom.Ease(0.0, ZoomEasing.Sine), 9);
		Assert.Equal(0.5, LevelZoom.Ease(0.5, ZoomEasing.Sine), 9);
		Assert.Equal(1.0, LevelZoom.Ease(1.0, ZoomEasing.Sine), 9);
		// The first and the last tenth of the time change the zoom less than the linear easing
		Assert.True(LevelZoom.Ease(0.1, ZoomEasing.Sine) < 0.1);
		Assert.True(LevelZoom.Ease(0.9, ZoomEasing.Sine) > 0.9);
		Assert.Equal(0.25, LevelZoom.Ease(0.25, ZoomEasing.Linear), 9);
	}

	[Fact]
	public void ZoomTo_ReachesTheTarget_InTheGivenTicks()
	{
		LevelZoom zoom = new LevelZoom();
		zoom.ZoomTo(0.5, 10);

		for (int i = 0; i < 5; i++)
		{
			zoom.Update();
		}
		Assert.Equal(0.75, zoom.Current, 9);
		Assert.Equal(0.5, zoom.Target);
		Assert.True(zoom.Changing);

		for (int i = 0; i < 5; i++)
		{
			zoom.Update();
		}
		Assert.Equal(0.5, zoom.Current);
		Assert.False(zoom.Changing);
	}

	[Fact]
	public void ZoomTo_DuringAChange_StartsFromTheCurrentZoom()
	{
		LevelZoom zoom = new LevelZoom();
		zoom.ZoomTo(2.0, 10, ZoomEasing.Linear);
		for (int i = 0; i < 5; i++)
		{
			zoom.Update();
		}

		zoom.ZoomTo(1.0, 10, ZoomEasing.Linear);
		zoom.Update();

		// The zoom was 1.5. It goes back to 1 by 0.05 on each tick, with no jump.
		Assert.Equal(1.45, zoom.Current, 9);
	}

	[Theory]
	[InlineData(0.1, LevelZoom.Min)]
	[InlineData(5.0, LevelZoom.Max)]
	[InlineData(double.NaN, 1.0)]
	[InlineData(double.PositiveInfinity, 1.0)]
	public void Zoom_OutsideTheLimits_GetsALimit(double value, double expected)
	{
		LevelZoom zoom = new LevelZoom();

		zoom.ZoomTo(value, 0);

		Assert.Equal(expected, zoom.Current);
		Assert.Equal(expected, zoom.Target);
	}

	[Theory]
	[InlineData(0.73)]
	[InlineData(1.37)]
	[InlineData(1.25)]
	[InlineData(2.0 / 3.0)]
	public void TilesAtAZoom_TouchTheNextTile_WithNoGap(double scale)
	{
		Vector2 view = new Vector2(scale, scale);
		for (int i = 0; i < 200; i++)
		{
			Rectanglei tile = RenderingTiler.GetDestination(i * 64 - 37, i * 64 + 5, 64, 64, view);
			Rectanglei next = RenderingTiler.GetDestination((i + 1) * 64 - 37, (i + 1) * 64 + 5, 64, 64, view);

			Assert.Equal(tile.Right, next.Left);
			Assert.Equal(tile.Bottom, next.Top);
		}
	}
}
