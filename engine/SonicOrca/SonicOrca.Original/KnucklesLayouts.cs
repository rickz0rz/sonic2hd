using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SonicOrca.Core;
using SonicOrca.Geometry;

namespace SonicOrca.Original;

/// <summary>
/// The object layouts of Knuckles mode. "Knuckles in Sonic 2" (KiS2) changes the object layouts of 15 of the 17 acts:
/// it adds invisible walls, layer switches, monitors and spikes for the glide and the climb, and it moves or removes
/// some objects. The maps, the collision and the rings do not change. The binding of Knuckles mode is BINDINGK, next to
/// the binding of the zone. The area of the zone loads it when Knuckles plays (Area.GetLevelBinding).
/// <para>
/// The import of a zone makes BINDINGK of the imported zones from the layouts of KiS2 (LevelImport.Import). The HD
/// zones (EHZ, CPZ and HTZ) have bindings of their own. Their maps keep the positions of the original, so the
/// Knuckles binding of an HD zone is its HD binding with the changes of KiS2: an object that KiS2 removes goes out of
/// the HD binding when the HD binding has the same object at the same place, and an object that KiS2 adds comes in.
/// </para>
/// </summary>
public static class KnucklesLayouts
{
	/// <summary>The name of the binding of Knuckles mode, next to BINDING in the folder of the zone.</summary>
	public const string BindingName = "BINDINGK";

	/// <summary>An act of an HD zone: its object layout and the position of its origin in the HD map, in level units.</summary>
	public sealed record HdAct(string ObjectsFile, Vector2i Origin);

	/// <summary>
	/// The acts of the HD zones. The origins come from the start markers of the HD maps, and the positions of the
	/// objects of the HD bindings agree with them.
	/// </summary>
	public static readonly IReadOnlyDictionary<string, HdAct[]> HdZones = new Dictionary<string, HdAct[]>
	{
		["EHZ"] = new[] { new HdAct("level/objects/EHZ_1.bin", new Vector2i(0, 0)), new HdAct("level/objects/EHZ_2.bin", new Vector2i(44736, 192)) },
		["CPZ"] = new[] { new HdAct("level/objects/CPZ_1.bin", new Vector2i(0, 0)), new HdAct("level/objects/CPZ_2.bin", new Vector2i(41984, 3072)) },
		["HTZ"] = new[] { new HdAct("level/objects/HTZ_1.bin", new Vector2i(0, 0)), new HdAct("level/objects/HTZ_2.bin", new Vector2i(45056, 0)) },
	};

	/// <summary>
	/// An HD object is the object that KiS2 removes when it is this near, in level units on each axis. Some HD objects
	/// have their position at a corner, not at the centre, for example the blocks of CPZ: 16 pixels away.
	/// </summary>
	private static readonly Vector2i MatchDistance = new Vector2i(64, 64);

	/// <summary>
	/// The HD layer switches are up to 16 pixels to the left of the original ones, and some are moved up or down
	/// along the switch, so a layer switch matches from farther away.
	/// </summary>
	private static readonly Vector2i LayerSwitchMatchDistance = new Vector2i(64, 320);

	private const string LayerSwitchKey = "SONICORCA/OBJECTS/LAYERSWITCH";

