using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SonicOrca.Resources;
using Xunit;

namespace S2HD.Tests;

/// <summary>The SORF resource file format, with small files made by the test. No game files are needed.</summary>
public sealed class ResourceFileTests : IDisposable
{
	private readonly string _directory = Directory.CreateTempSubdirectory("s2hd-tests-").FullName;

	public void Dispose() => Directory.Delete(_directory, recursive: true);

	[Fact]
	public void WriteThenScan_KeepsKeysTypesAndContent()
	{
		var expected = new Dictionary<string, (ResourceTypeIdentifier Type, byte[] Data)>
		{
			["SONICORCA/LEVELS/EHZ/MAP"] = (ResourceTypeIdentifier.Map, Encoding.UTF8.GetBytes("<map />")),
			["SONICORCA/LEVELS/EHZ/TILESET"] = (ResourceTypeIdentifier.TileSet, Encoding.UTF8.GetBytes("<tileset />")),
			["SONICORCA/OBJECTS/SONIC/SKIN"] = (ResourceTypeIdentifier.TexturePNG, Enumerable.Range(0, 5000).Select(i => (byte)i).ToArray()),
			["SONICORCA/EMPTY"] = (ResourceTypeIdentifier.Xml, Array.Empty<byte>()),
		};
		var tree = new ResourceTree();
		foreach (var (key, (type, data)) in expected)
		{
			string sourcePath = Path.Combine(_directory, Guid.NewGuid().ToString("N"));
			File.WriteAllBytes(sourcePath, data);
			tree.SetOrAdd(key, new Resource(key, type, new FileResourceSource(sourcePath, 0, data.Length)));
		}
		string archivePath = Path.Combine(_directory, "test.dat");

		new ResourceFile(archivePath).Write(tree);
		IDictionary<string, Resource> scanned = new ResourceFile(archivePath).Scan().GetResourceListing();

		Assert.Equal(expected.Keys.OrderBy(k => k), scanned.Keys.OrderBy(k => k));
		foreach (var (key, (type, data)) in expected)
		{
			Assert.Equal(type, scanned[key].Identifier);
			Assert.Equal(data, ReadAll(scanned[key].Source.ReadUncompressed()));
		}
	}

	[Fact]
	public void Scan_RejectsFileWithoutMagicNumber()
	{
		string path = Path.Combine(_directory, "bad.dat");
		File.WriteAllBytes(path, Encoding.ASCII.GetBytes("NOPE and some more bytes"));

		Assert.Throws<ResourceException>(() => new ResourceFile(path).Scan());
	}

	internal static byte[] ReadAll(Stream stream)
	{
		using (stream)
		using (var memory = new MemoryStream())
		{
			stream.CopyTo(memory);
			return memory.ToArray();
		}
	}
}
