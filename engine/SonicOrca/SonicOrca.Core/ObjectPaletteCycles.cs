using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SonicOrca.Graphics;

namespace SonicOrca.Core;

/// <summary>
/// The palette cycles of the original on the sprites of the objects of a zone. The import draws each colour of a cycle
/// in the sprites as a key colour, which is a little different from all the colours of the palette. When the level
/// draws one of the objects, the object shader draws each key colour as the colour of the current step of its cycle.
/// The import writes the resource PALETTECYCLES next to the map.
/// </summary>
public sealed class ObjectPaletteCycles
{
	public const string ResourceName = "PALETTECYCLES";

	/// <summary>The most key colours that the object shader takes (InputColourSwapFrom in object.shader).</summary>
	public const int MaxEntries = 16;

	/// <summary>A key colour of the sprites, and its colour at each step of its cycle.</summary>
	/// <param name="Period">The ticks of one step.</param>
	public sealed record Entry(Colour Key, IReadOnlyList<Colour> Colours, int Period);

	private readonly HashSet<string> _objectKeys;

	public IReadOnlyList<Entry> Entries { get; }

	public IReadOnlyCollection<string> ObjectKeys => _objectKeys;

	public ObjectPaletteCycles(IEnumerable<Entry> entries, IEnumerable<string> objectKeys)
	{
		Entries = entries.Take(MaxEntries).ToArray();
		_objectKeys = new HashSet<string>(objectKeys, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>True when the sprites of the object type have key colours.</summary>
	public bool Uses(string objectKey) => objectKey != null && _objectKeys.Contains(objectKey);

	/// <summary>The colour swaps at a level tick: each key colour and the colour of the step of its cycle.</summary>
	public ColourSwap[] GetSwaps(int tick)
	{
		return Entries.Select(entry => new ColourSwap(entry.Key, entry.Colours[Math.Max(0, tick) / entry.Period % entry.Colours.Count])).ToArray();
	}

	public static ObjectPaletteCycles FromXml(Stream stream)
	{
		XmlDocument document = new XmlDocument();
		document.Load(stream);
		XmlElement root = document.DocumentElement;
		IEnumerable<string> objects = root.SelectNodes("object").OfType<XmlElement>().Select(e => e.GetAttribute("key"));
		IEnumerable<Entry> entries = root.SelectNodes("entry").OfType<XmlElement>().Select(e => new Entry(
			Colour.ParseHex(e.GetAttribute("key")),
			e.GetAttribute("colours").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Colour.ParseHex).ToArray(),
			Math.Max(1, int.Parse(e.GetAttribute("period"), CultureInfo.InvariantCulture))));
		return new ObjectPaletteCycles(entries, objects);
	}

	public string ToXml()
	{
		var xml = new StringBuilder();
		xml.AppendLine("<?xml version=\"1.0\"?>");
		xml.AppendLine("<palettecycles>");
		foreach (string key in _objectKeys.OrderBy(k => k, StringComparer.Ordinal))
		{
			xml.AppendLine($"  <object key=\"{key}\" />");
		}
		foreach (Entry entry in Entries)
		{
			xml.AppendLine($"  <entry key=\"{entry.Key.ToHexString()}\" period=\"{entry.Period}\" colours=\"{string.Join(" ", entry.Colours.Select(c => c.ToHexString()))}\" />");
		}
		xml.AppendLine("</palettecycles>");
		return xml.ToString();
	}
}
