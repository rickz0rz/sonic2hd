using System.Collections.Generic;
using System.IO;
using SonicOrca.Resources;

namespace SonicOrca.Core.Editor;

/// <summary>
/// The file where the level editor saves the changes of a zone: mods/editor/ZONE.dat in the user data folder. The
/// game loads the files of this folder after all the other mods, so the changes replace the resources of the game
/// and of the imports. Delete the file to remove the changes.
/// </summary>
public static class EditorModFile
{
	/// <summary>The folder in the mods folder of the user data folder.</summary>
	public const string FolderName = "editor";

	/// <summary>Gives the folder of the editor files.</summary>
	public static string GetFolder(string userDataDirectory) => Path.Combine(userDataDirectory, "mods", FolderName);

	/// <summary>Gives the file of the zone of a resource, for example mods/editor/CNZ.dat for SONICORCA/LEVELS/CNZ/BINDING.</summary>
	public static string GetPath(string userDataDirectory, string resourceKey)
	{
		string[] parts = resourceKey.Split('/');
		string zone = parts.Length >= 2 ? parts[parts.Length - 2] : resourceKey;
		return Path.Combine(GetFolder(userDataDirectory), zone + ".dat");
	}

	/// <summary>
	/// Writes the binding into the file of its zone, and keeps the other resources of the file. The resource of the
	/// binding then reads the new data, so that a load of the level later in this session also has the changes.
	/// </summary>
	/// <returns>The path of the file.</returns>
	public static string Save(LevelBinding binding, string userDataDirectory)
	{
		string key = binding.Resource.FullKeyPath;
		string path = GetPath(userDataDirectory, key);
		using MemoryStream xml = new MemoryStream();
		new LevelBindingWriter(binding).Save(xml);
		MemoryResourceSource source = new MemoryResourceSource(xml.ToArray());

		ResourceTree tree = File.Exists(path) ? ReadIntoMemory(path) : new ResourceTree();
		tree.SetOrAdd(key, new Resource(key, ResourceTypeIdentifier.Binding, source));
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string temporaryPath = path + ".tmp";
		new ResourceFile(temporaryPath).Write(tree);
		File.Move(temporaryPath, path, overwrite: true);
		binding.Resource.Source = source;
		return path;
	}

	/// <summary>
	/// Reads all the resources of a file into memory. The editor can then write the file again while the game uses
	/// the resources.
	/// </summary>
	public static ResourceTree ReadIntoMemory(string path)
	{
		ResourceTree tree = new ResourceFile(path).Scan();
		foreach (KeyValuePair<string, Resource> item in tree.GetResourceListing())
		{
			item.Value.Source = MemoryResourceSource.CopyOf(item.Value.Source);
		}
		return tree;
	}
}
