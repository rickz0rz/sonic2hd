using System.Xml;

namespace SonicOrca.Extensions;

/// <summary>
/// Reads node text and attribute values from XML nodes.
/// </summary>
public static class XmlExtensions
{
	public static string GetNodeInnerText(this XmlNode node, string xpath, string defaultValue)
	{
		if (!node.TryGetNodeInnerText(xpath, out string value))
		{
			return defaultValue;
		}
		return value;
	}

	public static bool TryGetNodeInnerText(this XmlNode node, string xpath, out string value)
	{
		XmlNode selectedNode = node.SelectSingleNode(xpath);
		if (selectedNode == null)
		{
			value = null;
			return false;
		}
		value = selectedNode.InnerText;
		return true;
	}

	public static bool TryGetAttributeValue(this XmlNode node, string name, out string value)
	{
		XmlAttribute attribute = node.Attributes[name];
		if (attribute == null)
		{
			value = null;
			return false;
		}
		value = attribute.Value;
		return true;
	}
}
