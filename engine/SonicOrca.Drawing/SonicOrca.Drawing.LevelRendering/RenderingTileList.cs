namespace SonicOrca.Drawing.LevelRendering;

internal class RenderingTileList
{
	private const int InitialTileRenderInfoCapacity = 128;

	private TileRenderInfo[] _buffer = new TileRenderInfo[128];

	private int _currentCount;

	public int Count => _currentCount;

	public TileRenderInfo this[int index] => _buffer[index];

	private void ExpandBuffer()
	{
		int num = _buffer.Length * 2;
		_buffer = new TileRenderInfo[num];
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
