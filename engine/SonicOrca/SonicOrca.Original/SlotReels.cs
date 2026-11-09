using System.IO;

namespace SonicOrca.Original;

/// <summary>
/// Makes the reel strips of the Casino Night Zone slot machines from art/uncompressed/Slot pictures.bin: 6 faces of
/// 32 x 32 pixels, each stored as 4 columns of 4 tiles.
/// </summary>
public static class SlotReels
{
	public const string ArtFile = "art/uncompressed/Slot pictures.bin";

	/// <summary>
	/// The face of Knuckles of Knuckles in Sonic 2 (KiS2). SlotMachine_GetPixelRow of KiS2 takes face 0, the face of
	/// Sonic, from this file.
	/// </summary>
	public const string KnucklesArtFile = "art/uncompressed/Slot pictures (Knuckles patch).bin";

	/// <summary>The reels of Knuckles, next to TEXTURE in the folder of the reels. The reels use it when Knuckles plays.</summary>
	public const string KnucklesTextureKey = "SONICORCA/OBJECTS/CNZSLOTREELS/TEXTUREK";

	/// <summary>The block of the top left quarter of the left reel window (APM_CNZ adds the reel blocks at $2EC).</summary>
	public const int WindowBlock = 0x2EC;

	private const int FaceSize = 32;

	/// <summary>The faces of each reel (SlotSequence1 to 3). It must agree with CNZSlotMachine.</summary>
	private static readonly int[][] Sequences =
	{
		new[] { 3, 0, 1, 4, 2, 5, 4, 1 },
		new[] { 3, 0, 1, 4, 2, 5, 0, 2 },
		new[] { 3, 0, 1, 4, 2, 5, 4, 1 },
	};

	/// <summary>
	/// Renders the three strips side by side. Each strip has its 8 faces twice (512 rows), so that a 32-row window
	/// that starts at any row of the first 256 never wraps.
	/// </summary>
	/// <param name="kis2">The folder of KiS2 for the reels of Knuckles: face 0 is the face of Knuckles. Null for Sonic 2.</param>
	public static byte[] RenderTexture(string disassembly, uint[] palette, int scale, string kis2 = null)
	{
		byte[] art = File.ReadAllBytes(Path.Combine(disassembly, ArtFile));
		if (kis2 != null)
		{
			byte[] knuckles = File.ReadAllBytes(Path.Combine(kis2, KnucklesArtFile));
			System.Array.Copy(knuckles, 0, art, 0, System.Math.Min(knuckles.Length, 0x200));
		}
		int width = 3 * FaceSize * scale;
		int rows = 2 * Sequences[0].Length * FaceSize;
		int height = rows * scale;
		var pixels = new byte[width * height * 4];
		for (int reel = 0; reel < 3; reel++)
		{
			for (int row = 0; row < rows; row++)
			{
				int face = Sequences[reel][(row / FaceSize) % Sequences[reel].Length];
				int y = row % FaceSize;
				for (int x = 0; x < FaceSize; x++)
				{
					int offset = face * 0x200 + (x / 8) * 0x80 + y * 4 + (x % 8) / 2;
					int index = (x % 2 == 0 ? art[offset] >> 4 : art[offset]) & 0xF;
					if (index == 0)
					{
						continue;
					}
					uint colour = palette[index];
					for (int sy = 0; sy < scale; sy++)
					{
						for (int sx = 0; sx < scale; sx++)
						{
							int o = ((row * scale + sy) * width + (reel * FaceSize + x) * scale + sx) * 4;
							pixels[o] = (byte)(colour >> 24);
							pixels[o + 1] = (byte)(colour >> 16);
							pixels[o + 2] = (byte)(colour >> 8);
							pixels[o + 3] = 0xFF;
						}
					}
				}
			}
		}
		return SpriteImport.EncodePng(pixels, width, height);
	}
}
