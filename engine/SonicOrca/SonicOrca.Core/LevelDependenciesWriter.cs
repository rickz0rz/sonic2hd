using System.Linq;
using System.Xml;

namespace SonicOrca.Core;

/// <summary>
/// Writes a level dependencies file from the object placements of a level binding.
/// </summary>
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
		foreach (string key in _binding.ObjectPlacements.Select(p => p.Key).Distinct())
		{
			xmlWriter.WriteStartElement("Dependency");
			xmlWriter.WriteStartElement("Key");
			xmlWriter.WriteString(key.ToString());
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		xmlWriter.WriteEndDocument();
	}
}