	/// <summary>
	/// Makes the Knuckles binding of an HD zone from its HD binding.
	/// </summary>
	/// <param name="zone">EHZ, CPZ or HTZ.</param>
	/// <param name="hdBinding">The XML of the HD binding.</param>
	/// <param name="s2disasm">The folder of s2disasm, for the object layouts of Sonic 2.</param>
	/// <param name="kis2">The folder of KiS2, for the object layouts of KiS2.</param>
	/// <param name="log">Gets a note of the changes.</param>
	/// <param name="addedKeys">Gets the keys of the objects that the binding adds.</param>
	/// <returns>The XML of the Knuckles binding.</returns>
	public static string MakeHdBinding(string zone, string hdBinding, string s2disasm, string kis2, Action<string> log, ISet<string> addedKeys)
	{
		XDocument document = XDocument.Parse(hdBinding);
		XElement definitions = document.Root.Element("Definitions");
		int defaultLayer = (int?)definitions.Attribute("DefaultLayer") ?? 0;
		int removed = 0;
		int notFound = 0;
		int added = 0;
		int moved = 0;
		int unsupported = 0;
		int incompatible = 0;
		int index = 0;
		foreach (HdAct act in HdZones[zone])
		{
			List<byte[]> sonic2 = ReadEntries(Path.Combine(s2disasm, act.ObjectsFile));
			List<byte[]> knuckles = ReadEntries(Path.Combine(kis2, act.ObjectsFile));
			List<byte[]> removedEntries = Subtract(sonic2, knuckles).ToList();
			List<byte[]> addedEntries = Subtract(knuckles, sonic2).ToList();
			// A move: a removed entry and an added entry that differ only in their position. The HD objects keep their
			// own values and move with the original, because the conversion does not always make the values of the HD
			// objects (for example the blocks of CPZ).
			var moves = new HashSet<(string Key, Vector2i From, Vector2i Delta)>();
			foreach (byte[] entry in addedEntries.ToArray())
			{
				byte[] from = removedEntries.Where(r => SameExceptPosition(r, entry)).OrderBy(r => Distance(r, entry)).FirstOrDefault();
				ObjectPlacement converted = from != null ? Convert(from, zone) : null;
				if (converted == null)
				{
					continue;
				}
				removedEntries.Remove(from);
				addedEntries.Remove(entry);
				moves.Add((converted.Key, OriginalPosition(from) + act.Origin, OriginalPosition(entry) - OriginalPosition(from)));
			}
			foreach ((string key, Vector2i from, Vector2i delta) in moves)
			{
				moved += MoveDefinitions(definitions, key, from, delta);
			}
			foreach (byte[] entry in removedEntries)
			{
				ObjectPlacement converted = Convert(entry, zone);
				if (converted == null)
				{
					continue;
				}
				// The conversion moves some objects, for example the monitors, so the HD object can be at the converted
				// position or at the position of the original
				XElement match = FindDefinition(definitions, converted.Key, converted.Position + act.Origin, OriginalPosition(entry) + act.Origin);
				if (match == null)
				{
					notFound++;
					continue;
				}
				match.Remove();
				removed++;
			}
			foreach (byte[] entry in addedEntries)
			{
				ObjectPlacement converted = Convert(entry, zone);
				if (converted == null)
				{
					unsupported++;
					continue;
				}
				if (!HasCompatibleValues(definitions, converted))
				{
					incompatible++;
					continue;
				}
				string name = converted.Key.Substring(converted.Key.LastIndexOf('/') + 1).ToLowerInvariant();
				int layer = GetLayer(definitions, converted.Key, defaultLayer);
				// The state of a placement is an object with a property for each value, as LevelImport makes it
				IDictionary<string, object> state = new System.Dynamic.ExpandoObject();
				foreach (KeyValuePair<string, object> value in converted.Behaviour)
				{
					state[value.Key] = value.Value;
				}
				var placement = new ObjectPlacement(converted.Key, MakeUid(zone, index), $"{name} kis2-{index}", layer, converted.Position + act.Origin, state);
				index++;
				definitions.Add(ToDefinition(placement, defaultLayer));
				addedKeys.Add(converted.Key);
				added++;
			}
		}
		log($"Knuckles binding of {zone}: {added} objects added, {removed} removed, {moved} moved. " +
			$"{notFound} removed objects are not in the HD binding. {unsupported} added objects have no SonicOrca object, and " +
			$"{incompatible} have values that the HD objects do not use.");
		// The mark of the moved objects is not part of the binding
		foreach (XAttribute mark in document.Descendants("Position").Attributes("Moved").ToArray())
		{
			mark.Remove();
		}
		return document.ToString();
	}

	/// <summary>The 6-byte entries of an object layout, up to the end marker $FFFF.</summary>
	private static List<byte[]> ReadEntries(string file)
	{
		byte[] data = File.ReadAllBytes(file);
		var entries = new List<byte[]>();
		for (int i = 0; i + 6 <= data.Length && !(data[i] == 0xFF && data[i + 1] == 0xFF); i += 6)
		{
			entries.Add(data.AsSpan(i, 6).ToArray());
		}
		return entries;
	}

	/// <summary>The entries of the first layout that the second layout does not have, as a difference of multisets.</summary>
	private static IEnumerable<byte[]> Subtract(List<byte[]> from, List<byte[]> other)
	{
		var counts = other.GroupBy(System.Convert.ToHexString).ToDictionary(g => g.Key, g => g.Count());
		foreach (byte[] entry in from)
		{
			string key = System.Convert.ToHexString(entry);
			if (counts.TryGetValue(key, out int count) && count > 0)
			{
				counts[key] = count - 1;
				continue;
			}
			yield return entry;
		}
	}

	private static Vector2i OriginalPosition(byte[] entry) => new ObjectPlacements.OriginalObjectPlacement(entry).Position * 4;

	/// <summary>True when two entries have the same flags, type and subtype. Bits 4 to 7 of byte 2 are the flags.</summary>
	private static bool SameExceptPosition(byte[] a, byte[] b) => (a[2] & 0xF0) == (b[2] & 0xF0) && a[4] == b[4] && a[5] == b[5];

	private static int Distance(byte[] a, byte[] b)
	{
		Vector2i delta = OriginalPosition(a) - OriginalPosition(b);
		return Math.Abs(delta.X) + Math.Abs(delta.Y);
	}

	/// <summary>The radius around a moved original object, in level units, in which the HD objects of its kind move with it.</summary>
	private const int MoveRadius = 256;

