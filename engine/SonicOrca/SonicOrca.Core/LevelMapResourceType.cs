using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Core.Collision;
using SonicOrca.Core.Extensions;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Resources;

namespace SonicOrca.Core;

internal class LevelMapResourceType : ResourceType
{
	public override string Name => "map, xml";

	public override string DefaultExtension => ".map.xml";

	public override bool CompressByDefault => true;

	public LevelMapResourceType()
		: base(ResourceTypeIdentifier.Map)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		LevelMap map = new LevelMap();
		map.Resource = e.Resource;
		XmlNode parent = xmlDocument.SelectSingleNode("map/tiles");
		ILevelLayerTreeNode[] items = GetLevelLayersFromXmlNode(parent, map).ToArray();
		map.LayerTree.Children.Clear();
		map.LayerTree.Children.AddRange(items);
		LevelLayer[] items2 = (from x in map.LayerTree.GetDescendantsOrdered()
			select x as LevelLayer into x
			where x != null
			select x).ToArray();
		map.Layers.Clear();
		map.Layers.AddRange(items2);
		XmlNode xmlNode = xmlDocument.SelectSingleNode("map/collision");
		if (xmlNode != null)
		{
			map.CollisionPathLayers.AddRange(GetCollisionPathsFromXmlNode(xmlNode.SelectSingleNode("paths")));
			map.CollisionVectors.AddRange(GetCollisionVectorsFromXmlNode(xmlNode.SelectSingleNode("vectors")));
		}
		XmlNode xmlNode2 = xmlDocument.SelectSingleNode("map/markers");
		if (xmlNode2 != null)
		{
			map.Markers.AddRange(from x in xmlNode2.SelectNodes("marker").OfType<XmlNode>()
				select GetMarkerFromXmlNode(map, x));
		}
		return map;
	}

	private IEnumerable<ILevelLayerTreeNode> GetLevelLayersFromXmlNode(XmlNode parent, LevelMap map)
	{
		foreach (XmlNode node in parent.ChildNodes)
		{
			if (node.Name == "layergroup")
			{
				if (!node.TryGetAttributeValue("name", out var value))
				{
					value = string.Empty;
				}
				LevelLayerGroup levelLayerGroup = new LevelLayerGroup(value);
				foreach (ILevelLayerTreeNode item in GetLevelLayersFromXmlNode(node, map))
				{
					levelLayerGroup.Children.Add(item);
				}
				yield return levelLayerGroup;
			}
			else if (node.Name == "layer")
			{
				yield return GetLevelLayerFromXmlNode(node, map);
			}
		}
	}

	private LevelLayer GetLevelLayerFromXmlNode(XmlNode node, LevelMap map)
	{
		LevelLayer levelLayer = new LevelLayer(map);
		if (node.TryGetAttributeValue("name", out var value))
		{
			levelLayer.Name = value;
		}
		if (node.TryGetAttributeValue("minimap_colour", out value) && Colour.TryParseHex(value, out var result))
		{
			levelLayer.MiniMapColour = result;
		}
		levelLayer.LayerRowDefinitions.AddRange(from x in node.SelectNodes("rowdefinitions/rowdefinition").OfType<XmlNode>()
			select GetRowDefinitionFromXmlNode(x));
		XmlNode xmlNode = node.SelectSingleNode("lighting");
		if (xmlNode != null)
		{
			LevelLayerLighting levelLayerLighting = new LevelLayerLighting();
			if (xmlNode.TryGetAttributeValue("type", out value) && Enum.TryParse<LevelLayerLightingType>(value, ignoreCase: true, out var result2))
			{
				levelLayerLighting.Type = result2;
			}
			if (xmlNode.TryGetAttributeValue("light", out value))
			{
				levelLayerLighting.Light = double.Parse(value, CultureInfo.InvariantCulture);
			}
			levelLayer.Lighting = levelLayerLighting;
		}
		levelLayer.Shadows.AddRange(from x in node.SelectNodes("shadows/shadow").OfType<XmlNode>()
			select GetShadowFromXmlNode(x));
		XmlNode xmlNode2 = node.SelectSingleNode("tiles");
		if (xmlNode2 != null)
		{
			if (xmlNode2.TryGetAttributeValue("offset_y", out value))
			{
				levelLayer.OffsetY = int.Parse(value);
			}
			if (xmlNode2.TryGetAttributeValue("parallax_y", out value))
			{
				if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
				{
					levelLayer.AutomaticYParallax = true;
				}
				else
				{
					levelLayer.ParallaxY = double.Parse(value, CultureInfo.InvariantCulture);
				}
			}
			if (xmlNode2.TryGetAttributeValue("wrap_x", out value))
			{
				levelLayer.WrapX = value.Equals("true", StringComparison.OrdinalIgnoreCase);
			}
			if (xmlNode2.TryGetAttributeValue("wrap_y", out value))
			{
				levelLayer.WrapY = value.Equals("true", StringComparison.OrdinalIgnoreCase);
			}
		}
		int[][] array = (from x in node.SelectNodes("tiles/row").OfType<XmlNode>()
			select ReadTileValues(x.InnerText)).ToArray();
		if (array.Length != 0)
		{
			levelLayer.Resize(array.Max((int[] x) => x.Length), array.Length);
			for (int num = 0; num < array.Length; num++)
			{
				for (int num2 = 0; num2 < Math.Min(levelLayer.Columns, array[num].Length); num2++)
				{
					levelLayer.Tiles[num2, num] = array[num][num2];
				}
			}
		}
		return levelLayer;
	}

	private LayerRowDefinition GetRowDefinitionFromXmlNode(XmlNode node)
	{
		LayerRowDefinition layerRowDefinition = new LayerRowDefinition();
		if (node.TryGetAttributeValue("width", out var value))
		{
			layerRowDefinition.Width = int.Parse(value);
		}
		if (node.TryGetAttributeValue("height", out value))
		{
			layerRowDefinition.Height = int.Parse(value);
		}
		if (node.TryGetAttributeValue("parallax_x", out value))
		{
			layerRowDefinition.Parallax = double.Parse(value, CultureInfo.InvariantCulture);
		}
		if (node.TryGetAttributeValue("velocity_x", out value))
		{
			layerRowDefinition.Velocity = double.Parse(value, CultureInfo.InvariantCulture);
		}
		if (node.TryGetAttributeValue("offset_x", out value))
		{
			layerRowDefinition.InitialOffset = int.Parse(value);
		}
		return layerRowDefinition;
	}

	private LevelLayerShadow GetShadowFromXmlNode(XmlNode node)
	{
		LevelLayerShadow levelLayerShadow = new LevelLayerShadow();
		if (node.TryGetAttributeValue("tiles", out var value))
		{
			levelLayerShadow.Tiles = bool.Parse(value);
		}
		if (node.TryGetAttributeValue("objects", out value))
		{
			levelLayerShadow.Objects = bool.Parse(value);
		}
		if (node.TryGetAttributeValue("layerIndexOffset", out value))
		{
			levelLayerShadow.LayerIndexOffset = int.Parse(value);
		}
		if (node.TryGetAttributeValue("dx", out value))
		{
			Vector2i displacement = levelLayerShadow.Displacement;
			displacement.X = int.Parse(value);
			levelLayerShadow.Displacement = displacement;
		}
		if (node.TryGetAttributeValue("dy", out value))
		{
			Vector2i displacement2 = levelLayerShadow.Displacement;
			displacement2.Y = int.Parse(value);
			levelLayerShadow.Displacement = displacement2;
		}
		if (node.TryGetAttributeValue("softness", out value))
		{
			levelLayerShadow.Softness = int.Parse(value);
		}
		if (node.TryGetAttributeValue("colour", out value))
		{
			levelLayerShadow.Colour = Colour.ParseHex(value);
		}
		return levelLayerShadow;
	}

	private void ReadXYAttribute(string attributeValue, out bool x, out bool y)
	{
		string[] array = attributeValue.Split(',');
		x = array[0] == "true";
		y = array[1] == "true";
	}

	private void ReadXYAttribute(string attributeValue, out Vector2 result)
	{
		string[] array = attributeValue.Split(',');
		result = new Vector2(double.Parse(array[0], CultureInfo.InvariantCulture), double.Parse(array[1], CultureInfo.InvariantCulture));
	}

	private int[] ReadTileValues(string text)
	{
		StringBuilder stringBuilder = new StringBuilder();
		List<int> list = new List<int>();
		StringReader stringReader = new StringReader(text);
		while (stringReader.CanRead())
		{
			int num = 0;
			char c = (char)stringReader.Peek();
			if (char.ToUpper(c) == 'H')
			{
				num |= 0x4000;
				stringReader.Read();
				c = (char)stringReader.Peek();
			}
			if (char.ToUpper(c) == 'V')
			{
				num |= 0x8000;
				stringReader.Read();
				c = (char)stringReader.Peek();
			}
			stringBuilder.Clear();
			while (char.IsNumber(c))
			{
				stringReader.Read();
				stringBuilder.Append(c);
				c = (char)stringReader.Peek();
			}
			num |= int.Parse(stringBuilder.ToString());
			list.Add(num);
			if (c != ',' && c != '\uffff')
			{
				throw new Exception();
			}
			stringReader.Read();
		}
		return list.ToArray();
	}

	private LevelMarker GetMarkerFromXmlNode(LevelMap map, XmlNode node)
	{
		Rectanglei bounds = new Rectanglei
		{
			X = int.Parse(node.Attributes["x"].Value),
			Y = int.Parse(node.Attributes["y"].Value)
		};
		if (node.TryGetAttributeValue("width", out var value))
		{
			bounds.Width = int.Parse(value);
		}
		if (node.TryGetAttributeValue("height", out value))
		{
			bounds.Height = int.Parse(value);
		}
		string name = null;
		if (node.TryGetAttributeValue("name", out value))
		{
			name = value;
		}
		string tag = null;
		if (node.TryGetAttributeValue("tag", out value))
		{
			tag = value;
		}
		LevelLayer layer = null;
		if (node.TryGetAttributeValue("layer", out value))
		{
			layer = map.Layers[int.Parse(value)];
		}
		return new LevelMarker(name, tag, bounds, layer);
	}

	private IEnumerable<CollisionVector> GetCollisionVectorsFromXmlNode(XmlNode parent)
	{
		foreach (XmlNode childNode in parent.ChildNodes)
		{
			if (!(childNode.Name != "vector") && TryGetAttributeXY(childNode, "a", out var xy) && TryGetAttributeXY(childNode, "b", out var xy2) && childNode.TryGetAttributeValue("paths", out var value) && uint.TryParse(value, out var result) && childNode.TryGetAttributeValue("flags", out value) && int.TryParse(value, out var result2))
			{
				yield return new CollisionVector(xy, xy2, result, (CollisionFlags)result2);
			}
		}
	}

	private IEnumerable<int> GetCollisionPathsFromXmlNode(XmlNode parent)
	{
		foreach (XmlNode childNode in parent.ChildNodes)
		{
			if (!(childNode.Name != "path"))
			{
				if (!childNode.TryGetAttributeValue("layer", out var value))
				{
					throw new Exception();
				}
				if (!int.TryParse(value, out var result))
				{
					throw new Exception();
				}
				yield return result;
			}
		}
	}

	private bool TryGetAttributeXY(XmlNode node, string name, out Vector2i xy)
	{
		xy = default;
		if (!node.TryGetAttributeValue(name, out var value))
		{
			return false;
		}
		string[] array = value.Split(',');
		if (array.Length != 2)
		{
			return false;
		}
		if (!int.TryParse(array[0], out var result))
		{
			return false;
		}
		if (!int.TryParse(array[1], out var result2))
		{
			return false;
		}
		xy = new Vector2i(result, result2);
		return true;
	}
}
