using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SonicOrca.Core;

/// <summary>
/// Makes the files of a new level for the level editor. The level is a mod level (see SONICORCA.MODAREA.ModArea)
/// with the tile set of another level. The map has an empty background layer, an empty front layer, a flat floor
/// line and a start position. The binding has a signpost near the right end, so that the level can end.
/// </summary>
internal static class NewLevel
{
	public const int Columns = 128;

	public const int Rows = 24;

	public const int TileSize = 64;

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
		XElement back = new XElement("layer", new XAttribute("name", "back"),
			new XElement("tiles", new XAttribute("wrap_x", "true"), new XElement("row", "0")));
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
