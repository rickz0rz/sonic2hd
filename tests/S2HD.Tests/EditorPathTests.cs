using System.Linq;
using SonicOrca.Core.Editor;
using SonicOrca.Geometry;
using Xunit;

namespace S2HD.Tests;

/// <summary>Tests the text of the paths that the level editor edits, for example the routes of a spin tube.</summary>
public sealed class EditorPathTests
{
	[Fact]
	public void Parse_ReadsThePointsAndTheMarks_AndIgnoresBadPoints()
	{
		var points = EditorPath.Parse(" 512,0 |512,384|#-320,384|x,1|7||-1,-2");

		Assert.Equal(
			new[] { (512, 0, false), (512, 384, false), (-320, 384, true), (-1, -2, false) },
			points.Select(p => (p.Position.X, p.Position.Y, p.Marked)));
	}

	[Fact]
	public void Format_ThenParse_KeepsThePoints()
	{
		var points = new[] { new EditorPathPoint(new Vector2i(0, 0), false), new EditorPathPoint(new Vector2i(-64, 128), true) };

		string text = EditorPath.Format(points);

		Assert.Equal("0,0|#-64,128", text);
		Assert.Equal(points, EditorPath.Parse(text));
	}

	[Fact]
	public void Parse_GivesNoPoints_ForNullOrEmptyText()
	{
		Assert.Empty(EditorPath.Parse(null));
		Assert.Empty(EditorPath.Parse(""));
	}
}
