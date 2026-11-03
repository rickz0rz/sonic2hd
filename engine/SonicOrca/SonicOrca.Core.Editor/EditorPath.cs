using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SonicOrca.Geometry;

namespace SonicOrca.Core.Editor;

/// <summary>A point of a path, relative to its object. <see cref="Marked"/> is the '#' before the point.</summary>
public readonly record struct EditorPathPoint(Vector2i Position, bool Marked);

/// <summary>Reads and writes the text of a path state variable (see <see cref="Objects.Metadata.EditorPathAttribute"/>).</summary>
public static class EditorPath
{
	/// <summary>Reads "x,y|x,y|#x,y". Points that are not valid are ignored, as the objects do.</summary>
	public static List<EditorPathPoint> Parse(string text)
	{
		List<EditorPathPoint> points = new List<EditorPathPoint>();
		foreach (string part in (text ?? "").Split('|'))
		{
			string element = part.Trim();
			bool marked = element.StartsWith('#');
			string[] xy = (marked ? element.Substring(1) : element).Split(',');
			if (xy.Length == 2 && int.TryParse(xy[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) &&
				int.TryParse(xy[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
			{
				points.Add(new EditorPathPoint(new Vector2i(x, y), marked));
			}
		}
		return points;
	}

	public static string Format(IEnumerable<EditorPathPoint> points)
	{
		return string.Join("|", points.Select(p => (p.Marked ? "#" : "") +
			p.Position.X.ToString(CultureInfo.InvariantCulture) + "," + p.Position.Y.ToString(CultureInfo.InvariantCulture)));
	}
}
