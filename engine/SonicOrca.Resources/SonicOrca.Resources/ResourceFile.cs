using System;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Extensions;

namespace SonicOrca.Resources;

public class ResourceFile
{
	[Flags]
	private enum NodeFlags : byte
	{
		HasChildren = 2,
		HasResource = 4,
		External = 8,
		Compressed = 0x10
	}

	private static readonly char[] MagicNumberChars = new char[4] { 'S', 'O', 'R', 'F' };

	private const int LatestSupportedVersion = 2;

	private readonly string _path;

	private static int MagicNumber => BitConverter.ToInt32(Encoding.ASCII.GetBytes(MagicNumberChars), 0);

	public string Filename => _path;

	public ResourceFile(string path)
	{
		_path = path;
	}

	public override string ToString()
	{
		return _path;
	}

	public ResourceTree Scan()
	{
		try
		{
			using FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read);
			BinaryReader binaryReader = new BinaryReader(fileStream);
			if (binaryReader.ReadInt32() != MagicNumber)
			{
				throw new ResourceException("Invalid resource file.");
			}
			if (binaryReader.ReadByte() > 2)
			{
				throw new ResourceException("Unsupport resouce file version.");
			}
			long dataOffset = binaryReader.ReadInt64() + 13;
			ResourceTree resourceTree = new ResourceTree();
			ScanTableEntry(resourceTree.Head, fileStream, dataOffset);
			return resourceTree;
		}
		catch (IOException ex)
		{
			throw new ResourceException(ex.Message, ex);
		}
	}

	private void ScanTableEntry(ResourceTree.Node resourceNode, Stream stream, long dataOffset, string fullKeyPath = null)
	{
		BinaryReader binaryReader = new BinaryReader(stream);
		NodeFlags nodeFlags = (NodeFlags)binaryReader.ReadByte();
		ushort num = 0;
		if (nodeFlags.HasFlag(NodeFlags.HasChildren))
		{
			num = binaryReader.ReadUInt16();
		}
		string text = binaryReader.ReadNullTerminatedString();
		if (!string.IsNullOrEmpty(fullKeyPath))
		{
			fullKeyPath += "/";
		}
		fullKeyPath += text;
		Resource resource = null;
		if (nodeFlags.HasFlag(NodeFlags.HasResource))
		{
			ResourceTypeIdentifier identifier = (ResourceTypeIdentifier)binaryReader.ReadUInt16();
			string path = (nodeFlags.HasFlag(NodeFlags.External) ? binaryReader.ReadNullTerminatedString() : _path);
			long num2 = binaryReader.ReadUInt32();
			long size = binaryReader.ReadUInt32();
			if (!nodeFlags.HasFlag(NodeFlags.External))
			{
				num2 += dataOffset;
			}
			resource = new Resource(fullKeyPath, identifier, new FileResourceSource(path, num2, size, nodeFlags.HasFlag(NodeFlags.Compressed)));
		}
		if (!string.IsNullOrEmpty(text))
		{
			resourceNode = resourceNode.Add(text, resource);
		}
		for (uint num3 = 0u; num3 < num; num3++)
		{
			ScanTableEntry(resourceNode, stream, dataOffset, fullKeyPath);
		}
	}

	public void Write(ResourceTree tree)
	{
		MemoryStream memoryStream = new MemoryStream();
		MemoryStream memoryStream2 = new MemoryStream();
		try
		{
			WriteTableEntry(tree.Head, memoryStream, memoryStream2);
			using FileStream output = new FileStream(_path, FileMode.Create, FileAccess.Write);
			BinaryWriter binaryWriter = new BinaryWriter(output);
			binaryWriter.Write(MagicNumber);
			binaryWriter.Write((byte)2);
			binaryWriter.Write(memoryStream.Position);
			binaryWriter.Write(memoryStream.ToArray());
			binaryWriter.Write(memoryStream2.ToArray());
		}
		catch (IOException ex)
		{
			throw new ResourceException(ex.Message, ex);
		}
		finally
		{
			memoryStream.Dispose();
			memoryStream2.Dispose();
		}
	}

	private void WriteTableEntry(ResourceTree.Node resourceNode, Stream tableStream, Stream dataStream)
	{
		BinaryWriter binaryWriter = new BinaryWriter(tableStream);
		new BinaryWriter(dataStream);
		NodeFlags nodeFlags = (NodeFlags)0;
		if (resourceNode.Resource != null)
		{
			ResourceType resourceType = ResourceType.FromIdentifier(resourceNode.Resource.Identifier);
			if (resourceType != null && resourceType.CompressByDefault)
			{
				nodeFlags |= NodeFlags.Compressed;
			}
		}
		if (resourceNode.Children.Count > 0)
		{
			nodeFlags |= NodeFlags.HasChildren;
		}
		if (resourceNode.Resource != null)
		{
			nodeFlags |= NodeFlags.HasResource;
			if (resourceNode.Resource.Source is FileResourceSource fileResourceSource && string.Compare(fileResourceSource.Path, _path, ignoreCase: true) != 0)
			{
				nodeFlags |= NodeFlags.External;
			}
			nodeFlags &= ~NodeFlags.External;
		}
		binaryWriter.Write((byte)nodeFlags);
		if (nodeFlags.HasFlag(NodeFlags.HasChildren))
		{
			binaryWriter.Write((short)resourceNode.Children.Count);
		}
		binaryWriter.WriteNullTerminatedString(resourceNode.Key);
		if (nodeFlags.HasFlag(NodeFlags.HasResource))
		{
			binaryWriter.Write((ushort)resourceNode.Resource.Identifier);
			if (nodeFlags.HasFlag(NodeFlags.External))
			{
				FileResourceSource fileResourceSource2 = resourceNode.Resource.Source as FileResourceSource;
				binaryWriter.WriteNullTerminatedString(fileResourceSource2.Path);
				binaryWriter.Write((uint)fileResourceSource2.Offset);
				binaryWriter.Write((uint)resourceNode.Resource.Source.Size);
			}
			else
			{
				binaryWriter.Write((uint)dataStream.Position);
				long position = dataStream.Position;
				using (Stream stream = (nodeFlags.HasFlag(NodeFlags.Compressed) ? resourceNode.Resource.Source.ReadCompressed() : resourceNode.Resource.Source.ReadUncompressed()))
				{
					stream.CopyTo(dataStream);
				}
				position = dataStream.Position - position;
				binaryWriter.Write((uint)position);
			}
		}
		foreach (ResourceTree.Node child in resourceNode.Children)
		{
			WriteTableEntry(child, tableStream, dataStream);
		}
	}

	public static void GetResourcesFromDirectory(ResourceTree tree, string path, string currentFullKeyPath = null)
	{
		if (!string.IsNullOrEmpty(currentFullKeyPath))
		{
			if (!currentFullKeyPath.EndsWith("/"))
			{
				currentFullKeyPath += "/";
			}
		}
		else
		{
			currentFullKeyPath = string.Empty;
		}
		currentFullKeyPath += Path.GetFileName(path).ToUpper();
		string[] files = Directory.GetFiles(path);
		foreach (string text in files)
		{
			FileInfo fileInfo = new FileInfo(text);
			if (fileInfo.Name.Contains("."))
			{
				string text2 = fileInfo.Name.Remove(fileInfo.Name.IndexOf('.')).Replace('_', '/').ToUpper();
				string extension = fileInfo.Name.Substring(fileInfo.Name.IndexOf('.'));
				string fullKeyPath = ((text2 == "/") ? currentFullKeyPath : (currentFullKeyPath + "/" + text2));
				ResourceType resourceType = ResourceType.RegisteredResourceTypes.FirstOrDefault((ResourceType x) => x.DefaultExtension == extension);
				if (resourceType != null)
				{
					Resource resource = new Resource(fullKeyPath, resourceType.Identifier, new FileResourceSource(text, 0L, fileInfo.Length));
					tree.GetOrAdd(fullKeyPath).Resource = resource;
				}
			}
		}
		files = Directory.GetDirectories(path);
		foreach (string path2 in files)
		{
			GetResourcesFromDirectory(tree, path2, currentFullKeyPath);
		}
	}
}
