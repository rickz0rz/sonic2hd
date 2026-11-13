using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonicOrca.Core.Editor;

namespace SonicOrca;

/// <summary>Gives the order in which the game loads the resource files and the mod folders of a folder.</summary>
public static class ModLoadOrder
{
	/// <summary>
	/// Gives the .dat files of a folder in name order. With <paramref name="folders"/>, the mod folders follow, also in
	/// name order. A later file replaces the resources of an earlier file, so a mod folder replaces a .dat file. Thus a
	/// folder "ending" with changed files replaces "ending.dat". The editor folders and the folders whose names start
	/// with a dot are not mods.
	/// </summary>
	public static IReadOnlyList<string> GetPaths(string directory, bool folders)
	{
		if (!Directory.Exists(directory))
		{
			return Array.Empty<string>();
		}
		IEnumerable<string> paths = SortByName(Directory.GetFiles(directory, "*.dat"));
		if (folders)
		{
			paths = paths.Concat(SortByName(Directory.GetDirectories(directory).Where(d =>
			{
				string name = Path.GetFileName(d);
				// The editor folder loads last; in it, the changes that the user did not save do not load
				return !name.StartsWith('.') && !name.Equals(EditorModFile.FolderName, StringComparison.OrdinalIgnoreCase) &&
					!name.Equals(EditorModFile.UnsavedFolderName, StringComparison.OrdinalIgnoreCase);
			})));
		}
		return paths.ToList();
	}

	private static IEnumerable<string> SortByName(IEnumerable<string> paths) =>
		paths.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);
}
