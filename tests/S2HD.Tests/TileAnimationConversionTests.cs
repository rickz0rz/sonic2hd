using System.Linq;
using System.Xml.Linq;
using SonicOrca.Core.Editor;
using SonicOrca.Original;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the move of the frames of the animated tiles of a tile set into an ANIGROUP.</summary>
public sealed class TileAnimationConversionTests
{
	private const string Sample = """
		<?xml version="1.0" encoding="utf-8"?>
		<tileset>
		  <textures>
		    <texture>/TEXTURE</texture>
		    <texture>SONICORCA/LEVELS/SHARED</texture>
		  </textures>
		  <tiles>
		    <tile id="1" texture="0" x="64" y="0" />
		    <tile id="2" blend ="additive" delay="8">
		      <frame texture="1" x="128" y="64" opacity="1" />
		      <frame texture="1" x="192" y="64" opacity="0" delay="3" />
		    </tile>
		    <tile id="3">
		      <frame x="0" y="128" />
		    </tile>
		    <tile id="4" texture="1" opacity="0.5">
		      <frame x="0" y="0" delay="2" />
		      <frame x="64" y="0" texture="0" delay="2" />
		      <frame x="128" y="0" delay="4" />
		    </tile>
		    <tileseq id="5">
		      <tile id="2" />
		      <tile id="4" />
		    </tileseq>
		  </tiles>
		</tileset>
		""";

	[Fact]
	public void Convert_MovesTheFramesIntoTheGroup_AndKeepsTheOtherTiles()
	{
		var (tileSetXml, groupXml) = TileAnimationConversion.Convert(Sample).Value;

		XElement[] tiles = XDocument.Parse(tileSetXml).Root.Element("tiles").Elements("tile").ToArray();
		Assert.Equal("<tile id=\"1\" texture=\"0\" x=\"64\" y=\"0\" />", tiles[0].ToString());
		Assert.Equal("<tile id=\"2\" blend=\"additive\" texture=\"1\" x=\"128\" y=\"64\" anigroup=\"/ANIGROUP\" animation=\"0\" />", tiles[1].ToString());
		// A tile with one frame does not animate, so it stays as it is
		Assert.Single(tiles[2].Elements("frame"));
		Assert.Equal("<tile id=\"4\" texture=\"1\" x=\"0\" y=\"0\" anigroup=\"/ANIGROUP\" animation=\"1\" />", tiles[3].ToString());
		Assert.Equal(2, XDocument.Parse(tileSetXml).Root.Element("tiles").Element("tileseq").Elements("tile").Count());

		AnimationGroupDocument group = AnimationGroupDocument.Parse(groupXml);
		// A relative key gets one more "/", because the group is a child of the tile set
		Assert.Equal(new[] { "//TEXTURE", "SONICORCA/LEVELS/SHARED" }, group.Textures);
		Assert.Equal(2, group.Animations.Count);
		Assert.Equal("1: tile 4", group.Animations[1].Name);
		Assert.Equal(
			new[] { (1, 128, 64, 64, 64, 8, 1f), (1, 192, 64, 64, 64, 3, 0f) },
			group.Animations[0].Frames.Select(f => (f.Texture, f.X, f.Y, f.Width, f.Height, f.Delay, f.Opacity)));
		// The frames take the texture, the delay and the opacity of the tile when they do not have them
		Assert.Equal(
			new[] { (1, 0, 2, 0.5f), (0, 64, 2, 0.5f), (1, 128, 4, 0.5f) },
			group.Animations[1].Frames.Select(f => (f.Texture, f.X, f.Delay, f.Opacity)));
	}

	[Fact]
	public void Convert_GivesNull_WhenNoTileAnimates()
	{
		Assert.Null(TileAnimationConversion.Convert("<tileset><textures /><tiles><tile id=\"1\" x=\"0\" y=\"0\" /></tiles></tileset>"));
	}
}
