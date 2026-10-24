using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SonicOrca.Core.SpecialStage;

/// <summary>The segment types of the Sonic 2 special stage track. The numbers are the values in the original data.</summary>
public enum HalfPipeSegmentType
{
	TurnThenRise = 0,
	TurnThenDrop = 1,
	TurnThenStraight = 2,
	Straight = 3,
	StraightThenTurn = 4
}

/// <summary>What happens after the objects of a segment. The marker ends the segment's object group.</summary>
public enum HalfPipeMarker
{
	None,
	/// <summary>Shows the "rings to go" counter for the next checkpoint.</summary>
	RingsToGo,
	Checkpoint,
	/// <summary>The emerald comes, and its ring test is the last checkpoint.</summary>
	Emerald
}

/// <summary>
/// A ring or a bomb in the object group of a segment. <see cref="Distance"/> is in track frames after the start of
/// the next segment. <see cref="Angle"/> uses the units of the original game: 256 for each circle, $40 at the bottom
/// of the pipe, and the angle goes up toward the left.
/// </summary>
public readonly record struct HalfPipeObjectEntry(HalfPipeObjectKind Kind, int Distance, int Angle);

/// <summary>One segment of the track, with the objects that it brings.</summary>
public sealed class HalfPipeSegment
{
	public HalfPipeSegmentType Type { get; init; }

	/// <summary>Mirrors the segment. It decides the direction of the turns.</summary>
	public bool Flip { get; init; }

	public List<HalfPipeObjectEntry> Objects { get; init; } = new List<HalfPipeObjectEntry>();

	public HalfPipeMarker Marker { get; init; }
}

/// <summary>
/// The layout of one special stage: the track segments, and for each segment its rings, bombs and marker. This is
/// the structure of the original Sonic 2 data, so that the original stages can be imported without changes.
/// </summary>
/// <remarks>
/// The XML format:
/// <code>
/// &lt;halfpipestage&gt;
///   &lt;segment type="StraightThenTurn" flip="true" marker="Checkpoint"&gt;
///     &lt;ring distance="5" angle="64" /&gt;
///     &lt;bomb distance="3" angle="40" /&gt;
///   &lt;/segment&gt;
/// &lt;/halfpipestage&gt;
/// </code>
/// </remarks>
public sealed class HalfPipeStageData
{
	public List<HalfPipeSegment> Segments { get; init; } = new List<HalfPipeSegment>();

	public static HalfPipeStageData FromXml(XmlDocument document)
	{
		XmlElement root = document.DocumentElement;
		if (root == null || root.Name != "halfpipestage")
		{
			throw new InvalidDataException("The document is not a halfpipestage.");
		}
		var stage = new HalfPipeStageData();
		foreach (XmlElement segmentElement in root.GetElementsByTagName("segment").Cast<XmlElement>())
		{
			var objects = new List<HalfPipeObjectEntry>();
			foreach (XmlElement objectElement in segmentElement.ChildNodes.OfType<XmlElement>())
			{
				HalfPipeObjectKind kind = objectElement.Name switch
				{
					"ring" => HalfPipeObjectKind.Ring,
					"bomb" => HalfPipeObjectKind.Bomb,
					_ => throw new InvalidDataException("Unknown object: " + objectElement.Name),
				};
				objects.Add(new HalfPipeObjectEntry(kind, ParseInt(objectElement, "distance"), ParseInt(objectElement, "angle")));
			}
			stage.Segments.Add(new HalfPipeSegment
			{
				Type = Enum.Parse<HalfPipeSegmentType>(segmentElement.GetAttribute("type")),
				Flip = segmentElement.GetAttribute("flip") == "true",
				Marker = segmentElement.HasAttribute("marker") ? Enum.Parse<HalfPipeMarker>(segmentElement.GetAttribute("marker")) : HalfPipeMarker.None,
				Objects = objects,
			});
		}
		return stage;
	}

	public void WriteXml(TextWriter output)
	{
		using XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true, IndentChars = "  " });
		writer.WriteStartElement("halfpipestage");
		foreach (HalfPipeSegment segment in Segments)
		{
			writer.WriteStartElement("segment");
			writer.WriteAttributeString("type", segment.Type.ToString());
			if (segment.Flip)
			{
				writer.WriteAttributeString("flip", "true");
			}
			if (segment.Marker != HalfPipeMarker.None)
			{
				writer.WriteAttributeString("marker", segment.Marker.ToString());
			}
			foreach (HalfPipeObjectEntry entry in segment.Objects)
			{
				writer.WriteStartElement(entry.Kind == HalfPipeObjectKind.Ring ? "ring" : "bomb");
				writer.WriteAttributeString("distance", entry.Distance.ToString(CultureInfo.InvariantCulture));
				writer.WriteAttributeString("angle", entry.Angle.ToString(CultureInfo.InvariantCulture));
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	private static int ParseInt(XmlElement element, string name)
	{
		return int.Parse(element.GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture);
	}
}
