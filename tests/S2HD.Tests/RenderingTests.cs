using SonicOrca.Drawing.LevelRendering;
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
}
