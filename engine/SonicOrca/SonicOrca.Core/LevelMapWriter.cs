using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using SonicOrca.Core.Collision;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// Writes a level map to a map XML file.
/// </summary>
public class LevelMapWriter
{
	// Tile value flags. They must agree with the flags that LevelMapResourceType reads.
	private const int FlipHorizontalFlag = 0x4000;

	private const int FlipVerticalFlag = 0x8000;

	// All the bits of a tile value other than the flip flags. The reader keeps them too.
	private const int TileIndexMask = ~(FlipHorizontalFlag | FlipVerticalFlag) & 0xFFFF;

	private readonly LevelMap _levelMap;

	public LevelMapWriter(LevelMap map)
	{
		_levelMap = map;
	}

	public void Save(string path)
	{
		XmlWriterSettings settings = new XmlWriterSettings
		{
			Indent = true
		};
		using XmlWriter xmlWriter = XmlWriter.Create(path, settings);
		xmlWriter.WriteStartDocument();
		xmlWriter.WriteStartElement("map");
		xmlWriter.WriteStartElement("tiles");
		foreach (ILevelLayerTreeNode child in _levelMap.LayerTree.Children)
		{
			WriteLevelLayerTreeNode(xmlWriter, child);
		}
		xmlWriter.WriteEndElement();
		if (_levelMap.CollisionVectors.Count > 0)
		{
			WriteCollision(xmlWriter);
		}
		if (_levelMap.Markers.Count > 0)
		{
			WriteMarkers(xmlWriter);
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
	}

	/// <summary>
	/// Writes parts of the map into an existing map document, and keeps all the other content of the document. The
	/// level editor saves a map in this way, so that a value that this writer does not know stays in the file.
	/// </summary>
	/// <param name="document">A document that the map was read from.</param>
	/// <param name="tiles">Write the tile rows of each layer. The layers of the document must be the layers of the map, in the same order.</param>
	/// <param name="collision">Write the collision paths and vectors.</param>
	/// <param name="markers">Write the markers.</param>
	/// <param name="scroll">Write the scroll settings of each layer: the row definitions, the Y parallax, the Y offset and the wrap.</param>
	/// <param name="documentLayers">
	/// The layer of each layer element of the document, when the layers of the map changed. The patch then removes the
	/// element of each layer that the map does not have, and adds an element for each new layer of the map.
	/// </param>
	public void Patch(XmlDocument document, bool tiles, bool collision, bool markers, bool scroll = false, IReadOnlyList<LevelLayer> documentLayers = null)
	{
		XmlNode mapNode = document.SelectSingleNode("map") ?? throw new InvalidOperationException("The document has no map element.");
		if (documentLayers != null)
		{
			PatchLayerList(document, documentLayers);
		}
		if (tiles || scroll)
		{
			XmlNode[] layerNodes = document.SelectNodes("map/tiles//layer").OfType<XmlNode>().ToArray();
			if (layerNodes.Length != _levelMap.Layers.Count)
			{
				throw new InvalidOperationException("The layers of the document are not the layers of the map.");
			}
			for (int i = 0; i < layerNodes.Length; i++)
			{
				if (tiles)
				{
					PatchTileRows(document, layerNodes[i], _levelMap.Layers[i]);
				}
				if (scroll)
				{
					PatchScroll(document, layerNodes[i], _levelMap.Layers[i]);
				}
			}
		}
		if (collision)
		{
			ReplaceElement(document, mapNode, "collision", _levelMap.CollisionVectors.Count > 0 ? WriteCollision : null);
		}
		if (markers)
		{
			ReplaceElement(document, mapNode, "markers", _levelMap.Markers.Count > 0 ? WriteMarkers : null);
		}
	}

	private static void PatchTileRows(XmlDocument document, XmlNode layerNode, LevelLayer layer)
	{
		XmlNode tilesNode = layerNode.SelectSingleNode("tiles");
		if (tilesNode == null)
		{
			if (layer.Rows == 0)
			{
				return;
			}
			tilesNode = layerNode.AppendChild(document.CreateElement("tiles"));
		}
		foreach (XmlNode rowNode in tilesNode.SelectNodes("row").OfType<XmlNode>().ToArray())
		{
			tilesNode.RemoveChild(rowNode);
		}
		for (int y = 0; y < layer.Rows; y++)
		{
			XmlElement rowElement = document.CreateElement("row");
			rowElement.InnerText = FormatTileRow(layer, y);
			tilesNode.AppendChild(rowElement);
		}
	}

	private void PatchLayerList(XmlDocument document, IReadOnlyList<LevelLayer> documentLayers)
	{
		XmlNode[] layerNodes = document.SelectNodes("map/tiles//layer").OfType<XmlNode>().ToArray();
		if (layerNodes.Length != documentLayers.Count)
		{
			throw new InvalidOperationException("The layers of the document are not the layers of the map.");
		}
		Dictionary<LevelLayer, XmlNode> nodes = new Dictionary<LevelLayer, XmlNode>();
		for (int i = 0; i < layerNodes.Length; i++)
		{
			if (_levelMap.Layers.Contains(documentLayers[i]))
			{
				nodes[documentLayers[i]] = layerNodes[i];
				// A shadow refers to its caster by an index offset, so the offsets can change
				LevelLayer layer = documentLayers[i];
				ReplaceElement(document, layerNodes[i], "shadows", layer.Shadows.Count > 0 ? xmlWriter => WriteShadows(xmlWriter, layer) : null);
			}
			else
			{
				layerNodes[i].ParentNode.RemoveChild(layerNodes[i]);
			}
		}
		// A new layer goes after the element of the layer before it, or at the start of the tiles when it is the first layer
		XmlNode previous = null;
		foreach (LevelLayer layer in _levelMap.Layers)
		{
			if (!nodes.TryGetValue(layer, out XmlNode node))
			{
				node = CreateFragmentNode(document, xmlWriter => WriteLevelLayerTreeNode(xmlWriter, layer));
				if (previous != null)
				{
					previous.ParentNode.InsertAfter(node, previous);
				}
				else
				{
					XmlNode tilesNode = document.SelectSingleNode("map/tiles") ?? document.SelectSingleNode("map").AppendChild(document.CreateElement("tiles"));
					tilesNode.InsertBefore(node, tilesNode.FirstChild);
				}
			}
			previous = node;
		}
	}

	private static void PatchScroll(XmlDocument document, XmlNode layerNode, LevelLayer layer)
	{
		ReplaceElement(document, layerNode, "rowdefinitions", layer.LayerRowDefinitions.Count > 0 ? xmlWriter => WriteRowDefinitions(xmlWriter, layer) : null);
		// The row definitions come before the other settings of a layer
		XmlNode rowDefinitionsNode = layerNode.SelectSingleNode("rowdefinitions");
		if (rowDefinitionsNode != null && rowDefinitionsNode != layerNode.FirstChild)
		{
			layerNode.InsertBefore(layerNode.RemoveChild(rowDefinitionsNode), layerNode.FirstChild);
		}
		XmlElement tilesNode = layerNode.SelectSingleNode("tiles") as XmlElement;
		if (tilesNode == null)
		{
			return;
		}
		foreach (string name in new[] { "offset_y", "parallax_y", "wrap_x", "wrap_y" })
		{
			tilesNode.RemoveAttribute(name);
		}
		foreach ((string name, string value) in GetScrollAttributes(layer))
		{
			tilesNode.SetAttribute(name, value);
		}
	}

	/// <summary>Replaces a child element with the output of a write method, or removes it when the method is null.</summary>
	private static void ReplaceElement(XmlDocument document, XmlNode parent, string name, Action<XmlWriter> write)
	{
		XmlNode oldNode = parent.SelectSingleNode(name);
		XmlNode newNode = write != null ? CreateFragmentNode(document, write) : null;
		if (oldNode != null && newNode != null)
		{
			parent.ReplaceChild(newNode, oldNode);
		}
		else if (oldNode != null)
		{
			parent.RemoveChild(oldNode);
		}
		else if (newNode != null)
		{
			parent.AppendChild(newNode);
		}
	}

	/// <summary>Makes a node of the document from the output of a write method.</summary>
	private static XmlNode CreateFragmentNode(XmlDocument document, Action<XmlWriter> write)
	{
		StringBuilder text = new StringBuilder();
		using (XmlWriter xmlWriter = XmlWriter.Create(text, new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment, Indent = true }))
		{
			write(xmlWriter);
		}
		XmlDocumentFragment fragment = document.CreateDocumentFragment();
		fragment.InnerXml = text.ToString();
		return fragment.FirstChild;
	}

