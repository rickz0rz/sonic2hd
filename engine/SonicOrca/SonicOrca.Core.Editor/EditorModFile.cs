using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
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

	/// <summary>The folder in the editor folder for the changes that the user did not save. The game does not load it.</summary>
	public const string UnsavedFolderName = "unsaved";

	/// <summary>A resource to write, with its new data.</summary>
	public sealed class Change
	{
		public Resource Resource { get; }

		public byte[] Data { get; }

		public Change(Resource resource, byte[] data)
		{
			Resource = resource;
			Data = data;
		}

		/// <summary>The binding as the level has it now.</summary>
		public static Change ForBinding(LevelBinding binding)
		{
			using MemoryStream xml = new MemoryStream();
			new LevelBindingWriter(binding).Save(xml);
			return new Change(binding.Resource, xml.ToArray());
		}

		/// <summary>
		/// The map with new data for the parts that changed. The other content of the map file stays as it is (see
		/// <see cref="LevelMapWriter.Patch"/>).
		/// </summary>
		public static Change ForMap(LevelMap map, bool tiles = false, bool collision = false, bool markers = false)
		{
			XmlDocument document = new XmlDocument();
			using (Stream stream = map.Resource.Source.ReadUncompressed())
			{
				document.Load(stream);
			}
			new LevelMapWriter(map).Patch(document, tiles, collision, markers);
			using MemoryStream xml = new MemoryStream();
			using (XmlWriter writer = XmlWriter.Create(xml, new XmlWriterSettings { Indent = true }))
			{
				document.Save(writer);
			}
			return new Change(map.Resource, xml.ToArray());
		}
	}

	/// <summary>Gives the folder of the editor files.</summary>
	public static string GetFolder(string userDataDirectory) => Path.Combine(userDataDirectory, "mods", FolderName);

	/// <summary>Gives the file of the zone of a resource, for example mods/editor/CNZ.dat for SONICORCA/LEVELS/CNZ/BINDING.</summary>
	public static string GetPath(string userDataDirectory, string resourceKey)
	{
		string[] parts = resourceKey.Split('/');
		string zone = parts.Length >= 2 ? parts[parts.Length - 2] : resourceKey;
		return Path.Combine(GetFolder(userDataDirectory), zone + ".dat");
	}

	/// <summary>Writes the binding into the file of its zone. See <see cref="Save(IReadOnlyList{Change}, string, bool)"/>.</summary>
	public static string Save(LevelBinding binding, string userDataDirectory)
	{
		return Save(new[] { Change.ForBinding(binding) }, GetPath(userDataDirectory, binding.Resource.FullKeyPath));
	}

	/// <summary>
	/// Writes resources into a file, and keeps the other resources of the file. With <paramref name="updateSources"/>,
	/// each resource then reads its new data, so that a load later in this session also has the changes.
	/// </summary>
	/// <returns>The path of the file.</returns>
	public static string Save(IReadOnlyList<Change> changes, string path, bool updateSources = true)
	{
		if (changes.Count == 0)
		{
			return path;
		}
		ResourceTree tree = File.Exists(path) ? ReadIntoMemory(path) : new ResourceTree();
		List<(Resource Resource, MemoryResourceSource Source)> sources = new List<(Resource, MemoryResourceSource)>();
		foreach (Change change in changes)
		{
			string key = change.Resource.FullKeyPath;
			MemoryResourceSource source = new MemoryResourceSource(change.Data);
			tree.SetOrAdd(key, new Resource(key, change.Resource.Identifier, source));
			sources.Add((change.Resource, source));
		}
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string temporaryPath = path + ".tmp";
		new ResourceFile(temporaryPath).Write(tree);
		File.Move(temporaryPath, path, overwrite: true);
		if (updateSources)
		{
			foreach ((Resource resource, MemoryResourceSource source) in sources)
			{
				resource.Source = source;
			}
		}
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