	/// <summary>Moves the HD objects with the key near a position. Returns the number of objects that moved.</summary>
	private static int MoveDefinitions(XElement definitions, string key, Vector2i from, Vector2i delta)
	{
		int count = 0;
		foreach (XElement position in definitions.Elements("Definition").Select(d => d.Element("Common"))
			.Where(common => string.Equals((string)common?.Element("Key"), key, StringComparison.OrdinalIgnoreCase))
			.Select(common => common.Element("Position")).ToArray())
		{
			var current = new Vector2i((int)position.Attribute("X"), (int)position.Attribute("Y"));
			if (position.Attribute("Moved") != null || Math.Abs(current.X - from.X) > MoveRadius || Math.Abs(current.Y - from.Y) > MoveRadius)
			{
				continue;
			}
			position.SetAttributeValue("X", current.X + delta.X);
			position.SetAttributeValue("Y", current.Y + delta.Y);
			// Each object moves once, also when several original objects at one place move
			position.SetAttributeValue("Moved", true);
			count++;
		}
		return count;
	}

	/// <summary>
	/// True when the HD objects with the key of a converted object use one of its values, or when the converted object
	/// has no values, or when the HD binding has no object with its key. The conversion of some objects makes values
	/// for older objects, for example "Type" for the blocks of CPZ. An HD binding leaves out the values that are at
	/// their default, so the HD objects do not need to use all the values.
	/// </summary>
	private static bool HasCompatibleValues(XElement definitions, ObjectPlacement converted)
	{
		var hdValues = definitions.Elements("Definition")
			.Where(d => string.Equals((string)d.Element("Common")?.Element("Key"), converted.Key, StringComparison.OrdinalIgnoreCase))
			.SelectMany(d => d.Element("Behaviour")?.Elements() ?? Enumerable.Empty<XElement>())
			.Select(e => e.Name.LocalName)
			.ToHashSet(StringComparer.Ordinal);
		bool hdHasKey = definitions.Elements("Definition").Any(d => string.Equals((string)d.Element("Common")?.Element("Key"), converted.Key, StringComparison.OrdinalIgnoreCase));
		return !hdHasKey || converted.Behaviour.Count == 0 || converted.Behaviour.Any(value => hdValues.Contains(value.Key));
	}

	private static ObjectPlacement Convert(byte[] entry, string zone)
	{
		// The HD maps have no layer for the high-priority tiles, as the import of an HD zone
		return ObjectPlacements.ConvertOriginalObjectPlacement(new ObjectPlacements.OriginalObjectPlacement(entry), false, zone);
	}

	/// <summary>
	/// The nearest definition with the key to one of the positions, not farther than the match distance on each axis,
	/// or null.
	/// </summary>
	private static XElement FindDefinition(XElement definitions, string key, params Vector2i[] positions)
	{
		Vector2i limit = string.Equals(key, LayerSwitchKey, StringComparison.OrdinalIgnoreCase) ? LayerSwitchMatchDistance : MatchDistance;
		return definitions.Elements("Definition")
			.Select(definition => (Definition: definition, Common: definition.Element("Common")))
			.Where(item => string.Equals((string)item.Common?.Element("Key"), key, StringComparison.OrdinalIgnoreCase))
			.Select(item => (item.Definition, Position: new Vector2i((int)item.Common.Element("Position").Attribute("X"), (int)item.Common.Element("Position").Attribute("Y"))))
			.SelectMany(item => positions.Select(position => (item.Definition, DX: Math.Abs(item.Position.X - position.X), DY: Math.Abs(item.Position.Y - position.Y))))
			.Where(item => item.DX <= limit.X && item.DY <= limit.Y)
			.OrderBy(item => item.DX + item.DY)
			.Select(item => item.Definition)
			.FirstOrDefault();
	}

	/// <summary>The layer of the objects with the key in the HD binding (the layer that most of them use), or the default layer.</summary>
	private static int GetLayer(XElement definitions, string key, int defaultLayer)
	{
		return definitions.Elements("Definition")
			.Select(definition => definition.Element("Common"))
			.Where(common => string.Equals((string)common?.Element("Key"), key, StringComparison.OrdinalIgnoreCase))
			.Select(common => (int?)common.Element("Layer") ?? defaultLayer)
			.GroupBy(layer => layer)
			.OrderByDescending(group => group.Count())
			.Select(group => group.Key)
			.DefaultIfEmpty(defaultLayer)
			.First();
	}

	/// <summary>The definition of a placement, as LevelBindingWriter writes it, in a binding with this default layer.</summary>
	private static XElement ToDefinition(ObjectPlacement placement, int defaultLayer)
	{
		var binding = new LevelBinding();
		binding.ObjectPlacements.Add(placement);
		using var stream = new MemoryStream();
		new LevelBindingWriter(binding).Save(stream);
		XElement definition = XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()).TrimStart('﻿')).Descendants("Definition").Single();
		// The writer of one placement takes its layer as the default layer
		XElement common = definition.Element("Common");
		common.Element("Layer")?.Remove();
		if (placement.Layer != defaultLayer)
		{
			common.Element("Name").AddAfterSelf(new XElement("Layer", placement.Layer));
		}
		return definition;
	}

	private static Guid MakeUid(string zone, int index) => new Guid(Md5.HashData(Encoding.UTF8.GetBytes($"SONICORCA/LEVELS/{zone}/kis2/{index}")));
}
