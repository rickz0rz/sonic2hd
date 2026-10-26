using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Core.Editor;

/// <summary>
/// An animation group (".anigroup.xml") that the animation editor can change and write again. Each frame has all its
/// values, as the game reads them (see AnimationGroupResourceType): the values that a frame does not set come from
/// its animation. The game finds an animation by its index, so the comments before an animation are its only name;
/// the document keeps them.
/// </summary>
public sealed class AnimationGroupDocument
{
	/// <summary>The texture keys as the file has them, for example "//BODY", relative to the key of the group.</summary>
	public List<string> Textures { get; } = new List<string>();

	public List<AnimationDocument> Animations { get; } = new List<AnimationDocument>();

	/// <summary>Reads the text of an animation group file.</summary>
	public static AnimationGroupDocument Parse(string xml)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(xml);
		return FromXml(xmlDocument);
	}

	/// <summary>Reads an animation group file.</summary>
	public static AnimationGroupDocument Load(Stream stream)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(stream);
		return FromXml(xmlDocument);
	}

	private static AnimationGroupDocument FromXml(XmlDocument xmlDocument)
	{
		AnimationGroupDocument document = new AnimationGroupDocument();
		XmlNode group = xmlDocument.SelectSingleNode("anigroup") ?? throw new FormatException("The file has no anigroup element.");
		foreach (XmlNode texture in group.SelectNodes("textures/texture"))
		{
			document.Textures.Add(texture.InnerText.Trim());
		}
		List<string> comments = new List<string>();
		XmlNode animations = group.SelectSingleNode("animations");
		foreach (XmlNode node in animations?.ChildNodes.OfType<XmlNode>() ?? Enumerable.Empty<XmlNode>())
		{
			if (node is XmlComment comment)
			{
				comments.Add(comment.Value.Trim());
			}
			else if (node is XmlElement { Name: "animation" } element)
			{
				document.Animations.Add(AnimationDocument.FromXml(element, comments));
				comments = new List<string>();
			}
		}
		return document;
	}

	/// <summary>
	/// Adds an animation after the last one. The game finds an animation by its index, so the editor adds and removes
	/// animations only at the end: the other animations keep their indexes.
	/// </summary>
	public void AddAnimation(AnimationDocument animation) => Animations.Add(animation);

	/// <summary>Removes the last animation. A "next" that pointed to it is removed too.</summary>
	public void RemoveLastAnimation()
	{
		if (Animations.Count == 0)
		{
			return;
		}
		int removed = Animations.Count - 1;
		Animations.RemoveAt(removed);
		foreach (AnimationDocument animation in Animations.Where(a => a.Next == removed))
		{
			animation.Next = null;
		}
	}

	/// <summary>A copy that shares no animation or frame with this document, for the undo history.</summary>
	public AnimationGroupDocument Clone()
	{
		AnimationGroupDocument copy = new AnimationGroupDocument();
		copy.Textures.AddRange(Textures);
		copy.Animations.AddRange(Animations.Select(animation => animation.Clone()));
		return copy;
	}

	/// <summary>
	/// Writes the file. Each animation gets the most common value of each frame attribute, and a frame gives only the
	/// values that are different, as the files of the game do.
	/// </summary>
	public string ToXml()
	{
		StringBuilder xml = new StringBuilder();
		xml.Append("<?xml version=\"1.0\"?>\n");
		xml.Append("<anigroup>\n");
		xml.Append("  <textures>\n");
		foreach (string texture in Textures)
		{
			xml.Append("    <texture>").Append(Escape(texture)).Append("</texture>\n");
		}
		xml.Append("  </textures>\n");
		xml.Append("  <animations>\n");
		foreach (AnimationDocument animation in Animations)
		{
			animation.WriteXml(xml);
		}
		xml.Append("  </animations>\n");
		xml.Append("</anigroup>\n");
		return xml.ToString();
	}

	/// <summary>
	/// Makes the animations of the game from the document, for <see cref="AnimationGroup.Replace"/> in a running
	/// level. The game finds animations by index, so an animation after the last one of the document stays as it is
	/// in <paramref name="current"/>. Values that would stop the game are made safe: an animation without frames gets
	/// one empty frame, and a texture, a loop or a next out of range is clamped or removed.
	/// </summary>
	public List<Animation> ToAnimations(IReadOnlyList<Animation> current, int textureCount)
	{
		List<Animation> animations = new List<Animation>();
		int count = Math.Max(Animations.Count, current.Count);
		for (int i = 0; i < count; i++)
		{
			if (i >= Animations.Count)
			{
				animations.Add(current[i]);
				continue;
			}
			AnimationDocument animation = Animations[i];
			List<Animation.Frame> frames = animation.Frames.Select(frame => new Animation.Frame
			{
				TextureIndex = Math.Clamp(frame.Texture, 0, Math.Max(0, textureCount - 1)),
				Source = new Rectanglei(frame.X, frame.Y, frame.Width, frame.Height),
				Offset = new Vector2i(frame.OffsetX, frame.OffsetY),
				Delay = Math.Max(0, frame.Delay)
			}).ToList();
			if (frames.Count == 0)
			{
				frames.Add(new Animation.Frame());
			}
			int? loop = animation.Loop is int l ? Math.Clamp(l, 0, frames.Count - 1) : null;
			int? next = animation.Next is int n && n >= 0 && n < count ? n : null;
			animations.Add(new Animation(frames, next, loop));
		}
		return animations;
	}

	internal static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

