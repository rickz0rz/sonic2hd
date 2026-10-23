using System.Collections.Generic;
using System.IO;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

/// <summary>
/// Reads the ring positions of a level from the data format of the original game.
/// </summary>
public static class RingPositions
{
	private const int Horizontal = 0;

	private const int Vertical = 1;

	/// <summary>The distance between two rings in a group.</summary>
	private const int RingSpacing = 24;

	public static IReadOnlyCollection<Vector2i> FromFile(string filename)
	{
		using FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
		return FromStream(stream);
	}

	/// <summary>
	/// Reads 4-byte ring group entries until the end of the stream or an 0xFFFF marker.
	/// Each entry has an X position (16 bits), a direction (1 bit), a ring count minus one (3 bits)
	/// and a Y position (12 bits).
	/// </summary>
	public static IReadOnlyCollection<Vector2i> FromStream(Stream stream)
	{
		List<Vector2i> positions = new List<Vector2i>();
		byte[] entry = new byte[4];
		while (stream.Read(entry, 0, 2) == 2 && (entry[0] != byte.MaxValue || entry[1] != byte.MaxValue) && stream.Read(entry, 2, 2) == 2)
		{
			int x = (entry[0] << 8) | entry[1];
			int y = ((entry[2] & 0xF) << 8) | entry[3];
			int count = ((entry[2] >> 4) & 7) + 1;
			int direction = entry[2] >> 7;
			for (int i = 0; i < count; i++)
			{
				positions.Add(new Vector2i(x, y));
				switch (direction)
				{
					case Horizontal:
						x += RingSpacing;
						break;
					case Vertical:
						y += RingSpacing;
						break;
				}
			}
		}
		return positions.ToArray();
	}
}
