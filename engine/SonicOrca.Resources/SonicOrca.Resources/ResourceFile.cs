using System;
using System.IO;
using System.Text;
using SonicOrca.Extensions;

namespace SonicOrca.Resources;

/// <summary>
/// A resource archive (.dat), format "SORF" version 2. All numbers are little-endian.
/// <code>
/// header: "SORF", version (byte), table size (int64)
/// table:  the root node, then each node's children directly after it (depth first)
/// node:   flags (byte), [child count (uint16) if HasChildren], key (null-terminated string),
///         [if HasResource: type (uint16), [path (string) if External], offset (uint32), size (uint32)]
/// data:   the resource data. Offsets count from the end of the table, unless the resource is External.
/// </code>
/// A Compressed resource is stored as gzip.
/// </summary>
public class ResourceFile
{
	[Flags]
	private enum NodeFlags : byte
	{
		HasChildren = 2,
		HasResource = 4,
		/// <summary>The data is in another file, at an absolute offset. Read, but never written.</summary>
		External = 8,
		Compressed = 0x10
	}

	private const int LatestSupportedVersion = 2;

	/// <summary>Size of the header: magic number, version and table size.</summary>
	private const int HeaderSize = 13;

	private static readonly int MagicNumber = BitConverter.ToInt32(Encoding.ASCII.GetBytes("SORF"), 0);

	private readonly string _path;

	public string Filename => _path;

	public ResourceFile(string path)
	{
		_path = path;
	}

	public override string ToString() => _path;

	public ResourceTree Scan()
	{
		try
		{
			using FileStream stream = new FileStream(_path, FileMode.Open, FileAccess.Read);
			BinaryReader reader = new BinaryReader(stream);
			if (stream.Length < HeaderSize || reader.ReadInt32() != MagicNumber)
			{
				throw new ResourceException("Invalid resource file.");
			}
			if (reader.ReadByte() > LatestSupportedVersion)
			{
				throw new ResourceException("Unsupported resource file version.");
			}
			long dataOffset = reader.ReadInt64() + HeaderSize;
			ResourceTree tree = new ResourceTree();
			ReadNode(reader, tree.Head, dataOffset, parentKeyPath: null);
			return tree;
		}
		catch (IOException ex)
		{
			throw new ResourceException(ex.Message, ex);
		}
	}

	private void ReadNode(BinaryReader reader, ResourceTree.Node parent, long dataOffset, string parentKeyPath)
	{
		NodeFlags flags = (NodeFlags)reader.ReadByte();
		int childCount = flags.HasFlag(NodeFlags.HasChildren) ? reader.ReadUInt16() : 0;
		string key = reader.ReadNullTerminatedString();
		string fullKeyPath = string.IsNullOrEmpty(parentKeyPath) ? key : parentKeyPath + "/" + key;
		Resource resource = null;
		if (flags.HasFlag(NodeFlags.HasResource))
		{
			ResourceTypeIdentifier identifier = (ResourceTypeIdentifier)reader.ReadUInt16();
			bool external = flags.HasFlag(NodeFlags.External);
			string path = external ? reader.ReadNullTerminatedString() : _path;
			long offset = reader.ReadUInt32();
			long size = reader.ReadUInt32();
			if (!external)
			{
				offset += dataOffset;
			}
			resource = new Resource(fullKeyPath, identifier, new FileResourceSource(path, offset, size, flags.HasFlag(NodeFlags.Compressed)));
		}
		// The root node has an empty key and is the tree's head
		ResourceTree.Node node = string.IsNullOrEmpty(key) ? parent : parent.Add(key, resource);
		for (int i = 0; i < childCount; i++)
		{
			ReadNode(reader, node, dataOffset, fullKeyPath);
		}
	}

	/// <summary>
	/// Writes every resource of the tree into this file. The data of each resource is copied into the file. Types
	/// with <see cref="ResourceType.CompressByDefault"/> are stored compressed.
	/// </summary>
	public void Write(ResourceTree tree)
	{
		try
		{
			using MemoryStream table = new MemoryStream();
			using MemoryStream data = new MemoryStream();
			WriteNode(tree.Head, new BinaryWriter(table), data);
			using FileStream output = new FileStream(_path, FileMode.Create, FileAccess.Write);
			BinaryWriter writer = new BinaryWriter(output);
			writer.Write(MagicNumber);
			writer.Write((byte)LatestSupportedVersion);
			writer.Write(table.Length);
			table.Position = 0;
			table.CopyTo(output);
			data.Position = 0;
			data.CopyTo(output);
		}
		catch (IOException ex)
		{
			throw new ResourceException(ex.Message, ex);
		}
	}

	private static void WriteNode(ResourceTree.Node node, BinaryWriter table, Stream data)
	{
		Resource resource = node.Resource;
		NodeFlags flags = 0;
		if (node.Children.Count > 0)
		{
			flags |= NodeFlags.HasChildren;
		}
		if (resource != null)
		{
			flags |= NodeFlags.HasResource;
			if (ResourceType.FromIdentifier(resource.Identifier).CompressByDefault)
			{
				flags |= NodeFlags.Compressed;
			}
		}
		table.Write((byte)flags);
		if (flags.HasFlag(NodeFlags.HasChildren))
		{
			table.Write(checked((ushort)node.Children.Count));
		}
		table.WriteNullTerminatedString(node.Key);
		if (resource != null)
		{
			table.Write((ushort)resource.Identifier);
			long start = data.Position;
			using (Stream source = flags.HasFlag(NodeFlags.Compressed) ? resource.Source.ReadCompressed() : resource.Source.ReadUncompressed())
			{
				source.CopyTo(data);
			}
			table.Write(checked((uint)start));
			table.Write(checked((uint)(data.Position - start)));
		}
		foreach (ResourceTree.Node child in node.Children)
		{
			WriteNode(child, table, data);
		}
	}
}
