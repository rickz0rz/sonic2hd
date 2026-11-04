using System.Linq;
using System.Text;
using System.Xml.Linq;
using SonicOrca.Core;
using SonicOrca.Core.Editor;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests that the tile set tool writes its changes into the XML of a tile set and keeps the other content.</summary>
public sealed class TileSetWriterTests
{
	private const string Xml = """
		<?xml version="1.0" encoding="utf-8"?>
		<tileset>
		  <textures>
		    <texture>/TEXTURE</texture>
		  </textures>
		  <tiles>
		    <tile id="1" texture="0" x="0" y="0" />
		    <tile id="2" texture="0" x="64" y="0" />
		    <tile id="3" x="128" y="0" delay="4">
		      <frame x="128" y="0" />
		      <frame x="192" y="0" />
		    </tile>
		    <tile id="5" texture="0" x="0" y="64" />
		    <tileseq id="9">
		      <tile id="1" />
		    </tileseq>
		  </tiles>
		</tileset>
		""";

	private static Tile Still(TileSet tileSet, int id, int x, int y) =>
		new Tile(tileSet, id, new[] { new Tile.Frame { X = x, Y = y, Opacity = 1f } });

	private static XElement Patch(TileSet tileSet) =>
		XElement.Parse(Encoding.UTF8.GetString(TileSetWriter.Patch(Encoding.UTF8.GetBytes(Xml.Trim()), tileSet)).TrimStart('﻿'));

	[Fact]
	public void Patch_AddsMovesAndRemovesStillTiles()
	{
		TileSet tileSet = new TileSet();
		// Tile 1 stays, tile 2 moves, tile 5 is removed, and tile 4 is new
		tileSet[1] = Still(tileSet, 1, 0, 0);
		tileSet[2] = Still(tileSet, 2, 256, 128);
		tileSet[3] = new Tile(tileSet, 3, new[] { new Tile.Frame { X = 128, Opacity = 1f }, new Tile.Frame { X = 192, Opacity = 1f } });
		tileSet[4] = Still(tileSet, 4, 320, 64);
		tileSet[9] = new TileSequence(tileSet, 9, new[] { 1 });
		XElement tiles = Patch(tileSet).Element("tiles");
		XElement[] elements = tiles.Elements("tile").ToArray();
		Assert.Equal(new[] { "1", "2", "3", "4" }, elements.Select(e => (string)e.Attribute("id")));
		Assert.Equal(("256", "128"), ((string)elements[1].Attribute("x"), (string)elements[1].Attribute("y")));
		Assert.Equal(("320", "64", "0"), ((string)elements[3].Attribute("x"), (string)elements[3].Attribute("y"), (string)elements[3].Attribute("texture")));
		// The animated tile and the tile sequence stay as they are
		Assert.Equal(2, elements[2].Elements("frame").Count());
		Assert.Equal("4", (string)elements[2].Attribute("delay"));
		Assert.NotNull(tiles.Element("tileseq"));
	}

	[Fact]
	public void Patch_KeepsAFileWithoutChanges()
	{
		TileSet tileSet = new TileSet();
		tileSet[1] = Still(tileSet, 1, 0, 0);
		tileSet[2] = Still(tileSet, 2, 64, 0);
		tileSet[3] = new Tile(tileSet, 3, new[] { new Tile.Frame { X = 128, Opacity = 1f }, new Tile.Frame { X = 192, Opacity = 1f } });
		tileSet[5] = Still(tileSet, 5, 0, 64);
		tileSet[9] = new TileSequence(tileSet, 9, new[] { 1 });
		Assert.True(XNode.DeepEquals(XElement.Parse(Xml.Trim()), Patch(tileSet)));
	}
}
