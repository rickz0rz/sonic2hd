using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace SonicOrca.Core;

/// <summary>
/// Writes a level binding file from the object placements of a level binding.
/// </summary>
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
		// The default layer is the layer with the most objects. A placement on that layer does not write its layer.
		int defaultLayer = (from x in _binding.ObjectPlacements
							group x by x.Layer into layerGroup
							orderby layerGroup.Count() descending
							select layerGroup).First().Key;
		xmlWriter.WriteStartElement("Definitions");
		xmlWriter.WriteAttributeString("DefaultLayer", defaultLayer.ToString());
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
			if (objectPlacement.Layer != defaultLayer)
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
		foreach (KeyValuePair<string, object> behaviourValue in behaviour)
		{
			writer.WriteStartElement(behaviourValue.Key);
			if (behaviourValue.Value is IEnumerable<KeyValuePair<string, object>> childValues)
			{
				WriteObjectBehaviour(writer, childValues);
			}
			else
			{
				writer.WriteString(Convert.ToString(behaviourValue.Value, CultureInfo.InvariantCulture));
			}
			writer.WriteEndElement();
		}
	}

	public void ConvertObjectEntriesToPlacements()
	{
		_binding.ObjectPlacements.Clear();
		foreach (ObjectEntry entry in _binding.Level.ObjectManager.ObjectEntryTable)
		{
			ObjectPlacement objectPlacement = new ObjectPlacement(entry.Type.ResourceKey, entry.Uid, entry.Name, entry.Layer, entry.Position, entry.State);
			foreach (ObjectMapping mapping in entry.Mappings)
			{
				List<KeyValuePair<string, object>> mappings = objectPlacement.Mappings as List<KeyValuePair<string, object>>;
				KeyValuePair<string, object> mappingValue = new KeyValuePair<string, object>(mapping.Field, mapping.Target);
				mappings.Add(mappingValue);
			}
			_binding.ObjectPlacements.Add(objectPlacement);
		}
	}
}
