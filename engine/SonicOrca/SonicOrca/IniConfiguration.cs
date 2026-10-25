using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Extensions;

namespace SonicOrca;

/// <summary>
/// Reads and writes an INI configuration file. It keeps the original lines, so that comments and layout stay
/// the same when the file is saved.
/// </summary>
public class IniConfiguration
{
	/// <summary>
	/// A section of the file and the indexes of its header lines.
	/// </summary>
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

	/// <summary>
	/// A property value and the indexes of the lines that set it.
	/// </summary>
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
			string line;
			while ((line = streamReader.ReadLine()) != null)
			{
				_lines.Add(line);
			}
		}
		Parse();
	}

	public void Save()
	{
		Save(_path);
	}

	/// <summary>Writes the file. A configuration made without a file then saves to this path with <see cref="Save()"/>.</summary>
	public void Save(string path)
	{
		_path ??= path;
		using StreamWriter streamWriter = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write));
		foreach (string line in _lines)
		{
			streamWriter.WriteLine(line);
		}
	}

	/// <summary>Adds 1 to each kept line index from an index, after a line is inserted there.</summary>
	private void MoveLineIndexes(int fromIndex)
	{
		foreach (Section section in _sections.Values)
		{
			MoveLineIndexes(section.Lines, fromIndex);
			foreach (Property property in section.Properties.Values)
			{
				MoveLineIndexes(property.Lines, fromIndex);
			}
		}
	}

	private static void MoveLineIndexes(IList<int> lines, int fromIndex)
	{
		for (int i = 0; i < lines.Count; i++)
		{
			if (lines[i] >= fromIndex)
			{
				lines[i]++;
			}
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
		if (!bool.TryParse(property, out bool result))
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
		if (!double.TryParse(properties[propertyName].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
		{
			return defaultValue;
		}
		return result;
	}

	public void SetProperty(string sectionName, string propertyName, string value)
	{
		Section section = GetOrAddSection(sectionName);
		Property property;
		if (section.Properties.ContainsKey(propertyName))
		{
			// Replace the last line that sets the property.
			property = section.Properties[propertyName];
			property.Value = value;
			int index = property.Lines.Last();
			_lines[index] = GetPropertySetValueLine(propertyName, value);
			return;
		}
		property = (section.Properties[propertyName] = new Property(propertyName));
		property.Value = value;
		if (section.Lines.Count > 0)
		{
			// Insert the new line directly after the section header. The lines after it move down by one.
			int lineIndex = section.Lines.Last() + 1;
			_lines.Insert(lineIndex, GetPropertySetValueLine(propertyName, value));
			MoveLineIndexes(lineIndex);
			property.Lines.Add(lineIndex);
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
				// Ignore a line that is not valid and continue with the next line.
			}
		}
	}

	private void Parse(int lineIndex, string line, ref string currentSectionName)
	{
		StringReader reader = new StringReader(line);
		reader.SkipWhitespace();
		if (!reader.TryRead(out char c))
		{
			return;
		}
		switch (c)
		{
			case '[':
				{
					StringBuilder sectionNameBuilder = new StringBuilder();
					while (reader.TryRead(out c) && c != ']')
					{
						sectionNameBuilder.Append(c);
					}
					if (c != ']')
					{
						throw new Exception("Invalid section name");
					}
					currentSectionName = sectionNameBuilder.ToString().Trim();
					if (!_sections.TryGetValue(currentSectionName, out Section section))
					{
						section = new Section(currentSectionName);
						_sections.Add(currentSectionName, section);
					}
					section.Lines.Add(lineIndex);
					break;
				}
			case '#':
			case ';':
				// A comment line.
				break;
			default:
				{
					StringBuilder propertyNameBuilder = new StringBuilder();
					propertyNameBuilder.Append(c);
					while (reader.TryRead(out c) && c != '=' && c != ':')
					{
						propertyNameBuilder.Append(c);
					}
					if (c != '=' && c != ':')
					{
						throw new Exception("Expected = or : after property name");
					}
					string propertyName = propertyNameBuilder.ToString().Trim();
					string value = ReadPropertyValue(reader);
					Property property = GetOrAddProperty(currentSectionName, propertyName);
					property.Value = value;
					property.Lines.Add(lineIndex);
					break;
				}
		}
	}

	private string ReadPropertyValue(TextReader reader)
	{
		reader.SkipWhitespace();
		StringBuilder stringBuilder = new StringBuilder();
		bool inSingleQuotes = false;
		bool inDoubleQuotes = false;
		bool isQuoted = false;
		if (!reader.TryRead(out char c))
		{
			return null;
		}
		switch (c)
		{
			case '\'':
				inSingleQuotes = (isQuoted = true);
				break;
			case '"':
				inDoubleQuotes = (isQuoted = true);
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
			// In a value that is not in quotes, ';' or '#' starts a comment.
			if ((c == ';' || c == '#') && !isQuoted)
			{
				break;
			}
			if (c == '\'' && inSingleQuotes)
			{
				inSingleQuotes = false;
				break;
			}
			if (c == '"' && inDoubleQuotes)
			{
				inDoubleQuotes = false;
				break;
			}
			stringBuilder.Append(c);
		}
		if (inSingleQuotes || inDoubleQuotes)
		{
			throw new Exception("No end quote(s)");
		}
		if (!isQuoted)
		{
			return stringBuilder.ToString().TrimEnd();
		}
		return stringBuilder.ToString();
	}

	private char ReadEscapedCharacter(TextReader reader)
	{
		if (!reader.TryRead(out char c))
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
					// "\x" is followed by exactly 4 hexadecimal digits.
					char[] hexDigits = new char[4];
					if (reader.ReadBlock(hexDigits, 0, 4) != 4)
					{
						throw new Exception("Wrong format for escaped unicode character.");
					}
					return (char)int.Parse(new string(hexDigits), NumberStyles.AllowHexSpecifier);
				}
			default:
				return c;
		}
	}
}
