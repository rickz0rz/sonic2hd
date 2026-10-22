using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SonicOrca.Extensions;
using SonicOrca.Geometry;
using SonicOrca.Resources;

namespace SonicOrca.Core;

public class LevelBindingResourceType : ResourceType
{
	public override string Name => "binding, xml";

	public override string DefaultExtension => ".binding.xml";

	public override bool CompressByDefault => true;

	public LevelBindingResourceType()
		: base(ResourceTypeIdentifier.Binding)
	{
	}

	public override async Task<ILoadedResource> LoadAsync(ResourceLoadArgs e, CancellationToken ct = default(CancellationToken))
	{
		XmlDocument xmlDocument = new XmlDocument();
		await Task.Run(() =>
		{
			xmlDocument.Load(e.InputStream);
		});
		LevelBinding levelBinding = new LevelBinding();
		levelBinding.Resource = e.Resource;
		XmlNode xmlNode = xmlDocument.SelectSingleNode("Binding/Definitions");
		if (!xmlNode.TryGetAttributeValue("DefaultLayer", out var value) || !int.TryParse(value, out var defaultLayerIndex))
		{
			defaultLayerIndex = 0;
		}
		levelBinding.ObjectPlacements.AddRange(from x in xmlNode.SelectNodes("Definition").OfType<XmlNode>()
			select GetObjectPlacementFromXmlNode(x, defaultLayerIndex));
		e.PushDependencies(levelBinding.ObjectPlacements.Select((ObjectPlacement x) => x.Key).Distinct());
		return levelBinding;
	}

	private ObjectPlacement GetObjectPlacementFromXmlNode(XmlNode node, int defaultLayerIndex)
	{
		XmlNode xmlNode = node.SelectSingleNode("Common");
		XmlNode xmlNode2 = node.SelectSingleNode("Behaviour");
		XmlNode xmlNode3 = node.SelectSingleNode("Mappings");
		XmlNode xmlNode4 = xmlNode.SelectSingleNode("Key");
		XmlNode xmlNode5 = xmlNode.SelectSingleNode("Uid");
		XmlNode xmlNode6 = xmlNode.SelectSingleNode("Name");
		XmlNode xmlNode7 = xmlNode.SelectSingleNode("Layer");
		XmlNode xmlNode8 = xmlNode.SelectSingleNode("Position");
		if (xmlNode7 == null || !xmlNode.TryGetNodeInnerText("Layer", out var value) || !int.TryParse(value, out var result))
		{
			result = defaultLayerIndex;
		}
		Guid result2 = default;
		Guid.TryParse(xmlNode5.InnerText, out result2);
		dynamic val = new { };
		if (xmlNode2 != null && xmlNode2.HasChildNodes)
		{
			IEnumerable<KeyValuePair<string, object>> enumerable = ParseBehaviour(xmlNode2);
			ExpandoObject expandoObject = new ExpandoObject();
			ICollection<KeyValuePair<string, object>> collection = expandoObject;
			foreach (KeyValuePair<string, object> item in enumerable)
			{
				collection.Add(item);
			}
			val = expandoObject;
		}
		ObjectPlacement objectPlacement = new ObjectPlacement(xmlNode4.InnerText, result2, xmlNode6.InnerText, result, new Vector2i(int.Parse(xmlNode8.Attributes["X"].Value), int.Parse(xmlNode8.Attributes["Y"].Value)), val);
		if (xmlNode3 != null && xmlNode3.HasChildNodes)
		{
			objectPlacement.Mappings = new List<KeyValuePair<string, object>>(ParseBehaviour(xmlNode3));
		}
		return objectPlacement;
	}

	private IEnumerable<KeyValuePair<string, object>> ParseBehaviour(XmlNode entryNode)
	{
		foreach (XmlAttribute attribute in entryNode.Attributes)
		{
			string name = attribute.Name;
			object value = ParseBehaviourValue(attribute.Value);
			yield return new KeyValuePair<string, object>(name, value);
		}
		foreach (XmlNode item in entryNode)
		{
			if (item.NodeType == XmlNodeType.Element)
			{
				string name2 = item.Name;
				(from x in item.ChildNodes.OfType<XmlNode>()
					where x.NodeType == XmlNodeType.Element
					select x).ToArray();
				KeyValuePair<string, object>[] array = ParseBehaviour(item).ToArray();
				object value2 = ((array.Length == 0) ? ParseBehaviourValue(item.InnerText) : array);
				yield return new KeyValuePair<string, object>(name2, value2);
			}
		}
	}

	public static object ParseBehaviourValue(string value)
	{
		if (int.TryParse(value, out var result))
		{
			return result;
		}
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return result2;
		}
		if (bool.TryParse(value, out var result3))
		{
			return result3;
		}
		if (Guid.TryParse(value, out var result4))
		{
			return result4;
		}
		return value;
	}

	public static object ParseBehaviourValue(string value, Type type)
	{
		if (type == typeof(int))
		{
			if (int.TryParse(value, out var result))
			{
				return result;
			}
			return 0;
		}
		if (type == typeof(uint))
		{
			if (uint.TryParse(value, out var result2))
			{
				return result2;
			}
			return 0u;
		}
		if (type == typeof(double))
		{
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
			{
				return result3;
			}
			return 0.0;
		}
		if (type == typeof(bool))
		{
			if (bool.TryParse(value, out var result4))
			{
				return result4;
			}
			return false;
		}
		if (type == typeof(Vector2))
		{
			string s = value.Trim('{', '}', ' ').Replace(" ", "").Split(',')[0].Split('=')[1];
			string s2 = value.Trim('{', '}', ' ').Replace(" ", "").Split(',')[1].Split('=')[1];
			double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result5);
			double.TryParse(s2, NumberStyles.Float, CultureInfo.InvariantCulture, out var result6);
			return new Vector2(result5, result6);
		}
		if (type == typeof(Vector2i))
		{
			string s3 = value.Trim('{', '}', ' ').Replace(" ", "").Split(',')[0].Split('=')[1];
			string s4 = value.Trim('{', '}', ' ').Replace(" ", "").Split(',')[1].Split('=')[1];
			int.TryParse(s3, out var result7);
			int.TryParse(s4, out var result8);
			return new Vector2i(result7, result8);
		}
		if (type == typeof(Guid))
		{
			if (Guid.TryParse(value, out var result9))
			{
				return result9;
			}
			return default(Guid);
		}
		if (type.IsEnum)
		{
			return Enum.Parse(type, value, ignoreCase: true);
		}
		return value;
	}
}
