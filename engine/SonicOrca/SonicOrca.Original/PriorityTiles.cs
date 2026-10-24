using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonicOrca.Original;

/// <summary>
/// Makes the tiles that go in front of the characters. In the original, each 8 x 8 pattern of a block has a priority
/// bit, and the high-priority patterns cover the sprites. The HD map of a zone that the HD developers did not finish
/// has one front layer, so nothing covers the characters. These tiles have only the high-priority patterns of each
/// block, at four times the size, for a layer above the characters.
/// </summary>
public static class PriorityTiles
{
	/// <summary>The tile of the priority part of block N is FirstTile + N.</summary>
	public const int FirstTile = 1024;

	/// <summary>The HD tile sets have the blocks in rows of 32 tiles.</summary>
	private const int TilesPerRow = 32;

	private const int BlockSize = 16;

	private const int BlockCount = 1024;

	private const int BytesPerPattern = 32;

	/// <summary>The blocks that have at least one high-priority pattern.</summary>
	public static HashSet<int> GetPriorityBlocks(byte[] blockMappings)
	{
		var result = new HashSet<int>();
		for (int block = 0; block * 8 + 7 < blockMappings.Length; block++)
		{
			for (int quarter = 0; quarter < 4; quarter++)
			{
				if ((blockMappings[block * 8 + quarter * 2] & 0x80) != 0)
				{
					result.Add(block);
				}
			}
		}
		return result;
	}

	/// <summary>Draws the high-priority patterns of the blocks in a texture with the layout of the HD tile set.</summary>
	/// <param name="art">The level art: 8 x 8 patterns, 4 bits for each pixel.</param>
	/// <param name="blockMappings">The 16 x 16 block mappings: four pattern words for each block.</param>
	public static byte[] RenderTexture(byte[] art, byte[] blockMappings, uint[] palette, IEnumerable<int> blocks, int scale)
	{
		int tileSize = BlockSize * scale;
		int width = TilesPerRow * tileSize;
		int height = BlockCount / TilesPerRow * tileSize;
		var pixels = new byte[width * height * 4];
		foreach (int block in blocks)
		{
			int left = block % TilesPerRow * tileSize;
			int top = block / TilesPerRow * tileSize;
			for (int quarter = 0; quarter < 4; quarter++)
			{
				int word = (blockMappings[block * 8 + quarter * 2] << 8) | blockMappings[block * 8 + quarter * 2 + 1];
				if ((word & 0x8000) == 0)
				{
					continue;
				}
				int pattern = word & 0x7FF;
				bool flipX = (word & 0x800) != 0;
				bool flipY = (word & 0x1000) != 0;
				int line = (word >> 13) & 3;
				for (int y = 0; y < 8; y++)
				{
					for (int x = 0; x < 8; x++)
					{
						int tx = flipX ? 7 - x : x;
						int ty = flipY ? 7 - y : y;
						int offset = pattern * BytesPerPattern + ty * 4 + tx / 2;
						if (offset >= art.Length)
						{
							continue;
						}
						int index = (tx % 2 == 0 ? art[offset] >> 4 : art[offset]) & 0xF;
						uint colour = palette[line * 16 + index];
						// The HD tiles show black as transparent, so the priority tiles do too
						if (index == 0 || (colour & 0xFFFFFF00) == 0)
						{
							continue;
						}
						int px = left + ((quarter % 2) * 8 + x) * scale;
						int py = top + ((quarter / 2) * 8 + y) * scale;
						for (int sy = 0; sy < scale; sy++)
						{
							for (int sx = 0; sx < scale; sx++)
							{
								int o = ((py + sy) * width + px + sx) * 4;
								pixels[o] = (byte)(colour >> 24);
								pixels[o + 1] = (byte)(colour >> 16);
								pixels[o + 2] = (byte)(colour >> 8);
								pixels[o + 3] = 0xFF;
							}
						}
					}
				}
			}
		}
		return SpriteImport.EncodePng(pixels, width, height);
	}

	/// <summary>
	/// The tile set of the zone: the HD tiles (1 to <paramref name="hdTiles"/>, in rows of 32 on TEXTURE), then the
	/// priority tiles on PRIORITY.
	/// </summary>
	public static string CreateTileSet(int hdTiles, IEnumerable<int> priorityBlocks, int scale)
	{
		int tileSize = BlockSize * scale;
		var xml = new StringBuilder();
		xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
		xml.AppendLine("<tileset>");
		xml.AppendLine("  <textures>");
		xml.AppendLine("    <texture>/TEXTURE</texture>");
		xml.AppendLine("    <texture>/PRIORITY</texture>");
		xml.AppendLine("  </textures>");
		xml.AppendLine("  <tiles>");
		for (int tile = 1; tile <= hdTiles; tile++)
		{
			xml.AppendLine($"    <tile id=\"{tile}\" texture=\"0\" x=\"{tile % TilesPerRow * tileSize}\" y=\"{tile / TilesPerRow * tileSize}\" />");
		}
		foreach (int block in priorityBlocks.OrderBy(b => b))
		{
			xml.AppendLine($"    <tile id=\"{FirstTile + block}\" texture=\"1\" x=\"{block % TilesPerRow * tileSize}\" y=\"{block / TilesPerRow * tileSize}\" />");
		}
		xml.AppendLine("  </tiles>");
		xml.AppendLine("</tileset>");
		return xml.ToString();
	}
}
