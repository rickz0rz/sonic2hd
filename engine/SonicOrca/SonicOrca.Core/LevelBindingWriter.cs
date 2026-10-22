using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace SonicOrca.Core;

public class LevelBindingWriter
{
	private readonly LevelBinding _binding;

	public LevelBindingWriter(LevelBinding binding)
	{
		_binding = binding;
	}

	public void Save(string path)
	{
		XmlWriterSettings settings = new XmlWriterSettings
		{
			Indent = true
		};
		using XmlWriter xmlWriter = XmlWriter.Create(path, settings);
		xmlWriter.WriteStartDocument();
		xmlWriter.WriteStartElement("Binding");
		int key = (from x in _binding.ObjectPlacements
			group x by x.Layer into x
			orderby x.Count() descending
			select x).First().Key;
		xmlWriter.WriteStartElement("Definitions");
		xmlWriter.WriteAttributeString("DefaultLayer", key.ToString());
		foreach (ObjectPlacement objectPlacement in _binding.ObjectPlacements)
		{
			xmlWriter.WriteStartElement("Definition");
			xmlWriter.WriteStartElement("Common");
			xmlWriter.WriteStartElement("Key");
			xmlWriter.WriteString(objectPlacement.Key.ToString());
			xmlWriter.WriteEndElement();
			xmlWriter.WriteStartElement("Uid");
			xmlWriter.WriteString(objectPlacement.Uid.ToString());
			xmlWriter.WriteEndElement();
			xmlWriter.WriteStartElement("Name");
			xmlWriter.WriteString(objectPlacement.Name.ToString());
			xmlWriter.WriteEndElement();
			if (objectPlacement.Layer != key)
			{
				xmlWriter.WriteStartElement("Layer");
				xmlWriter.WriteString(objectPlacement.Layer.ToString());
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteStartElement("Position");
			xmlWriter.WriteAttributeString("X", objectPlacement.Position.X.ToString());
			xmlWriter.WriteAttributeString("Y", objectPlacement.Position.Y.ToString());
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();
			if (objectPlacement.Behaviour.Count > 0)
			{
				xmlWriter.WriteStartElement("Behaviour");
				WriteObjectBehaviour(xmlWriter, objectPlacement.Behaviour);
				xmlWriter.WriteEndElement();
			}
			if (objectPlacement.Mappings.Count > 0)
			{
				xmlWriter.WriteStartElement("Mappings");
				WriteObjectBehaviour(xmlWriter, objectPlacement.Mappings);
				xmlWriter.WriteEndElement();
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
	}

	private void WriteObjectBehaviour(XmlWriter writer, IEnumerable<KeyValuePair<string, object>> behaviour)
	{
		foreach (KeyValuePair<string, object> item in behaviour)
		{
			writer.WriteStartElement(item.Key);
			if (item.Value is IEnumerable<KeyValuePair<string, object>>)
			{
				WriteObjectBehaviour(writer, (IEnumerable<KeyValuePair<string, object>>)item.Value);
			}
			else
			{
				writer.WriteString(Convert.ToString(item.Value, CultureInfo.InvariantCulture));
			}
			writer.WriteEndElement();
		}
	}

	public void ConvertObjectEntriesToPlacements()
	{
		_binding.ObjectPlacements.Clear();
		foreach (ObjectEntry item2 in _binding.Level.ObjectManager.ObjectEntryTable)
		{
			ObjectPlacement objectPlacement = new ObjectPlacement(item2.Type.ResourceKey, item2.Uid, item2.Name, item2.Layer, item2.Position, item2.State);
			foreach (ObjectMapping mapping in item2.Mappings)
			{
				List<KeyValuePair<string, object>> obj = objectPlacement.Mappings as List<KeyValuePair<string, object>>;
				KeyValuePair<string, object> item = new KeyValuePair<string, object>(mapping.Field, mapping.Target);
				obj.Add(item);
			}
			_binding.ObjectPlacements.Add(objectPlacement);
		}
	}
}
