using System.Linq;
using System.Xml;

namespace SonicOrca.Core;

public class LevelDependenciesWriter
{
	private readonly LevelBinding _binding;

	public LevelDependenciesWriter(LevelBinding binding)
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
		xmlWriter.WriteStartElement("Dependencies");
		foreach (string item in _binding.ObjectPlacements.Select((ObjectPlacement p) => p.Key).Distinct())
		{
			xmlWriter.WriteStartElement("Dependency");
			xmlWriter.WriteStartElement("Key");
			xmlWriter.WriteString(item.ToString());
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
	}
}
