using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Hjg.Pngcs;

namespace SonicOrca.Core;

/// <summary>
/// Makes the files of a new level for the level editor. The level is a mod level (see SONICORCA.MODAREA.ModArea)
/// with the tile set of another level, or with a tile set from a PNG image. The map has an empty background layer, an empty front layer, a flat floor
/// line and a start position. The binding has a signpost near the right end, so that the level can end.
/// </summary>
internal static class NewLevel
{
	public const int Columns = 128;

	public const int Rows = 24;

	public const int TileSize = 64;

	/// <summary>The columns of the background layer: 2048 pixels, which repeat.</summary>
	public const int BackgroundColumns = 32;

	/// <summary>The Y of the floor line.</summary>
	public const int FloorY = 1152;

	/// <summary>The length of each part of the floor line.</summary>
	private const int FloorPartLength = 512;

	public const string DefaultMusic = "SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1";

	public const string DefaultAnimals = "SONICORCA/OBJECTS/FLICKY SONICORCA/OBJECTS/RICKY";

	public const int MaxMnemonicLength = 16;

	/// <summary>Gives the problem of a short name, or null when the short name is correct.</summary>
	public static string CheckMnemonic(string mnemonic)
	{
		if (string.IsNullOrEmpty(mnemonic))
			return "The short name is empty.";
		if (mnemonic.Length > MaxMnemonicLength)
			return $"The short name has more than {MaxMnemonicLength} characters.";
		if (!mnemonic.All(char.IsAsciiLetterOrDigit))
			return "The short name can have only letters and digits.";
		return null;
	}

	public static byte[] MakeArea() => Encoding.UTF8.GetBytes("#use SONICORCA.MODAREA\n");

	public static byte[] MakeInfo(string name, string tileSetKey)
	{
		XElement level = new XElement("level",
			new XAttribute("name", name),
			new XAttribute("music", DefaultMusic),
			new XAttribute("start", "startpos"),
			new XAttribute("animals", DefaultAnimals),
			new XAttribute("tileset", tileSetKey));
		return ToBytes(level);
	}

	public static byte[] MakeMap()
	{
		string emptyRow = string.Join(",", Enumerable.Repeat("0", Columns));
		// The background repeats to the left and the right. It is large enough to paint a background on it.
		string backRow = string.Join(",", Enumerable.Repeat("0", BackgroundColumns));
		XElement back = new XElement("layer", new XAttribute("name", "back"),
			new XElement("tiles", new XAttribute("wrap_x", "true"), Enumerable.Range(0, Rows).Select(_ => new XElement("row", backRow))));
		XElement front = new XElement("layer", new XAttribute("name", "front"),
			new XElement("tiles", Enumerable.Range(0, Rows).Select(_ => new XElement("row", emptyRow))));
		XElement vectors = new XElement("vectors");
		for (int x = 0; x < Columns * TileSize; x += FloorPartLength)
		{
			vectors.Add(new XElement("vector",
				new XAttribute("a", Point(x, FloorY)),
				new XAttribute("b", Point(x + FloorPartLength, FloorY)),
				new XAttribute("paths", "3"),
				new XAttribute("flags", "0")));
		}
		XElement map = new XElement("map",
			new XElement("tiles", back, front),
			new XElement("collision",
				new XElement("paths", new XElement("path", new XAttribute("layer", "1")), new XElement("path", new XAttribute("layer", "1"))),
				vectors),
			new XElement("markers",
				Marker("startpos", 256, FloorY - 80),
				Marker("bounds_lt", 0, 0),
				Marker("bounds_rb", Columns * TileSize, Rows * TileSize)));
		return ToBytes(map);
	}

