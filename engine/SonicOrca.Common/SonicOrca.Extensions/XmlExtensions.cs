using System.Xml;

namespace SonicOrca.Extensions;

public static class XmlExtensions
{
	public static string GetNodeInnerText(this XmlNode node, string xpath, string defaultValue)
	{
		if (!node.TryGetNodeInnerText(xpath, out var value))
		{
			return defaultValue;
		}
		return value;
	}

	public static bool TryGetNodeInnerText(this XmlNode node, string xpath, out string value)
	{
		XmlNode xmlNode = node.SelectSingleNode(xpath);
		if (xmlNode == null)
		{
			value = null;
			return false;
		}
		value = xmlNode.InnerText;
		return true;
	}

	public static bool TryGetAttributeValue(this XmlNode node, string name, out string value)
	{
		XmlAttribute xmlAttribute = node.Attributes[name];
		if (xmlAttribute == null)
		{
			value = null;
			return false;
		}
		value = xmlAttribute.Value;
		return true;
	}
}
