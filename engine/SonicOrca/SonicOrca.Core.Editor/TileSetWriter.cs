using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Writes the changes of the tile set tool into the XML of a tile set. The tool adds, moves and removes tiles with one
/// frame. The other content of the file stays as it is: the textures, the animated tiles and the tile sequences.
/// </summary>
public static class TileSetWriter
{
	/// <summary>True when the tool can move the tile: it has one frame and no animation group.</summary>
	public static bool IsStill(ITile tile) => tile is Tile still && still.Frames.Count == 1 && still.AnimationGroupKey == null;

	public static byte[] Patch(byte[] xml, TileSet tileSet)
	{
		XmlDocument document = new XmlDocument();
		using (MemoryStream stream = new MemoryStream(xml))
		{
			document.Load(stream);
		}
		Patch(document, tileSet);
		using MemoryStream output = new MemoryStream();
		using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true }))
		{
			document.Save(writer);
		}
		return output.ToArray();
	}

	public static void Patch(XmlDocument document, TileSet tileSet)
	{
		XmlElement root = (XmlElement)document.SelectSingleNode("tileset");
		XmlElement tiles = (XmlElement)root.SelectSingleNode("tiles");
		if (tiles == null)
		{
			tiles = document.CreateElement("tiles");
			root.AppendChild(tiles);
		}
		Dictionary<int, XmlElement> elements = new Dictionary<int, XmlElement>();
		foreach (XmlElement element in tiles.SelectNodes("tile").OfType<XmlElement>().ToArray())
		{
			if (!int.TryParse(element.GetAttribute("id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
			{
				continue;
			}
			if (tileSet[id] == null)
			{
				tiles.RemoveChild(element);
				continue;
			}
			elements[id] = element;
		}
		foreach (int id in tileSet.Keys.OrderBy(id => id))
		{
			if (!IsStill(tileSet[id]))
			{
				continue;
			}
			Tile.Frame frame = ((Tile)tileSet[id]).Frames[0];
			if (elements.TryGetValue(id, out XmlElement element))
			{
				// The frames of an element with frame elements are in these elements, so the tool does not change it
				if (element.SelectSingleNode("frame") != null)
				{
					continue;
				}
				SetIfChanged(element, "texture", frame.TextureId, 0);
				SetIfChanged(element, "x", frame.X, 0);
				SetIfChanged(element, "y", frame.Y, 0);
				continue;
			}
			element = document.CreateElement("tile");
			element.SetAttribute("id", id.ToString(CultureInfo.InvariantCulture));
			element.SetAttribute("texture", frame.TextureId.ToString(CultureInfo.InvariantCulture));
			element.SetAttribute("x", frame.X.ToString(CultureInfo.InvariantCulture));
			element.SetAttribute("y", frame.Y.ToString(CultureInfo.InvariantCulture));
			// The new tile goes after the tile with the next lower ID, so that the file stays in the order of the IDs
			XmlElement before = elements.Where(e => e.Key < id).OrderByDescending(e => e.Key).Select(e => e.Value).FirstOrDefault();
			if (before != null)
			{
				tiles.InsertAfter(element, before);
			}
			else
			{
				tiles.PrependChild(element);
			}
			elements[id] = element;
		}
	}

	/// <summary>Writes an attribute when its value changes. An attribute that is not in the element has its default value.</summary>
	private static void SetIfChanged(XmlElement element, string name, int value, int defaultValue)
	{
		int old = int.TryParse(element.GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : defaultValue;
		if (old != value)
		{
			element.SetAttribute(name, value.ToString(CultureInfo.InvariantCulture));
		}
	}
}
