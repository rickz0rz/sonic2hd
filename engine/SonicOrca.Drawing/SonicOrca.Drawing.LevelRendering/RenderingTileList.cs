namespace SonicOrca.Drawing.LevelRendering;

/// <summary>
/// Holds the tiles that the level renderer prepared for the current frame.
/// </summary>
internal class RenderingTileList
{
	private const int InitialTileRenderInfoCapacity = 128;

	private TileRenderInfo[] _buffer = new TileRenderInfo[InitialTileRenderInfoCapacity];

	private int _currentCount;

	public int Count => _currentCount;

	public TileRenderInfo this[int index] => _buffer[index];

	private void ExpandBuffer()
	{
		// Note: this does not copy the existing entries into the new buffer.
		_buffer = new TileRenderInfo[_buffer.Length * 2];
	}

	public void Clear()
	{
		_currentCount = 0;
	}

	public void AddTileRenderInfo(TileRenderInfo tileRenderInfo)
	{
		if (_currentCount >= _buffer.Length)
		{
			ExpandBuffer();
		}
		_buffer[_currentCount] = tileRenderInfo;
		_currentCount++;
	}
}