	private void WriteCollision(XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("collision");
		xmlWriter.WriteStartElement("paths");
		foreach (int collisionPathLayer in _levelMap.CollisionPathLayers)
		{
			xmlWriter.WriteStartElement("path");
			xmlWriter.WriteAttributeString("layer", collisionPathLayer.ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteStartElement("vectors");
		foreach (CollisionVector collisionVector in _levelMap.CollisionVectors)
		{
			xmlWriter.WriteStartElement("vector");
			xmlWriter.WriteAttributeString("a", FormattableString.Invariant($"{collisionVector.RelativeA.X},{collisionVector.RelativeA.Y}"));
			xmlWriter.WriteAttributeString("b", FormattableString.Invariant($"{collisionVector.RelativeB.X},{collisionVector.RelativeB.Y}"));
			xmlWriter.WriteAttributeString("paths", collisionVector.Paths.ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteAttributeString("flags", ((int)collisionVector.Flags).ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndElement();
	}

	private void WriteMarkers(XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("markers");
		foreach (LevelMarker marker in _levelMap.Markers)
		{
			xmlWriter.WriteStartElement("marker");
			if (!string.IsNullOrEmpty(marker.Name))
			{
				xmlWriter.WriteAttributeString("name", marker.Name);
			}
			if (!string.IsNullOrEmpty(marker.Tag))
			{
				xmlWriter.WriteAttributeString("tag", marker.Tag);
			}
			xmlWriter.WriteAttributeString("x", marker.Bounds.X.ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteAttributeString("y", marker.Bounds.Y.ToString(CultureInfo.InvariantCulture));
			if (marker.Bounds.Width > 0)
			{
				xmlWriter.WriteAttributeString("width", marker.Bounds.Width.ToString(CultureInfo.InvariantCulture));
			}
			if (marker.Bounds.Height > 0)
			{
				xmlWriter.WriteAttributeString("height", marker.Bounds.Height.ToString(CultureInfo.InvariantCulture));
			}
			if (marker.Layer != null)
			{
				xmlWriter.WriteAttributeString("layer", _levelMap.Layers.IndexOf(marker.Layer).ToString(CultureInfo.InvariantCulture));
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
	}

	/// <summary>Writes each tile of a row as an optional "h" and "v" flip prefix, then the tile index.</summary>
	public static string FormatTileRow(LevelLayer layer, int y)
	{
		return string.Join(",", Enumerable.Range(0, layer.Columns).Select(x =>
		{
			int tileValue = layer.Tiles[x, y];
			return ((tileValue & FlipHorizontalFlag) != 0 ? "h" : string.Empty) +
				((tileValue & FlipVerticalFlag) != 0 ? "v" : string.Empty) +
				(tileValue & TileIndexMask).ToString(CultureInfo.InvariantCulture);
		}));
	}

	private static void WriteShadows(XmlWriter xmlWriter, LevelLayer layer)
	{
		xmlWriter.WriteStartElement("shadows");
		foreach (LevelLayerShadow shadow in layer.Shadows)
		{
			xmlWriter.WriteStartElement("shadow");
			if (!shadow.Tiles)
			{
				xmlWriter.WriteAttributeString("tiles", "false");
			}
			if (!shadow.Objects)
			{
				xmlWriter.WriteAttributeString("objects", "false");
			}
			xmlWriter.WriteAttributeString("layerIndexOffset", shadow.LayerIndexOffset.ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteAttributeString("dx", shadow.Displacement.X.ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteAttributeString("dy", shadow.Displacement.Y.ToString(CultureInfo.InvariantCulture));
			if (shadow.Softness != 0)
			{
				xmlWriter.WriteAttributeString("softness", shadow.Softness.ToString(CultureInfo.InvariantCulture));
			}
			if (shadow.Colour != LevelLayerShadow.DefaultShadowColour)
			{
				xmlWriter.WriteAttributeString("colour", shadow.Colour.ToHexString());
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
	}

	private static void WriteRowDefinitions(XmlWriter xmlWriter, LevelLayer layer)
	{
		xmlWriter.WriteStartElement("rowdefinitions");
		foreach (LayerRowDefinition layerRowDefinition in layer.LayerRowDefinitions)
		{
			xmlWriter.WriteStartElement("rowdefinition");
			if (layerRowDefinition.Width != 0)
			{
				xmlWriter.WriteAttributeString("width", layerRowDefinition.Width.ToString(CultureInfo.InvariantCulture));
			}
			xmlWriter.WriteAttributeString("height", layerRowDefinition.Height.ToString(CultureInfo.InvariantCulture));
			if (layerRowDefinition.Parallax != 1.0)
			{
				xmlWriter.WriteAttributeString("parallax_x", layerRowDefinition.Parallax.ToString(CultureInfo.InvariantCulture));
			}
			if (layerRowDefinition.Velocity != 0.0)
			{
				xmlWriter.WriteAttributeString("velocity_x", layerRowDefinition.Velocity.ToString(CultureInfo.InvariantCulture));
			}
			if (layerRowDefinition.InitialOffset != 0)
			{
				xmlWriter.WriteAttributeString("offset_x", layerRowDefinition.InitialOffset.ToString(CultureInfo.InvariantCulture));
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
	}

	/// <summary>The attributes of the tiles element of a layer for its Y offset, its Y parallax and its wrap.</summary>
	private static IEnumerable<(string Name, string Value)> GetScrollAttributes(LevelLayer layer)
	{
		if (layer.OffsetY != 0)
		{
			yield return ("offset_y", layer.OffsetY.ToString(CultureInfo.InvariantCulture));
		}
		if (layer.AutomaticYParallax)
		{
			yield return ("parallax_y", "auto");
		}
		else if (layer.ParallaxY != 1.0)
		{
			yield return ("parallax_y", layer.ParallaxY.ToString(CultureInfo.InvariantCulture));
		}
		if (layer.WrapX)
		{
			yield return ("wrap_x", "true");
		}
		if (layer.WrapY)
		{
			yield return ("wrap_y", "true");
		}
	}

	private void WriteLevelLayerTreeNode(XmlWriter xmlWriter, ILevelLayerTreeNode node)
	{
		if (node is LevelLayerGroup)
		{
			xmlWriter.WriteStartElement("layergroup");
			xmlWriter.WriteAttributeString("name", node.Name);
			foreach (ILevelLayerTreeNode child in node.Children)
			{
				WriteLevelLayerTreeNode(xmlWriter, child);
			}
			xmlWriter.WriteEndElement();
			return;
		}
		LevelLayer layer = (LevelLayer)node;
		xmlWriter.WriteStartElement("layer");
		if (!string.IsNullOrEmpty(layer.Name))
		{
			xmlWriter.WriteAttributeString("name", layer.Name);
		}
		if (layer.MiniMapColour != default(Colour))
		{
			xmlWriter.WriteAttributeString("minimap_colour", layer.MiniMapColour.ToHexString());
		}
		if (!layer.Visible)
		{
			xmlWriter.WriteAttributeString("visible", "false");
		}
		if (layer.LayerRowDefinitions.Count > 0)
		{
			WriteRowDefinitions(xmlWriter, layer);
		}
		if (layer.Lighting.Type != LevelLayerLightingType.Outside || layer.Lighting.Light != 1.0)
		{
			xmlWriter.WriteStartElement("lighting");
			xmlWriter.WriteAttributeString("type", layer.Lighting.Type.ToString().ToLower());
			xmlWriter.WriteAttributeString("light", layer.Lighting.Light.ToString(CultureInfo.InvariantCulture));
			xmlWriter.WriteEndElement();
		}
		if (layer.Shadows.Count > 0)
		{
			WriteShadows(xmlWriter, layer);
		}
		if (layer.Rows > 0)
		{
			xmlWriter.WriteStartElement("tiles");
			foreach ((string name, string value) in GetScrollAttributes(layer))
			{
				xmlWriter.WriteAttributeString(name, value);
			}
			for (int y = 0; y < layer.Rows; y++)
			{
				xmlWriter.WriteElementString("row", FormatTileRow(layer, y));
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
	}
}