	public static byte[] MakeBinding()
	{
		XElement signpost = new XElement("Definition",
			new XElement("Common",
				new XElement("Key", "SONICORCA/OBJECTS/SIGNPOST"),
				new XElement("Uid", Guid.NewGuid().ToString()),
				new XElement("Name", "Signpost 0"),
				new XElement("Position", new XAttribute("X", Columns * TileSize - 640), new XAttribute("Y", FloorY - 92))));
		XElement binding = new XElement("Binding", new XElement("Definitions", new XAttribute("DefaultLayer", "1"), signpost));
		return ToBytes(binding);
	}

	/// <summary>The largest width and height of a tile image. GPUs limit the size of a texture.</summary>
	public const int MaxTileImageSize = 8192;

	/// <summary>
	/// Makes a tile set from a PNG image: a tile for each cell of 64 x 64 pixels that is not fully transparent, from
	/// the top left, row by row. The tiles have the numbers from 1, so tile 0 stays empty. The texture of the tile set
	/// is the image itself (/TEXTURE). Throws <see cref="InvalidOperationException"/> with a message for the user when
	/// the image cannot be a tile set.
	/// </summary>
	/// <returns>The XML of the tile set and the number of tiles.</returns>
	public static (byte[] TileSet, int Tiles) MakeTileSet(byte[] png)
	{
		PngReader reader;
		try
		{
			reader = new PngReader(new MemoryStream(png));
		}
		catch (Exception ex) when (ex is not OutOfMemoryException)
		{
			throw new InvalidOperationException("The tile image is not a PNG file.");
		}
		try
		{
			int width = reader.ImgInfo.Cols;
			int height = reader.ImgInfo.Rows;
			int channels = reader.ImgInfo.Channels;
			if (reader.ImgInfo.BitDepth != 8 || channels < 3 || reader.ImgInfo.Indexed)
				throw new InvalidOperationException("The tile image must be an 8-bit RGB or RGBA PNG file.");
			if (width % TileSize != 0 || height % TileSize != 0)
				throw new InvalidOperationException($"The width and the height of the tile image must be multiples of {TileSize} pixels. The image is {width} x {height}.");
			if (width > MaxTileImageSize || height > MaxTileImageSize)
				throw new InvalidOperationException($"The tile image can be {MaxTileImageSize} x {MaxTileImageSize} pixels at most. The image is {width} x {height}.");
			int columns = width / TileSize;
			int rows = height / TileSize;
			// A cell is a tile when one of its pixels is not fully transparent
			bool[,] used = new bool[columns, rows];
			for (int y = 0; y < height; y++)
			{
				byte[] line = reader.ReadRowByte(y).ScanlineB;
				for (int x = 0; x < width; x++)
				{
					if (channels < 4 || line[x * channels + 3] != 0)
						used[x / TileSize, y / TileSize] = true;
				}
			}
			XElement tiles = new XElement("tiles");
			int id = 1;
			for (int row = 0; row < rows; row++)
			{
				for (int column = 0; column < columns; column++)
				{
					if (used[column, row])
						tiles.Add(new XElement("tile", new XAttribute("id", id++), new XAttribute("texture", 0),
							new XAttribute("x", column * TileSize), new XAttribute("y", row * TileSize)));
				}
			}
			if (id == 1)
				throw new InvalidOperationException("The tile image has no tiles: all its pixels are transparent.");
			XElement tileSet = new XElement("tileset", new XElement("textures", new XElement("texture", "/TEXTURE")), tiles);
			return (ToBytes(tileSet), id - 1);
		}
		finally
		{
			reader.End();
		}
	}

	private static XElement Marker(string name, int x, int y)
	{
		return new XElement("marker", new XAttribute("name", name), new XAttribute("x", x), new XAttribute("y", y), new XAttribute("layer", "1"));
	}

	private static string Point(int x, int y) => string.Create(CultureInfo.InvariantCulture, $"{x},{y}");

	private static byte[] ToBytes(XElement element)
	{
		return Encoding.UTF8.GetBytes(new XDeclaration("1.0", "utf-8", null) + "\n" + element + "\n");
	}
}
