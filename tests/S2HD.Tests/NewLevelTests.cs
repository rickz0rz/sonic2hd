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
