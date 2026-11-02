using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SonicOrca.Original;

/// <summary>
/// Makes the tiles that go in front of the characters. In the original, each 8 x 8 pattern of a block has a priority
/// bit, and the high-priority patterns cover the sprites. The HD map of a zone that the HD developers did not finish
/// has one front layer, so nothing covers the characters. These tiles have only the high-priority patterns of each
/// block, at four times the size, for a layer above the characters. The HD tiles also do not have the animated
/// level art and the palette cycles, so the blocks with animated art or with a cycled colour get animated tiles from the
/// original art.
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

	/// <summary>The tile of the low-priority part of animated block N is AnimatedFirstTile + N.</summary>
	public const int AnimatedFirstTile = 2048;

	/// <summary>The tile of a whole redrawn block on the background layer is BackgroundFirstTile + N.</summary>
	public const int BackgroundFirstTile = 3072;

	/// <summary>The blocks with a pattern that animated art changes.</summary>
	public static HashSet<int> GetAnimatedBlocks(byte[] blockMappings, IReadOnlyList<AnimatedArt> animations)
	{
		var result = new HashSet<int>();
		for (int block = 0; block * 8 + 7 < blockMappings.Length; block++)
		{
			for (int quarter = 0; quarter < 4; quarter++)
			{
				int pattern = ((blockMappings[block * 8 + quarter * 2] << 8) | blockMappings[block * 8 + quarter * 2 + 1]) & 0x7FF;
				if (animations.Any(a => a.Contains(pattern)))
				{
					result.Add(block);
				}
			}
		}
		return result;
	}

	/// <summary>
	/// The most frames of a block with colours of more than one palette cycle. The cycles of a block can have lengths
	/// with a large common multiple, for example 108 and 100 ticks in Metropolis. Such a block then follows only the
	/// cycle with most of its colours.
	/// </summary>
	private const int MaxPaletteFrames = 64;

	/// <summary>The blocks with a pattern that has a colour of a palette cycle.</summary>
	public static HashSet<int> GetPaletteCycleBlocks(byte[] art, byte[] blockMappings, IReadOnlyList<PaletteCycle> cycles)
	{
		var result = new HashSet<int>();
		var entries = new HashSet<int>(cycles.SelectMany(c => c.Entries));
		if (entries.Count == 0)
		{
			return result;
		}
		for (int block = 0; block * 8 + 7 < blockMappings.Length; block++)
		{
			if (GetEntries(art, blockMappings, block).Overlaps(entries))
			{
				result.Add(block);
			}
		}
		return result;
	}

	/// <summary>The palette entries (line * 16 + index) of the pixels of a block, without the transparent index 0.</summary>
	private static HashSet<int> GetEntries(byte[] art, byte[] blockMappings, int block)
	{
		var entries = new HashSet<int>();
		for (int quarter = 0; quarter < 4; quarter++)
		{
			int word = GetWord(blockMappings, block, quarter);
			int pattern = word & 0x7FF;
			int line = (word >> 13) & 3;
			for (int i = 0; i < BytesPerPattern; i++)
			{
				int offset = pattern * BytesPerPattern + i;
				if (offset >= art.Length)
				{
					break;
				}
				foreach (int index in new[] { art[offset] >> 4, art[offset] & 0xF })
				{
					if (index != 0)
					{
						entries.Add(line * 16 + index);
					}
				}
			}
		}
		return entries;
	}

	/// <summary>
	/// The frames of a block with colours of palette cycles: the time of each frame and its palette. The frames follow
	/// all the cycles of the block until they all start again together.
	/// </summary>
	private static List<(int Duration, uint[] Palette)> GetPaletteFrames(uint[] palette, IReadOnlyList<PaletteCycle> cycles, HashSet<int> entries)
	{
		List<PaletteCycle> used = cycles.Where(c => c.Entries.Any(entries.Contains)).ToList();
		int length = used.Aggregate(1, (total, cycle) => Lcm(total, cycle.Length));
		List<int> changes = used.SelectMany(c => Enumerable.Range(0, length / c.Period).Select(step => step * c.Period)).Distinct().OrderBy(t => t).ToList();
		if (changes.Count > MaxPaletteFrames)
		{
			used = new List<PaletteCycle> { used.OrderByDescending(c => c.Entries.Count(entries.Contains)).First() };
			length = used[0].Length;
			changes = Enumerable.Range(0, used[0].Steps.Count).Select(step => step * used[0].Period).ToList();
		}
		var frames = new List<(int, uint[])>();
		for (int i = 0; i < changes.Count; i++)
		{
			uint[] framePalette = (uint[])palette.Clone();
			foreach (PaletteCycle cycle in used)
			{
				cycle.Apply(framePalette, changes[i]);
			}
			int end = i + 1 < changes.Count ? changes[i + 1] : length;
			frames.Add((end - changes[i], framePalette));
		}
		return frames;
	}

	private static int Lcm(int a, int b) => a / Gcd(a, b) * b;

	private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

	/// <summary>
	/// Makes the tile set of the zone and its second texture (PRIORITY). The tile set has the HD tiles (1 to
	/// <paramref name="hdTiles"/>, in rows of 32 on TEXTURE), the high-priority part of the priority blocks
	/// (FirstTile + block), and the low-priority part of the animated blocks (AnimatedFirstTile + block). The HD tiles
	/// do not have the animated art, so the tiles of an animated block have one frame for each frame of its animation.
	/// The texture has the static priority tiles in the layout of the HD tile set, then the animation frames.
	/// </summary>
	/// <param name="art">The level art: 8 x 8 patterns, 4 bits for each pixel.</param>
	/// <param name="blockMappings">The 16 x 16 block mappings: four pattern words for each block.</param>
	public static (string TileSet, byte[] Texture) Create(int hdTiles, byte[] art, byte[] blockMappings, uint[] palette,
		ICollection<int> priorityBlocks, ICollection<int> animatedBlocks, IReadOnlyList<AnimatedArt> animations, int scale,
		ICollection<int> backgroundBlocks = null, IReadOnlyList<PaletteCycle> paletteCycles = null)
	{
		backgroundBlocks ??= Array.Empty<int>();
		paletteCycles ??= Array.Empty<PaletteCycle>();
		int tileSize = BlockSize * scale;
		// The tile id, then the texture slot and the time of each frame
		var tiles = new SortedDictionary<int, List<(int Slot, int Duration)>>();
		// The priority of the patterns to draw: high, low, or null for all the patterns (the background)
		var drawings = new List<(int Slot, byte[] Art, uint[] Palette, int Block, bool? Priority)>();
		foreach (int block in priorityBlocks.Where(b => !animatedBlocks.Contains(b)))
		{
			tiles[FirstTile + block] = new List<(int, int)> { (block, 0) };
			drawings.Add((block, art, palette, block, true));
		}
		int nextSlot = BlockCount;
		foreach (int block in animatedBlocks.Union(backgroundBlocks).OrderBy(b => b))
		{
			// A block with animated art has its frames. A block with colours of palette cycles has a frame for each change
			// of the colours. Another block (for example one with patterns that the HD tiles do not have) has one frame.
			AnimatedArt animation = animations.FirstOrDefault(a => Enumerable.Range(0, 4).Any(q => a.Contains(GetWord(blockMappings, block, q) & 0x7FF)));
			List<(int Duration, uint[] Palette)> paletteFrames = animation == null ? GetPaletteFrames(palette, paletteCycles, GetEntries(art, blockMappings, block)) : null;
			if (paletteFrames != null && paletteFrames.Count <= 1)
			{
				paletteFrames = null;
			}
			int frameCount = animation?.Frames.Count ?? paletteFrames?.Count ?? 1;
			foreach (bool? priority in new bool?[] { true, false, null })
			{
				bool wanted = priority == null ? backgroundBlocks.Contains(block) : animatedBlocks.Contains(block) && HasPatterns(blockMappings, block, priority.Value);
				if (!wanted)
				{
					continue;
				}
				var frames = new List<(int, int)>();
				for (int frame = 0; frame < frameCount; frame++)
				{
					frames.Add((nextSlot, animation?.Frames[frame].Duration ?? paletteFrames?[frame].Duration ?? 0));
					drawings.Add((nextSlot, animation?.Apply(art, frame) ?? art, paletteFrames?[frame].Palette ?? palette, block, priority));
					nextSlot++;
				}
				tiles[(priority == null ? BackgroundFirstTile : priority.Value ? FirstTile : AnimatedFirstTile) + block] = frames;
			}
		}

		int width = TilesPerRow * tileSize;
		int height = (nextSlot + TilesPerRow - 1) / TilesPerRow * tileSize;
		var pixels = new byte[width * height * 4];
		foreach ((int slot, byte[] slotArt, uint[] slotPalette, int block, bool? priority) in drawings)
		{
			DrawBlock(pixels, width, slot % TilesPerRow * tileSize, slot / TilesPerRow * tileSize, slotArt, blockMappings, slotPalette, block, priority, scale);
		}

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
		foreach ((int id, List<(int Slot, int Duration)> frames) in tiles)
		{
			if (frames.Count == 1)
			{
				xml.AppendLine($"    <tile id=\"{id}\" texture=\"1\" x=\"{frames[0].Slot % TilesPerRow * tileSize}\" y=\"{frames[0].Slot / TilesPerRow * tileSize}\" />");
				continue;
			}
			xml.AppendLine($"    <tile id=\"{id}\">");
			foreach ((int slot, int duration) in frames)
			{
				xml.AppendLine($"      <frame texture=\"1\" x=\"{slot % TilesPerRow * tileSize}\" y=\"{slot / TilesPerRow * tileSize}\" delay=\"{duration}\" />");
			}
			xml.AppendLine("    </tile>");
		}
		xml.AppendLine("  </tiles>");
		xml.AppendLine("</tileset>");
		return (xml.ToString(), SpriteImport.EncodePng(pixels, width, height));
	}

	/// <summary>True when the block has a high-priority pattern (or a low-priority pattern).</summary>
	public static bool HasPatterns(byte[] blockMappings, int block, bool priority)
	{
		return Enumerable.Range(0, 4).Any(q => ((GetWord(blockMappings, block, q) & 0x8000) != 0) == priority);
	}

	private static int GetWord(byte[] blockMappings, int block, int quarter)
	{
		return (blockMappings[block * 8 + quarter * 2] << 8) | blockMappings[block * 8 + quarter * 2 + 1];
	}

	/// <summary>Draws the high-priority or the low-priority patterns of a block (or all of them) at four times the size.</summary>
	private static void DrawBlock(byte[] pixels, int width, int left, int top, byte[] art, byte[] blockMappings, uint[] palette,
		int block, bool? priority, int scale)
	{
		for (int quarter = 0; quarter < 4; quarter++)
		{
			int word = GetWord(blockMappings, block, quarter);
			if (priority != null && ((word & 0x8000) != 0) != priority.Value)
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
					// The HD tiles show black as transparent, so these tiles do too
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
}
