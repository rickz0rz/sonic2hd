using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Extensions;

namespace SonicOrca;

public class IniConfiguration
{
	private class Section
	{
		private readonly string _name;

		private readonly List<int> _lines = new List<int>();

		private readonly Dictionary<string, Property> _properties = new Dictionary<string, Property>();

		public IList<int> Lines => _lines;

		public IDictionary<string, Property> Properties => _properties;

		public Section()
			: this(string.Empty)
		{
		}

		public Section(string name)
		{
			_name = name;
		}
	}

	private class Property
	{
		private readonly string _name;

		private readonly List<int> _lines = new List<int>();

		public string Value { get; set; }

		public IList<int> Lines => _lines;

		public Property(string name)
		{
			_name = name;
		}
	}

	private readonly List<string> _lines = new List<string>();

	private readonly Dictionary<string, Section> _sections = new Dictionary<string, Section>();

	private string _path;

	public string this[string sectionName, string propertyName]
	{
		get
		{
			return GetProperty(sectionName, propertyName);
		}
		set
		{
			SetProperty(sectionName, propertyName, value);
		}
	}

	public IniConfiguration()
	{
		_sections[string.Empty] = new Section();
	}

	public IniConfiguration(string path)
		: this()
	{
		_path = path;
		using (StreamReader streamReader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
		{
			string item;
			while ((item = streamReader.ReadLine()) != null)
			{
				_lines.Add(item);
			}
		}
		Parse();
	}

	public void Save()
	{
		Save(_path);
	}

	public void Save(string path)
	{
		using StreamWriter streamWriter = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write));
		foreach (string line in _lines)
		{
			streamWriter.WriteLine(line);
		}
	}

	public bool PropertyExists(string sectionName, string propertyName)
	{
		if (!_sections.ContainsKey(sectionName))
		{
			return false;
		}
		if (!_sections[sectionName].Properties.ContainsKey(propertyName))
		{
			return false;
		}
		return true;
	}

	public string GetProperty(string sectionName, string propertyName, string defaultValue = null)
	{
		if (!_sections.ContainsKey(sectionName))
		{
			return defaultValue;
		}
		IDictionary<string, Property> properties = _sections[sectionName].Properties;
		if (!properties.ContainsKey(propertyName))
		{
			return defaultValue;
		}
		return properties[propertyName].Value;
	}

	public bool GetPropertyBoolean(string sectionName, string propertyName, bool defaultValue = false)
	{
		string property = GetProperty(sectionName, propertyName);
		if (property == null)
		{
			return defaultValue;
		}
		if (!bool.TryParse(property, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public double GetPropertyDouble(string sectionName, string propertyName, double defaultValue = 0.0)
	{
		if (!_sections.ContainsKey(sectionName))
		{
			return defaultValue;
		}
		IDictionary<string, Property> properties = _sections[sectionName].Properties;
		if (!properties.ContainsKey(propertyName))
		{
			return defaultValue;
		}
		if (!double.TryParse(properties[propertyName].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return defaultValue;
		}
		return result;
	}

	public void SetProperty(string sectionName, string propertyName, string value)
	{
		Section orAddSection = GetOrAddSection(sectionName);
		Property property;
		if (orAddSection.Properties.ContainsKey(propertyName))
		{
			property = orAddSection.Properties[propertyName];
			property.Value = value;
			int index = property.Lines.Last();
			_lines[index] = GetPropertySetValueLine(propertyName, value);
			return;
		}
		property = (orAddSection.Properties[propertyName] = new Property(propertyName));
		property.Value = value;
		if (orAddSection.Lines.Count > 0)
		{
			int num = orAddSection.Lines.Last() + 1;
			_lines.Insert(num, GetPropertySetValueLine(propertyName, value));
			property.Lines.Add(num);
			return;
		}
		if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines.Last()))
		{
			_lines.Add(string.Empty);
		}
		_lines.Add(GetPropertySetValueLine(propertyName, value));
		property.Lines.Add(_lines.Count - 1);
	}

	private string GetPropertySetValueLine(string propertyName, string value)
	{
		return propertyName + " = " + value;
	}

	private void InsertLine(int index, string line)
	{
		foreach (Section value in _sections.Values)
		{
			foreach (Property value2 in value.Properties.Values)
			{
				for (int i = 0; i < value2.Lines.Count; i++)
				{
					if (value2.Lines[i] <= index)
					{
						value2.Lines[i]++;
					}
				}
			}
		}
		_lines.Insert(index, line);
	}

	private Section GetOrAddSection(string sectionName)
	{
		if (_sections.ContainsKey(sectionName))
		{
			return _sections[sectionName];
		}
		Section section = new Section(sectionName);
		if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines.Last()))
		{
			_lines.Add(string.Empty);
		}
		_lines.Add("[" + sectionName + "]");
		section.Lines.Add(_lines.Count - 1);
		return _sections[sectionName] = section;
	}

	private Property GetOrAddProperty(string sectionName, string propertyName)
	{
		IDictionary<string, Property> properties = _sections[sectionName].Properties;
		if (!properties.ContainsKey(propertyName))
		{
			return properties[propertyName] = new Property(propertyName);
		}
		return properties[propertyName];
	}

	private void Parse()
	{
		string currentSectionName = string.Empty;
		for (int i = 0; i < _lines.Count; i++)
		{
			try
			{
				Parse(i, _lines[i], ref currentSectionName);
			}
			catch
			{
			}
		}
	}

	private void Parse(int lineIndex, string line, ref string currentSectionName)
	{
		StringReader reader = new StringReader(line);
		reader.SkipWhitespace();
		if (!reader.TryRead(out var c))
		{
			return;
		}
		switch (c)
		{
			case '[':
				{
					StringBuilder stringBuilder2 = new StringBuilder();
					while (reader.TryRead(out c) && c != ']')
					{
						stringBuilder2.Append(c);
					}
					if (c != ']')
					{
						throw new Exception("Invalid section name");
					}
					currentSectionName = stringBuilder2.ToString().Trim();
					if (!_sections.TryGetValue(currentSectionName, out var value2))
					{
						value2 = new Section(currentSectionName);
						_sections.Add(currentSectionName, value2);
					}
					value2.Lines.Add(lineIndex);
					break;
				}
			default:
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append(c);
					while (reader.TryRead(out c) && c != '=' && c != ':')
					{
						stringBuilder.Append(c);
					}
					if (c != '=' && c != ':')
					{
						throw new Exception("Expected = or : after property name");
					}
					string propertyName = stringBuilder.ToString().Trim();
					string value = ReadPropertyValue(reader);
					Property orAddProperty = GetOrAddProperty(currentSectionName, propertyName);
					orAddProperty.Value = value;
					orAddProperty.Lines.Add(lineIndex);
					break;
				}
			case '#':
			case ';':
				break;
		}
	}

	private string ReadPropertyValue(TextReader reader)
	{
		reader.SkipWhitespace();
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (!reader.TryRead(out var c))
		{
			return null;
		}
		switch (c)
		{
			case '\'':
				flag = (flag3 = true);
				break;
			case '"':
				flag2 = (flag3 = true);
				break;
			default:
				stringBuilder.Append(c);
				break;
		}
		while (reader.TryRead(out c))
		{
			if (c == '\\')
			{
				stringBuilder.Append(ReadEscapedCharacter(reader));
				continue;
			}
			if ((c == ';' || c == '#') && !flag3)
			{
				break;
			}
			if ((c == '\'') & flag)
			{
				flag = false;
				break;
			}
			if ((c == '"') & flag2)
			{
				flag2 = false;
				break;
			}
			stringBuilder.Append(c);
		}
		if (flag | flag2)
		{
			throw new Exception("No end quote(s)");
		}
		if (!flag3)
		{
			return stringBuilder.ToString().TrimEnd();
		}
		return stringBuilder.ToString();
	}

	private char ReadEscapedCharacter(TextReader reader)
	{
		if (!reader.TryRead(out var c))
		{
			throw new Exception("Expected escape character");
		}
		switch (c)
		{
			case '0':
				return '\0';
			case 't':
				return '\t';
			case 'r':
				return '\r';
			case 'n':
				return '\n';
			case 'x':
				{
					char[] array = new char[4];
					if (reader.ReadBlock(array, 0, 4) != 4)
					{
						throw new Exception("Wrong format for escaped unicode character.");
					}
					return (char)int.Parse(new string(array), NumberStyles.AllowHexSpecifier);
				}
			default:
				return c;
		}
	}
}
