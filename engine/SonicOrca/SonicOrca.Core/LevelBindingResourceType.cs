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

/// <summary>
/// Loads a level binding file. A level binding file holds the object placements of a level.
/// </summary>
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
		XmlNode definitionsNode = xmlDocument.SelectSingleNode("Binding/Definitions");
		if (!definitionsNode.TryGetAttributeValue("DefaultLayer", out string defaultLayerText) || !int.TryParse(defaultLayerText, out int defaultLayerIndex))
		{
			defaultLayerIndex = 0;
		}
		levelBinding.ObjectPlacements.AddRange(from x in definitionsNode.SelectNodes("Definition").OfType<XmlNode>()
											   select GetObjectPlacementFromXmlNode(x, defaultLayerIndex));
		e.PushDependencies(levelBinding.ObjectPlacements.Select(x => x.Key).Distinct());
		return levelBinding;
	}

	private ObjectPlacement GetObjectPlacementFromXmlNode(XmlNode node, int defaultLayerIndex)
	{
		XmlNode commonNode = node.SelectSingleNode("Common");
		XmlNode behaviourNode = node.SelectSingleNode("Behaviour");
		XmlNode mappingsNode = node.SelectSingleNode("Mappings");
		XmlNode keyNode = commonNode.SelectSingleNode("Key");
		XmlNode uidNode = commonNode.SelectSingleNode("Uid");
		XmlNode nameNode = commonNode.SelectSingleNode("Name");
		XmlNode layerNode = commonNode.SelectSingleNode("Layer");
		XmlNode positionNode = commonNode.SelectSingleNode("Position");
		if (layerNode == null || !commonNode.TryGetNodeInnerText("Layer", out string layerText) || !int.TryParse(layerText, out int layerIndex))
		{
			layerIndex = defaultLayerIndex;
		}
		Guid.TryParse(uidNode.InnerText, out Guid uid);
		// This must stay dynamic: the ObjectPlacement constructor is then bound at run time.
		dynamic behaviour = new { };
		if (behaviourNode != null && behaviourNode.HasChildNodes)
		{
			IEnumerable<KeyValuePair<string, object>> behaviourValues = ParseBehaviour(behaviourNode);
			ExpandoObject expandoObject = new ExpandoObject();
			ICollection<KeyValuePair<string, object>> expandoValues = expandoObject;
			foreach (KeyValuePair<string, object> behaviourValue in behaviourValues)
			{
				expandoValues.Add(behaviourValue);
			}
			behaviour = expandoObject;
		}
		ObjectPlacement objectPlacement = new ObjectPlacement(keyNode.InnerText, uid, nameNode.InnerText, layerIndex, new Vector2i(int.Parse(positionNode.Attributes["X"].Value), int.Parse(positionNode.Attributes["Y"].Value)), behaviour);
		if (mappingsNode != null && mappingsNode.HasChildNodes)
		{
			objectPlacement.Mappings = new List<KeyValuePair<string, object>>(ParseBehaviour(mappingsNode));
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
		// A child element without attributes or child elements is a simple value. Other child elements are nested behaviours.
		foreach (XmlNode childNode in entryNode)
		{
			if (childNode.NodeType == XmlNodeType.Element)
			{
				string childName = childNode.Name;
				KeyValuePair<string, object>[] childValues = ParseBehaviour(childNode).ToArray();
				object childValue = (childValues.Length == 0) ? ParseBehaviourValue(childNode.InnerText) : childValues;
				yield return new KeyValuePair<string, object>(childName, childValue);
			}
		}
	}

	public static object ParseBehaviourValue(string value)
	{
		if (int.TryParse(value, out int intValue))
		{
			return intValue;
		}
		if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
		{
			return doubleValue;
		}
		if (bool.TryParse(value, out bool boolValue))
		{
			return boolValue;
		}
		if (Guid.TryParse(value, out Guid guidValue))
		{
			return guidValue;
		}
		return value;
	}

	public static object ParseBehaviourValue(string value, Type type)
	{
		if (type == typeof(int))
		{
			if (int.TryParse(value, out int intValue))
			{
				return intValue;
			}
			return 0;
		}
		if (type == typeof(uint))
		{
			if (uint.TryParse(value, out uint uintValue))
			{
				return uintValue;
			}
			return 0u;
		}
		if (type == typeof(double))
		{
			if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
			{
				return doubleValue;
			}
			return 0.0;
		}
		if (type == typeof(bool))
		{
			if (bool.TryParse(value, out bool boolValue))
			{
				return boolValue;
			}
			return false;
		}
		if (type == typeof(Vector2))
		{
			string xText = GetVectorComponentText(value, 0);
			string yText = GetVectorComponentText(value, 1);
			double.TryParse(xText, NumberStyles.Float, CultureInfo.InvariantCulture, out double x);
			double.TryParse(yText, NumberStyles.Float, CultureInfo.InvariantCulture, out double y);
			return new Vector2(x, y);
		}
		if (type == typeof(Vector2i))
		{
			string xText = GetVectorComponentText(value, 0);
			string yText = GetVectorComponentText(value, 1);
			int.TryParse(xText, out int x);
			int.TryParse(yText, out int y);
			return new Vector2i(x, y);
		}
		if (type == typeof(Guid))
		{
			if (Guid.TryParse(value, out Guid guidValue))
			{
				return guidValue;
			}
			return default(Guid);
		}
		if (type.IsEnum)
		{
			return Enum.Parse(type, value, ignoreCase: true);
		}
		return value;
	}

	/// <summary>
	/// Gets the text of one component of a vector in the form "{X=1, Y=2}".
	/// </summary>
	private static string GetVectorComponentText(string value, int componentIndex)
	{
		return value.Trim('{', '}', ' ').Replace(" ", "").Split(',')[componentIndex].Split('=')[1];
	}
}
