using System;
using System.Collections.Generic;

namespace SonicOrca.Core.Editor;

/// <summary>
/// Changes the order of the story: the list of the short names of the levels of the story. The levels tool of the
/// editor uses these functions.
/// </summary>
public static class EditorLevelOrder
{
	/// <summary>
	/// Moves a level of the story up (-1) or down (1). Nothing changes when the level is not in the story or is
	/// already at the end of the list.
	/// </summary>
	/// <returns>True when the order changed.</returns>
	public static bool Move(IList<string> story, string mnemonic, int delta)
	{
		int index = IndexOf(story, mnemonic);
		int target = index + delta;
		if (index < 0 || target < 0 || target >= story.Count)
		{
			return false;
		}
		string level = story[index];
		story.RemoveAt(index);
		story.Insert(target, level);
		return true;
	}

	/// <summary>Adds a level to the end of the story, or removes it from the story.</summary>
	/// <returns>True when the order changed.</returns>
	public static bool SetInStory(IList<string> story, string mnemonic, bool inStory)
	{
		int index = IndexOf(story, mnemonic);
		if (inStory == index >= 0)
		{
			return false;
		}
		if (inStory)
		{
			story.Add(mnemonic);
		}
		else
		{
			story.RemoveAt(index);
		}
		return true;
	}

	/// <summary>The position of a level in the story, or -1. The comparison ignores case.</summary>
	public static int IndexOf(IList<string> story, string mnemonic)
	{
		for (int i = 0; i < story.Count; i++)
		{
			if (string.Equals(story[i], mnemonic, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}
}