/// <summary>An animation of an <see cref="AnimationGroupDocument"/>.</summary>
public sealed class AnimationDocument
{
	/// <summary>The comments before the animation in the file. The last one is its name, for example "wait".</summary>
	public List<string> Comments { get; } = new List<string>();

	/// <summary>The frame after the last frame, when the animation does not end there (the attribute "loop").</summary>
	public int? Loop { get; set; }

	/// <summary>The animation that comes after this one (the attribute "next").</summary>
	public int? Next { get; set; }

	public List<AnimationFrameDocument> Frames { get; } = new List<AnimationFrameDocument>();

	/// <summary>The name of the animation: its last comment, or null.</summary>
	public string Name
	{
		get => Comments.Count > 0 ? Comments[^1] : null;
		set
		{
			if (Comments.Count > 0)
			{
				Comments.RemoveAt(Comments.Count - 1);
			}
			if (!string.IsNullOrWhiteSpace(value))
			{
				// "--" cannot be in an XML comment
				Comments.Add(value.Trim().Replace("--", "-"));
			}
		}
	}

	/// <summary>Inserts a frame. The loop frame stays the same frame.</summary>
	public void InsertFrame(int index, AnimationFrameDocument frame)
	{
		index = Math.Clamp(index, 0, Frames.Count);
		Frames.Insert(index, frame);
		if (Loop is int loop && loop >= index && Frames.Count > 1)
		{
			Loop = loop + 1;
		}
	}

	/// <summary>Removes a frame. The loop frame stays the same frame, or the next frame when it is the removed frame.</summary>
	public void RemoveFrame(int index)
	{
		if (index < 0 || index >= Frames.Count)
		{
			return;
		}
		Frames.RemoveAt(index);
		if (Frames.Count == 0)
		{
			Loop = null;
		}
		else if (Loop is int loop && (loop > index || loop >= Frames.Count))
		{
			Loop = Math.Min(Frames.Count - 1, loop > index ? loop - 1 : loop);
		}
	}

	/// <summary>Moves a frame to another index. The loop frame stays the same frame.</summary>
	public void MoveFrame(int from, int to)
	{
		if (from < 0 || from >= Frames.Count || to < 0 || to >= Frames.Count || from == to)
		{
			return;
		}
		AnimationFrameDocument loopFrame = Loop is int loop && loop < Frames.Count ? Frames[loop] : null;
		AnimationFrameDocument frame = Frames[from];
		Frames.RemoveAt(from);
		Frames.Insert(to, frame);
		if (loopFrame != null)
		{
			Loop = Frames.IndexOf(loopFrame);
		}
	}

	public AnimationDocument Clone()
	{
		AnimationDocument copy = new AnimationDocument { Loop = Loop, Next = Next };
		copy.Comments.AddRange(Comments);
		copy.Frames.AddRange(Frames.Select(frame => frame.Clone()));
		return copy;
	}

