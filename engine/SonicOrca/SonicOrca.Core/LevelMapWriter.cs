using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

	private const int TileIndexMask = 0xFFF;

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
			xmlWriter.WriteStartElement("collision");
			xmlWriter.WriteStartElement("paths");
			foreach (int collisionPathLayer in _levelMap.CollisionPathLayers)
			{
				xmlWriter.WriteStartElement("path");
				xmlWriter.WriteAttributeString("layer", collisionPathLayer.ToString());
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
			xmlWriter.WriteStartElement("vectors");
			foreach (CollisionVector collisionVector in _levelMap.CollisionVectors)
			{
				xmlWriter.WriteStartElement("vector");
				xmlWriter.WriteAttributeString("a", collisionVector.RelativeA.X + "," + collisionVector.RelativeA.Y);
				xmlWriter.WriteAttributeString("b", collisionVector.RelativeB.X + "," + collisionVector.RelativeB.Y);
				xmlWriter.WriteAttributeString("paths", collisionVector.Paths.ToString());
				xmlWriter.WriteAttributeString("flags", ((int)collisionVector.Flags).ToString());
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();
		}
		if (_levelMap.Markers.Count > 0)
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
				xmlWriter.WriteAttributeString("x", marker.Bounds.X.ToString());
				xmlWriter.WriteAttributeString("y", marker.Bounds.Y.ToString());
				if (marker.Bounds.Width > 0)
				{
					xmlWriter.WriteAttributeString("width", marker.Bounds.Width.ToString());
				}
				if (marker.Bounds.Height > 0)
				{
					xmlWriter.WriteAttributeString("height", marker.Bounds.Height.ToString());
				}
				if (marker.Layer != null)
				{
					xmlWriter.WriteAttributeString("layer", _levelMap.Layers.IndexOf(marker.Layer).ToString());
				}
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
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
			xmlWriter.WriteStartElement("rowdefinitions");
			foreach (LayerRowDefinition layerRowDefinition in layer.LayerRowDefinitions)
			{
				xmlWriter.WriteStartElement("rowdefinition");
				if (layerRowDefinition.Width != 0)
				{
					xmlWriter.WriteAttributeString("width", layerRowDefinition.Width.ToString());
				}
				xmlWriter.WriteAttributeString("height", layerRowDefinition.Height.ToString());
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
		if (layer.Lighting.Type != LevelLayerLightingType.Outside || layer.Lighting.Light != 1.0)
		{
			xmlWriter.WriteStartElement("lighting");
			xmlWriter.WriteAttributeString("type", layer.Lighting.Type.ToString().ToLower());
			xmlWriter.WriteAttributeString("light", layer.Lighting.Light.ToString());
			xmlWriter.WriteEndElement();
		}
		if (layer.Shadows.Count > 0)
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
				xmlWriter.WriteAttributeString("layerIndexOffset", shadow.LayerIndexOffset.ToString());
				xmlWriter.WriteAttributeString("dx", shadow.Displacement.X.ToString());
				xmlWriter.WriteAttributeString("dy", shadow.Displacement.Y.ToString());
				if (shadow.Softness != 0)
				{
					xmlWriter.WriteAttributeString("softness", shadow.Softness.ToString());
				}
				if (shadow.Colour != LevelLayerShadow.DefaultShadowColour)
				{
					xmlWriter.WriteAttributeString("colour", shadow.Colour.ToHexString());
				}
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
		}
		if (layer.Rows > 0)
		{
			xmlWriter.WriteStartElement("tiles");
			if (layer.OffsetY != 0)
			{
				xmlWriter.WriteAttributeString("offset_y", layer.OffsetY.ToString());
			}
			if (layer.AutomaticYParallax)
			{
				xmlWriter.WriteAttributeString("parallax_y", "auto");
			}
			else if (layer.ParallaxY != 1.0)
			{
				xmlWriter.WriteAttributeString("parallax_y", layer.ParallaxY.ToString(CultureInfo.InvariantCulture));
			}
			if (layer.WrapX)
			{
				xmlWriter.WriteAttributeString("wrap_x", "true");
			}
			if (layer.WrapY)
			{
				xmlWriter.WriteAttributeString("wrap_y", "true");
			}
			int y;
			for (y = 0; y < layer.Rows; y++)
			{
				xmlWriter.WriteStartElement("row");
				// Write each tile as an optional "h" and "v" flip prefix, then the tile index.
				IEnumerable<int> rowTiles = from x in Enumerable.Range(0, layer.Columns)
											select layer.Tiles[x, y];
				xmlWriter.WriteString(string.Join(",", rowTiles.Select(tileIndex => (((tileIndex & FlipHorizontalFlag) != 0) ? "h" : string.Empty) + (((tileIndex & FlipVerticalFlag) != 0) ? "v" : string.Empty) + (tileIndex & TileIndexMask))));
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
	}
}
