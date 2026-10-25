using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonicOrca.Resources;

/// <summary>
/// Converts between a resource tree and a folder of normal files that people can edit.
/// <list type="bullet">
/// <item>Each key segment is a folder: SONICORCA/LEVELS/EHZ/MAP is SONICORCA/LEVELS/EHZ/MAP.map.xml.</item>
/// <item>The extension gives the type (<see cref="ResourceType.DefaultExtension"/>), from the first dot.</item>
/// <item>A resource that also has children is the file "_" in its own folder: SONICORCA/FONTS/HUD/_.font.xml.</item>
/// <item>When reading, an underscore in a file name also separates key segments (A_B.png is A/B), and names are
/// converted to upper case. This is the convention of the original SonicOrca tools.</item>
/// </list>
/// Files hold the uncompressed bytes. <see cref="ResourceFile.Write"/> compresses them again where the type needs it.
/// </summary>
public static class ResourceDirectory
{
	private const string SelfFileName = "_";

	/// <summary>
	/// Writes each resource of the tree as a file under <paramref name="directory"/>. With <paramref name="include"/>,
	/// only the resources that it accepts. Returns the file count.
	/// </summary>
	public static int Export(ResourceTree tree, string directory, Func<Resource, bool> include = null)
	{
		int count = 0;
		foreach (ResourceTree.Node child in tree.Head.Children)
		{
			count += ExportNode(child, directory, include);
		}
		return count;
	}

	private static int ExportNode(ResourceTree.Node node, string parentDirectory, Func<Resource, bool> include)
	{
		int count = 0;
		string nodeDirectory = Path.Combine(parentDirectory, node.Key);
		if (node.Resource != null && (include == null || include(node.Resource)))
		{
			string extension = ResourceType.FromIdentifier(node.Resource.Identifier).DefaultExtension;
			string path = node.Children.Count > 0
				? Path.Combine(nodeDirectory, SelfFileName + extension)
				: Path.Combine(parentDirectory, node.Key + extension);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (Stream source = node.Resource.Source.ReadUncompressed())
			using (FileStream file = File.Create(path))
			{
				source.CopyTo(file);
			}
			count++;
		}
		foreach (ResourceTree.Node child in node.Children)
		{
			count += ExportNode(child, nodeDirectory, include);
		}
		return count;
	}

	/// <summary>
	/// Reads every file under <paramref name="directory"/> into a new tree. The resources refer to the files, so
	/// the files must stay in place until the tree is written. Files without a known extension are not read, and
	/// their paths are added to <paramref name="skippedFiles"/>.
	/// </summary>
	public static ResourceTree Import(string directory, ICollection<string> skippedFiles = null)
	{
		ResourceTree tree = new ResourceTree();
		string root = Path.GetFullPath(directory);
		foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
		{
			string fullKeyPath = GetFullKeyPath(root, path, out string extension);
			ResourceType type = extension == null ? null
				: ResourceType.RegisteredResourceTypes.FirstOrDefault(t => string.Equals(t.DefaultExtension, extension, StringComparison.OrdinalIgnoreCase));
			if (type == null || string.IsNullOrEmpty(fullKeyPath))
			{
				skippedFiles?.Add(path);
				continue;
			}
			long size = new FileInfo(path).Length;
			tree.SetOrAdd(fullKeyPath, new Resource(fullKeyPath, type.Identifier, new FileResourceSource(path, 0, size)));
		}
		return tree;
	}

	private static string GetFullKeyPath(string root, string path, out string extension)
	{
		string relative = Path.GetRelativePath(root, path);
		List<string> segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToList();
		string fileName = segments[^1];
		segments.RemoveAt(segments.Count - 1);
		int dot = fileName.IndexOf('.');
		extension = dot >= 0 ? fileName.Substring(dot) : null;
		string name = dot >= 0 ? fileName.Substring(0, dot) : fileName;
		if (name != SelfFileName)
		{
			segments.AddRange(name.Split('_', StringSplitOptions.RemoveEmptyEntries));
		}
		return string.Join("/", segments).ToUpperInvariant();
	}
}