	internal static AnimationDocument FromXml(XmlElement element, List<string> comments)
	{
		AnimationDocument animation = new AnimationDocument
		{
			Loop = GetOptional(element, "loop"),
			Next = GetOptional(element, "next")
		};
		animation.Comments.AddRange(comments);
		// The animation gives the values that its frames do not set
		AnimationFrameDocument defaults = new AnimationFrameDocument
		{
			Texture = GetOptional(element, "texture") ?? 0,
			Width = GetOptional(element, "w") ?? 0,
			Height = GetOptional(element, "h") ?? 0,
			OffsetX = GetOptional(element, "offset_x") ?? 0,
			OffsetY = GetOptional(element, "offset_y") ?? 0,
			Delay = GetOptional(element, "delay") ?? 0
		};
		foreach (XmlElement frame in element.SelectNodes("frame").OfType<XmlElement>())
		{
			animation.Frames.Add(new AnimationFrameDocument
			{
				X = GetOptional(frame, "x") ?? 0,
				Y = GetOptional(frame, "y") ?? 0,
				Width = GetOptional(frame, "w") ?? defaults.Width,
				Height = GetOptional(frame, "h") ?? defaults.Height,
				Texture = GetOptional(frame, "texture") ?? defaults.Texture,
				OffsetX = GetOptional(frame, "offset_x") ?? defaults.OffsetX,
				OffsetY = GetOptional(frame, "offset_y") ?? defaults.OffsetY,
				Delay = GetOptional(frame, "delay") ?? defaults.Delay
			});
		}
		return animation;
	}

	private static int? GetOptional(XmlElement element, string name)
	{
		string value = element.GetAttribute(name);
		return value.Length > 0 ? int.Parse(value, CultureInfo.InvariantCulture) : null;
	}

	internal void WriteXml(StringBuilder xml)
	{
		xml.Append('\n');
		foreach (string comment in Comments)
		{
			xml.Append("    <!-- ").Append(comment.Replace("--", "-")).Append(" -->\n");
		}
		// The most common value of each attribute goes on the animation; a value of 0 needs no attribute
		int texture = MostCommon(frame => frame.Texture);
		int width = MostCommon(frame => frame.Width);
		int height = MostCommon(frame => frame.Height);
		int offsetX = MostCommon(frame => frame.OffsetX);
		int offsetY = MostCommon(frame => frame.OffsetY);
		int delay = MostCommon(frame => frame.Delay);
		xml.Append("    <animation");
		Attribute(xml, "texture", texture, 0);
		Attribute(xml, "w", width, 0);
		Attribute(xml, "h", height, 0);
		Attribute(xml, "offset_x", offsetX, 0);
		Attribute(xml, "offset_y", offsetY, 0);
		Attribute(xml, "delay", delay, 0);
		if (Next is int next)
		{
			Attribute(xml, "next", next, int.MinValue);
		}
		if (Loop is int loop)
		{
			Attribute(xml, "loop", loop, int.MinValue);
		}
		if (Frames.Count == 0)
		{
			xml.Append(" />\n");
			return;
		}
		xml.Append(">\n");
		foreach (AnimationFrameDocument frame in Frames)
		{
			xml.Append("      <frame");
			Attribute(xml, "x", frame.X, int.MinValue);
			Attribute(xml, "y", frame.Y, int.MinValue);
			Attribute(xml, "w", frame.Width, width);
			Attribute(xml, "h", frame.Height, height);
			Attribute(xml, "texture", frame.Texture, texture);
			Attribute(xml, "offset_x", frame.OffsetX, offsetX);
			Attribute(xml, "offset_y", frame.OffsetY, offsetY);
			Attribute(xml, "delay", frame.Delay, delay);
			xml.Append(" />\n");
		}
		xml.Append("    </animation>\n");
	}

	private int MostCommon(Func<AnimationFrameDocument, int> value)
	{
		return Frames.Count == 0 ? 0 : Frames.GroupBy(value).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
	}

	/// <summary>Writes an attribute when its value is not the default value.</summary>
	private static void Attribute(StringBuilder xml, string name, int value, int defaultValue)
	{
		if (value != defaultValue)
		{
			xml.Append(' ').Append(name).Append("=\"").Append(value.ToString(CultureInfo.InvariantCulture)).Append('"');
		}
	}
}

/// <summary>A frame of an <see cref="AnimationDocument"/>, with all its values.</summary>
public sealed class AnimationFrameDocument
{
	/// <summary>The index of the texture in <see cref="AnimationGroupDocument.Textures"/>.</summary>
	public int Texture { get; set; }

	/// <summary>The source rectangle in the texture, in pixels.</summary>
	public int X { get; set; }

	public int Y { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	/// <summary>The offset of the centre of the frame from the position of the object, in pixels.</summary>
	public int OffsetX { get; set; }

	public int OffsetY { get; set; }

	/// <summary>The extra frames that the frame shows: a delay of 0 shows the frame for one frame.</summary>
	public int Delay { get; set; }

	public AnimationFrameDocument Clone() => (AnimationFrameDocument)MemberwiseClone();
}
