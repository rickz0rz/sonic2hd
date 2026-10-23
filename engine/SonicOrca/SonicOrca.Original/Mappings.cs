using System.IO;
using SonicOrca.Core;

namespace SonicOrca.Original;

/// <summary>
/// Converts the chunks and the level layout of the original game into a level map file.
/// </summary>
public static class Mappings
{
	private const int ChunkCount = 256;

	/// <summary>The size of one chunk in the file: 8 x 8 tiles of 2 bytes each.</summary>
	private const int ChunkByteSize = 128;

	/// <summary>The width and the height of a chunk, in tiles.</summary>
	private const int ChunkSize = 8;

	private const int MapColumns = 1024;

	private const int MapRows = 128;

	/// <summary>The width of the layout, in chunks.</summary>
	private const int LayoutColumns = 128;

	/// <summary>The height of the layout, in chunks.</summary>
	private const int LayoutRows = 16;

	/// <summary>The tile flag for a horizontal flip.</summary>
	private const int FlipXFlag = 0x4000;

	/// <summary>The tile flag for a vertical flip.</summary>
	private const int FlipYFlag = 0x8000;

	/// <summary>The value to add to the tile indices of the second layer.</summary>
	private const int SecondLayerTileOffset = 2048;

	public static void Export(string tilesFilename, string chunksFilename, string layoutFilename, string outputFilename)
	{
		byte[] data = Kosinski.Decompress(File.ReadAllBytes(chunksFilename));
		int[][,] chunks = new int[ChunkCount][,];
		for (int i = 0; i < ChunkCount; i++)
		{
			chunks[i] = GetChunk(data, i * ChunkByteSize);
		}
		data = Kosinski.Decompress(File.ReadAllBytes(layoutFilename));
		byte[,] backgroundLayout = GetBackgroundLayout(data);
		byte[,] foregroundLayout = GetForegroundLayout(data);
		int[,] backgroundTiles = new int[MapColumns, MapRows];
		int[,] foregroundTiles = new int[MapColumns, MapRows];
		for (int y = 0; y < MapRows; y++)
		{
			for (int x = 0; x < MapColumns; x++)
			{
				int[,] chunk = chunks[backgroundLayout[x / ChunkSize, y / ChunkSize]];
				backgroundTiles[x, y] = chunk[x % ChunkSize, y % ChunkSize];
				chunk = chunks[foregroundLayout[x / ChunkSize, y / ChunkSize]];
				foregroundTiles[x, y] = chunk[x % ChunkSize, y % ChunkSize];
			}
		}
		LevelMap levelMap = new LevelMap();
		levelMap.Layers.Add(new LevelLayer(levelMap));
		levelMap.Layers.Add(new LevelLayer(levelMap));
		levelMap.Layers[0].Resize(MapColumns, MapRows);
		for (int row = 0; row < levelMap.Layers[0].Rows; row++)
		{
			for (int column = 0; column < levelMap.Layers[0].Columns; column++)
			{
				levelMap.Layers[0].Tiles[column, row] = backgroundTiles[column, row];
			}
		}
		levelMap.Layers[1].Resize(MapColumns, MapRows);
		for (int row = 0; row < levelMap.Layers[1].Rows; row++)
		{
			for (int column = 0; column < levelMap.Layers[1].Columns; column++)
			{
				levelMap.Layers[1].Tiles[column, row] = foregroundTiles[column, row] + SecondLayerTileOffset;
			}
		}
		new LevelMapWriter(levelMap).Save(outputFilename);
	}

	/// <summary>
	/// Reads one chunk. Each tile is a big-endian 16-bit value: the tile index in bits 0-9 and the flip flags in bits 10 and 11.
	/// </summary>
	private static int[,] GetChunk(byte[] chunks, int index)
	{
		int[,] chunk = new int[ChunkSize, ChunkSize];
		for (int y = 0; y < ChunkSize; y++)
		{
			for (int x = 0; x < ChunkSize; x++)
			{
				byte high = chunks[index + y * 16 + x * 2];
				byte low = chunks[index + y * 16 + x * 2 + 1];
				int value = (high << 8) | low;
				chunk[x, y] = value & 0x3FF;
				if ((value & 0x400) != 0)
				{
					chunk[x, y] |= FlipXFlag;
				}
				if ((value & 0x800) != 0)
				{
					chunk[x, y] |= FlipYFlag;
				}
			}
		}
		return chunk;
	}

	// Each row of the layout has 128 foreground bytes and then 128 background bytes.
	private static byte[,] GetBackgroundLayout(byte[] layout)
	{
		return GetLayout(layout, LayoutColumns);
	}

	private static byte[,] GetForegroundLayout(byte[] layout)
	{
		return GetLayout(layout, 0);
	}

	private static byte[,] GetLayout(byte[] layout, int rowOffset)
	{
		byte[,] result = new byte[LayoutColumns, LayoutRows];
		for (int y = 0; y < LayoutRows; y++)
		{
			for (int x = 0; x < LayoutColumns; x++)
			{
				result[x, y] = layout[256 * y + x + rowOffset];
			}
		}
		return result;
	}
}
