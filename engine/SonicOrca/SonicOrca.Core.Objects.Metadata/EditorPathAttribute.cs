using System;

namespace SonicOrca.Core.Objects.Metadata;

/// <summary>
/// Marks a string state variable as a path: points relative to the object, in the form "x,y|x,y|#x,y". A '#' marks a
/// point where the object does something, for example a sound. The level editor draws the path and lets the user
/// move, add and remove its points.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class EditorPathAttribute : Attribute
{
	public EditorPathAttribute(string description = null, string markName = null)
	{
		Description = description;
		MarkName = markName;
	}

	/// <summary>What the path does, for the tooltip in the editor, or null.</summary>
	public string Description { get; }

	/// <summary>What a '#' on a point does, for example "spin sound", or null when the object does not use it.</summary>
	public string MarkName { get; }
}
