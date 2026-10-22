using System.IO;
using SonicOrca.Core;

namespace SonicOrca.Original;

public static class Mappings
{
	public static void Export(string tilesFilename, string chunksFilename, string layoutFilename, string outputFilename)
	{
		byte[] chunks = Kosinski.Decompress(File.ReadAllBytes(chunksFilename));
		int[][,] array = new int[256][,];
		for (int i = 0; i < 256; i++)
		{
			array[i] = GetChunk(chunks, i * 128);
		}
		chunks = Kosinski.Decompress(File.ReadAllBytes(layoutFilename));
		byte[,] backgroundLayout = GetBackgroundLayout(chunks);
		byte[,] foregroundLayout = GetForegroundLayout(chunks);
		int[,] array2 = new int[1024, 128];
		int[,] array3 = new int[1024, 128];
		for (int j = 0; j < 128; j++)
		{
			for (int k = 0; k < 1024; k++)
			{
				int[,] array4 = array[backgroundLayout[k / 8, j / 8]];
				array2[k, j] = array4[k % 8, j % 8];
				array4 = array[foregroundLayout[k / 8, j / 8]];
				array3[k, j] = array4[k % 8, j % 8];
			}
		}
		LevelMap levelMap = new LevelMap();
		levelMap.Layers.Add(new LevelLayer(levelMap));
		levelMap.Layers.Add(new LevelLayer(levelMap));
		levelMap.Layers[0].Resize(1024, 128);
		for (int l = 0; l < levelMap.Layers[0].Rows; l++)
		{
			for (int m = 0; m < levelMap.Layers[0].Columns; m++)
			{
				levelMap.Layers[0].Tiles[m, l] = array2[m, l];
			}
		}
		levelMap.Layers[1].Resize(1024, 128);
		for (int n = 0; n < levelMap.Layers[1].Rows; n++)
		{
			for (int num = 0; num < levelMap.Layers[1].Columns; num++)
			{
				levelMap.Layers[1].Tiles[num, n] = array3[num, n] + 2048;
			}
		}
		new LevelMapWriter(levelMap).Save(outputFilename);
	}

	private static int[,] GetChunk(byte[] chunks, int index)
	{
		int[,] array = new int[8, 8];
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 8; j++)
			{
				byte b = chunks[index + i * 16 + j * 2];
				byte b2 = chunks[index + i * 16 + j * 2 + 1];
				int num = (b << 8) | b2;
				array[j, i] = num & 0x3FF;
				if ((num & 0x400) != 0)
				{
					array[j, i] |= 16384;
				}
				if ((num & 0x800) != 0)
				{
					array[j, i] |= 32768;
				}
			}
		}
		return array;
	}

	private static byte[,] GetBackgroundLayout(byte[] layout)
	{
		byte[,] array = new byte[128, 16];
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				array[j, i] = layout[256 * i + j + 128];
			}
		}
		return array;
	}

	private static byte[,] GetForegroundLayout(byte[] layout)
	{
		byte[,] array = new byte[128, 16];
		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				array[j, i] = layout[256 * i + j];
			}
		}
		return array;
	}
}
