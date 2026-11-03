using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SonicOrca.Core;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the files of a new level that the level editor makes, and the tile set of a mod level.</summary>
public sealed class NewLevelTests
{
	[Theory]
	[InlineData("hills2", null)]
	[InlineData("", "The short name is empty.")]
	[InlineData("my level", "The short name can have only letters and digits.")]
	[InlineData("abcdefghijklmnopq", "The short name has more than 16 characters.")]
	public void CheckMnemonic_GivesTheProblem(string mnemonic, string problem)
	{
		Assert.Equal(problem, NewLevel.CheckMnemonic(mnemonic));
	}

	[Fact]
	public void MakeTileSet_GivesATileForEachCellThatIsNotTransparent()
	{
		// 3 x 2 cells: the cell at column 1 of row 0 is fully transparent
		byte[] png = MakePng(192, 128, (x, y) => x / 64 == 1 && y / 64 == 0 ? (byte)0 : (byte)255);

		(byte[] tileSet, int count) = NewLevel.MakeTileSet(png);

		Assert.Equal(5, count);
		XElement[] tiles = XElement.Parse(Encoding.UTF8.GetString(tileSet).TrimStart('\uFEFF')).Element("tiles").Elements("tile").ToArray();
		Assert.Equal(new[] { (1, 0, 0), (2, 128, 0), (3, 0, 64), (4, 64, 64), (5, 128, 64) },
			tiles.Select(t => ((int)t.Attribute("id"), (int)t.Attribute("x"), (int)t.Attribute("y"))));
	}

	[Theory]
	[InlineData(100, 64, "multiples of 64")]
	[InlineData(64, 64, "no tiles")]
	public void MakeTileSet_TellsWhyAnImageCannotBeATileSet(int width, int height, string problem)
	{
		byte[] png = MakePng(width, height, (_, _) => 0);

		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => NewLevel.MakeTileSet(png));
		Assert.Contains(problem, ex.Message);
	}

	/// <summary>Makes an RGBA PNG file with grey pixels and the alpha of the function.</summary>
	internal static byte[] MakePng(int width, int height, Func<int, int, byte> alpha)
	{
		var raw = new MemoryStream();
		for (int y = 0; y < height; y++)
		{
			raw.WriteByte(0);
			for (int x = 0; x < width; x++)
			{
				raw.Write(new byte[] { 128, 128, 128, alpha(x, y) });
			}
		}
		var compressed = new MemoryStream();
		using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
		{
			raw.Position = 0;
			raw.CopyTo(zlib);
		}
		var png = new MemoryStream();
		png.Write(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 13, 10, 26, 10 });
		byte[] header = new byte[13];
		WriteBigEndian(header, 0, width);
		WriteBigEndian(header, 4, height);
		header[8] = 8;
		header[9] = 6;
		WriteChunk(png, "IHDR", header);
		WriteChunk(png, "IDAT", compressed.ToArray());
		WriteChunk(png, "IEND", Array.Empty<byte>());
		return png.ToArray();
	}

	private static void WriteChunk(Stream stream, string type, byte[] data)
	{
		byte[] length = new byte[4];
		WriteBigEndian(length, 0, data.Length);
		stream.Write(length);
		byte[] typeAndData = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
		stream.Write(typeAndData);
		byte[] crc = new byte[4];
		WriteBigEndian(crc, 0, (int)Crc32(typeAndData));
		stream.Write(crc);
	}

	private static void WriteBigEndian(byte[] buffer, int offset, int value)
	{
		buffer[offset] = (byte)(value >> 24);
		buffer[offset + 1] = (byte)(value >> 16);
		buffer[offset + 2] = (byte)(value >> 8);
		buffer[offset + 3] = (byte)value;
	}

	private static uint Crc32(byte[] data)
	{
		uint crc = 0xFFFFFFFF;
		foreach (byte b in data)
		{
			crc ^= b;
			for (int i = 0; i < 8; i++)
			{
				crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
			}
		}
		return ~crc;
	}

	[Fact]
	public void Map_HasAFloorUnderTheStart_AndTheBoundsOfTheLevel()
	{
		XElement map = Parse(NewLevel.MakeMap());

		XElement[] layers = map.Element("tiles").Elements("layer").ToArray();
		Assert.Equal(new[] { "back", "front" }, layers.Select(layer => (string)layer.Attribute("name")));
		XElement[] rows = layers[1].Element("tiles").Elements("row").ToArray();
		Assert.Equal(NewLevel.Rows, rows.Length);
		Assert.All(rows, row => Assert.Equal(NewLevel.Columns, row.Value.Split(',').Length));
		Assert.All(map.Element("collision").Element("paths").Elements("path"), path => Assert.Equal("1", (string)path.Attribute("layer")));

		// The floor goes from the left to the right of the level without a gap, so that it is solid on its top side
		XElement[] vectors = map.Element("collision").Element("vectors").Elements("vector").ToArray();
		Assert.Equal($"0,{NewLevel.FloorY}", (string)vectors[0].Attribute("a"));
		Assert.Equal($"{NewLevel.Columns * NewLevel.TileSize},{NewLevel.FloorY}", (string)vectors[^1].Attribute("b"));
		for (int i = 1; i < vectors.Length; i++)
		{
			Assert.Equal((string)vectors[i - 1].Attribute("b"), (string)vectors[i].Attribute("a"));
		}

		XElement start = map.Element("markers").Elements("marker").Single(marker => (string)marker.Attribute("name") == "startpos");
		Assert.True((int)start.Attribute("y") < NewLevel.FloorY);
	}

	[Fact]
	public void Binding_HasASignpost()
	{
		XElement binding = Parse(NewLevel.MakeBinding());

		Assert.Equal("SONICORCA/OBJECTS/SIGNPOST", binding.Descendants("Key").Single().Value);
	}

	[Fact]
	public void TileSet_OfAModLevel_IsTheTileSetOfItsInfoFile()
	{
		ResourceTree tree = new ResourceTree();
		Add(tree, "SONICORCA/LEVELS/EHZ/TILESET", "<tileset />");
		Add(tree, "SONICORCA/LEVELS/HILLS2/INFO", Encoding.UTF8.GetString(NewLevel.MakeInfo("Hills 2", "SONICORCA/LEVELS/EHZ/TILESET")));
		Add(tree, "SONICORCA/LEVELS/MEADOW/TILESET", "<tileset />");

		Assert.Equal("SONICORCA/LEVELS/EHZ/TILESET", Levels.GetTileSetResourceKey("hills2", tree));
		Assert.Equal("SONICORCA/LEVELS/MEADOW/TILESET", Levels.GetTileSetResourceKey("meadow", tree));
		Assert.Null(Levels.GetTileSetResourceKey("cpz", tree));
	}

	private static XElement Parse(byte[] data) => XDocument.Parse(Encoding.UTF8.GetString(data)).Root;

	private static void Add(ResourceTree tree, string key, string text)
	{
		tree.SetOrAdd(key, new Resource(key, ResourceTypeIdentifier.Xml, new MemoryResourceSource(Encoding.UTF8.GetBytes(text))));
	}
}
